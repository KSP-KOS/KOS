using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using kOS;
using kOS.Compilation;

namespace kOS.Compilation
{
    
    // Because nulls don't have real Types,
    // use this for a fake "type" to reperesent null:
    public class PseudoNull : IEquatable<object>
    {
        // all instances of PseudoNull should be considered identical:
        public override bool Equals(object o) { if (o is PseudoNull) return true; else return false; }
        public override int GetHashCode() { return 0; }
    }

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
    public class CompiledObject
    {
        /// <summary>A homemade magic number for file identifcation, that will
        /// appear as the first part of the file contents and is hopefully
        /// unique and unlike anything another file type uses:</summary>
        public static byte[] MagicId { get { return magicId; } private set{} }
        private static byte[] magicId = { (byte)'k', (byte)0x03, (byte)'X', (byte)'E' };
        private static Dictionary<Type,byte> IdFromType = null;
        private static Dictionary<byte,Type> TypeFromId = null;
        
        /// <summary>
        /// Populate the above two lookup dictionaries.
        /// </summary>
        public static void InitTypeData()
        {
            IdFromType = new Dictionary<Type,byte>();
            TypeFromId = new Dictionary<byte,Type>();
            
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
        }
        
        private static void AddTypeData(int byteType, Type CSType)
        {
            IdFromType.Add(CSType, (byte)byteType);
            TypeFromId.Add((byte)byteType, CSType);
        }
        
        /// <summary>
        /// Return the fewest number of bytes it would take
        /// to hold the given integer value, assuming its stored
        /// in an unsigned way.  Does not go higher than 8 bytes.
        /// </summary>
        /// <param name="maxVal">max value the bytes have to hold</param>
        /// <returns>number of bytes to hold it</returns>
        public static int FewestBytesToHold(long maxValue)
        {
            return  (maxValue <= 0x100) ? 1 :
                    (maxValue <= 0x10000) ? 2 :
                    (maxValue <= 0x1000000) ? 3 :
                    (maxValue <= 0x100000000) ? 4 :
                    (maxValue <= 0x10000000000) ? 5 :
                    (maxValue <= 0x1000000000000) ? 6 :
                    (maxValue <= 0x100000000000000) ? 7 : 8 ;
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
            byte[] returnValue = new byte[numBytes];
            for (int bNum = 0; bNum < numBytes ; ++bNum)
            {
                int bitsToShift = (numBytes-bNum) - 1;
                // This ends up being big-endian.  Dunno if that's the standard,
                // but as long as the reading back is consistent it's fine:
                returnValue[bNum] = (byte)( (number>>((bitsToShift)*8)) & (ulong)0xff);
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
                int bitsToShift = (numBytes-bNum) - 1;
                // Read it back assuming it's big-endian, because that's how EncodeNumberToNBytes does it:
                returnValue += ((ulong)encodedForm[bNum]) << bitsToShift;
            }
            return returnValue;
        }
        
        
        /// <summary>
        /// Holds all previously packed arguments to ML instructions.
        /// </summary>
        private static byte[] argumentPack = null;
        
        /// <summary>
        /// For efficiency, argumentPack will grow by 2x whenever it
        /// needs to expand.  This tracks how far into argumentPack is
        /// its logical length, which can be shorter than its physical
        /// length.
        /// </summary>
        private static int argumentPackLogicalLength = 0;

        /// <summary>
        /// Holds the mapping of line numbers from source to the ranges of the
        /// machine language section of the file where the opcodes derived
        /// from those line numbers are:
        /// </summary>
        private static DebugLineMap lineMap = null;

        /// <summary>
        /// A memory stream writer being used to temporarily pack and unpack values so
        /// we can borrow the standard algorithms for that.
        /// </summary>
        private static BinaryWriter packTempWriter = null;

        /// <summary>
        /// Holds a list of previously encountered machine language arguments,
        /// and the byte index at which they were inserted into the argumentPack array,
        /// and the length within the list they take up.
        /// </summary>
        private static Dictionary<object,int> argumentPackFinder = null;
        
