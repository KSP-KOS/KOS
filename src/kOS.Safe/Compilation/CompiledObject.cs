using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.Safe.Execution;
using kOS.Safe.Persistence;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace kOS.Safe.Compilation
{
    
    /// <summary>
    /// The class that controls the ability to make a compiled object
    /// file out of the opcode lists the compiler makes.  This is
    /// the engine behind the COMPILE command.
    /// 
    /// -----------------------------------------------------------
    /// Please do not attempt to understand this code until after
    /// you've read the file CompiledObject-doc.md located in this
    /// same directory.
    /// -----------------------------------------------------------
    /// 
    /// Knowing the algorithm, design, and format of the saved ML file will
    /// make this a lot easier to follow and understand.
    /// 
    /// </summary>
    public static class CompiledObject
    {
        /// <summary>A homemade magic number for file identifcation, that will
        /// appear as the first part of the file contents and is hopefully
        /// unique and unlike anything another file type uses:</summary>
        public static IEnumerable<byte> MagicId { get { return magicId; } }
        private static readonly byte[] magicId = { (byte)'k', 0x03, (byte)'X', (byte)'E' };
        private static readonly Regex trailingDigitsRegex = new Regex(@"\d+$");
        private static Dictionary<Type,byte> idFromType;
        private static Dictionary<byte,Type> typeFromId;
        
        /// <summary>
        /// Populate the above two lookup dictionaries.
        /// </summary>
        public static void InitTypeData()
        {
            idFromType = new Dictionary<Type,byte>();
            typeFromId = new Dictionary<byte,Type>();
            
            // WARNING:  All the possible object types supported as arguments
            // to Opcodes in the CPU (everything with an [MLField] attribute
            // in an Opcode) must be mentioned here or compilation of programs
            // containing that opcode will not be storable as compiled files.
            //
            // TODO: Make some sort of Assertion to run during OnAwake that will
            // verify this is the case and give a Nag Message if it's not.
            //
            AddTypeData(0, typeof(PseudoNull));
            AddTypeData(1, typeof(bool));
            AddTypeData(2, typeof(byte));
            AddTypeData(3, typeof(Int16));
            AddTypeData(4, typeof(Int32));
            AddTypeData(5, typeof(float));
            AddTypeData(6, typeof(double));
            AddTypeData(7, typeof(string));
            AddTypeData(8, typeof(KOSArgMarkerType));
            AddTypeData(9, typeof(ScalarIntValue));
            AddTypeData(10, typeof(ScalarDoubleValue));
            AddTypeData(11, typeof(BooleanValue));
            AddTypeData(12, typeof(StringValue));
        }
        
        private static void AddTypeData(int byteType, Type csType)
        {
            idFromType.Add(csType, (byte)byteType);
            typeFromId.Add((byte)byteType, csType);
        }
        
        /// <summary>
        /// Given the need for a number to hold this many possible unique values, return how many bytes is has to be.
        /// (i.e. it takes 1 byte to hold up to 256 different values, 2 bytes to hold up to 65536 values, etc).
        /// </summary>
        /// <param name="maxUniqueValues">max number of unique values the bytes have to store</param>
        /// <returns>number of bytes to hold it</returns>
        public static int FewestBytesToHold(long maxUniqueValues)
        {
            return  (maxUniqueValues <= 0x100) ? 1 :
                    (maxUniqueValues <= 0x10000) ? 2 :
                    (maxUniqueValues <= 0x1000000) ? 3 :
                    (maxUniqueValues <= 0x100000000) ? 4 :
                    (maxUniqueValues <= 0x10000000000) ? 5 :
                    (maxUniqueValues <= 0x1000000000000) ? 6 :
                    (maxUniqueValues <= 0x100000000000000) ? 7 : 8 ;
        }
        
        /// <summary>
        /// Create a packing of the number given into the number of
        /// bytes given, using a homegrown packing algorithm that's
        /// always big-endian and allows weird oddball numbers of
        /// bytes, like 3 or 5, which the default BinaryWriter routines
        /// won't do.
        /// </summary>
        /// <param name="number">number to encode</param>
        /// <param name="numBytes">bytes to use</param>
        /// <returns>resulting array of bytes</returns>
        public static byte[] EncodeNumberToNBytes(ulong number, int numBytes)
        {
            // Encode the index into the right number of bytes:
            var returnValue = new byte[numBytes];
            for (int bNum = 0; bNum < numBytes ; ++bNum)
            {
                int bytesToShift = (numBytes-bNum) - 1;
                // This ends up being big-endian.  Dunno if that's the standard,
                // but as long as the reading back is consistent it's fine:
                returnValue[bNum] = (byte)( (number>>((bytesToShift)*8)) & 0xff);
            }
            return returnValue;
        }

        /// <summary>
        /// The inverse operation of EncodeNumberToNBytes().  Given a packed
        /// byte array of the encoded version, return the decoded version.
        /// </summary>
        /// <param name="encodedForm">The byte array holding the encoded pack of bytes</param>
        /// <returns>resulting value - returned as ulong just in case it's big, but can be casted down to smaller formats</returns>
        public static ulong DecodeNumberFromBytes(byte[] encodedForm)
        {
            int numBytes = encodedForm.Length;
            ulong returnValue = 0;
            for (int bNum = 0; bNum < numBytes ; ++bNum)
            {
                int bytesToShift = (numBytes-bNum) - 1;
                // Read it back assuming it's big-endian, because that's how EncodeNumberToNBytes does it:
                returnValue += ((ulong)encodedForm[bNum]) << (bytesToShift*8);
            }
            return returnValue;
        }
        
        
        /// <summary>
        /// Holds all previously packed arguments to ML instructions.
        /// </summary>
        private static byte[] argumentPack;
        
        /// <summary>
        /// For efficiency, argumentPack will grow by 2x whenever it
        /// needs to expand.  This tracks how far into argumentPack is
        /// its logical length, which can be shorter than its physical
        /// length.
        /// </summary>
        private static int argumentPackLogicalLength;

        /// <summary>
        /// Holds the mapping of line numbers from source to the ranges of the
        /// machine language section of the file where the opcodes derived
        /// from those line numbers are:
        /// </summary>
        private static DebugLineMap lineMap;

        /// <summary>
        /// A memory stream writer being used to temporarily pack and unpack values so
        /// we can borrow the standard algorithms for that.
        /// </summary>
        private static BinaryWriter packTempWriter;

        /// <summary>
        /// Holds a list of previously encountered machine language arguments,
        /// and the byte index at which they were inserted into the argumentPack array,
        /// and the length within the list they take up.
        /// </summary>
        private static Dictionary<object,int> argumentPackFinder;
        
        private static string previousLabel = "######"; // bogus value that is ensured to differ from any real value the first time through.

        /// <summary>
        /// Returns the compiled program's opcodes packed into a tight form, that is a direct
        /// streamed dump of the list of opcodes.
        /// </summary>
        /// <param name="program">The list of opcode codeparts for the compiled program.</param>
        /// <returns>The packed bytes that should be written to the binary file.</returns>
        public static byte[] Pack(List<CodePart> program)
        {
            packTempWriter = new BinaryWriter( new MemoryStream() );
            var allCodeBuff = new List<byte>();
            var headBuff = new List<byte>();
            argumentPack = new byte[8]; // this will grow bigger (be replaced by new arrays) as needed.
            argumentPackLogicalLength = 0; // nothing in the argumentPack yet.
            argumentPackFinder = new Dictionary<object,int>();
            lineMap = new DebugLineMap();
            previousLabel = "######"; // bogus value that is ensured to differ from any real value the first time through.

            for (int index = 0 ; index < program.Count ; ++index)  // --.    This can be replaced with a
            {                                                      //   |--- foreach.  I do it this way so I
                CodePart codePart = program[index];                // --'    can print the index in debugging.
                PackArgs(codePart.FunctionsCode);
                PackArgs(codePart.InitializationCode);
                PackArgs(codePart.MainCode);
            }

            // Purpose of numArgIndexBytes calculated below:
            // The first thing in the argument pack section after the "%A" idicator
            // is a single byte that tells how big the addresses into the argment pack
            // are.  The argument pack might address arguments using a single byte if
            // the argument pack is small enough that you don't need addresses bigger than
            // 255.  If you need addresses bigger than 255, then it might take two bytes
            // to store addresses so it can cover values up to 65535, and so on.
            int argSectionHeaderBytes = 3; // Adjust this if you add or subtract headBuf.Add() lines below:
            int numArgIndexBytes = FewestBytesToHold(argumentPackLogicalLength + argSectionHeaderBytes);
            headBuff.Add((byte)'%');
            headBuff.Add((byte)'A');
            headBuff.Add(((byte)numArgIndexBytes));
            // ^^^ IF YOU ADD OR REMOVE ANY headBuff.Add(...) LINES ABOVE YOU MUST ALSO CHANGE argSectionHeaderBytes.

            var truncatedArgumentPack = new byte[argumentPackLogicalLength];
            Array.Copy(argumentPack, 0, truncatedArgumentPack, 0, argumentPackLogicalLength);

            headBuff.AddRange(truncatedArgumentPack);

            for (int index = 0 ; index < program.Count ; ++index)  // --.    This can be replaced with a
            {                                                      //   |--- foreach.  I do it this way so I
                CodePart codePart = program[index];                // --'    can print the index in debugging.

                var codeBuff = new List<byte>();

                int indexSoFar = allCodeBuff.Count + codeBuff.Count;
                codeBuff.Add((byte)'%');
                codeBuff.Add((byte)'F');
                byte[] packedCode = PackCode(codePart.FunctionsCode, numArgIndexBytes, indexSoFar+2);
                codeBuff.AddRange(packedCode);

                indexSoFar = allCodeBuff.Count + codeBuff.Count;
                codeBuff.Add((byte)'%');
                codeBuff.Add((byte)'I');
                packedCode = PackCode(codePart.InitializationCode, numArgIndexBytes, indexSoFar+2);
                codeBuff.AddRange(packedCode);

                indexSoFar = allCodeBuff.Count + codeBuff.Count;
                codeBuff.Add((byte)'%');
                codeBuff.Add((byte)'M');
                packedCode = PackCode(codePart.MainCode, numArgIndexBytes, indexSoFar+2);
                codeBuff.AddRange(packedCode);

                allCodeBuff.AddRange(codeBuff);
            }

            var everything = new List<byte>();
            everything.AddRange(MagicId);
            everything.AddRange(headBuff);
            everything.AddRange(allCodeBuff);
            everything.AddRange(lineMap.Pack());
            using (var compressedStream = new MemoryStream())
            {
                using (var csStream = new GZipOutputStream(compressedStream))
                {
                    csStream.Write(everything.ToArray(), 0, everything.Count);
                    csStream.Flush();
                }
                return compressedStream.ToArray();
            }
        }
        
        /// <summary>
        /// Read the program fragment represented by this list of opcodes and packs all args it sees.
        /// </summary>
        /// <param name="fragment">the section being packed</param>
        private static void PackArgs(List<Opcode> fragment)
        {
            for (int index = 0; index < fragment.Count ; ++index)
            {
                Opcode op = fragment[index];
                string expectedLabel = NextConsecutiveLabel(previousLabel);

                // in cases where there's a gap or jump in the consecutive labels, we'll be needing
                // that label value in the argument pack to refer to later:
                if ( (! string.IsNullOrEmpty(op.Label)) &&
                      (op.Label != expectedLabel) )
                    PackedArgumentLocation(op.Label);

                IEnumerable<MLArgInfo> args = op.GetArgumentDefs();
                foreach (MLArgInfo arg in args)
                {
                    object argVal = arg.PropertyInfo.GetValue(op,null);

                    // Just trying to add the argument to the pack.  Don't
                    // care where in the pack it is (yet).
                    PackedArgumentLocation(argVal);
                }
                
                previousLabel = op.Label;
            }
        }
        
        /// <summary>
        /// Pack the program fragment represented by this list of opcodes into ML code.
        /// </summary>
        /// <param name="fragment">the section being packed</param>
        /// <param name="argIndexSize">Number of bytes to use to encode argument indexes into the arg section.</param>
        /// <param name="startByteIndex">this many bytes will be removed from the start of each fragment when debugging</param>
        /// <returns>the byte array of the packed together arguments</returns>
        private static byte[] PackCode(List<Opcode> fragment, int argIndexSize, int startByteIndex)
        {
            packTempWriter.Seek(0,SeekOrigin.Begin);
            bool justInsertedLabel = false;
            for (int index = 0; index < fragment.Count ; ++index)
            {
                Opcode op = fragment[index];
                string expectedLabel = NextConsecutiveLabel(previousLabel);

                if (justInsertedLabel)
                {
                    justInsertedLabel = false;
                }
                else if ( (! string.IsNullOrEmpty(op.Label)) &&
                          (op.Label != expectedLabel) )
                {
                    op = new OpcodeLabelReset(op.Label) { SourcePath = op.SourcePath, SourceLine = op.SourceLine } ;
                    --index;
                    justInsertedLabel = true;
                }
                
                var opcodeStartByte = (int)( startByteIndex + packTempWriter.BaseStream.Position );
                var code = (byte)op.Code;
                
                //Always start with the opcode's bytecode:
                packTempWriter.Write(code);
                
                // Then append a number of argument indexes depending
                // on how many arguments the opcode is supposed to have:
                IEnumerable<MLArgInfo> args = op.GetArgumentDefs();
                foreach (MLArgInfo arg in args)
                {
                    object argVal = arg.PropertyInfo.GetValue(op,null);
                    int argPackedIndex = PackedArgumentLocation(argVal);
                    byte[] argIndexEncoded = EncodeNumberToNBytes((ulong)argPackedIndex,argIndexSize);
                    packTempWriter.Write(argIndexEncoded);
                }
                
                // Now add this range to the Debug line mapping for this source line:
                var opcodeStopByte = (int)(startByteIndex + packTempWriter.BaseStream.Position - 1);
                lineMap.Add(op.SourceLine, new IntRange(opcodeStartByte,opcodeStopByte));
                
                previousLabel = op.Label;
            }
            
            // Return the byte array that the memory writer has been outputting to in the above loop:
            var mem = packTempWriter.BaseStream as MemoryStream;
            var returnVal = new Byte[mem.Position];
            Array.Copy(mem.GetBuffer(), 0, returnVal, 0, (int)mem.Position);
            
            return returnVal;
        }
        
        /// <summary>
        /// Given an argument to some Opcode, add it to the argument pack,
        /// or don't if it's already there.  In either case, return the index
        /// into where it is in the argument pack.
        /// </summary>
        /// <param name="argument">Thing to pack</param>
        /// <returns>byte index of where it starts in the argument pack.</returns>
        private static int PackedArgumentLocation(object argument)
        {
            const int LABEL_OFFSET = 3; // Account for the %An at the front of the argument pack.
            
            object arg = argument ?? new PseudoNull();
            
            int returnValue; // bogus starting value before it's calculated.
            bool existsAlready = argumentPackFinder.TryGetValue(arg, out returnValue);
            if (existsAlready)
                return returnValue;
            
            // Okay, so have to encode it and add it.
            // --------------------------------------
            
            // When it gets added, it's going to be tacked on right at the end.
            // We already know that, se let's get that populated now:
            argumentPackFinder.Add(arg, LABEL_OFFSET + argumentPackLogicalLength);
            returnValue = LABEL_OFFSET + argumentPackLogicalLength;
            
            // Borrow C#'s Binary IO writer to pack the object into the byte form,
            // rather than writing our own for each type:
            packTempWriter.Seek(0,SeekOrigin.Begin);
            
            WriteSomeBinaryPrimitive(packTempWriter, arg);
            var mem = packTempWriter.BaseStream as MemoryStream;
            var argByteLength = (int)(mem.Position);
            var packedArg = new byte[argByteLength+1]; // +1 because we insert the type byte at the front.
            packedArg[0] = idFromType[arg.GetType()];
            Array.Copy(mem.GetBuffer(), 0, packedArg, 1, argByteLength);
                
            AddByteChunkToArgumentPack(packedArg);
            
            return returnValue;
        }
        
        /// <summary>
        /// It's surprising that BinaryWriter.Write doesn't have a method that does the
        /// equivalent of this and allows any object of the types it knows how to write:
        /// </summary>
        /// <param name="writer">the stream to write to</param>
        /// <param name="obj">the thing to write</param>
        private static void WriteSomeBinaryPrimitive(BinaryWriter writer, object obj)
        {
            if      (obj is PseudoNull) { /* do nothing.  for a null the type byte code is enough - no further data. */ }
            else if (obj is KOSArgMarkerType) { /*do nothing,  for this type has no data*/ }
            else if (obj is Boolean)    writer.Write((bool)obj);
            else if (obj is Int32)      writer.Write((Int32)obj);
            else if (obj is String)     writer.Write((String)obj);
            else if (obj is Double)     writer.Write((Double)obj);
            else if (obj is Single)     writer.Write((Single)obj);
            else if (obj is Byte)       writer.Write((byte)obj);
            else if (obj is Char)       writer.Write((char)obj);
            else if (obj is Decimal)    writer.Write((Decimal)obj);
            else if (obj is Int16)      writer.Write((Int16)obj);
            else if (obj is Int64)      writer.Write((Int64)obj);
            else if (obj is UInt16)     writer.Write((UInt16)obj);
            else if (obj is UInt32)     writer.Write((UInt32)obj);
            else if (obj is UInt64)     writer.Write((UInt64)obj);
            else if (obj is SByte)      writer.Write((SByte)obj);
            else if (obj is ScalarIntValue) writer.Write(((ScalarIntValue)obj).GetIntValue());
            else if (obj is ScalarDoubleValue) writer.Write(((ScalarDoubleValue)obj).GetDoubleValue());
            else if (obj is BooleanValue) writer.Write((BooleanValue)obj);
            else if (obj is StringValue) writer.Write((StringValue)obj);
            else
                throw new Exception("Don't konw how to write this type of object to binary file: " + obj.GetType().Name);
        }
        /// <summary>
        /// It's surprising that BinaryWriter.Read doesn't have a method that does the
        /// equivalent of this and allow you to pass in the Type as a parameter:
        /// </summary>
        /// <param name="reader">the stream to read from</param>
        /// <param name="cSharpType">the expected type of object stored here</param>
        private static object ReadSomeBinaryPrimitive(BinaryReader reader, Type cSharpType)
        {
            object returnValue = null;
            
            if      (cSharpType == typeof(PseudoNull)) { /* do nothing.  for a null the type byte code is enough - no further data. */ }
            else if (cSharpType == typeof(KOSArgMarkerType)) returnValue = new KOSArgMarkerType(); // no packed data - just make a default one.
            else if (cSharpType == typeof(Boolean))    returnValue = reader.ReadBoolean();
            else if (cSharpType == typeof(Int32))      returnValue = reader.ReadInt32();
            else if (cSharpType == typeof(String))     returnValue = reader.ReadString();
            else if (cSharpType == typeof(Double))     returnValue = reader.ReadDouble();
            else if (cSharpType == typeof(Single))     returnValue = reader.ReadSingle();
            else if (cSharpType == typeof(Byte))       returnValue = reader.ReadByte();
            else if (cSharpType == typeof(Char))       returnValue = reader.ReadChar();
            else if (cSharpType == typeof(Decimal))    returnValue = reader.ReadDecimal();
            else if (cSharpType == typeof(Int16))      returnValue = reader.ReadInt16();
            else if (cSharpType == typeof(Int64))      returnValue = reader.ReadInt64();
            else if (cSharpType == typeof(UInt16))     returnValue = reader.ReadUInt16();
            else if (cSharpType == typeof(UInt32))     returnValue = reader.ReadUInt32();
            else if (cSharpType == typeof(UInt64))     returnValue = reader.ReadUInt64();
            else if (cSharpType == typeof(SByte))      returnValue = reader.ReadSByte();
            else if (cSharpType == typeof(ScalarIntValue)) returnValue = ScalarValue.Create(reader.ReadInt32());
            else if (cSharpType == typeof(ScalarDoubleValue)) returnValue = ScalarValue.Create(reader.ReadDouble());
            else if (cSharpType == typeof(BooleanValue)) returnValue = new BooleanValue(reader.ReadBoolean());
            else if (cSharpType == typeof(StringValue)) returnValue = new StringValue(reader.ReadString());
            else
                throw new Exception("Don't know how to read this type of object from binary file: " + cSharpType.Name);
            return returnValue;
        }
        
        /// <summary>
        /// Add another block of bytes to the end of argumentPack, moving
        /// argumentPackLogiclLength, and expanding its physical size if
        /// need be.
        /// </summary>
        /// <param name="appendMe">block of bytes to append</param>
        private static void AddByteChunkToArgumentPack(byte[] appendMe)
        {
            int newLogicalLength = argumentPackLogicalLength + appendMe.Length;
            if (newLogicalLength > argumentPack.Length)
            {
                // Increase to double current size or if current size is too small or zero
                // so doubling it doesn't help, then incrase to hold new logical length:
                var newBiggerPack = new byte[ Math.Max(argumentPack.Length*2, newLogicalLength) ];
                if (argumentPackLogicalLength > 0)
                {
                    Array.Copy(argumentPack,newBiggerPack,argumentPackLogicalLength);
                }
                argumentPack = newBiggerPack;
            }
            Array.Copy(appendMe, 0, argumentPack, argumentPackLogicalLength, appendMe.Length);
            argumentPackLogicalLength = newLogicalLength;
        }


        /// <summary>
        /// Given a packed representation of the program, load it back into program form:
        /// </summary>
        /// <param name="path">path of file (with preceeding "volume:") that the program came from, for runtime error reporting.</param>
        /// <param name="prefix">prepend this string to all labels in this program, with the exception
        /// of when this program is calling the @LR LoadRunner labels.</param>
        /// <param name="content">the file itself in ony big binary array.</param>
        /// <returns></returns>
        public static List<CodePart> UnPack(GlobalPath path, string prefix, byte[] content)
        {
            var program = new List<CodePart>();
            var reader = new BinaryReader(new MemoryStream(content));
            
            byte[] firstFour = reader.ReadBytes(4);
            if (firstFour.SequenceEqual(PersistenceUtilities.GzipHeader))
            {
                reader.Close();

                var zipStream = new GZipInputStream(new MemoryStream(content));
                var decompressedStream = new MemoryStream();
                var buffer = new byte[4096];
                int read;
                while ((read = zipStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    decompressedStream.Write(buffer, 0, read);
                }
                decompressedStream.Seek(0, SeekOrigin.Begin);
                reader = new BinaryReader(decompressedStream);

                firstFour = reader.ReadBytes(4);
            }
            
            if (! firstFour.SequenceEqual(MagicId))
                throw new Exception("Attempted to read an ML file that doesn't seem to be an ML file");
            
            int argIndexSize;
            Dictionary<int,object> arguments = ReadArgumentPack(reader, out argIndexSize);
            lineMap = new DebugLineMap();
            
            previousLabel = "######"; // bogus value that is ensured to differ from any real value the first time through.

            int codeStart = 0;
            
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                // In principle, the delimiter should *always* be the next byte when this loop starts and
                // the following loop should just read that one character:
                while (reader.BaseStream.Position < reader.BaseStream.Length && reader.ReadByte() != (byte)(ByteCode.DELIMITER))
                {
                }
                
                if (reader.BaseStream.Position >= reader.BaseStream.Length)
                    continue;
                
                byte sectionTypeId = reader.ReadByte();
                switch (sectionTypeId)
                {
                    case (byte)'F':
                        // new CodePart's always start with the function header:
                        var nextPart = new CodePart();
                        program.Add(nextPart);
                        // If this is the very first code we've ever encountered in the file, remember its position:
                        if (codeStart == 0)
                            codeStart = (int)(reader.BaseStream.Position - 2); // start is where the ByteCode.DELIMITER of the first section is.

                        program[program.Count-1].FunctionsCode = ReadOpcodeList(reader, codeStart, prefix, arguments, argIndexSize);
                        break;
                    case (byte)'I':
                        program[program.Count-1].InitializationCode = ReadOpcodeList(reader, codeStart, prefix, arguments, argIndexSize);
                        break;
                    case (byte)'M':
                        program[program.Count-1].MainCode = ReadOpcodeList(reader, codeStart, prefix, arguments, argIndexSize);
                        break;
                    case (byte)'D':
                        lineMap = new DebugLineMap(reader);
                        break;
                }
            }
            reader.Close();

            PostReadProcessing(program, path, lineMap);

            return program;
        }
        
        /// <summary>
        /// Fills an argument dictionary from the given reader, assuming its been positioned
        /// at the start of the argument pack's "%A".
        /// </summary>
        /// <param name="reader">The BinaryReader to read from.</param>
        /// <returns>The dictionary mapping indeces within the argument pack to the object that was stored there.</returns>
        private static Dictionary<int,object> ReadArgumentPack(BinaryReader reader, out int argIndexSize)
        {
            var returnArgs = new Dictionary<int,object>();

            var startPos = (int) reader.BaseStream.Position;
            
            byte[] header = reader.ReadBytes(2);
            if ( header[0] != '%' || header[1] != 'A')
                throw new Exception("Attempted to read an ML file that doesn't have an Argument Pack in it.");

            argIndexSize = reader.ReadByte();

            bool sectionEnded = false;
            while (reader.BaseStream.Position < reader.BaseStream.Length && !(sectionEnded))
            {
                int offsetLocation = (int)(reader.BaseStream.Position) - startPos;
                
                byte argTypeId = reader.ReadByte();
                Type argCSharpType;
                if (typeFromId.TryGetValue(argTypeId, out argCSharpType))
                {
                    object arg = ReadSomeBinaryPrimitive(reader, argCSharpType);
                    returnArgs.Add(offsetLocation,arg);
                }
                else
                {
                    // Just read something that wasn't a proper type ID, so un-read that byte, and finish:
                    reader.BaseStream.Seek(-1,SeekOrigin.Current);
                    sectionEnded = true;
                }
            }
            return returnArgs;
        }

        /// <summary>
        /// Go back and re-assign all the line number labels and location labels to their
        /// proper values.
        /// </summary>
        /// <param name="program">recently built program parts to re-assign.</param>
        /// <param name="path">path of file (with preceeding volume:) that the compiled code came from, for rutime error reporting purposes.</param>
        /// <param name="lineMap">describes the mapping of line numbers to code locations.</param>
        private static void PostReadProcessing(IEnumerable<CodePart> program, GlobalPath path, DebugLineMap lineMap)
        {
            //TODO:prefix is never used.
            SortedList<IntRange,int> lineLookup = lineMap.BuildInverseLookup();
            var lineEnumerator = lineLookup.GetEnumerator();
            
            int curLine = 0;
            var curRange = new IntRange(-1,-1);

            //TODO:This is never used, just incremented. remove?
            int opIndex = 0;
            
            foreach (CodePart part in program)
            {
                // It's easier to iterate over the sections in the CodePart this way:
                var sections = new List<List<Opcode>>
                {
                    part.FunctionsCode, 
                    part.InitializationCode, 
                    part.MainCode
                };

                foreach (List<Opcode> codeList in sections)
                {
                    foreach (Opcode op in codeList)
                    {
                        ++opIndex; // First opIndex is called 1, not 0.  This matches the behavior of Compiler.cs's AddOpcode()'s call to GetNextIndex().
                        
                        // The algorithm is dependent on the fact that the opcodes will be iterated over
                        // in the same order as they appeared in the ML file, and therefore the
                        // DebugLineMap only ever has to be advanced forward, never backward.
                        while (op.MLIndex > curRange.Stop)
                        {
                            if (lineEnumerator.MoveNext())
                            {
                                curRange = lineEnumerator.Current.Key;
                                curLine = lineEnumerator.Current.Value;
                            }
                            else
                                break; // This shouldn't happen.  It means the %D section was written out wrong.
                        }
                        if (curRange.Start <= op.MLIndex && op.MLIndex <= curRange.Stop)
                            op.SourceLine = (short)curLine;
                        else
                            // Not every opcode came from a source line - so if it's skipped over, assign it to bogus value.
                            op.SourceLine = -1;

                        op.SourcePath = (op.SourcePath == null || op.SourcePath == GlobalPath.EMPTY) ? path : GlobalPath.EMPTY;

                    }
                }
            }
        }
        
        /// <summary>
        /// Generate what the expected next consecutive label string would be
        /// given an existing label string.  For example, if given "@_0001"
        /// it should return "@_0002".
        /// </summary>
        /// <param name="label">label to test</param>
        /// <returns>Next label string</returns>
        private static string NextConsecutiveLabel( string label)
        {
            string outLabel;
            Match m = trailingDigitsRegex.Match(label);
            if (m.Success)
            {
                int number = Convert.ToInt32(label.Substring(m.Index,m.Length));
                string padFormat = "{0:" + new String('0', m.Length) + "}";
                outLabel = label.Substring(0,m.Index) + String.Format(padFormat,number+1);
            }
            else
                outLabel = label + "1"; // no digits already, so append one.
            return outLabel;
        }

        /// <summary>
        /// Creates the list of opcodes for one of the code sections of a codepart, assuming the
        /// BinaryReader is starting at the byte right after the identifying header '%F', '%I', or '%M'.
        /// </summary>
        /// <param name="reader">binary reader to read from.</param>
        /// <param name="codeStartPos">index into the stream where the first code block in the ML file started,
        /// for calculating indeces.</param>
        /// <param name="prefix">prefix to prepend to all labels within this program, with the
        /// exception of cases where this program is calling the @LR load runner labels.</param>
        /// <param name="arguments">argument dictionary to pull arguments from.</param>
        /// <param name="argIndexSize">number of bytes the argument indeces are packed into.</param>
        /// <returns>list of opcodes generated</returns>
        private static List<Opcode> ReadOpcodeList(BinaryReader reader, int codeStartPos, string prefix, Dictionary<int,object> arguments, int argIndexSize)
        {            
            var returnValue = new List<Opcode>();
            
            bool sectionEnded = false;
            bool prevWasLabelReset = false;
            
            while (reader.BaseStream.Position < reader.BaseStream.Length && !(sectionEnded))
            {
                var opcodeMLPosition = (int)(reader.BaseStream.Position - codeStartPos); // For later use in PostReadProcessing().
                byte opCodeTypeId = reader.ReadByte();
                Type opCodeCSharpType = Opcode.TypeFromCode((ByteCode)opCodeTypeId);
                
                if (opCodeCSharpType == typeof(PseudoNull))
                {
                    // As soon as there's an opcode encountered that isn't a known opcode type, the section is done:
                    sectionEnded = true;
                    // Un-read the byte that wasn't an opcode (it's probably the '%' delimiter for the next section):
                    reader.BaseStream.Seek(-1,SeekOrigin.Current);
                    continue;
                }

                // Make a new empty Opcode instance of this type:
                var op = (Opcode)(Activator.CreateInstance(opCodeCSharpType,true));
                op.MLIndex = opcodeMLPosition; // For later use in PostReadProcessing().
                
                // Find out how many [MLField] arguments it expects to have, and read them from the BinaryReader:
                var opArgs = new List<object>();
                foreach (MLArgInfo argInfo in op.GetArgumentDefs())
                {
                    byte[] argPackIndex = reader.ReadBytes(argIndexSize);
                    var argIndex = (int) DecodeNumberFromBytes(argPackIndex);
                    object val;
                    if (argInfo.NeedReindex)
                    {                                              
                        val = arguments[argIndex];
                        if (val is string &&
                            ((string)val).StartsWith("@") &&
                            (((string)val).Length > 1) &&
                            !((string)val).StartsWith("@LR") // Do not re-assign calls to the LoadRunner global label that resides outside this KSM file.
                           )
                        {
                            val = "@" + prefix + "_" + ((string)val).Substring(1);
                        }
                    }
                    else
                    {
                        val = arguments[argIndex];
                    }
                    opArgs.Add(val);
                }
                
                // Fill the MLFields, then add the opcode to the list:
                if (opArgs.Count > 0)
                    op.PopulateFromMLFields(opArgs);
                
                // Special case exception: if the opcode is just a dummy label, then don't insert it, and instead remember
                // it's value so it can be used as a label for the next opcode to come:
                if (prevWasLabelReset)
                {
                    op.Label = previousLabel;
                    prevWasLabelReset = false;
                }
                else
                    op.Label = NextConsecutiveLabel(previousLabel);

                var reset = op as OpcodeLabelReset;
                if (reset != null)
                {
                    previousLabel = reset.UpcomingLabel;
                    prevWasLabelReset = true;
                }
                else
                {
                    returnValue.Add(op);
                    previousLabel = op.Label;
                }
            }
            return returnValue;
        }

    }
    /// <summary>
    ///  Stores a range of ints [start,end], inclusive of both
    /// </summary>
    public class IntRange
    {
        public int Start {get; private set;}
        public int Stop  {get;set;}
        public IntRange(int start, int stop)
        {
            Start = start;
            Stop = stop;
        }
    }
    
    public class IntRangeCompare : Comparer<IntRange>
    {
        public override int Compare(IntRange a, IntRange b)
        {
            // If the range starts differ, use that to compare.
            // If they tie, then break the tie with the range stops.
            int returnValue = a.Start - b.Start;
            if (returnValue == 0)
                returnValue = a.Stop - b.Stop;
            return returnValue;
        }
    }

    /// <summary>
    /// Stores the mapping from line number to ranges of 
    /// opcode locations that contain the code from that line number
    /// </summary>
    public class DebugLineMap
    {

        // When this gets packed out, how many bytes will be needed to
        // hold the byte range indeces?        
        private int numDebugIndexBytes;

        private readonly Dictionary<int,List<IntRange>> store = new Dictionary<int,List<IntRange>>();
        
        public DebugLineMap()
        {
            // does anything need doing here?
        }
        
        /// <summary>
        /// Build a DebugLineMap from the encoded stream, assuming the stream is currently
        /// positioned right after the letter 'D' in the "%D" header.
        /// </summary>
        /// <param name="reader">The stream to read it from.  Must be rewindable.</param>
        public DebugLineMap(BinaryReader reader)
        {
            numDebugIndexBytes = reader.ReadByte();

            // The debug line section must be placed just before EOF.  It expects
            // to be terminated by running out of stream and not by anything else:
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                short lineNum = reader.ReadInt16();
                byte countRanges = reader.ReadByte();
                
                var ranges = new List<IntRange>();
                for (int index = 0 ; index < countRanges ; ++index)
                {
                    byte[] encodedStart = reader.ReadBytes(numDebugIndexBytes);
                    var start = (int)(CompiledObject.DecodeNumberFromBytes(encodedStart));

                    byte[] encodedStop = reader.ReadBytes(numDebugIndexBytes);
                    var stop = (int)(CompiledObject.DecodeNumberFromBytes(encodedStop));

                    ranges.Add(new IntRange(start, stop));
                }
                store.Add(lineNum, ranges);
            }
        }
        
        public void Add( int lineNum, IntRange addRange )
        {
            List<IntRange> ranges;

            int neededBytes = CompiledObject.FewestBytesToHold(Math.Max(addRange.Start, addRange.Stop) );
            if (neededBytes > numDebugIndexBytes)
                numDebugIndexBytes = neededBytes;
            
            // If it doesn't already exist, make it exist:
            if (! store.TryGetValue(lineNum, out ranges))
            {
                ranges = new List<IntRange>();
                store.Add(lineNum,ranges);
            }
            
            // Check if the addRange is contiguously just after an existing range:
            bool needNewEntry = true;
            foreach (IntRange range in ranges)
            {
                // If it's just after an existing range, then widen the existing range to include it:
                if (range.Stop == addRange.Start - 1)
                {
                    range.Stop = addRange.Stop;
                    needNewEntry = false;
                    break;
                }
            }
            // If it's not just after an existing range, then make a new range for it:
            if (needNewEntry)
            {
                ranges.Add(new IntRange(addRange.Start, addRange.Stop));
            }
        }
        
        /// <summary>
        /// pack together the debug line info into a section to tack on the
        /// end of the ML file.
        /// </summary>
        public IEnumerable<byte> Pack()
        {
            var writer = new BinaryWriter(new MemoryStream());
            
            // section header: identify it as the debug map,
            // and specify how many bytes the indeces will be packed into:
            writer.Write('%');
            writer.Write('D');
            writer.Write((byte)numDebugIndexBytes);
            
            foreach (int lineNum in store.Keys)
            {
                List<IntRange> ranges = store[lineNum];

                if (ranges.Count == 0)
                    continue;
                
                // write line num (2 bytes), followed by 1 byte for how many
                // ranges there are, followed by the ranges given as
                // ranges of start/stop indeces of size numDebugIndexBytes.
                writer.Write((short)lineNum);
                writer.Write((byte)ranges.Count); // It would be weird for there to be > 256 ranges.
                foreach (IntRange range in ranges)
                {
                    writer.Write( CompiledObject.EncodeNumberToNBytes((ulong)range.Start, numDebugIndexBytes));
                    writer.Write( CompiledObject.EncodeNumberToNBytes((ulong)range.Stop, numDebugIndexBytes));
                }
            }
            
            var bufLength = (int)(writer.BaseStream.Position);
            
            var returnValue = new byte[bufLength];
            Array.Copy( ((MemoryStream)(writer.BaseStream)).GetBuffer(),0,returnValue,0,bufLength);
            return returnValue;
        }
        
        /// <summary>
        /// Creates a useful mapping in the inverse direction for this DebugLineMap,
        /// that lets you search over a list ordered by the ML locations rather
        /// than by the line numbers.
        /// </summary>
        /// <returns>A SotedList mapping ranges of ML offsets to line numbers they came from, in order by IntRanges.</returns>
        public SortedList<IntRange,int> BuildInverseLookup()
        {
            var returnValue = new SortedList<IntRange,int>(new IntRangeCompare());
            
            foreach (int lineNum in store.Keys)
            {
                List<IntRange> ranges = store[lineNum];
                foreach (IntRange range in ranges)
                {
                    returnValue.Add(range,lineNum);
                }
            }
            return returnValue;
        }
    }
}
