using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Turnaj;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEngine;

namespace AIJam.Validation {
    public enum TestStatus { Pass, Warn, Fail, Info }

    [System.Serializable]
    public class TestResult {
        public string Event;
        public TestStatus Status;
        public string Text;
        public override string ToString() => $"{Status.ToString().ToUpper(),-4} {Event}: {Text}";
    }

    /// <summary>
    /// Validation scene (one team at a time). In Play mode:
    ///  - Run validation: full rule check of the team's robot + arena prefabs (TeamValidation.FullCheck).
    ///  - Run test match: the team's robot in its own arena vs. a scripted enemy, six measured events.
    /// Buttons live in the custom Inspector (ValidationRunnerEditor). Nothing here is shown on the Game view.
    /// </summary>
    public class ValidationRunner : MonoBehaviour {
        public static readonly string[] EventNames = { "Enemy rams", "Robot falls", "Traps and buffs", "Robot flips", "Body hit", "Battery hit" };

        [Header("Team")]
        public List<soTeam> Teams = new List<soTeam>();
        [HideInInspector] public int TeamIndex;

        [Header("One robot or one arena (optional)")]
        [Tooltip("Robot prefab for 'Validate robot only'. Empty = the robot of the picked team.")]
        public GameObject SingleRobot;
        [Tooltip("Arena prefab for 'Validate arena only'. Empty = the arena of the picked team.")]
        public GameObject SingleArena;

        [Header("Setup")]
        public ValidationRules Rules;
        [Tooltip("Scripted enemy (a valid robot; its model is not used).")]
        public GameObject EnemyPrefab;
        public Camera SpectatorCamera;

        [Header("Test match")]
        public float RamDuration = 5f;
        public float EnemyTurnGain = 2f;
        public float ProbeSpeed = 4f;
        public float TrapDriveTime = 4f;
        [Tooltip("Throttle used to drive the robot through each trigger.")]
        public float TrapThrottle = 0.6f;

        // ---- state for the Inspector ----
        public List<Finding> Findings { get; private set; } = new List<Finding>();
        public string FindingsReport { get; private set; } = "";
        public List<TestResult> Results { get; } = new List<TestResult>();
        public bool Busy { get; private set; }
        public string Status { get; private set; } = "";
        public RenderTexture AgentView { get; private set; }
        public soTeam Team => TeamIndex >= 0 && TeamIndex < Teams.Count ? Teams[TeamIndex] : null;
        public GameObject RobotInstance => robot != null ? robot.gameObject : null;
        public GameObject ArenaInstance => arena;
        /// <summary>Prefabs the last validation looked at (for selecting the object of a finding).</summary>
        public GameObject CheckedRobot { get; private set; }
        public GameObject CheckedArena { get; private set; }
        public GameObject RobotToCheck => SingleRobot != null ? SingleRobot : Team != null ? Team.RobotPrefab : null;
        public GameObject ArenaToCheck => SingleArena != null ? SingleArena : Team != null ? Team.ArenaPrefab : null;

        private GameObject arena, matchRoot;
        private SceneRoots roots; // arena rule: runtime spawns must stay under the arena
        private RobotAgent robot, enemy;
        private RobotMovement robotMove, enemyMove;
        private int respawns;
        private float nextPreview;

        // ------------------------------------------------------------------ setup

