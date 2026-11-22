using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AIJam.Validation.EditorTools {
    [CustomEditor(typeof(ValidationRunner))]
    public class ValidationRunnerEditor : Editor {
        private Vector2 scroll;

        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI() {
            var r = (ValidationRunner)target;

            EditorGUILayout.LabelField("Team", EditorStyles.boldLabel);
            var labels = r.Teams.Select((t, i) => $"{(char)('A' + i)} – {(t != null ? t.DisplayName : "(empty)")}").ToArray();
            if (labels.Length == 0) {
                EditorGUILayout.HelpBox("Add team assets (soTeam) to the Teams list below.", MessageType.Warning);
            } else {
                using (new EditorGUI.DisabledScope(Application.isPlaying)) {
                    EditorGUI.BeginChangeCheck();
                    int i = EditorGUILayout.Popup("Team to validate", Mathf.Clamp(r.TeamIndex, 0, labels.Length - 1), labels);
                    if (EditorGUI.EndChangeCheck()) {
                        Undo.RecordObject(r, "Pick team");
                        r.TeamIndex = i;
                        EditorUtility.SetDirty(r);
                    }
                }
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Run validation", GUILayout.Height(30))) r.RunValidation();
            DrawFindings(r);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Test match", EditorStyles.boldLabel);
            if (!Application.isPlaying) {
                EditorGUILayout.HelpBox("Press Play to run the test match (scripted enemy, six events).", MessageType.Info);
            } else {
                if (!string.IsNullOrEmpty(r.Status)) EditorGUILayout.HelpBox(r.Status, MessageType.None);
                using (new EditorGUI.DisabledScope(r.Busy || r.RobotInstance == null)) {
                    if (GUILayout.Button("Run test match (all events)", GUILayout.Height(30))) r.RunTest(-1);
                    for (int row = 0; row < ValidationRunner.EventNames.Length; row += 3) {
                        EditorGUILayout.BeginHorizontal();
                        for (int k = row; k < Mathf.Min(row + 3, ValidationRunner.EventNames.Length); k++)
                            if (GUILayout.Button(ValidationRunner.EventNames[k])) r.RunTest(k);
                        EditorGUILayout.EndHorizontal();
                    }
                }
                foreach (var res in r.Results) {
                    var type = res.Status == TestStatus.Fail ? MessageType.Error : res.Status == TestStatus.Warn ? MessageType.Warning : MessageType.Info;
                    EditorGUILayout.HelpBox($"{res.Event} — {res.Status}\n{res.Text}", type);
                }
                if (r.Results.Count > 0 && GUILayout.Button("Copy test report")) EditorGUIUtility.systemCopyBuffer = r.ResultsReport();

                if (r.AgentView != null) {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("What the robot's camera sees (RobotView only)", EditorStyles.boldLabel);
                    var rect = GUILayoutUtility.GetRect(192, 192, GUILayout.ExpandWidth(false));
                    EditorGUI.DrawPreviewTexture(rect, r.AgentView);
                }
            }

            EditorGUILayout.Space();
            DrawDefaultInspector();
        }

        private void DrawFindings(ValidationRunner r) {
            if (r.Findings == null || r.Findings.Count == 0) {
                if (!string.IsNullOrEmpty(r.FindingsReport)) EditorGUILayout.HelpBox("PASS – no findings.", MessageType.Info);
                return;
            }
            int e = r.Findings.Count(f => f.Severity == Severity.Error), w = r.Findings.Count(f => f.Severity == Severity.Warning);
            EditorGUILayout.HelpBox(e == 0 ? $"PASS with {w} warning(s)." : $"FAIL: {e} error(s), {w} warning(s). Click a finding to select the object.",
                e == 0 ? MessageType.Info : MessageType.Error);
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(320));
            foreach (var f in r.Findings.OrderBy(x => x.Severity)) {
                var type = f.Severity == Severity.Error ? MessageType.Error : f.Severity == Severity.Warning ? MessageType.Warning : MessageType.Info;
                EditorGUILayout.HelpBox($"{f.Source} · {f.Rule}: {f.Message}" + (string.IsNullOrEmpty(f.Path) ? "" : $"\n{f.Path}"), type);
                var rect = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition)) {
                    Select(r, f);
                    Event.current.Use();
                }
            }
            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("Copy validation report")) EditorGUIUtility.systemCopyBuffer = r.FindingsReport;
        }

        /// <summary>Selects the offending object: the live instance in Play mode, otherwise the object inside the prefab asset.</summary>
        private static void Select(ValidationRunner r, Finding f) {
            var team = r.Team;
            if (team == null) return;
            GameObject live = f.Source == "Robot" ? r.RobotInstance : f.Source == "Arena" ? r.ArenaInstance : null;
            GameObject asset = f.Source == "Robot" ? team.RobotPrefab : f.Source == "Arena" ? team.ArenaPrefab : null;
            var root = live != null ? live : asset;
            if (root == null) { Selection.activeObject = team; return; }
            var t = string.IsNullOrEmpty(f.Path) ? root.transform : root.transform.Find(f.Path);
            var obj = t != null ? t.gameObject : root;
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }
    }
}
