using UnityEngine;

namespace AIJam.Validation {
    public enum VisionRole { Default, RobotPart, Trap, Buff, Battery }

    /// <summary>
    /// The colours the agent camera (RobotView layer) may see. Each role has one shared material, and changing
    /// a colour here recolours every prefab that uses it. Decided 2026-11-22: default black, robot parts white,
    /// trap red, buff green, battery blue. May change at the hackathon start.
    /// </summary>
    [CreateAssetMenu(fileName = "VisionPalette", menuName = "AI Jam/Vision Palette")]
    public class VisionPalette : ScriptableObject {
        public Material DefaultMaterial;
        public Material RobotPartMaterial;
        public Material TrapMaterial;
        public Material BuffMaterial;
        public Material BatteryMaterial;

        [Header("Colours (applied to the materials above)")]
        public Color DefaultColor = Color.black;
        public Color RobotPartColor = Color.white;
        public Color TrapColor = new Color(1f, 0f, 0.06f);
        public Color BuffColor = new Color(0.05f, 1f, 0f);
        public Color BatteryColor = new Color(0f, 0.08f, 1f);

        public Material MaterialFor(VisionRole role) {
            switch (role) {
                case VisionRole.RobotPart: return RobotPartMaterial;
                case VisionRole.Trap: return TrapMaterial;
                case VisionRole.Buff: return BuffMaterial;
                case VisionRole.Battery: return BatteryMaterial;
                default: return DefaultMaterial;
            }
        }

        /// <summary>Role of a material, or null if it isn't a palette material.</summary>
        public VisionRole? RoleOf(Material m) {
            if (m == null) return null;
            if (m == DefaultMaterial) return VisionRole.Default;
            if (m == RobotPartMaterial) return VisionRole.RobotPart;
            if (m == TrapMaterial) return VisionRole.Trap;
            if (m == BuffMaterial) return VisionRole.Buff;
            if (m == BatteryMaterial) return VisionRole.Battery;
            return null;
        }

        private void OnValidate() {
            Apply(DefaultMaterial, DefaultColor);
            Apply(RobotPartMaterial, RobotPartColor);
            Apply(TrapMaterial, TrapColor);
            Apply(BuffMaterial, BuffColor);
            Apply(BatteryMaterial, BatteryColor);
        }

        /// <summary>Sets only the base colour; emission (HDR glow the agents were trained with) is left untouched.</summary>
        private static void Apply(Material m, Color c) {
            if (m == null) return;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }
    }
}