        private void Start() {
            if (FindAnyObjectByType<RewardEditor>() == null) new GameObject("RewardEditor (auto)").AddComponent<RewardEditor>();
            AgentView = new RenderTexture(128, 128, 16) { name = "Agent view", filterMode = FilterMode.Point };
            if (Team == null || Team.RobotPrefab == null || Team.ArenaPrefab == null) {
                Status = "Pick a team with a robot and an arena, then press Play.";
                return;
            }
            roots = SceneRoots.Take();
            arena = Instantiate(Team.ArenaPrefab, Vector3.zero, Team.ArenaPrefab.transform.rotation);
            arena.name = "Arena (" + Team.DisplayName + ")";
            matchRoot = new GameObject("Match");
            matchRoot.SetActive(false); // both robots must exist before RobotAgent.Start looks for its enemy
            robot = Spawn(Team.RobotPrefab, Team.DisplayName);
            if (EnemyPrefab != null) enemy = Spawn(EnemyPrefab, "Scripted enemy");
            matchRoot.SetActive(true);
            robotMove = robot.GetComponent<RobotMovement>();
            enemyMove = enemy != null ? enemy.GetComponent<RobotMovement>() : null;
            robot.OnRobotRespawn += (s, e) => { respawns++; Place(robot, true); };
            if (enemy != null) enemy.OnRobotRespawn += (s, e) => Place(enemy, false);
            StartCoroutine(PlaceNextFrame());
            if (SpectatorCamera != null) {
                var angle = arena.GetComponent<ArenaViewAngle>();
                float yaw = (angle != null ? angle.Yaw : 120f) + arena.transform.eulerAngles.y;
                var pivot = SpectatorCamera.transform.parent;
                if (pivot != null) pivot.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
            Status = "Ready. Run validation or a test event.";
        }

        private IEnumerator PlaceNextFrame() {
            yield return null;
            Place(robot, true);
            if (enemy != null) Place(enemy, false);
        }

        private RobotAgent Spawn(GameObject prefab, string name) {
            var go = Instantiate(prefab, matchRoot.transform);
            go.name = name;
            var bp = go.GetComponent<BehaviorParameters>();
            if (bp != null) bp.BehaviorType = BehaviorType.InferenceOnly;
            VisionLayers.ForceAgentCamera(go);
            // The test events drive the robots themselves. A disabled DecisionRequester still requests
            // decisions (it ignores its enabled flag), so remove it: no model decisions in this scene.
            var dr = go.GetComponent<DecisionRequester>();
            if (dr != null) DestroyImmediate(dr);
            return go.GetComponent<RobotAgent>();
        }

        private void Update() {
            if (robot == null || Time.unscaledTime < nextPreview) return;
            nextPreview = Time.unscaledTime + 0.2f;
            var cam = robot.GetComponentsInChildren<Camera>(true).FirstOrDefault(c => c.CompareTag("RobotCamera"));
            if (cam == null) return;
            var old = cam.targetTexture;
            cam.targetTexture = AgentView;
            cam.Render();
            cam.targetTexture = old;
        }

        // ------------------------------------------------------------------ 1. validation

        public void RunValidation() {
            CheckedRobot = Team != null ? Team.RobotPrefab : null;
            CheckedArena = Team != null ? Team.ArenaPrefab : null;
            Show(TeamValidation.FullCheck(Team, Rules), Team != null ? Team.DisplayName : "no team");
        }

        /// <summary>Rule check of one robot prefab only: no team, no arena needed.</summary>
        public void RunRobotValidation() {
            CheckedRobot = RobotToCheck;
            CheckedArena = null;
            Show(TeamValidation.RobotCheck(CheckedRobot, Rules), "robot " + (CheckedRobot != null ? CheckedRobot.name : "(none)"));
        }

        /// <summary>Rule check of one arena prefab only: no team, no robot needed.</summary>
        public void RunArenaValidation() {
            CheckedRobot = null;
            CheckedArena = ArenaToCheck;
            Show(TeamValidation.ArenaCheck(CheckedArena, Rules), "arena " + (CheckedArena != null ? CheckedArena.name : "(none)"));
        }

        private void Show(List<Finding> findings, string what) {
            Findings = findings;
            FindingsReport = AIJam.Validation.Findings.Report($"Validation – {what}", Findings);
            Debug.Log("[Validation] " + FindingsReport);
        }

        // ------------------------------------------------------------------ 2. test match

        public void RunTest(int index) {
            if (Busy || robot == null) return;
            StartCoroutine(RunEvents(index < 0 ? Enumerable.Range(0, EventNames.Length).ToArray() : new[] { index }));
        }

        private IEnumerator RunEvents(int[] events) {
            Busy = true;
            if (events.Length > 1) Results.Clear();
            foreach (int i in events) {
                Status = $"Running: {EventNames[i]}";
                yield return ResetRound();
                switch (i) {
                    case 0: yield return Ram(); break;
                    case 1: yield return Fall(); break;
                    case 2: yield return Traps(); break;
                    case 3: yield return Flip(); break;
                    case 4: yield return Hit("body", "Body hit"); break;
                    case 5: yield return Hit("battery", "Battery hit"); break;
                }
            }
            yield return ResetRound();
            CheckArenaSpawns();
            Status = "Done. " + Results.Count(r => r.Status == TestStatus.Fail) + " failed, " + Results.Count(r => r.Status == TestStatus.Warn) + " warnings.";
            Debug.Log("[Test match]\n" + string.Join("\n", Results));
            Busy = false;
        }

        public string ResultsReport() =>
            $"Test match – {(Team != null ? Team.DisplayName : "")}\n" + string.Join("\n", Results.Select(r => "  " + r));

        /// <summary>Everything the arena spawned during the events must be a child of the arena object.</summary>
        private void CheckArenaSpawns() {
            const string evt = "Arena spawns";
            if (roots == null) return;
            var stray = roots.NewSince(arena, matchRoot);
            if (stray.Count == 0) Add(evt, TestStatus.Pass, "nothing was spawned outside the arena object");
            else Add(evt, TestStatus.Fail, $"{stray.Count} object(s) spawned outside the arena (must be children of the arena so they are destroyed with it): {SceneRoots.Names(stray)}");
        }

        private void Add(string evt, TestStatus s, string text) {
            Results.RemoveAll(r => r.Event == evt);
            Results.Add(new TestResult { Event = evt, Status = s, Text = text });
        }

        /// <summary>Full HP, both robots on their markers, passive (no model decisions, no torque).</summary>
        private IEnumerator ResetRound() {
            SetPassive(robot, true);
            SetPassive(enemy, true);
            robot.EndEpisode();             // resets every zone's HP, fires OnRobotRespawn -> Place
            if (enemy != null) enemy.EndEpisode();
            Place(robot, true);
            if (enemy != null) Place(enemy, false);
            respawns = 0;
            yield return new WaitForSeconds(0.5f);
        }

        private static void SetPassive(RobotAgent r, bool passive) {
            var m = r != null ? r.GetComponent<RobotMovement>() : null;
            if (m != null && passive) m.ApplyBothTrackInputsHeuristic(0f, 0f);
        }

        // 1. enemy drives straight at the robot
        private IEnumerator Ram() {
            const string evt = "Enemy rams";
            if (enemy == null) { Add(evt, TestStatus.Fail, "No scripted enemy assigned."); yield break; }
            var before = Zones(robot);
            float hpBefore = Hp(robot);
            float end = Time.time + RamDuration;
            while (Time.time < end) {
                Drive(enemyMove, enemy.transform, robot.transform.position, 1f);
                yield return new WaitForFixedUpdate();
            }
            enemyMove.ApplyBothTrackInputsHeuristic(0f, 0f);
            var after = Zones(robot);
            var hit = Changed(before, after);
            Add(evt, hit.Count > 0 ? TestStatus.Pass : TestStatus.Warn,
                hit.Count > 0 ? $"damaged {string.Join(", ", hit)}; robot HP {Pct(hpBefore)} → {Pct(Hp(robot))}"
                              : "no zone took damage (the enemy did not reach the robot?)");
        }

        // 2. robot dropped outside the arena
        private IEnumerator Fall() {
            const string evt = "Robot falls";
            float hpBefore = HpSum(robot);
            var marker = Marker(true);
            SetKinematic(robot, false);
            robot.transform.SetPositionAndRotation(new Vector3(30f, 0.5f, 0f), Quaternion.identity);
            Physics.SyncTransforms();
            float end = Time.time + 6f;
            while (respawns == 0 && Time.time < end) yield return null;
            yield return new WaitForSeconds(0.3f);
            if (respawns == 0) { Add(evt, TestStatus.Fail, "the robot did not respawn after falling out (fall detection missing?)"); yield break; }
            float dist = marker != null ? Flat(robot.transform.position - marker.position) : -1f;
            bool lost = HpSum(robot) < hpBefore - 1e-3f;
            Add(evt, dist >= 0f && dist < 0.3f && lost ? TestStatus.Pass : TestStatus.Fail,
                $"respawned {(dist >= 0f ? $"{dist:F2} m from its Home marker" : "(no marker)")}, zone HP lowered: {(lost ? "yes" : "NO")}");
        }

        // 3. drive through each trigger in the arena
        private IEnumerator Traps() {
            const string evt = "Traps and buffs";
            var triggers = arena.GetComponentsInChildren<Collider>(true).Where(c => c.enabled && c.isTrigger).ToList();
            if (triggers.Count == 0) { Add(evt, TestStatus.Info, "the arena has no traps or buffs (no trigger colliders)"); yield break; }
            var lines = new List<string>();
            foreach (var t in triggers) {
                // control run with the trigger off, then the real run: the difference is the trap's effect
                var area = t.bounds; // a disabled collider reports empty bounds, so capture it first
                var control = new TrapPass();
                t.enabled = false;
                yield return DriveThrough(area, control);
                t.enabled = true;
                var real = new TrapPass();
                yield return DriveThrough(area, real);
                string speed = !real.Entered ? "robot never got inside"
                    : control.SpeedInside > 1e-3f ? $"speed inside {real.SpeedInside / control.SpeedInside * 100f:0} % of the same drive without it" : "speed n/a";
                lines.Add($"'{t.name}': HP {Pct(real.HpBefore)} → {Pct(real.HpAfter)}, {speed}");
            }
            Add(evt, TestStatus.Info, string.Join("; ", lines));
        }

        private class TrapPass { public bool Entered; public float SpeedInside, HpBefore, HpAfter; }

        /// <summary>Drives the robot in a straight line through the trigger's area on its own wheels and measures it.</summary>
        private IEnumerator DriveThrough(Bounds b, TrapPass result) {
            yield return ResetRound();
            var target = b.center; target.y = 0f;
            var dir = target.sqrMagnitude > 0.01f ? target.normalized : Vector3.forward; // from the centre side, outward
            var start = target - dir * (0.6f + Mathf.Max(b.extents.x, b.extents.z));
            start.y = FloorHeight(start) + 0.03f;
            robot.transform.SetPositionAndRotation(start, Quaternion.LookRotation(dir));
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.3f);
            result.HpBefore = Hp(robot);
            var rb = robot.GetComponent<Rigidbody>();
            float sum = 0f; int n = 0;
            float end = Time.time + TrapDriveTime;
            while (Time.time < end) {
                Drive(robotMove, robot.transform, target + dir * 3f, TrapThrottle);
                bool inside = b.Contains(new Vector3(robot.transform.position.x, b.center.y, robot.transform.position.z));
                if (inside) { sum += rb.linearVelocity.magnitude; n++; }
                else if (n > 0) break;
                yield return new WaitForFixedUpdate();
            }
            robotMove.ApplyBothTrackInputsHeuristic(0f, 0f);
            result.Entered = n > 0;
            result.SpeedInside = n > 0 ? sum / n : 0f;
            result.HpAfter = Hp(robot);
        }

