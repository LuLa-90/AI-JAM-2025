using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Turnaj {
    public enum MatchPhase { Invalid, Waiting, Countdown, Fight, SuddenDeath, RoundOver, MatchOver }
    public enum RoundEndReason { Knockout, Timeout, SuddenDeath, Referee }

    [Serializable]
    public class RoundResult {
        public int Round;
        public string Arena;
        public bool WinnerIsA;
        public string Winner;
        public RoundEndReason Reason;
        public float HpA;
        public float HpB;
        public bool Flawless;
    }

    /// <summary>
    /// Runs ONE match (best of 3 rounds) per Play session. Teams are picked in the Inspector before Play.
    /// Round 1 = Team A's arena (A home), round 2 = Team B's arena (B home), round 3 = neutral arena (A home).
    /// Every round end goes through RobotAgent's own death path (zones to 0 -> OnRobotDie -> EndEpisode).
    /// Referee keys (Game view focused): Space start, W×2 separator, Q×2 / E×2 knockout A / B, R×2 replay round, T pause.
    /// </summary>
    public class TournamentManager : MonoBehaviour {
        [Header("Teams (pick A and B in the dropdowns above)")]
        public List<soTeam> Teams = new List<soTeam>();
        [HideInInspector] public int TeamAIndex = 0;
        [HideInInspector] public int TeamBIndex = 1;
        [Tooltip("Arena for the final round. Same rules as team arenas (Home + Guest SpawnPoints).")]
        public GameObject NeutralArenaPrefab;

        [Header("Rules")]
        public float RoundDuration = 90f;
        public float DoubleTapWindow = 0.4f;
        [Tooltip("Velocity change (m/s) pushing each robot away from the other.")]
        public float SeparatorSpeed = 2.5f;
        [Tooltip("Upward velocity change (m/s) so stuck robots come loose.")]
        public float SeparatorLift = 0.8f;
        public float SeparatorCooldown = 1f;
        [Tooltip("If the opponent is this close to a robot's marker, the robot respawns at the mirrored point.")]
        public float RespawnClearance = 0.5f;
        [Tooltip("Seconds the knockout is shown before the next round's waiting screen.")]
        public float RoundOverDelay = 4f;

        [Header("Scene")]
        public TournamentHUD Hud;
        public TournamentCameras Cameras;
        public AudioSource Voice;

        [Header("Sounds")]
        public AudioClip Round1Clip;
        public AudioClip Round2Clip;
        public AudioClip FinalRoundClip;
        public AudioClip FlawlessClip;
        public AudioClip RoundWonClip;
        [Tooltip("Optional tick for 3-2-1.")]
        public AudioClip CountdownTickClip;
        [Tooltip("Optional sound on FIGHT!!")]
        public AudioClip FightClip;

        // ---- state (read by the custom Inspector / Scene overlay) ----
        public MatchPhase Phase { get; private set; } = MatchPhase.Invalid;
        public int RoundIndex { get; private set; }
        public int ScoreA { get; private set; }
        public int ScoreB { get; private set; }
        public float TimeLeft { get; private set; }
        public bool Paused { get; private set; }
        public List<RoundResult> Results { get; } = new List<RoundResult>();
        public string Hint { get; private set; } = "";
        public float HintTime { get; private set; } = -100f;
        public RobotAgent RobotA => robotA;
        public RobotAgent RobotB => robotB;

        public soTeam TeamA => TeamAt(TeamAIndex);
        public soTeam TeamB => TeamAt(TeamBIndex);
        public string NameA => TeamA != null ? TeamA.DisplayName : "Team A";
        public string NameB => TeamB != null ? TeamB.DisplayName : "Team B";

        private RobotAgent robotA, robotB;
        private GameObject matchRoot, arena;
        private DoubleTap tapW, tapQ, tapE, tapR;
        private float lastSeparator = -100f;
        private Coroutine countdown, afterRound;
        private RobotAgent pendingLoser;
        private RoundEndReason pendingReason;
        private Vector2 timeoutHp;
        private readonly Dictionary<RobotAgent, float> suddenDeathDamage = new Dictionary<RobotAgent, float>();

        private soTeam TeamAt(int i) => i >= 0 && i < Teams.Count ? Teams[i] : null;

        /// <summary>Everything that blocks the match from starting. Empty = OK.</summary>
        public List<string> ValidationProblems() {
            var problems = new List<string>();
            if (TeamA == null || TeamB == null) problems.Add("Pick Team A and Team B.");
            else if (TeamAIndex == TeamBIndex || TeamA == TeamB) problems.Add("Team A and Team B are the same team.");
            if (TeamA != null) problems.AddRange(TeamValidation.CheckTeam(TeamA));
            if (TeamB != null && TeamB != TeamA) problems.AddRange(TeamValidation.CheckTeam(TeamB));
            problems.AddRange(TeamValidation.CheckArena(NeutralArenaPrefab, "Neutral arena"));
            if (Hud == null) problems.Add("HUD reference is missing.");
            if (Cameras == null) problems.Add("Cameras reference is missing.");
            return problems;
        }

        // ------------------------------------------------------------------ setup

        private void Awake() {
            tapW = new DoubleTap(DoubleTapWindow);
            tapQ = new DoubleTap(DoubleTapWindow);
            tapE = new DoubleTap(DoubleTapWindow);
            tapR = new DoubleTap(DoubleTapWindow);
            // RobotAgent looks these up; the tournament scene needs one of each.
            if (FindAnyObjectByType<RewardEditor>() == null) new GameObject("RewardEditor (auto)").AddComponent<RewardEditor>();
            if (FindAnyObjectByType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
            Time.timeScale = 1f;
        }

        private void Start() {
            var problems = ValidationProblems();
            if (problems.Count > 0) {
                Phase = MatchPhase.Invalid;
                foreach (var p in problems) Debug.LogError("[Turnaj] " + p);
                if (Hud != null) Hud.Clear();
                return;
            }

            matchRoot = new GameObject($"Match {NameA} vs {NameB}");
            matchRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            matchRoot.SetActive(false); // both robots must exist before RobotAgent.Start looks for its enemy
            robotA = SpawnRobot(TeamA);
            robotB = SpawnRobot(TeamB);
            matchRoot.SetActive(true);

            Cameras.SetChaseCameras(AddChaseCamera(robotA), AddChaseCamera(robotB));
            Hud.SetRobots(robotA, robotB);
            Hud.SetScore(NameA, 0, 0, NameB);
            LoadRound(0);
        }

        private RobotAgent SpawnRobot(soTeam team) {
            var go = Instantiate(team.RobotPrefab, matchRoot.transform);
            go.name = team.DisplayName;
            var bp = go.GetComponent<BehaviorParameters>();
            if (bp != null) bp.BehaviorType = BehaviorType.InferenceOnly; // never train by accident
            var robot = go.GetComponent<RobotAgent>();
            robot.OnRobotDie += (s, e) => OnRobotDied(robot);
            robot.OnRobotRespawn += (s, e) => OnRobotRespawned(robot);
            foreach (var zone in go.GetComponentsInChildren<CollisionZoneBehaviour>(true)) {
                zone.OnCollision += (s, e) => OnZoneHit(robot, e);
            }
            SetFrozen(robot, true);
            return robot;
        }

        private Camera AddChaseCamera(RobotAgent robot) {
            var go = new GameObject("Chase Camera");
            go.transform.SetParent(robot.transform, false);
            go.transform.localPosition = new Vector3(0f, 0.835f, -1.075f);
            go.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.05f;
            cam.enabled = false;
            return cam;
        }

        // ------------------------------------------------------------------ rounds

        private void LoadRound(int round) {
            RoundIndex = round;
            ResetRoundState();
            if (arena != null) Destroy(arena);
            var owner = MatchRules.ArenaForRound(round);
            var prefab = owner == ArenaOwner.TeamA ? TeamA.ArenaPrefab : owner == ArenaOwner.TeamB ? TeamB.ArenaPrefab : NeutralArenaPrefab;
            arena = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            arena.name = $"Arena R{round + 1} ({(owner == ArenaOwner.Neutral ? "neutral" : owner == ArenaOwner.TeamA ? NameA : NameB)})";
            Cameras.SetArena(arena);
            EnterWaiting();
        }

        private void EnterWaiting() {
            Phase = MatchPhase.Waiting;
            SetFrozen(robotA, true);
            SetFrozen(robotB, true);
            PlaceAtMarker(robotA, false);
            PlaceAtMarker(robotB, false);
            Hud.ShowWaiting(NameA, NameB, MatchRules.RoundTitle(RoundIndex));
            Cameras.ShowWaiting();
        }

        private void ResetRoundState() {
            StopRoutine(ref countdown);
            StopRoutine(ref afterRound);
            SetPaused(false);
            pendingLoser = null;
            suddenDeathDamage.Clear();
            TimeLeft = RoundDuration;
        }

        private IEnumerator Countdown() {
            Phase = MatchPhase.Countdown;
            Play(RoundIndex == 0 ? Round1Clip : RoundIndex == 1 ? Round2Clip : FinalRoundClip);
            Hud.ShowFightLayout();
            Hud.SetTimer(RoundDuration, false);
            Hud.ShowCenter(MatchRules.RoundTitle(RoundIndex), Hud.AccentColor);
            Cameras.ShowFront();
            yield return Wait(1.5f);
            for (int i = 3; i >= 1; i--) {
                Hud.ShowCenter(i.ToString(), Color.white);
                Play(CountdownTickClip);
                yield return Wait(1f);
            }
            Hud.ShowCenter("FIGHT!!", Hud.DangerColor, 1.2f);
            Play(FightClip);
            TimeLeft = RoundDuration;
            SetFrozen(robotA, false);
            SetFrozen(robotB, false);
            Phase = MatchPhase.Fight;
            countdown = null;
        }

        /// <summary>Real-time wait that stops while paused.</summary>
        private IEnumerator Wait(float seconds) {
            while (seconds > 0f) {
                if (!Paused) seconds -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void Update() {
            HandleKeys();
            if (Phase == MatchPhase.Fight) {
                TimeLeft -= Time.deltaTime;
                if (TimeLeft <= 0f) {
                    TimeLeft = 0f;
                    OnTimeUp();
                }
            }
            if (Phase == MatchPhase.Fight || Phase == MatchPhase.SuddenDeath) Hud.SetTimer(TimeLeft, Phase == MatchPhase.SuddenDeath);
        }

        /// <summary>Higher HP wins; equal HP -> sudden death in the final round, draw (replay) otherwise.</summary>
        private void OnTimeUp() {
            if (pendingLoser != null) return;
            float hpA = robotA.weightedAverageHealth, hpB = robotB.weightedAverageHealth;
            switch (MatchRules.ResolveTimeout(RoundIndex, hpA, hpB)) {
                case TimeoutOutcome.TeamAWins:
                    timeoutHp = new Vector2(hpA, hpB);
                    Knockout(robotB, RoundEndReason.Timeout);
                    break;
                case TimeoutOutcome.TeamBWins:
                    timeoutHp = new Vector2(hpA, hpB);
                    Knockout(robotA, RoundEndReason.Timeout);
                    break;
                case TimeoutOutcome.SuddenDeath:
                    Phase = MatchPhase.SuddenDeath;
                    suddenDeathDamage.Clear();
                    Hud.ShowCenter("SUDDEN DEATH", Hud.DangerColor, 2f);
                    break;
                case TimeoutOutcome.Draw:
                    Phase = MatchPhase.RoundOver; // respawn events ignored while frozen
                    SetFrozen(robotA, true);
                    SetFrozen(robotB, true);
                    Hud.SetTimer(0f, false);
                    Hud.ShowCenter("DRAW", Hud.AccentColor);
                    Hud.Flash("ROUND WILL BE REPLAYED", RoundOverDelay);
                    SetHint("Draw on HP – round will be replayed (press Space when ready)");
                    afterRound = StartCoroutine(AfterDraw());
                    break;
            }
        }

        private IEnumerator AfterDraw() {
            yield return Wait(RoundOverDelay);
            afterRound = null;
            ReplayRound();
        }

        private void LateUpdate() {
            // Sudden death: first robot damaged by the opponent loses; same frame -> bigger damage loses.
            if (Phase != MatchPhase.SuddenDeath || pendingLoser != null || suddenDeathDamage.Count == 0) return;
            var loser = suddenDeathDamage.OrderByDescending(kv => kv.Value).First().Key;
            suddenDeathDamage.Clear();
            Knockout(loser, RoundEndReason.SuddenDeath);
        }

        private bool Fighting => Phase == MatchPhase.Fight || Phase == MatchPhase.SuddenDeath;

        // ------------------------------------------------------------------ robot events

        private void OnZoneHit(RobotAgent robot, CollisionZoneBehaviour.CollisionEventArgs e) {
            if (Phase != MatchPhase.SuddenDeath || pendingLoser != null) return;
            if (e.colliderHitName != "Robot" && e.colliderHitName != "RobotArm") return; // walls / floor don't count
            suddenDeathDamage.TryGetValue(robot, out float d);
            suddenDeathDamage[robot] = d + e.Damage;
        }

        /// <summary>RobotAgent raises this after a fall or flip (HP already reduced) and at every episode start.</summary>
        private void OnRobotRespawned(RobotAgent robot) {
            if (Phase == MatchPhase.Waiting || Phase == MatchPhase.Countdown) {
                PlaceAtMarker(robot, false);
                return;
            }
            if (!Fighting || pendingLoser != null) return; // round is ending: EndEpisode resets, ignore
            PlaceAtMarker(robot, true);
            if (Phase == MatchPhase.SuddenDeath) Knockout(robot, RoundEndReason.SuddenDeath); // fall / flip loses
        }

        private void OnRobotDied(RobotAgent loser) {
            if (!Fighting) return;
            var reason = loser == pendingLoser ? pendingReason : RoundEndReason.Knockout;
            bool winnerIsA = loser == robotB;
            var winner = winnerIsA ? robotA : robotB;

            if (winnerIsA) ScoreA++; else ScoreB++;
            var result = new RoundResult {
                Round = RoundIndex + 1,
                Arena = arena != null ? arena.name : "",
                WinnerIsA = winnerIsA,
                Winner = winnerIsA ? NameA : NameB,
                Reason = reason,
                HpA = robotA.weightedAverageHealth,
                HpB = robotB.weightedAverageHealth,
            };
            if (reason == RoundEndReason.Timeout) { // record HP at the buzzer, not the forced 0
                result.HpA = timeoutHp.x;
                result.HpB = timeoutHp.y;
            }
            result.Flawless = MatchRules.IsFlawless(winnerIsA ? result.HpA : result.HpB);
            Results.Add(result);

            Phase = MatchPhase.RoundOver; // set before RobotAgent calls EndEpisode -> respawn events are ignored
            pendingLoser = null;
            SetPaused(false);
            SetFrozen(robotA, true);
            SetFrozen(robotB, true);
            Hud.SetScore(NameA, ScoreA, ScoreB, NameB);
            Hud.SetTimer(TimeLeft, false);
            Hud.ShowCenter($"{result.Winner} WINS", Hud.AccentColor);
            if (result.Flawless) Hud.Flash("FLAWLESS VICTORY", RoundOverDelay);
            else if (reason == RoundEndReason.Referee) Hud.Flash("REFEREE DECISION", RoundOverDelay);
            else if (reason == RoundEndReason.SuddenDeath) Hud.Flash("SUDDEN DEATH", RoundOverDelay);
            else if (reason == RoundEndReason.Timeout) Hud.Flash("TIME! MORE HP WINS", RoundOverDelay);
            Play(result.Flawless ? FlawlessClip : RoundWonClip);
            afterRound = StartCoroutine(AfterRound());
        }

        private IEnumerator AfterRound() {
            yield return Wait(RoundOverDelay);
            afterRound = null;
            if (MatchRules.IsMatchOver(ScoreA, ScoreB)) {
                Phase = MatchPhase.MatchOver;
                Hud.ShowWinner(ScoreA > ScoreB ? NameA : NameB, NameA, ScoreA, ScoreB, NameB);
                Cameras.ShowWaiting();
                Debug.Log("[Turnaj] RESULT\n" + ResultSummary());
            } else {
                LoadRound(RoundIndex + 1);
            }
        }

        // ------------------------------------------------------------------ referee actions

        private void HandleKeys() {
            var kb = Keyboard.current;
            if (kb == null || Phase == MatchPhase.Invalid) return;
            float now = Time.unscaledTime;

            if (kb.spaceKey.wasPressedThisFrame && Phase == MatchPhase.Waiting) countdown = StartCoroutine(Countdown());

            if (kb.tKey.wasPressedThisFrame && (Fighting || Phase == MatchPhase.Countdown)) SetPaused(!Paused);

            if (kb.rKey.wasPressedThisFrame && (Fighting || Phase == MatchPhase.Countdown)) {
                if (tapR.Tap(now)) ReplayRound(); else SetHint("R – press again to REPLAY the round");
            }
            if (Paused || !Fighting) return;

            if (kb.wKey.wasPressedThisFrame) {
                if (tapW.Tap(now)) Separate(); else SetHint("W – press again to SEPARATE");
            }
            if (kb.qKey.wasPressedThisFrame && pendingLoser == null) {
                if (tapQ.Tap(now)) Knockout(robotA, RoundEndReason.Referee); else SetHint($"Q – press again to KNOCK OUT {NameA}");
            }
            if (kb.eKey.wasPressedThisFrame && pendingLoser == null) {
                if (tapE.Tap(now)) Knockout(robotB, RoundEndReason.Referee); else SetHint($"E – press again to KNOCK OUT {NameB}");
            }
        }

        private void SetHint(string text) {
            Hint = text;
            HintTime = Time.unscaledTime;
        }

        /// <summary>Sets every zone to 0; RobotAgent then dies by its own rule next frame.</summary>
        private void Knockout(RobotAgent robot, RoundEndReason reason) {
            if (robot == null || pendingLoser != null) return;
            pendingLoser = robot;
            pendingReason = reason;
            foreach (var zone in robot.GetComponentsInChildren<CollisionZoneBehaviour>(true)) zone.ChangeHealth(-10f);
            SetHint($"Knockout: {robot.name} ({reason})");
        }

        private void Separate() {
            if (Time.unscaledTime - lastSeparator < SeparatorCooldown) {
                SetHint("Separator cooling down");
                return;
            }
            lastSeparator = Time.unscaledTime;
            var dir = robotB.transform.position - robotA.transform.position;
            dir.y = 0f;
            dir = dir.sqrMagnitude < 1e-4f ? Vector3.forward : dir.normalized;
            Push(robotA, -dir);
            Push(robotB, dir);
            Hud.Flash("BREAK!", 1f);
            SetHint("Separator used");
        }

        private void Push(RobotAgent robot, Vector3 dir) {
            var dv = dir * SeparatorSpeed + Vector3.up * SeparatorLift;
            foreach (var rb in robot.GetComponentsInChildren<Rigidbody>()) {
                if (!rb.isKinematic) rb.AddForce(dv, ForceMode.VelocityChange);
            }
        }

        private void ReplayRound() {
            ResetRoundState();
            Phase = MatchPhase.Waiting; // respawn events from EndEpisode -> own markers
            robotA.EndEpisode();       // resets HP of all zones
            robotB.EndEpisode();
            EnterWaiting();
            SetHint($"Round {RoundIndex + 1} will be replayed");
        }

        private void SetPaused(bool paused) {
            Paused = paused;
            Time.timeScale = paused ? 0f : 1f;
            if (Hud != null) Hud.SetPaused(paused);
        }

        // ------------------------------------------------------------------ helpers

        private void PlaceAtMarker(RobotAgent robot, bool avoidOpponent) {
            if (robot == null || arena == null) return;
            bool isA = robot == robotA;
            bool home = isA == MatchRules.TeamAIsHome(RoundIndex);
            var marker = TeamValidation.Find(arena, home ? SpawnSide.Home : SpawnSide.Guest);
            if (marker == null) return;

            Vector3 pos = marker.transform.position;
            Quaternion rot = Quaternion.Euler(0f, marker.transform.eulerAngles.y, 0f);
            var other = isA ? robotB : robotA;
            if (avoidOpponent && other != null) {
                var flat = other.transform.position - pos;
                flat.y = 0f;
                if (flat.magnitude < RespawnClearance) {
                    pos = MatchRules.MirrorThroughCentre(pos, arena.transform.position);
                    rot *= Quaternion.Euler(0f, 180f, 0f);
                }
            }

            foreach (var rb in robot.GetComponentsInChildren<Rigidbody>()) {
                if (!rb.isKinematic) {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
            robot.transform.SetPositionAndRotation(pos + Vector3.up * 0.022f, rot);
            Physics.SyncTransforms();
        }

        private static void SetFrozen(RobotAgent robot, bool frozen) {
            if (robot == null) return;
            foreach (var rb in robot.GetComponentsInChildren<Rigidbody>()) {
                if (!rb.isKinematic) {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = frozen;
            }
        }

        private void Play(AudioClip clip) {
            if (clip != null && Voice != null) Voice.PlayOneShot(clip);
        }

        private void StopRoutine(ref Coroutine c) {
            if (c != null) StopCoroutine(c);
            c = null;
        }

        private void OnDisable() {
            Time.timeScale = 1f;
        }

        /// <summary>Text for the website: score line, points, and one line per round.</summary>
        public string ResultSummary() {
            var sb = new StringBuilder();
            sb.AppendLine($"{NameA} {ScoreA} : {ScoreB} {NameB}");
            if (MatchRules.IsMatchOver(ScoreA, ScoreB)) {
                sb.AppendLine($"Points: {NameA} {MatchRules.LeaguePoints(ScoreA, ScoreB)}, {NameB} {MatchRules.LeaguePoints(ScoreB, ScoreA)}");
            }
            foreach (var r in Results) {
                sb.AppendLine($"R{r.Round} {r.Arena}: {r.Winner} – {r.Reason}{(r.Flawless ? ", flawless" : "")} " +
                              $"(HP {NameA} {MatchRules.BarPercent(r.HpA):P0}, {NameB} {MatchRules.BarPercent(r.HpB):P0})");
            }
            return sb.ToString().TrimEnd();
        }
    }
}
