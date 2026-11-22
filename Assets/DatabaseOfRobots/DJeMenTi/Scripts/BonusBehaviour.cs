using System.Collections;
using UnityEngine;


public class BonusBehaviour : MonoBehaviour
{
    float timer = 0f;
    SphereCollider sphereCollider;
    MeshRenderer mesh;
    Transform childMesh;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        childMesh = transform.GetChild(0);
        sphereCollider = gameObject.GetComponent<SphereCollider>();
        mesh = gameObject.GetComponent<MeshRenderer>();

    }
    private void OnTriggerEnter(Collider other)
    {
        Transform parrent = other.transform;
        while (parrent != null)
        {
            if (parrent.CompareTag("Robot"))
            {
                break;
            }
            parrent = parrent.parent;
        }
        if (parrent.tag == "Robot") {
            sphereCollider.enabled = false;
            mesh.enabled = false;
            childMesh.gameObject.SetActive(false);
            StartCoroutine(SetAttr(parrent));
            timer = 0f;
        }
    }

    IEnumerator SetAttr(Transform robot)
    {
        RobotMovement robotMovement = robot.GetComponent<RobotMovement>();
        RobotAgent robotAgent = robot.GetComponent<RobotAgent>();
        if (robotMovement != null)
        {
            int rand = 0;
            switch (this.gameObject.tag)
            {
                case "Bonus":
                    robotAgent.AddReward(10000);
                    rand = Random.Range(0, 3);
                    //if (rand == 0) robotMovement.motorTorqueMax = 50f;
                    if (rand == 1)
                    {
                        AssignDamageAndResistanceBoost(2, 1, robot);
                    }
                    else if (rand == 2)
                    {
                        AssignDamageAndResistanceBoost(1, 2, robot);
                    }
                    break;

                case "Trap":
                    robotAgent.AddReward(-10000);
                    rand = Random.Range(0, 4);
                    //if (rand == 0) robotMovement.motorTorqueMax = 5f;
                    if (rand == 1)
                    {
                        Rigidbody rg = robot.GetComponent<Rigidbody>();
                        rg.angularVelocity = new Vector3(100f, 100f, 100f);
                    }
                    else if (rand == 2)
                    {
                        AssignDamageAndResistanceBoost(.5f, 1, robot);
                    }
                    else if (rand == 3)
                    {
                        AssignDamageAndResistanceBoost(1, .5f, robot);
                    }
                    break;

                default:
                    break;
            }
        }
        yield return new WaitForSeconds(5);
        //robotMovement.motorTorqueMax = 25f;
    }


    void Update()
    {
        timer += Time.deltaTime;
        if (timer > 5f && !sphereCollider.enabled && !mesh.enabled)
        {
            sphereCollider.enabled = true;
            mesh.enabled = true;
            childMesh.gameObject.SetActive(true);
        }
    }

    private void AssignDamageAndResistanceBoost(float damageModificator, float resistanceModificator, Transform robot)
    {
        var zoneComponents = robot.GetComponentsInChildren<CollisionZoneBehaviour>();

        foreach (var zone in zoneComponents)
        {
            if (zone.gameObject.name.Contains("body"))
            {
                zone.damageOfZone = 1f * damageModificator;
                zone.resistanceOfZone = 3f * resistanceModificator;
            }
            else if (zone.gameObject.name.Contains("battery"))
            {
                zone.damageOfZone = 1f * damageModificator;
                zone.resistanceOfZone = 1f * resistanceModificator;
            }
            else if (zone.gameObject.name.Contains("wheels"))
            {
                zone.damageOfZone = 5f * damageModificator;
                zone.resistanceOfZone = 4f * resistanceModificator; ;
            }
            else if (zone.gameObject.name.Contains("armbase"))
            {
                zone.damageOfZone = 2f * damageModificator;
                zone.resistanceOfZone = 3f * resistanceModificator; ;
            }
            else if (zone.gameObject.name.Contains("weapon"))
            {
                zone.damageOfZone = 200f * damageModificator;
                zone.resistanceOfZone = 15f * resistanceModificator; ;
            }
            else if (zone.gameObject.name.Contains("bumper"))
            {
                zone.damageOfZone = 150f * damageModificator;
                zone.resistanceOfZone = 10f * resistanceModificator; ;
            }
            else
            {
                zone.damageOfZone = 1f * damageModificator;
                zone.resistanceOfZone = 5f * resistanceModificator; ;
            }
        }
    }
}
