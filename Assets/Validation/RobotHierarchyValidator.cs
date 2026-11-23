
using System.Collections.Generic;
using UnityEngine;

public class RobotHierarchyValidator : MonoBehaviour
{
    [Header("Validation Rules")]
    [SerializeField] private RobotValidationRules validationRules;

    [ContextMenu("Validate Robot Hierarchy")]
    public void ValidateRobotHierarchy()
    {
        if (validationRules == null)
        {
            Debug.LogError(
                "[Robot Validator] Validation Rules asset is not assigned!",
                gameObject
            );
            return;
        }

        Debug.Log("========== ROBOT VALIDATION STARTED ==========");

        bool isValid = true;

        // 1. Main component count
        bool mainComponentsValid = true;
        mainComponentsValid &= ValidateComponent<BodyComponent>(validationRules.bodyRule);
        mainComponentsValid &= ValidateComponent<ArmComponent>(validationRules.armRule);
        mainComponentsValid &= ValidateComponent<WheelComponent>(validationRules.wheelRule);
        mainComponentsValid &= ValidateComponent<BumperComponent>(validationRules.bumperRule);
        LogSectionResult("Main Components", mainComponentsValid);
        isValid &= mainComponentsValid;

        // 2. Additional component limits
        bool additionalLimitsValid = ValidateAdditionalComponentLimits();
        LogSectionResult("Additional Component Limits", additionalLimitsValid);
        isValid &= additionalLimitsValid;

        // 3. Main component placement
        bool placementValid = ValidatePlacementRules();
        LogSectionResult("Component Placement", placementValid);
        isValid &= placementValid;

        // 4. Child objects and required components
        bool childRulesValid = true;
        childRulesValid &= ValidateChildRules(validationRules.bodyChildRules, "BODY COMPONENT CHECK");
        childRulesValid &= ValidateChildRules(validationRules.wheelChildRules, "WHEEL COMPONENT CHECK");
        childRulesValid &= ValidateChildRules(validationRules.armChildRules, "ARM COMPONENT CHECK");
        childRulesValid &= ValidateChildRules(validationRules.bumperChildRules, "BUMPER COMPONENT CHECK");
        LogSectionResult("Child Components", childRulesValid);
        isValid &= childRulesValid;

        // 5. Physical overlap validation
        PhysicalOverlapValidator physicalValidator = GetComponent<PhysicalOverlapValidator>();
        if (physicalValidator != null)
        {
            bool physicalValid = physicalValidator.ValidatePhysicalOverlaps();
            isValid &= physicalValid;
        }
        else
        {
            Debug.LogError("[Physical Overlap] PhysicalOverlapValidator is missing!", gameObject);
            isValid = false;
        }

        // 6. Overall result
        Debug.Log("========== ROBOT VALIDATION FINISHED ==========");

        if (isValid)
        {
            Debug.Log("[ROBOT VALIDATION] SUCCESSFUL! Robot is ready for the tournament.", gameObject);
        }
        else
        {
            Debug.LogError("[ROBOT VALIDATION] FAILED! Please fix the issues listed above.", gameObject);
        }
    }

    private void LogSectionResult(string sectionName, bool isValid)
    {
        if (isValid)
            Debug.Log($"[{sectionName}] OK", gameObject);
        else
            Debug.LogError($"[{sectionName}] FAILED. See the issues listed above.", gameObject);
    }

    // ==================================================
    // KONTROLA POCTU HLAVNYCH KOMPONENTOV
    // ==================================================

    private bool ValidateComponent<T>(
        RobotValidationRules.ComponentRule rule
    ) where T : Behaviour
    {
        if (rule == null)
        {
            Debug.LogError(
                $"[ERROR] Missing validation rule for {typeof(T).Name}.",
                gameObject
            );

            return false;
        }

        T[] components = GetComponentsInChildren<T>(true);

        int count = 0;

        foreach (T component in components)
        {
            if (component != null &&
                component.enabled &&
                component.gameObject.activeInHierarchy)
            {
                count++;
            }
        }

        if (count < rule.minCount)
        {
            Debug.LogError(
                $"[FAIL] {rule.componentName}: Found {count}, minimum required: {rule.minCount}.",
                gameObject
            );

            return false;
        }

        if (rule.checkMaximum && count > rule.maxCount)
        {
            Debug.LogError(
                $"[FAIL] {rule.componentName}: Found {count}, maximum allowed: {rule.maxCount}.",
                gameObject
            );

            return false;
        }

        return true;
    }


    // ==================================================
    // KONTROLA DODATOCNYCH LIMITOV KOMPONENTOV
    // ==================================================

