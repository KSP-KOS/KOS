using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Execution;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using kOS.Safe.Persistence;

namespace kOS.Safe.Compilation
{
    /// A very short numerical ID for the opcode. <br/>
    /// When adding a new opcode, remember to also add it to this enum list.<br/>
    /// <br/>
    /// BACKWARD COMPATIBILITY WARNING:<br/>
    /// If a value is ever inserted or
    /// deleted from this list, it will cause all existing precompiled kos
    /// programs to break.<br/>
    /// It will change the meaning of the bytecoes in
    /// the compiled files.  Try to only tack values onto the end of the list,
    /// if possible:
    /// 
    public enum ByteCode :byte
    {
        // It's good practice to always have a zero value in an enum, even if not used:
        BOGUS = 0,
        
        // This is the "%" character that will be used as a section delimiter in the
        // machine code file - when there's an "instruction" with a ByteCode of '%',
        // that means it's not an instruction.  It's the start of a different section
        // of the program:
        DELIMITER = 0x25,

        // The explicit picking of the hex numbers is not strictly necessary,
        // but it's being done to aid in debugging the ML load/unload process,
        // as it makes it possible to look at hexdumps of the machine code
        // and compare that to this list:
        EOF            = 0x31,
        EOP            = 0x32,
        NOP            = 0x33,
        STORE          = 0x34,
        UNSET          = 0x35,
        GETMEMBER      = 0x36,
        SETMEMBER      = 0x37,
        GETINDEX       = 0x38,
        SETINDEX       = 0x39,
        BRANCHFALSE    = 0x3a,
        JUMP           = 0x3b,
        ADD            = 0x3c,
        SUB            = 0x3d,
        MULT           = 0x3e,
        DIV            = 0x3f,
        POW            = 0x40,
        GT             = 0x41,
        LT             = 0x42,
        GTE            = 0x43,
        LTE            = 0x44,
        EQ             = 0x45,
        NE             = 0x46,
        NEGATE         = 0x47,
        BOOL           = 0x48,
        NOT            = 0x49,
        AND            = 0x4a,
        OR             = 0x4b,
        CALL           = 0x4c,
        RETURN         = 0x4d,
        PUSH           = 0x4e,
        POP            = 0x4f,
        DUP            = 0x50,
        SWAP           = 0x51,
        EVAL           = 0x52,
        ADDTRIGGER     = 0x53,
        REMOVETRIGGER  = 0x54,
        WAIT           = 0x55,
        // Removing ENDWAIT, 0x56 may be reused in a future version
        GETMETHOD      = 0x57,
        STORELOCAL     = 0x58,
        STOREGLOBAL    = 0x59,
        PUSHSCOPE      = 0x5a,
        POPSCOPE       = 0x5b,
        STOREEXIST     = 0x5c,
        PUSHDELEGATE   = 0x5d,
        BRANCHTRUE     = 0x5e,
        EXISTS         = 0x5f,
        ARGBOTTOM      = 0x60,
        TESTARGBOTTOM  = 0x61,
        TESTCANCELLED  = 0x62,
        

        // Augmented bogus placeholder versions of the normal
        // opcodes: These only exist in the program temporarily
        // or in the ML file but never actually can be executed.
        
        PUSHRELOCATELATER = 0xce,
        PUSHDELEGATERELOCATELATER = 0xcd,
        LABELRESET = 0xf0     // for storing the fact that the Opcode.Label's positional index jumps weirdly.
    }

    /// <summary>
    /// Attach this attribute to all members of the opcode that are meant to be encoded into the packed
    /// byte-wise machine language version of this opcode:  There should be enough information
    /// in all the Opcode's MLFields to reconstruct the entire program again.<br/>
    /// But the members who's value is calculated upon loading, like the DeltaInstructionPointer,
    /// the SourceName, and so on, do not need to be MLFields.
    /// <br/>
    /// One important consequence of the phrase "there should be enough information in all the
    /// Opcode's MLFields to reconstruct the entire program again" is that if you make an
    /// Opcode which requires arguments to the constructor, then all those arguments must be
    /// flagged as [MLField]'s, otherwise it will be impossible to obtain the information
    /// needed to reconstruct the Opcode when loading the ML file.
    /// <br/>
    /// WARNING! BE SURE TO EDIT CompiledObject.InitTypeData() if you add any new [MLField]'s that
    /// refer to argument types that haven't already been mentioned in CompiledObject.InitTypeData().
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class MLField : Attribute
    {
        public int Ordering { get; private set; }
        public bool NeedReindex { get; private set;}
        
        /// <summary>
        /// When constructing an MLField attribute, it's important
        /// to give it a sort order to ensure that it comes out in the
        /// ML file in a predictable consistent order.
        /// </summary>
        /// <param name="sortNumber">Sort the MLField by the order of these numbers</param>
        /// <param name="needReindex">True if this is a numeric value that needs its value adjusted by the base code offset.</param>
        public MLField(int sortNumber, bool needReindex)
        {
            Ordering = sortNumber;
            NeedReindex = needReindex;
        }
    }
    
    public class MLArgInfo
    {
        public PropertyInfo PropertyInfo {get; private set;}
        public bool NeedReindex {get; private set;}
        
        public MLArgInfo(PropertyInfo pInfo, bool needReindex)
        {
            PropertyInfo = pInfo;
            NeedReindex = needReindex;
        }
    }

    #region Base classes

    //     All classes derived from Opcode MUST be capable of producing at least a
    //     dummy instance without having to provide any constructor argument.
    //     Either define NO constructors, or if any constructors are defined. then there
    //     must also be a default constructor (no parameters) as well.
    //
    public abstract class Opcode
    {
        private static int lastId;
        private readonly int id = ++lastId;

        // SHOULD-BE-STATIC MEMBERS:
        // ========================
        //
        // There are places in this class where a static abstract member was the intent,
        // meaning "Enforce that all derived classes MUST override this", but also
        // meaning "To make a new value for this should require making a new derived class.  You
        // should not be able to make two instances of the same derived class differ in this value."
        // (i.e. all OpcodePush'es should have Name="push", and all OpcodeBranchJump's should have
        // Name="jump", and so on.)
        // 
        // But C# cannot support this, apparently, due to a limitation in how it implements class
        // inheritances.  It doesn't know how to store overrides at the static level where there's just
        // one instance per subclass definition.   It only knows how to override dynamic members.  Because of
        // this the compiler will call it an error to try to make a member be both abstract and static.
        //
        // Any place you see a member which is marked with a SHOULD-BE-STATIC comment, please do NOT
        // try to store separate values per instance into it.  Treat it like a static member, where to
        // change its value you should make a new derived class for the new value.

        protected abstract /*SHOULD-BE-STATIC*/ string Name { get; }
        
        /// <summary>
        /// The short coded value that indicates what kind of instruction this is.
        /// Hopefully one byte will be enough, and we won't have more than 256 different opcodes.
        /// </summary>
        public abstract /*SHOULD-BE-STATIC*/ ByteCode Code { get; }
        
        // A mapping of CodeName to Opcode type, built at initialization time:
        private static Dictionary<ByteCode,Type> mapCodeToType; // will init this later.

        // A mapping of Name to Opcode type,  built at initialization time:
        private static Dictionary<string,Type> mapNameToType; // will init this later.
        
        // A table describing the arguments in machine language form that each opcode needs.
        // This is populated by using Reflection to scan all the Opcodes for their MLField Attributes.
        private static Dictionary<Type,List<MLArgInfo>> mapOpcodeToArgs;
        
        private const string FORCE_DEFAULT_CONSTRUCTOR_MSG =
            "+----------- ERROR IN OPCODE DEFINITION ----------------------------------+\n" +
            "|                                                                         |\n" +
            "|         This is a message that only developers of the kOS mod are       |\n" +
            "|         likely to ever see.                                             |\n" +
            "|         If you are NOT a developer of kOS and this message happens      |\n" +
            "|         then that means some kOS developer should hang their head       |\n" +
            "|         in shame for giving out code without ever trying to run it once.|\n" +
            "|                                                                         |\n" +
            "|  THE PROBLEM:                                                           |\n" +
            "|                                                                         |\n" +
            "|  All Opcodes in kOS must be capable of being instanced from a default   |\n" +
            "|  constructor with no parameters.  The default constructor need not lead |\n" +
            "|  to a useful Opcode that works, and it can be protected if you like.    |\n" +
            "|                                                                         |\n" +
            "|  THE FOLLOWING DOES NOT HAVE A DEFAULT CONSTRUCTOR:                     |\n" +
            "|  {0,30}                                         |\n" +
            "|                                                                         |\n" +
            "+-------------------------------------------------------------------------+\n";

        public int Id { get { return id; } }
        /// <summary>
        /// How far to jump to the next opcode.  1 is typical (just go to the next opcode),
        /// but in the case of jump and branch statements, it can be other numbers.  This will
        /// be ignored if the CPU has been put into a yield state with CPU.YieldProgram().
        /// </summary>
        public int DeltaInstructionPointer { get; protected set; }
        public int MLIndex { get; set; } // index into the Machine Language code file for the COMPILE command.
        
        /// <summary>
        /// Used when profiling: Number of times this one instruction got executed in the life of a program run.
        /// </summary>
        public int ProfileExecutionCount { get; set; }
        /// <summary>
        /// Used when profiling: Total stopwatch ticks (not Unity update ticks) spent on this instruction during the
        /// program run.  If this instruction occurs in a loop and thus gets executed more than once, this will be the sum
        /// of all its executions that took place.  Divide by ProfileExecutionCount to get what the
        /// average for a single execution of it was.
        /// </summary>
        public long ProfileTicksElapsed { get; set; }

        public string Label {get{return label;} set {label = value;} }
        public virtual string DestinationLabel {get;set;}
        public GlobalPath SourcePath;

        public short SourceLine { get; set; } // line number in the source code that this was compiled from.
        public short SourceColumn { get; set; }  // column number of the token nearest the cause of this Opcode.

        private string label = string.Empty;

        public bool AbortProgram { get; set; }
        public bool AbortContext { get; set; }
        
        public virtual void Execute(ICpu cpu)
        {
        }

        public override string ToString()
        {
            return Name;
        }

        protected Opcode()
        {
            DeltaInstructionPointer = 1;
            AbortProgram = false;
            AbortContext = false;
        }
        
        /// <summary>
        /// Starting from an empty instance of this opcode that you can assume was created
        /// from the default constructor, populate the Opcode's properties from a
        /// list of all the [MLFields] saved to the machine language file.<br/>
        /// This needs to be overridden only if your Opcode has declared [MLField]'s.
        /// If your Opcode has no [MLFields] then the generic base version of this method works
        /// well enough.<br/>
        /// <br/>
        /// TODO: Perhaps add an assert to the Init methods that will throw up a NagMessage
        /// if it detects an Opcode has be defined which has [MLFields] but lacks an override
        /// of this method.
        /// <br/>
        /// </summary>
        /// <param name="fields">A list of all the [MLFields] to populate the opcode with,
        /// given *IN ORDER* of their Ordering fields.  This is important.  If the
        /// opcode has 2 properties, one that was given attribute [MLField(10)] and the
        /// other that was given attribute [MLField(20)], then the one with ordering=10
        /// will be the first one in this list, and the one with ordering=20 will be the
        /// second.  You can process the list in the guaranteed assumption that the caller
        /// ordered the arguments this way.<br/>
        /// NOTE: If the opcode has no MLField's attributes, then this may be passed in
        /// as null, rather than as a list of 0 items.<br/>
        /// </param>
        public virtual void PopulateFromMLFields(List<object> fields)
        {
            // No use of the fields argument in the generic base version
            // of this.  The compiler warning about this is ignorable.
        }
        
