using System;

namespace kOS.Safe.Serialization
{
    /// <summary>
    /// Classes implementing this interface can dump their data to a dictionary.
    ///
    /// Dumps should only contain primitives, strings, lists and other Dumps.
    /// SerializationMgr, for convenience, will handle any encapsulation types that implement
    /// PrimitiveStructure when serializing.
    ///
    /// Types implementing IDumper should make sure that proper encapsulation types are created in LoadDump whenever
    /// necessary.
    /// </summary>
    public interface IDumper
    {
        Dump Dump();
        void LoadDump(Dump dump);

        // Here is a limitation of C#'s inheritence model.  It would be good to force all
        // implementers of IDumper (except abstract classes, as they cannot be "instanced")
        // to have this method - but it's a static method, so we can't.  All IDumper's should implement
        // this static method.  We may make some ad-hoc reflection walk to enforce this rule since
        // the compiler cannot:
        //
        //   /// <summary> Creates an instance of <whatever_this_class_is_called> from the Dump
        //   /// passed in.  If this object cares about needing the reference to Shared, it can use
        //   /// the parameter for that, or it is free to throw that away if it doesn't care.
        //   /// This method should essentially both construct the object and populate it with
        //   /// the LoadDump() method above.
        //   /// </summary>
        //   static  <whatever_this_class_is_called> CreateFromDump(SafeSharedObjects shared, Dump d)
        //
    }
}
