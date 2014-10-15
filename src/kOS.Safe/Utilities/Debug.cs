using System.Runtime.Serialization;

namespace kOS.Safe.Utilities
{
    public static class Debug
    {
        static Debug ()
        {
            IDGenerator = new ObjectIDGenerator();
        }

        public static ILogger Logger { get; set; }
        public static ObjectIDGenerator IDGenerator { get; set; }
    }
}