        /// <summary>
        /// This is intended to be called once, during the mod's initialization, and never again.
        /// It builds the Dictionaries that look up the type of opcode given its string name or code.
        /// </summary>
        public static void InitMachineCodeData()
        {
            // Because of how hard it is to guarantee that KSP only runs a method once and never again, and
            // how so many of the Unity initializer hooks seem to be called repeatedly, this check ensures this
            // only ever does its work once, no matter how many times it's called:
            if (mapCodeToType != null)
                return;
            mapCodeToType = new Dictionary<ByteCode,Type>();
            mapNameToType = new Dictionary<string,Type>();
            mapOpcodeToArgs = new Dictionary<Type,List<MLArgInfo>>();
            
            // List of all subclasses of Opcode:
            Type opcodeType = typeof(Opcode);
            IEnumerable<Type> opcodeTypes = ReflectUtil.GetLoadedTypes(opcodeType.Assembly).Where( t => t.IsSubclassOf(opcodeType) );
            foreach (Type opType in opcodeTypes)
            {
                if (!opType.IsAbstract) // only the ones that can be instanced matter.
                {
                    // (Because overridden values cannot be static, discovering the Opcode's overridden names and codes
                    // requires making a dummy instance.  See the comment about SHOULD-BE-STATIC up at the top of this
                    // class definition for a longer explanation of why.)

                    // New rule: all Opcode derivatives must have a default constructor, or this won't work:
                    object dummyInstance;
                    try
                    {
                        dummyInstance = Activator.CreateInstance(opType,true);
                    }
                    catch (MissingMethodException)
                    {
                        SafeHouse.Logger.Log( String.Format(FORCE_DEFAULT_CONSTRUCTOR_MSG, opType.Name) );
                        Debug.AddNagMessage( Debug.NagType.NAGFOREVER, "ERROR IN OPCODE DEFINITION " + opType.Name );
                        return;
                    }
                    
                    var argsInfo = new List<MLArgInfo>();

                    PropertyInfo[] props = opType.GetProperties(BindingFlags.Instance |
                                                                BindingFlags.FlattenHierarchy |
                                                                BindingFlags.Public |
                                                                BindingFlags.NonPublic);

                    foreach (PropertyInfo pInfo in props)
                    {
                        object[] attribs = pInfo.GetCustomAttributes(true);
                        if (pInfo.Name == "Code")
                        {
                            // Add to the map from codename to Opcode type:
                            var opCodeName = (ByteCode) pInfo.GetValue(dummyInstance, null);
                            mapCodeToType.Add(opCodeName, opType);
                        }
                        else if (pInfo.Name == "Name")
                        {
                            // Add to the map from Name to Opcode type:
                            var opName = (string) pInfo.GetValue(dummyInstance, null);
                            mapNameToType.Add(opName, opType);
                        }
                        else
                        {
                            // See if this property has an MLFields attribute somewhere on it.
                            foreach (object attrib in attribs)
                            {
                                var field = attrib as MLField;
                                if (field == null) continue;

                                argsInfo.Add(new MLArgInfo(pInfo, field.NeedReindex));
                                break;
                            }
                        }
                    }
                    argsInfo.Sort(MLFieldComparator);
                    mapOpcodeToArgs.Add(opType, argsInfo);
                }
            }
        }
        
        /// <summary>
        /// Delegate function used for Sort() of the MLFields on an Opcode.
        /// Should only be called on properties that have [MLField] attributes
        /// on them.
        /// </summary>
        /// <param name="a1"></param>
        /// <param name="a2"></param>
        /// <returns>negative if a1 less than a2, 0 if same, positive if a1 greater than a2</returns>
        private static int MLFieldComparator(MLArgInfo a1, MLArgInfo a2)
        {
            // All the following is doing is just comparing p1 and p2's
            // MLField.Ordering fields to decide the sort order.
            //
            // Reflection: A good way to make a simple idea look messier than it really is.
            //
            var attributes1 = a1.PropertyInfo.GetCustomAttributes(true).Cast<Attribute>().ToList();
            var attributes2 = a2.PropertyInfo.GetCustomAttributes(true).Cast<Attribute>().ToList();
            var f1 = (MLField) attributes1.First(a => a is MLField);
            var f2 = (MLField) attributes2.First(a => a is MLField);
            return (f1.Ordering < f2.Ordering) ? -1 : (f1.Ordering > f2.Ordering) ? 1 : 0;
        }
        
        /// <summary>
        /// Given a string value of Code, find the Opcode Type that uses that as its CodeName.
        /// </summary>
        /// <param name="code">ByteCode to look up</param>
        /// <returns>Type, one of the subclasses of Opcode, or PseudoNull if there was no match</returns>
        public static Type TypeFromCode(ByteCode code)
        {
            Type returnValue;
            if (! mapCodeToType.TryGetValue(code, out returnValue))
            {
                returnValue = typeof(PseudoNull); // flag telling the caller "not found".
            }
            return returnValue;
        }

        /// <summary>
        /// Given a string value of Name, find the Opcode Type that uses that as its Name.
        /// </summary>
        /// <param name="name">name to look up</param>
        /// <returns>Type, one of the subclasses of Opcode, or PseudoNull if there was no match</returns>
        public static Type TypeFromName(string name)
        {
            Type returnValue;
            if (! mapNameToType.TryGetValue(name, out returnValue))
            {
                returnValue = typeof(PseudoNull); // flag telling the caller "not found".
            }
            return returnValue;
        }
        
        /// <summary>
        /// Return the list of member Properties that are part of what gets stored to machine language
        /// for this opcode.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<MLArgInfo> GetArgumentDefs()
        {
            return mapOpcodeToArgs[GetType()];
        }
        
        /// <summary>
        /// A utility function that will do a cpu.PopValue, but with an additional check to ensure
        /// the value atop the stack isn't the arg bottom marker.
        /// </summary>
        /// <returns>object popped if it all worked fine</returns>
        protected object PopValueAssert(ICpu cpu, bool barewordOkay = false)
        {
            object returnValue = cpu.PopValueArgument(barewordOkay);
            if (returnValue != null && returnValue.GetType() == OpcodeCall.ArgMarkerType)
                throw new KOSArgumentMismatchException("Called with not enough arguments.");
            return returnValue;
        }

        /// <summary>
        /// A utility function that will do the same as a cpu.PopValueEncapsulated, but with an additional check to ensure
        /// the value atop the stack isn't the arg bottom marker.
        /// </summary>
        /// <returns>object popped if it all worked fine</returns>
        protected object PopValueAssertEncapsulated(ICpu cpu, bool barewordOkay = false)
        {
            return Structure.FromPrimitive(PopValueAssert(cpu, barewordOkay));
        }

        /// <summary>
        /// A utility function that will do the same as a cpu.PopStructureEncapsulated, but with an additional check to ensure
        /// the value atop the stack isn't the arg bottom marker.
        /// </summary>
        /// <returns>object popped if it all worked fine</returns>
        protected Structure PopStructureAssertEncapsulated(ICpu cpu, bool barewordOkay = false)
        {
            return Structure.FromPrimitiveWithAssert(PopValueAssert(cpu, barewordOkay));
        }

    }

    public abstract class BinaryOpcode : Opcode
    {
        protected OperandPair Operands { get; private set; }

        public override void Execute(ICpu cpu)
        {            
            object right = cpu.PopValueArgument();
            object left = cpu.PopValueArgument();

            var operands = new OperandPair(left, right);

            Calculator calc = Calculator.GetCalculator(operands);
            Operands = operands;
            object result = ExecuteCalculation(calc);
            cpu.PushArgumentStack(result);
        }

        protected virtual object ExecuteCalculation(Calculator calc)
        {
            return null;
        }
    }

    /// <summary>
    /// The base class for opcodes that operate on an identifier as an MLfield
    /// (rather than reading the identifier from a stack argument).
    /// </summary>
    public abstract class OpcodeIdentifierBase : Opcode
    {
        [MLField(1, false)]
        public string Identifier { get; set; }

        protected OpcodeIdentifierBase(string identifier)
        {
            Identifier = identifier;
        }

        public override void PopulateFromMLFields(List<object> fields)
        {
            // Expect fields in the same order as the [MLField] properties of this class:
            if (fields == null || fields.Count<1)
                throw new Exception(String.Format("Saved field in ML file for {0} seems to be missing.  Version mismatch?", Name));
            Identifier = Convert.ToString(fields[0]);
        }

        public override string ToString()
        {
            return String.Format("{0} {1}", Name, (string)Identifier);
        }
    }

    #endregion

    #region General


    /// <summary>
    /// Consumes the topmost value of the stack, storing it into
    /// a variable named by the Identifier MLField of this opcode.<br/>
    /// <br/>
    /// If the variable does not exist in the local scope, then it will attempt to look for
    /// it in the next scoping level up, and the next one up, and so on
    /// until it hits global scope and if it still hasn't found it,
    /// it will CREATE the variable anew there, at global scope, and
    /// then store the value there.<br/>
    /// <br/>
    /// This is the usual way to make a new GLOBAL variable, or to overwrite
    /// an existing LOCAL variable.  Note that since this
    /// is the way to make a variable, it's impossible to make a variable
    /// that hasn't been given an initial value.  Its the act of storing a value into
    /// the variable that causues it to exist.  This is deliberate design.
    /// </summary>
    public class OpcodeStore : OpcodeIdentifierBase
    {
        protected override string Name { get { return "store"; } }
        public override ByteCode Code { get { return ByteCode.STORE; } }

        public OpcodeStore(string identifier) : base(identifier)
        {
        }

        protected OpcodeStore() : base("")
        {
        }

        public override void Execute(ICpu cpu)
        {
            Structure value = PopStructureAssertEncapsulated(cpu);
            cpu.SetValue(Identifier, value);
        }
    }

    /// <summary>
    /// Tests if the identifier atop the stack is an identifier that exists in the system
    /// and is accessible in scope at the moment.  If the identifier doesn't
    /// exist, or if it does but it's out of scope right now, then it results in
    /// a FALSE, else it results in a TRUE.  The result is pushed onto the stack
    /// for reading.
    /// Note that the ident atop the stack must be formatted like a variable
    /// name (i.e. have the leading '$').
    /// </summary>
    public class OpcodeExists : Opcode
    {
        protected override string Name { get { return "exists"; } }
        public override ByteCode Code { get { return ByteCode.EXISTS; } }
        
        public override void Execute(ICpu cpu)
        {
            bool result = false; //pessimistic default
            // Convert to string instead of cast in case the identifier is stored
            // as an encapsulated StringValue, preventing an unboxing collision.
            string ident = Convert.ToString(cpu.PopArgumentStack());
            if (ident != null && cpu.IdentifierExistsInScope(ident))
            {
                result = true;
            }
            cpu.PushArgumentStack(result);
        }
    }

