using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AIJam.Validation {
    /// <summary>Shape of one collider or RobotView renderer inside a part, measured in the part root's space.</summary>
    public class ShapePrint {
        public string Name;
        public Type Type;
        public Mesh Mesh;
        public Vector3 LocalPos;
        public Quaternion LocalRot;
        public Vector3 Scale;          // per-axis length of the object's axes in part-root space (mirror-free)
        public bool Enabled;
        public bool IsTrigger;
        public bool HasZone;
        public string ZoneKeyword;
        public PhysicsMaterial Material;
        public Material RenderMaterial;
        public bool Used;

        public ShapePrint Clone() => (ShapePrint)MemberwiseClone();
    }

    /// <summary>Fingerprint of one provided part prefab (collider meshes, offsets, sizes, RobotView copies).</summary>
    public class PartPrint {
        public GameObject Prefab;
        public string Group;          // "" for sensors / template pseudo-parts
        public bool Fixed;            // sensors and ButtMarker: 0 % size change
        public Type RootType;
        public List<ShapePrint> Colliders = new List<ShapePrint>();
        public List<ShapePrint> Views = new List<ShapePrint>();
        public Rigidbody Body;
        public HingeJoint Hinge;

        public IEnumerable<string> MeshKeys => Colliders.Select(c => c.Mesh != null ? c.Mesh.name : c.Type.Name + ":" + c.Name);
    }

    /// <summary>Builds part fingerprints from the prefabs listed in ValidationRules.</summary>
    public class PartCatalog {
        public static readonly Type[] PartRootTypes = {
            typeof(BodyComponent), typeof(ArmComponent), typeof(WheelComponent), typeof(BumperComponent),
            typeof(ProximitySensorRay), typeof(ButtSensorComponent)
        };

        public readonly List<PartPrint> Parts = new List<PartPrint>();
        public PartPrint ButtMarker;

        public PartCatalog(ValidationRules rules) {
            defaultOverlap.Clear(); // a part prefab may have been edited since the last check
            foreach (var g in rules.Groups)
                foreach (var e in g.Parts)
                    if (e.Prefab != null) Parts.Add(Build(e.Prefab.transform, e.Prefab, g.Name, false));
            foreach (var s in rules.SensorPrefabs)
                if (s != null) Parts.Add(Build(s.transform, s, "", true));
            var marker = rules.RobotTemplate != null ? rules.RobotTemplate.transform.Find("ButtMarker") : null;
            if (marker != null) ButtMarker = Build(marker, rules.RobotTemplate, "", true, rules.RobotTemplate.transform);
        }

        /// <summary>Nearest ancestor (or self) that is a part root.</summary>
        public static Transform PartRootOf(Transform t) {
            for (var c = t; c != null; c = c.parent)
                if (PartRootTypes.Any(type => c.GetComponent(type) != null)) return c;
            return null;
        }

        public static Type RootTypeOf(Transform partRoot) =>
            PartRootTypes.FirstOrDefault(type => partRoot.GetComponent(type) != null);

        /// <summary>Matches a part instance to a provided prefab by root type and collider meshes.</summary>
        public PartPrint Identify(Transform partRoot) {
            var type = RootTypeOf(partRoot);
            var keys = OwnColliders(partRoot).Select(c => c is MeshCollider mc && mc.sharedMesh != null ? mc.sharedMesh.name : c.GetType().Name + ":" + c.name)
                .OrderBy(k => k).ToList();
            var candidates = Parts.Where(p => p.RootType == type).ToList();
            var exact = candidates.FirstOrDefault(p => p.MeshKeys.OrderBy(k => k).SequenceEqual(keys));
            if (exact != null) return exact;
            // best effort: most shared meshes (so the report can name what is wrong)
            return candidates.OrderByDescending(p => p.MeshKeys.Intersect(keys).Count()).FirstOrDefault(p => p.MeshKeys.Intersect(keys).Any());
        }

        /// <summary>Colliders whose nearest part root is this part (excludes nested parts).</summary>
        public static IEnumerable<Collider> OwnColliders(Transform partRoot) =>
            partRoot.GetComponentsInChildren<Collider>(true).Where(c => !(c is WheelCollider) && PartRootOf(c.transform) == partRoot);

        public static IEnumerable<Renderer> OwnViews(Transform partRoot, int robotView) =>
            partRoot.GetComponentsInChildren<Renderer>(true).Where(r => r.gameObject.layer == robotView && PartRootOf(r.transform) == partRoot);

        /// <param name="scaleSpace">Space sizes are measured in: null = the prefab's own space (includes the part root's scale).</param>
        private static PartPrint Build(Transform root, GameObject prefab, string group, bool isFixed, Transform scaleSpace = null) {
            int robotView = LayerMask.NameToLayer(VisionLayers.RobotViewName);
            var p = new PartPrint {
                Prefab = prefab, Group = group, Fixed = isFixed,
                RootType = RootTypeOf(root),
                Body = root.GetComponent<Rigidbody>(),
                Hinge = root.GetComponent<HingeJoint>(),
            };
            var cols = p.RootType != null ? OwnColliders(root) : root.GetComponentsInChildren<Collider>(true);
            foreach (var c in cols) p.Colliders.Add(Print(c.transform, root, c, null, scaleSpace, true));
            var views = p.RootType != null ? OwnViews(root, robotView) : root.GetComponentsInChildren<Renderer>(true).Where(r => r.gameObject.layer == robotView);
            foreach (var r in views) p.Views.Add(Print(r.transform, root, null, r, scaleSpace, true));
            return p;
        }

        /// <param name="fromPrefab">true: size in scaleSpace (or prefab world); false: size relative to scaleSpace (robot root).</param>
        public static ShapePrint Print(Transform t, Transform partRoot, Collider c = null, Renderer r = null, Transform scaleSpace = null, bool fromPrefab = false) {
            var s = new ShapePrint {
                Name = t.name,
                LocalPos = RelPos(t, partRoot),
                LocalRot = RelRot(t, partRoot),
                Scale = scaleSpace != null ? ScaleIn(t, scaleSpace) : WorldScale(t),
            };
            if (c != null) {
                s.Type = c.GetType();
                s.Mesh = c is MeshCollider mc ? mc.sharedMesh : null;
                s.Enabled = c.enabled;
                s.IsTrigger = c.isTrigger;
                var zone = c.GetComponent<CollisionZoneBehaviour>();
                s.HasZone = zone != null && zone.enabled;
                s.ZoneKeyword = ZoneKeyword(t.name);
                s.Material = c.sharedMaterial;
            }
            if (r != null) {
                s.Type = r.GetType();
                var mf = r.GetComponent<MeshFilter>();
                s.Mesh = mf != null ? mf.sharedMesh : null;
                s.Enabled = r.enabled;
                s.RenderMaterial = r.sharedMaterial;
            }
            return s;
        }

        /// <summary>Position of t in root space (works under mirrored parents).</summary>
        public static Vector3 RelPos(Transform t, Transform root) => (root.worldToLocalMatrix * t.localToWorldMatrix).GetColumn(3);

        /// <summary>Rotation of t in root space, built from axis directions so mirrored parents don't break it.</summary>
        public static Quaternion RelRot(Transform t, Transform root) {
            var m = root.worldToLocalMatrix * t.localToWorldMatrix;
            Vector3 fwd = m.GetColumn(2), up = m.GetColumn(1);
            if (fwd.sqrMagnitude < 1e-12f || up.sqrMagnitude < 1e-12f) return Quaternion.identity;
            return Quaternion.LookRotation(fwd.normalized, up.normalized);
        }

        private static readonly Dictionary<(GameObject, GameObject), float> defaultOverlap = new Dictionary<(GameObject, GameObject), float>();

        /// <summary>
        /// How deep two part prefabs already sink into each other when both sit where they are made to sit
        /// (the same origin), in metres. Measured once per pair on temporary copies; nothing is kept.
        /// </summary>
        public static float DefaultOverlap(GameObject prefabA, GameObject prefabB, Func<Collider, bool> counts) {
            if (prefabA == null || prefabB == null) return 0f;
            if (defaultOverlap.TryGetValue((prefabA, prefabB), out float known)) return known;
            var far = new Vector3(0f, 1000f, 0f); // away from anything in the open scene
            var a = UnityEngine.Object.Instantiate(prefabA, far, Quaternion.identity);
            var b = UnityEngine.Object.Instantiate(prefabB, far, Quaternion.identity);
            a.hideFlags = b.hideFlags = HideFlags.HideAndDontSave;
            float deepest = 0f;
            try {
                Physics.SyncTransforms();
                foreach (var ca in OwnColliders(a.transform).Where(counts))
                    foreach (var cb in OwnColliders(b.transform).Where(counts))
                        if (Physics.ComputePenetration(ca, ca.transform.position, ca.transform.rotation, cb, cb.transform.position, cb.transform.rotation, out _, out float d))
                            deepest = Mathf.Max(deepest, d);
            } finally {
                UnityEngine.Object.DestroyImmediate(a);
                UnityEngine.Object.DestroyImmediate(b);
            }
            defaultOverlap[(prefabA, prefabB)] = deepest;
            return deepest;
        }

        /// <summary>Directions of t's local x, y and z axes in root space (unit length; a mirrored axis points the other way).</summary>
        public static Vector3[] AxesIn(Transform t, Transform root) {
            var m = root.worldToLocalMatrix * t.localToWorldMatrix;
            return new[] { ((Vector3)m.GetColumn(0)).normalized, ((Vector3)m.GetColumn(1)).normalized, ((Vector3)m.GetColumn(2)).normalized };
        }

        public static Vector3 WorldScale(Transform t) {
            var m = t.localToWorldMatrix;
            return new Vector3(((Vector3)m.GetColumn(0)).magnitude, ((Vector3)m.GetColumn(1)).magnitude, ((Vector3)m.GetColumn(2)).magnitude);
        }

        /// <summary>Length of each local axis of t, expressed in root space (mirroring removed).</summary>
        public static Vector3 ScaleIn(Transform t, Transform root) {
            var m = root.worldToLocalMatrix * t.localToWorldMatrix;
            return new Vector3(((Vector3)m.GetColumn(0)).magnitude, ((Vector3)m.GetColumn(1)).magnitude, ((Vector3)m.GetColumn(2)).magnitude);
        }

        /// <summary>Same order as CollisionForwarder.AssignDamageAndResistance.</summary>
        public static string ZoneKeyword(string objectName) {
            foreach (var k in new[] { "body", "battery", "wheels", "armbase", "weapon", "bumper" })
                if (objectName.Contains(k)) return k;
            return "other";
        }
    }
}
