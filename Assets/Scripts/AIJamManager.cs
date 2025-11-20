using UnityEngine;
using UnityEngine.UIElements;

public class AIJamManager : MonoBehaviour
{
    [SerializeField] RobotAgent robotPrefabA;
    [SerializeField] RobotAgent robotPrefabB;
    private RobotAgent robotA;
    private RobotAgent robotB;

    [SerializeField] Transform environment;

    private void Start() {
        if (robotPrefabA == null || robotPrefabB == null) {
            Debug.LogError("AIJamManager: One or both RobotAgent references are not set.");
            return;
        }

        robotA = Instantiate(robotPrefabA, environment);
        robotB = Instantiate(robotPrefabB, environment);

        robotA.OnNewEpisode += Robot_OnNewEpisode;
        robotB.OnNewEpisode += Robot_OnNewEpisode;
    }

    private void Robot_OnNewEpisode(object sender, System.EventArgs e) {
        MoveRobot(sender as RobotAgent);
    }

    private void MoveRobot(RobotAgent robot) {
        // TODO: premiestnit respawn do RobotAgent skriptu a zavolat z OnEpisodeBegin

        float angle = 0f;
        if (robot == robotA) {
            angle = Random.Range(-25f, -155f);
        } else if (robot == robotB) {
            angle = Random.Range(25f, 155f);
        }

        robot.transform.localRotation = Quaternion.identity;
        robot.transform.localPosition = new Vector3(0, 0.022f, 0);
        robot.transform.localPosition += Quaternion.Euler(0, angle, 0) * Vector3.forward * Random.Range(0.8f, 1.7f);
        robot.transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
    }
}