    /// <summary>
    /// Consumes the topmost value of the stack, storing it into
    /// a variable described by Identifer MLField of this opcode,
    /// which must already exist as a variable before this is executed.<br/>
    /// <br/>
    /// Unlike OpcodeStore, OpcodeStoreExist will NOT create the variable if it
    /// does not already exist.  Instead it will cause an
    /// error.  (It corresponds to kerboscript's @LAZYGLOBAL OFF directive).<br/>
    /// <br/>
    /// </summary>
    public class OpcodeStoreExist : OpcodeIdentifierBase
    {
        protected override string Name { get { return "storeexist"; } }
        public override ByteCode Code { get { return ByteCode.STOREEXIST; } }

        public OpcodeStoreExist(string identifier) : base(identifier)
        {
        }

        protected OpcodeStoreExist() : base("")
        {
        }

        public override void Execute(ICpu cpu)
        {
            Structure value = PopStructureAssertEncapsulated(cpu);
            cpu.SetValueExists(Identifier, value);
        }
    }
    
    /// <summary>
    /// Consumes the topmost value of the stack, storing it into
    /// a variable named in the Identiver MLField of this Opcode.<br/>
    /// <br/>
    /// The variable must not exist already in the local nesting level, and it will
    /// NOT attempt to look for it in the next scoping level up.<br/>
    /// <br/>
    /// Instead it will attempt to create the variable anew at the current local
    /// nesting scope.<br/>
    /// <br/>
    /// This is the usual way to make a new LOCAL variable.  Do not attempt to
    /// use this opcode to store the value into an existing variable - just use it
    /// when making new variables.  If you use it to store into an existing
    /// local variable, it will generate an error at runtime.<br/>
    /// <br/>
    /// Note that since this
    /// is the way to make a variable, it's impossible to make a variable
    /// that hasn't been given an initial value.  Its the act of storing a value into
    /// the variable that causues it to exist.  This is deliberate design.
    /// </summary>
    public class OpcodeStoreLocal : OpcodeIdentifierBase
    {
        protected override string Name { get { return "storelocal"; } }
        public override ByteCode Code { get { return ByteCode.STORELOCAL; } }

        public OpcodeStoreLocal(string identifier) : base(identifier)
        {
        }

        protected OpcodeStoreLocal() : base("")
        {
        }

        public override void Execute(ICpu cpu)
        {
            Structure value = PopStructureAssertEncapsulated(cpu);
            cpu.SetNewLocal(Identifier, value);
        }
    }

    /// <summary>
    /// Consumes the topmost value of the stack, storing it into
    /// a variable named by the Identifier MLfield of this Opcode.<br/>
    /// <br/>
    /// The variable will always be stored at a global scope, overwriting
    /// whatever else was there if the variable already existed.<br/>
    /// <br/>
    /// It will ignore local scoping and never store the value in a local
    /// variable<br/>
    /// <br/>
    /// It's impossible to make a variable that hasn't been given an initial value.
    /// Its the act of storing a value into the variable that causues it to exist.
    /// This is deliberate design.
    /// </summary>
    public class OpcodeStoreGlobal : OpcodeIdentifierBase
    {
        protected override string Name { get { return "storeglobal"; } }
        public override ByteCode Code { get { return ByteCode.STOREGLOBAL; } }

        public OpcodeStoreGlobal(string identifier) : base(identifier)
        {
        }

        protected OpcodeStoreGlobal() : base("")
        {
        }

        public override void Execute(ICpu cpu)
        {
            Structure value = PopStructureAssertEncapsulated(cpu);
            cpu.SetGlobal(Identifier, value);
        }
    }

    /// <summary>
    /// Consumes the topmost value of the stack as an identifier, unsetting
    /// the variable referenced by this identifier. This will remove the
    /// variable referenced by this identifier in the innermost scope that
    /// it is set in.
    /// </summary>
    public class OpcodeUnset : Opcode
    {
        protected override string Name { get { return "unset"; } }
        public override ByteCode Code { get { return ByteCode.UNSET; } }

        public override void Execute(ICpu cpu)
        {
            object identifier = cpu.PopArgumentStack();
            if (identifier != null)
            {
                cpu.RemoveVariable(identifier.ToString());
            }
            else
            {
                throw new KOSObsoletionException("0.17","UNSET ALL", "<not supported anymore now that we have nested scoping>", "");
            }
        }
    }

    /// <summary>
    /// <para>
    /// Consumes the topmost value of the stack, getting the suffix of it
    /// specified by the Identifier MLField and putting that value back on
    /// the stack. If this suffix refers to a method suffix, it will be
    /// called with no arguments.
    /// </para>
    /// <para></para>
    /// <para>getmember identifier</para>
    /// <para>... obj -- ... result</para>
    /// <para></para>
    /// <para>
    /// If this is instead a GetMethod call, it will leave the
    /// DelegateSuffixResult on the stack to be called by a later instruction.
    /// </para>
    /// </summary>
    public class OpcodeGetMember : OpcodeIdentifierBase
    {
        protected override string Name { get { return "getmember"; } }
        public override ByteCode Code { get { return ByteCode.GETMEMBER; } }
        protected bool IsMethodCallAttempt = false;

        public OpcodeGetMember(string identifier) : base(identifier)
        {
        }

        protected OpcodeGetMember() : base("")
        {
        }

        public override void Execute(ICpu cpu)
        {
            object popValue = cpu.PopValueEncapsulatedArgument();

            var specialValue = popValue as ISuffixed;
            
            if (specialValue == null)
            {
                throw new Exception(string.Format("Values of type {0} cannot have suffixes", popValue.GetType()));
            }

            ISuffixResult result = specialValue.GetSuffix(Identifier);

            // If the result is a suffix that is still in need of being invoked and hasn't resolved to a value yet:
            if (result != null && !IsMethodCallAttempt && !result.HasValue)
            {
                // This is what happens when someone tries to call a suffix method as if
                // it wasn't a method (i.e. leaving the parentheses off the call).  The
                // member returned is a delegate that needs to be called to get its actual
                // value.  Borrowing the same routine that OpcodeCall uses for its method calls:

                cpu.PushArgumentStack(result);
                cpu.PushArgumentStack(new KOSArgMarkerType());
                OpcodeCall.StaticExecute(cpu, false, "", false); // this will push the return value on the stack for us.
            }
            else
            {
                if (result.HasValue)
                {
                    // Push the already calculated value.

                    cpu.PushArgumentStack(result.Value);
                }
                else
                {
                    // Push the indirect suffix delegate, but don't execute it yet
                    // because we need to put the upcoming arg list above it on the stack.
                    // Eventually an <indirect> OpcodeCall will occur further down the program which
                    // will actually execute this.
                    
                    cpu.PushArgumentStack(result);
                }
            }
        }
    }
    
    /// <summary>
    /// <para>
    /// OpcodeGetMethod is *exactly* the same thing as OpcodeGetMember, and is in fact a subclass of it.
    /// The only reason for the distinction is so that at runtime the Opcode can tell whether the
    /// getting of the member was done with method call syntax with parentheses, like SHIP:NAME(), or
    /// non-method call syntax, like SHIP:NAME. It needs to know whether there is an upcoming
    /// OpcodeCall coming next or not, so it knows whether the delegate will get dealt with later
    /// or if it needs to perform it now.
    /// </para>
    /// <para></para>
    /// <para>getmethod identifier</para>
    /// <para>... obj -- ... DelegateSuffixResult</para>
    /// </summary>
    public class OpcodeGetMethod : OpcodeGetMember
    {
        protected override string Name { get { return "getmethod"; } }
        public override ByteCode Code { get { return ByteCode.GETMETHOD; } }

        public OpcodeGetMethod(string identifier) : base(identifier)
        {
        }

        protected OpcodeGetMethod() : base("")
        {
        }

        public override void Execute(ICpu cpu)
        {
            IsMethodCallAttempt = true;
            base.Execute(cpu);
        }
    }

    /// <summary>
    /// <para>
    /// Consumes a value and a destination object from the stack,
    /// setting the objects suffix specified by the Identifier MLField
    /// to the popped value.
    /// </para>
    /// <para></para>
    /// <para>setmember identifier</para>
    /// <para>... obj value -- ...</para>
    /// </summary>
    public class OpcodeSetMember : OpcodeIdentifierBase
    {
        protected override string Name { get { return "setmember"; } }
        public override ByteCode Code { get { return ByteCode.SETMEMBER; } }

        public OpcodeSetMember(string identifier) : base(identifier)
        {
        }

        protected OpcodeSetMember() : base("")
        {
        }

        public override void Execute(ICpu cpu)
        {
            Structure value = cpu.PopStructureEncapsulatedArgument();         // new value to set it to
            Structure popValue = cpu.PopStructureEncapsulatedArgument();      // object to which the suffix is attached.

            // We aren't converting the popValue to a Scalar, Boolean, or String structure here because
            // the referenced variable wouldn't be updated.  The primitives themselves are treated as value
            // types instead of reference types.  This is also why I removed the string unboxing
            // from the ISuffixed check below.

            var specialValue = popValue as ISuffixed;
            if (specialValue == null)
            {
                throw new Exception(string.Format("Values of type {0} cannot have suffixes", popValue.GetType()));
            }

            // TODO: When we refactor to make every structure use the new suffix style, this conversion
            // to primative can be removed.  Right now there are too many structures that override the
            // SetSuffix method while relying on unboxing the object rahter than using Convert
            if (!specialValue.SetSuffix(Identifier, Structure.ToPrimitive(value)))
            {
                throw new Exception(string.Format("Suffix {0} not found on object", Identifier));
            }
        }
    }

    /// <summary>
    /// <para>
    /// Consumes an index and an target object from the stack,
    /// getting the indexed value from the object and pushing
    /// the result back on the stack.
    /// </para>
    /// <para></para>
    /// <para>getindex</para>
    /// <para>... obj index -- ... result</para>
    /// </summary>
    public class OpcodeGetIndex : Opcode
    {
        protected override string Name { get { return "getindex"; } }
        public override ByteCode Code { get { return ByteCode.GETINDEX; } }

        public override void Execute(ICpu cpu)
        {
            Structure index = cpu.PopStructureEncapsulatedArgument();
            Structure collection = cpu.PopStructureEncapsulatedArgument();

            Structure result;

            var indexable = collection as IIndexable;
            if (indexable != null)
            {
                result = indexable.GetIndex(index);
            }
            else
            {
                throw new Exception(string.Format("Can't iterate on an object of type {0}", collection.GetType()));
            }

            cpu.PushArgumentStack(result);
        }
    }

    /// <summary>
    /// <para>
    /// Consumes a value, an index, and an object from the stack,
    /// setting the specified index on the object to the given value.
    /// </para>
    /// <para></para>
    /// <para>setindex</para>
    /// <para>... obj index value -- ...</para>
    /// </summary>
    public class OpcodeSetIndex : Opcode
    {
        protected override string Name { get { return "setindex"; } }
        public override ByteCode Code { get { return ByteCode.SETINDEX; } }

        public override void Execute(ICpu cpu)
        {
            Structure value = cpu.PopStructureEncapsulatedArgument();
            Structure index = cpu.PopStructureEncapsulatedArgument();
            Structure list = cpu.PopStructureEncapsulatedArgument();

            if (index == null || value == null)
            {
                throw new KOSException("Neither the key nor the index of a collection may be null");
            }

            var indexable = list as IIndexable;
            if (indexable == null)
            {
                throw new KOSException(string.Format("Can't set indexed elements on an object of type {0}", list.GetType()));
            }
            indexable.SetIndex(index, value);
        }
    }