    private bool ValidateAdditionalComponentLimits()
    {
        if (validationRules.additionalComponentLimits == null)
            return true;

        bool allValid = true;

        foreach (var rule in validationRules.additionalComponentLimits)
        {
            if (rule == null)
                continue;

            int count = CountAdditionalComponent(rule.componentType);

            bool ruleValid = true;

            if (rule.checkMinimum && count < rule.minCount)
            {
                Debug.LogError(
                    $"[FAIL] {rule.componentType}: Found {count}, minimum required: {rule.minCount}.",
                    gameObject
                );

                ruleValid = false;
            }

            if (rule.checkMaximum && count > rule.maxCount)
            {
                Debug.LogError(
                    $"[FAIL] {rule.componentType}: Found {count}, maximum allowed: {rule.maxCount}.",
                    gameObject
                );

                ruleValid = false;
            }

            if (!ruleValid)
                allValid = false;
        }

        return allValid;
    }


    // Spocita vybrany typ komponentu v hierarchii robota.

    private int CountAdditionalComponent(
        RobotValidationRules.CountableComponentType componentType
    )
    {
        switch (componentType)
        {
            case RobotValidationRules.CountableComponentType.WheelCollider:
                return CountComponents<WheelCollider>();

            case RobotValidationRules.CountableComponentType.SensorComponent:
                return CountComponents<SensorComponent>();

            case RobotValidationRules.CountableComponentType.CollisionZoneBehaviour:
                return CountComponents<CollisionZoneBehaviour>();

            case RobotValidationRules.CountableComponentType.ProximitySensorRay:
                return CountComponents<ProximitySensorRay>();

            case RobotValidationRules.CountableComponentType.ButtSensorComponent:
                return CountComponents<ButtSensorComponent>();

            default:
                Debug.LogWarning(
                    $"[WARNING] Unsupported component type: {componentType}",
                    gameObject
                );

                return 0;
        }
    }


    // Univerzalne pocitanie komponentov.

    private int CountComponents<T>() where T : Component
    {
        T[] components = GetComponentsInChildren<T>(true);

        int count = 0;

        foreach (T component in components)
        {
            if (component == null ||
                !component.gameObject.activeInHierarchy)
                continue;

            // Kontrola enabled pri Behaviour komponentoch
            if (component is Behaviour behaviour &&
                !behaviour.enabled)
                continue;

            // Kontrola enabled pri Collider komponentoch
            if (component is Collider collider &&
                !collider.enabled)
                continue;

            count++;
        }

        return count;
    }


    // ==================================================
    // KONTROLA UMIESTNENIA HLAVNYCH KOMPONENTOV
    // ==================================================

    private bool ValidatePlacementRules()
    {
        if (validationRules.placementRules == null)
            return true;

        bool allValid = true;

        MonoBehaviour[] components =
            GetComponentsInChildren<MonoBehaviour>(true);

        foreach (var rule in validationRules.placementRules)
        {
            if (rule == null ||
                string.IsNullOrWhiteSpace(rule.componentName))
            {
                continue;
            }

            List<MonoBehaviour> matchingComponents =
                new List<MonoBehaviour>();

            foreach (var component in components)
            {
                if (component == null)
                    continue;

                if (component.GetType().Name != rule.componentName)
                    continue;

                if (!component.enabled ||
                    !component.gameObject.activeInHierarchy)
                    continue;

                matchingComponents.Add(component);
            }

            int correctCount = 0;

            Transform expectedParent = null;

            if (rule.parentMustBeRobotRoot)
            {
                foreach (var component in matchingComponents)
                {
                    if (component.transform.parent == transform)
                        correctCount++;
                }
            }
            else
            {
                expectedParent = FindChildByName(
                    transform,
                    rule.expectedParentName
                );

                if (expectedParent == null)
                {
                    Debug.LogError(
                        $"[FAIL] {rule.componentName}: Expected parent '{rule.expectedParentName}' was not found.",
                        gameObject
                    );

                    allValid = false;
                    continue;
                }

                foreach (var component in matchingComponents)
                {
                    if (component.transform != expectedParent &&
                        component.transform.IsChildOf(expectedParent))
                    {
                        correctCount++;
                    }
                }
            }

            if (correctCount != rule.expectedCount)
            {
                string expectedLocation = rule.parentMustBeRobotRoot
                    ? gameObject.name
                    : rule.expectedParentName;

                Debug.LogError(
                    $"[FAIL] {rule.componentName}: Expected {rule.expectedCount} under '{expectedLocation}', but found {correctCount}.",
                    gameObject
                );

                foreach (var component in matchingComponents)
                {
                    Debug.LogWarning(
                        $"[LOCATION] {rule.componentName} is currently at: {GetHierarchyPath(component.transform)}",
                        component.gameObject
                    );
                }

                allValid = false;
            }
        }

        return allValid;
    }


    // ==================================================
    // KONTROLA VNORENYCH OBJEKTOV
    // ==================================================

