namespace kOS.Safe.Encapsulation
{
    public interface IIndexable
    {
        Structure GetIndex(Structure index);
        void SetIndex(Structure index, Structure value);

        /// <summary>
        /// This should redirect to GetIndex(Structure index), and is provided as 
        /// a convenient shorthand for GetIndex(Structure.FromPrimitive(someInt)),
        /// because of the large number of places in the code that were written to
        /// assume integer indeces:
        /// </summary>
        /// <param name="index"></param>
        /// <param name="failOkay">if failed to find a value return null instead of throwing an exception</param>
        /// <returns></returns>
        Structure GetIndex(int index, bool failOkay = false);

        /// <summary>
        /// This should redirect to SetIndex(Structure index, Structure value), and is provided as 
        /// a convenient shorthand for SetIndex(Structure.FromPrimitive(someInt), someValue)
        /// because of the large number of places in the code that were written to
        /// assume integer indeces:
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        void SetIndex(int index, Structure value);
    }
}