    /// <summary>
    /// Stops executing for this cycle. Has no stack effect.
    /// </summary>
    public class OpcodeEOF : Opcode
    {
        protected override string Name { get { return "EOF"; } }
        public override ByteCode Code { get { return ByteCode.EOF; } }
        public override void Execute(ICpu cpu)
        {
            AbortContext = true;
        }
    }

    /// <summary>
    /// Aborts the current program. This is used to return back to the interpreter context
    /// once a program is finished executing. Has no stack effect. (The
    /// system may wipe some things off the stack as it performs cleanup associated
    /// with ending the program, but this opcode doesn't do it directly itself.)
    /// </summary>
    public class OpcodeEOP : Opcode
    {
        protected override string Name { get { return "EOP"; } }
        public override ByteCode Code { get { return ByteCode.EOP; } }
        public override void Execute(ICpu cpu)
        {
            AbortProgram = true;
        }
    }

    /// <summary>
    /// No-op. Does nothing.
    /// </summary>
    public class OpcodeNOP : Opcode
    {
        protected override string Name { get { return "nop"; } }
        public override ByteCode Code { get { return ByteCode.NOP; } }
    }

    
    /// <summary>
    /// Opcode to be returned when getting an opcode that doesn't exist (outside program range).
    /// This generally happens only when there's an exception that occurs outside running
    /// a program, and the KSPLogger has to have something valid returned or it throws
    /// an exception that hides the original exception it was trying to report.
    /// </summary>
    public class OpcodeBogus : Opcode
    {
        protected override string Name { get { return "not an opcode in the program."; } }
        public override ByteCode Code { get { return ByteCode.BOGUS; } }
    }
    
    #endregion

    #region Branch

    
    public abstract class BranchOpcode : Opcode
    {
        // This stores EITHER the label OR the relative distance,
        // depending, in the KSM packed file.  Only if the label is
        // an empty string does it store the integer distance instead.
        [MLField(1,true)]
        public object KSMLabelOrDistance
        {
            get
            {
                if (DestinationLabel == string.Empty)
                    return Distance;
                else
                    return DestinationLabel;
            }
        }
        
        public int Distance { get; set; }
        
        public override void PopulateFromMLFields(List<object> fields)
        {
            // Expect fields in the same order as the [MLField] properties of this class:
            if (fields == null || fields.Count<1)
                throw new Exception("Saved field in ML file for BranchOpcode seems to be missing.  Version mismatch?");
            // This class does something strange - it expects the KSM file to encode EITHER a string label OR an integer,
            // but never both.  Therefore it has to determine the type of the arg to decide which it was:
            if (fields[0] is string)
                DestinationLabel = (string)fields[0];
            else
                Distance = (int)fields[0];
        }

        public override string ToString()
        {
            // Format string forces printing of '+/-' sign always, even for positive numbers.
            // The intent is to make it more clear that this is a relative, not absolute jump:
            return string.Format("{0} {1:+#;-#;+0}", Name, Distance);
        }
    }

    /// <summary>
    /// <para>
    /// Consumes one value from the stack and branches to the given destination if the value was false
    /// </para>
    /// <para></para>
    /// <para>br.false destination</para>
    /// <para>... flag -- ...</para>
    /// </summary>
    public class OpcodeBranchIfFalse : BranchOpcode
    {
        protected override string Name { get { return "br.false"; } }
        public override ByteCode Code { get { return ByteCode.BRANCHFALSE; } } // branch if zero - a longstanding name for this op in many machine codes.

        public override void Execute(ICpu cpu)
        {
            bool condition = Convert.ToBoolean(cpu.PopValueArgument());

            DeltaInstructionPointer = !condition ? Distance : 1;
        }
    }

    /// <summary>
    /// <para>
    /// Consumes one value from the stack and branches to the given destination if the value was true
    /// </para>
    /// <para></para>
    /// <para>br.true destination</para>
    /// <para>... flag -- ...</para>
    /// </summary>
    public class OpcodeBranchIfTrue : BranchOpcode
    {
        protected override string Name { get { return "br.true"; } }
        public override ByteCode Code { get { return ByteCode.BRANCHTRUE; } }

        public override void Execute(ICpu cpu)
        {
            bool condition = Convert.ToBoolean(cpu.PopValueArgument());
            DeltaInstructionPointer = condition ? Distance : 1;
        }
    }

    /// <summary>
    /// <para>
    /// Unconditionally branches to the given destination.
    /// </para>
    /// <para></para>
    /// <para>jump destination</para>
    /// <para>... -- ...</para>
    /// </summary>
    public class OpcodeBranchJump : BranchOpcode
    {
        protected override string Name { get { return "jump"; } }
        public override ByteCode Code { get { return ByteCode.JUMP; } }

        public override void Execute(ICpu cpu)
        {
            DeltaInstructionPointer = Distance;
        }
    }
    
    /// <summary>
    /// Most Opcode.Label fields are just string-ified numbers for their index
    /// position.  But sometimes, when they are the entry point for a function
    /// call (from a lock expression), the label is an identifier string.  When
    /// this is the case, then the mere position of the opcode within the program
    /// is not enough to store the label.  Therefore, for import/export to an ML
    /// file, in this case the numeric label needs to be stored.  It is done by
    /// creating a dummy opcode that is just a no-op instruction intended to be
    /// removed when the program is actually loaded into memory and run.  It
    /// exists purely to store, as an argument, the label of the next opcode to
    /// follow it. Has no stack effect.
    /// </summary>
    public class OpcodeLabelReset : Opcode
    {
        [MLField(0,true)]
        public string UpcomingLabel {get; private set;}

        protected override string Name { get { return "OpcodeLabelReset"; } }
        public override ByteCode Code { get { return ByteCode.LABELRESET; } }

        public OpcodeLabelReset(string myLabel)
        {
            UpcomingLabel = myLabel;
        }

        /// <summary>
        /// This variant of the constructor is just for ML file save/load to use.
        /// </summary>
        protected OpcodeLabelReset() { }

        public override void PopulateFromMLFields(List<object> fields)
        {
            // Expect fields in the same order as the [MLField] properties of this class:
            if (fields == null || fields.Count<1)
                throw new Exception("Saved field in ML file for OpcodePushRelocateLater seems to be missing.  Version mismatch?");
            UpcomingLabel = (string)fields[0];
        }

        public override void Execute(ICpu cpu)
        {
            throw new InvalidOperationException("Attempt to execute OpcodeNonNumericLabel. This type of Opcode should have been replaced before execution.\n");
        }

        public override string ToString()
        {
            return Name + " Label of next thing = {" + UpcomingLabel +"}";
        }
    }

    #endregion

    #region Compare

    /// <summary>
    /// <para>
    /// Consumes 2 values from the stack, pushing back a boolean of if the second is greater than the first.
    /// </para>
    /// <para></para>
    /// <para>gt</para>
    /// <para>... left right -- ... result</para>
    /// </summary>
    public class OpcodeCompareGT : BinaryOpcode
    {
        protected override string Name { get { return "gt"; } }
        public override ByteCode Code { get { return ByteCode.GT; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.GreaterThan(Operands);
        }
    }

    /// <summary>
    /// <para>
    /// Consumes 2 values from the stack, pushing back a boolean of if the second is less than the first.
    /// </para>
    /// <para></para>
    /// <para>lt</para>
    /// <para>... left right -- ... result</para>
    /// </summary>
    public class OpcodeCompareLT : BinaryOpcode
    {
        protected override string Name { get { return "lt"; } }
        public override ByteCode Code { get { return ByteCode.LT; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.LessThan(Operands);
        }
    }

    /// <summary>
    /// <para>
    /// Consumes 2 values from the stack, pushing back a boolean of if the second is greater than or equal to the first.
    /// </para>
    /// <para></para>
    /// <para>gte</para>
    /// <para>... left right -- ... result</para>
    /// </summary>
    public class OpcodeCompareGTE : BinaryOpcode
    {
        protected override string Name { get { return "gte"; } }
        public override ByteCode Code { get { return ByteCode.GTE; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.GreaterThanEqual(Operands);
        }
    }

    /// <summary>
    /// <para>
    /// Consumes 2 values from the stack, pushing back a boolean of if the second is less than or equal to the first.
    /// </para>
    /// <para></para>
    /// <para>lte</para>
    /// <para>... left right -- ... result</para>
    /// </summary>
    public class OpcodeCompareLTE : BinaryOpcode
    {
        protected override string Name { get { return "lte"; } }
        public override ByteCode Code { get { return ByteCode.LTE; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.LessThanEqual(Operands);
        }
    }

    /// <summary>
    /// <para>
    /// Consumes 2 values from the stack, pushing back a boolean of if the second is not equal to the first.
    /// </para>
    /// <para></para>
    /// <para>ne</para>
    /// <para>... left right -- ... result</para>
    /// </summary>
    public class OpcodeCompareNE : BinaryOpcode
    {
        protected override string Name { get { return "ne"; } }
        public override ByteCode Code { get { return ByteCode.NE; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.NotEqual(Operands);
        }
    }

    /// <summary>
    /// <para>
    /// Consumes 2 values from the stack, pushing back a boolean of if the second is equal to the first.
    /// </para>
    /// <para></para>
    /// <para>eq</para>
    /// <para>... left right -- ... result</para>
    /// </summary>
    public class OpcodeCompareEqual : BinaryOpcode
    {
        protected override string Name { get { return "eq"; } }
        public override ByteCode Code { get { return ByteCode.EQ; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.Equal(Operands);
        }
    }

    #endregion

    #region Math
    
    /// <summary>
    /// <para>
    /// Consumes one value from the stack, pushing back the mathematical
    /// negation of the value (i.e. 99 becomes -99)
    /// </para>
    /// <para></para>
    /// <para>negate</para>
    /// <para>... value -- ... negativeValue</para>
    /// </summary>
    public class OpcodeMathNegate : Opcode
    {
        protected override string Name { get { return "negate"; } }
        public override ByteCode Code { get { return ByteCode.NEGATE; } }

        public override void Execute(ICpu cpu)
        {
            Structure value = cpu.PopStructureEncapsulatedArgument();

            var scalarValue = value as ScalarValue;

            if (scalarValue != null && scalarValue.IsValid)
            {
                cpu.PushArgumentStack(-scalarValue);
                return;
            }

            // Generic last-ditch to catch any sort of object that has
            // overloaded the unary negate operator '-'.
            // (For example, kOS.Suffixed.Vector and kOS.Suffixed.Direction)
            Type t = value.GetType();
            MethodInfo negateMe = t.GetMethod("op_UnaryNegation", BindingFlags.FlattenHierarchy |BindingFlags.Static | BindingFlags.Public);
            if (negateMe != null)
            {
                object result = negateMe.Invoke(null, new[]{value});
                cpu.PushArgumentStack(result);
            }
            else
                throw new KOSUnaryOperandTypeException("negate", value);

        }
    }

    /// <summary>
    /// <para>
    /// Consumes 2 values from the stack, pushing back the sum of the 2 values.
    /// </para>
    /// <para></para>
    /// <para>add</para>
    /// <para>... left right -- ... sum</para>
    /// </summary>
    public class OpcodeMathAdd : BinaryOpcode
    {
        protected override string Name { get { return "add"; } }
        public override ByteCode Code { get { return ByteCode.ADD; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            object result = calc.Add(Operands);
            if (result == null)
                throw new KOSBinaryOperandTypeException(Operands, "add", "to");
            return result;
        }
    }