        // 4. robot turned upside down
        private IEnumerator Flip() {
            const string evt = "Robot flips";
            float hpBefore = HpSum(robot);
            var p = robot.transform.position;
            SetKinematic(robot, false);
            robot.transform.SetPositionAndRotation(p + Vector3.up * 0.4f, Quaternion.Euler(180f, robot.transform.eulerAngles.y, 0f));
            Physics.SyncTransforms();
            float end = Time.time + 4f;
            while (respawns == 0 && Time.time < end) yield return null;
            yield return new WaitForSeconds(0.3f);
            bool lost = HpSum(robot) < hpBefore - 1e-3f;
            Add(evt, respawns > 0 && lost ? TestStatus.Pass : TestStatus.Fail,
                respawns > 0 ? $"respawned upright, zone HP lowered: {(lost ? "yes" : "NO")}" : "no flip detected / no respawn");
        }

        // 5–6. weapon probe launched at a zone from a side where it is exposed
        private IEnumerator Hit(string keyword, string evt) {
            var target = robot.GetComponentsInChildren<CollisionZoneBehaviour>(true).FirstOrDefault(z => PartCatalog.ZoneKeyword(z.name) == keyword);
            var col = target != null ? target.GetComponent<Collider>() : null;
            if (col == null) { Add(evt, TestStatus.Fail, $"the robot has no '{keyword}' zone"); yield break; }
            SetKinematic(robot, true); // keep it still so the probe hits where it aims
            Physics.SyncTransforms();
            if (!FindExposedSide(col, out var from, out var aim)) {
                Add(evt, TestStatus.Warn, $"the {keyword} can't be reached from outside (fully covered by other parts)");
                SetKinematic(robot, false);
                yield break;
            }
            float zoneBefore = target.CurrentHealth, hpBefore = Hp(robot);
            var probe = MakeProbe(from);
            probe.linearVelocity = (aim - from).normalized * ProbeSpeed;
            float end = Time.time + 1.5f;
            while (Time.time < end && Mathf.Approximately(target.CurrentHealth, zoneBefore)) yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(0.2f);
            Destroy(probe.gameObject);
            // measure while the robot is still held, then release it
            float zoneAfter = target.CurrentHealth, hpAfter = Hp(robot);
            SetKinematic(robot, false);
            bool dropped = zoneAfter < zoneBefore - 1e-4f;
            Add(evt, dropped ? TestStatus.Pass : TestStatus.Fail,
                $"{target.name} {Pct(zoneBefore)} → {Pct(zoneAfter)}, robot HP {Pct(hpBefore)} → {Pct(hpAfter)}");
        }

