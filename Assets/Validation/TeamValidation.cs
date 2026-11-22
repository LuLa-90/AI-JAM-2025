using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents.Policies;
using UnityEngine;

namespace Turnaj {
    /// <summary>Checks that a team or arena has the fields and markers the tournament scene needs (not the game rules).</summary>
    public static class TeamValidation {
        public const float MaxMarkerHeight = 0.3f;

        public static List<string> CheckTeam(soTeam team) {
            var problems = new List<string>();
            if (team == null) {
                problems.Add("Team slot is empty.");
                return problems;
            }
            if (string.IsNullOrWhiteSpace(team.TeamName)) problems.Add($"{team.name}: TeamName is empty.");

            if (team.RobotPrefab == null) {
                problems.Add($"{team.DisplayName}: RobotPrefab is missing.");
            } else if (team.RobotPrefab.GetComponent<RobotAgent>() == null) {
                problems.Add($"{team.DisplayName}: RobotPrefab has no RobotAgent component.");
            } else {
                var bp = team.RobotPrefab.GetComponent<BehaviorParameters>();
                if (bp == null || bp.Model == null) problems.Add($"{team.DisplayName}: robot has no trained model assigned.");
            }

            if (team.ArenaPrefab == null) problems.Add($"{team.DisplayName}: ArenaPrefab is missing.");
            else problems.AddRange(CheckArena(team.ArenaPrefab, team.DisplayName));
            return problems;
        }

        public static List<string> CheckArena(GameObject arena, string owner) {
            var problems = new List<string>();
            if (arena == null) {
                problems.Add($"{owner}: arena is missing.");
                return problems;
            }
            var points = arena.GetComponentsInChildren<SpawnPoint>(true);
            foreach (SpawnSide side in new[] { SpawnSide.Home, SpawnSide.Guest }) {
                var ofSide = points.Where(p => p.Side == side).ToArray();
                if (ofSide.Length == 0) problems.Add($"{owner}: arena '{arena.name}' has no {side} SpawnPoint.");
                else if (ofSide.Length > 1) problems.Add($"{owner}: arena '{arena.name}' has {ofSide.Length} {side} SpawnPoints (needs 1).");
                foreach (var p in ofSide) {
                    float y = arena.transform.InverseTransformPoint(p.transform.position).y;
                    if (Mathf.Abs(y) > MaxMarkerHeight) problems.Add($"{owner}: {side} SpawnPoint is {y:F2} m from floor level (arena origin must be at the floor).");
                }
            }
            return problems;
        }

        /// <summary>
        /// Full rule check of a team (robot + arena prefabs) with the validation engine. Editor only:
        /// prefabs are opened in an isolated preview scene, nothing is changed.
        /// </summary>
        public static List<AIJam.Validation.Finding> FullCheck(soTeam team, AIJam.Validation.ValidationRules rules) {
            var all = new List<AIJam.Validation.Finding>();
            foreach (var p in CheckTeam(team)) all.Add(new AIJam.Validation.Finding(AIJam.Validation.Severity.Error, "Team", p) { Source = "Team" });
            if (team == null) return all;
            if (team.RobotPrefab != null) all.AddRange(Tag(ValidatePrefab(team.RobotPrefab, go => AIJam.Validation.RobotValidator.Validate(go, rules)), "Robot"));
            if (team.ArenaPrefab != null) all.AddRange(Tag(ValidatePrefab(team.ArenaPrefab, go => AIJam.Validation.ArenaValidator.Validate(go, rules)), "Arena"));
            if (team.RobotPrefab != null && team.RobotPrefab == team.ArenaPrefab)
                all.Add(new AIJam.Validation.Finding(AIJam.Validation.Severity.Error, "Team", "Robot and arena must be separate prefabs.") { Source = "Team" });
            return all;
        }

        public static List<AIJam.Validation.Finding> ValidatePrefab(GameObject prefab, System.Func<GameObject, List<AIJam.Validation.Finding>> check) {
#if UNITY_EDITOR
            string path = UnityEditor.AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(path)) return check(prefab); // scene object / runtime instance
            var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
            try { return check(root); }
            finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
#else
            return new List<AIJam.Validation.Finding> { new AIJam.Validation.Finding(AIJam.Validation.Severity.Error, "Validation", "Validation runs only in the Unity Editor.") };
#endif
        }

        private static IEnumerable<AIJam.Validation.Finding> Tag(List<AIJam.Validation.Finding> f, string source) {
            foreach (var x in f) x.Source = source;
            return f;
        }

        public static SpawnPoint Find(GameObject arenaInstance, SpawnSide side) {
            return arenaInstance.GetComponentsInChildren<SpawnPoint>(true).FirstOrDefault(p => p.Side == side);
        }
    }
}