    /// <summary>
    /// <para>
    /// Consumes 2 values from the stack, pushing back the difference of the 2 values.
    /// </para>
    /// <para></para>
    /// <para>sub</para>
    /// <para>... left right -- ... difference</para>
    /// </summary>
    public class OpcodeMathSubtract : BinaryOpcode
    {
        protected override string Name { get { return "sub"; } }
        public override ByteCode Code { get { return ByteCode.SUB; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.Subtract(Operands);
        }
    }

    /// <summary>
    /// <para>
    /// Consumes 2 values from the stack, pushing back the product of the 2 values.
    /// </para>
    /// <para></para>
    /// <para>mult</para>
    /// <para>... left right -- ... product</para>
    /// </summary>
    public class OpcodeMathMultiply : BinaryOpcode
    {
        protected override string Name { get { return "mult"; } }
        public override ByteCode Code { get { return ByteCode.MULT; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.Multiply(Operands);
        }
    }

    /// <summary>
    /// <para>
    /// Consumes 2 values from the stack, pushing back their quotient.
    /// </para>
    /// <para></para>
    /// <para>add</para>
    /// <para>... divident divisor -- ... quotient</para>
    /// </summary>
    public class OpcodeMathDivide : BinaryOpcode
    {
        protected override string Name { get { return "div"; } }
        public override ByteCode Code { get { return ByteCode.DIV; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.Divide(Operands);
        }
    }

    /// <summary>
    /// <para>
    /// Consumes 2 values from the stack, pushing back the result of raising the second value to the power of the first.
    /// </para>
    /// <para></para>
    /// <para>add</para>
    /// <para>... base exponent -- ... power</para>
    /// </summary>
    public class OpcodeMathPower : BinaryOpcode
    {
        protected override string Name { get { return "pow"; } }
        public override ByteCode Code { get { return ByteCode.POW; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.Power(Operands);
        }
    }

    #endregion
    
    #region Logic

    /// <summary>
    /// <para>
    /// Consumes a value from the stack, coercing it to a boolean and then pushing it back.
    /// This uses the nonzero=true Boolean interpretation.
    /// </para>
    /// <para></para>
    /// <para>bool</para>
    /// <para>... value -- ... boolValue</para>
    /// </summary>
    public class OpcodeLogicToBool : Opcode
    {
        protected override string Name { get { return "bool"; } }
        public override ByteCode Code { get { return ByteCode.BOOL; } }

        public override void Execute(ICpu cpu)
        {
            // This may look like it's just pointlessly converting from a
            // ScalarBoolean to a primitive boolean and then back into a
            // ScalarBoolean, and in the case where the operand was already
            // a ScalarBoolean that would be true.  But the purpose of this opcode
            // is to also change integers and floats into booleans. Thus the call to
            // Convert.ToBoolean():
            object value = cpu.PopValueArgument();
            bool result = Convert.ToBoolean(value);
            cpu.PushArgumentStack(Structure.FromPrimitive(result));
        }
    }

    /// <summary>
    /// <para>
    /// Consumes a value from the stack, pushing back the logical not of the value.
    /// If the value on the stack is not a BooleanValue, this will treat it as one
    /// using nonzero=true Boolean interpretation.
    /// </para>
    /// <para></para>
    /// <para>not</para>
    /// <para>... value -- ... notValue</para>
    /// </summary>
    public class OpcodeLogicNot : Opcode
    {
        protected override string Name { get { return "not"; } }
        public override ByteCode Code { get { return ByteCode.NOT; } }

        public override void Execute(ICpu cpu)
        {
            object value = cpu.PopValueArgument();
            object result;

            // Convert to bool instead of cast in case the identifier is stored
            // as an encapsulated BooleanValue, preventing an unboxing collision.
            // Wrapped in a try/catch since the Convert framework doesn't have a
            // way to determine if a type can be converted.
            try
            {
                result = !Convert.ToBoolean(value);
            }
            catch
            {
                throw new KOSCastException(value.GetType(), typeof(BooleanValue));
            }
            cpu.PushArgumentStack(Structure.FromPrimitive(result));
        }
    }

    /// <summary>
    /// <para>
    /// Consumes 2 values from the stack, pushing back a boolean of if both values were true.
    /// If one or more of the values on the stack are not BooleanValues, this will attempt
    /// to treat them as Booleans using the nonzero=true Boolean interpretation.
    /// </para>
    /// <para>The kerboscript compiler avoids using this opcode by using short-circuit logic instead.
    /// This opcode is only left here to support other future potential languages.</para>
    /// <para></para>
    /// <para>and</para>
    /// <para>... left right -- ... both</para>
    /// </summary>
    public class OpcodeLogicAnd : Opcode
    {
        protected override string Name { get { return "and"; } }
        public override ByteCode Code { get { return ByteCode.AND; } }

        public override void Execute(ICpu cpu)
        {
            bool argument2 = Convert.ToBoolean(cpu.PopValueArgument());
            bool argument1 = Convert.ToBoolean(cpu.PopValueArgument());
            object result = argument1 && argument2;
            cpu.PushArgumentStack(Structure.FromPrimitive(result));
        }
    }

    /// <summary>
    /// <para>
    /// Consumes 2 values from the stack, pushing back a boolean of if either of values were true.
    /// If one or more of the values on the stack are not BooleanValues, this will attempt
    /// to treat them as Booleans using the nonzero=true Boolean interpretation.
    /// </para>
    /// <para>The kerboscript compiler avoids using this opcode by using short-circuit logic instead.
    /// This opcode is only left here to support other future potential languages.</para>
    /// <para></para>
    /// <para>or</para>
    /// <para>... left right -- ... either</para>
    /// </summary>
    public class OpcodeLogicOr : Opcode
    {
        protected override string Name { get { return "or"; } }
        public override ByteCode Code { get { return ByteCode.OR; } }

        public override void Execute(ICpu cpu)
        {
            bool argument2 = Convert.ToBoolean(cpu.PopValueArgument());
            bool argument1 = Convert.ToBoolean(cpu.PopValueArgument());
            object result = argument1 || argument2;
            cpu.PushArgumentStack(Structure.FromPrimitive(result));
        }
    }

    #endregion

    #region Call

    /// <summary>
    /// <para>
    /// Calls a subroutine, leaving the result on the stack. What actually happens under the hood depends on what type
    /// of call is happening, but the end result is always the arguments being consumed and the result being put back.
    /// </para>
    /// <para></para>
    /// <para>call destinationLabel destination</para>
    /// <para>... [delegate] argmarker arg1 arg2 .. argN -- ... result</para>
    /// </summary>
    public class OpcodeCall : Opcode
    {

        [MLField(0,true)]
        public override string DestinationLabel { get; set; } // masks Opcode.DestinationLabel - so it can be saved as an MLField.
        
        [MLField(1,true)]
        public object Destination { get; set; }

        protected override string Name { get { return "call"; } }
        public override ByteCode Code { get { return ByteCode.CALL; } }

        public static Type ArgMarkerType { get; private set; } // Don't query with reflection at runtime - get the type just once and keep it here.

        /// <summary>
        /// The Direct property flags which mode the opcode will be operating in:<br/>
        /// <br/>
        /// If its a direct OpcodeCall, then that means the instruction index or function
        /// name being called is contained inside the OpcodeCall's Destination argument,
        /// and the current top of the stack contains the parameters to pass to it.
        /// <br/>
        /// If it's an indirect OpcodeCall, then that means the instruction index or function
        /// name (or delegate) being called is contained in the stack just underneath the argument
        /// list.  A string value of ArgMarkerString will exist on the stack just under the argument
        /// list to differentiate where the arguments stop and the function name or index or
        /// delegate itself is actually stored.
        /// <br/>
        /// EXAMPLE:<br/>
        /// <br/>
        /// Calling a function called "somefunc", which takes 2 parameters:<br/>
        /// If the OpcodeCall is Direct, then the stack should look like this when it's executed:<br/>
        /// <br/>
        /// (arg2)  &lt; -- top of stack<br/>
        /// (arg1) <br/>
        /// <br/>
        /// If the OpcodeCall is Indirect, then the stack should look like this when it's executed:<br/>
        /// <br/>
        /// (arg2)  &lt; -- top of stack<br/>
        /// (arg1) <br/>
        /// (ArgMarkerString) <br/>
        /// ("somefunc" (or a delegate))<br/>
        /// </summary>
        public bool Direct
        {
            // Behind the scenes this is implemented as a flag value in the
            // Destination field.  The Opcode is only indirect if the Destination
            // is a string equal to indirectPlaceholder.
            get
            {
                return !(Destination is string) || Destination.ToString() != INDIRECT_PLACEHOLDER;
            }
            set
            {
                if (value && !Direct)
                    Destination = "TODO"; // If it's indirect and you set it to Direct, you'll need to also give it a Destination.
                else if (!value && Direct)
                    Destination = INDIRECT_PLACEHOLDER;
            }
        }

        private const string INDIRECT_PLACEHOLDER = "<indirect>"; // Guaranteed not to be a possible real function name because of the "<" character.

        public OpcodeCall(object destination)
        {
            Destination = destination;
        }
        /// <summary>
        /// This variant of the constructor is just for machine language file read/write to use.
        /// </summary>
        protected OpcodeCall() { }

        static OpcodeCall()
        {
            ArgMarkerType = typeof(KOSArgMarkerType);
        }

        public override void PopulateFromMLFields(List<object> fields)
        {
            // Expect fields in the same order as the [MLField] properties of this class:
            if (fields == null || fields.Count<1)
                throw new Exception("Saved field in ML file for OpcodeCall seems to be missing.  Version mismatch?");
            // Convert to string instead of cast in case the identifier is stored
            // as an encapsulated StringValue, preventing an unboxing collision.
            DestinationLabel = Convert.ToString(fields[0]);
            Destination = fields[1];
        }

        public override void Execute(ICpu cpu)
        {
            int absoluteJumpTo = StaticExecute(cpu, Direct, Destination, false);
            if (absoluteJumpTo >= 0)
                DeltaInstructionPointer = absoluteJumpTo - cpu.InstructionPointer;
        }
        
