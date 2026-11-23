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

        // Colours last applied to the materials. A material is touched only when its colour changes here,
        // so the materials the old robots were trained with stay exactly as they are until someone recolours them.
        [SerializeField, HideInInspector] private bool appliedInit;
        [SerializeField, HideInInspector] private Color appliedDefault, appliedRobotPart, appliedTrap, appliedBuff, appliedBattery;

        private void OnValidate() {
            if (!appliedInit) {
                // first run on an existing asset: remember the current colours, change nothing
                appliedDefault = DefaultColor; appliedRobotPart = RobotPartColor; appliedTrap = TrapColor; appliedBuff = BuffColor; appliedBattery = BatteryColor;
                appliedInit = true;
                MarkDirty(this);
                return;
            }
            Recolour(DefaultMaterial, DefaultColor, ref appliedDefault);
            Recolour(RobotPartMaterial, RobotPartColor, ref appliedRobotPart);
            Recolour(TrapMaterial, TrapColor, ref appliedTrap);
            Recolour(BuffMaterial, BuffColor, ref appliedBuff);
            Recolour(BatteryMaterial, BatteryColor, ref appliedBattery);
        }

        private void Recolour(Material m, Color c, ref Color applied) {
            if (c == applied) return;
            applied = c;
            MarkDirty(this);
            Apply(m, c);
        }

        /// <summary>
        /// Sets the base colour and gives the emission (the HDR glow that decides most of what the camera sees)
        /// the same colour at its current strength (brightest channel).
        /// </summary>
        public static void Apply(Material m, Color c) {
            if (m == null) return;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_EmissionColor")) {
                // raw values: Get/SetColor convert gamma<->linear, which doesn't round-trip HDR values
                var e = m.GetVector("_EmissionColor");
                float strength = Mathf.Max(e.x, Mathf.Max(e.y, e.z));
                if (strength > 0f) m.SetVector("_EmissionColor", new Vector4(c.r * strength, c.g * strength, c.b * strength, e.w));
            }
            MarkDirty(m);
        }

        private static void MarkDirty(Object o) {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(o);
#endif
        }
    }
}
