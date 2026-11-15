
using System.Collections.Generic;
using UnityEngine;

public class PhysicalOverlapValidator : MonoBehaviour
{
    private enum ComponentType
    {
        Body,
        Wheel,
        Battery,
        Bumper,
        Arm,
        Weapon,
        Unknown
    }

    [Header("Minimum Attachment")]
    [SerializeField] private float minAttachmentPenetration = 0.0001f;

    [Header("Maximum Penetration")]
    [SerializeField] private float maxWheelBodyPenetration = 0.02f;
    [SerializeField] private float maxWheelBumperPenetration = 0.005f;
    [SerializeField] private float maxBatteryPenetration = 0.02f;

    [ContextMenu("Validate Physical Overlaps")]
    public bool ValidatePhysicalOverlaps()
    {
        bool isValid = true;

        int constructionLayer = LayerMask.NameToLayer("ConstructionView");

        if (constructionLayer == -1)
        {
            Debug.LogError(
                "[Physical Overlap] ConstructionView layer was not found!",
                gameObject
            );

            return false;
        }

        MeshCollider[] colliders =
            GetComponentsInChildren<MeshCollider>(true);

        List<MeshCollider> validColliders = new List<MeshCollider>();

        foreach (MeshCollider collider in colliders)
        {
            if (IsValidCollider(collider, constructionLayer))
                validColliders.Add(collider);
        }

        HashSet<string> attachedComponents = new HashSet<string>();

        for (int i = 0; i < validColliders.Count; i++)
        {
            MeshCollider a = validColliders[i];

            for (int j = i + 1; j < validColliders.Count; j++)
            {
                MeshCollider b = validColliders[j];

                ComponentType typeA = GetComponentType(a);
                ComponentType typeB = GetComponentType(b);

                if (typeA == ComponentType.Unknown ||
                    typeB == ComponentType.Unknown)
                    continue;

                if (typeA == typeB)
                    continue;

                bool overlapping = Physics.ComputePenetration(
                    a, a.transform.position, a.transform.rotation,
                    b, b.transform.position, b.transform.rotation,
                    out Vector3 direction,
                    out float distance
                );

                if (!overlapping)
                    continue;

                CheckAttachment(
                    a, b, typeA, typeB,
                    distance, attachedComponents
                );

                float maxAllowed = GetMaximumPenetration(
                    typeA, typeB
                );

                if (maxAllowed >= 0f && distance > maxAllowed)
                {
                    isValid = false;

                    Debug.LogError(
                        $"[Physical Overlap] EXCESSIVE OVERLAP: " +
                        $"{a.name} <-> {b.name}. " +
                        $"Penetration: {distance:F4}, " +
                        $"Maximum allowed: {maxAllowed:F4}.",
                        a.gameObject
                    );
                }
            }
        }

        if (!CheckRequiredAttachments(attachedComponents))
            isValid = false;

        if (isValid)
        {
            Debug.Log("[Physical Overlap] OK", gameObject);
        }
        else
        {
            Debug.LogError(
                "[Physical Overlap] FAILED. Check the physical issues above.",
                gameObject
            );
        }

        return isValid;
    }

    private bool IsValidCollider(
        MeshCollider collider,
        int constructionLayer)
    {
        return collider != null &&
               collider.enabled &&
               collider.gameObject.activeInHierarchy &&
               !collider.isTrigger &&
               collider.convex &&
               collider.gameObject.layer == constructionLayer;
    }

    private ComponentType GetComponentType(MeshCollider collider)
    {
        string path = GetHierarchyPath(collider.transform).ToLower();

        if (path.Contains("wheels left") ||
            path.Contains("wheels right"))
            return ComponentType.Wheel;

        if (path.Contains("battery_shape"))
            return ComponentType.Battery;

        if (path.Contains("bumper_shape"))
            return ComponentType.Bumper;

        if (path.Contains("arm_weapon"))
            return ComponentType.Weapon;

        if (path.Contains("arm_armbase"))
            return ComponentType.Arm;

        if (path.Contains("body_shape"))
            return ComponentType.Body;

        return ComponentType.Unknown;
    }

    private void CheckAttachment(
        MeshCollider a,
        MeshCollider b,
        ComponentType typeA,
        ComponentType typeB,
        float distance,
        HashSet<string> attachedComponents)
    {
        if (distance < minAttachmentPenetration)
            return;

        if (typeA == ComponentType.Wheel && typeB == ComponentType.Body)
            attachedComponents.Add(GetHierarchyPath(a.transform));

        if (typeB == ComponentType.Wheel && typeA == ComponentType.Body)
            attachedComponents.Add(GetHierarchyPath(b.transform));

        if (typeA == ComponentType.Battery && typeB == ComponentType.Body)
            attachedComponents.Add("Battery");

        if (typeB == ComponentType.Battery && typeA == ComponentType.Body)
            attachedComponents.Add("Battery");

        if (typeA == ComponentType.Bumper && typeB == ComponentType.Body)
            attachedComponents.Add(GetHierarchyPath(a.transform));

        if (typeB == ComponentType.Bumper && typeA == ComponentType.Body)
            attachedComponents.Add(GetHierarchyPath(b.transform));

        if (typeA == ComponentType.Arm && typeB == ComponentType.Body)
            attachedComponents.Add("Arm");

        if (typeB == ComponentType.Arm && typeA == ComponentType.Body)
            attachedComponents.Add("Arm");

        if (typeA == ComponentType.Weapon && typeB == ComponentType.Arm)
            attachedComponents.Add("Weapon");

        if (typeB == ComponentType.Weapon && typeA == ComponentType.Arm)
            attachedComponents.Add("Weapon");
    }

    private float GetMaximumPenetration(
        ComponentType a,
        ComponentType b)
    {
        if ((a == ComponentType.Wheel && b == ComponentType.Body) ||
            (b == ComponentType.Wheel && a == ComponentType.Body))
            return maxWheelBodyPenetration;

        if ((a == ComponentType.Wheel && b == ComponentType.Bumper) ||
            (b == ComponentType.Wheel && a == ComponentType.Bumper))
            return maxWheelBumperPenetration;

        // Battery penetration is checked only against Body.
        if ((a == ComponentType.Battery && b == ComponentType.Body) ||
            (b == ComponentType.Battery && a == ComponentType.Body))
            return maxBatteryPenetration;

        return -1f;
    }

    private bool CheckRequiredAttachments(
        HashSet<string> attachedComponents)
    {
        bool allAttached = true;

        string[] required = {
            "Battery",
            "Arm",
            "Weapon"
        };

        foreach (string component in required)
        {
            if (!attachedComponents.Contains(component))
            {
                Debug.LogError(
                    $"[Physical Overlap] MISSING ATTACHMENT: {component} has no confirmed connection.",
                    gameObject
                );

                allAttached = false;
            }
        }

        if (!CheckGroupAttachments("Wheels Left", attachedComponents))
            allAttached = false;

        if (!CheckGroupAttachments("Wheels Right", attachedComponents))
            allAttached = false;

        if (!CheckGroupAttachments("Bumper_Shape2", attachedComponents))
            allAttached = false;

        if (!CheckGroupAttachments("Bumper_Shape2 (1)", attachedComponents))
            allAttached = false;

        return allAttached;
    }

    private bool CheckGroupAttachments(
        string group,
        HashSet<string> attachedComponents)
    {
        foreach (string path in attachedComponents)
        {
            if (path.Contains(group))
                return true;
        }

        Debug.LogError(
            $"[Physical Overlap] MISSING ATTACHMENT: {group} has no confirmed connection.",
            gameObject
        );

        return false;
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
