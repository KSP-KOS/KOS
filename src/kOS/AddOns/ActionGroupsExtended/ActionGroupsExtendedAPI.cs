using System.Reflection;

namespace kOS.AddOns.ActionGroupsExtended
{
    public class ActionGroupsExtendedAPI
    {
        private const BindingFlags BINDINGS = BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static;
        private static ActionGroupsExtendedAPI instance;

        private readonly System.Type calledType;

        public static ActionGroupsExtendedAPI Instance
        {
            get { return instance ?? (instance = new ActionGroupsExtendedAPI()); }
        }

        public ActionGroupsExtendedAPI()
        {
            calledType = System.Type.GetType("ActionGroupsExtended.AGExtExternal, AGExt");
        }

        public bool Installed() 
        {
            try //try-catch is required as the below code returns a NullRef if AGX is not present.
            {
                return (bool)calledType.InvokeMember("AGXInstalled", BINDINGS, null, null, null);
            }
            catch
            {
                return false;
            }
        }

        public bool ActivateGroup(uint flightID, int group, bool forceDir) //activate/deactivate an action group
        {
            var args = new System.Object[] {flightID, group, forceDir};
            return (bool)calledType.InvokeMember("AGX2VslActivateGroup", BINDINGS, null, null, args);
        }

        public bool GetGroupState(uint flightID, int group)
        {
            var args = new System.Object[] {flightID, group};
            return (bool)calledType.InvokeMember("AGX2VslGroupState", BINDINGS, null, null, args);
        }
    }
}
