using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Turnaj.EditorTools {
    [CustomEditor(typeof(TournamentManager))]
    public class TournamentManagerEditor : Editor {
        public static bool GameViewFocused {
            get {
                var w = EditorWindow.focusedWindow;
                return w != null && w.GetType().Name == "GameView";
            }
        }

        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI() {
            var m = (TournamentManager)target;

            if (Application.isPlaying) DrawLive(m);
            DrawTeamPicker(m);
            EditorGUILayout.Space();
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Keys work only while the Game view is focused.\n" +
                "Director (numpad): 8 top · 5 front · 2 close · 1/3 rotating · 4/6 chase A/B\n" +
                "Referee: Space start round · W×2 separator · Q×2 KO Team A · E×2 KO Team B · R×2 replay round · T pause\n" +
                "OBS: F1–F4 (not used by Unity)", MessageType.None);
        }

        private void DrawTeamPicker(TournamentManager m) {
            EditorGUILayout.LabelField("Match", EditorStyles.boldLabel);
            var labels = m.Teams.Select((t, i) => $"{(char)('A' + i)} – {(t != null ? t.DisplayName : "(empty)")}").ToArray();
            if (labels.Length == 0) {
                EditorGUILayout.HelpBox("Add team assets (soTeam) to the Teams list below.", MessageType.Warning);
                return;
            }
            using (new EditorGUI.DisabledScope(Application.isPlaying)) {
                EditorGUI.BeginChangeCheck();
                int a = EditorGUILayout.Popup("Team A (left, home R1)", Mathf.Clamp(m.TeamAIndex, 0, labels.Length - 1), labels);
                int b = EditorGUILayout.Popup("Team B (right, home R2)", Mathf.Clamp(m.TeamBIndex, 0, labels.Length - 1), labels);
                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(m, "Pick tournament teams");
                    m.TeamAIndex = a;
                    m.TeamBIndex = b;
                    EditorUtility.SetDirty(m);
                }
            }
            if (Application.isPlaying) return;

            var problems = m.ValidationProblems();
            if (problems.Count == 0) {
                EditorGUILayout.HelpBox($"Ready: {m.NameA} vs {m.NameB}. Press Play.", MessageType.Info);
            } else {
                EditorGUILayout.HelpBox(string.Join("\n", problems), MessageType.Error);
            }
        }

        private void DrawLive(TournamentManager m) {
            if (!GameViewFocused) {
                var old = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
                EditorGUILayout.HelpBox("GAME VIEW NOT FOCUSED – hotkeys off. Click the Game view.", MessageType.Error);
                GUI.backgroundColor = old;
            }

            EditorGUILayout.LabelField("Live", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Phase", m.Phase + (m.Paused ? " (PAUSED)" : ""));
            EditorGUILayout.LabelField("Round", MatchRules.RoundTitle(m.RoundIndex));
            EditorGUILayout.LabelField("Score", $"{m.NameA} {m.ScoreA} : {m.ScoreB} {m.NameB}");
            EditorGUILayout.LabelField("Time left", m.Phase == MatchPhase.SuddenDeath ? "SUDDEN DEATH" : m.TimeLeft.ToString("F1"));
            if (m.RobotA != null && m.RobotB != null) {
                EditorGUILayout.LabelField("HP (bar)", $"{m.NameA} {MatchRules.BarPercent(m.RobotA.weightedAverageHealth):P0} · {m.NameB} {MatchRules.BarPercent(m.RobotB.weightedAverageHealth):P0}");
            }
            if (Time.unscaledTime - m.HintTime < 3f && !string.IsNullOrEmpty(m.Hint)) {
                EditorGUILayout.HelpBox(m.Hint, MessageType.Warning);
            }
            if (m.Phase == MatchPhase.Invalid) {
                EditorGUILayout.HelpBox("Match not started – see errors in the Console. Stop Play, fix the setup.", MessageType.Error);
            }

            if (m.Results.Count > 0) {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(m.Phase == MatchPhase.MatchOver ? "RESULT (copy to website)" : "Result so far", EditorStyles.boldLabel);
                string summary = m.ResultSummary();
                EditorGUILayout.SelectableLabel(summary, EditorStyles.textArea, GUILayout.Height(18 * (summary.Split('\n').Length + 1)));
                if (GUILayout.Button("Copy result to clipboard")) EditorGUIUtility.systemCopyBuffer = summary;
            }
            EditorGUILayout.Space();
        }
    }

    /// <summary>Small status box in the Scene view for the referee (never visible in the Game view / OBS).</summary>
    [InitializeOnLoad]
    public static class TournamentSceneOverlay {
        private static double lastRepaint;

        static TournamentSceneOverlay() {
            SceneView.duringSceneGui += OnSceneGui;
            EditorApplication.update += () => {
                if (!Application.isPlaying || EditorApplication.timeSinceStartup - lastRepaint < 0.2) return;
                lastRepaint = EditorApplication.timeSinceStartup;
                SceneView.RepaintAll();
            };
        }

        private static void OnSceneGui(SceneView view) {
            if (!Application.isPlaying) return;
            var m = Object.FindAnyObjectByType<TournamentManager>();
            if (m == null) return;

            Handles.BeginGUI();
            var style = new GUIStyle(EditorStyles.helpBox) { fontSize = 14, richText = true, alignment = TextAnchor.UpperLeft };
            string focus = TournamentManagerEditor.GameViewFocused
                ? "<color=#7CFC00>Game view focused – keys ON</color>"
                : "<color=#FF5050><b>GAME VIEW NOT FOCUSED – hotkeys off</b></color>";
            string time = m.Phase == MatchPhase.SuddenDeath ? "SUDDEN DEATH" : m.TimeLeft.ToString("F0") + " s";
            string hint = Time.unscaledTime - m.HintTime < 3f ? $"\n<color=#FFD700>{m.Hint}</color>" : "";
            string text = $"{focus}\n<b>{m.NameA} {m.ScoreA} : {m.ScoreB} {m.NameB}</b>   {MatchRules.RoundTitle(m.RoundIndex)}\n" +
                          $"{m.Phase}{(m.Paused ? " – PAUSED" : "")}   {time}{hint}\n" +
                          "Space start · W×2 separate · Q×2 / E×2 KO · R×2 replay · T pause";
            GUI.Label(new Rect(10, 10, 470, 100), text, style);
            Handles.EndGUI();
        }
    }
}
