using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Manažuje logiku najvyššej úrovne pre AI Jam. Stará sa o spawnovanie robotov vo viacerých arénach
/// pre účely trénovania alebo turnaja, riadi, začína a končí zápasy.
/// </summary>
public class AIJamManager : MonoBehaviour
{
    // Prefaby robotov pre tréning alebo turnaj. Môžu to byť rovnaké alebo rôzne roboty.
    [SerializeField] RobotAgent robotPrefabA;
    [SerializeField] RobotAgent robotPrefabB;

    [Tooltip("Optional: the team's home arena prefab. When set, training runs in this arena; when empty, in the neutral arena of the scene.")]
    [SerializeField] GameObject teamArenaPrefab;


    // Počet arén pre tréning. Je potrebné otestovať výkon pri rôznych počtoch.
    private enum numArenas {
        One = 1,
        Three = 3,
        Five = 5
    }
    [SerializeField] numArenas numOfArenas = numArenas.Three;


    // Pole všetkých trénovacích prostredí - určené počtom arén zvoleným v inšpektore.
    private GameObject[] environments;


    private void Start() {

        environments = GameObject.FindGameObjectsWithTag("Environment");

        if (robotPrefabA == null || robotPrefabB == null) {
            Debug.LogError("AIJamManager: One or both RobotAgent references are not set.");
            return;
        }

        foreach (var env in environments.OrderBy(e => e.name).Take((int)numOfArenas)) {
            // The arena must be in place before the robots, so that its traps and bonuses find them.
            UseTeamArena(env, teamArenaPrefab);

            var robotA = Instantiate(robotPrefabA, env.transform);
            var robotB = Instantiate(robotPrefabB, env.transform);
            // Agent camera sees only the RobotView layer (old prefabs also saw Default - a bug).
            AIJam.Validation.VisionLayers.ForceAgentCamera(robotA.gameObject);
            AIJam.Validation.VisionLayers.ForceAgentCamera(robotB.gameObject);
            robotA.gameObject.name = robotPrefabA.gameObject.name;
            robotB.gameObject.name = robotPrefabB.gameObject.name;

            robotA.OnRobotRespawn += (s, e) => MoveRobot(s as RobotAgent, 0);
            robotB.OnRobotRespawn += (s, e) => MoveRobot(s as RobotAgent, 1); 

            var statsGUI = env.GetComponentInChildren<StatisticsGUI>();
            statsGUI.SetRobots(robotA, robotB);

            var scoreGUI = env.GetComponentInChildren<ScoreGUI>();
            scoreGUI.SetRobots(robotA, robotB);

        }
    }

    /// <summary>Name of the built-in neutral arena inside every training environment.</summary>
    public const string NeutralArenaName = "Arena";

    /// <summary>
    /// Puts the team's arena into a training environment in place of the built-in neutral one.
    /// Without a prefab nothing changes and the neutral arena stays. Returns the new arena or null.
    /// </summary>
    public static GameObject UseTeamArena(GameObject environment, GameObject arenaPrefab) {
        if (environment == null || arenaPrefab == null) return null;

        var neutral = environment.transform.Find(NeutralArenaName);
        if (neutral != null) neutral.gameObject.SetActive(false);

        var arena = Instantiate(arenaPrefab, environment.transform);
        arena.name = arenaPrefab.name;
        arena.transform.localPosition = Vector3.zero;
        arena.transform.localRotation = arenaPrefab.transform.rotation;
        return arena;
    }

    private void MoveRobot(RobotAgent robot, int robotIdx) {
        // TODO: premiestnit respawn do RobotAgent skriptu a zavolat z OnEpisodeBegin

        float angle = 0f;
        if (robotIdx == 0) {
            angle = Random.Range(-25f, -155f);
        } else if (robotIdx == 1) {
            angle = Random.Range(25f, 155f);
        }

        robot.transform.localRotation = Quaternion.identity;
        robot.transform.localPosition = new Vector3(0, 0.022f, 0);
        robot.transform.localPosition += Quaternion.Euler(0, angle, 0) * Vector3.forward * Random.Range(0.8f, 1.7f);
        robot.transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

        // Stop the robot and its arm. Otherwise a robot that fell or flipped keeps its speed and spin
        // after the respawn and can flip again at once.
        foreach (var body in robot.GetComponentsInChildren<Rigidbody>()) {
            if (body.isKinematic) continue;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }
}
