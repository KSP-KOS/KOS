using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace kOS.Communication
{
    public class kOSConnectivityParameters : GameParameters.CustomParameterNode
    {
        private static kOSConnectivityParameters instance;
        public static kOSConnectivityParameters Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = HighLogic.CurrentGame.Parameters.CustomParams<kOSConnectivityParameters>();
                }
                return instance;
            }
        }

        public const string SELECT_DIALOG_TEXT = "kOS has detected that there are one or more new connectivity " +
            "managers available, or that your selected manager is no longer available.  A connectivity manager " +
            "determines how kOS will interact with a communications network with regard to both inter-vessel and " +
            "vessel-kerbin communication and file transfer.  Please select a manager to use.";

        private List<string> availableConnectivityManagers;

        [GameParameters.CustomIntParameterUI("")]
        public int version = 0;

        [GameParameters.CustomStringParameterUI("")]
        public string knownHandlerList = "PermitAllConnectivityManager";

        [GameParameters.CustomStringParameterUI("CONNECTIVITY MANAGER", autoPersistance = false)]
        public string connectivityHandlerTitle = "";

        [GameParameters.CustomParameterUI("Selected", toolTip = "The currently slected manager.  The slider will progress\n" + 
                                                                "through the list of available managers below, in order.\n" + 
                                                                "The manager you select will determine how kOS will interact\n" + 
                                                                "with stock or mod communications networks.  See the communications\n" + 
                                                                "documentation for details on each manager.")]
        public string connectivityHandler = "StockConnectivityManager";

        [GameParameters.CustomStringParameterUI("Available managers", autoPersistance = false)]
        public string connectivityHandlerList = "connectivityHandlerList";

        public override GameParameters.GameMode GameMode
        {
            get
            {
                return GameParameters.GameMode.ANY;
            }
        }

        public override bool HasPresets
        {
            get
            {
                return false;
            }
        }

        public override string DisplaySection { get { return "kOS"; } }

        public override string Section
        {
            get
            {
                return "kOS";
            }
        }

        public override int SectionOrder
        {
            get
            {
                return 1;
            }
        }

        public override string Title
        {
            get
            {
                return "Connectivity";
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            Safe.Utilities.SafeHouse.Logger.SuperVerbose("kOSConnectivityParameters.OnLoad()");
            base.OnLoad(node);
            instance = null;
            if (HighLogic.CurrentGame != null || HighLogic.LoadedSceneIsGame)
            {
                instance = this;
            }
        }

        public override IList ValidValues(MemberInfo member)
        {
            if (member.Name == "connectivityHandler")
            {
                if (availableConnectivityManagers == null)
                {
                    availableConnectivityManagers = ConnectivityManager.GetStringList();
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine();
                    for (int i = 0; i < availableConnectivityManagers.Count; ++i)
                    {
                        sb.AppendLine(" * " + availableConnectivityManagers[i]);
                    }
                    connectivityHandlerList = sb.ToString();
                }
                return availableConnectivityManagers;
            }
            else if (member.Name == "connectivityHandlerList")
            {
                if (availableConnectivityManagers == null)
                {
                    availableConnectivityManagers = ConnectivityManager.GetStringList();
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < availableConnectivityManagers.Count; ++i)
                    {
                        sb.AppendLine(" * " + availableConnectivityManagers[i]);
                    }
                    connectivityHandlerList = sb.ToString();
                }
            }
            return null;
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            if (member.Name == "connectivityHandler" && HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                return false;
            }
            return base.Interactible(member, parameters);
        }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            if (member.Name == "version" || member.Name == "knownHandlerList")
            {
                return false;
            }
            if (HighLogic.LoadedScene == GameScenes.MAINMENU)
            {
                if (member.Name == "connectivityHandlerTitle")
                {
                    connectivityHandlerTitle = "You will be prompted to select a handler when the game loads.";
                }
                else
                {
                    return false;
                }
            }
            return base.Enabled(member, parameters);
        }

        public void CheckNewManagers()
        {
            Safe.Utilities.SafeHouse.Logger.SuperVerbose("kOSConnectivityParameters.CheckNewManagers()");
            var availableConnectivityManagers = ConnectivityManager.GetStringHash();
            var knownHandlers = knownHandlerList.Split(',').ToList();
            if (!availableConnectivityManagers.Contains(connectivityHandler) || !availableConnectivityManagers.IsSubsetOf(knownHandlers))
            {
                List<DialogGUIBase> options = new List<DialogGUIBase>();
                foreach (var name in availableConnectivityManagers)
                {
                    string text;
                    if (name.Equals(connectivityHandler))
                    {
                        text = connectivityHandler + " (Selected)";
                    }
                    else
                    {
                        text = name;
                    }
                    options.Add(new DialogGUIButton(text, () => { connectivityHandler = name; }, true));
                }
                Module.kOSSettingsChecker.QueueDialog(
                    new MultiOptionDialog(
                        "Select Dialog",
                        SELECT_DIALOG_TEXT,
                        "kOS",
                        HighLogic.UISkin,
                        options.ToArray()));
            }
            availableConnectivityManagers.UnionWith(knownHandlers);
            knownHandlerList = string.Join(",", availableConnectivityManagers.ToArray());
        }
    }
}
