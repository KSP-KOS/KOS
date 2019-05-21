using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using kOS.Safe.Exceptions;
using kOS.Safe.Encapsulation;

namespace kOS.Safe.Persistence
{
    /// <summary>
    /// Represents the location of a directory or a file inside a volume.
    /// </summary>
    public class VolumePath
    {
        public static VolumePath EMPTY = new VolumePath();

        public const char PathSeparator = '/';
        public const string UpSegment = "..";
        public const int MaxSegmentLength = 255;
        public const string SegmentRegexString = @"\A[^/\\]+\Z";
        private static readonly Regex segmentRegex = new Regex(SegmentRegexString);

        /// <summary>
        /// Number of segments in the path.
        /// </summary>
        public int Length
        {
            get
            {
                return Segments.Count;
            }
        }

        /// <summary>
        /// True if path is a root path.
        /// </summary>
        public bool IsRoot
        {
            get
            {
                return Segments.Count == 0;
            }
        }

        /// <summary>
        /// Depth of the path. Same as Length if the path does not contain any '..'.
        /// </summary>
        public int Depth
        {
            get
            {
                int upSegments = Segments.Count(s => s.Equals(UpSegment));
                return Length - 2 * upSegments;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="kOS.Safe.Persistence.VolumePath"/> points outside of this volume.
        /// </summary>
        public bool PointsOutside
        {
            get
            {
                return Segments.Count > 0 && Segments[0].Equals(UpSegment);
            }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name
        {
            get
            {
                return Segments.Count > 0 ? Segments.Last() : string.Empty;
            }
        }

        public string Extension
        {
            get
            {
                if (Name == null)
                {
                    return null;
                }

                var nameParts = Name.Split('.');
                return nameParts.Count() > 1 ? nameParts.Last() : string.Empty;
            }
        }

        /// <summary>
        /// Gets this path's segments.
        /// </summary>
        public List<string> Segments { get; private set; }

        public static bool IsValidSegment(string segment)
        {
            return segment.Length > 0 && segment.Length <= MaxSegmentLength && segmentRegex.IsMatch(segment);
        }

        /// <summary>
        /// Determines if a string represents an absolute path.
        /// </summary>
        public static Boolean IsAbsolute(String pathString)
        {
            return pathString.StartsWith(PathSeparator.ToString());
        }

        public VolumePath Combine(params string[] segments)
        {
            var parsedSegments = segments.SelectMany((segment) => GetSegmentsFromString(segment));

            return new VolumePath(Segments.Concat(parsedSegments));
        }

        public static VolumePath FromString(string pathString, VolumePath basePath)
        {
            if (IsAbsolute(pathString))
            {
                throw new KOSInvalidPathException("Relative path expected", pathString);
            }

            List<string> mergedSegments = new List<string>();
            mergedSegments.AddRange(basePath.Segments);
            mergedSegments.AddRange(GetSegmentsFromString(pathString));

            return new VolumePath(mergedSegments);
        }

        public static VolumePath FromString(string pathString)
        {
            return new VolumePath(GetSegmentsFromString(pathString));
        }

        protected static List<string> GetSegmentsFromString(string pathString)
        {
            IEnumerable<string> segments = pathString.Split(PathSeparator).Where((s) => !String.IsNullOrEmpty(s));

            foreach (string segment in segments)
            {
                if (!IsValidSegment(segment))
                {
                    throw new KOSInvalidPathException("Invalid path segment: '" + segment + "'", pathString);
                }
            }

            return new List<string>(segments);
        }

        protected VolumePath()
        {
            this.Segments = new List<string>();
        }

        protected VolumePath(IEnumerable<string> segments)
        {
            this.Segments = new List<string>(segments);

            Canonicalize();
        }

        private void Canonicalize()
        {
            List<string> newSegments = new List<string>();

            for (int i = 0; i < Segments.Count; i++)
            {
                if (string.IsNullOrEmpty(Segments[i])) {
                    continue;
                }

                if (Segments[i].Contains(PathSeparator))
                {
                    throw new KOSInvalidPathException("Segment can't contain '" + PathSeparator + "'", Segments[i]);
                }

                if (Segments[i].Equals(UpSegment) && newSegments.Count != 0 && !newSegments.Last().Equals(UpSegment))
                {
                    newSegments.RemoveAt(newSegments.Count() - 1);
                }
                else
                {
                    newSegments.Add(Segments[i]);
                }
            }

            Segments = newSegments;

            if (PointsOutside)
            {
                throw new KOSInvalidPathException("This path points to something outside of volume", ToString());
            }
        }

        public VolumePath GetParent()
        {
            if (Depth < 1)
            {
                throw new KOSException("This path does not have a parent");
            }

            return new VolumePath(new List<string>(Segments.Take(Segments.Count - 1)));
        }

        public bool IsParent(VolumePath path)
        {
            return path.Segments.Count > Segments.Count && path.Segments.GetRange(0, Segments.Count).SequenceEqual(Segments);
        }

        public override int GetHashCode()
        {
            return Segments.Aggregate(1, (i, s) => i + s.GetHashCode());
        }

        public override bool Equals(object other)
        {
            VolumePath otherPath = other as VolumePath;

            if (ReferenceEquals(otherPath,null)) // ReferenceEquals prevents infinite recursion with overloaded == operator.
            {
                return false;
            }

            return Segments.SequenceEqual(otherPath.Segments);
        }

        public static bool operator ==(VolumePath left, VolumePath right)
        {
            if (ReferenceEquals(left,null) || ReferenceEquals(right,null)) // ReferenceEquals prevents infinite recursion with overloaded == operator.
                return ReferenceEquals(left, null) && ReferenceEquals(right, null); // ReferenceEquals prevents infinite recursion with overloaded == operator.
            return left.Equals(right);
        }

        public static bool operator !=(VolumePath left, VolumePath right)
        {
            return !(left == right);
        }


        public override string ToString()
        {
            return "/" + String.Join("/", Segments.ToArray());
        }

    }
}

