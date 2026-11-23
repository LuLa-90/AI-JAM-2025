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
        public void Validator_NeutralArenaAsTeamArena_Passes() {
            var f = Arena();
            Assert.IsFalse(f.HasErrors(), string.Join("\n", f));
        }

        [Test]
        public void Validator_ColliderScaledBeyondTolerance_Fails() {
            AssertFails(Robot(r => Find(r, "bumper_shape").localScale *= 1f + rules.ScaleTolerancePercent / 100f + 0.05f), "Size");
        }

        [Test]
        public void Validator_ColliderScaledWithinTolerance_Passes() {
            var f = Robot(r => Find(r, "bumper_shape").localScale *= 1f + rules.ScaleTolerancePercent / 200f);
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
        public void Validator_PartNotUnderBody_Fails() {
            AssertFails(Robot(r => Find(r, "Bumper_Shape1").SetParent(r.transform, true)), "Structure");
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
