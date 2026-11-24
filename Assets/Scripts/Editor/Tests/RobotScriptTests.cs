using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AIJam.RobotScripts.Tests {
    /// <summary>Damage, reward and respawn logic of the robot scripts in Assets/Scripts.</summary>
    public class RobotScriptTests {
        private const string RobotPath = "Assets/Validation/Robots/ReferenceBot.prefab";
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void Cleanup() {
            foreach (var go in spawned) if (go != null) Object.DestroyImmediate(go);
            spawned.Clear();
        }

        private GameObject Robot(string name) {
            var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RobotPath));
            go.name = name;
            spawned.Add(go);
            return go;
        }

        /// <summary>A zone of the robot with neutral stats (damage 1, resistance 1); CollisionForwarder sets the real ones only in Play mode.</summary>
        private static CollisionZoneBehaviour Zone(GameObject robot, string name) {
            var zone = robot.GetComponentsInChildren<CollisionZoneBehaviour>().First(z => z.name == name);
            zone.damageOfZone = 1f;
            zone.resistanceOfZone = 1f;
            zone.CurrentHealth = 1f;
            return zone;
        }

        private static Collider Col(CollisionZoneBehaviour zone) => zone.GetComponent<Collider>();

        private static float Force(float impulse) => impulse / Time.fixedDeltaTime / 100000f;

        /// <summary>Impulse that gives the normalised collision force (at 1, a neutral zone loses all its HP).</summary>
        private static float Impulse(float force) => force * Time.fixedDeltaTime * 100000f;

        // ---------------------------------------------------------------- body owns only its own parts

        [Test]
        public void Body_WithPartsNestedUnderIt_UsesOnlyItsOwnDamageSensorAndZones() {
            var body = Robot("A").GetComponentInChildren<BodyComponent>();
            typeof(BodyComponent).GetMethod("Start", Private).Invoke(body, null);

            var sensor = (SensorComponent)typeof(BodyComponent).GetField("sensorDamage", Private).GetValue(body);
            var zones = (CollisionZoneBehaviour[])typeof(BodyComponent).GetField("zoneComponents", Private).GetValue(body);

            Assert.AreSame(body.transform, sensor.transform.parent, "damage sensor belongs to: " + sensor.transform.parent.name);
            CollectionAssert.AreEquivalent(new[] { "body_shape", "battery_shape" }, zones.Select(z => z.name).ToArray());
        }

        // ---------------------------------------------------------------- one hit = one damage

        [Test]
        public void ResolveHit_SamePairTwiceInOnePhysicsStep_DamagesEachZoneOnce() {
            var a = Zone(Robot("A"), "bumper_shape");
            var b = Zone(Robot("B"), "bumper_shape");
            int eventsA = 0;
            a.OnCollision += (s, e) => eventsA++;

            CollisionZoneBehaviour.ResolveHit(Col(a), Col(b), Impulse(0.1f), 1.00f);
            float afterFirst = a.CurrentHealth;
            CollisionZoneBehaviour.ResolveHit(Col(a), Col(b), Impulse(0.1f), 1.00f); // second contact point
            CollisionZoneBehaviour.ResolveHit(Col(b), Col(a), Impulse(0.1f), 1.00f); // the other robot's forwarder

            Assert.Less(afterFirst, 1f, "the first hit must damage");
            Assert.AreEqual(afterFirst, a.CurrentHealth, 1e-6f, "repeats in the same step must not damage again");
            Assert.AreEqual(1, eventsA);
        }

        [Test]
        public void ResolveHit_SamePairInALaterPhysicsStep_DamagesAgain() {
            var a = Zone(Robot("A"), "bumper_shape");
            var b = Zone(Robot("B"), "bumper_shape");

            CollisionZoneBehaviour.ResolveHit(Col(a), Col(b), Impulse(0.1f), 1.00f);
            float afterFirst = a.CurrentHealth;
            CollisionZoneBehaviour.ResolveHit(Col(a), Col(b), Impulse(0.1f), 1.02f);

            Assert.Less(a.CurrentHealth, afterFirst);
        }

        // ---------------------------------------------------------------- who was hit by what

        [Test]
        public void ResolveHit_WeaponAgainstBumper_EachZoneGetsTheTagOfWhatHitIt() {
            var bumper = Zone(Robot("A"), "bumper_shape");
            var weapon = Zone(Robot("B"), "arm_weapon_shape");
            string bumperHitBy = null, weaponHitBy = null;
            bumper.OnCollision += (s, e) => bumperHitBy = e.colliderHitName;
            weapon.OnCollision += (s, e) => weaponHitBy = e.colliderHitName;

            CollisionZoneBehaviour.ResolveHit(Col(bumper), Col(weapon), 2000f, 1.00f);

            Assert.AreEqual("RobotArm", bumperHitBy, "the bumper was hit by the enemy arm");
            Assert.AreEqual("Robot", weaponHitBy, "the weapon hit the enemy robot");
        }

        // ---------------------------------------------------------------- reward for damage dealt

        [Test]
        public void ResolveHit_ReportsDamageDealtToTheOtherZone() {
            var weapon = Zone(Robot("A"), "arm_weapon_shape");
            var body = Zone(Robot("B"), "body_shape");
            weapon.damageOfZone = 200f; weapon.resistanceOfZone = 15f;
            body.damageOfZone = 1f; body.resistanceOfZone = 3f;
            CollisionZoneBehaviour.CollisionEventArgs weaponEvent = null, bodyEvent = null;
            weapon.OnCollision += (s, e) => weaponEvent = e;
            body.OnCollision += (s, e) => bodyEvent = e;

            CollisionZoneBehaviour.ResolveHit(Col(weapon), Col(body), 1000f, 1.00f);

            Assert.AreEqual(200f / 3f * Force(1000f), weaponEvent.DamageDealt, 1e-5f);
            Assert.AreEqual(bodyEvent.Damage, weaponEvent.DamageDealt, 1e-6f, "dealt by the weapon = taken by the body");
            Assert.AreEqual(1f / 15f * Force(1000f), weaponEvent.Damage, 1e-6f);
        }

        [Test]
        public void HitReward_PaysForDamageDealtToAnEnemy_NotForDamageTaken() {
            var hitEnemy = new CollisionZoneBehaviour.CollisionEventArgs(0.5f, "Robot", 0.001f);
            var hitEnemyArm = new CollisionZoneBehaviour.CollisionEventArgs(0.5f, "RobotArm", 0.002f);
            var hitWall = new CollisionZoneBehaviour.CollisionEventArgs(0.5f, "Arena", 0f);

            Assert.AreEqual(10f, RobotAgent.HitReward(hitEnemy), 1e-4f);
            Assert.AreEqual(20f, RobotAgent.HitReward(hitEnemyArm), 1e-4f);
            Assert.AreEqual(0f, RobotAgent.HitReward(hitWall));
        }

        // ---------------------------------------------------------------- collision reward per step

        [Test]
        public void CollisionStepReward_UsesTheRewardEditorFactor() {
            Assert.AreEqual(0.7f, RobotAgent.CollisionStepReward(0.5f, 0.02f, 0.7f), 1e-5f); // 0.5 * 100 * 0.02 = 1, times 0.7
            Assert.AreEqual(0f, RobotAgent.CollisionStepReward(0.5f, 0.02f, 0f));
        }

        [Test]
        public void CollisionReward_IsVisibleForExactlyOnePhysicsStep() {
            var weapon = Zone(Robot("A"), "arm_weapon_shape");
            var body = Zone(Robot("B"), "body_shape");
            weapon.damageOfZone = 200f; body.resistanceOfZone = 3f;
            float dt = Time.fixedDeltaTime;

            CollisionZoneBehaviour.ResolveHit(Col(weapon), Col(body), 1000f, 1.00f);

            Assert.AreNotEqual(0f, weapon.CollisionRewardAt(1.00f + dt), "the step after the hit reads the reward");
            Assert.AreEqual(0f, weapon.CollisionRewardAt(1.00f + 2f * dt), "one step later it is gone");
        }

        // ---------------------------------------------------------------- respawn

        [Test]
        public void MoveRobot_StopsTheRobotAndItsArm() {
            var robot = Robot("A");
            var bodies = robot.GetComponentsInChildren<Rigidbody>();
            Assert.GreaterOrEqual(bodies.Length, 2, "root and arm");
            foreach (var rb in bodies) { rb.linearVelocity = new Vector3(1f, -4f, 2f); rb.angularVelocity = new Vector3(3f, 0f, 1f); }
            Assert.AreNotEqual(Vector3.zero, bodies[0].linearVelocity, "test setup: velocity can be set in edit mode");

            var managerGo = new GameObject("manager");
            spawned.Add(managerGo);
            var manager = managerGo.AddComponent<AIJamManager>();
            typeof(AIJamManager).GetMethod("MoveRobot", Private).Invoke(manager, new object[] { robot.GetComponent<RobotAgent>(), 0 });

            foreach (var rb in bodies) {
                Assert.AreEqual(Vector3.zero, rb.linearVelocity, rb.name + " linear");
                Assert.AreEqual(Vector3.zero, rb.angularVelocity, rb.name + " angular");
            }
        }
    }
}
