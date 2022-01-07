using System;
using kOS.Safe.Serialization;
using kOS.Safe.Persistence;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Encapsulation;
using System.Linq;
using kOS.Safe.Function;

namespace kOS.Safe
{
    /// <summary>
    /// Contains suffixes related to GlobalPath.
    ///
    /// This exists as a separate class because some of the suffixes require an instance of VolumeManager to work. I think
    /// it would be counter-productive to pass around an instance of VolumeManager whenever we're dealing with GlobalPath internally.
    /// Instances of this class are on the other hand created only for the user.
    /// </summary>
    [kOS.Safe.Utilities.KOSNomenclature("Path")]
    public class PathValue : Structure
    {
        [Function("path")]
        public class FunctionPath : SafeFunctionBase
        {
            public override void Execute(SafeSharedObjects shared)
            {
                int remaining = CountRemainingArgs(shared);

                GlobalPath path;

                if (remaining == 0)
                {
                    path = GlobalPath.FromVolumePath(shared.VolumeMgr.CurrentDirectory.Path,
                                                     shared.VolumeMgr.GetVolumeRawIdentifier(shared.VolumeMgr.CurrentVolume));
                }
                else
                {
                    object pathObject = PopValueAssert(shared, true);
                    path = shared.VolumeMgr.GlobalPathFromObject(pathObject);
                }

                AssertArgBottomAndConsume(shared);

                ReturnValue = new PathValue(path, shared);
            }
        }

        private const string DumpPath = "path";

        public GlobalPath Path { get; private set; }
        private SafeSharedObjects sharedObjects;

        public SafeSharedObjects Shared {
            set
            {
                sharedObjects = value;
            }
        }

        public PathValue(GlobalPath path, SafeSharedObjects sharedObjects)
        {
            InitializeSuffixes();
            Path = path;
            this.sharedObjects = sharedObjects;
        }

        public PathValue FromPath(GlobalPath path)
        {
            return new PathValue(path, sharedObjects);
        }

        public PathValue FromPath(VolumePath volumePath, string volumeId)
        {
            return new PathValue(GlobalPath.FromVolumePath(volumePath, volumeId), sharedObjects);
        }

        private void InitializeSuffixes()
        {
            AddSuffix("VOLUME", new Suffix<Volume>(() => sharedObjects.VolumeMgr.GetVolumeFromPath(Path)));
            AddSuffix("SEGMENTS", new Suffix<ListValue>(() => new ListValue(Path.Segments.Select((s) => (Structure)new StringValue(s)))));
            AddSuffix("LENGTH", new Suffix<ScalarIntValue>(() => Path.Length));
            AddSuffix("NAME", new Suffix<StringValue>(() => Path.Name));
            AddSuffix("HASEXTENSION", new Suffix<BooleanValue>(() => string.IsNullOrEmpty(Path.Extension)));
            AddSuffix("EXTENSION", new Suffix<StringValue>(() => Path.Extension));
            AddSuffix("ROOT", new Suffix<PathValue>(() => FromPath(Path.RootPath())));
            AddSuffix("PARENT", new Suffix<PathValue>(() => FromPath(Path.GetParent())));

            AddSuffix("ISPARENT", new OneArgsSuffix<BooleanValue, PathValue>((p) => Path.IsParent(p.Path)));
            AddSuffix("CHANGENAME", new OneArgsSuffix<PathValue, StringValue>((n) => FromPath(Path.ChangeName(n))));
            AddSuffix("CHANGEEXTENSION", new OneArgsSuffix<PathValue, StringValue>((e) => FromPath(Path.ChangeExtension(e))));
            AddSuffix("COMBINE", new VarArgsSuffix<PathValue, Structure>(Combine));
        }
        public PathValue Combine(params Structure[] segments)
        {
            if (segments.All(s => s.GetType() == typeof(StringValue)))
            {
                return Combine(segments.Cast<StringValue>().ToArray());
            }
            throw new Exceptions.KOSInvalidArgumentException("PATH:COMBINE", "SEGMENTS", "all segments must be strings");
        }

        public PathValue Combine(params StringValue[] segments)
        {
            return FromPath(Path.Combine(segments.Select(s => s.ToString()).ToArray()));
        }

        public Dump Dump()
        {
            //return new Dump { { DumpPath, Path.ToString() } };
            return null;
        }

        public void LoadDump(Dump dump)
        {
            //Path = GlobalPath.FromString(dump[DumpPath] as string);
        }

        public override string ToString()
        {
            return Path.ToString();
        }

        public override bool Equals(object other)
        {
            PathValue pVal = other as PathValue;
            if (!ReferenceEquals(pVal,null)) // ReferenceEquals prevents infinite recursion with overloaded == operator.
                return Path == pVal.Path;
            GlobalPath gVal = other as GlobalPath;
            if (!ReferenceEquals(gVal,null)) // ReferenceEquals prevents infinite recursion with overloaded == operator.
                return Path == gVal;

            // fallback:
            return base.Equals(other);
        }

        public override int GetHashCode()
        {
            if (!ReferenceEquals(Path,null)) // ReferenceEquals prevents infinite recursion with overloaded == operator.
                return Path.GetHashCode();
            return base.GetHashCode();
        }

        public static bool operator ==(PathValue left, PathValue right)
        {
            if (ReferenceEquals(left,null) || ReferenceEquals(right,null)) // ReferenceEquals prevents infinite recursion with overloaded == operator.
                return ReferenceEquals(left, null) && ReferenceEquals(right, null); // ReferenceEquals prevents infinite recursion with overloaded == operator.
            return left.Equals(right);
        }
        public static bool operator !=(PathValue left, PathValue right)
        {
            return !(left == right);
        }

    }
}

