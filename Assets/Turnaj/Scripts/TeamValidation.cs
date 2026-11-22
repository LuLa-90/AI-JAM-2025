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

        public static SpawnPoint Find(GameObject arenaInstance, SpawnSide side) {
            return arenaInstance.GetComponentsInChildren<SpawnPoint>(true).FirstOrDefault(p => p.Side == side);
        }
    }
}
