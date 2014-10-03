using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Execution;

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
        // and comapre that to this list: 
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
        ADDTRIGGER     = 0x52,
        REMOVETRIGGER  = 0x53,
        WAIT           = 0x54,
        ENDWAIT        = 0x55,
        
        // Augmented bogus placeholder versions of the normal
        // opcodes: These only exist in the program temporarily
        // or in the ML file but never actually can be executed.
        
        PUSHRELOCATELATER = 0xce,
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
    [System.AttributeUsage(System.AttributeTargets.Property, Inherited=true)]
    public class MLField : System.Attribute
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
        public PropertyInfo propertyInfo {get;set;}
        public bool NeedReindex {get;set;}
        
        public MLArgInfo(PropertyInfo pInfo, bool needReindex)
        {
            propertyInfo = pInfo;
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

        // SHOUD-BE-STATIC MEMBERS:
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
        // inheritences.  It doesn't know how to store overrides at the static level where there's just
        // one instance per subclass definition.   It only knows how to override dynamic members.  Because of
        // this the compiler will call it an error to try to make a member be both abstract and static.
        //
        // Any place you see a member which is marked with a SHOULD-BE-STATIC comment, please do NOT
        // try to store separate values per instance into it.  Treat it like a static member, where to
        // change its value you should make a new derived class for the new value.
        
        public abstract /*SHOULD-BE-STATIC*/ string Name { get; }
        
        /// <summary>
        /// The short coded value that indicates what kind of instruction this is.
        /// Hopefully one byte will be enough, and we won't have more than 256 different opcodes.
        /// </summary> 
        public abstract /*SHOULD-BE-STATIC*/ ByteCode Code { get; }
        
        // A mapping of CodeName to Opcode type, built at initialization time:        
        private static Dictionary<ByteCode,Type> mapCodeToType = null; // will init this later.

        // A mapping of Name to Opcode type,  built at initialization time:
        private static Dictionary<string,Type> mapNameToType = null; // will init this later.
        
        // A table describing the arguments in machine language form that each opcode needs.
        // This is populated by using Reflection to scan all the Opcodes for their MLField Attributes.
        private static Dictionary<Type,List<MLArgInfo>> mapOpcodeToArgs = null;
                
        private static string forceDefaultConstructorMsg =
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

        // TODO - The following should probably be properties instead of Fields, since
        //        they are public:
        public int Id { get { return id; } }
        public int DeltaInstructionPointer = 1;
        public int MLIndex = 0; // index into the Machine Language code file for the COMPILE command.
        public virtual string Label {get{return label;} set {label = value;} }
        private string label = "";
        public virtual string DestinationLabel {get;set;}
        public string SourceName;

        public short SourceLine { get; set; } // line number in the source code that this was compiled from.
        public short SourceColumn { get; set; }  // column number of the token nearest the cause of this Opcode.
        
        public virtual void Execute(ICpu cpu)
        {
        }

        public override string ToString()
        {
            return Name;
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
                        //TODO: CHRIS FIX IT!
                     //   UnityEngine.Debug.Log( String.Format(forceDefaultConstructorMsg, opType.Name) );
                     //   kOS.Utilities.Utils.AddNagMessage( kOS.Utilities.NagType.NAGFOREVER, "ERROR IN OPCODE DEFINITION " + opType.Name );
                        return;
                    }
                    
                    List<MLArgInfo> argsInfo = new List<MLArgInfo>();

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
                            ByteCode opCodeName = (ByteCode) pInfo.GetValue(dummyInstance, null);
                            mapCodeToType.Add(opCodeName, opType);                                                 
                        }
                        else if (pInfo.Name == "Name")
                        {
                            // Add to the map from Name to Opcode type:
                            string opName = (string) pInfo.GetValue(dummyInstance, null);
                            mapNameToType.Add(opName, opType);
                        }
                        else
                        {
                            // See if this property has an MLFields attribute somewhere on it.
                            foreach (object attrib in attribs)
                            {
                                if (attrib is MLField)
                                {
                                    argsInfo.Add(new MLArgInfo(pInfo, ((MLField)attrib).NeedReindex));
                                    break;
                                }
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
        public static int MLFieldComparator(MLArgInfo a1, MLArgInfo a2)
        {
            // All the following is doing is just comparing p1 and p2's
            // MLField.Ordering fields to decide the sort order.
            //
            // Reflection: A good way to make a simple idea look messier than it really is.
            //
            List<Attribute> attributes1 = new List<Attribute>(a1.propertyInfo.GetCustomAttributes(true) as Attribute[]);
            List<Attribute> attributes2 = new List<Attribute>(a2.propertyInfo.GetCustomAttributes(true) as Attribute[]);
            MLField f1 = (MLField) attributes1.First(delegate (Attribute a) {return a is MLField;} );
            MLField f2 = (MLField) attributes2.First(delegate (Attribute a) {return a is MLField;} );
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
        public List<MLArgInfo> GetArgumentDefs()
        {
            return mapOpcodeToArgs[this.GetType()];
        }
    }
        
    public abstract class BinaryOpcode : Opcode
    {
        protected object Argument1 { get; set; }
        protected object Argument2 { get; set; }

        public override void Execute(ICpu cpu)
        {
            Argument2 = cpu.PopValue();
            Argument1 = cpu.PopValue();

            // convert floats to doubles
            if (Argument1 is float) Argument1 = Convert.ToDouble(Argument1);
            if (Argument2 is float) Argument2 = Convert.ToDouble(Argument2);

            Calculator calc = Calculator.GetCalculator(Argument1, Argument2);
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

    
    public class OpcodeStore : Opcode
    {
        public override string Name { get { return "store"; } }
        public override ByteCode Code { get { return ByteCode.STORE; } }

        public override void Execute(ICpu cpu)
        {
            object value = cpu.PopValue();
            var identifier = (string)cpu.PopStack();
            cpu.SetValue(identifier, value);
        }
    }

    
    public class OpcodeUnset : Opcode
    {
        public override string Name { get { return "unset"; } }
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
                cpu.RemoveAllVariables();
            }
        }
    }

    
    public class OpcodeGetMember : Opcode
    {
        public override string Name { get { return "getmember"; } }
        public override ByteCode Code { get { return ByteCode.GETMEMBER; } }

        public override void Execute(ICpu cpu)
        {
            string suffixName = cpu.PopStack().ToString().ToUpper();
            object popValue = cpu.PopValue();

            var specialValue = popValue as ISuffixed;
            if (specialValue == null)
            {
                throw new Exception(string.Format("Values of type {0} cannot have suffixes", popValue.GetType()));
            }

            object value = specialValue.GetSuffix(suffixName);
            if (value != null)
            {
                cpu.PushStack(value);
            }
            else
            {
                throw new Exception(string.Format("Suffix {0} not found on object", suffixName));
            }
        }
    }

    
    public class OpcodeSetMember : Opcode
    {
        public override string Name { get { return "setmember"; } }
        public override ByteCode Code { get { return ByteCode.SETMEMBER; } }

        public override void Execute(ICpu cpu)
        {
            object value = cpu.PopValue();
            string suffixName = cpu.PopStack().ToString().ToUpper();
            object popValue = cpu.PopValue();

            var specialValue = popValue as ISuffixed;
            if (specialValue == null)
            {
                throw new Exception(string.Format("Values of type {0} cannot have suffixes", popValue.GetType()));
            }

            if (!specialValue.SetSuffix(suffixName, value))
            {
                throw new Exception(string.Format("Suffix {0} not found on object", suffixName));
            }
        }
    }

    
    public class OpcodeGetIndex : Opcode
    {
        public override string Name { get { return "getindex"; } }
        public override ByteCode Code { get { return ByteCode.GETINDEX; } }

        public override void Execute(ICpu cpu)
        {
            object index = cpu.PopValue();
            if (index is double || index is float)
            {
                index = Convert.ToInt32(index);  // allow expressions like (1.0) to be indexes
            }
            object list = cpu.PopValue();

            if (!(list is IIndexable)) throw new Exception(string.Format("Can't iterate on an object of type {0}", list.GetType()));
            if (!(index is int)) throw new Exception("The index must be an integer number");

            object value = ((IIndexable)list).GetIndex((int)index);
            cpu.PushStack(value);
        }
    }

    
    public class OpcodeSetIndex : Opcode
    {
        public override string Name { get { return "setindex"; } }
        public override ByteCode Code { get { return ByteCode.SETINDEX; } }

        public override void Execute(ICpu cpu)
        {
            object value = cpu.PopValue();
            object index = cpu.PopValue();
            object list = cpu.PopValue();
            if (index is double || index is float)
            {
                index = Convert.ToInt32(index);  // allow expressions like (1.0) to be indexes
            }
            if (!(list is IIndexable)) throw new Exception(string.Format("Can't iterate on an object of type {0}", list.GetType()));
            if (!(index is int)) throw new Exception("The index must be an integer number");

            if (value != null)
            {
                ((IIndexable)list).SetIndex((int)index, value);
            }
        }
    }

    
    public class OpcodeEOF : Opcode
    {
        public override string Name { get { return "EOF"; } }
        public override ByteCode Code { get { return ByteCode.EOF; } }
    }

    
    public class OpcodeEOP : Opcode
    {
        public override string Name { get { return "EOP"; } }
        public override ByteCode Code { get { return ByteCode.EOP; } }
    }

    
    public class OpcodeNOP : Opcode
    {
        public override string Name { get { return "nop"; } }
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
        public override string Name { get { return "not an opcode in the program."; } }
        public override ByteCode Code { get { return ByteCode.BOGUS; } }
    }
    
    #endregion

    #region Branch

    
    public abstract class BranchOpcode : Opcode
    {
        // This is identical to the base DestinationLabel, except that it has
        // the MLFIeld attached to it:
        [MLField(1,true)]
        public override string DestinationLabel {get;set;}
        
        public int Distance { get; set; }

        public override void PopulateFromMLFields(List<object> fields)
        {
            // Expect fields in the same order as the [MLField] properties of this class:
            if (fields == null || fields.Count<1)
                throw new Exception("Saved field in ML file for BranchOpcode seems to be missing.  Version mismatch?");
            DestinationLabel = (string)(fields[0]);  // should throw exception if not an integer-ish type.
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Name, Distance);
        }
    }

    
    public class OpcodeBranchIfFalse : BranchOpcode
    {
        public override string Name { get { return "br.false"; } }
        public override ByteCode Code { get { return ByteCode.BRANCHFALSE; } } // branch if zero - a longstanding name for this op in many machine codes.

        public override void Execute(ICpu cpu)
        {
            bool condition = Convert.ToBoolean(cpu.PopValue());
            DeltaInstructionPointer = !condition ? Distance : 1;
        }
    }

    
    public class OpcodeBranchJump : BranchOpcode
    {
        public override string Name { get { return "jump"; } }
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
        public string UpcomingLabel {get;set;}
        
        public override string Name { get { return "OpcodeLabelReset"; } }
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
        public override string Name { get { return "gt"; } }
        public override ByteCode Code { get { return ByteCode.GT; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.GreaterThan(Argument1, Argument2);
        }
    }

    
    public class OpcodeCompareLT : BinaryOpcode
    {
        public override string Name { get { return "lt"; } }
        public override ByteCode Code { get { return ByteCode.LT; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.LessThan(Argument1, Argument2);
        }
    }

    
    public class OpcodeCompareGTE : BinaryOpcode
    {
        public override string Name { get { return "gte"; } }
        public override ByteCode Code { get { return ByteCode.GTE; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.GreaterThanEqual(Argument1, Argument2);
        }
    }

    
    public class OpcodeCompareLTE : BinaryOpcode
    {
        public override string Name { get { return "lte"; } }
        public override ByteCode Code { get { return ByteCode.LTE; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.LessThanEqual(Argument1, Argument2);
        }
    }

    
    public class OpcodeCompareNE : BinaryOpcode
    {
        public override string Name { get { return "ne"; } }
        public override ByteCode Code { get { return ByteCode.NE; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.NotEqual(Argument1, Argument2);
        }
    }
    
    
    public class OpcodeCompareEqual : BinaryOpcode
    {
        public override string Name { get { return "eq"; } }
        public override ByteCode Code { get { return ByteCode.EQ; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.Equal(Argument1, Argument2);
        }
    }

    #endregion

    #region Math
        
    
    public class OpcodeMathNegate : Opcode
    {
        public override string Name { get { return "negate"; } }
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
                throw new ArgumentException(string.Format("Can't negate object {0} of type {1}", value, value.GetType()));

            cpu.PushStack(result);
        }
    }

    
    public class OpcodeMathAdd : BinaryOpcode
    {
        public override string Name { get { return "add"; } }
        public override ByteCode Code { get { return ByteCode.ADD; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            object result = calc.Add(Argument1, Argument2);
            // TODO: complete message
            if (result == null) throw new ArgumentException("Can't add ....");
            return result;
        }
    }

    
    public class OpcodeMathSubtract : BinaryOpcode
    {
        public override string Name { get { return "sub"; } }
        public override ByteCode Code { get { return ByteCode.SUB; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.Subtract(Argument1, Argument2);
        }
    }

    
    public class OpcodeMathMultiply : BinaryOpcode
    {
        public override string Name { get { return "mult"; } }
        public override ByteCode Code { get { return ByteCode.MULT; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.Multiply(Argument1, Argument2);
        }
    }

    
    public class OpcodeMathDivide : BinaryOpcode
    {
        public override string Name { get { return "div"; } }
        public override ByteCode Code { get { return ByteCode.DIV; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.Divide(Argument1, Argument2);
        }
    }

    
    public class OpcodeMathPower : BinaryOpcode
    {
        public override string Name { get { return "pow"; } }
        public override ByteCode Code { get { return ByteCode.POW; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.Power(Argument1, Argument2);
        }
    }

    #endregion

    #region Logic

    
    public class OpcodeLogicToBool : Opcode
    {
        public override string Name { get { return "bool"; } }
        public override ByteCode Code { get { return ByteCode.BOOL; } }

        public override void Execute(ICpu cpu)
        {
            object value = cpu.PopValue();
            bool result = Convert.ToBoolean(value);
            cpu.PushStack(result);
        }
    }

    
    public class OpcodeLogicNot : Opcode
    {
        public override string Name { get { return "not"; } }
        public override ByteCode Code { get { return ByteCode.NOT; } }

        public override void Execute(ICpu cpu)
        {
            object value = cpu.PopValue();
            object result;

            if (value is bool)
                result = !((bool)value);
            else if (value is int)
                result = Convert.ToBoolean(value) ? 0 : 1;
            else if ((value is double) || (value is float))
                result = Convert.ToBoolean(value) ? 0.0 : 1.0;
            else
                throw new ArgumentException(string.Format("Can't negate object {0} of type {1}", value, value.GetType()));

            cpu.PushStack(result);
        }
    }

    
    public class OpcodeLogicAnd : Opcode
    {
        public override string Name { get { return "and"; } }
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
        public override string Name { get { return "or"; } }
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

        public override string Name { get { return "call"; } }
        public override ByteCode Code { get { return ByteCode.CALL; } }
        
        public static string ArgMarkerString = "$<argstart>"; // guaranteed to not be a legal variable name.

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
                if (Destination is string && Destination.ToString() == indirectPlaceholder)
                    return false;
                else
                    return true;
            }
            set
            {
                if (value && !Direct)
                    Destination = "TODO"; // If it's indirect and you set it to Direct, you'll need to also give it a Destination.
                else if (!value && Direct)
                    Destination = indirectPlaceholder;
            }
        }

        private static string indirectPlaceholder = "<indirect>";  // Guaranteed not to be a possible real function name because of the "<" character.

        public OpcodeCall(object destination)
        {
            Destination = destination;
        }
        /// <summary>
        /// This variant of the constructor is just for machine language file read/write to use.
        /// </summary>
        protected OpcodeCall() { }
        
        public override void PopulateFromMLFields(List<object> fields)
        {
            // Expect fields in the same order as the [MLField] properties of this class:
            if (fields == null || fields.Count<1)
                throw new Exception("Saved field in ML file for OpcodeCall seems to be missing.  Version mismatch?");
            DestinationLabel = (string)fields[0];
            Destination = fields[1];
        }

        public override void Execute(ICpu cpu)
        {
            object functionPointer;
            object delegateReturn = null;

            if (Direct)
            {
                functionPointer = cpu.GetValue(Destination);
            }
            else // for indirect calls, dig down to find what's underneath the argument list in the stack and use that:
            {
                bool foundBottom = false;
                int digDepth = 0;
                for (digDepth = 0; (! foundBottom) && digDepth < cpu.GetStackSize() ; ++digDepth)
                {
                    object arg = cpu.PeekValue(digDepth);
                    if (arg is string && arg.ToString() == ArgMarkerString)
                        foundBottom = true;
                }
                functionPointer = cpu.PeekValue(digDepth);
            }


            if (functionPointer is int)
            {
                int currentPointer = cpu.InstructionPointer;
                DeltaInstructionPointer = (int)functionPointer - currentPointer;
                var contextRecord = new SubroutineContext(currentPointer+1);
                cpu.PushStack(contextRecord);
                cpu.MoveStackPointer(-1);
            }
            else if (functionPointer is string)
            {
                var name = functionPointer as string;
                if (name != null)
                {
                    string functionName = name;
                    functionName = functionName.Substring(0, functionName.Length - 2);
                    cpu.CallBuiltinFunction(functionName);
                }
            }
            else if (functionPointer is Delegate)
            {
                delegateReturn = ExecuteDelegate(cpu, (Delegate)functionPointer);
            }
            else
            {
                // This is one of those "the user had better NEVER see this error" sorts of messages that's here to keep us in check:
                throw new Exception("kOS internal error: OpcodeCall calling a function described using " + functionPointer.ToString() +
                                    " which is of type " + functionPointer.GetType().Name + " and kOS doesn't know how to call that.");
            }
            
            if (! Direct)
            {
                // Indirect calls have two more elements to consume from the stack.
                cpu.PopValue(); // consume arg bottom mark
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
        protected object ExecuteDelegate(ICpu cpu, Delegate dlg)
        {
            MethodInfo methInfo = dlg.Method;
            ParameterInfo[] paramArray = methInfo.GetParameters();
            List<object> args = new List<object>();
            
            // Iterating over parameter signature backward because stack:
            for (int i = paramArray.Length - 1 ; i >= 0 ; --i)
            {
                object arg = cpu.PopValue();
                Type argType = arg.GetType();
                ParameterInfo paramInfo = paramArray[i];
                Type paramType = paramInfo.ParameterType;
                
                // Parameter type-safe checking:
                bool inheritable = paramType.IsAssignableFrom(argType);
                if (! inheritable)
                {
                    // If it's not directly assignable to the expected type, maybe it's "castable" to it:
                    try
                    {
                        arg = Convert.ChangeType(arg, Type.GetTypeCode(paramType));
                    }
                    catch (InvalidCastException)
                    {
                        throw new Exception("Argument " + i + " to method " + methInfo.Name + " should be " + paramType.Name + " instead of " + argType + ".");
                    }
                }
                
                args.Add(arg);
            }
            args.Reverse(); // Put back in normal order instead of stack order.
            
            // Dialog.DynamicInvoke expects a null, rather than an array of zero length, when
            // there are no arguments to pass:
            object[] argArray = (args.Count>0) ? args.ToArray() : null;

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
            else
            {
                return dlg.DynamicInvoke(argArray);
            }
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Name, Destination);
        }
    }
    
    public class OpcodeReturn : Opcode
    {
        public override string Name { get { return "return"; } }
        public override ByteCode Code { get { return ByteCode.RETURN; } }

        public override void Execute(ICpu cpu)
        {
            cpu.MoveStackPointer(1);
            object shouldBeContextRecord = cpu.PopValue();
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
    }

    #endregion

    #region Stack

    
    public class OpcodePush : Opcode
    {
        [MLField(1,false)]
        public object Argument { get; set; }

        public override string Name { get { return "push"; } }
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
        [MLField(1,true)]
        public override string DestinationLabel {get;set;}
        
        public override string Name { get { return "PushRelocateLater"; } }
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
        public override string Name { get { return "pop"; } }
        public override ByteCode Code { get { return ByteCode.POP; } }

        public override void Execute(ICpu cpu)
        {
            cpu.PopStack();
        }
    }

    
    public class OpcodeDup : Opcode
    {
        public override string Name { get { return "dup"; } }
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
        public override string Name { get { return "swap"; } }
        public override ByteCode Code { get { return ByteCode.SWAP; } }

        public override void Execute(ICpu cpu)
        {
            object value1 = cpu.PopStack();
            object value2 = cpu.PopStack();
            cpu.PushStack(value1);
            cpu.PushStack(value2);
        }
    }

    #endregion

    #region Wait / Trigger

    
    public class OpcodeAddTrigger : Opcode
    {
        [MLField(1,false)]
        public bool ShouldWait { get; set; }
        
        public override string Name { get { return "addtrigger"; } }
        public override ByteCode Code { get { return ByteCode.ADDTRIGGER; } }

        public OpcodeAddTrigger(bool shouldWait)
        {
            ShouldWait = shouldWait;
        }

        /// <summary>
        /// This variant of the constructor is just for ML save/load to use.
        /// </summary>
        protected OpcodeAddTrigger() { }

        public override void PopulateFromMLFields(List<object> fields)
        {
            // Expect fields in the same order as the [MLField] properties of this class:
            if (fields == null || fields.Count<1)
                throw new Exception("Saved field in ML file for OpcodeAddTrigger seems to be missing.  Version mismatch?");
            ShouldWait = (bool)(fields[0]); // should throw error if it's not a bool.
        }

        public override void Execute(ICpu cpu)
        {
            var functionPointer = (int)cpu.PopValue();
            cpu.AddTrigger(functionPointer);
            if (ShouldWait)
                cpu.StartWait(0);
        }

        public override string ToString()
        {
            return Name + " " + ShouldWait.ToString().ToLower();
        }
    }

    
    public class OpcodeRemoveTrigger : Opcode
    {
        public override string Name { get { return "removetrigger"; } }
        public override ByteCode Code { get { return ByteCode.REMOVETRIGGER; } }

        public override void Execute(ICpu cpu)
        {
            var functionPointer = (int)cpu.PopValue();
            cpu.RemoveTrigger(functionPointer);
        }
    }

    
    public class OpcodeWait : Opcode
    {
        public override string Name { get { return "wait"; } }
        public override ByteCode Code { get { return ByteCode.WAIT; } }

        public override void Execute(ICpu cpu)
        {
            object waitTime = cpu.PopValue();
            if (waitTime is double)
                cpu.StartWait((double)waitTime);
            else if (waitTime is int)
                cpu.StartWait((int)waitTime);
        }
    }

    
    public class OpcodeEndWait : Opcode
    {  
        public override string Name { get { return "endwait"; } }
        public override ByteCode Code { get { return ByteCode.ENDWAIT; } }

        public override void Execute(ICpu cpu)
        {
            cpu.EndWait();
        }
    }

    #endregion

}

