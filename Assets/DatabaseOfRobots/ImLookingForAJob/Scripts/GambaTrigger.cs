using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class GambaTrigger : MonoBehaviour
{
    public enum TriggerType
    {
        Good,
        Bad
    }

    [Header("Settings")]
    [SerializeField] private TriggerType triggerType = TriggerType.Good;
    [SerializeField] private GambaSlotController slotController;

    [Header("Respawn")]
    [SerializeField] private bool oneUseOnly = false;
    [SerializeField] private float respawnTime = 5f;
    [SerializeField] private GameObject[] visualObject;

    private Collider triggerCollider;
    private bool used = false;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (used)
        {
            return;
        }

        RobotAgent robot = other.GetComponentInParent<RobotAgent>();
        if (robot == null)
        {
            return;
        }

        bool isGood = triggerType == TriggerType.Good;

        if (isGood)
        {
            robot.AddReward(+1000f);
        }
        else
        {
            robot.AddReward(-1000f);
        }

        if (slotController != null)
        {
            slotController.StartSpinForRobot(robot, isGood);
        }
        else
        {
            Debug.LogWarning("GambaTrigger: SlotController not assigned.");
        }

        used = true;

        if (!oneUseOnly)
        {
            StartCoroutine(RespawnRoutine());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator RespawnRoutine()
    {
        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }

        if (visualObject != null)
        {
            foreach(GameObject v in visualObject)
            {
                v.SetActive(false);
            }
        }

        yield return new WaitForSeconds(respawnTime);

        used = false;

        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
        }

        if (visualObject != null)
        {
            foreach (GameObject v in visualObject)
            {
                v.SetActive(true);
            }
        }
    }
}
