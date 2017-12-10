using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using kOS.Safe.Persistence;
using kOS.Safe.Exceptions;
using System.Linq;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Safe.Persistence
{
    /// <summary>
    /// Represents the location of a directory or a file inside a kOS. It contains a volumeId and a VolumePath.
    /// </summary>
    /// <seealso cref="VolumePath"/>
    public class GlobalPath : VolumePath
    {
        private const string CurrentDirectoryPath = ".";
        public const string VolumeIdentifierRegexString = @"\A(?<id>[\w\.]+):(?<rest>.*)\Z";
        private static Regex volumeIdentifierRegex = new Regex(VolumeIdentifierRegexString);

        public static new GlobalPath EMPTY = new GlobalPath("$$empty$$", VolumePath.EMPTY);

        public object VolumeId { get; private set; }

        protected GlobalPath(object volumeId)
        {
            VolumeId = ValidateVolumeId(volumeId);
        }

        private GlobalPath(object volumeId, VolumePath path) : this(volumeId, new List<string>(path.Segments))
        {
        }

        private GlobalPath(object volumeId, IEnumerable<string> segments) : base(segments)
        {
            VolumeId = ValidateVolumeId(volumeId);
        }

        private static object ValidateVolumeId(object volumeId)
        {
            if (!(volumeId is int || volumeId is string) || (volumeId is string && String.IsNullOrEmpty(volumeId as string)))
            {
                throw new KOSException("Invalid volumeId: '" + volumeId + "'");
            }

            int result;
            if (volumeId is string && int.TryParse(volumeId as string, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
            {
                volumeId = result;
            }

            return volumeId;
        }

        public static bool HasVolumeId(string pathString)
        {
            return volumeIdentifierRegex.Match(pathString).Success;
        }

        public new GlobalPath GetParent()
        {
            if (Depth < 1)
            {
                throw new KOSException("This path does not have a parent");
            }

            return new GlobalPath(VolumeId, new List<string>(Segments.Take(Segments.Count - 1)));
        }

        public bool IsParent(GlobalPath path)
        {
            return VolumeId.Equals(path.VolumeId) && base.IsParent(path);
        }

        public GlobalPath RootPath()
        {
            return new GlobalPath(VolumeId);
        }

        public static GlobalPath FromVolumePath(VolumePath volumePath, object volumeId)
        {
            return new GlobalPath(volumeId, new List<string>(volumePath.Segments));
        }

        public static GlobalPath FromVolumePath(VolumePath volumePath, int volumeId)
        {
            return new GlobalPath(volumeId, new List<string>(volumePath.Segments));
        }

        public GlobalPath ChangeName(string newName)
        {
            if (Segments.Count == 0)
            {
                throw new KOSInvalidPathException("This path points to the root directory, you can't change its name",
                    this.ToString());
            }

            List<string> newSegments = new List<string>(Segments);
            newSegments.RemoveAt(newSegments.Count - 1);
            newSegments.Add(newName);

            return new GlobalPath(VolumeId, newSegments);
        }

        public GlobalPath ChangeExtension(string newExtension)
        {
            if (Segments.Count == 0)
            {
                throw new KOSInvalidPathException("This path points to the root directory, you can't change its extension", this.ToString());
            }

            string nameSegment = Segments.Last();
            List<string> newSegments = new List<string>(Segments);
            newSegments.RemoveAt(newSegments.Count - 1);
            var nameParts = new List<string>(nameSegment.Split('.'));

            if (nameParts.Count() > 1)
            {
                nameParts.RemoveAt(nameParts.Count() - 1);
            }

            nameParts = new List<string>(nameParts);
            nameParts.Add(newExtension);

            var newName = String.Join(".", nameParts.ToArray());

            newSegments.Add(newName);

            return new GlobalPath(VolumeId, newSegments);
        }

        public new GlobalPath Combine(params string[] segments)
        {
            var parsedSegments = segments.SelectMany((segment) => GetSegmentsFromString(segment));

            return new GlobalPath(VolumeId, Segments.Concat(parsedSegments));
        }

        /// <summary>
        /// Create a GlobalPath from a base path and a relative path.
        /// </summary>
        /// <returns>GlobalPath that represents the new path.</returns>
        /// <param name="pathString">Path string relative to basePath.</param>
        /// <param name="basePath">Base path.</param>
        public static GlobalPath FromStringAndBase(string pathString, GlobalPath basePath)
        {
            if (IsAbsolute(pathString))
            {
                throw new KOSInvalidPathException("Relative path expected", pathString);
            }

            if (pathString.Equals(CurrentDirectoryPath))
            {
                return basePath;
            }

            List<string> mergedSegments = new List<string>();
            mergedSegments.AddRange(basePath.Segments);
            mergedSegments.AddRange(GetSegmentsFromString(pathString));

            return new GlobalPath(basePath.VolumeId, mergedSegments);
        }

        /// <summary>
        /// Creates a GlobalPath from string.
        /// </summary>
        /// <returns>Path string, must have the following format: volumeId:[/]segment1[/furthersegments]*</returns>
        /// <param name="pathString">Path string.</param>
        public static new GlobalPath FromString(string pathString)
        {
            string volumeName = null;
            Match match = volumeIdentifierRegex.Match(pathString);

            if (match.Success)
            {
                volumeName = match.Groups["id"].Captures[0].Value;
                pathString = match.Groups["rest"].Captures[0].Value;
            }
            else
            {
                throw new KOSInvalidPathException("GlobalPath should contain a volumeId", pathString);
            }

            VolumePath path = VolumePath.FromString(pathString);
            return new GlobalPath(volumeName, path);
        }

        public override int GetHashCode()
        {
            return 13 * VolumeId.GetHashCode() + base.GetHashCode();
        }

        public override bool Equals(object other)
        {
            GlobalPath otherPath = other as GlobalPath;

            if (ReferenceEquals(otherPath, null)) // ReferenceEquals prevents infinite recursion with overloaded == operator.
            {
                return false;
            }
            bool result =  VolumeId.Equals(otherPath.VolumeId) && Segments.SequenceEqual(otherPath.Segments);
            return result;
        }

        public static bool operator ==(GlobalPath left, GlobalPath right)
        {
            if (ReferenceEquals(left,null) || ReferenceEquals(right,null)) // ReferenceEquals prevents infinite recursion with overloaded == operator.
                return ReferenceEquals(left, null) && ReferenceEquals(right, null); // ReferenceEquals prevents infinite recursion with overloaded == operator.
            return left.Equals(right);
        }

        public static bool operator !=(GlobalPath left, GlobalPath right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return VolumeId + ":" + base.ToString();
        }

    }
}