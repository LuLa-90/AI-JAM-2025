using UnityEngine;

namespace Turnaj {
    /// <summary>One tournament team: its robot (with the trained model assigned) and its home arena.</summary>
    [CreateAssetMenu(fileName = "soTeam", menuName = "AI Jam/Turnaj Team")]
    public class soTeam : ScriptableObject {
        public string TeamName;
        [Tooltip("Robot prefab with RobotAgent and the trained ONNX model assigned in Behavior Parameters.")]
        public GameObject RobotPrefab;
        [Tooltip("Home arena. Origin = arena centre, floor at y = 0, with a Home and a Guest SpawnPoint.")]
        public GameObject ArenaPrefab;

        public string DisplayName => string.IsNullOrWhiteSpace(TeamName) ? name : TeamName;
    }
}
