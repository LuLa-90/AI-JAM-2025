using UnityEngine;

namespace Turnaj {
    public enum SpawnSide { Home, Guest }

    /// <summary>Arena rule: every arena has one Home and one Guest marker. The robot spawns here, facing forward (blue arrow).</summary>
    public class SpawnPoint : MonoBehaviour {
        public SpawnSide Side = SpawnSide.Home;

        private void OnDrawGizmos() {
            Gizmos.color = Side == SpawnSide.Home ? new Color(0.2f, 0.6f, 1f) : new Color(1f, 0.4f, 0.2f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.05f, 0.25f);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.05f, transform.position + Vector3.up * 0.05f + transform.forward * 0.5f);
        }
    }
}
