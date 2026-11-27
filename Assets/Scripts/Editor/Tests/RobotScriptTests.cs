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

            CollisionZoneBehaviour.ResolveHit(Col(weapon), Col(body), Impulse(0.001f), 1.00f);

            Assert.AreEqual(200f / 3f * 0.001f, weaponEvent.DamageDealt, 1e-6f);
            Assert.AreEqual(bodyEvent.Damage, weaponEvent.DamageDealt, 1e-6f, "dealt by the weapon = taken by the body");
            Assert.AreEqual(1f / 15f * 0.001f, weaponEvent.Damage, 1e-7f);
        }

        [Test]
        public void HitReward_PaysForDamageDealtToAnEnemy_NotForDamageTaken() {
            var hitEnemy = new CollisionZoneBehaviour.CollisionEventArgs(0.5f, "Robot", 0.001f);
            var hitEnemyArm = new CollisionZoneBehaviour.CollisionEventArgs(0.5f, "RobotArm", 0.002f);
            var hitWall = new CollisionZoneBehaviour.CollisionEventArgs(0.5f, "Arena", 0f);

            Assert.AreEqual(0.0001f, RobotAgent.HitReward(hitEnemy), 1e-8f); // 0.1 per unit of damage dealt
            Assert.AreEqual(0.0002f, RobotAgent.HitReward(hitEnemyArm), 1e-8f);
            Assert.AreEqual(0f, RobotAgent.HitReward(hitWall));
        }

        // ---------------------------------------------------------------- reward balance: win > hits > shaping

        [Test]
        public void Rewards_WinIsPlusOne_LossIsMinusOne() {
            Assert.AreEqual(1f, RobotAgent.WinReward);
            Assert.AreEqual(-1f, RobotAgent.LoseReward);
        }

        [Test]
        public void Rewards_AllTheHitsOfALongFight_AreWorthLessThanAWin() {
            // The check run measured 2 to 3.5 units of damage dealt per robot per fight.
            var wholeFight = new CollisionZoneBehaviour.CollisionEventArgs(0f, "Robot", 4f);
            Assert.Less(RobotAgent.HitReward(wholeFight), RobotAgent.WinReward);
            Assert.Greater(RobotAgent.HitReward(wholeFight), 0.1f, "hits must still matter");
        }

        [Test]
        public void ShapingReward_SmallValuesAreNotRoundedAway() {
            // value 0.2, gain 0.5, step 0.02 s, factor 0.8: this used to round to 0.
            float reward = RobotAgent.ShapingReward(0.2f, 0.5f, 0.02f, 0.8f);
            Assert.AreEqual(0.2f * 0.5f * 0.02f * 0.8f * RobotAgent.ShapingScale, reward, 1e-10f);
            Assert.Greater(reward, 0f);
        }

        [Test]
        public void ShapingReward_APerfectEpisodeWithAllPositionSlidersAtMaximum_MattersButIsWorthLessThanAWin() {
            // movement, facing, enemy distance and arena position, each at value 1 for a whole 90 s episode
            int steps = Mathf.RoundToInt(90f / 0.02f);
            float gains = RobotAgent.MovementGain + RobotAgent.FacingGain + RobotAgent.EnemyDistanceGain + RobotAgent.ArenaPositionGain;
            float episode = steps * RobotAgent.ShapingReward(1f, gains, 0.02f, 1f);
            Assert.Less(episode, RobotAgent.WinReward);
            Assert.Greater(episode, 0.5f, "teams steer training only with the sliders, so they must carry weight");
        }

        // ---------------------------------------------------------------- fight sliders (Reward Editor)

        [Test]
        public void HitReward_ScalesWithTheHitSlider() {
            var hit = new CollisionZoneBehaviour.CollisionEventArgs(0.5f, "Robot", 1f);
            Assert.AreEqual(0.2f, RobotAgent.HitReward(hit, 1f, 0f), 1e-6f);
            Assert.AreEqual(0.1f, RobotAgent.HitReward(hit, 0.5f, 0f), 1e-6f);
            Assert.AreEqual(0f, RobotAgent.HitReward(hit, 0f, 0f));
        }

        [Test]
        public void HitReward_DamageTakenSlider_PenalisesDamageFromTheEnemyOnly() {
            var hitByEnemy = new CollisionZoneBehaviour.CollisionEventArgs(0.5f, "Robot", 0f);
            var hitWall = new CollisionZoneBehaviour.CollisionEventArgs(0.5f, "Arena", 0f);
            Assert.AreEqual(-0.1f, RobotAgent.HitReward(hitByEnemy, 0f, 1f), 1e-6f);
            Assert.AreEqual(0f, RobotAgent.HitReward(hitByEnemy, 0f, 0f));
            Assert.AreEqual(0f, RobotAgent.HitReward(hitWall, 1f, 1f));
        }

        [Test]
        public void HitReward_WithBothSliders_IsRewardForDamageDealtMinusPenaltyForDamageTaken() {
            var trade = new CollisionZoneBehaviour.CollisionEventArgs(0.5f, "RobotArm", 1f);
            Assert.AreEqual(1f * 0.2f - 0.5f * 0.2f, RobotAgent.HitReward(trade, 1f, 1f), 1e-6f);
        }

        [Test]
        public void TimeoutReward_UsesTheTimeoutSlider() {
            Assert.AreEqual(1f, RobotAgent.TimeoutReward(0.8f, 0.5f, 1f));
            Assert.AreEqual(-0.25f, RobotAgent.TimeoutReward(0.5f, 0.8f, 0.25f));
            Assert.AreEqual(0f, RobotAgent.TimeoutReward(0.8f, 0.5f, 0f), "0: only a knockout counts");
            Assert.AreEqual(0f, RobotAgent.TimeoutReward(0.6f, 0.6f, 1f));
        }

        [Test]
        public void FightSliders_AtAnySetting_NeverMakeLosingPayOrHitsWorthMoreThanAWin() {
            var wholeFight = new CollisionZoneBehaviour.CollisionEventArgs(0f, "Robot", 4f);
            Assert.Less(RobotAgent.HitReward(wholeFight, 1f, 0f), RobotAgent.WinReward);
            Assert.LessOrEqual(RobotAgent.TimeoutReward(0.8f, 0.5f, 1f), RobotAgent.WinReward);
            Assert.LessOrEqual(RobotAgent.TimeoutReward(0.5f, 0.8f, 1f), 0f);
            Assert.GreaterOrEqual(RobotAgent.TimeoutReward(0.5f, 0.8f, 1f), RobotAgent.LoseReward);
        }

        [Test]
        public void RewardEditor_DefaultFightSliders_GiveTheStandardBalance() {
            var editor = new GameObject("RewardEditor").AddComponent<RewardEditor>();
            spawned.Add(editor.gameObject);
            var hit = new CollisionZoneBehaviour.CollisionEventArgs(0.5f, "Robot", 1f);

            Assert.AreEqual(0.1f, RobotAgent.HitReward(hit, editor.hitRF, editor.damageTakenRF), 1e-6f);
            Assert.AreEqual(0.5f, RobotAgent.TimeoutReward(0.8f, 0.5f, editor.timeoutRF));
            Assert.AreEqual(RobotAgent.HitReward(hit), RobotAgent.HitReward(hit, editor.hitRF, editor.damageTakenRF), 1e-6f,
                "a robot without a Reward Editor gets the same rewards as the default sliders");
        }

        [Test]
        public void CollisionStepReward_UsesTheRewardEditorFactor() {
            Assert.AreEqual(0.5f * 100f * 0.02f * 0.7f * RobotAgent.ShapingScale, RobotAgent.CollisionStepReward(0.5f, 0.02f, 0.7f), 1e-9f);
            Assert.AreEqual(0f, RobotAgent.CollisionStepReward(0.5f, 0.02f, 0f));
        }

        // ---------------------------------------------------------------- episode time limit (training only)

        [Test]
        public void TimeoutReward_HigherHpWinsHalfAPoint() {
            Assert.AreEqual(0.5f, RobotAgent.TimeoutReward(0.8f, 0.5f));
            Assert.AreEqual(-0.5f, RobotAgent.TimeoutReward(0.5f, 0.8f));
            Assert.AreEqual(0f, RobotAgent.TimeoutReward(0.6f, 0.6f));
        }

        [Test]
        public void EpisodeTimesOut_OnlyWhileTraining_AndOnlyWithALimit() {
            Assert.IsTrue(RobotAgent.IsTimedOut(91f, 90f, true));
            Assert.IsFalse(RobotAgent.IsTimedOut(89f, 90f, true));
            Assert.IsFalse(RobotAgent.IsTimedOut(91f, 90f, false), "tournament and validation run their own clock");
            Assert.IsFalse(RobotAgent.IsTimedOut(91f, 0f, true), "0 switches the limit off");
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

        // ---------------------------------------------------------------- zone stats: ramming is the main weapon

        [Test]
        public void ZoneStats_RammingWithTheBumper_DealsFarMoreThanAnArmStrike() {
            var bumper = CollisionForwarder.StatsFor("bumper_shape");
            var weapon = CollisionForwarder.StatsFor("arm_weapon_shape");

            Assert.AreEqual(1500f, bumper.damage);
            Assert.AreEqual(300f, weapon.damage);
            Assert.GreaterOrEqual(bumper.damage, 3f * weapon.damage);
        }

        [Test]
        public void ZoneStats_ResistancesAndMinorZones_AreUnchanged() {
            Assert.AreEqual((1f, 3f), CollisionForwarder.StatsFor("body_shape"));
            Assert.AreEqual((1f, 1f), CollisionForwarder.StatsFor("battery_shape"));
            Assert.AreEqual((5f, 4f), CollisionForwarder.StatsFor("wheels_shape"));
            Assert.AreEqual((2f, 3f), CollisionForwarder.StatsFor("arm_armbase_shape"));
            Assert.AreEqual(15f, CollisionForwarder.StatsFor("arm_weapon_shape").resistance);
            Assert.AreEqual(10f, CollisionForwarder.StatsFor("bumper_shape").resistance);
            Assert.AreEqual((1f, 5f), CollisionForwarder.StatsFor("something_else"));
        }

        [Test]
        public void ResolveHit_DamageDealt_IsNeverMoreThanTheHpTheEnemyZoneHad() {
            var bumper = Zone(Robot("A"), "bumper_shape");
            var battery = Zone(Robot("B"), "battery_shape");
            bumper.damageOfZone = 1500f;
            battery.CurrentHealth = 0.25f;
            CollisionZoneBehaviour.CollisionEventArgs bumperEvent = null, batteryEvent = null;
            bumper.OnCollision += (s, e) => bumperEvent = e;
            battery.OnCollision += (s, e) => batteryEvent = e;

            CollisionZoneBehaviour.ResolveHit(Col(bumper), Col(battery), Impulse(0.1f), 1.00f); // 150 units of raw damage

            Assert.AreEqual(0f, battery.CurrentHealth);
            Assert.AreEqual(0.25f, batteryEvent.Damage, 1e-6f, "damage taken = HP actually lost");
            Assert.AreEqual(0.25f, bumperEvent.DamageDealt, 1e-6f, "damage dealt = HP actually removed");
        }

        // ---------------------------------------------------------------- fight statistics for TensorBoard

        [Test]
        public void FightStats_AddUpHitsAndRespawns_AndResetForTheNextEpisode() {
            var stats = new FightStats();
            stats.AddHit(0.2f, 0.1f);
            stats.AddHit(0.3f, 0f);
            stats.AddRespawn();

            Assert.AreEqual(0.5f, stats.DamageDealt, 1e-6f);
            Assert.AreEqual(0.1f, stats.DamageTaken, 1e-6f);
            Assert.AreEqual(1, stats.Respawns);

            stats.Reset();
            Assert.AreEqual(0f, stats.DamageDealt);
            Assert.AreEqual(0f, stats.DamageTaken);
            Assert.AreEqual(0, stats.Respawns);
        }

        // ---------------------------------------------------------------- floor and wall contacts

        [Test]
        public void ResolveHit_RestingContactWithTheArena_CostsNoHp() {
            var wheel = Zone(Robot("A"), "wheels_shape");
            var floor = Box("floor", Vector3.zero, false);

            CollisionZoneBehaviour.ResolveHit(Col(wheel), floor, 0f, 1.00f);

            Assert.AreEqual(1f, wheel.CurrentHealth, "a contact without force must not damage");
        }

        [Test]
        public void ResolveHit_HardImpactWithTheArena_StillDamages() {
            var wheel = Zone(Robot("A"), "wheels_shape");
            var wall = Box("wall", Vector3.zero, false);

            CollisionZoneBehaviour.ResolveHit(Col(wheel), wall, Impulse(0.1f), 1.00f);

            Assert.AreEqual(0.9f, wheel.CurrentHealth, 1e-5f);
        }

        // ---------------------------------------------------------------- no log line per hit

        [Test]
        public void ResolveHit_AndHitReward_WriteNoLogLines() {
            var a = Zone(Robot("A"), "bumper_shape");
            var b = Zone(Robot("B"), "bumper_shape");
            var agent = a.GetComponentInParent<RobotAgent>();
            a.OnCollision += agent.HandleZoneCollision;
            int lines = 0;
            Application.LogCallback count = (msg, stack, type) => lines++;
            Application.logMessageReceived += count;
            try {
                CollisionZoneBehaviour.ResolveHit(Col(a), Col(b), Impulse(0.1f), 1.00f);
            } finally {
                Application.logMessageReceived -= count;
            }
            Assert.AreEqual(0, lines, "logging every hit slows training down");
        }

        // ---------------------------------------------------------------- sensors

        private Collider Box(string name, Vector3 position, bool trigger, string tag = null) {
            var go = new GameObject(name);
            spawned.Add(go);
            go.transform.position = position;
            if (tag != null) go.tag = tag;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = trigger;
            return box;
        }

        private static readonly Vector3 Far = new Vector3(500f, 500f, 500f); // away from anything in the open scene

        [Test]
        public void ButtSensor_SeesTheMarkerThroughATrigger_ButNotThroughASolidObject() {
            Box("marker", Far + Vector3.forward * 5f, false, "ButtTag");
            var between = Box("trap", Far + Vector3.forward * 2.5f, true);
            Physics.SyncTransforms();
            Assert.IsTrue(ButtSensorComponent.SeesMarker(Far, Far + Vector3.forward * 5f), "a trap or buff trigger must not block the sensor");

            between.isTrigger = false;
            Physics.SyncTransforms();
            Assert.IsFalse(ButtSensorComponent.SeesMarker(Far, Far + Vector3.forward * 5f), "a solid object blocks it");
        }

        [Test]
        public void ButtSensor_FindsTheEnemyMarker_EvenWhenTheEnemyIsKnownOnlyAfterStart() {
            var a = Robot("A");
            var b = Robot("B");
            var sensorGo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Base Shapes/ButtSensorReciever.prefab"), a.transform);
            var sensor = sensorGo.GetComponent<ButtSensorComponent>();

            typeof(ButtSensorComponent).GetMethod("Start", Private).Invoke(sensor, null); // enemy not known yet
            a.GetComponent<RobotAgent>().enemyRobot = b.GetComponent<RobotAgent>();
            typeof(ButtSensorComponent).GetMethod("Update", Private).Invoke(sensor, null);

            var marker = (ButtMarker)typeof(ButtSensorComponent).GetField("buttMarkerObject", Private).GetValue(sensor);
            Assert.IsNotNull(marker);
            Assert.AreSame(b.GetComponentInChildren<ButtMarker>(), marker);
        }

        [Test]
        public void ProximityHitType_EnemyArmCountsAsRobot_OwnArmDoesNot() {
            var a = Robot("A");
            var b = Robot("B");
            var own = a.GetComponent<RobotAgent>();

            Assert.AreEqual(2, ProximitySensorRay.HitType(b.transform, own), "enemy robot");
            Assert.AreEqual(2, ProximitySensorRay.HitType(b.GetComponentInChildren<ArmComponent>().transform, own), "enemy arm");
            Assert.AreEqual(0, ProximitySensorRay.HitType(a.GetComponentInChildren<ArmComponent>().transform, own), "own arm");
            Assert.AreEqual(1, ProximitySensorRay.HitType(Box("wall", Far, false, "Arena").transform, own));
            Assert.AreEqual(3, ProximitySensorRay.HitType(Box("buff", Far, true, "Bonus").transform, own));
            Assert.AreEqual(0, ProximitySensorRay.HitType(Box("other", Far, false).transform, own));
        }

        // ---------------------------------------------------------------- robots with missing parts must not crash

        private static object Call(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, Private | BindingFlags.Public).Invoke(target, args);

        [Test]
        public void RobotMovement_WithoutAnArm_DoesNotThrow() {
            var robot = Robot("A");
            Object.DestroyImmediate(robot.GetComponentInChildren<ArmComponent>().gameObject);
            var movement = robot.GetComponent<RobotMovement>();

            Assert.DoesNotThrow(() => Call(movement, "Start"));
            Assert.DoesNotThrow(() => movement.ApplyArmInput(1));
        }

        [Test]
        public void RobotMovement_WithoutWheelColliders_DoesNotThrow() {
            var movement = Robot("A").GetComponent<RobotMovement>();
            typeof(RobotMovement).GetField("leftWheels", Private).SetValue(movement, new WheelCollider[0]);
            typeof(RobotMovement).GetField("rightWheels", Private).SetValue(movement, null);

            Assert.DoesNotThrow(() => movement.ApplyBothTrackInputs(1f, 1f));
            Assert.DoesNotThrow(() => movement.ApplyBothTrackInputsHeuristic(1f, 1f));
            Assert.DoesNotThrow(() => Call(movement, "FixedUpdate"));
        }

        [Test]
        public void Part_WithoutDamageZones_ReportsFullHealthAndDoesNotThrow() {
            var bumper = Robot("A").GetComponentInChildren<BumperComponent>();
            foreach (var zone in bumper.GetComponentsInChildren<CollisionZoneBehaviour>()) Object.DestroyImmediate(zone);

            Assert.DoesNotThrow(() => Call(bumper, "Start"));
            Assert.DoesNotThrow(() => Call(bumper, "Update"));
            Assert.AreEqual(1f, ((IHealth)bumper).CurrentHealth);
        }

        [Test]
        public void RobotAgent_RewardsOfARobotWithoutArmAndZones_AreZeroAndDoNotThrow() {
            var robot = Robot("A");
            Object.DestroyImmediate(robot.GetComponentInChildren<ArmComponent>().gameObject);
            var agent = robot.GetComponent<RobotAgent>();
            typeof(RobotAgent).GetField("robotMovement", Private).SetValue(agent, robot.GetComponent<RobotMovement>());
            typeof(RobotAgent).GetField("zoneComponents", Private).SetValue(agent, new CollisionZoneBehaviour[0]);

            Assert.AreEqual(0f, (float)Call(agent, "GetRewards_ArmRelease"));
            Assert.AreEqual(0f, (float)Call(agent, "GetRewards_collisionCombination"));
        }

        [Test]
        public void StatisticsBoard_ForARobotWithOneWheelSet_DoesNotThrow() {
            var robot = Robot("A");
            Object.DestroyImmediate(robot.GetComponentsInChildren<WheelComponent>()[1].gameObject);
            var boards = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Turnaj/Prefabs/StatsBoards.prefab"));
            spawned.Add(boards);
            var gui = boards.GetComponentInChildren<StatisticsGUI>();
            Call(gui, "Start");
            foreach (var bar in boards.GetComponentsInChildren<StatsBarGUI>()) Call(bar, "Start");
            var board = (GameObject)typeof(StatisticsGUI).GetField("robotBoardA", Private).GetValue(gui);

            Assert.DoesNotThrow(() => Call(gui, "UpdateRobotBoard", board, robot.GetComponent<RobotAgent>()));
        }

        // ---------------------------------------------------------------- shared keyboard input

        [Test]
        public void DisablingOneRobot_KeepsKeyboardInputOnForTheOthers() {
            var a = Robot("A").GetComponent<RobotMovement>();
            var b = Robot("B").GetComponent<RobotMovement>();
            Call(a, "OnEnable");
            Call(b, "OnEnable");
            try {
                Call(a, "OnDisable");
                Assert.IsTrue(b.leftWheelsAxis.enabled, "robot B must keep its input when robot A is disabled");
            } finally {
                Call(b, "OnDisable");
            }
            Assert.IsFalse(b.leftWheelsAxis.enabled, "input goes off with the last robot");
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

        // ---- AIJamManager: team arena in the training scene ----

        private const string ArenaPath = "Assets/Turnaj/Prefabs/NeutralArena.prefab";

        /// <summary>A training environment like in TrainingScene: the built-in neutral arena is the child "Arena".</summary>
        private GameObject Environment(out GameObject neutralArena) {
            var env = new GameObject("Environment test");
            spawned.Add(env);
            env.transform.position = new Vector3(7f, 0f, 0f);
            neutralArena = new GameObject("Arena");
            neutralArena.transform.SetParent(env.transform, false);
            return env;
        }

        [Test]
        public void TeamArena_WhenLinked_ReplacesTheNeutralArena() {
            var env = Environment(out var neutral);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArenaPath);

            var arena = AIJamManager.UseTeamArena(env, prefab);

            Assert.IsNotNull(arena, "team arena instance");
            Assert.IsFalse(neutral.activeSelf, "the neutral arena must be switched off");
            Assert.AreEqual(env.transform, arena.transform.parent);
            Assert.AreEqual(Vector3.zero, arena.transform.localPosition, "in the centre of the environment");
            Assert.AreEqual(prefab.name, arena.name);
            Assert.IsTrue(arena.activeSelf);
        }

        [Test]
        public void TeamArena_WhenNotLinked_KeepsTheNeutralArena() {
            var env = Environment(out var neutral);

            var arena = AIJamManager.UseTeamArena(env, null);

            Assert.IsNull(arena);
            Assert.IsTrue(neutral.activeSelf);
            Assert.AreEqual(1, env.transform.childCount);
        }

        [Test]
        public void TeamArena_KeepsTheRotationOfThePrefab() {
            var env = Environment(out _);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArenaPath);

            var arena = AIJamManager.UseTeamArena(env, prefab);

            Assert.AreEqual(prefab.transform.rotation.eulerAngles, arena.transform.localRotation.eulerAngles);
        }

        // ---- DemoDriver: scripted driver for recording demonstrations ----

        [Test]
        public void DemoDriver_EnemyStraightAhead_DrivesBothTracksForward() {
            DemoDriver.Steer(new Vector3(0f, 0f, 3f), out float left, out float right);
            Assert.AreEqual(1f, left, 1e-4f);
            Assert.AreEqual(1f, right, 1e-4f);
        }

        [Test]
        public void DemoDriver_EnemyToTheRight_TurnsRight() {
            DemoDriver.Steer(new Vector3(2f, 0f, 2f), out float left, out float right);
            Assert.Greater(left, right, "the left track must push harder to turn right");
            Assert.Greater(left, 0f);
        }

        [Test]
        public void DemoDriver_EnemyToTheLeft_TurnsLeft() {
            DemoDriver.Steer(new Vector3(-2f, 0f, 2f), out float left, out float right);
            Assert.Greater(right, left);
            Assert.Greater(right, 0f);
        }

        [Test]
        public void DemoDriver_EnemyBehind_TurnsOnTheSpot() {
            DemoDriver.Steer(new Vector3(0.5f, 0f, -3f), out float left, out float right);
            Assert.Greater(left, 0f);
            Assert.Less(right, 0f);
        }

        [Test]
        public void DemoDriver_AlwaysGivesFullCommands_LikeAKeyboard() {
            // A model action changes the motor torque step by step, so only a held full command has a clear meaning.
            // Small or zero actions would teach the imitating robot to do nothing.
            for (int deg = -180; deg <= 180; deg += 5) {
                var enemy = Quaternion.Euler(0f, deg, 0f) * Vector3.forward * 2f;
                DemoDriver.Steer(enemy, out float left, out float right);
                Assert.AreEqual(1f, Mathf.Abs(left), 1e-4f, "left track at " + deg + " degrees");
                Assert.AreEqual(1f, Mathf.Abs(right), 1e-4f, "right track at " + deg + " degrees");
            }
        }

        [Test]
        public void DemoDriver_EnemySlightlyOffCentre_StillDrivesStraight() {
            DemoDriver.Steer(Quaternion.Euler(0f, 5f, 0f) * Vector3.forward * 2f, out float left, out float right);
            Assert.AreEqual(1f, left, 1e-4f);
            Assert.AreEqual(1f, right, 1e-4f);
        }

        [Test]
        public void DemoDriver_PushingAgainstTheEnemyWithoutMoving_IsStalled() {
            Assert.IsTrue(DemoDriver.Stalled(0.5f, 0.1f), "touching and standing");
            Assert.IsFalse(DemoDriver.Stalled(0.5f, 1.5f), "touching but still moving: the ram is in progress");
            Assert.IsFalse(DemoDriver.Stalled(3f, 0f), "far away: just starting to drive");
        }

        [Test]
        public void DemoDriver_ArmAction_LiftsOnlyWhenTheEnemyIsCloseInFront() {
            Assert.AreEqual(1, DemoDriver.ArmAction(new Vector3(0.1f, 0f, 0.8f)));
            Assert.AreEqual(0, DemoDriver.ArmAction(new Vector3(0f, 0f, 3f)), "too far");
            Assert.AreEqual(0, DemoDriver.ArmAction(new Vector3(0f, 0f, -0.8f)), "behind");
        }
    }
}