        /// <summary>
        /// Returns the compiled program's opcodes packed into a tight form, that is a direct
        /// streamed dump of the list of opcodes.
        /// </summary>
        /// <param name="program">The list of opcode codeparts for the compiled program.</param>
        public static string Pack(string programName, List<CodePart> program)
        {
            packTempWriter = new BinaryWriter( new MemoryStream() );
            StringBuilder allCodeBuff = new StringBuilder();
            StringBuilder headBuff = new StringBuilder();
            argumentPack = new byte[8]; // this will grow bigger (be replaced by new arrays) as needed.
            argumentPackLogicalLength = 0; // nothing in the argumentPack yet.
            argumentPackFinder = new Dictionary<object,int>();
            lineMap = new DebugLineMap();

            for (int index = 0 ; index < program.Count ; ++index)  // --.    This can be replaced with a
            {                                                      //   |--- foreach.  I do it this way so I
                CodePart codePart = program[index];                // --'    can print the index in debugging.
                PackArgs(codePart.FunctionsCode);
                PackArgs(codePart.InitializationCode);
                PackArgs(codePart.MainCode);
            }

            // Now that we've seen every argument, we know how many bytes are needed
            // to store the argumentPack, and thus the larges possible index into it.
            // This will be how many bytes our indeces will be in this packed ML file.
            int numArgIndexBytes = FewestBytesToHold((long)argumentPackLogicalLength);
            headBuff.Append("%A"+ (char)(numArgIndexBytes) );

            byte[] truncatedArgumentPack = new Byte[argumentPackLogicalLength];
            Array.Copy(argumentPack, 0, truncatedArgumentPack, 0, argumentPackLogicalLength);
                
            headBuff.Append(System.Text.Encoding.ASCII.GetString(truncatedArgumentPack));

            for (int index = 0 ; index < program.Count ; ++index)  // --.    This can be replaced with a
            {                                                      //   |--- foreach.  I do it this way so I
                CodePart codePart = program[index];                // --'    can print the index in debugging.

                StringBuilder codeBuff = new StringBuilder();

                byte[] packedCode;
                int indexSoFar;

                indexSoFar = allCodeBuff.Length + codeBuff.Length;
                codeBuff.Append("%F");
                packedCode = PackCode(codePart.FunctionsCode, numArgIndexBytes, indexSoFar+2);
                codeBuff.Append(System.Text.Encoding.ASCII.GetString(packedCode));

                indexSoFar = allCodeBuff.Length + codeBuff.Length;
                codeBuff.Append("%I");
                packedCode = PackCode(codePart.InitializationCode, numArgIndexBytes, indexSoFar+2);
                codeBuff.Append(System.Text.Encoding.ASCII.GetString(packedCode));

                indexSoFar = allCodeBuff.Length + codeBuff.Length;
                codeBuff.Append("%M");
                packedCode = PackCode(codePart.MainCode, numArgIndexBytes, indexSoFar+2);
                codeBuff.Append(System.Text.Encoding.ASCII.GetString(packedCode));

                UnityEngine.Debug.Log(
                    "DEBUG: CompiledObject program dump below:\n"+
                    "####### CodePart " + index + " ###########\n" +
                    "### FUNC ###\n" +
                    GetCodeFragment( codePart.FunctionsCode ) +
                    "### INIT ###\n" +
                    GetCodeFragment( codePart.InitializationCode ) +
                    "### MAIN ###\n" +
                    GetCodeFragment( codePart.MainCode ) );
                                      
                allCodeBuff.Append(codeBuff);
            }
            return
                System.Text.Encoding.ASCII.GetString(MagicId) +
                headBuff.ToString() +
                allCodeBuff.ToString() +
                System.Text.Encoding.ASCII.GetString(lineMap.Pack());
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
                List<MLArgInfo> args = op.GetArgumentDefs();
                foreach (MLArgInfo arg in args)
                {
                    object argVal = arg.propertyInfo.GetValue(op,null);

                    // Just trying to add the argument to the pack.  Don't
                    // care where in the pack it is (yet).
                    UnityEngine.Debug.Log("BEFORE PackedArgumentLocation");
                    PackedArgumentLocation(argVal);
                    UnityEngine.Debug.Log("AFTER PackedArgumentLocation");
                }
            }
        }
        
