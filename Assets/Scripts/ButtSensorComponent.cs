using System.Linq;
using UnityEngine;

public class ButtSensorComponent : MonoBehaviour {

    private SensorComponent buttSensor = null;
    private ButtMarker buttMarkerObject;
    private RobotAgent thisRobot;

    private void Start() {
        thisRobot = GetComponentInParent<RobotAgent>();

        var sensors = GetComponentsInChildren<SensorComponent>();

        foreach (var sensor in sensors) {
            // TODO: znormalizovat
            if (sensor.GetSensorType == SensorComponent.SensorType.ButtSensor) {
                buttSensor = sensor;

                buttMarkerObject = thisRobot.enemyRobot.GetComponentInChildren<ButtMarker>();

                //GameObject.FindGameObjectsWithTag("ButtTag")
                //    .FirstOrDefault(o=>o == thisRobot.enemyRobot.gameObject); // .transform.position

                

            }
        }
    }

    private void Update() {
        if (Physics.Raycast(transform.position,
            (buttMarkerObject.transform.position - transform.position).normalized,
            out RaycastHit hitInfo)) {
            Debug.Log("Butt Receiver Hit:" + hitInfo.collider.name);
        }
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.yellow;

        if (buttMarkerObject != null) {
            Gizmos.DrawLine(transform.position, buttMarkerObject.transform.position);
        }
    }
}