    private bool ValidateChildRules(
        List<RobotValidationRules.ChildObjectRule> rules,
        string sectionName
    )
    {
        if (rules == null)
            return true;

        bool allValid = true;

        foreach (var rule in rules)
        {
            if (rule == null)
                continue;

            Transform expectedParent = FindChildByName(
                transform,
                rule.expectedParentName
            );

            if (expectedParent == null)
            {
                Debug.LogError(
                    $"[FAIL] Parent '{rule.expectedParentName}' was not found.",
                    gameObject
                );

                allValid = false;
                continue;
            }

            List<Transform> directChildren =
                FindDirectChildrenByName(
                    expectedParent,
                    rule.childObjectName
                );

            if (directChildren.Count == 0)
            {
                Debug.LogError(
                    $"[FAIL] Missing object '{rule.childObjectName}' directly under '{rule.expectedParentName}'.",
                    expectedParent.gameObject
                );

                List<Transform> misplacedObjects =
                    FindAllObjectsByName(rule.childObjectName);

                if (misplacedObjects.Count > 0)
                {
                    foreach (Transform misplaced in misplacedObjects)
                    {
                        Debug.LogWarning(
                            $"[LOCATION] '{rule.childObjectName}' was found elsewhere at: {GetHierarchyPath(misplaced)}",
                            misplaced.gameObject
                        );
                    }
                }

                allValid = false;
                continue;
            }

            foreach (Transform child in directChildren)
            {
                bool componentValid = true;

                if (!child.gameObject.activeInHierarchy)
                {
                    Debug.LogError(
                        $"[FAIL] Object '{rule.childObjectName}' is inactive.",
                        child.gameObject
                    );

                    allValid = false;
                    continue;
                }

                if (rule.requiredComponent ==
                    RobotValidationRules.RequiredComponentType.CollisionZoneBehaviour)
                {
                    CollisionZoneBehaviour collisionZone =
                        child.GetComponent<CollisionZoneBehaviour>();

                    if (collisionZone == null || !collisionZone.enabled)
                    {
                        Debug.LogError(
                            $"[FAIL] '{rule.childObjectName}' is missing CollisionZoneBehaviour.",
                            child.gameObject
                        );

                        componentValid = false;
                    }
                }

                if (rule.requiredComponent ==
                    RobotValidationRules.RequiredComponentType.SensorComponent)
                {
                    SensorComponent sensor =
                        child.GetComponent<SensorComponent>();

                    if (sensor == null || !sensor.enabled)
                    {
                        Debug.LogError(
                            $"[FAIL] '{rule.childObjectName}' is missing SensorComponent.",
                            child.gameObject
                        );

                        componentValid = false;
                    }
                    else if (rule.checkSensorType &&
                             sensor.GetSensorType != rule.expectedSensorType)
                    {
                        Debug.LogError(
                            $"[FAIL] '{rule.childObjectName}' has incorrect SensorType. Expected: {rule.expectedSensorType}, Actual: {sensor.GetSensorType}.",
                            child.gameObject
                        );

                        componentValid = false;
                    }
                }

                if (rule.requiredComponent ==
                    RobotValidationRules.RequiredComponentType.WheelComponent)
                {
                    WheelComponent wheel =
                        child.GetComponent<WheelComponent>();

                    if (wheel == null || !wheel.enabled)
                    {
                        Debug.LogError(
                            $"[FAIL] '{rule.childObjectName}' is missing WheelComponent.",
                            child.gameObject
                        );

                        componentValid = false;
                    }
                }

                if (rule.requiredComponent ==
                    RobotValidationRules.RequiredComponentType.WheelCollider)
                {
                    WheelCollider wheelCollider =
                        child.GetComponent<WheelCollider>();

                    if (wheelCollider == null || !wheelCollider.enabled)
                    {
                        Debug.LogError(
                            $"[FAIL] '{rule.childObjectName}' is missing WheelCollider.",
                            child.gameObject
                        );

                        componentValid = false;
                    }
                }

                if (!componentValid)
                    allValid = false;
            }
        }

        return allValid;
    }


    // ==================================================
    // VYHLADAVANIE OBJEKTOV
    // ==================================================

    private Transform FindChildByName(
        Transform parent,
        string childName
    )
    {
        if (string.IsNullOrWhiteSpace(childName))
            return null;

        foreach (Transform child in parent)
        {
            if (child.name.Trim() == childName.Trim())
                return child;

            Transform result = FindChildByName(child, childName);

            if (result != null)
                return result;
        }

        return null;
    }


    private List<Transform> FindDirectChildrenByName(
        Transform parent,
        string childName
    )
    {
        List<Transform> results = new List<Transform>();

        if (string.IsNullOrWhiteSpace(childName))
            return results;

        foreach (Transform child in parent)
        {
            if (child.name.Trim() == childName.Trim())
                results.Add(child);
        }

        return results;
    }


    private List<Transform> FindAllObjectsByName(string objectName)
    {
        List<Transform> results = new List<Transform>();

        if (string.IsNullOrWhiteSpace(objectName))
            return results;

        Transform[] allObjects =
            GetComponentsInChildren<Transform>(true);

        foreach (Transform item in allObjects)
        {
            if (item != transform &&
                item.name.Trim() == objectName.Trim())
            {
                results.Add(item);
            }
        }

        return results;
    }


    private string GetHierarchyPath(Transform target)
    {
        string path = target.name;

        Transform current = target.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