        /// <summary>
        /// Performs the actual execution of a subroutine call, either from this opcode or externally from elsewhere.
        /// All "call a routine" logic should shunt through this code here, which handles all the complex cases,
        /// or at least it should.
        /// Note that in the case of a user function, this does not *ACTUALLY* execute the function yet.  It just
        /// arranges the stack correctly for the call and returns the new location that the IP should be jumped to
        /// on the next instruction to begin the subroutine.  For all built-in cases, it actually executes the 
        /// call right now and doesn't return until it's done.  But for User functions it can't do that - it can only
        /// advise on where to jump on the next instruction to begin the function.
        /// </summary>
        /// <param name="cpu">the cpu its running on</param>
        /// <param name="direct">same meaning as OpcodeCall.Direct</param>
        /// <param name="destination">if direct, then this is the function name</param>
        /// <param name="calledFromKOSDelegateCall">true if KOSDelegate.Call() brought us here.  If true that
        /// means any pre-bound args are already on the stack.  If false it means they aren't and this will have to
        /// put them there.</param>
        /// <returns>new IP to jump to, if this should be followed up by a jump.  If -1 then it means don't jump.</returns>
        public static int StaticExecute(ICpu cpu, bool direct, object destination, bool calledFromKOSDelegateCall)
        {
            object functionPointer;
            object delegateReturn = null;
            int newIP = -1; // new instruction pointer to jump to, next, if any.

            if (direct)
            {
                functionPointer = cpu.GetValue(destination);
                if (functionPointer == null)
                    throw new KOSException("Attempt to call function failed - Value of function pointer for " + destination + " is null.");
            }
            else // for indirect calls, dig down to find what's underneath the argument list in the stack and use that:
            {
                bool foundBottom = false;
                int digDepth;
                int argsCount = 0;
                for (digDepth = 0; (! foundBottom) && digDepth < cpu.GetArgumentStackSize() ; ++digDepth)
                {
                    object arg = cpu.PeekValueArgument(digDepth);
                    if (arg != null && arg.GetType() == ArgMarkerType)
                        foundBottom = true;
                    else
                        ++argsCount;
                }
                functionPointer = cpu.PeekValueArgument(digDepth);
                if (! ( functionPointer is Delegate || functionPointer is KOSDelegate || functionPointer is ISuffixResult))
                {
                    // Indirect calls are meant to be delegates.  If they are not, then that means the
                    // function parentheses were put on by the user when they weren't required.  Just dig
                    // through the stack to the result of the getMember and skip the rest of the execute logic

                    // If args were passed to a non-method, then clean them off the stack, and complain:
                    if (argsCount>0)
                    {
                        for (int i=1 ; i<=argsCount; ++i)
                            cpu.PopValueArgument();
                        throw new KOSArgumentMismatchException(
                            0, argsCount, "\n(In fact in this case the parentheses are entirely optional)");
                    }
                    cpu.PopValueArgument(); // pop the ArgMarkerString too.
                    return -1;
                }
            }

            // If it's a string it might not really be a built-in, it might still be a user func.
            // Detect whether it's built-in, and if it's not, then convert it into the equivalent
            // user func call by making it be an integer instruction pointer instead:
            if (functionPointer is string || functionPointer is StringValue)
            {
                string functionName = functionPointer.ToString();
                if (StringUtil.EndsWith(functionName, "()"))
                    functionName = functionName.Substring(0, functionName.Length - 2);
                if (!(cpu.BuiltInExists(functionName)))
                {
                    // It is not a built-in, so instead get its value as a user function pointer variable, despite
                    // the fact that it's being called AS IF it was direct.
                    if (!StringUtil.EndsWith(functionName, "*")) functionName = functionName + "*";
                    if (!StringUtil.StartsWith(functionName, "$")) functionName = "$" + functionName;
                    functionPointer = cpu.GetValue(functionName);
                }
            }

            KOSDelegate kosDelegate = functionPointer as KOSDelegate;
            if (kosDelegate != null)
            {
                if (! calledFromKOSDelegateCall)
                    kosDelegate.InsertPreBoundArgs();
            }

            IUserDelegate userDelegate = functionPointer as IUserDelegate;
            if (userDelegate != null)
            {
                if (userDelegate is NoDelegate)
                    functionPointer = userDelegate; // still leave it as a delegate as a flag to not call the entry point.
                else
                    functionPointer = userDelegate.EntryPoint;
            }

            BuiltinDelegate builtinDel = functionPointer as BuiltinDelegate;
            if (builtinDel != null && (! calledFromKOSDelegateCall) )
                functionPointer = builtinDel.Name;

            // If the IP for a jump location got encapsulated as a user int when it got stored
            // into the internal variable, then get the primitive int back out of it again:
            ScalarIntValue userInt = functionPointer as ScalarIntValue;
            if (userInt != null)
                functionPointer = userInt.GetIntValue();
            
            // Convert to int instead of cast in case the identifier is stored
            // as an encapsulated ScalarValue, preventing an unboxing collision.
            if (functionPointer is int || functionPointer is ScalarValue)
            {
                CpuUtility.ReverseStackArgs(cpu, direct);
                var contextRecord = new SubroutineContext(cpu.InstructionPointer+1);
                newIP = Convert.ToInt32(functionPointer);
                
                cpu.PushScopeStack(contextRecord);
                if (userDelegate != null)
                {
                    cpu.AssertValidDelegateCall(userDelegate);
                    // Reverse-push the closure's scope record, just after the function return context got put on the stack.
                    for (int i = userDelegate.Closure.Count - 1 ; i >= 0 ; --i)
                        cpu.PushScopeStack(userDelegate.Closure[i]);
                }
            }
            else if (functionPointer is string)
            {
                // Built-ins don't need to dereference the stack values because they
                // don't leave the scope - they're not implemented that way.  But later we
                // might want to change that.
                var name = functionPointer as string;
                string functionName = name;
                if (StringUtil.EndsWith(functionName, "()"))
                    functionName = functionName.Substring(0, functionName.Length - 2);
                cpu.CallBuiltinFunction(functionName);

                // If this was indirect, we need to consume the thing under the return value.
                // as that was the indirect BuiltInDelegate:
                if ((! direct) && builtinDel != null)
                {
                    object topThing = cpu.PopArgumentStack();
                    cpu.PopArgumentStack(); // remove BuiltInDelegate object.
                    cpu.PushArgumentStack(topThing); // put return value back.
                }
            }
            else if (functionPointer is ISuffixResult)
            {
                var result = (ISuffixResult) functionPointer;

                if (!result.HasValue)
                {
                    result.Invoke(cpu);
                }

                delegateReturn = result.Value;
            }
            else if (functionPointer is NoDelegate)
            {
                delegateReturn = ((NoDelegate)functionPointer).CallWithArgsPushedAlready();
            }
            // TODO:erendrake This else if is likely never used anymore
            else if (functionPointer is Delegate)
            {
                throw new KOSYouShouldNeverSeeThisException("OpcodeCall unexpected function pointer delegate");
            }
            else
            {
                throw new KOSNotInvokableException(functionPointer);
            }

            if (functionPointer is ISuffixResult || functionPointer is NoDelegate)
            {
                if (! (delegateReturn is KOSPassThruReturn))
                    cpu.PushArgumentStack(delegateReturn); // And now leave the return value on the stack to be read.
            }

            return newIP;
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Name, Destination);
        }
    }

    /// <summary>
    /// <para>
    /// Returns from an OpcodeCall, popping a number of scope depths off
    /// the stack as it does so.  It evals the topmost thing on the stack.
    /// to remove any local variable references and replace them with their
    /// current values, and then performs the equivalent of a popscope, then
    /// jumps back to where the routine was called from.
    /// It also checks to ensure that the argument stack contains the arg
    /// bottom marker.  If it does not, that proves the number of parameters
    /// consumed did not match the number of arguments passed and it throws
    /// an exception (to avoid stack misalignment that would happen if it
    /// tried to continue).
    /// </para>
    /// <para></para>
    /// <para>return depth</para>
    /// <para>... argmarker returnVal -- ... returnVal</para>
    /// </summary>
    public class OpcodeReturn : Opcode
    {
        protected override string Name { get { return "return"; } }
        public override ByteCode Code { get { return ByteCode.RETURN; } }
        
        [MLField(0,true)]
        public Int16 Depth { get; private set; } // Determines how many levels to popscope.

        public override void PopulateFromMLFields(List<object> fields)
        {
            // Expect fields in the same order as the [MLField] properties of this class:
            if (fields == null || fields.Count<1)
                throw new Exception("Saved field in ML file for OpcodeCall seems to be missing.  Version mismatch?");
            Depth = (Int16)fields[0];
        }
        
        // Default constructor is needed for PopulateFromMLFields but shouldn't be used outside the KSM file handler:
        private OpcodeReturn()
        {
        }
        
        /// <summary>
        /// Make a return, telling it how many levels of the scope stack it should
        /// be popping as it does so.  It combines the behavior of a PopScope inside
        /// itself, AFTER it reads and evaluates the thing atop the stack for return
        /// purposes (that way it evals the top thing BEFORE it pops the scope and forgets
        /// what variables exist).<br/>
        /// <br/>
        /// Doing this:<br/>
        ///   push $val<br/>
        ///   return 2 deep<br/>
        /// is the same as this:<br/>
        ///   push $val<br/>
        ///   eval<br/>
        ///   popscope 2<br/>
        ///   return 0 deep<br/>
        /// <br/>
        /// </summary>
        /// <param name="depth">the number of levels to be popped</param>
        public OpcodeReturn(Int16 depth)
        {
            Depth = depth;
        }
        
        public override void Execute(ICpu cpu)
        {
            // Return value should be atop the stack.
            // Pop it, eval it, and push it back,
            // i.e. if the statement was RETURN X, and X is 2, then you want
            // it to return the number 2, not the variable name $x, which could
            // be a variable local to this function which is about to go out of scope
            // so the caller can't access it:
            object returnVal = cpu.PopValueArgument();

            // Now dig down through the stack until the argbottom is found.
            // anything still leftover above that should be unread parameters we
            // should throw away:
            object shouldBeArgMarker = 0; // just a temp to force the loop to execute at least once.
            while (shouldBeArgMarker == null || (shouldBeArgMarker.GetType() != OpcodeCall.ArgMarkerType))
            {
                if (cpu.GetArgumentStackSize() <= 0)
                {
                    throw new KOSArgumentMismatchException(
                        string.Format("Something is wrong with the stack - no arg bottom mark when doing a return.  This is an internal problem with kOS")
                       );
                }
                shouldBeArgMarker = cpu.PopArgumentStack();
            }

            cpu.PushArgumentStack(Structure.FromPrimitive(returnVal));

            // Now, after the eval was done, NOW finally pop the scope, after we don't need local vars anymore:
            if( Depth > 0 )
                OpcodePopScope.DoPopScope(cpu, Depth);

            // The only thing on the "above stack" now that is allowed to get in the way of
            // finding the context record that tells us where to jump back to, are the potential
            // closure scope frames that might have been pushed if this subroutine was
            // called via a delegate reference.  Consume any of those that are in
            // the way, then expect the context record.  Any other pattern encountered
            // is proof the stack alignment got screwed up:
            bool okay;
            VariableScope peeked = cpu.PeekRawScope(0, out okay) as VariableScope;
            while (okay && peeked != null && peeked.IsClosure)
            {
                cpu.PopScopeStack(1);
                peeked = cpu.PeekRawScope(0, out okay) as VariableScope;
            }
            object shouldBeContextRecord = cpu.PopScopeStack(1);
            if ( !(shouldBeContextRecord is SubroutineContext) )
            {
                // This should never happen with any user code:
                throw new Exception( "kOS internal error: Stack misalignment detected when returning from routine.");
            }
            
            var contextRecord = shouldBeContextRecord as SubroutineContext;
            
            // Special case for when the subroutine was really being called as an interrupt
            // trigger from the kOS CPU itself.  In that case we don't want to leave the
            // return value atop the stack, and instead want to pop it and use it:
            if (contextRecord.IsTrigger)
            {
                cpu.PopArgumentStack(); // already got the return value up above, just ignore it.
                TriggerInfo trigger = contextRecord.Trigger;
                // For callbacks, the return value should be preserved in the trigger object
                // so the C# code can find it there.  For non-callbacks, the return value 
                // determines whether or not to re-add the trigger so it happens again.
                // (For C# callbacks, it's always a one-shot call.  The C# code should re-add the
                // trigger itself if it wants to make the call happen again.)
                if (trigger.IsCSharpCallback)
                    trigger.FinishCallback(returnVal);
                else
                    if (returnVal is bool || returnVal is BooleanValue )
                        if (Convert.ToBoolean(returnVal))
                            cpu.AddTrigger(trigger.EntryPoint, trigger.Priority, trigger.InstanceCount, false /*next update, not right now*/, trigger.Closure);
            }
            
            int destinationPointer = contextRecord.CameFromInstPtr;
            int currentPointer = cpu.InstructionPointer;
            DeltaInstructionPointer = destinationPointer - currentPointer;
        }

