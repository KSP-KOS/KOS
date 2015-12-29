using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Execution;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
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
        ENDWAIT        = 0x56,
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
            "|  THE FOLLOWING DOeS NOT HAVE A DEFAULT CONSTRUCTOR:                     |\n" +
            "|  {0,30}                                         |\n" +
            "|                                                                         |\n" +
            "+-------------------------------------------------------------------------+\n";

        public int Id { get { return id; } }
        public int DeltaInstructionPointer { get; protected set; } 
        public int MLIndex { get; set; } // index into the Machine Language code file for the COMPILE command.
        public string Label {get{return label;} set {label = value;} }
        public virtual string DestinationLabel {get;set;}
        public string SourceName;

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
            IEnumerable<Type> opcodeTypes = opcodeType.Assembly.GetTypes().Where( t => t.IsSubclassOf(opcodeType) );
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
            var attributes1 = new List<Attribute>(a1.PropertyInfo.GetCustomAttributes(true) as Attribute[]);
            var attributes2 = new List<Attribute>(a2.PropertyInfo.GetCustomAttributes(true) as Attribute[]);
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
        /// Return the list of member Properties that are part of what gets stored to machine langauge
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
            object returnValue = cpu.PopValue(barewordOkay);
            if (returnValue != null && returnValue.GetType() == OpcodeCall.ArgMarkerType)
                throw new KOSArgumentMismatchException("Called with not enough arguments.");
            return returnValue;
        }
    }

    public abstract class BinaryOpcode : Opcode
    {
        protected OperandPair Operands { get; private set; }

        public override void Execute(ICpu cpu)
        {
            object right = cpu.PopValue();
            object left = cpu.PopValue();

            var operands = new OperandPair(left, right);

            Calculator calc = Calculator.GetCalculator(operands);
            Operands = operands;
            object result = ExecuteCalculation(calc);
            cpu.PushStack(result);
        }

        protected virtual object ExecuteCalculation(Calculator calc)
        {
            return null;
        }
    }

    #endregion

    #region General


    /// <summary>
    /// Consumes the topmost 2 values of the stack, storing the topmost stack
    /// value into a variable described by the next value down the stack. <br/>
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
    public class OpcodeStore : Opcode
    {
        protected override string Name { get { return "store"; } }
        public override ByteCode Code { get { return ByteCode.STORE; } }

        public override void Execute(ICpu cpu)
        {
            object value = PopValueAssert(cpu);
            // Convert to string instead of cast in case the identifier is stored
            // as an encapsulated StringValue, preventing an unboxing collision.
            var identifier = Convert.ToString(cpu.PopStack());
            cpu.SetValue(identifier, value);
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
            string ident = Convert.ToString(cpu.PopStack());
            if (ident != null && cpu.IdentifierExistsInScope(ident))
            {
                result = true;
            }
            cpu.PushStack(result);
        }
    }

    /// <summary>
    /// Consumes the topmost 2 values of the stack, storing the topmost stack
    /// value into a variable described by the next value down the stack. <br/>
    /// <br/>
    /// Unlike OpcodeStore, OpcodeStoreExist will NOT create the variable if it
    /// does not already exist.  Instead it will cause an
    /// error.  (It corresponds to kerboscript's @LAZYGLOBAL OFF directive).<br/>
    /// <br/>
    /// </summary>
    public class OpcodeStoreExist : Opcode
    {
        protected override string Name { get { return "storeexist"; } }
        public override ByteCode Code { get { return ByteCode.STOREEXIST; } }

        public override void Execute(ICpu cpu)
        {
            object value = PopValueAssert(cpu);
            // Convert to string instead of cast in case the identifier is stored
            // as an encapsulated StringValue, preventing an unboxing collision.
            var identifier = Convert.ToString(cpu.PopStack());
            cpu.SetValueExists(identifier, value);
        }
    }
    
    /// <summary>
    /// Consumes the topmost 2 values of the stack, storing the topmost stack
    /// value into a variable described by the next value down the stack. <br/>
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
    public class OpcodeStoreLocal : Opcode
    {
        protected override string Name { get { return "storelocal"; } }
        public override ByteCode Code { get { return ByteCode.STORELOCAL; } }

        public override void Execute(ICpu cpu)
        {
            object value = PopValueAssert(cpu);
            // Convert to string instead of cast in case the identifier is stored
            // as an encapsulated StringValue, preventing an unboxing collision.
            var identifier = Convert.ToString(cpu.PopStack());
            cpu.SetNewLocal(identifier, value);
        }
    }

    /// <summary>
    /// Consumes the topmost 2 values of the stack, storing the topmost stack
    /// value into a variable described by the next value down the stack. <br/>
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
    public class OpcodeStoreGlobal : Opcode
    {
        protected override string Name { get { return "storeglobal"; } }
        public override ByteCode Code { get { return ByteCode.STOREGLOBAL; } }

        public override void Execute(ICpu cpu)
        {
            object value = PopValueAssert(cpu);
            // Convert to string instead of cast in case the identifier is stored
            // as an encapsulated StringValue, preventing an unboxing collision.
            var identifier = Convert.ToString(cpu.PopStack());
            cpu.SetGlobal(identifier, value);
        }
    }

    public class OpcodeUnset : Opcode
    {
        protected override string Name { get { return "unset"; } }
        public override ByteCode Code { get { return ByteCode.UNSET; } }

        public override void Execute(ICpu cpu)
        {
            object identifier = cpu.PopStack();
            if (identifier != null)
            {
                cpu.RemoveVariable(identifier.ToString());
            }
            else
            {
                throw new KOSDeprecationException("0.17","UNSET ALL", "<not supported anymore now that we have nested scoping>", "");
            }
        }
    }
    
    public class OpcodeGetMember : Opcode
    {
        protected override string Name { get { return "getmember"; } }
        public override ByteCode Code { get { return ByteCode.GETMEMBER; } }
        protected bool IsMethodCallAttempt = false;

        public override void Execute(ICpu cpu)
        {
            string suffixName = cpu.PopStack().ToString().ToUpper();
            object popValue = Structure.FromPrimitive(cpu.PopValue());
            // We convert the popValue to a structure to ensure that we can get suffixes on values
            // stored in primitive form as a fall back.  All variables should be stored as a structure
            // now, other than system variables like pointers and labels.  This technically means that
            // a change performed by calling a function on Scalar, Boolean, or String values might not
            // save the value to the original objet.

            var specialValue = popValue as ISuffixed;
            
            if (specialValue == null)
            {
                throw new Exception(string.Format("Values of type {0} cannot have suffixes", popValue.GetType()));
            }

            object value = specialValue.GetSuffix(suffixName);
            if (value is Delegate && !IsMethodCallAttempt)
            {
                // This is what happens when someone tries to call a suffix method as if
                // it wasn't a method (i.e. leaving the parentheses off the call).  The
                // member returned is a delegate that needs to be called to get its actual
                // value.  Borrowing the same routine that OpcodeCall uses for its method calls:
                cpu.PushStack(new KOSArgMarkerType());
                value = OpcodeCall.ExecuteDelegate(cpu, (Delegate)value);
            }
            // TODO: When we refactor to make every structure use the new suffix style, this conversion
            // from primative can be removed.  Right now there are too many structures that override the
            // GetSuffix method and return their own types, preventing us from converting directly in
            // the GetSuffix method.
            value = Structure.FromPrimitive(value);

            cpu.PushStack(value);
        }
    }
    
    /// <summary>
    /// OpcodeGetMethod is *exactly* the same thing as OpcodeGetMember, and is in fact a subclass of it.
    /// The only reason for the distinction is so that at runtime the Opcode can tell whether the
    /// getting of the member was done with method call syntax with parentheses, like SHIP:NAME(), or
    /// non-method call syntax, like SHIP:NAME. It needs to know whether there is an upcoming
    /// OpcodeCall coming next or not, so it knows whether the delegate will get dealt with later
    /// or if it needs to perform it now.
    /// </summary>
    public class OpcodeGetMethod : OpcodeGetMember
    {
        protected override string Name { get { return "getmethod"; } }
        public override ByteCode Code { get { return ByteCode.GETMETHOD; } }
        public override void Execute(ICpu cpu)
        {
            IsMethodCallAttempt = true;
            base.Execute(cpu);
        }
    }

    
    public class OpcodeSetMember : Opcode
    {
        protected override string Name { get { return "setmember"; } }
        public override ByteCode Code { get { return ByteCode.SETMEMBER; } }

        public override void Execute(ICpu cpu)
        {
            object value = cpu.PopValue();
            string suffixName = cpu.PopStack().ToString().ToUpper();
            object popValue = cpu.PopValue();
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
            if (!specialValue.SetSuffix(suffixName, Structure.ToPrimitive(value)))
            {
                throw new Exception(string.Format("Suffix {0} not found on object", suffixName));
            }
        }
    }

    
    public class OpcodeGetIndex : Opcode
    {
        protected override string Name { get { return "getindex"; } }
        public override ByteCode Code { get { return ByteCode.GETINDEX; } }

        public override void Execute(ICpu cpu)
        {
            object index = cpu.PopValue();
            object list = cpu.PopValue();

            object value;

            var indexable = list as IIndexable;
            if (indexable != null)
            {
                value = indexable.GetIndex(index);
            }
            // Box strings if necessary to allow them to be indexed
            else if (list is string)
            {
                value = new StringValue((string) list).GetIndex(index);
            }
            else
            {
                throw new Exception(string.Format("Can't iterate on an object of type {0}", list.GetType()));
            }

            if (!(list is IIndexable)) throw new Exception(string.Format("Can't iterate on an object of type {0}", list.GetType()));

            cpu.PushStack(value);
        }
    }

    
    public class OpcodeSetIndex : Opcode
    {
        protected override string Name { get { return "setindex"; } }
        public override ByteCode Code { get { return ByteCode.SETINDEX; } }

        public override void Execute(ICpu cpu)
        {
            object value = cpu.PopValue();
            object index = cpu.PopValue();
            object list = cpu.PopValue();

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

    public class OpcodeEOF : Opcode
    {
        protected override string Name { get { return "EOF"; } }
        public override ByteCode Code { get { return ByteCode.EOF; } }
        public override void Execute(ICpu cpu)
        {
            AbortContext = true;
        }
    }

    
    public class OpcodeEOP : Opcode
    {
        protected override string Name { get { return "EOP"; } }
        public override ByteCode Code { get { return ByteCode.EOP; } }
        public override void Execute(ICpu cpu)
        {
            AbortProgram = true;
        }
    }

    
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
                DestinationLabel = (string)(fields[0]);
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

    
    public class OpcodeBranchIfFalse : BranchOpcode
    {
        protected override string Name { get { return "br.false"; } }
        public override ByteCode Code { get { return ByteCode.BRANCHFALSE; } } // branch if zero - a longstanding name for this op in many machine codes.

        public override void Execute(ICpu cpu)
        {
            bool condition = Convert.ToBoolean(cpu.PopValue());

            DeltaInstructionPointer = !condition ? Distance : 1;
        }
    }
    
    public class OpcodeBranchIfTrue : BranchOpcode
    {
        protected override string Name { get { return "br.true"; } }
        public override ByteCode Code { get { return ByteCode.BRANCHTRUE; } }

        public override void Execute(ICpu cpu)
        {
            bool condition = Convert.ToBoolean(cpu.PopValue());
            DeltaInstructionPointer = condition ? Distance : 1;
        }
    }

    
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
    /// follow it.
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
            UpcomingLabel = (string)( fields[0] );
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

    
    public class OpcodeCompareGT : BinaryOpcode
    {
        protected override string Name { get { return "gt"; } }
        public override ByteCode Code { get { return ByteCode.GT; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.GreaterThan(Operands);
        }
    }

    
    public class OpcodeCompareLT : BinaryOpcode
    {
        protected override string Name { get { return "lt"; } }
        public override ByteCode Code { get { return ByteCode.LT; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.LessThan(Operands);
        }
    }

    
    public class OpcodeCompareGTE : BinaryOpcode
    {
        protected override string Name { get { return "gte"; } }
        public override ByteCode Code { get { return ByteCode.GTE; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.GreaterThanEqual(Operands);
        }
    }

    
    public class OpcodeCompareLTE : BinaryOpcode
    {
        protected override string Name { get { return "lte"; } }
        public override ByteCode Code { get { return ByteCode.LTE; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.LessThanEqual(Operands);
        }
    }

    
    public class OpcodeCompareNE : BinaryOpcode
    {
        protected override string Name { get { return "ne"; } }
        public override ByteCode Code { get { return ByteCode.NE; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.NotEqual(Operands);
        }
    }
    
    
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
        
    
    public class OpcodeMathNegate : Opcode
    {
        protected override string Name { get { return "negate"; } }
        public override ByteCode Code { get { return ByteCode.NEGATE; } }

        public override void Execute(ICpu cpu)
        {
            object value = cpu.PopValue();
            object result;

            if (value is int)
                result = -((int)value);
            else if (value is float)
                result = -(Convert.ToDouble(value));
            else if (value is double)
                result = -((double)value);
            else
            {
                // Generic last-ditch to catch any sort of object that has
                // overloaded the unary negate operator '-'.
                // (For example, kOS.Suffixed.Vector and kOS.Suffixed.Direction)
                Type t = value.GetType();
                MethodInfo negateMe = t.GetMethod("op_UnaryNegation", BindingFlags.FlattenHierarchy |BindingFlags.Static | BindingFlags.Public); // C#'s alternate name for '-' operator
                if (negateMe != null)
                    result = negateMe.Invoke(null, new[]{value}); // value is an arg, not the 'this'.  (Method is static.)
                else
                    throw new KOSUnaryOperandTypeException("negate", value);
            }

            cpu.PushStack(result);
        }
    }

    
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

    
    public class OpcodeMathSubtract : BinaryOpcode
    {
        protected override string Name { get { return "sub"; } }
        public override ByteCode Code { get { return ByteCode.SUB; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.Subtract(Operands);
        }
    }

    
    public class OpcodeMathMultiply : BinaryOpcode
    {
        protected override string Name { get { return "mult"; } }
        public override ByteCode Code { get { return ByteCode.MULT; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.Multiply(Operands);
        }
    }

    
    public class OpcodeMathDivide : BinaryOpcode
    {
        protected override string Name { get { return "div"; } }
        public override ByteCode Code { get { return ByteCode.DIV; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.Divide(Operands);
        }
    }

    
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

    
    public class OpcodeLogicToBool : Opcode
    {
        protected override string Name { get { return "bool"; } }
        public override ByteCode Code { get { return ByteCode.BOOL; } }

        public override void Execute(ICpu cpu)
        {
            object value = cpu.PopValue();
            // Convert to bool instead of cast in case the identifier is stored
            // as an encapsulated BooleanValue, preventing an unboxing collision.
            bool result = Convert.ToBoolean(value);
            cpu.PushStack(result);
        }
    }

    
    public class OpcodeLogicNot : Opcode
    {
        protected override string Name { get { return "not"; } }
        public override ByteCode Code { get { return ByteCode.NOT; } }

        public override void Execute(ICpu cpu)
        {
            object value = cpu.PopValue();
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
                throw new KOSCastException(value.GetType(), typeof(bool));
            }
            cpu.PushStack(result);
        }
    }

    
    public class OpcodeLogicAnd : Opcode
    {
        protected override string Name { get { return "and"; } }
        public override ByteCode Code { get { return ByteCode.AND; } }

        public override void Execute(ICpu cpu)
        {
            bool argument2 = Convert.ToBoolean(cpu.PopValue());
            bool argument1 = Convert.ToBoolean(cpu.PopValue());
            object result = argument1 & argument2;
            cpu.PushStack(result);
        }
    }

    
    public class OpcodeLogicOr : Opcode
    {
        protected override string Name { get { return "or"; } }
        public override ByteCode Code { get { return ByteCode.OR; } }

        public override void Execute(ICpu cpu)
        {
            bool argument2 = Convert.ToBoolean(cpu.PopValue());
            bool argument1 = Convert.ToBoolean(cpu.PopValue());
            object result = argument1 | argument2;
            cpu.PushStack(result);
        }
    }

    #endregion

    #region Call

    
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
            object functionPointer;
            object delegateReturn = null;

            if (Direct)
            {
                functionPointer = cpu.GetValue(Destination);
                if (functionPointer == null)
                    throw new KOSException("Attempt to call function failed - Value of function pointer for " + Destination + " is null.");
            }
            else // for indirect calls, dig down to find what's underneath the argument list in the stack and use that:
            {
                bool foundBottom = false;
                int digDepth;
                int argsCount = 0;
                for (digDepth = 0; (! foundBottom) && digDepth < cpu.GetStackSize() ; ++digDepth)
                {
                    object arg = cpu.PeekValue(digDepth);
                    if (arg != null && arg.GetType() == ArgMarkerType)
                        foundBottom = true;
                    else
                        ++argsCount;
                }
                functionPointer = cpu.PeekValue(digDepth);
                if (! ( functionPointer is Delegate))
                {
                    // Indirect calls are meant to be delegates.  If they are not, then that means the
                    // function parentheses were put on by the user when they weren't required.  Just dig
                    // through the stack to the result of the getMember and skip the rest of the execute logic

                    // If args were passed to a non-method, then clean them off the stack, and complain:
                    if (argsCount>0)
                    {
                        for (int i=1 ; i<=argsCount; ++i)
                            cpu.PopValue();
                        throw new KOSArgumentMismatchException(
                            0, argsCount, "\n(In fact in this case the parentheses are entirely optional)");
                    }
                    cpu.PopValue(); // pop the ArgMarkerString too.
                    return;
                }
            }
            
            // If it's a string it might not really be a built-in, it might still be a user func.
            // Detect whether it's built-in, and if it's not, then convert it into the equivalent
            // user func call by making it be an integer instruction pointer instead:
            if (functionPointer is string)
            {
                string functionName = functionPointer as string;
                if (functionName.EndsWith("()"))
                    functionName = functionName.Substring(0, functionName.Length - 2);
                if (!(cpu.BuiltInExists(functionName)))
                {
                    // It is not a built-in, so instead get its value as a user function pointer variable, despite 
                    // the fact that it's being called AS IF it was direct.
                    if (!functionName.EndsWith("*")) functionName = functionName + "*";
                    if (!functionName.StartsWith("$")) functionName = "$" + functionName;
                    functionPointer = cpu.GetValue(functionName);
                }
            }
            IUserDelegate userDelegate = functionPointer as IUserDelegate;
            if (userDelegate != null)
                functionPointer = userDelegate.EntryPoint;

            // Convert to int instead of cast in case the identifier is stored
            // as an encapsulated ScalarValue, preventing an unboxing collision.
            if (functionPointer is int || functionPointer is ScalarValue)
            {
                ReverseStackArgs(cpu);
                int currentPointer = cpu.InstructionPointer;
                DeltaInstructionPointer = Convert.ToInt32(functionPointer) - currentPointer;
                var contextRecord = new SubroutineContext(currentPointer+1);
                cpu.PushAboveStack(contextRecord);
                if (userDelegate != null)
                {
                    cpu.AssertValidDelegateCall(userDelegate);
                    // Reverse-push the closure's scope record, just after the function return context got put on the stack.
                    for (int i = userDelegate.Closure.Count - 1 ; i >= 0 ; --i)
                        cpu.PushAboveStack(userDelegate.Closure[i]);
                }
            }
            else if (functionPointer is string)
            {
                // Built-ins don't need to dereference the stack values because they
                // don't leave the scope - they're not implemented that way.  But later we
                // might want to change that.
                var name = functionPointer as string;
                string functionName = name;
                if (functionName.EndsWith("()"))
                    functionName = functionName.Substring(0, functionName.Length - 2);
                cpu.CallBuiltinFunction(functionName);
            }
            else if (functionPointer is Delegate)
            {
                delegateReturn = ExecuteDelegate(cpu, (Delegate)functionPointer);
            }
            else
            {
                // This is one of those "the user had better NEVER see this error" sorts of messages that's here to keep us in check:
                throw new Exception(
                    string.Format("kOS internal error: OpcodeCall calling a function described using {0} which is of type {1} and kOS doesn't know how to call that.", functionPointer, functionPointer.GetType().Name)
                    );
            }

            if (! Direct)
            {
                cpu.PopValue(); // consume function name, branch index, or delegate
            }
            if (functionPointer is Delegate)
            {
                cpu.PushStack(delegateReturn); // And now leave the return value on the stack to be read.
            }
        }
        
        /// <summary>
        /// Call this when executing a delegate function whose delegate object was stored on
        /// the stack underneath the arguments.  The code here is using reflection and complex
        /// enough that it needed to be separated from the main Execute method.
        /// </summary>
        /// <param name="cpu">the cpu this opcode is being called on</param>
        /// <param name="dlg">the delegate object this opcode is being called for.</param>
        /// <returns>whatever object the delegate method returned</returns>
        public static object ExecuteDelegate(ICpu cpu, Delegate dlg)
        {
            MethodInfo methInfo = dlg.Method;
            ParameterInfo[] paramArray = methInfo.GetParameters();
            var args = new List<object>();
            
            // Iterating over parameter signature backward because stack:
            for (int i = paramArray.Length - 1 ; i >= 0 ; --i)
            {
                object arg = cpu.PopValue();
                if (arg != null && arg.GetType() == ArgMarkerType)
                    throw new KOSArgumentMismatchException(paramArray.Length, paramArray.Length - (i+1));
                Type argType = arg.GetType();
                ParameterInfo paramInfo = paramArray[i];
                Type paramType = paramInfo.ParameterType;
                
                // Parameter type-safe checking:
                bool inheritable = paramType.IsAssignableFrom(argType);
                if (! inheritable)
                {
                    bool castError = false;
                    // If it's not directly assignable to the expected type, maybe it's "castable" to it:
                    try
                    {
                        arg = Convert.ChangeType(arg, Type.GetTypeCode(paramType));
                    }
                    catch (InvalidCastException)
                    {
                        throw new KOSCastException(argType, paramType);
                    }
                    catch (FormatException) {
                        castError = true;
                    }
                    if (castError) {
                        throw new Exception(string.Format("Argument {0}({1}) to method {2} should be {3} instead of {4}.", (paramArray.Length - i), arg, methInfo.Name, paramType.Name, argType));
                    }
                }
                
                args.Add(arg);
            }
            // Consume the bottom marker under the args, which had better be
            // immediately under the args we just popped, or the count was off:
            bool foundArgMarker = false;
            int numExtraArgs = 0;
            while (cpu.GetStackSize() > 0 && !foundArgMarker)
            {
                object marker = cpu.PopValue();
                if (marker != null && marker.GetType() == ArgMarkerType)
                    foundArgMarker = true;
                else
                    ++numExtraArgs;
            }
            if (numExtraArgs > 0)
                throw new KOSArgumentMismatchException(paramArray.Length, paramArray.Length + numExtraArgs);

            args.Reverse(); // Put back in normal order instead of stack order.
            
            // Dialog.DynamicInvoke expects a null, rather than an array of zero length, when
            // there are no arguments to pass:
            object[] argArray = (args.Count>0) ? args.ToArray() : null;

            try
            {
                // I could find no documentation on what DynamicInvoke returns when the delegate
                // is a function returning void.  Does it return a null?  I don't know.  So to avoid the
                // problem, I split this into these two cases:
                if (methInfo.ReturnType == typeof(void))
                {
                    dlg.DynamicInvoke(argArray);
                    return null; // So that the compiler building the opcodes for a function call statement doesn't
                                 // have to know the function prototype to decide whether or
                                 // not it needs to pop a value from the stack for the return value.  By adding this,
                                 // it can unconditionally assume there will be exactly 1 value left behind on the stack
                                 // regardless of what function it was that was being called.
                }
                // Convert a primitive return type to a structure.  This is done in the opcode, since
                // the opcode calls the deligate directly and cannot be (quickly) intercepted
                return Structure.FromPrimitive(dlg.DynamicInvoke(argArray));
            }
            catch (TargetInvocationException e)
            {
                // Annoyingly, calling DynamicInvoke on a delegate wraps any exceptions the delegate throws inside
                // this TargetInvocationException, which hides them from the kOS user unless we do this:
                if (e.InnerException != null)
                    throw e.InnerException;
                throw;
            }
        }
        
        /// <summary>
        /// Take the topmost arguments down to the ARG_MARKER_STRING, pop them off, and then
        /// put them back again in reversed order so a function can read them in normal order.
        /// </summary>
        public void ReverseStackArgs(ICpu cpu)
        {
            List<object> args = new List<object>();
            object arg = cpu.PopValue();
            while (cpu.GetStackSize() > 0 && arg.GetType() != ArgMarkerType)
            {
                args.Add(arg);

                // It's important to dereference with PopValue, not using PopStack, because the function
                // being called might not even be able to see the variable in scope anyway.
                // In other words, if calling a function like so:
                //     declare foo to 3.
                //     myfunc(foo).
                // The code inside myfunc needs to see that as being identical to just saying:
                //     myfunc(3).
                // It has to be unaware of the fact that the name of the argument was 'foo'.  It just needs to
                // see the contents that were inside foo.
                arg = cpu.PopValue();
            }
            // Push the arg marker back on again.
            cpu.PushStack(new KOSArgMarkerType());
            // Push the arguments back on again, which will invert their order:
            foreach (object item in args)
                cpu.PushStack(item);
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Name, Destination);
        }
    }
    
    /// <summary>
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
            object returnVal = cpu.PopValue();

            // Now dig down through the stack until the argbottom is found.
            // anything still leftover above that should be unread parameters we
            // should throw away:
            object shouldBeArgMarker = 0; // just a temp to force the loop to execute at least once.
            while (shouldBeArgMarker == null || (shouldBeArgMarker.GetType() != OpcodeCall.ArgMarkerType))
            {
                if (cpu.GetStackSize() <= 0)
                {
                    throw new KOSArgumentMismatchException(
                        string.Format("Something is wrong with the stack - no arg bottom mark when doing a return.  This is an internal problem with kOS")
                    );
                }
                shouldBeArgMarker = cpu.PopStack();
            }
            
            cpu.PushStack(returnVal);

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
            VariableScope peeked = cpu.PeekRaw(-1, out okay) as VariableScope;
            while (okay && peeked != null && peeked.IsClosure)
            {
                cpu.PopAboveStack(1);
                peeked = cpu.PeekRaw(-1, out okay) as VariableScope;
            }
            object shouldBeContextRecord = cpu.PopAboveStack(1);
            if ( !(shouldBeContextRecord is SubroutineContext) )
            {
                // This should never happen with any user code:
                throw new Exception( "kOS internal error: Stack misalignment detected when returning from routine.");
            }
            
            var contextRecord = shouldBeContextRecord as SubroutineContext;
            
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
            cpu.PushStack(Argument);
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

    
    public class OpcodePop : Opcode
    {
        protected override string Name { get { return "pop"; } }
        public override ByteCode Code { get { return ByteCode.POP; } }

        public override void Execute(ICpu cpu)
        {
            // Even though this value is being thrown away it's still important to attempt to
            // process it (with cpu.PopValue()) rather than ignore it (with cpu.PopStack()).  This
            // is just in case it's an unknown variable name in need of an error message
            // to the user.  Detecting that a variable name is unknown occurs during the popping
            // of the value, not the pushing of it.  (This is necessary because SET and DECLARE
            // statements have to be allowed to push undefined variable references onto the stack
            // for new variables that they are going to create.)

            cpu.PopValue();
        }
    }

    /// <summary>
    /// Asserts that the next thing on the stack is the argument bottom marker.
    /// If it's not the argument bottom, it throws an error.
    /// This does NOT pop the value from the stack - it merely peeks at the stack top.
    /// The actual popping of the arg bottom value comes later when doing a return,
    /// or a program bottom exit.
    /// </summary>
    public class OpcodeArgBottom : Opcode
    {
        protected override string Name { get { return "argbottom"; } }
        public override ByteCode Code { get { return ByteCode.ARGBOTTOM; } }

        public override void Execute(ICpu cpu)
        {
            bool worked;
            object shouldBeArgMarker = cpu.PeekRaw(0,out worked);

            if ( !worked || (shouldBeArgMarker == null) || (shouldBeArgMarker.GetType() != OpcodeCall.ArgMarkerType) )
            {
                throw new KOSArgumentMismatchException("Called with too many arguments.");
            }
        }
    }

    /// <summary>
    /// Tests whether or not the next thing on the stack is the argument bottom marker.
    /// It pushes a true on top if it is, or false if it is not.  In either case it does
    /// NOT consume the arg bottom marker, but just peeks for it.
    /// </summary>
    public class OpcodeTestArgBottom : Opcode
    {
        protected override string Name { get { return "testargbottom"; } }
        public override ByteCode Code { get { return ByteCode.TESTARGBOTTOM; } }

        public override void Execute(ICpu cpu)
        {
            bool worked;
            object shouldBeArgMarker = cpu.PeekRaw(0,out worked);

            if ( !worked || (shouldBeArgMarker == null) || (shouldBeArgMarker.GetType() != OpcodeCall.ArgMarkerType) )
            {
                cpu.PushStack(false);
            }
            else
            {
                cpu.PushStack(true);
            }
        }
    }
    
    public class OpcodeDup : Opcode
    {
        protected override string Name { get { return "dup"; } }
        public override ByteCode Code { get { return ByteCode.DUP; } }

        public override void Execute(ICpu cpu)
        {
            object value = cpu.PopStack();
            cpu.PushStack(value);
            cpu.PushStack(value);
        }
    }

    
    public class OpcodeSwap : Opcode
    {
        protected override string Name { get { return "swap"; } }
        public override ByteCode Code { get { return ByteCode.SWAP; } }

        public override void Execute(ICpu cpu)
        {
            object value1 = cpu.PopStack();
            object value2 = cpu.PopStack();
            cpu.PushStack(value1);
            cpu.PushStack(value2);
        }
    }
    
    /// <summary>
    /// Replaces the topmost thing on the stack with its evaluated,
    /// fully dereferenced version.  For example, if the variable
    /// foo contains value 4, and the top of the stack is the
    /// identifier name "$foo", then this will replace the "$foo"
    /// with a 4.
    /// </summary>
    public class OpcodeEval : Opcode
    {
        protected override string Name { get { return "eval"; } }
        public override ByteCode Code { get { return ByteCode.EVAL; } }

        public override void Execute(ICpu cpu)
        {
            cpu.PushStack(cpu.PopValue());
        }
    }

    /// <summary>
    /// Pushes a new variable namespace scope (for example, when a "{" is encountered
    /// in a block-scoping language like C++ or Java or C#.)
    /// From now on any local variables created will be made in this new
    /// namespace.
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
            cpu.PushAboveStack(new VariableScope(ScopeId,ParentScopeId));
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
            cpuObj.PopAboveStack(levels);
        }

        public override string ToString()
        {
            return Name + " " + NumLevels;
        }
  
    }
    
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
            cpu.PushStack(pushMe);
        }

        public override string ToString()
        {
            return Name + " " + EntryPoint.ToString();
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

    
    public class OpcodeAddTrigger : Opcode
    {
        protected override string Name { get { return "addtrigger"; } }
        public override ByteCode Code { get { return ByteCode.ADDTRIGGER; } }

        public override void Execute(ICpu cpu)
        {
            var functionPointer = (int)cpu.PopValue();
            cpu.AddTrigger(functionPointer);
        }

        public override string ToString()
        {
            return Name;
        }
    }

    
    public class OpcodeRemoveTrigger : Opcode
    {
        protected override string Name { get { return "removetrigger"; } }
        public override ByteCode Code { get { return ByteCode.REMOVETRIGGER; } }

        public override void Execute(ICpu cpu)
        {
            var functionPointer = (int)cpu.PopValue();
            cpu.RemoveTrigger(functionPointer);
        }
    }

    
    public class OpcodeWait : Opcode
    {
        protected override string Name { get { return "wait"; } }
        public override ByteCode Code { get { return ByteCode.WAIT; } }

        public override void Execute(ICpu cpu)
        {
            object arg = cpu.PopValue();
            cpu.StartWait(Convert.ToDouble(arg));
        }
    }

    
    public class OpcodeEndWait : Opcode
    {
        protected override string Name { get { return "endwait"; } }
        public override ByteCode Code { get { return ByteCode.ENDWAIT; } }

        public override void Execute(ICpu cpu)
        {
            cpu.EndWait();
        }
    }

    #endregion

}

