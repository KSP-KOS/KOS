namespace kOS.AddOns.ActionGroupsExtended
{
    public class ActionGroupsExtendedAPI
    {
        private static ActionGroupsExtendedAPI instance;
        
        public static ActionGroupsExtendedAPI Instance
        {
            get { return instance ?? (instance = new ActionGroupsExtendedAPI()); }
        }
    }
}
