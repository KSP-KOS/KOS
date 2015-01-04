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

        /// <summary>
        /// Tests for the presence of AGX
        /// </summary>
        /// <returns>Is AGX installed?</returns>
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

        /// <summary>
        /// Activates one of the extended action group extended.
        /// </summary>
        /// <param name="vessel">The vessel that will catch the action</param>
        /// <param name="group">A ActionGroup number from 11-251</param>
        /// <param name="forceDir">The value you want to set the action group to</param>
        /// <returns>Did the action execute? If yes, return True</returns>
        public bool ActivateGroup(Vessel vessel, int group, bool forceDir) //activate/deactivate an action group
        {
            var args = new System.Object[] {vessel.rootPart.flightID, group, forceDir};
            return (bool)calledType.InvokeMember("AGX2VslActivateGroup", BINDINGS, null, null, args);
        }

        /// <summary>
        /// Gets the state of an action group
        /// </summary>
        /// <param name="vessel">The vessel that will catch the action</param>
        /// <param name="group">A ActionGroup number from 11-251</param>
        /// <returns>The Action group state</returns>
        public bool GetGroupState(Vessel vessel, int group)
        {
            var args = new System.Object[] {vessel.rootPart.flightID, group};
            return (bool)calledType.InvokeMember("AGX2VslGroupState", BINDINGS, null, null, args);
        }
    }
}