        public override string ToString()
        {
            return String.Format("{0} {1} deep", Name, Depth);
        }
    }
    #endregion

    #region Stack

    /// <summary>
    /// <para>
    /// Pushes a constant value onto the stack.
    /// </para>
    /// <para></para>
    /// <para>push val</para>
    /// <para>... -- ... val</para>
    /// </summary>
    public class OpcodePush : Opcode
    {
        [MLField(1,false)]
        private object Argument { get; set; }

        protected override string Name { get { return "push"; } }
        public override ByteCode Code { get { return ByteCode.PUSH; } }

        public OpcodePush(object argument)
        {
            Argument = argument;
        }

        /// <summary>
        /// This variant of the constructor is just for ML file save/load to use.
        /// </summary>
        protected OpcodePush() { }

        public override void PopulateFromMLFields(List<object> fields)
        {
            // Expect fields in the same order as the [MLField] properties of this class:
            if (fields == null || fields.Count<1)
                throw new Exception("Saved field in ML file for OpcodePush seems to be missing.  Version mismatch?");
            Argument = fields[0];
        }

        public override void Execute(ICpu cpu)
        {
            cpu.PushArgumentStack(Argument);
        }

        public override string ToString()
        {
            string argumentString = Argument != null ? Argument.ToString() : "null";
            return Name + " " + argumentString;
        }
    }
    
    /// <summary>
    /// This class is an ugly placeholder to handle the fact that sometimes
    /// The compiler creates an OpcodePush that uses relocatable DestinationLabels as a
    /// temporary place to store their arguments in the list until they get added to the program.
    /// 
    /// Basically, it's this:
    /// 
    /// Old way:
    /// 1. In some cases, like setting up locks, Compiler would create an OpcodePush with Argument = null,
    /// and a DestinationLabel = something.
    /// 2. ProgramBuilder would rebuild the OpcodePush's Argument by copying it from the DestinationLabel
    /// as part of ReplaceLabels at runtime.
    /// 
    /// The Problem: When storing this in the ML file, BOTH the Argument AND the DestinationLabel would
    /// need to be stored as [MLFields] even though they are never BOTH populated at the same time, which
    /// causes ridiculous bloat.
    /// 
    /// New Way:
    /// 1. Compiler creates an OpcodePushRelocateLater with a DestinationLabel = something.
    /// 2. ProgramBuilder replaces this with a normal OpcodePush who's argument is set to
    /// the relocated value of the DestinationLabel.
    /// 
    /// Why:
    /// This way the MLFile only stores 1 argument: If it's an OpcodePush, it stores
    /// OpcodePush.Argument.  If it's an OpcodePushRelocateLater, then it stores just
    /// OpcodePushRelocateLater.DestinationLabel and no argument.
    /// 
    /// </summary>
    public class OpcodePushRelocateLater : Opcode
    {
        [MLField(0,true)]
        public override sealed string DestinationLabel {get;set;}

        protected override string Name { get { return "PushRelocateLater"; } }
        public override ByteCode Code { get { return ByteCode.PUSHRELOCATELATER; } }

        public OpcodePushRelocateLater(string destLabel)
        {
            DestinationLabel = destLabel;
        }

        /// <summary>
        /// This variant of the constructor is just for ML file save/load to use.
        /// </summary>
        protected OpcodePushRelocateLater() { }

        public override void PopulateFromMLFields(List<object> fields)
        {
            // Expect fields in the same order as the [MLField] properties of this class:
            if (fields == null || fields.Count<1)
                throw new Exception("Saved field in ML file for OpcodePushRelocateLater seems to be missing.  Version mismatch?");
            DestinationLabel = (string)( fields[0] );
        }

        public override void Execute(ICpu cpu)
        {
            throw new InvalidOperationException("Attempt to execute OpcodePushRelocateLater. This type of Opcode should have been replaced before execution.\n");
        }

        public override string ToString()
        {
            return Name + " Dest{" + DestinationLabel +"}";
        }
    }

    /// <summary>
    /// <para>
    /// Pops a value off the stack, discarding it.
    /// </para>
    /// <para></para>
    /// <para>pop</para>
    /// <para>... val -- ...</para>
    /// </summary>
    public class OpcodePop : Opcode
    {
        protected override string Name { get { return "pop"; } }
        public override ByteCode Code { get { return ByteCode.POP; } }

        public override void Execute(ICpu cpu)
        {
            // Even though this value is being thrown away it's still important to attempt to
            // process it (with cpu.PopValueArgument()) rather than ignore it (with cpu.PopArgumentStack()).  This
            // is just in case it's an unknown variable name in need of an error message
            // to the user.  Detecting that a variable name is unknown occurs during the popping
            // of the value, not the pushing of it.  (This is necessary because SET and DECLARE
            // statements have to be allowed to push undefined variable references onto the stack
            // for new variables that they are going to create.)

            cpu.PopValueArgument();
        }
    }

    /// <summary>
    /// <para>
    /// Asserts that the next thing on the stack is the argument bottom marker.
    /// If it's not the argument bottom, it throws an error.
    /// This does NOT pop the value from the stack - it merely peeks at the stack top.
    /// The actual popping of the arg bottom value comes later when doing a return,
    /// or a program bottom exit.
    /// </para>
    /// <para></para>
    /// <para>argbottom</para>
    /// <para>... argmarker -- ... argmarker</para>
    /// </summary>
    public class OpcodeArgBottom : Opcode
    {
        protected override string Name { get { return "argbottom"; } }
        public override ByteCode Code { get { return ByteCode.ARGBOTTOM; } }

        public override void Execute(ICpu cpu)
        {
            bool worked;
            object shouldBeArgMarker = cpu.PeekRawArgument(0,out worked);

            if ( !worked || (shouldBeArgMarker == null) || (shouldBeArgMarker.GetType() != OpcodeCall.ArgMarkerType) )
            {
                throw new KOSArgumentMismatchException("Called with too many arguments.");
            }
        }
    }

    /// <summary>
    /// <para>
    /// Tests whether or not the next thing on the stack is the argument bottom marker.
    /// It pushes a true on top if it is, or false if it is not.  In either case it does
    /// NOT consume the arg bottom marker, but just peeks for it.
    /// </para>
    /// <para></para>
    /// <para>testargbottom</para>
    /// <para>... argbottom? -- ... argbottom? isargbottom</para>
    /// </summary>
    public class OpcodeTestArgBottom : Opcode
    {
        protected override string Name { get { return "testargbottom"; } }
        public override ByteCode Code { get { return ByteCode.TESTARGBOTTOM; } }

        public override void Execute(ICpu cpu)
        {
            bool worked;
            object shouldBeArgMarker = cpu.PeekRawArgument(0,out worked);

            if ( !worked || (shouldBeArgMarker == null) || (shouldBeArgMarker.GetType() != OpcodeCall.ArgMarkerType) )
            {
                cpu.PushArgumentStack(false); // these are internally used, so no Strucutre.FromPrimitive wrapper call.
            }
            else
            {
                cpu.PushArgumentStack(true); // these are internally used, so no Strucutre.FromPrimitive wrapper call.
            }
        }
    }

    /// <summary>
    /// Tests whether or not the current subroutine context on the stack that is being
    /// executed right now is one that has been flagged as cancelled by someone
    /// having called SubroutineContext.Cancel().  This pushes a True or a False on
    /// the stack to provide the answer.  This should be the first thing done by triggers
    /// that wish to be cancel-able by other triggers.  (For example if someone unlocks
    /// steering in one trigger, the steering function should not be run after that even
    /// if it had been queued up at the start of this physics tick)  If you are a trigger
    /// that wishes to be cancel-able in this fashion, your trigger body should start by
    /// first calling this to see if you have been cancelled, and if it returns true,
    /// then you should return early without doing the rest of your body.
    /// <br/><br/>
    /// See kOS Github issue 2178 for a lengthy discussion
    /// about what caused the need for this.
    /// </summary>
    public class OpcodeTestCancelled : Opcode
    {
        protected override string Name { get { return "testcancelled"; } }
        public override ByteCode Code { get { return ByteCode.TESTCANCELLED; } }

        public override void Execute(ICpu cpu)
        {
            SubroutineContext sr = cpu.GetCurrentSubroutineContext();
            cpu.PushArgumentStack(new BooleanValue((sr == null ? false : sr.IsCancelled)));
        }
    }

    /// <summary>
    /// <para>
    /// Push the thing atop the stack onto the stack again so there are now two of it atop the stack.
    /// </para>
    /// <para></para>
    /// <para>dup</para>
    /// <para>... val -- ... val val</para>
    /// </summary>
    public class OpcodeDup : Opcode
    {
        protected override string Name { get { return "dup"; } }
        public override ByteCode Code { get { return ByteCode.DUP; } }

        public override void Execute(ICpu cpu)
        {
            object value = cpu.PopArgumentStack();
            cpu.PushArgumentStack(value);
            cpu.PushArgumentStack(value);
        }
    }

    /// <summary>
    /// <para>
    /// Swaps the order of the top 2 values on the stack.
    /// </para>
    /// <para></para>
    /// <para>swap</para>
    /// <para>... val1 val2 -- ... val2 val1</para>
    /// </summary>
    public class OpcodeSwap : Opcode
    {
        protected override string Name { get { return "swap"; } }
        public override ByteCode Code { get { return ByteCode.SWAP; } }

        public override void Execute(ICpu cpu)
        {
            object value1 = cpu.PopArgumentStack();
            object value2 = cpu.PopArgumentStack();
            cpu.PushArgumentStack(value1);
            cpu.PushArgumentStack(value2);
        }
    }
    
    /// <summary>
    /// <para>
    /// Replaces the topmost thing on the stack with its evaluated,
    /// fully dereferenced version.  For example, if the variable
    /// foo contains value 4, and the top of the stack is the
    /// identifier name "$foo", then this will replace the "$foo"
    /// with a 4.
    /// </para>
    /// <para></para>
    /// <para>eval</para>
    /// <para>... nameOrVal -- ... val</para>
    /// </summary>
    public class OpcodeEval : Opcode
    {
        protected override string Name { get { return "eval"; } }
        public override ByteCode Code { get { return ByteCode.EVAL; } }
        private bool barewordOkay;
        
        public OpcodeEval()
        {
            barewordOkay = false;
        }
        
        /// <summary>
        /// Eval top thing on the stack and replace it with its dereferenced
        /// value.  If you want to allow bare words like filenames then set argument bareOkay to true
        /// when constructing.
        /// </summary>
        /// <param name="bareOkay"></param>
        public OpcodeEval(bool bareOkay)
        {
            barewordOkay = bareOkay;
        }

