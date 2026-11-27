using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AIJam.Validation.Tests {
    /// <summary>Known-good robot and arena pass; each kind of cheat fails with the expected rule.</summary>
    public class ValidatorTests {
        private const string RulesPath = "Assets/Validation/ValidationRules.asset";
        private const string RobotPath = "Assets/Validation/Robots/ReferenceBot.prefab";
        private const string ArenaPath = "Assets/Turnaj/Prefabs/NeutralArena.prefab";
        private ValidationRules rules;

        [SetUp]
        public void Load() {
            rules = AssetDatabase.LoadAssetAtPath<ValidationRules>(RulesPath);
            Assert.IsNotNull(rules, "rules asset");
        }

        private List<Finding> Robot(Action<GameObject> mutate = null) => Check(RobotPath, mutate, go => RobotValidator.Validate(go, rules));
        private List<Finding> Arena(Action<GameObject> mutate = null) => Check(ArenaPath, mutate, go => ArenaValidator.Validate(go, rules));

        private static List<Finding> Check(string path, Action<GameObject> mutate, Func<GameObject, List<Finding>> check) {
            var root = PrefabUtility.LoadPrefabContents(path);
            try {
                mutate?.Invoke(root);
                Physics.SyncTransforms();
                return check(root);
            } finally {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AssertFails(List<Finding> f, string rule, string contains = null) {
            bool hit = f.Any(x => x.Severity == Severity.Error && x.Rule == rule && (contains == null || x.Message.Contains(contains)));
            Assert.IsTrue(hit, $"expected {rule} error{(contains != null ? " containing '" + contains + "'" : "")}, got:\n" + string.Join("\n", f));
        }

        private static Transform Find(GameObject root, string name) =>
            root.GetComponentsInChildren<Transform>(true).First(t => t.name == name);

        [Test]
        public void Validator_ReferenceRobot_Passes() {
            var f = Robot();
            Assert.IsFalse(f.HasErrors(), string.Join("\n", f));
        }

        [Test]
        public void Validator_VectorObservationSizeOfLastYear_Fails() {
            AssertFails(Robot(r => r.GetComponent<Unity.MLAgents.Policies.BehaviorParameters>().BrainParameters.VectorObservationSize = 7), "Model", "must be 10");
        }

        [Test]
        public void Rules_RobotTemplate_HasTheStandardObservationSize() {
            var bp = rules.RobotTemplate.GetComponent<Unity.MLAgents.Policies.BehaviorParameters>();
            Assert.AreEqual(10, rules.VectorObservationSize, "3 fixed values + 7 sensor slots");
            Assert.AreEqual(rules.VectorObservationSize, bp.BrainParameters.VectorObservationSize, "robot template");
        }

        [Test]
        public void Validator_NeutralArenaAsTeamArena_Passes() {
            var f = Arena();
            Assert.IsFalse(f.HasErrors(), string.Join("\n", f));
        }

        [Test]
        public void Validator_ColliderScaledBeyondTolerance_Fails() {
            AssertFails(Robot(r => Find(r, "bumper_shape").localScale *= 1f + Group("Bumpers").ScaleTolerancePercent / 100f + 0.05f), "Size");
        }

        [Test]
        public void Validator_ColliderScaledWithinTolerance_Passes() {
            var f = Robot(r => Find(r, "bumper_shape").localScale *= 1f + Group("Bumpers").ScaleTolerancePercent / 200f);
            Assert.IsFalse(f.Any(x => x.Rule == "Size"), string.Join("\n", f));
        }

        [Test]
        public void Validator_PartRootScaledUp_Fails() {
            AssertFails(Robot(r => Find(r, "Arm_Shape2").localScale *= 3f), "Size");
        }

        [Test]
        public void Validator_ExtraSolidCollider_Fails() {
            AssertFails(Robot(r => {
                var armour = GameObject.CreatePrimitive(PrimitiveType.Cube);
                armour.transform.SetParent(Find(r, "Body_Shape2"), false);
            }), "Colliders");
        }

        [Test]
        public void Validator_ZoneRenamedToWeapon_Fails() {
            AssertFails(Robot(r => Find(r, "body_shape").name = "weapon_core"), "Zones");
            AssertFails(Robot(r => Find(r, "bumper_shape").name = "weapon_shape"), "Zones");
        }

        [Test]
        public void Validator_MassChanged_Fails() {
            AssertFails(Robot(r => r.GetComponent<Rigidbody>().mass *= 2f), "Physics", "mass");
        }

        [Test]
        public void Validator_WheelFrictionChanged_Fails() {
            AssertFails(Robot(r => {
                var w = r.GetComponentInChildren<WheelCollider>();
                var fr = w.sidewaysFriction; fr.stiffness *= 2f; w.sidewaysFriction = fr;
            }), "Physics");
        }

        [Test]
        public void Validator_FakeRobotViewShape_Fails() {
            AssertFails(Robot(r => {
                var fake = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                UnityEngine.Object.DestroyImmediate(fake.GetComponent<Collider>());
                fake.layer = LayerMask.NameToLayer("RobotView");
                fake.GetComponent<Renderer>().sharedMaterial = rules.Palette.BatteryMaterial;
                fake.transform.SetParent(r.transform, false);
            }), "Vision");
        }

        [Test]
        public void Validator_WheelHiddenInsideBody_Fails() {
            AssertFails(Robot(r => {
                var bodyCol = Find(r, "body_shape").GetComponent<Collider>();
                var wheel = r.GetComponentInChildren<WheelComponent>().transform;
                var wheelCol = wheel.Find("wheels_shape").GetComponent<Collider>();
                wheel.position += bodyCol.bounds.center - wheelCol.bounds.center; // wheel buried in the body
            }), "Attachment", "sinks");
        }

        [Test]
        public void Validator_WeaponDetachedFromArm_Fails() {
            AssertFails(Robot(r => Find(r, "arm_weapon_shape").position += Vector3.up), "Attachment", "weapon");
        }

        private static Renderer[] ViewCopies(GameObject root) =>
            root.GetComponentsInChildren<Renderer>(true).Where(r => r.gameObject.layer == LayerMask.NameToLayer("RobotView")).ToArray();

        [Test]
        public void PartVisionColor_Body_ColoursBatteryAndBodyOnly() {
            var root = PrefabUtility.LoadPrefabContents(RobotPath);
            try {
                foreach (var r in ViewCopies(root)) r.sharedMaterial = rules.Palette.TrapMaterial; // wrong colour everywhere
                var body = root.GetComponentInChildren<BodyComponent>();
                body.GetComponent<PartVisionColor>().Apply();
                foreach (var r in ViewCopies(root)) {
                    var owner = PartCatalog.PartRootOf(r.transform);
                    Material want = owner != body.transform ? rules.Palette.TrapMaterial               // parts under the body untouched
                        : r.name.Contains("battery") ? rules.Palette.BatteryMaterial : rules.Palette.RobotPartMaterial;
                    Assert.AreEqual(want, r.sharedMaterial, r.name);
                }
            } finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [Test]
        public void PartVisionColor_Wheel_OneColour() {
            var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Base Shapes/Wheels_Shape2.prefab");
            try {
                foreach (var r in ViewCopies(root)) r.sharedMaterial = rules.Palette.BatteryMaterial;
                root.GetComponent<PartVisionColor>().Apply();
                foreach (var r in ViewCopies(root)) Assert.AreEqual(rules.Palette.RobotPartMaterial, r.sharedMaterial, r.name);
            } finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [Test]
        public void VisionPalette_ColourChange_RecoloursOnlyThatMaterialIncludingEmission() {
            var pal = ScriptableObject.CreateInstance<VisionPalette>();
            var trap = new Material(rules.Palette.TrapMaterial);
            var battery = new Material(rules.Palette.BatteryMaterial);
            try {
                pal.TrapMaterial = trap; pal.BatteryMaterial = battery;
                pal.TrapColor = rules.Palette.TrapColor; pal.BatteryColor = rules.Palette.BatteryColor;
                var validate = typeof(VisionPalette).GetMethod("OnValidate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var batteryEmission = battery.GetVector("_EmissionColor");
                var t0 = trap.GetVector("_EmissionColor");
                float trapStrength = Mathf.Max(t0.x, Mathf.Max(t0.y, t0.z));

                validate.Invoke(pal, null); // first run: remembers colours, touches nothing
                Assert.AreEqual(batteryEmission, battery.GetVector("_EmissionColor"));

                pal.TrapColor = new Color(1f, 1f, 0f);
                validate.Invoke(pal, null);
                Assert.AreEqual(new Color(1f, 1f, 0f), trap.GetColor("_BaseColor"));
                var e = trap.GetVector("_EmissionColor");
                Assert.AreEqual(trapStrength, e.x, 1e-4f); Assert.AreEqual(trapStrength, e.y, 1e-4f); Assert.AreEqual(0f, e.z, 1e-4f);
                Assert.AreEqual(batteryEmission, battery.GetVector("_EmissionColor"), "unchanged colour keeps its material");
            } finally {
                UnityEngine.Object.DestroyImmediate(trap); UnityEngine.Object.DestroyImmediate(battery); UnityEngine.Object.DestroyImmediate(pal);
            }
        }

        [Test]
        public void Validator_PartNotUnderBody_Fails() {
            AssertFails(Robot(r => Find(r, "Bumper_Shape1").SetParent(r.transform, true)), "Structure");
        }

        [Test]
        public void Validator_WheelsUnderTheRootInsteadOfTheBody_Pass() {
            var f = Robot(r => {
                Find(r, "Wheels Left").SetParent(r.transform, true);
                Find(r, "Wheels Right").SetParent(r.transform, true);
            });
            Assert.IsFalse(f.HasErrors(), string.Join("\n", f));
        }

        [Test]
        public void Validator_WheelsUnderTheRootNotTouchingTheBody_Fails() {
            AssertFails(Robot(r => {
                var left = Find(r, "Wheels Left");
                left.SetParent(r.transform, true);
                left.GetComponentInChildren<WheelComponent>().transform.position += Vector3.left;
            }), "Attachment", "doesn't touch the body");
        }

        [Test]
        public void Validator_BumperAndWheelsAtTheirPrefabPlaces_Pass() {
            // the part prefabs overlap a little where they are made to sit; that layout must be valid
            var f = Robot(r => Find(r, "Bumper_Shape1").localPosition = Vector3.zero);
            Assert.IsFalse(f.HasErrors(), string.Join("\n", f));
        }

        [Test]
        public void Validator_BumperPushedBackOverTheWheels_Fails() {
            AssertFails(Robot(r => Find(r, "Bumper_Shape1").localPosition = new Vector3(0f, 0f, -0.05f)), "Attachment", "bumper");
        }

        [Test]
        public void Validator_SensorColliderDisabled_Fails() {
            AssertFails(Robot(r => r.GetComponentInChildren<ProximitySensorRay>().GetComponentInChildren<SphereCollider>().enabled = false), "Colliders");
        }

        [Test]
        public void Validator_SecondBody_Fails() {
            AssertFails(Robot(r => {
                var extra = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Base Shapes/Body_Shape1.prefab"));
                extra.transform.SetParent(Find(r, "Body_Shape2"), false);
            }), "Parts", "Body");
        }

        // ---------------------------------------------------------------- part limits (README 6.2.2, 6.2.3)
        private ValidationRules.PartGroup Group(string name) => rules.Groups.First(g => g.Name == name);

        private static bool Has(List<Finding> f, string rule) => f.Any(x => x.Severity == Severity.Error && x.Rule == rule);

        /// <summary>Size of the collider along the robot's axes (the loaded robot stands at the origin, unrotated).</summary>
        private static Vector3 SizeOnRobot(Transform collider) => collider.GetComponent<Collider>().bounds.size;

        private static void AddBumpers(GameObject r, int count) {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Base Shapes/Bumper_Shape2.prefab");
            for (int i = 0; i < count; i++) {
                var b = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                b.transform.SetParent(Find(r, "Body_Shape2"), false);
                b.transform.localRotation = Quaternion.Euler(0f, 90f * (i + 1), 0f);
            }
        }

        [Test]
        public void Validator_ThreeBumpers_AreAllowed() {
            var f = Robot(r => AddBumpers(r, 2));
            Assert.IsFalse(Has(f, "Parts") || Has(f, "Position"), string.Join("\n", f));
        }

        [Test]
        public void Validator_FourBumpers_Fails() {
            AssertFails(Robot(r => AddBumpers(r, 3)), "Parts", "Bumpers");
        }

        [Test]
        public void Validator_BumperMovedWithinAQuarterOfItsSize_Passes() {
            var f = Robot(r => Find(r, "Bumper_Shape1").localPosition = new Vector3(0.2f * SizeOnRobot(Find(r, "bumper_shape")).x, 0f, 0f));
            Assert.IsFalse(Has(f, "Position"), string.Join("\n", f));
        }

        [Test]
        public void Validator_BumperMovedBeyondAQuarterOfItsSize_Fails() {
            AssertFails(Robot(r => Find(r, "Bumper_Shape1").localPosition = new Vector3(0.3f * SizeOnRobot(Find(r, "bumper_shape")).x, 0f, 0f)), "Position", "bumper_shape");
        }

        [Test]
        public void Validator_BumperTurnedToTheRear_Passes() {
            var f = Robot(r => Find(r, "Bumper_Shape1").localRotation = Quaternion.Euler(0f, 180f, 0f));
            Assert.IsFalse(Has(f, "Position"), string.Join("\n", f));
        }

        [Test]
        public void Validator_BumperTurnedSideways_MoveLimitFollowsTheBumper() {
            // the bumper is wider than deep: after a quarter turn its wide axis lies along the robot's z axis
            var f = Robot(r => {
                var b = Find(r, "Bumper_Shape1");
                var size = SizeOnRobot(Find(r, "bumper_shape"));
                Assert.Greater(0.2f * size.x, 0.25f * size.z, "test needs a bumper that is wider than deep");
                b.localRotation = Quaternion.Euler(0f, 90f, 0f);
                b.localPosition = new Vector3(0f, 0f, 0.2f * size.x);
            });
            Assert.IsFalse(Has(f, "Position"), string.Join("\n", f));
        }

        [Test]
        public void Validator_BatteryResized_Fails() {
            AssertFails(Robot(r => Find(r, "battery_shape").localScale *= 1.05f), "Size", "battery_shape");
        }

        [Test]
        public void Validator_BatteryMovedFourCentimetres_Passes() {
            var f = Robot(r => Find(r, "battery_shape").localPosition += new Vector3(0f, 0f, 0.04f));
            Assert.IsFalse(Has(f, "Position"), string.Join("\n", f));
        }

        [Test]
        public void Validator_BatteryMovedSixCentimetres_Fails() {
            AssertFails(Robot(r => Find(r, "battery_shape").localPosition += new Vector3(0f, 0f, 0.06f)), "Position", "battery_shape");
        }

        [Test]
        public void Validator_BodyMoved_Fails() {
            AssertFails(Robot(r => Find(r, "Body_Shape2").localPosition += new Vector3(0.01f, 0f, 0f)), "Position", "body_shape");
        }

        [Test]
        public void Validator_BodyResizedByAFifth_Passes() {
            var f = Robot(r => Find(r, "body_shape").localScale *= 1.2f);
            Assert.IsFalse(Has(f, "Size"), string.Join("\n", f));
        }

        [Test]
        public void Validator_WheelsResizedByThirtyPercent_Fails() {
            AssertFails(Robot(r => Find(r, "wheels_shape").localScale *= 1.3f), "Size", "wheels_shape");
        }

        [Test]
        public void Validator_WheelsResizedByAFifth_Passes() {
            var f = Robot(r => Find(r, "wheels_shape").localScale *= 1.2f);
            Assert.IsFalse(Has(f, "Size"), string.Join("\n", f));
        }

        [Test]
        public void Validator_WheelsMoved_Fails() {
            AssertFails(Robot(r => Find(r, "wheels_shape").localPosition += new Vector3(0f, 0f, 0.01f)), "Position", "wheels_shape");
        }

        [Test]
        public void Validator_ArmMoved_HasNoPositionLimit() {
            var f = Robot(r => Find(r, "Arm_Shape2").localPosition += new Vector3(0f, 0f, 0.05f));
            Assert.IsFalse(Has(f, "Position"), string.Join("\n", f));
        }

        // ---------------------------------------------------------------- one robot or one arena, without a team
        [Test]
        public void SingleRobot_ReferenceRobot_PassesAndIsTaggedRobot() {
            var f = Turnaj.TeamValidation.RobotCheck(AssetDatabase.LoadAssetAtPath<GameObject>(RobotPath), rules);
            Assert.IsFalse(f.HasErrors(), string.Join("\n", f));
            Assert.IsTrue(f.All(x => x.Source == "Robot"), "every finding names its source");
        }

        [Test]
        public void SingleRobot_NoPrefab_Fails() {
            var f = Turnaj.TeamValidation.RobotCheck(null, rules);
            Assert.IsTrue(f.HasErrors());
            Assert.AreEqual("Robot", f[0].Source);
        }

        [Test]
        public void SingleRobot_ArenaGivenAsRobot_Fails() {
            Assert.IsTrue(Turnaj.TeamValidation.RobotCheck(AssetDatabase.LoadAssetAtPath<GameObject>(ArenaPath), rules).HasErrors());
        }

        [Test]
        public void SingleArena_NeutralArena_PassesAndIsTaggedArena() {
            var f = Turnaj.TeamValidation.ArenaCheck(AssetDatabase.LoadAssetAtPath<GameObject>(ArenaPath), rules);
            Assert.IsFalse(f.HasErrors(), string.Join("\n", f));
            Assert.IsTrue(f.All(x => x.Source == "Arena"), "every finding names its source");
        }

        [Test]
        public void SingleArena_NoPrefab_Fails() {
            var f = Turnaj.TeamValidation.ArenaCheck(null, rules);
            Assert.IsTrue(f.HasErrors());
            Assert.AreEqual("Arena", f[0].Source);
        }

        [Test]
        public void SingleArena_RobotGivenAsArena_Fails() {
            Assert.IsTrue(Turnaj.TeamValidation.ArenaCheck(AssetDatabase.LoadAssetAtPath<GameObject>(RobotPath), rules).HasErrors());
        }

        [Test]
        public void Validator_ArenaHardObstacle_Fails() {
            AssertFails(Arena(a => GameObject.CreatePrimitive(PrimitiveType.Cube).transform.SetParent(a.transform, false)), "Ground");
        }

        [Test]
        public void Validator_ArenaTriggerObstacle_Passes() {
            var f = Arena(a => {
                var trap = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trap.GetComponent<Collider>().isTrigger = true;
                trap.transform.SetParent(a.transform, false);
            });
            Assert.IsFalse(f.Any(x => x.Rule == "Ground"), string.Join("\n", f));
        }

        [Test]
        public void Validator_ArenaRedWithoutTrigger_Fails() {
            AssertFails(Arena(a => {
                var fake = GameObject.CreatePrimitive(PrimitiveType.Cube);
                UnityEngine.Object.DestroyImmediate(fake.GetComponent<Collider>());
                fake.layer = LayerMask.NameToLayer("RobotView");
                fake.GetComponent<Renderer>().sharedMaterial = rules.Palette.TrapMaterial;
                fake.transform.SetParent(a.transform, false);
                fake.transform.localPosition = new Vector3(0.5f, 0.1f, 0.5f);
            }), "Vision", "fake");
        }

        [Test]
        public void Validator_ArenaBlackObject_Fails() {
            AssertFails(Arena(a => {
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.layer = LayerMask.NameToLayer("RobotView");
                floor.GetComponent<Renderer>().sharedMaterial = rules.Palette.DefaultMaterial;
                floor.transform.SetParent(a.transform, false);
            }), "Vision", "black");
        }

        [Test]
        public void Validator_ArenaWhiteObject_Fails() {
            AssertFails(Arena(a => {
                var fake = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fake.GetComponent<Collider>().isTrigger = true;
                fake.layer = LayerMask.NameToLayer("RobotView");
                fake.GetComponent<Renderer>().sharedMaterial = rules.Palette.RobotPartMaterial;
                fake.transform.SetParent(a.transform, false);
            }), "Vision");
        }

        [Test]
        public void Validator_ArenaAsymmetricSpawn_Fails() {
            AssertFails(Arena(a => {
                var guest = a.GetComponentsInChildren<Turnaj.SpawnPoint>().First(s => s.Side == Turnaj.SpawnSide.Guest);
                guest.transform.position += new Vector3(0.4f, 0f, 0f);
            }), "Spawns");
        }

        [Test]
        public void Validator_ArenaGroundResized_Fails() {
            AssertFails(Arena(a => Find(a, "Cylinder").localScale *= 1.1f), "Ground");
        }
    }
}
