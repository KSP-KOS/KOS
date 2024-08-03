using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using kOS.Utilities;
using kOS;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe;

namespace kOS.Suffixed
{
    /// <summary>
    /// BoundsValue is kOS's wrapper around Unity's Bounds object for making a
    /// bounding box aligned to some axis.  A bounding box MUST be allinged to
    /// some set of axes, and the BoundsValue won't remember what those axes
    /// are- it will depend on how you created it (i.e. if it's the bounds of
    /// a whole rocket, then it's oriented to ship:facing, but if it's the
    /// bounds of a single part then it's oriented to that part's part:facing.)
    /// One important difference between Unity's Bounds object
    /// and kOS's BoundsValue is that kOS will store the "anchor point" that
    /// is offcenter within the bounds, and return Min/Max values relative to
    /// THAT anchor point instead of relative to the box's center like default
    /// Unity does.  This is so that part bounds values will make sense relative
    /// to the anchor point of the part (i.e. where the part's part.transform is,
    /// which isn't always centered within the mesh.)
    /// </summary>
    [kOS.Safe.Utilities.KOSNomenclature("Bounds")]
    public class BoundsValue : Structure
    {
        public delegate Vector OriginUpdater();
        public delegate Direction FacingUpdater();

        private SharedObjects shared;
        private Bounds unityBounds;
        /// <summary>
        /// This describes where the Unity's bounds box's (0,0,0) origin is anchored to the
        /// world coordinates.  This is in *world* space, in *world* orientation, NOT in the
        /// bounding box's own orientation.
        /// </summary>
        private Vector origin;
        private OriginUpdater originDel;
        private FacingUpdater facingDel;
        private Direction facing;

        /// <summary>
        /// This variante of the BoundsValue constructor makes a fixed origin and direction that
        /// won't properly update when the universe's world axes rotate, or the object in question
        /// the bounding box surrounds moves.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="origin"></param>
        /// <param name="direction"></param>
        /// <param name="shared"></param>
        public BoundsValue(Vector min, Vector max, Vector origin, Direction facing, SharedObjects shared)
        {
            unityBounds = new Bounds();
            unityBounds.SetMinMax(min.ToVector3(), max.ToVector3());
            this.origin = origin;
            this.shared = shared;
            this.facing = facing;
            RegisterInitializer(InitializeSuffixes);
        }

        /// <summary>
        /// If this variant of the BoundsValue constructor is used, then the origin and direction will always
        /// get updated every time a user gets a suffix that needs them.  But if the user ever directly sets the
        /// origin or direction, that will stop the updater delegate and turn off this functionality.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="originDel"></param>
        /// <param name="directionDel"></param>
        /// <param name="shared"></param>
        public BoundsValue(Vector min, Vector max, OriginUpdater originDel, FacingUpdater facingDel, SharedObjects shared) :
            this(min, max, originDel(), facingDel(), shared)
        {
            this.originDel = originDel;
            this.facingDel = facingDel;
        }

        public BoundsValue(BoundsValue boundsVal, SharedObjects shared) : 
            this(new Vector(boundsVal.unityBounds.min), new Vector(boundsVal.unityBounds.max), boundsVal.origin, boundsVal.facing, shared)
        {
            this.originDel = boundsVal.originDel;
            this.facingDel = boundsVal.facingDel;
        }

        /// <summary>
        /// Just gets the raw Unity bounds object that this wraps.
        /// </summary>
        /// <returns></returns>
        public Bounds GetUnityBounds()
        {
            return unityBounds;
        }

        public Vector RelativeMin
        {
            get { return new Vector(unityBounds.min); }
            set { unityBounds.min = value.ToVector3(); }
        }

        public Vector AbsoluteMin
        {
            get { return Origin + Facing * new Vector(unityBounds.min); }
        }

        public Vector RelativeMax
        {
            get { return new Vector(unityBounds.max); }
            set { unityBounds.max = value.ToVector3(); }
        }

        public Vector AbsoluteMax
        {
            get { return Origin + Facing * new Vector(unityBounds.max); }
        }

        public Vector RelativeCenter
        {
            get { return new Vector(unityBounds.center); }
        }

        public Vector AbsoluteCenter
        {
            get { return new Vector(Facing.Rotation * unityBounds.center + Origin.ToVector3()); }
        }