        private bool FindExposedSide(Collider target, out Vector3 from, out Vector3 aim) {
            var cols = robot.GetComponentsInChildren<Collider>(true).Where(c => c.enabled && !c.isTrigger && !(c is WheelCollider)).ToList();
            aim = target.bounds.center;
            var root = robot.transform;
            var dirs = new[] { root.up, root.forward, -root.forward, root.right, -root.right, (root.up + root.forward).normalized, (root.up - root.forward).normalized };
            foreach (var d in dirs) {
                var ray = new Ray(aim + d * 1.5f, -d);
                Collider first = null; float best = float.MaxValue;
                foreach (var c in cols) if (c.Raycast(ray, out var h, 3f) && h.distance < best) { best = h.distance; first = c; }
                if (first == target) { from = aim + d * 0.6f; return true; }
            }
            from = default;
            return false;
        }

        private static Rigidbody MakeProbe(Vector3 at) {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Weapon probe";
            go.tag = "RobotArm";
            go.transform.position = at;
            go.transform.localScale = Vector3.one * 0.08f;
            var zone = go.AddComponent<CollisionZoneBehaviour>();
            var weaponStats = CollisionForwarder.StatsFor("arm_weapon_shape");
            zone.damageOfZone = weaponStats.damage;
            zone.resistanceOfZone = weaponStats.resistance;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            return rb;
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>Height of the arena floor below a point (arena colliders only).</summary>
        private float FloorHeight(Vector3 p) {
            float best = 0f; bool found = false;
            var ray = new Ray(new Vector3(p.x, 3f, p.z), Vector3.down);
            foreach (var c in arena.GetComponentsInChildren<Collider>(true).Where(c => c.enabled && !c.isTrigger))
                if (c.Raycast(ray, out var h, 6f) && (!found || h.point.y > best)) { best = h.point.y; found = true; }
            return found ? best : 0f;
        }

        private Transform Marker(bool home) {
            var sp = TeamValidation.Find(arena, home ? SpawnSide.Home : SpawnSide.Guest);
            return sp != null ? sp.transform : null;
        }

        private void Place(RobotAgent r, bool home) {
            var m = Marker(home);
            if (r == null || m == null) return;
            foreach (var rb in r.GetComponentsInChildren<Rigidbody>()) {
                if (rb.isKinematic) continue;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            r.transform.SetPositionAndRotation(m.position + Vector3.up * 0.022f, Quaternion.Euler(0f, m.eulerAngles.y, 0f));
            Physics.SyncTransforms();
        }

        private static void SetKinematic(RobotAgent r, bool k) {
            foreach (var rb in r.GetComponentsInChildren<Rigidbody>()) {
                if (!rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
                rb.isKinematic = k;
            }
        }

        /// <summary>Simple tank steering toward a point.</summary>
        private void Drive(RobotMovement m, Transform self, Vector3 target, float throttle) {
            var to = target - self.position; to.y = 0f;
            float angle = Vector3.SignedAngle(Vector3.ProjectOnPlane(self.forward, Vector3.up), to, Vector3.up) / 90f;
            float turn = Mathf.Clamp(angle * EnemyTurnGain, -1f, 1f);
            float fwd = throttle * Mathf.Clamp01(1f - Mathf.Abs(turn) * 0.5f);
            m.ApplyBothTrackInputsHeuristic(Mathf.Clamp(fwd + turn, -1f, 1f), Mathf.Clamp(fwd - turn, -1f, 1f));
        }

        private static Dictionary<CollisionZoneBehaviour, float> Zones(RobotAgent r) =>
            r.GetComponentsInChildren<CollisionZoneBehaviour>(true).ToDictionary(z => z, z => z.CurrentHealth);

        private static List<string> Changed(Dictionary<CollisionZoneBehaviour, float> a, Dictionary<CollisionZoneBehaviour, float> b) =>
            a.Where(kv => b[kv.Key] < kv.Value - 1e-4f).Select(kv => $"{kv.Key.name} {Pct(kv.Value)} → {Pct(b[kv.Key])}").ToList();

        /// <summary>Same formula as RobotAgent.CheckIfDied, computed now (the agent's cached value lags a frame).</summary>
        private static float Hp(RobotAgent r) {
            var parts = r.GetComponentsInChildren<IHealth>(true);
            float w = parts.Sum(h => h.influenceOnTotalHealth);
            return w > 0f ? parts.Sum(h => h.CurrentHealth * h.influenceOnTotalHealth) / w : 0f;
        }

        private static float HpSum(RobotAgent r) => r.GetComponentsInChildren<CollisionZoneBehaviour>(true).Sum(z => z.CurrentHealth);
        private static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }
        private static string Pct(float v) => $"{v * 100f:0} %";

        private void OnDestroy() {
            if (AgentView != null) AgentView.Release();
        }
    }
}
