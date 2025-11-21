using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class StatisticsGUI : MonoBehaviour {

    private RobotAgent robotA;
    private RobotAgent robotB;

    private GameObject robotBoardA;
    private GameObject robotBoardB;

    private void Start() {
        robotBoardA = transform.Find("RobotBoardA").gameObject;
        robotBoardB = transform.Find("RobotBoardB").gameObject;
    }

    internal void SetRobots(RobotAgent robotA, RobotAgent robotB) {
        this.robotA = robotA;
        this.robotB = robotB;
    }

    private void Update() {
        if (robotA != null) {
            UpdateRobotBoard(robotBoardA, robotA);
        }

        if (robotB != null) {
            UpdateRobotBoard(robotBoardB, robotB);
        }
    }

    private void UpdateRobotBoard(GameObject robotBoard, RobotAgent robot) {
        StatsBarGUI[] statBarArray = robotBoard.GetComponentsInChildren<StatsBarGUI>();

        foreach (StatsBarGUI statBar in statBarArray) {
            switch (statBar.Name) {
                case "StatItemBumper":
                    //statBar.UpdateStatBar(robot.GetCumulativeReward());
                    break;
                case "StatItemBody":
                    //statBar.UpdateStatBar(robot.GetMovementReward());
                    break;
                case "StatItemLWheels":
                    //statBar.UpdateStatBar(robot.GetRobotFacingReward());
                    break;
                case "StatItemRWheels":
                    //statBar.UpdateStatBar(robot.GetArmReleaseReward());
                    break;
                case "StatItemArm":
                    //statBar.UpdateStatBar(robot.GetEnemyDistanceReward());
                    break;
                default:
                    Debug.LogWarning("Unknown stat name: " + statBar.Name);
                    break;
            }
        }

        // Debug.Log("Updating " + statBarArray.Count() + " bars for " + robot.name);
    }
}
