using kOS.Module;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Function;
using kOS.Safe.Persistence;
using kOS.Suffixed;
using kOS.Suffixed.Part;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using kOS.Utilities;
using Math = System.Math;
using System.Linq;
using kOS.Safe;

namespace kOS.Function
{
    [Function("printlist")]
    public class FunctionPrintList : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string listType = PopValueAssert(shared).ToString();
            AssertArgBottomAndConsume(shared);

            if (shared.Screen == null) return;

            kList list;

            switch (listType)
            {
                case "files":
                    list = GetFileList(shared);
                    break;

                case "volumes":
                    list = GetVolumeList(shared);
                    break;

                case "processors":
                    list = GetProcessorList(shared);
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

                case "rcs":
                    list = GetRCSList(shared);
                    break;

                case "reactionwheels":
                    list = GetReactionWheelList(shared);
                    break;

                case "sensors":
                    list = GetSensorList(shared);
                    break;

                case "config":
                    list = GetConfigList();
                    break;

                case "fonts":
                    list = GetFontList();
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

        private kList GetFileList(Safe.SafeSharedObjects shared)
        {
            var list = new kList();
            list.AddColumn("Name", 30, ColumnAlignment.Left);
            list.AddColumn("Size", 7, ColumnAlignment.Right);

            list.Title = shared.VolumeMgr.CurrentDirectory.Path.ToString();

            IOrderedEnumerable<VolumeItem> items = shared.VolumeMgr.CurrentDirectory.ListAsLexicon().Values.Cast<VolumeItem>().OrderBy(i => i.Name);

            foreach (VolumeDirectory info in items.OfType<VolumeDirectory>())
            {
                list.AddItem(info.Name, "<DIR>");
            }

            foreach (VolumeFile info in items.OfType<VolumeFile>())
            {
                list.AddItem(info.Name, info.Size);
            }

            long freeSpace = shared.VolumeMgr.CurrentVolume.FreeSpace;
            list.Footer = "Free space remaining: " + (freeSpace != Volume.INFINITE_CAPACITY ? freeSpace.ToString() : " infinite");

            return list;
        }

        private kList GetVolumeList(Safe.SafeSharedObjects shared)
        {
            var list = new kList { Title = "Volumes" };
            list.AddColumn("ID", 6, ColumnAlignment.Left);
            list.AddColumn("Name", 24, ColumnAlignment.Left);
            list.AddColumn("Size", 7, ColumnAlignment.Right);

            if (shared.VolumeMgr == null) return list;

            foreach (KeyValuePair<int, Volume> kvp in shared.VolumeMgr.Volumes)
            {
                Volume volume = kvp.Value;
                string id = kvp.Key.ToString() + (shared.VolumeMgr.VolumeIsCurrent(volume) ? "*" : "");
                string size = volume.Capacity.ToString();
                list.AddItem(id, volume.Name, size);
            }

            return list;
        }

        private kList GetProcessorList(SharedObjects shared)
        {
            var list = new kList { Title = "Processors" };
            list.AddColumn("Name", 16, ColumnAlignment.Left);
            list.AddColumn("Tag", 12, ColumnAlignment.Left);
            list.AddColumn("Volume ID", 6, ColumnAlignment.Left);

            if (shared.VolumeMgr == null) return list;

            foreach (kOSProcessor processor in shared.ProcessorMgr.processors.Values)
            {
                string name = processor.name + (shared.Processor == processor ? "*" : "");
                int volumeId = shared.VolumeMgr.GetVolumeId(processor.HardDisk);
                list.AddItem(name, processor.Tag, volumeId);
            }

            return list;
        }

        private kList GetBodyList(SharedObjects shared)
        {
            var list = new kList();
            list.AddColumn("Name", 15, ColumnAlignment.Left);
            list.AddColumn("Distance", 22, ColumnAlignment.Right, "0");

            foreach (var body in FlightGlobals.fetch.bodies)
            {
                list.AddItem(body.bodyName, Vector3d.Distance(body.position, shared.Vessel.CoMD));
            }

            return list;
        }

        private kList GetFontList()
        {
            var list = new kList();
            list.AddColumn("Font Name", 15, ColumnAlignment.Left);

            foreach (Font f in Resources.FindObjectsOfTypeAll<Font>())
            {
                list.AddItem(f.name);
            }

            return list;
        }

        private kList GetTargetList(SharedObjects shared)
        {
            var list = new kList();
            list.AddColumn("Vessel Name", 25, ColumnAlignment.Left);
            list.AddColumn("Distance", 12, ColumnAlignment.Right, "0.0");

            foreach (Vessel vessel in FlightGlobals.Vessels)
            {
                if (vessel == shared.Vessel) continue;

                var vT = VesselTarget.CreateOrGetExisting(vessel, shared);
                list.AddItem(vT.Vessel.vesselName, vT.GetDistance());
            }

            return list;
        }

        private kList GetResourceList(SharedObjects shared)
        {
            var list = new kList();
            list.AddColumn("Stage", 11, ColumnAlignment.Left);
            list.AddColumn("Resource Name", 28, ColumnAlignment.Left);
            list.AddColumn("Amount", 9, ColumnAlignment.Right, "0.00");

            var resourceDict = new SortedDictionary<string, double>();

            foreach (Part part in shared.Vessel.Parts)
            {
                string stageStr = part.inverseStage.ToString();
                PartResource resource;
                for (int i = 0; i < part.Resources.Count; ++i)
                {
                    resource = part.Resources[i];
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
                string key = kvp.Key;
                string stageStr = key.Substring(0, key.IndexOf('|'));
                string resourceName = key.Substring(key.IndexOf('|') + 1);

                list.AddItem(stageStr, resourceName, kvp.Value);
            }

            return list;
        }

        private kList GetPartList(SharedObjects shared)
        {
            var list = new kList();
            list.AddColumn("ID", 28, ColumnAlignment.Left);
            list.AddColumn("Name", 20, ColumnAlignment.Left);

            foreach (Part part in shared.Vessel.Parts)
            {
                list.AddItem(part.ConstructID(), part.partInfo.name);
            }

            return list;
        }

        private kList GetEngineList(SharedObjects shared)
        {
            var list = new kList();
            list.AddColumn("ID", 12, ColumnAlignment.Left);
            list.AddColumn("Stage", 8, ColumnAlignment.Left);
            list.AddColumn("Name", 28, ColumnAlignment.Left);

            ListValue partList = EngineValue.PartsToList(shared.Vessel.Parts, shared);

            foreach (Structure structure in partList)
            {
                var part = (PartValue) structure;
                list.AddItem(part.Part.uid(), part.Part.inverseStage, part.Part.partInfo.name);
            }

            return list;
        }

        private kList GetRCSList(SharedObjects shared)
        {
            var list = new kList();
            list.AddColumn("ID", 12, ColumnAlignment.Left);
            list.AddColumn("Name", 28, ColumnAlignment.Left);

            foreach (Part part in shared.Vessel.Parts)
            {
                foreach (PartModule module in part.Modules)
                {
                    var rcs = module as ModuleRCS;
                    if (rcs != null)
                    {
                        list.AddItem(part.ConstructID(), part.partInfo.name);
                    }
                }
            }

            return list;
        }

        private kList GetReactionWheelList(SharedObjects shared)
        {
            var list = new kList();
            list.AddColumn("ID", 12, ColumnAlignment.Left);
            list.AddColumn("Name", 28, ColumnAlignment.Left);

            foreach (Part part in shared.Vessel.Parts)
            {
                foreach (PartModule module in part.Modules)
                {
                    var wheel = module as ModuleReactionWheel;
                    if (wheel != null)
                    {
                        list.AddItem(part.ConstructID(), part.partInfo.name);
                    }
                }
            }

            return list;
        }

        private kList GetSensorList(SharedObjects shared)
        {
            var list = new kList();
            list.AddColumn("Part Name", 37, ColumnAlignment.Left);
            list.AddColumn("Sensor Type", 11, ColumnAlignment.Left);

            foreach (Part part in shared.Vessel.Parts)
            {
                foreach (PartModule module in part.Modules)
                {
                    var sensor = module as ModuleEnviroSensor;
                    if (sensor != null)
                    {
                        list.AddItem(part.partInfo.title, sensor.sensorType);
                    }
                }
            }

            return list;
        }

        private kList GetConfigList()
        {
            var list = new kList();
            list.AddColumn("", 9, ColumnAlignment.Left);
            list.AddColumn("Name", 34, ColumnAlignment.Left);
            list.AddColumn("Value", 6, ColumnAlignment.Left);

            foreach (ConfigKey key in Config.Instance.GetConfigKeys())
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

            private readonly List<kListColumn> columns = new List<kListColumn>();
            private readonly List<string> items = new List<string>();
            private string formatString = string.Empty;
            private string headerString = string.Empty;
            private int totalWidth;

            public void AddColumn(string title, int width, ColumnAlignment alignment)
            {
                AddColumn(title, width, alignment, string.Empty);
            }

            public void AddColumn(string title, int width, ColumnAlignment alignment, string format)
            {
                columns.Add(new kListColumn(title, width, alignment, format));
                formatString = string.Empty;
                totalWidth += width;

                if (alignment == ColumnAlignment.Left)
                {
                    headerString += title.Substring(0, Math.Min(width, title.Length)).PadRight(width);
                }
                else
                {
                    headerString += title.Substring(0, Math.Min(width, title.Length)).PadLeft(width);
                }
            }

            private void BuildFormatString()
            {
                var builder = new StringBuilder();

                for (int index = 0; index < columns.Count; index++)
                {
                    string alignment = columns[index].Alignment == ColumnAlignment.Left ? "-" : "";
                    string separator;

                    if (index < columns.Count - 1)
                    {
                        columns[index].ItemWidth = columns[index].Width - 1;
                        separator = " ";
                    }
                    else
                    {
                        columns[index].ItemWidth = columns[index].Width;
                        separator = "";
                    }

                    builder.AppendFormat("{{{0},{1}{2}}}{3}", index, alignment, columns[index].ItemWidth, separator);
                }

                formatString = builder.ToString();
            }

            public void AddItem(params object[] fields)
            {
                if (fields.Length == columns.Count)
                {
                    if (formatString == string.Empty)
                    {
                        BuildFormatString();
                    }

                    var stringFields = new object[fields.Length];
                    for (int index = 0; index < columns.Count; index++)
                    {
                        string field;

                        if (columns[index].Format != string.Empty &&
                            (fields[index] is int ||
                             fields[index] is double ||
                             fields[index] is float))
                        {
                            double number = Convert.ToDouble(fields[index]);
                            field = number.ToString(columns[index].Format);
                        }
                        else
                        {
                            field = fields[index].ToString();
                        }

                        stringFields[index] = field.Substring(0, Math.Min(columns[index].ItemWidth, field.Length));
                    }

                    items.Add(string.Format(formatString, stringFields));
                }
                else
                {
                    throw new Exception("Wrong number of items to insert into list");
                }
            }

            public override string ToString()
            {
                var builder = new StringBuilder();
                if (Title != string.Empty) builder.AppendLine(Title);
                builder.AppendLine(headerString);
                builder.AppendLine(new string('-', totalWidth));

                foreach (string item in items)
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
            public kListColumn(string title, int width, ColumnAlignment alignment, string format)
            {
                Title = title;
                Width = width;
                Alignment = alignment;
                Format = format;
            }

            public string Title { get; private set; }

            public int Width { get; private set; }

            public int ItemWidth { get; set; }

            public ColumnAlignment Alignment { get; private set; }

            public string Format { get; private set; }
        }

        #endregion List class
    }
}