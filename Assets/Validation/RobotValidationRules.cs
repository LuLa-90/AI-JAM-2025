
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "RobotValidationRules",
    menuName = "AI-JAM/Robot Validation Rules"
)]
public class RobotValidationRules : ScriptableObject
{
    public enum RequiredComponentType
    {
        None,
        CollisionZoneBehaviour,
        SensorComponent,
        WheelComponent,
        WheelCollider
    }

    // Typy komponentov, pre ktoré chceme nastavovať
    // dodatočné limity počtu.
    public enum CountableComponentType
    {
        WheelCollider,
        SensorComponent,
        CollisionZoneBehaviour,
        ProximitySensorRay,
        ButtSensorComponent
    }

    [System.Serializable]
    public class ComponentRule
    {
        public string componentName;
        public int minCount;
        public int maxCount;
        public bool checkMaximum = true;
    }

    [System.Serializable]
    public class ComponentLimitRule
    {
        public CountableComponentType componentType;

        public int minCount = 0;
        public int maxCount = 0;

        public bool checkMinimum = false;
        public bool checkMaximum = false;
    }

    [System.Serializable]
    public class PlacementRule
    {
        public string componentName;
        public bool parentMustBeRobotRoot;
        public string expectedParentName;
        public int expectedCount = 1;
    }

    [System.Serializable]
    public class ChildObjectRule
    {
        [Tooltip("Nazov rodicovskeho objektu.")]
        public string expectedParentName;

        [Tooltip("Nazov povinneho objektu.")]
        public string childObjectName;

        [Tooltip("Komponent, ktory musi objekt obsahovat.")]
        public RequiredComponentType requiredComponent;

        [Tooltip("Kontrolovat aj typ senzora.")]
        public bool checkSensorType;

        [Tooltip("Ocakavany typ senzora.")]
        public SensorComponent.SensorType expectedSensorType;
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

    [Header("Additional Component Limits")]
    public List<ComponentLimitRule> additionalComponentLimits =
        new List<ComponentLimitRule>();

    [Header("Hierarchy Placement Rules")]
    public List<PlacementRule> placementRules =
        new List<PlacementRule>();

    [Header("Body Child Object Rules")]
    public List<ChildObjectRule> bodyChildRules =
        new List<ChildObjectRule>();

    [Header("Wheel Child Object Rules")]
    public List<ChildObjectRule> wheelChildRules =
        new List<ChildObjectRule>();

    [Header("Arm Child Object Rules")]
    public List<ChildObjectRule> armChildRules =
        new List<ChildObjectRule>();

    [Header("Bumper Child Object Rules")]
    public List<ChildObjectRule> bumperChildRules =
        new List<ChildObjectRule>();
}
