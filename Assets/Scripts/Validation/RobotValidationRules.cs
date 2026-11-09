
using UnityEngine;

[CreateAssetMenu(
    fileName = "RobotValidationRules",
    menuName = "AI-JAM/Robot Validation Rules"
)]
public class RobotValidationRules : ScriptableObject
{
    [System.Serializable]
    public class ComponentRule
    {
        public string componentName;
        public int minCount;
        public int maxCount;

        // Ak je false, maximálny počet sa nekontroluje.
        public bool checkMaximum = true;
    }

    [Header("Main Robot Components")]
    public ComponentRule bodyRule = new ComponentRule
    {
        componentName = "BodyComponent",
        minCount = 1,
        maxCount = 1
    };

    public ComponentRule armRule = new ComponentRule
    {
        componentName = "ArmComponent",
        minCount = 1,
        maxCount = 1
    };

    public ComponentRule wheelRule = new ComponentRule
    {
        componentName = "WheelComponent",
        minCount = 2,
        maxCount = 2
    };

    public ComponentRule bumperRule = new ComponentRule
    {
        componentName = "BumperComponent",
        minCount = 0,
        maxCount = 0,
        checkMaximum = false
    };
}
