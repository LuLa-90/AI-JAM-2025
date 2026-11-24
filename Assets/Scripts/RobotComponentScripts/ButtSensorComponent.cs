using System.Linq;
using UnityEngine;

public class ButtSensorComponent : MonoBehaviour {

    private SensorComponent buttSensor = null;
    private ButtMarker buttMarkerObject;
    private RobotAgent thisRobot;

    private void Start() {
        thisRobot = GetComponentInParent<RobotAgent>();

        // TODO: znormalizovat
        buttSensor = GetComponentsInChildren<SensorComponent>()
            .FirstOrDefault(s => s.GetSensorType == SensorComponent.SensorType.ButtSensor);

        FindEnemyMarker();
    }

    /// <summary>
    /// Looks up the enemy's marker. The robot may learn who its enemy is only after this Start has run
    /// (the order of Start calls is not fixed), so Update keeps trying until the marker is found.
    /// </summary>
    private void FindEnemyMarker() {
        if (thisRobot == null || thisRobot.enemyRobot == null) return;
        buttMarkerObject = thisRobot.enemyRobot.GetComponentInChildren<ButtMarker>();
    }

    /// <summary>
    /// True when the first solid thing on the line from the sensor to the marker is the marker itself.
    /// Trigger colliders (traps and buffs in an arena) don't block the view.
    /// </summary>
    public static bool SeesMarker(Vector3 from, Vector3 marker) {
        return Physics.Raycast(from, (marker - from).normalized, out RaycastHit hitInfo,
                   Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
               && hitInfo.collider.CompareTag("ButtTag");
    }

    private void Update() {
        if (buttSensor == null) return;
        if (buttMarkerObject == null) FindEnemyMarker();
        if (buttMarkerObject == null) return;

        buttSensor.SensorValue = SeesMarker(transform.position, buttMarkerObject.transform.position) ? 1f : 0f;
    }

    private void OnDrawGizmos() {
        if (buttSensor != null && buttSensor.SensorValue == 1f) {
            Gizmos.color = Color.green;
        }
        else {
            Gizmos.color = Color.blue;
        }

        if (buttMarkerObject != null) {
            Gizmos.DrawLine(transform.position, buttMarkerObject.transform.position);
        }
    }
}
