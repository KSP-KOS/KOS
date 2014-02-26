using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Persistence;
using kOS.Suffixed;
using kOS.Utilities;

namespace kOS.Function
{
    [FunctionAttribute("printlist")]
    public class FunctionPrintList : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string listType = shared.Cpu.PopValue().ToString();

            if (shared.Screen != null)
            {
                kList list = null;

                switch (listType)
                {
                    case "files":
                        list = GetFileList(shared);
                        break;
                    case "volumes":
                        list = GetVolumeList(shared);
                        break;
                    case "bodies":
                        list = GetBodyList(shared);
                        break;
                    case "targets":
                        list = GetTargetList(shared);
                        break;
                    case "resources":
                        list = GetResourceList(shared);
                        break;
                    case "parts":
                        list = GetPartList(shared);
                        break;
                    case "engines":
                        list = GetEngineList(shared);
                        break;
                    case "sensors":
                        list = GetSensorList(shared);
                        break;
                    case "config":
                        list = GetConfigList(shared);
                        break;
                    default:
                        throw new Exception("List type not supported");
                }

                if (list != null)
                {
                    shared.Screen.Print(" ");
                    shared.Screen.Print(list.ToString());
                    shared.Screen.Print(" ");
                }
            }
        }

        private kList GetFileList(SharedObjects shared)
        {
            kList list = new kList();
            list.AddColumn("Name", 30, ColumnAlignment.Left);
            list.AddColumn("Size", 7, ColumnAlignment.Right);

            if (shared.VolumeMgr != null)
            {
                Volume volume = shared.VolumeMgr.CurrentVolume;
                if (volume != null)
                {
                    list.Title = "Volume " + shared.VolumeMgr.GetVolumeBestIdentifier(volume);
                    
                    foreach (FileInfo info in volume.GetFileList())
                    {
                        list.AddItem(info.Name, info.Size);
                    }

                    int freeSpace = volume.GetFreeSpace();
                    list.Footer = "Free space remaining: " + (freeSpace > -1 ? freeSpace.ToString() : " infinite");
                }
            }

            return list;
        }

        private kList GetVolumeList(SharedObjects shared)
        {
            kList list = new kList();
            list.Title = "Volumes";
            list.AddColumn("ID", 6, ColumnAlignment.Left);
            list.AddColumn("Name", 24, ColumnAlignment.Left);
            list.AddColumn("Size", 7, ColumnAlignment.Right);

            if (shared.VolumeMgr != null)
            {
                foreach (KeyValuePair<int, Volume> kvp in shared.VolumeMgr.Volumes)
                {
                    Volume volume = kvp.Value;
                    string id = kvp.Key.ToString() + (shared.VolumeMgr.VolumeIsCurrent(volume) ? "*" : "");
                    string size = volume.CheckRange(shared.Vessel) ? (volume.Capacity > -1 ? volume.Capacity.ToString() : "Inf") : "Disc";
                    list.AddItem(id, volume.Name, size);
                }
            }

            return list;
        }

        private kList GetBodyList(SharedObjects shared)
        {
            kList list = new kList();
            list.AddColumn("Name", 15, ColumnAlignment.Left);
            list.AddColumn("Distance", 22, ColumnAlignment.Right, "0");
            
            foreach (var body in FlightGlobals.fetch.bodies)
            {
                list.AddItem(body.bodyName, Vector3d.Distance(body.position, shared.Vessel.GetWorldPos3D()));
            }

            return list;
        }

        private kList GetTargetList(SharedObjects shared)
        {
            kList list = new kList();
            list.AddColumn("Vessel Name", 25, ColumnAlignment.Left);
            list.AddColumn("Distance", 12, ColumnAlignment.Right, "0.0");

            double commRange = VesselUtils.GetCommRange(shared.Vessel);

            foreach (Vessel vessel in FlightGlobals.Vessels)
            {
                if (vessel != shared.Vessel)
                {
                    var vT = new VesselTarget(vessel, shared.Vessel);
                    if (vT.IsInRange(commRange))
                    {
                        list.AddItem(vT.Target.vesselName, vT.GetDistance());
                    }
                }
            }

            return list;
        }

        private kList GetResourceList(SharedObjects shared)
        {
            kList list = new kList();
            list.AddColumn("Stage", 11, ColumnAlignment.Left);
            list.AddColumn("Resource Name", 28, ColumnAlignment.Left);
            list.AddColumn("Amount", 9, ColumnAlignment.Right, "0.00");

            SortedDictionary<string, double> resourceDict = new SortedDictionary<string, double>();

            foreach (Part part in shared.Vessel.Parts)
            {
                string stageStr = part.inverseStage.ToString();
                foreach (PartResource resource in part.Resources)
                {
                    string key = stageStr + "|" + resource.resourceName;
                    if (resourceDict.ContainsKey(key))
                    {
                        resourceDict[key] += resource.amount;
                    }
                    else
                    {
                        resourceDict.Add(key, resource.amount);
                    }
                }
            }

            foreach (KeyValuePair<string, double> kvp in resourceDict)
            {
                string key = kvp.Key.ToString();
                string stageStr = key.Substring(0, key.IndexOf('|'));
                string resourceName = key.Substring(key.IndexOf('|') + 1);

                list.AddItem(stageStr, resourceName, kvp.Value);
            }
            
            return list;
        }

        private kList GetPartList(SharedObjects shared)
        {
            kList list = new kList();
            list.AddColumn("ID", 28, ColumnAlignment.Left);
            list.AddColumn("Name", 20, ColumnAlignment.Left);

            foreach (Part part in shared.Vessel.Parts)
            {
                list.AddItem(part.ConstructID, part.partInfo.name);
            }

            return list;
        }

        private kList GetEngineList(SharedObjects shared)
        {
            kList list = new kList();
            list.AddColumn("ID", 12, ColumnAlignment.Left);
            list.AddColumn("Stage", 8, ColumnAlignment.Left);
            list.AddColumn("Name", 28, ColumnAlignment.Left);

            foreach (Part part in VesselUtils.GetListOfActivatedEngines(shared.Vessel))
            {
                foreach (PartModule module in part.Modules)
                {
                    if (module is ModuleEngines)
                    {
                        var engineMod = (ModuleEngines)module;
                        list.AddItem(part.uid, part.inverseStage, engineMod.moduleName);
                    }
                }
            }

            return list;
        }

        private kList GetSensorList(SharedObjects shared)
        {
            kList list = new kList();
            list.AddColumn("Part Name", 37, ColumnAlignment.Left);
            list.AddColumn("Sensor Type", 11, ColumnAlignment.Left);

            foreach (Part part in shared.Vessel.Parts)
            {
                foreach (PartModule module in part.Modules)
                {
                    ModuleEnviroSensor sensor = module as ModuleEnviroSensor;
                    if (sensor != null)
                    {
                        list.AddItem(part.partInfo.title, sensor.sensorType);
                    }
                }
            }

            return list;
        }

        private kList GetConfigList(SharedObjects shared)
        {
            kList list = new kList();
            list.AddColumn("", 5, ColumnAlignment.Left);
            list.AddColumn("Name", 30, ColumnAlignment.Left);
            list.AddColumn("Value", 10, ColumnAlignment.Left);

            foreach (ConfigKey key in Config.GetInstance().GetConfigKeys())
            {
                list.AddItem(key.Alias, key.Name, key.Value);
            }            

            return list;
        }


        #region List class

        private class kList
        {
            public string Title = string.Empty;
            public string Footer = string.Empty;

            private List<kListColumn> _columns = new List<kListColumn>();
            private List<string> _items = new List<string>();
            private string _formatString = string.Empty;
            private string _headerString = string.Empty;
            private int _totalWidth = 0;

            public void AddColumn(string title, int width)
            {
                AddColumn(title, width, ColumnAlignment.Left, string.Empty);
            }
            
            public void AddColumn(string title, int width, ColumnAlignment alignment)
            {
                AddColumn(title, width, alignment, string.Empty);
            }

            public void AddColumn(string title, int width, ColumnAlignment alignment, string format)
            {
                _columns.Add(new kListColumn(title, width, alignment, format));
                _formatString = string.Empty;
                _totalWidth += width;

                if (alignment == ColumnAlignment.Left)
                {
                    _headerString += title.Substring(0, Math.Min(width, title.Length)).PadRight(width);
                }
                else
                {
                    _headerString += title.Substring(0, Math.Min(width, title.Length)).PadLeft(width);
                }
            }

            private void BuildFormatString()
            {
                StringBuilder builder = new StringBuilder();

                for (int index = 0; index < _columns.Count; index++)
                {
                    string alignment = _columns[index].Alignment == ColumnAlignment.Left ? "-" : "";
                    string separator;

                    if (index < (_columns.Count - 1))
                    {
                        _columns[index].ItemWidth = _columns[index].Width - 1;
                        separator = " ";
                    }
                    else
                    {
                        _columns[index].ItemWidth = _columns[index].Width;
                        separator = "";
                    }

                    builder.AppendFormat("{{{0},{1}{2}}}{3}", index, alignment, _columns[index].ItemWidth, separator);
                }

                _formatString = builder.ToString();
            }

            public void AddItem(params object[] fields)
            {
                if (fields.Length == _columns.Count)
                {
                    if (_formatString == string.Empty)
                    {
                        BuildFormatString();
                    }

                    string[] stringFields = new string[fields.Length];
                    for (int index = 0; index < _columns.Count; index++)
                    {
                        string field;

                        if (_columns[index].Format != string.Empty &&
                            (fields[index] is int ||
                             fields[index] is double ||
                             fields[index] is float))
                        {
                            double number = Convert.ToDouble(fields[index]);
                            field = number.ToString(_columns[index].Format);
                        }
                        else
                        {
                            field = fields[index].ToString();
                        }
                        
                        stringFields[index] = field.Substring(0, Math.Min(_columns[index].ItemWidth, field.Length));
                    }

                    _items.Add(string.Format(_formatString, stringFields));
                }
                else
                {
                    throw new Exception("Wrong number of items to insert into list");
                }
            }

            public override string ToString()
            {
                StringBuilder builder = new StringBuilder();
                if (Title != string.Empty) builder.AppendLine(Title);
                builder.AppendLine(_headerString);
                builder.AppendLine(new string('-', _totalWidth));

                foreach (string item in _items)
                {
                    builder.AppendLine(item);
                }

                if (Footer != string.Empty) builder.AppendLine(Footer);

                return builder.ToString();
            }
        }

        private enum ColumnAlignment
        {
            Left = 0,
            Right = 1
        }

        private class kListColumn
        {
            public string Title;
            public int Width;
            public int ItemWidth;
            public ColumnAlignment Alignment;
            public string Format;

            public kListColumn(string title, int width, ColumnAlignment alignment, string format)
            {
                this.Title = title;
                this.Width = width;
                this.Alignment = alignment;
                this.Format = format;
            }
        }

        #endregion
    }
}
