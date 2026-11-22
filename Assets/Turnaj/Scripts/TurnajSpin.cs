using UnityEngine;

namespace Turnaj {
    /// <summary>Spins its object around Y (pivot for rotating and orbit cameras).</summary>
    public class TurnajSpin : MonoBehaviour {
        [Tooltip("Degrees per second; negative spins the other way.")]
        public float DegreesPerSecond = 10f;

        private void Update() {
            transform.Rotate(0f, DegreesPerSecond * Time.unscaledDeltaTime, 0f);
        }
    }
}