        public override void Execute(ICpu cpu)
        {
            cpu.PushArgumentStack(cpu.PopValueEncapsulatedArgument(barewordOkay));
        }
    }

    /// <summary>
    /// Pushes a new variable namespace scope (for example, when a "{" is encountered
    /// in a block-scoping language like C++ or Java or C#.)
    /// From now on any local variables created will be made in this new
    /// namespace. Has no argument stack effect.
    /// </summary>
    public class OpcodePushScope : Opcode
    {
        [MLField(1,true)]
        public Int16 ScopeId {get;set;}
        [MLField(2,true)]
        public Int16 ParentScopeId {get;set;}

        /// <summary>
        /// Push a scope frame that knows the id of its lexical parent scope.
        /// </summary>
        /// <param name="id">the unique id of this scope frame.</param>
        /// <param name="parentId">the unique id of the scope frame this scope is inside of.</param>
        public OpcodePushScope(Int16 id, Int16 parentId)
        {
            ScopeId = id;
            ParentScopeId = parentId;
        }

        /// <summary>
        /// This variant of the constructor is just for ML file save/load to use.
        /// </summary>
        protected OpcodePushScope()
        {
            ScopeId = -1;
            ParentScopeId = -1;
        }

        public override void PopulateFromMLFields(List<object> fields)
        {
            // Expect fields in the same order as the [MLField] properties of this class:
            if (fields == null || fields.Count<2)
                throw new Exception("Saved field in ML file for OpcodePushScope seems to be missing.  Version mismatch?");
            ScopeId = (Int16)( fields[0] );
            ParentScopeId = (Int16)( fields[1] );
        }

        protected override string Name { get { return "pushscope"; } }
        public override ByteCode Code { get { return ByteCode.PUSHSCOPE; } }
        
        public override void Execute(ICpu cpu)
        {
            cpu.PushNewScope(ScopeId,ParentScopeId);
        }

        public override string ToString()
        {
            return String.Format("{0} {1} {2}", Name, ScopeId, ParentScopeId);
        }
        
    }

    /// <summary>
    /// Pops a variable namespace scope (for example, when a "}" is encountered
    /// in a block-scoping language like C++ or Java or C#.)
    /// From now on any local variables created within the previous scope are
    /// orphaned and gone forever ready to be garbage collected.
    /// It is possible to give it an argument to pop more than one nesting level of scope, to
    /// handle the case where you are breaking out of more than one nested level at once.
    /// (i.e. such as might happen with a break, return, or exit keyword).
    /// Has no argument stack effect.
    /// </summary>
    public class OpcodePopScope : Opcode
    {
        [MLField(1,true)]
        public Int16 NumLevels {get;set;} // Are we really going to have recursion more than 32767 levels?  Int16 is fine.

        protected override string Name { get { return "popscope"; } }
        public override ByteCode Code { get { return ByteCode.POPSCOPE; } }
        
        public OpcodePopScope(int numLevels)
        {
            NumLevels = (Int16)numLevels;
        }
        
        public OpcodePopScope()
        {
            NumLevels = 1;
        }

        public override void PopulateFromMLFields(List<object> fields)
        {
            // Expect fields in the same order as the [MLField] properties of this class:
            if (fields == null || fields.Count<1)
                throw new Exception("Saved field in ML file for OpcodePopScope seems to be missing.  Version mismatch?");
            NumLevels = (Int16)(fields[0]); // should throw error if it's not an int.
        }

        public override void Execute(ICpu cpu)
        {
            DoPopScope(cpu, NumLevels);
        }
        
        /// <summary>
        /// Do the actual work of the Execute() method.  This was pulled out
        /// to a separate static method so that others can call it without needing
        /// an actual popscope object.  Everything OpcodePopScope.Execute() does
        /// should actually be done here, so as to ensure that external callers of
        /// this get exactly the same behaviour as a full popstack opcode.
        /// </summary>
        /// <param name="cpuObj">the shared.cpu to operate on.</param>
        /// <param name="levels">number of levels to popscope.</param>
        public static void DoPopScope(ICpu cpuObj, Int16 levels)
        {
            cpuObj.PopScopeStack(levels);
        }

        public override string ToString()
        {
            return Name + " " + NumLevels;
        }
        
    }

    /// <summary>
    /// <para>
    /// Pushes a delegate object onto the stack, optionally capturing a closure.
    /// </para>
    /// <para></para>
    /// <para>pushdelegate entrypoint withClosure</para>
    /// <para>... -- ... del</para>
    /// </summary>
    public class OpcodePushDelegate : Opcode
    {
        [MLField(1,false)]
        private int EntryPoint { get; set; }
        [MLField(2,false)]
        private bool WithClosure { get; set; }

        protected override string Name { get { return "pushdelegate"; } }
        public override ByteCode Code { get { return ByteCode.PUSHDELEGATE; } }

        public OpcodePushDelegate(int entryPoint, bool withClosure)
        {
            EntryPoint = entryPoint;
            WithClosure = withClosure;
        }

        /// <summary>
        /// This variant of the constructor is just for ML file save/load to use.
        /// </summary>
        protected OpcodePushDelegate() { }

        public override void PopulateFromMLFields(List<object> fields)
        {
            // Expect fields in the same order as the [MLField] properties of this class:
            if (fields == null || fields.Count<2)
                throw new Exception("Saved field in ML file for OpcodePushDelegate seems to be missing.  Version mismatch?");
            EntryPoint = Convert.ToInt32(fields[0]);
            WithClosure = Convert.ToBoolean(fields[1]);
        }

        public override void Execute(ICpu cpu)
        {
            IUserDelegate pushMe = cpu.MakeUserDelegate(EntryPoint, WithClosure);
            cpu.PushArgumentStack(pushMe);
        }

        public override string ToString()
        {
            return Name + " " + EntryPoint.ToString() + (WithClosure ? " closure" : "");
        }
    }
        
    /// <summary>
    /// This serves the same purpose as OpcodePushRelocateLater, except it's for
    /// use with UserDelegates instead of raw integer IP calls.
    /// </summary>
    public class OpcodePushDelegateRelocateLater : OpcodePushRelocateLater
    {
        [MLField(100,false)]
        public bool WithClosure { get; set; }

        protected override string Name { get { return "PushDelegateRelocateLater"; } }
        public override ByteCode Code { get { return ByteCode.PUSHDELEGATERELOCATELATER; } }

        public OpcodePushDelegateRelocateLater(string destLabel, bool withClosure) : base(destLabel)
        {
            WithClosure = withClosure;
        }

        /// <summary>
        /// This variant of the constructor is just for ML file save/load to use.
        /// </summary>
        protected OpcodePushDelegateRelocateLater()
        {}

        public override void PopulateFromMLFields(List<object> fields)
        {
            // Expect fields in the same order as the [MLField] properties of this class:
            if (fields == null || fields.Count<1)
                throw new Exception("Saved field in ML file for OpcodePushDelegateRelocatelater seems to be missing.  Version mismatch?");
            DestinationLabel = Convert.ToString(fields[0]); // this is really from the base class.
            WithClosure = Convert.ToBoolean(fields[1]);
        }
    }
    
    #endregion

    #region Wait / Trigger

    /// <summary>
    /// <para>
    /// Pops a function pointer from the stack and adds a trigger that will be called each cycle.
    /// The argument (to the opcode, not on the stack) contains the Interrupt Priority level
    /// of the trigger.  For one trigger to interrupt another, it needs a higher priority,
    /// else it waits until the first trigger is completed before it will fire.
    /// </para>
    /// <para></para>
    /// <para>addtrigger N</para>
    /// <para>... fp -- ...</para>
    /// </summary>
    public class OpcodeAddTrigger : Opcode
    {

        protected override string Name { get { return "addtrigger"; } }
        public override ByteCode Code { get { return ByteCode.ADDTRIGGER; } }

        /// <summary>
        /// True if the trigger being added should be called with an argument
        /// that identifies this instance/entrypoint uniquely at runtime.
        /// (For example, ON triggers need this, but WHEN triggers do not).
        /// </summary>
        [MLField(1,false)]
        public bool Unique { get; set; }
        /// <summary>
        /// The interrupt priority level of the trigger.
        /// (It's an Int32 type instead of InterruptPrioirity purely because MLFields
        /// need to be one of the limited primitive types the system knows how to store.)
        /// </summary>
        [MLField(2, false)]
        public Int32 Priority { get; set; }

        public OpcodeAddTrigger(bool unique, InterruptPriority priority)
        {
            Unique = unique;
            Priority = (Int32)priority;
        }

        public OpcodeAddTrigger(InterruptPriority priority) // Must have a defualt constructor for how KSM files work.
        {
            Unique = true;
            Priority = (Int32)priority;
        }

        /// <summary>Only here because the compile storage system requires a default constructor.
        /// It's private because we want to force everyone ELSE to use one of the versions with args.
        /// </summary>
        private OpcodeAddTrigger()
        {
        }

        public override void PopulateFromMLFields(List<object> fields)
        {
            // Expect fields in the same order as the [MLField] properties of this class:
            if (fields == null || fields.Count < 2)
                throw new Exception("Saved field in ML file for OpcodeAddTrigger seems to be missing.  Version mismatch?");
            Unique = Convert.ToBoolean(fields[0]);
            Priority = Convert.ToInt32(fields[1]);
        }

        public override void Execute(ICpu cpu)
        {
            int functionPointer = Convert.ToInt32(cpu.PopValueArgument()); // in case it got wrapped in a ScalarIntValue

            List<Structure> args = new List<Structure>();
            cpu.AddTrigger(functionPointer, (InterruptPriority) Priority, (Unique ? cpu.NextTriggerInstanceId : 0), false, cpu.GetCurrentClosure());
        }

        public override string ToString()
        {
            return string.Format("{0}{1}, Pri {2} ", Name, (Unique ? " unique" : ""), Priority );
        }
    }

    /// <summary>
    /// <para>
    /// Pops a function pointer from the stack and removes any triggers that call that function pointer.
    /// </para>
    /// <para></para>
    /// <para>removetrigger</para>
    /// <para>... fp -- ...</para>
    /// </summary>
    public class OpcodeRemoveTrigger : Opcode
    {
        protected override string Name { get { return "removetrigger"; } }
        public override ByteCode Code { get { return ByteCode.REMOVETRIGGER; } }

        public override void Execute(ICpu cpu)
        {
            var functionPointer = Convert.ToInt32(cpu.PopValueArgument()); // in case it got wrapped in a ScalarIntValue
            cpu.RemoveTrigger(functionPointer, 0);
            cpu.CancelCalledTriggers(functionPointer, 0);
        }
    }

    /// <summary>
    /// <para>
    /// Pops a duration in seconds from the stack and yields execution for that amount of game time.
    /// </para>
    /// <para></para>
    /// <para>wait</para>
    /// <para>... duration -- ...</para>
    /// </summary>
    public class OpcodeWait : Opcode
    {
        protected override string Name { get { return "wait"; } }
        public override ByteCode Code { get { return ByteCode.WAIT; } }

        public override void Execute(ICpu cpu)
        {
            double arg = Convert.ToDouble(cpu.PopValueArgument());
            cpu.YieldProgram(new YieldFinishedGameTimer(arg));
        }
    }

    #endregion

}