        /// <summary>
        /// Pack the program fragment represented by this list of opcodes into ML code.
        /// </summary>
        /// <param name="fragment">the section being packed</param>
        /// <param name="argIndexSize">Number of bytes to use to encode argument indexes into the arg section.</param>
        /// <returns>the byte array of the packed together arguments</returns>
        private static byte[] PackCode(List<Opcode> fragment, int argIndexSize, int startByteIndex)
        {
            packTempWriter.Seek(0,SeekOrigin.Begin);
            for (int index = 0; index < fragment.Count ; ++index)
            {
                int opcodeStartByte = (int)( startByteIndex + packTempWriter.BaseStream.Position );
                Opcode op = fragment[index];
                byte code = (byte)op.Code;
                
                UnityEngine.Debug.Log( "eraseme Opcode " + index + " = " + (uint)code );

                //Always start with the opcode's bytecode:
                packTempWriter.Write(code);
                
                // Then append a number of argument indexes depending
                // on how many arguments the opcode is supposed to have:
                List<MLArgInfo> args = op.GetArgumentDefs();
                foreach (MLArgInfo arg in args)
                {
                    object argVal = arg.propertyInfo.GetValue(op,null);
                    int argPackedIndex = PackedArgumentLocation(argVal);
                    
                    byte[] argIndexEncoded = EncodeNumberToNBytes((ulong)argPackedIndex,argIndexSize);
                    packTempWriter.Write(argIndexEncoded);
                }
                
                // Now add this range to the Debug line mapping for this source line:
                int opcodeStopByte = (int)(startByteIndex + packTempWriter.BaseStream.Position - 1);
                lineMap.Add(op.SourceLine, new IntRange(opcodeStartByte,opcodeStopByte));
            }
            
            // Return the byte array that the memory writer has been outputting to in the above loop:
            MemoryStream mem = packTempWriter.BaseStream as MemoryStream;
            byte[] returnVal = new Byte[mem.Position];
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
            int labelOffset = 3; // Account for the %A at the front of the argument pack.
            
            object arg = (argument==null) ? new PseudoNull() : argument;
            
            UnityEngine.Debug.Log("AAA got here 1");
            int returnValue = -1; // bogus starting value before it's calculated.
            UnityEngine.Debug.Log("AAA got here 2");
            bool existsAlready = argumentPackFinder.TryGetValue(arg, out returnValue);
            UnityEngine.Debug.Log("AAA got here 3");
            if (existsAlready)
                return returnValue;
            UnityEngine.Debug.Log("AAA got here 4");
            
            // Okay, so have to encode it and add it.
            // --------------------------------------
            
            // When it gets added, it's going to be tacked on right at the end.
            // We already know that, se let's get that populated now:
            UnityEngine.Debug.Log("AAA got here 5");
            argumentPackFinder.Add(arg, labelOffset + argumentPackLogicalLength);
            UnityEngine.Debug.Log("AAA got here 6");
            returnValue = argumentPackLogicalLength;
            UnityEngine.Debug.Log("AAA got here 7");
            
            // Borrow C#'s Binary IO writer to pack the object into the byte form,
            // rather than writing our own for each type:
            UnityEngine.Debug.Log("AAA got here 8");
            packTempWriter.Seek(0,SeekOrigin.Begin);
            UnityEngine.Debug.Log("AAA got here 9: writing object of type " + arg.GetType().Name );
            
            WriteSomeBinaryPrimitive(packTempWriter, arg);
            UnityEngine.Debug.Log("AAA got here 10");
            MemoryStream mem = packTempWriter.BaseStream as MemoryStream;
            UnityEngine.Debug.Log("AAA got here 11: number of bytes written " + mem.Position);
            int argByteLength = (int)(mem.Position);
            UnityEngine.Debug.Log("AAA got here 12");
            byte[] packedArg = new byte[argByteLength+1]; // +1 because we insert the type byte at the front.
            UnityEngine.Debug.Log("AAA got here 13, for arg type = " + arg.GetType().Name);
            packedArg[0] = IdFromType[arg.GetType()];
            UnityEngine.Debug.Log("AAA got here 14: ");
            Array.Copy(mem.GetBuffer(), 0, packedArg, 1, argByteLength);
            UnityEngine.Debug.Log("AAA got here 15");
            for (int eraseme_i = 0 ; eraseme_i < packedArg.Length ; ++eraseme_i)
                UnityEngine.Debug.Log( "packedArg[" + eraseme_i + "] = " + packedArg[eraseme_i] );
                
            AddByteChunkToArgumentPack(packedArg);
            UnityEngine.Debug.Log("AAA got here 16");
            
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
            else
                throw new Exception( "Don't konw how to write this type of object to binary file: " + obj.GetType().Name );
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
            else
                throw new Exception( "Don't konw how to read this type of object from binary file: " + cSharpType.Name );
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
            UnityEngine.Debug.Log("Adding " + appendMe.Length + " bytes to buffer that is " + argumentPackLogicalLength + " of " + argumentPack.Length );
            int newLogicalLength = argumentPackLogicalLength + appendMe.Length;
            if (newLogicalLength > argumentPack.Length)
            {
                // Increase to double current size or if current size is too small or zero
                // so doubling it doesn't help, then incrase to hold new logical length:
                byte[] newBiggerPack = new byte[ Math.Max(argumentPack.Length*2, newLogicalLength) ];
                UnityEngine.Debug.Log("Made Bigger argumentPack.  New size is " + newBiggerPack.Length );
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
        /// <param name="filePath">name of file (with preceeding "volume/") that the program came from, for runtime error reporting.</param>
        /// <param name="startLineNum">line number the file should be assumed to start at (normally 1)</param>
        /// <param name="prefix">prepend this string to all labels in this program.</param>
        /// <param name="content">the file itself in ony big string.</param>
        /// <returns></returns>
        public static List<CodePart> UnPack(string filePath, int startLineNum, string prefix, string content)
        {
            List<CodePart> program = new List<CodePart>();
            
            byte[] packedContent = Encoding.UTF8.GetBytes(content);
            BinaryReader reader = new BinaryReader(new MemoryStream(packedContent));
            
            byte[] firstFour = reader.ReadBytes(4);
            
            if (! firstFour.SequenceEqual(MagicId))
                throw new Exception("Attempted to read an ML file that doesn't seem to be an ML file");
            
            int argIndexSize = 0;
            Dictionary<int,object> arguments = ReadArgumentPack(reader, out argIndexSize);
            lineMap = new DebugLineMap();
            
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
                        CodePart nextPart = new CodePart(filePath);
                        program.Add(nextPart);
                        // If this is the very first code we've ever encountered in the file, remember its position:
                        if (codeStart == 0)
                            codeStart = (int)(reader.BaseStream.Position - 2); // start is where the ByteCode.DELIMITER of the first section is.
                        
                        program[program.Count-1].FunctionsCode = ReadOpcodeList(reader, codeStart, prefix, arguments, argIndexSize);
                        UnityEngine.Debug.Log("Just built FunctionsCode with " + program[program.Count-1].FunctionsCode.Count + " opcodes.");
                        break;
                    case (byte)'I':
                        program[program.Count-1].InitializationCode = ReadOpcodeList(reader, codeStart, prefix, arguments, argIndexSize);
                        UnityEngine.Debug.Log("Just built InitializtionsCode with " + program[program.Count-1].InitializationCode.Count + " opcodes.");
                        break;
                    case (byte)'M':
                        program[program.Count-1].MainCode = ReadOpcodeList(reader, codeStart, prefix, arguments, argIndexSize);
                        UnityEngine.Debug.Log("Just built MainCode with " + program[program.Count-1].MainCode.Count + " opcodes.");
                        break;
                    case (byte)'D':
                        lineMap = new DebugLineMap(reader);
                        break;
                }
            }
            reader.Close();

            PostReadProcessing(program, filePath, prefix, lineMap);

            // This is debugging that will probably be removed later:
            foreach (CodePart codePart in program)
                UnityEngine.Debug.Log(
                    "DEBUG: CompiledObject program dump below:\n"+
                    "### FUNC ###\n" +
                    (codePart.FunctionsCode!=null ? GetCodeFragment( codePart.FunctionsCode ) : "NULL\n" )+
                    "### INIT ###\n" +
                    (codePart.InitializationCode!=null ? GetCodeFragment( codePart.InitializationCode ) : "NULL\n" ) +
                    "### MAIN ###\n" +
                    (codePart.MainCode!=null ? GetCodeFragment( codePart.MainCode ) : "NULL\n" ) );
                                                  
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
            Dictionary<int,object> returnArgs = new Dictionary<int,object>();

            int startPos = (int) reader.BaseStream.Position;
            
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
                if (TypeFromId.TryGetValue(argTypeId, out argCSharpType))
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
        /// <param name="filePath">name of file (with preceeding volume/) that the compiled code came from, for rutime error reporting purposes.</param>
        /// <param name="prefix">a string to prepend to the labels in the program.</param>
        /// <param name="lineMap">describes the mapping of line numbers to code locations.</param>
        public static void PostReadProcessing(List<CodePart>program, string filePath, string prefix, DebugLineMap lineMap)
        {
            SortedList<IntRange,int> lineLookup = lineMap.BuildInverseLookup();
            var lineEnumerator = lineLookup.GetEnumerator();
            
            int curLine = 0;
            IntRange curRange = new IntRange(-1,-1);

            int opIndex = 0;
            
            foreach (CodePart part in program)
            {
                // It's easier to iterate over the sections in the CodePart this way:
                List<List<Opcode>> sections = new List<List<Opcode>>();
                sections.Add(part.FunctionsCode);
                sections.Add(part.InitializationCode);
                sections.Add(part.MainCode);
                
                foreach (List<Opcode> codeList in sections)
                {
                    foreach (Opcode op in codeList)
                    {
                        ++opIndex; // First opIndex is called 1, not 0.  This matches the behavior of Compiler.cs's AddOpcode()'s call to GetNextIndex().
                        
                        // The algorithm is dependent on the fact that the opcodes will be iterated over
                        // in the same order as they appeared in the ML file, and therefore the
                        // DebugLineMap only ever has to be advanced forward, never backward.
                        UnityEngine.Debug.Log("for op: " + op + ", MLIndex="+op.MLIndex+", and working on range: ["+curRange.Start+","+curRange.Stop+"]");
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
                        
                        // As long as we're visiting every opcode, may as well set the Label and filename strings too:
                        if (op.Label == null || op.Label == String.Empty)
                        {
                            op.Label = "@" + prefix + "_" + string.Format("{0:0000}", opIndex);
                        }
                        else
                            op.Label = String.Empty; // ensure no nulls here.

                        if (op.SourceName == null || op.SourceName == String.Empty)
                            op.SourceName = filePath;
                        else
                            op.SourceName = String.Empty; // ensure no nulls here.

                    }
                }
            }
        }

        /// <summary>
        /// Creates the list of opcodes for one of the code sections of a codepart, assuming the
        /// BinaryReader is starting at the byte right after the identifying header '%F', '%I', or '%M'.
        /// </summary>
        /// <param name="reader">binary reader to read from.</param>
        /// <param name="codeStartPos">index into the stream where the first code block in the ML file started,
        /// for calculating indeces.</param>
        /// <param name="prefix">prefix to prepend to all labels within this program</param>.</param>
        /// <param name="arguments">argument dictionary to pull arguments from.</param>
        /// <param name="argIndexSize">number of bytes the argument indeces are packed into.</param>
        /// <returns>list of opcodes generated</returns>
        private static List<Opcode> ReadOpcodeList(BinaryReader reader, int codeStartPos, string prefix, Dictionary<int,object> arguments, int argIndexSize)
        {            
            List<Opcode> returnValue = new List<Opcode>();
            
            //
            // TODO: this method isn't working.  it's returning an opcode list of zero opcodes.  find out why.
            //

            bool sectionEnded = false;
            while (reader.BaseStream.Position < reader.BaseStream.Length && !(sectionEnded))
            {
                int opcodeMLPosition = (int)(reader.BaseStream.Position - codeStartPos); // For later use in PostReadProcessing().
                byte opCodeTypeId = reader.ReadByte();
                Type opCodeCSharpType = Opcode.TypeFromCode((ByteCode)opCodeTypeId);
                
                UnityEngine.Debug.Log("Just read an opcode with id="+opCodeTypeId+", Type="+opCodeCSharpType);
                if (opCodeCSharpType == typeof(PseudoNull))
                {
                    // As soon as there's an opcode encountered that isn't a known opcode type, the section is done:
                    sectionEnded = true;
                    // Un-read the byte that wasn't an opcode (it's probably the '%' delimiter for the next section):
                    reader.BaseStream.Seek(-1,SeekOrigin.Current);
                    continue;
                }

                // Make a new empty Opcode instance of this type:
                Opcode op = (Opcode)(Activator.CreateInstance(opCodeCSharpType,true));
                op.MLIndex = opcodeMLPosition; // For later use in PostReadProcessing().
                
                // Find out how many [MLField] arguments it expects to have, and read them from the BinaryReader:
                List<object> opArgs = new List<object>();
                foreach (MLArgInfo argInfo in op.GetArgumentDefs())
                {
                    byte[] argPackIndex = reader.ReadBytes(argIndexSize);
                    int argIndex = (int) DecodeNumberFromBytes(argPackIndex);
                    object val;
                    UnityEngine.Debug.Log("opcode NeedReindex = " + argInfo.NeedReindex);
                    if (argInfo.NeedReindex)
                    {
                        val = arguments[argIndex];
                        if ( val is string && ((string)val).StartsWith("@") && (((string)val).Length > 1) )
                        {
                            val = "@" + prefix + "_" + ((string)val).Substring(1);
                        }
                    }
                    else
                        val = arguments[argIndex];
                    UnityEngine.Debug.Log("opcode new value = " + val);
                    opArgs.Add(val);
                }
                
                // Fill the MLFields, then add the opcode to the list:
                if (opArgs.Count > 0)
                    op.PopulateFromMLFields(opArgs);
                returnValue.Add(op);
            }
            return returnValue;
        }

        /// <summary>
        /// This is copied almost verbatim from ProgramContext,
        /// and will probably get removed later.  It's here to help me debug.
        /// </summary>
        private static string GetCodeFragment(List<Opcode> codes)
        {
            var codeFragment = new List<string>();
            
            const string FORMAT_STR = "{0,-20} {1,4}:{2,-3} {3:0000} {4} {5} {6} {7}";
            codeFragment.Add(string.Format(FORMAT_STR, "File", "Line", "Col", "IP  ", "Label  ", "opcode", "operand", "Destination" ));
            codeFragment.Add(string.Format(FORMAT_STR, "----", "----", "---", "----", "-------", "---------------------", "", "" ));

            for (int index = 0; index < codes.Count; index++)
            {
                codeFragment.Add(string.Format(FORMAT_STR,
                                               codes[index].SourceName ?? "null",
                                               codes[index].SourceLine,
                                               codes[index].SourceColumn ,
                                               index,
                                               codes[index].Label ?? "null",
                                               codes[index] ?? new OpcodeBogus(),
                                               "DEST: " + (codes[index].DestinationLabel ?? "null" ),
                                               "" ) );
            }
            
            string returnVal = "";
            foreach (string s in codeFragment) returnVal += s + "\n";
            return returnVal;
        }
        
    }
    /// <summary>
    ///  Stores a range of ints [start,end], inclusive of both
    /// </summary>
    public class IntRange
    {
        public int Start {get;set;}
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
        private int numDebugIndexBytes = 0;

