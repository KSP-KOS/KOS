namespace kOS.Safe.Function.Persistence
{
    [Function("exists")]
    public class FunctionExists : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string fileName = PopValueAssert(shared, true).ToString();
            AssertArgBottomAndConsume(shared);

            ReturnValue = shared.VolumeMgr.CurrentVolume.Exists(fileName);
        }
    }
}