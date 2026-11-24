using System.Collections.Generic;
using System.Linq;
using Turnaj;
using UnityEngine;

namespace AIJam.Validation {
    /// <summary>
    /// Checks an arena instance (e.g. loaded prefab contents) against ValidationRules. Pure: changes nothing.
    /// Rules: README.md 6.3.
    /// </summary>
    public static class ArenaValidator {
        /// <param name="isReference">true for the neutral arena itself: only origin and spawns are checked.</param>
        public static List<Finding> Validate(GameObject arena, ValidationRules rules, bool isReference = false) {
            var f = new List<Finding>();
            if (arena == null) { f.Add(new Finding(Severity.Error, "Arena", "Arena prefab is missing.")); return f; }
            if (rules == null) { f.Add(new Finding(Severity.Error, "Rules", "ValidationRules asset is missing.")); return f; }
            var root = arena.transform;
            string P(Transform t) => Findings.PathOf(t, root);
            void Err(string rule, string msg, Transform t = null) => f.Add(new Finding(Severity.Error, rule, msg, P(t)));
            void Warn(string rule, string msg, Transform t = null) => f.Add(new Finding(Severity.Warning, rule, msg, P(t)));

            // 1. origin
            if (root.position.sqrMagnitude > 1e-6f || Vector3.Distance(root.localScale, Vector3.one) > 1e-4f)
                Err("Origin", "The arena root must be centred at (0, 0, 0) with scale (1, 1, 1).");

            // 3. spawns
            var spawns = root.GetComponentsInChildren<SpawnPoint>(true);
            var home = spawns.Where(s => s.Side == SpawnSide.Home).ToList();
            var guest = spawns.Where(s => s.Side == SpawnSide.Guest).ToList();
            if (home.Count != 1) Err("Spawns", $"Needs exactly one Home SpawnPoint, found {home.Count}.");
            if (guest.Count != 1) Err("Spawns", $"Needs exactly one Guest SpawnPoint, found {guest.Count}.");
            foreach (var s in spawns) {
                float y = root.InverseTransformPoint(s.transform.position).y;
                if (Mathf.Abs(y) > rules.SpawnMaxHeight) Err("Spawns", $"{s.Side} SpawnPoint is {y:F2} m from floor level.", s.transform);
            }
            if (home.Count == 1 && guest.Count == 1) {
                Vector3 h = root.InverseTransformPoint(home[0].transform.position), g = root.InverseTransformPoint(guest[0].transform.position);
                if (new Vector2(h.x + g.x, h.z + g.z).magnitude > rules.SpawnSymmetryTolerance)
                    Err("Spawns", $"Home and Guest are not symmetric across the centre (Home {Flat(h)}, Guest {Flat(g)}; Guest must be at −Home).", guest[0].transform);
                float yaw = Mathf.Abs(Mathf.DeltaAngle(home[0].transform.eulerAngles.y + 180f, guest[0].transform.eulerAngles.y));
                if (yaw > rules.SpawnYawToleranceDegrees)
                    Err("Spawns", $"Guest must face the opposite way to Home (off by {yaw:F1}°).", guest[0].transform);
            }
            if (isReference) return f;

            // 2. solid colliders must equal the reference arena's
            var solid = root.GetComponentsInChildren<Collider>(true).Where(c => c.enabled && !c.isTrigger).ToList();
            if (rules.ReferenceArena == null) {
                Err("Rules", "No reference arena set in ValidationRules.");
            } else {
                var want = rules.ReferenceArena.GetComponentsInChildren<Collider>(true).Where(c => c.enabled && !c.isTrigger).ToList();
                var used = new HashSet<Collider>();
                foreach (var c in solid) {
                    var match = want.FirstOrDefault(w => !used.Contains(w) && SameShape(c, root, w, rules.ReferenceArena.transform, rules));
                    if (match != null) { used.Add(match); continue; }
                    Err("Ground", $"Solid collider '{c.name}' isn't part of the standard arena ground. Hard obstacles aren't allowed – make it a trigger.", c.transform);
                }
                foreach (var w in want.Where(w => !used.Contains(w)))
                    Err("Ground", $"The arena ground is missing '{w.name}' (same floor and wall as the neutral arena, same size and position).");
            }
            foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true).Where(rb => rb.GetComponentsInChildren<Collider>(true).Any(c => c.enabled && !c.isTrigger)))
                Err("Ground", $"'{rb.name}' is a physics object with solid colliders; only triggers are allowed.", rb.transform);
            foreach (var a in root.GetComponentsInChildren<RobotAgent>(true))
                Err("Arena", $"The arena contains a robot ('{a.name}').", a.transform);

