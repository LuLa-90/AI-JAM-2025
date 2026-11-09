
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

        isValid &= ValidateComponent<BodyComponent>(
            validationRules.bodyRule
        );

        isValid &= ValidateComponent<ArmComponent>(
            validationRules.armRule
        );

        isValid &= ValidateComponent<WheelComponent>(
            validationRules.wheelRule
        );

        // Bumper zatiaľ kontrolujeme iba informatívne.
        int bumperCount = GetComponentsInChildren<BumperComponent>(true).Length;

        Debug.Log(
            $"[INFO] BumperComponent: {bumperCount} found. Maximum limit not defined.",
            gameObject
        );

        Debug.Log("========== ROBOT VALIDATION FINISHED ==========");

        if (isValid)
        {
            Debug.Log(
                "[Robot Validator] VALIDATION SUCCESSFUL!",
                gameObject
            );
        }
        else
        {
            Debug.LogError(
                "[Robot Validator] VALIDATION FAILED! Check the errors above.",
                gameObject
            );
        }
    }

    
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

    bool valid = true;

    if (count < rule.minCount)
    {
        Debug.LogError(
            $"[FAIL] {rule.componentName}: Found {count}, minimum required: {rule.minCount}.",
            gameObject
        );

        valid = false;
    }
    else if (rule.checkMaximum && count > rule.maxCount)
    {
        Debug.LogError(
            $"[FAIL] {rule.componentName}: Found {count}, maximum allowed: {rule.maxCount}.",
            gameObject
        );

        valid = false;
    }
    else
    {
        Debug.Log(
            $"[PASS] {rule.componentName}: Found {count}.",
            gameObject
        );
    }

    return valid;
}

}
