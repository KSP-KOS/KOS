using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
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
        private static char[] magicID = { 'k', (char)0x03, 'X', 'E' };

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
        public static byte[] EncodeNumberToNBytes(long number, int numBytes)
        {
            // Encode the index into the right number of bytes:
            byte[] returnValue = new byte[numBytes];
            for (int bNum = 0; bNum < numBytes ; ++bNum)
            {
                // This ends up being big-endian.  Dunno if that's the standard,
                // but as long as the reading back is consistent it's fine:
                returnValue[bNum] = (byte)( ((number >> (bNum*8)) & 0xff) << (bNum*8) );
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

                indexSoFar = allCodeBuff.Length;
                codeBuff.Append("%F");
                packedCode = PackCode(codePart.FunctionsCode, numArgIndexBytes, indexSoFar);
                codeBuff.Append(System.Text.Encoding.ASCII.GetString(packedCode));

                indexSoFar = allCodeBuff.Length + codeBuff.Length;
                codeBuff.Append("%I");
                packedCode = PackCode(codePart.InitializationCode, numArgIndexBytes, indexSoFar);
                codeBuff.Append(System.Text.Encoding.ASCII.GetString(packedCode));

                indexSoFar = allCodeBuff.Length + codeBuff.Length;
                codeBuff.Append("%M");
                packedCode = PackCode(codePart.MainCode, numArgIndexBytes, indexSoFar);
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
            return new
                String(magicID) +
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
                List<PropertyInfo> args = op.GetArgumentDefs();
                foreach (PropertyInfo pInfo in args)
                {
                    object argVal = pInfo.GetValue(op,null);

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
                
                UnityEngine.Debug.Log( "ERASEME Opcode " + index + " = " + (uint)code );

                //Always start with the opcode's bytecode:
                packTempWriter.Write(code);
                
                // Then append a number of argument indexes depending
                // on how many arguments the opcode is supposed to have:
                List<PropertyInfo> args = op.GetArgumentDefs();
                foreach (PropertyInfo pInfo in args)
                {
                    object argVal = pInfo.GetValue(op,null);
                    int argPackedIndex = PackedArgumentLocation(argVal);
                    
                    byte[] argIndexEncoded = EncodeNumberToNBytes(argPackedIndex,argIndexSize);
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
        /// <param name="the thing to write"></param>
        private static void WriteSomeBinaryPrimitive(BinaryWriter writer, object obj)
        {
            if      (obj is PseudoNull) { /* do nothing.  for a null the type byte code is enough - no further data. */ }
            else if (obj is Boolean)    writer.Write((bool)obj);
            else if (obj is Int32)      writer.Write((Int32)obj);
            else if (obj is String)     writer.Write((String)obj);
            else if (obj is Double)     writer.Write((Double)obj);
            else if (obj is Single)     writer.Write((Single)obj);
            else if (obj is Byte)       writer.Write((byte)obj);
            else if (obj is Byte[])     writer.Write((byte[])obj);
            else if (obj is Char)       writer.Write((char)obj);
            else if (obj is Char[])     writer.Write((char[])obj);
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
        /// <param name="program">The list of opcodes for the compiled program.</param>
        public static List<Opcode> Unpack(string packedString)
        {
            return null; // TODO - populate this method with real code.
        }
        
        /// <summary>
        /// This is copied almost verbatim from ProgramContext,
        /// and will probably get removed later.  It's here to help me debug.
        /// </summary>
        private static string GetCodeFragment(List<Opcode> codes)
        {
            var codeFragment = new List<string>();
            
            const string FORMAT_STR = "{0,-20} {1,4}:{2,-3} {3:0000} {4} {5}";
            codeFragment.Add(string.Format(FORMAT_STR, "File", "Line", "Col", "IP  ", "opcode", "operand" ));
            codeFragment.Add(string.Format(FORMAT_STR, "----", "----", "---", "----", "---------------------", "" ));

            for (int index = 0; index < codes.Count; index++)
            {
                codeFragment.Add(string.Format(FORMAT_STR,
                                               codes[index].SourceName,
                                               codes[index].SourceLine,
                                               codes[index].SourceColumn,
                                               index,
                                               codes[index],
                                               "" ) );
            }
            
            string returnVal = "";
            foreach (string s in codeFragment) returnVal += s + "\n";
            return returnVal;
        }
        
    }
    /// <summary>
    ///  Stores a range of ints [start,end], incusive of both
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

    /// <summary>
    /// Stores the mapping from line number to ranges of 
    /// opcode locations that contain the code from that line number
    /// </summary>
    public class DebugLineMap
    {

        // When this gets packed out, how many bytes will be needed to
        // hold the byte range indeces?        
        private int numDebugIndexBytes = 0;

        private static Dictionary<int,List<IntRange>> store = new Dictionary<int,List<IntRange>>();
        
        public DebugLineMap()
        {
            // TODO - does anything need doing here?
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
                    writer.Write( CompiledObject.EncodeNumberToNBytes(range.Start, numDebugIndexBytes));
                    writer.Write( CompiledObject.EncodeNumberToNBytes(range.Stop, numDebugIndexBytes));
                }
            }
            
            int bufLength = (int)(writer.BaseStream.Position);
            
            byte[] returnValue = new byte[bufLength];
            Array.Copy( ((MemoryStream)(writer.BaseStream)).GetBuffer(),0,returnValue,0,bufLength);
            return returnValue;
        }
    }
}