            // 4. vision
            int robotView = LayerMask.NameToLayer(VisionLayers.RobotViewName);
            var triggers = root.GetComponentsInChildren<Collider>(true).Where(c => c.enabled && c.isTrigger).ToList();
            foreach (var r in root.GetComponentsInChildren<Renderer>(true).Where(r => r.gameObject.layer == robotView)) {
                var role = rules.Palette != null ? rules.Palette.RoleOf(r.sharedMaterial) : null;
                foreach (var m in r.sharedMaterials.Skip(1))
                    if (rules.Palette != null && rules.Palette.RoleOf(m) == null) Err("Vision", $"'{r.name}' uses a non-palette material '{(m ? m.name : "none")}'.", r.transform);
                if (role == null) { Err("Vision", $"'{r.name}' is on RobotView with a non-palette material '{(r.sharedMaterial ? r.sharedMaterial.name : "none")}'.", r.transform); continue; }
                switch (role.Value) {
                    case VisionRole.Default:
                        Err("Vision", $"'{r.name}' is a black RobotView object – arenas may show only trap (red) and buff (green) colours; the agent camera background is already black.", r.transform);
                        break;
                    case VisionRole.RobotPart:
                    case VisionRole.Battery:
                        Err("Vision", $"'{r.name}' uses the {role.Value} colour – robot and battery colours are not allowed in arenas.", r.transform);
                        break;
                    case VisionRole.Trap:
                    case VisionRole.Buff:
                        if (!OnTrigger(r, triggers))
                            Err("Vision", $"'{r.name}' shows a {role.Value.ToString().ToLower()} colour but has no trigger collider (fake {role.Value.ToString().ToLower()}).", r.transform);
                        break;
                }
            }

            // 5. custom scripts for manual review
            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true)) {
                if (mb == null) { Err("Scripts", "A component has a missing script."); continue; }
                var t = mb.GetType();
                if (RobotValidator.IsEngineType(t) || t == typeof(SpawnPoint) || t == typeof(ArenaViewAngle)) continue;
                Warn("Scripts", $"Custom script '{t.Name}' (trap/buff/effect?) – review by hand.", mb.transform);
            }
            return f;
        }

        private static bool SameShape(Collider c, Transform root, Collider w, Transform refRoot, ValidationRules rules) {
            if (c.GetType() != w.GetType()) return false;
            if (c is MeshCollider mc && w is MeshCollider mw && mc.sharedMesh != mw.sharedMesh) return false;
            if (Vector3.Distance(PartCatalog.RelPos(c.transform, root), PartCatalog.RelPos(w.transform, refRoot)) > rules.ArenaPositionTolerance) return false;
            if (Quaternion.Angle(PartCatalog.RelRot(c.transform, root), PartCatalog.RelRot(w.transform, refRoot)) > 0.5f) return false;
            var a = PartCatalog.ScaleIn(c.transform, root);
            var b = PartCatalog.ScaleIn(w.transform, refRoot);
            for (int i = 0; i < 3; i++) if (Mathf.Abs(a[i] - b[i]) > rules.ArenaScaleTolerance * Mathf.Max(1f, b[i])) return false;
            if (c is BoxCollider bc && w is BoxCollider bw && (bc.size != bw.size || bc.center != bw.center)) return false;
            if (c is SphereCollider sc && w is SphereCollider sw && (sc.radius != sw.radius || sc.center != sw.center)) return false;
            if (c is CapsuleCollider cc && w is CapsuleCollider cw && (cc.radius != cw.radius || cc.height != cw.height || cc.center != cw.center)) return false;
            return true;
        }

        /// <summary>The renderer sits on/under a trigger collider's object, or its centre is inside one.</summary>
        private static bool OnTrigger(Renderer r, List<Collider> triggers) {
            foreach (var t in triggers) {
                if (r.transform.IsChildOf(t.transform) || t.transform.IsChildOf(r.transform)) return true;
                var centre = r.bounds.center;
                if (t.bounds.Contains(centre)) {
                    if (t is MeshCollider mc && !mc.convex) return true; // ClosestPoint needs convex; bounds is enough
                    if (Vector3.Distance(t.ClosestPoint(centre), centre) < 1e-3f) return true;
                }
            }
            return false;
        }

        private static string Flat(Vector3 v) => $"({v.x:F2}, {v.z:F2})";
    }
}
