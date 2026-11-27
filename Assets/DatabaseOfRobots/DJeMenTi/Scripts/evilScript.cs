using UnityEngine;
using System.Linq;
public class evilScript : MonoBehaviour
{
    RobotAgent santa = null;
    private IHealth[] healthComponents;
    Transform normalArena;
    Transform evilArena;
    Transform armShape;
    Transform evilarmShape;
    Transform santaModel;
    Transform evilsantaModel;
    AudioSource transitionEffect;

    // Start is called once before the first execution of Update after the MonoBehaviour is 
    void Start()
    {
        transitionEffect = GetComponent<AudioSource>();
        for (int i = 0; i < transform.childCount; i++) {
            Transform child = transform.GetChild(i);
            if (child.name == "normalArena") normalArena = child;
            else if (child.name == "evilArena") evilArena = child;

            if(normalArena != null && evilArena != null) break;
        }
        for (int i = 0; i < transform.root.transform.childCount; i++)
        {
            Debug.Log(transform.root.GetChild(i));
        }

        santa = GameObject.FindAnyObjectByType<marker>().GetComponent<RobotAgent>();//Find("Santabumpers");
    }

    private void LateUpdate()
    {
        CheckIfDied();
    }

    private void CheckIfDied()
    {
        //santa = GameObject.Find("Santabumpers").GetComponent<RobotAgent>();//Find("Santabumpers");
        //if (santa == null) return;
        healthComponents = santa.GetComponentsInChildren<IHealth>();
        armShape = santa.transform.Find("Arm_Shape1").Find("santas_wand");
        /*for (int i = 0; i < santa.transform.Find("Arm_Shape1").childCount; i++)
        {
            Debug.Log(santa.transform.Find("Arm_Shape1").GetChild(i).name);
        }   */
        evilarmShape = santa.transform.Find("Arm_Shape1").Find("evil_santas_wand");
        santaModel = santa.transform.Find("SantaModel");
        evilsantaModel = santa.transform.Find("evil_santa");
        //Debug.Log(evilsantaModel.name);
        //Debug.Log(santaModel.name);
        //Debug.Log(armShape.name);
        //Debug.Log(evilarmShape.name);
        if (healthComponents.Count() == 0 || santa == null)
        {
            return;
        }


        // Probably not needed, because of the new weighted average method of average health

        //foreach (var healthComp in healthComponents) {
        //    switch (healthComp) {
        //        case BodyComponent bodyComp:
        //            //Debug.Log($"Body health: {healthComp.CurrentHealth}");

        //            if (healthComp.CurrentHealth <= 0.1f) {
        //                Debug.Log("Robot body destroyed!");
        //                // TODO: Method for robot dying, losing/winning logic
        //                return true;
        //            }
        //            break;
        //        case WheelComponent wheelComp:
        //            //Debug.Log($"Wheel health: {healthComp.CurrentHealth}");
        //            break;
        //        case BumperComponent bumperComp:
        //            //Debug.Log($"Bumper health: {healthComp.CurrentHealth}");
        //            break;
        //        case ArmComponent armComp:
        //            //Debug.Log($"Arm health: {healthComp.CurrentHealth}");
        //            break;
        //    }
        //}

        var sumHealth = healthComponents.Sum(e => e.CurrentHealth * e.influenceOnTotalHealth);
        var weightsHealth = healthComponents.Sum(e => e.influenceOnTotalHealth);
        float weightedAverageHealth = sumHealth / weightsHealth;

        if (weightedAverageHealth < 0.8f)
        {
            Debug.Log("poof");
            normalArena.gameObject.SetActive(false);
            santaModel.gameObject.SetActive(false);
            armShape.gameObject.SetActive(false);

            transitionEffect.Play();

            evilArena.gameObject.SetActive(true);
            evilsantaModel.gameObject.SetActive(true);
            evilarmShape.gameObject.SetActive(true);
        }
        else {
            normalArena.gameObject.SetActive(true);
            santaModel.gameObject.SetActive(true);
            armShape.gameObject.SetActive(true);

            //  transitionEffect.Play();

            evilArena.gameObject.SetActive(false);
            evilsantaModel.gameObject.SetActive(false);
            evilarmShape.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
}