        public Vector Origin
        {
            get
            {
                if (originDel != null) origin = originDel();
                return origin;
            }
            set
            {
                originDel = null; // disengage the auto-updater when the user sets their own new value.
                origin = value;
            }
        }

        public Direction Facing
        {
            get
            {
                if (facingDel != null)
                    facing = facingDel();
                return facing;
            }
            set
            {
                facingDel = null; // disengage the auto-updater when the user sets their own new value.
                facing = value;
            }
        }

        /// <summary>
        /// Get which of the 8 corners of the bounds box is most "in the direction of" the ray given.
        /// i.e. if passed in the up:vector, it will get the topmost corner and if passed in the
        /// -up:vector, it will get the bottommost corner.  Corner is returned in Ship-Raw ref frame.
        /// </summary>
        /// <param name="ray"></param>
        /// <returns></returns>
        public Vector FurthestCorner(Vector ray)
        {
            // Rotate the ray into the bounds box's local frame axes and that will make it clear which corner to use:
            // If that ray has a +x component, then you want the max X, if it has a -x component, then you want the min x,
            // etc for each of the 3 axes:
            Vector3 orientedRay = Quaternion.Inverse(Facing.Rotation) * ray.Normalized().ToVector3();
            float relX = orientedRay.x > 0 ? unityBounds.max.x : unityBounds.min.x;
            float relY = orientedRay.y > 0 ? unityBounds.max.y : unityBounds.min.y;
            float relZ = orientedRay.z > 0 ? unityBounds.max.z : unityBounds.min.z;
            Vector3 relCorner = new Vector3(relX, relY, relZ);
            return Origin + new Vector(Facing.Rotation * relCorner);
        }

        public double BottomAltitude()
        {
            BodyTarget body = BodyTarget.CreateOrGetExisting(shared.Vessel.mainBody, shared);
            Vector bottomInShipRaw = FurthestCorner(new Vector(-shared.Vessel.upAxis));
            return body.AltitudeFromPosition(bottomInShipRaw);
        }

        public double BottomAltitudeRadar()
        {
            BodyTarget body = BodyTarget.CreateOrGetExisting(shared.Vessel.mainBody, shared);
            Vector bottomInShipRaw = FurthestCorner(new Vector(-shared.Vessel.upAxis));
            return body.RadarAltitudeFromPosition(bottomInShipRaw);
        }

        public void InitializeSuffixes()
        {
            AddSuffix("ABSORIGIN", new SetSuffix<Vector>(() => Origin, value => Origin = value));
            AddSuffix("FACING", new SetSuffix<Direction>(() => Facing, value => Facing = value));
            AddSuffix("RELMIN", new SetSuffix<Vector>(() => RelativeMin, value => RelativeMin = value));
            AddSuffix("RELMAX", new SetSuffix<Vector>(() => RelativeMax, value => RelativeMax = value));
            AddSuffix("ABSMIN", new NoArgsSuffix<Vector>(() => AbsoluteMin));
            AddSuffix("ABSMAX", new NoArgsSuffix<Vector>(() => AbsoluteMax));
            AddSuffix("RELCENTER", new NoArgsSuffix<Vector>(() => RelativeCenter));
            AddSuffix("ABSCENTER", new NoArgsSuffix<Vector>(() => AbsoluteCenter));
            AddSuffix("EXTENTS", new SetSuffix<Vector>(() => new Vector(unityBounds.extents), value => unityBounds.extents = value.ToVector3()));
            AddSuffix("SIZE", new SetSuffix<Vector>(() => new Vector(unityBounds.size), value => unityBounds.size = value.ToVector3()));
            AddSuffix("FURTHESTCORNER", new OneArgsSuffix<Vector,Vector>((ray) => FurthestCorner(ray)));
            AddSuffix("BOTTOMALT", new NoArgsSuffix<ScalarValue>(() => BottomAltitude()));
            AddSuffix("BOTTOMALTRADAR", new NoArgsSuffix<ScalarValue>(() => BottomAltitudeRadar()));
        }

        public override string ToString()
        {
            return string.Format("Bounds: ABSORIGIN = {0}, FACING = {1}, RELMIN = {2}, RELMAX = {3}", Origin, Facing, RelativeMin, RelativeMax);
        }
    }
}