        private Dictionary<int,List<IntRange>> store = new Dictionary<int,List<IntRange>>();
        
        public DebugLineMap()
        {
            // TODO - does anything need doing here?
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
                
                UnityEngine.Debug.Log("DebugLineMap constructing: numDebugIndexBytes="+numDebugIndexBytes+", lineNum="+lineNum+", countRanges="+countRanges);
                
                List<IntRange> ranges = new List<IntRange>();
                for (int index = 0 ; index < countRanges ; ++index)
                {
                    byte[] encodedStart = reader.ReadBytes(numDebugIndexBytes);
                    int start = (int)(CompiledObject.DecodeNumberFromBytes(encodedStart));

                    byte[] encodedStop = reader.ReadBytes(numDebugIndexBytes);
                    int stop = (int)(CompiledObject.DecodeNumberFromBytes(encodedStop));

                    UnityEngine.Debug.Log("DebugLineMap adding range <"+start+", "+stop+">");
                    ranges.Add(new IntRange(start, stop));
                }
                store.Add(lineNum, ranges);
            }
        }
        
        public void Add( int lineNum, IntRange addRange )
        {
            List<IntRange> ranges = null;
            
            int neededBytes = CompiledObject.FewestBytesToHold( Math.Max(addRange.Start, addRange.Stop) );
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
        public byte[] Pack()
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream());
            
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
            
            int bufLength = (int)(writer.BaseStream.Position);
            
            byte[] returnValue = new byte[bufLength];
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
            SortedList<IntRange,int> returnValue = new SortedList<IntRange,int>(new IntRangeCompare());
            
            foreach (int lineNum in store.Keys)
            {
                UnityEngine.Debug.Log("BuildInverseLookup: adding for line number = "+lineNum);
                List<IntRange> ranges = store[lineNum];
                foreach (IntRange range in ranges)
                {
                    UnityEngine.Debug.Log("BuildInverseLookup: adding <"+range.Start+", "+range.Stop+">");
                    returnValue.Add(range,lineNum);
                }
            }
            return returnValue;
        }
    }
}
