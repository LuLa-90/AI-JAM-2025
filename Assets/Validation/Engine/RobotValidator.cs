using System;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents.Policies;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AIJam.Validation {
    /// <summary>
    /// Checks a robot instance (e.g. loaded prefab contents) against ValidationRules. Pure: changes nothing.
    /// Rules: README.md 6.2. Parts are identified by collider mesh, never by name.
    /// </summary>
    public static class RobotValidator {
        public static List<Finding> Validate(GameObject robot, ValidationRules rules) {
            var f = new List<Finding>();
            if (robot == null) { f.Add(new Finding(Severity.Error, "Robot", "Robot prefab is missing.")); return f; }
            if (rules == null) { f.Add(new Finding(Severity.Error, "Rules", "ValidationRules asset is missing.")); return f; }
            var ctx = new Ctx { Root = robot.transform, Rules = rules, F = f, Catalog = new PartCatalog(rules),
                RobotView = LayerMask.NameToLayer(VisionLayers.RobotViewName) };
            CheckRoot(ctx);
            var parts = CheckStructure(ctx);
            CheckColliders(ctx, parts);
            CheckPhysics(ctx, parts);
            CheckAttachment(ctx, parts);
            CheckVision(ctx, parts);
            CheckScripts(ctx);
            return f;
        }

        private class Ctx {
            public Transform Root;
            public ValidationRules Rules;
            public List<Finding> F;
            public PartCatalog Catalog;
            public int RobotView;
            public string P(Transform t) => Findings.PathOf(t, Root);
            public void Err(string rule, string msg, Transform t = null) => F.Add(new Finding(Severity.Error, rule, msg, P(t)));
            public void Warn(string rule, string msg, Transform t = null) => F.Add(new Finding(Severity.Warning, rule, msg, P(t)));
        }

        private class PartInstance {
            public Transform Root;
            public PartPrint Print;
            public string Group => Print?.Group ?? "";
        }

        // ---------------------------------------------------------------- 1. root
        private static void CheckRoot(Ctx c) {
            var go = c.Root.gameObject;
            if (go.GetComponent<RobotAgent>() == null) c.Err("Root", "Robot root has no RobotAgent.");
            if (go.GetComponent<RobotMovement>() == null) c.Err("Root", "Robot root has no RobotMovement.");
            if (go.GetComponent<Rigidbody>() == null) c.Err("Root", "Robot root has no Rigidbody.");
            if (go.GetComponent<CollisionForwarder>() == null) c.Err("Root", "Robot root has no CollisionForwarder (damage zones would not work).");
            if (Vector3.Distance(c.Root.localScale, Vector3.one) > 1e-4f) c.Err("Size", "The robot root must have scale (1, 1, 1); resize parts instead.");
            var bp = go.GetComponent<BehaviorParameters>();
            if (bp == null) { c.Err("Root", "Robot root has no BehaviorParameters."); return; }
            if (bp.Model == null) c.Err("Model", "No trained model assigned in Behavior Parameters.");
            if (!string.IsNullOrEmpty(c.Rules.BehaviorName) && bp.BehaviorName != c.Rules.BehaviorName)
                c.Err("Model", $"Behavior Name is '{bp.BehaviorName}', must be '{c.Rules.BehaviorName}' (trainer config).");
            if (c.Rules.VectorObservationSize > 0 && bp.BrainParameters.VectorObservationSize != c.Rules.VectorObservationSize)
                c.Err("Model", $"Vector observation size is {bp.BrainParameters.VectorObservationSize}, must be {c.Rules.VectorObservationSize}.");
        }

        // ---------------------------------------------------------------- 2–3. structure and part counts
        private static List<PartInstance> CheckStructure(Ctx c) {
            var roots = c.Root.GetComponentsInChildren<Transform>(true)
                .Where(t => PartCatalog.RootTypeOf(t) != null).ToList();
            var parts = roots.Select(r => new PartInstance { Root = r, Print = c.Catalog.Identify(r) }).ToList();

            foreach (var p in parts.Where(p => p.Print == null))
                c.Err("Parts", $"'{p.Root.name}' is not one of the provided Base Shapes parts (collider meshes don't match).", p.Root);

            var bodies = parts.Where(p => p.Root.GetComponent<BodyComponent>() != null).ToList();
            Transform body = bodies.Count == 1 ? bodies[0].Root : null;
            if (bodies.Count == 1 && body.parent != c.Root) c.Err("Structure", "The body part must be a direct child of the robot root.", body);
            if (body != null) {
                // wheels may hang anywhere in the robot (the template keeps them under the root); they only have to touch the body
                foreach (var p in parts.Where(p => p.Root != body && !p.Root.IsChildOf(body) && p.Root.GetComponent<WheelComponent>() == null))
                    c.Err("Structure", $"'{p.Root.name}' must be under the body ({body.name}).", p.Root);
            }

            // group min / max and per-prefab max
            foreach (var g in c.Rules.Groups) {
                int n = parts.Count(p => p.Group == g.Name);
                if (n < g.Min) c.Err("Parts", $"{g.Name}: {n} used, at least {g.Min} required.");
                if (n > g.Max) c.Err("Parts", $"{g.Name}: {n} used, at most {g.Max} allowed.");
                foreach (var e in g.Parts.Where(e => e.Prefab != null)) {
                    int u = parts.Count(p => p.Print != null && p.Print.Prefab == e.Prefab);
                    if (u > e.MaxUses) c.Err("Parts", $"{e.Prefab.name}: used {u}×, at most {e.MaxUses}× allowed.");
                }
            }

            // wheel containers: one wheel part each
            foreach (var side in new[] { "Wheels Left", "Wheels Right" }) {
                var containers = c.Root.GetComponentsInChildren<Transform>(true).Where(t => t.name == side).ToList();
                if (containers.Count != 1) { c.Err("Structure", $"Expected exactly one '{side}' container, found {containers.Count}."); continue; }
                var box = containers[0];
                int wheels = parts.Count(p => p.Root.GetComponent<WheelComponent>() != null && p.Root.IsChildOf(box));
                if (wheels != 1) c.Err("Structure", $"'{side}' must contain exactly one wheel part, found {wheels}.", box);
            }
            return parts;
        }

        // ---------------------------------------------------------------- 4–6. colliders
        private static void CheckColliders(Ctx c, List<PartInstance> parts) {
            var marker = c.Root.Find("ButtMarker");
            var markerPrint = c.Catalog.ButtMarker;

            foreach (var p in parts.Where(p => p.Print != null))
                MatchShapes(c, p.Root, p.Print, p.Print.Fixed ? 0.001f : GroupOf(c, p).ScaleTolerancePercent / 100f, PartCatalog.OwnColliders(p.Root).ToList(), $"{p.Print.Prefab.name}", lockPose: p.Print.Fixed,
                    group: p.Print.Fixed ? null : GroupOf(c, p));

            if (marker != null && markerPrint != null) {
                var cols = marker.GetComponentsInChildren<Collider>(true).ToList();
                MatchShapes(c, marker, markerPrint, 0.001f, cols, "ButtMarker", lockPose: true); // may move, not be resized
            } else if (markerPrint != null) {
                c.Err("Structure", "ButtMarker (rear target from the template) is missing.");
            }

            // every other collider is foreign (wheel colliders are checked in physics)
            foreach (var col in c.Root.GetComponentsInChildren<Collider>(true)) {
                if (col is WheelCollider) continue;
                if (PartCatalog.PartRootOf(col.transform) != null) continue;
                if (marker != null && col.transform.IsChildOf(marker)) continue;
                if (!col.enabled) continue;
                c.Err("Colliders", $"Collider '{col.name}' ({col.GetType().Name}) is not part of a provided part.", col.transform);
            }
        }

        private static readonly ValidationRules.PartGroup NoGroup = new ValidationRules.PartGroup { ScaleTolerancePercent = 0f };

        private static ValidationRules.PartGroup GroupOf(Ctx c, PartInstance p) => c.Rules.Groups.FirstOrDefault(g => g.Name == p.Group) ?? NoGroup;

        // Fixed colliders (sensors, ButtMarker) keep their pose inside the part. Where a part collider may sit
        // on the robot is set by its group's move limit (CheckPlace) and by the attachment/overlap check.
        private const float FixedOffset = 0.002f, FixedAngle = 0.5f;
        // An agent-view copy must sit on its collider.
        private const float ViewOffset = 0.001f, ViewAngle = 0.5f;

        private static void MatchShapes(Ctx c, Transform partRoot, PartPrint print, float tol, List<Collider> cols, string partName, bool lockPose, ValidationRules.PartGroup group = null) {
            var prints = print.Colliders.Select(s => s.Clone()).ToList();
            foreach (var col in cols) {
                var mesh = col is MeshCollider mc ? mc.sharedMesh : null;
                var fp = prints.FirstOrDefault(s => !s.Used && s.Type == col.GetType() && s.Mesh == mesh && (mesh != null || s.Name == col.name));
                if (fp == null) {
                    if (col.enabled) c.Err("Colliders", $"Collider '{col.name}' doesn't belong to {partName}.", col.transform);
                    continue;
                }
                fp.Used = true;
                var have = PartCatalog.Print(col.transform, partRoot, col);
                if (have.Enabled != fp.Enabled) c.Err("Colliders", $"'{col.name}' must be {(fp.Enabled ? "enabled" : "disabled")} (as in {partName}).", col.transform);
                if (have.IsTrigger != fp.IsTrigger) c.Err("Colliders", $"'{col.name}' trigger setting differs from {partName}.", col.transform);
                if (have.Material != fp.Material) c.Err("Physics", $"'{col.name}' uses physic material '{(have.Material ? have.Material.name : "none")}', expected '{(fp.Material ? fp.Material.name : "none")}'.", col.transform);
                if (lockPose && Vector3.Distance(have.LocalPos, fp.LocalPos) > FixedOffset)
                    c.Err("Colliders", $"'{col.name}' was moved inside {partName}; this collider is fixed.", col.transform);
                if (lockPose && Quaternion.Angle(have.LocalRot, fp.LocalRot) > FixedAngle)
                    c.Err("Colliders", $"'{col.name}' was rotated inside {partName}; this collider is fixed.", col.transform);
                bool battery = group != null && fp.ZoneKeyword == "battery";
                // size relative to the robot root (includes every parent's scale)
                var size = PartCatalog.ScaleIn(col.transform, c.Root);
                if (battery && !Within(size, fp.Scale, 0.001f))
                    c.Err("Size", $"'{col.name}' size {Pct(size, fp.Scale)} of {partName}; the battery must keep its size.", col.transform);
                else if (!Within(size, fp.Scale, tol))
                    c.Err("Size", $"'{col.name}' size {Pct(size, fp.Scale)} of {partName}; allowed ±{tol * 100f:0.#} %.", col.transform);
                if (group != null) CheckPlace(c, col, fp, group, battery, partName);
                if (fp.HasZone) {
                    var zone = col.GetComponent<CollisionZoneBehaviour>();
                    if (zone == null || !zone.enabled) c.Err("Zones", $"'{col.name}' lost its damage zone (CollisionZoneBehaviour).", col.transform);
                    var kw = PartCatalog.ZoneKeyword(col.name);
                    if (kw != fp.ZoneKeyword) c.Err("Zones", $"'{col.name}' is named like a '{kw}' zone; it must stay a '{fp.ZoneKeyword}' zone (damage depends on the name).", col.transform);
                } else if (col.GetComponent<CollisionZoneBehaviour>() != null) {
                    c.Err("Zones", $"'{col.name}' has a damage zone that {partName} doesn't have.", col.transform);
                }
            }
            foreach (var fp in prints.Where(s => !s.Used && s.Enabled))
                c.Err("Colliders", $"{partName} is missing its collider '{fp.Name}'.", partRoot);
        }

        /// <summary>
        /// Where a part collider sits on the robot, compared with where its prefab puts it. The part prefabs are
        /// made to sit at the body's origin, so the collider's pose in robot space is compared with its pose in the prefab.
        /// </summary>
        private static void CheckPlace(Ctx c, Collider col, ShapePrint fp, ValidationRules.PartGroup group, bool battery, string partName) {
            var pos = PartCatalog.RelPos(col.transform, c.Root);
            var rot = PartCatalog.RelRot(col.transform, c.Root);
            var axes = PartCatalog.AxesIn(col.transform, c.Root);
            // mirroring is allowed (the right wheel set is a mirrored left one), so an axis may point either way
            float turned = 0f;
            for (int i = 0; i < 3; i++) {
                float a = Vector3.Angle(axes[i], fp.LocalRot * Unit(i));
                turned = Mathf.Max(turned, Mathf.Min(a, 180f - a));
            }
            if (battery) {
                var d = pos - fp.LocalPos;
                float max = c.Rules.BatteryMoveMetres + 1e-4f;
                if (Mathf.Abs(d.x) > max || Mathf.Abs(d.y) > max || Mathf.Abs(d.z) > max)
                    c.Err("Position", $"'{col.name}' is moved by {Cm(d)} from its place in {partName}; the battery may move ±{c.Rules.BatteryMoveMetres * 100f:0.#} cm on each axis.", col.transform);
                if (turned > FixedAngle) c.Err("Position", $"'{col.name}' is rotated by {turned:0.#}°; the battery must keep its rotation.", col.transform);
                return;
            }
            switch (group.Move) {
                case ValidationRules.MoveLimit.Fixed:
                    if (Vector3.Distance(pos, fp.LocalPos) > FixedOffset)
                        c.Err("Position", $"'{col.name}' is moved by {Cm(pos - fp.LocalPos)} from its place in {partName}; {group.Name} must stay where the prefab puts it.", col.transform);
                    if (turned > FixedAngle)
                        c.Err("Position", $"'{col.name}' is rotated by {turned:0.#}°; {group.Name} must keep the prefab's rotation.", col.transform);
                    break;
                case ValidationRules.MoveLimit.PercentOfSize: {
                    // the limit follows the part: the shift is measured along the collider's own axes, so a turned part keeps its limits
                    var turn = rot * Quaternion.Inverse(fp.LocalRot);
                    var off = pos - turn * fp.LocalPos;
                    var d = new Vector3(Vector3.Dot(off, axes[0]), Vector3.Dot(off, axes[1]), Vector3.Dot(off, axes[2]));
                    var size = Vector3.Scale(fp.Mesh != null ? fp.Mesh.bounds.size : Vector3.one, fp.Scale);
                    var max = size * (group.MovePercentOfSize / 100f);
                    if (Mathf.Abs(d.x) > max.x + 1e-4f || Mathf.Abs(d.y) > max.y + 1e-4f || Mathf.Abs(d.z) > max.z + 1e-4f)
                        c.Err("Position", $"'{col.name}' is moved by {Cm(d)} along its own axes; allowed ±{Cm(max)} ({group.MovePercentOfSize:0.#} % of its size).", col.transform);
                    break;
                }
            }
        }

        private static string Cm(Vector3 v) => $"({v.x * 100f:0.##}, {v.y * 100f:0.##}, {v.z * 100f:0.##}) cm";

        private static Vector3 Unit(int axis) => axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;

        // ---------------------------------------------------------------- 9. locked physics
        private static void CheckPhysics(Ctx c, List<PartInstance> parts) {
            var tpl = c.Rules.RobotTemplate;
            var rootRb = c.Root.GetComponent<Rigidbody>();
            if (tpl != null && rootRb != null) CompareBody(c, rootRb, tpl.GetComponent<Rigidbody>(), "Robot root");

            var arms = parts.Where(p => p.Root.GetComponent<ArmComponent>() != null && p.Print != null).ToList();
            foreach (var rb in c.Root.GetComponentsInChildren<Rigidbody>(true)) {
                if (rb == rootRb) continue;
                var arm = arms.FirstOrDefault(a => a.Root == rb.transform);
                if (arm == null) { c.Err("Physics", $"Extra Rigidbody on '{rb.name}' is not allowed.", rb.transform); continue; }
                CompareBody(c, rb, arm.Print.Body, arm.Print.Prefab.name);
            }
            foreach (var j in c.Root.GetComponentsInChildren<Joint>(true)) {
                var arm = arms.FirstOrDefault(a => a.Root == j.transform);
                if (arm == null || !(j is HingeJoint h)) { c.Err("Physics", $"Extra joint ({j.GetType().Name}) on '{j.name}' is not allowed.", j.transform); continue; }
                CompareHinge(c, h, arm.Print.Hinge, arm.Print.Prefab.name);
            }

            // wheel colliders: same count and settings as the template
            var tw = tpl != null ? tpl.GetComponentsInChildren<WheelCollider>(true) : new WheelCollider[0];
            var rw = c.Root.GetComponentsInChildren<WheelCollider>(true);
            if (tw.Length > 0 && rw.Length != tw.Length) c.Err("Physics", $"{rw.Length} WheelColliders, the template has {tw.Length}.");
            foreach (var w in rw) {
                var parent = w.transform.parent;
                if (parent == null || (parent.name != "Wheels Left" && parent.name != "Wheels Right"))
                    c.Err("Structure", $"WheelCollider '{w.name}' must be directly under 'Wheels Left' or 'Wheels Right'.", w.transform);
                var reference = tw.FirstOrDefault(t => t.name == w.name) ?? tw.FirstOrDefault();
                if (reference != null) CompareWheel(c, w, reference);
            }
        }

        private static void CompareBody(Ctx c, Rigidbody have, Rigidbody want, string what) {
            if (want == null) return;
            void Num(string n, float a, float b) { if (!Same(c, a, b)) c.Err("Physics", $"{what} Rigidbody {n} is {a}, must be {b}.", have.transform); }
            Num("mass", have.mass, want.mass);
            Num("linear damping", have.linearDamping, want.linearDamping);
            Num("angular damping", have.angularDamping, want.angularDamping);
            if (have.useGravity != want.useGravity) c.Err("Physics", $"{what} Rigidbody gravity setting changed.", have.transform);
            if (have.constraints != want.constraints) c.Err("Physics", $"{what} Rigidbody constraints changed.", have.transform);
            if (have.automaticCenterOfMass != want.automaticCenterOfMass ||
                Vector3.Distance(Stored(have, "m_CenterOfMass", have.centerOfMass), Stored(want, "m_CenterOfMass", want.centerOfMass)) > 1e-4f)
                c.Err("Physics", $"{what} Rigidbody centre of mass changed.", have.transform);
            if (have.automaticInertiaTensor != want.automaticInertiaTensor) c.Err("Physics", $"{what} Rigidbody inertia tensor setting changed.", have.transform);
        }

        private static void CompareHinge(Ctx c, HingeJoint have, HingeJoint want, string what) {
            if (want == null) return;
            bool same = have.useSpring == want.useSpring && have.useMotor == want.useMotor && have.useLimits == want.useLimits
                && Same(c, have.spring.spring, want.spring.spring) && Same(c, have.spring.damper, want.spring.damper)
                && Same(c, have.spring.targetPosition, want.spring.targetPosition)
                && Same(c, have.motor.force, want.motor.force) && Same(c, have.motor.targetVelocity, want.motor.targetVelocity)
                && Same(c, have.limits.min, want.limits.min) && Same(c, have.limits.max, want.limits.max)
                && Vector3.Distance(have.axis, want.axis) < 1e-4f;
            if (!same) c.Err("Physics", $"{what} HingeJoint spring/motor/limits/axis differ from the prefab.", have.transform);
        }

        private static void CompareWheel(Ctx c, WheelCollider have, WheelCollider want) {
            bool Fr(WheelFrictionCurve a, WheelFrictionCurve b) => Same(c, a.extremumSlip, b.extremumSlip) && Same(c, a.extremumValue, b.extremumValue)
                && Same(c, a.asymptoteSlip, b.asymptoteSlip) && Same(c, a.asymptoteValue, b.asymptoteValue) && Same(c, a.stiffness, b.stiffness);
            bool same = Same(c, have.mass, want.mass) && Same(c, have.radius, want.radius) && Same(c, have.wheelDampingRate, want.wheelDampingRate)
                && Same(c, have.suspensionDistance, want.suspensionDistance) && Same(c, have.forceAppPointDistance, want.forceAppPointDistance)
                && Same(c, have.suspensionSpring.spring, want.suspensionSpring.spring) && Same(c, have.suspensionSpring.damper, want.suspensionSpring.damper)
                && Same(c, have.suspensionSpring.targetPosition, want.suspensionSpring.targetPosition)
                && Fr(have.forwardFriction, want.forwardFriction) && Fr(have.sidewaysFriction, want.sidewaysFriction);
            if (!same) c.Err("Physics", $"WheelCollider '{have.name}' settings (radius, suspension, friction, mass) differ from the template.", have.transform);
            if (!have.enabled) c.Err("Physics", $"WheelCollider '{have.name}' is disabled.", have.transform);
        }

        // ---------------------------------------------------------------- 7. attachment
        private static void CheckAttachment(Ctx c, List<PartInstance> parts) {
            var body = parts.FirstOrDefault(p => p.Root.GetComponent<BodyComponent>() != null);
            if (body == null) return;
            var bodyCols = PartCatalog.OwnColliders(body.Root).Where(Solid).ToList();
            var bumpers = parts.Where(p => p.Root.GetComponent<BumperComponent>() != null && p.Print != null).ToList();

            foreach (var p in parts.Where(p => p.Group != "" && p != body)) {
                var own = PartCatalog.OwnColliders(p.Root).Where(Solid).ToList();
                if (p.Root.GetComponent<ArmComponent>() != null) {
                    var weapon = own.Where(x => PartCatalog.ZoneKeyword(x.name) == "weapon").ToList();
                    own = own.Where(x => PartCatalog.ZoneKeyword(x.name) == "armbase").ToList();
                    float grip = 0f;
                    foreach (var a in weapon) foreach (var b in own) grip = Mathf.Max(grip, Penetration(a, b));
                    if (grip < c.Rules.MinAttachmentPenetration)
                        c.Err("Attachment", $"The weapon of '{p.Root.name}' doesn't touch its arm base.", p.Root);
                }
                float deepest = 0f;
                foreach (var a in own) foreach (var b in bodyCols) deepest = Mathf.Max(deepest, Penetration(a, b));
                if (deepest < c.Rules.MinAttachmentPenetration)
                    c.Err("Attachment", $"'{p.Root.name}' doesn't touch the body; every part must be attached.", p.Root);
                if (p.Root.GetComponent<WheelComponent>() != null && p.Print != null) {
                    // The prefabs already overlap a little where they are made to sit; only overlap beyond that counts.
                    float maxBody = PartCatalog.DefaultOverlap(p.Print.Prefab, body.Print?.Prefab, Solid) + c.Rules.MaxWheelBodyPenetration;
                    if (deepest > maxBody)
                        c.Err("Attachment", $"'{p.Root.name}' sinks {deepest:F3} m into the body (max {maxBody:F3}: the prefabs' own overlap + {c.Rules.MaxWheelBodyPenetration}).", p.Root);
                    foreach (var bumper in bumpers) {
                        float bump = 0f;
                        foreach (var a in own) foreach (var b in PartCatalog.OwnColliders(bumper.Root).Where(Solid)) bump = Mathf.Max(bump, Penetration(a, b));
                        float maxBump = PartCatalog.DefaultOverlap(p.Print.Prefab, bumper.Print.Prefab, Solid) + c.Rules.MaxWheelBumperPenetration;
                        if (bump > maxBump)
                            c.Err("Attachment", $"'{p.Root.name}' sinks {bump:F3} m into bumper '{bumper.Root.name}' (max {maxBump:F3}: the prefabs' own overlap + {c.Rules.MaxWheelBumperPenetration}). Move the bumper away from the wheels.", p.Root);
                    }
                }
            }
        }

        private static bool Solid(Collider col) => col.enabled && !col.isTrigger;

        private static float Penetration(Collider a, Collider b) {
            return Physics.ComputePenetration(a, a.transform.position, a.transform.rotation, b, b.transform.position, b.transform.rotation, out _, out float d) ? d : 0f;
        }

        // ---------------------------------------------------------------- 8. vision
        private static void CheckVision(Ctx c, List<PartInstance> parts) {
            foreach (var cam in c.Root.GetComponentsInChildren<Camera>(true).Where(x => x.CompareTag("RobotCamera"))) {
                if (cam.cullingMask != VisionLayers.AgentCameraMask)
                    c.Warn("Vision", "The robot camera should see only the RobotView layer; spawners force this, but set it in the prefab too.", cam.transform);
            }
            var byRoot = parts.Where(p => p.Print != null).ToDictionary(p => p.Root);
            foreach (var r in c.Root.GetComponentsInChildren<Renderer>(true).Where(x => x.gameObject.layer == c.RobotView)) {
                var partRoot = PartCatalog.PartRootOf(r.transform);
                if (partRoot == null || !byRoot.TryGetValue(partRoot, out var part)) {
                    c.Err("Vision", $"'{r.name}' is on RobotView but isn't a part's own agent-view copy (no extra shapes).", r.transform);
                    continue;
                }
                var mesh = r.GetComponent<MeshFilter>() != null ? r.GetComponent<MeshFilter>().sharedMesh : null;
                var fp = part.Print.Views.FirstOrDefault(v => v.Mesh == mesh);
                if (fp == null) { c.Err("Vision", $"'{r.name}' is not one of {part.Print.Prefab.name}'s agent-view copies.", r.transform); continue; }
                // same pose and size as the collider with the same mesh
                var col = PartCatalog.OwnColliders(partRoot).FirstOrDefault(x => x is MeshCollider mc && mc.sharedMesh == mesh);
                if (col != null) {
                    var rv = PartCatalog.Print(r.transform, c.Root, null, r);
                    var cv = PartCatalog.Print(col.transform, c.Root, col);
                    if (Vector3.Distance(rv.LocalPos, cv.LocalPos) > ViewOffset || Quaternion.Angle(rv.LocalRot, cv.LocalRot) > ViewAngle)
                        c.Err("Vision", $"Agent-view copy '{r.name}' was moved away from its collider.", r.transform);
                }
                if (col != null && !Within(PartCatalog.ScaleIn(r.transform, c.Root), PartCatalog.ScaleIn(col.transform, c.Root), 0.005f))
                    c.Err("Vision", $"Agent-view copy '{r.name}' has a different size than its collider.", r.transform);
                if (r.sharedMaterial != fp.RenderMaterial)
                    c.Err("Vision", $"'{r.name}' must use '{(fp.RenderMaterial ? fp.RenderMaterial.name : "?")}' (agent colour), uses '{(r.sharedMaterial ? r.sharedMaterial.name : "none")}'.", r.transform);
                if (c.Rules.Palette != null && c.Rules.Palette.RoleOf(r.sharedMaterial) == null)
                    c.Err("Vision", $"'{r.name}' uses a material that isn't in the vision palette.", r.transform);
            }
        }

        // ---------------------------------------------------------------- custom scripts (review list)
        private static void CheckScripts(Ctx c) {
            var known = KnownTypes(c);
            foreach (var mb in c.Root.GetComponentsInChildren<MonoBehaviour>(true)) {
                if (mb == null) { c.Err("Scripts", "A component has a missing script."); continue; }
                if (!known.Contains(mb.GetType()) && !IsEngineType(mb.GetType()))
                    c.Warn("Scripts", $"Custom script '{mb.GetType().Name}' – review by hand.", mb.transform);
            }
        }

        private static HashSet<Type> KnownTypes(Ctx c) {
            var set = new HashSet<Type>();
            void Add(GameObject g) { if (g != null) foreach (var mb in g.GetComponentsInChildren<MonoBehaviour>(true)) if (mb != null) set.Add(mb.GetType()); }
            Add(c.Rules.RobotTemplate);
            foreach (var g in c.Rules.Groups) foreach (var e in g.Parts) Add(e.Prefab);
            foreach (var s in c.Rules.SensorPrefabs) Add(s);
            set.Add(typeof(RobotHierarchyValidator));
            set.Add(typeof(PhysicalOverlapValidator));
            return set;
        }

        public static bool IsEngineType(Type t) {
            var ns = t.Namespace ?? "";
            return ns.StartsWith("UnityEngine") || ns.StartsWith("Unity.") || ns.StartsWith("TMPro");
        }

        // ---------------------------------------------------------------- helpers
        /// <summary>The serialized value (what the prefab stores); runtime getters can return computed values.</summary>
        private static Vector3 Stored(Object o, string prop, Vector3 fallback) {
#if UNITY_EDITOR
            var sp = new UnityEditor.SerializedObject(o).FindProperty(prop);
            if (sp != null && sp.propertyType == UnityEditor.SerializedPropertyType.Vector3) return sp.vector3Value;
#endif
            return fallback;
        }

        private static bool Same(Ctx c, float a, float b) => Mathf.Abs(a - b) <= c.Rules.PhysicsTolerance * Mathf.Max(1f, Mathf.Abs(b));

        private static bool Within(Vector3 have, Vector3 want, float tol) {
            for (int i = 0; i < 3; i++) {
                if (want[i] < 1e-6f) continue;
                float r = have[i] / want[i];
                if (r < 1f - tol - 1e-4f || r > 1f + tol + 1e-4f) return false;
            }
            return true;
        }

        private static string Pct(Vector3 have, Vector3 want) {
            string Ax(int i) => want[i] < 1e-6f ? "–" : $"{(have[i] / want[i] - 1f) * 100f:+0.#;-0.#;0} %";
            return $"x {Ax(0)}, y {Ax(1)}, z {Ax(2)}";
        }
    }
}
