/*using System.Collections;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class bonus : MonoBehaviour
{
    [Header("Bonus Settings")]
    public bool reward;
    private RobotAgent agent;
    public bool trapFreeze;
    public bool trapCatapult; //catapult bud impact sily alebo platforma ktora sa odstreli
    public bool rewardHeal; //urob +20 percent heal vsetkych komponentov
    public bool rewardDamagePlus20; 
    private RobotMovement robot;

    // when the robot get bonus then it can have damage plus 20 % for its attacks.
    // around the robot once the bonus has been collected then we will have around us a damage of 20 % for 5 seconds.


    [Header ("Bonus health")]
    private Vector3 catapultForce = new Vector3(0, 20f, 0);

    private IEnumerator SetFreezeForTime(RobotMovement target, float duration)
    {
        if (target == null) yield break;

        target.stationaryRobot = true;
        yield return new WaitForSeconds(duration); // wait here
        target.stationaryRobot = false;
    }

    private IEnumerator SetDamageForTime()
    {

        // First, save the original damage values for later restoration
        float[] originalDamageValues = new float[agent.zoneComponents.Length];

        // Store the original damage value of each zone component
        for (var i = 0; i < agent.zoneComponents.Length; i++)
        {
            originalDamageValues[i] = agent.zoneComponents[i].damageOfZone;  // Save the original damage value
        }

        // Modify the damageOfZone values by adding 50% of the original value
        for (var i = 0; i < agent.zoneComponents.Length; i++)
        {
            agent.zoneComponents[i].damageOfZone += originalDamageValues[i] * 0.5f;  // Add 50% of the original value
        }

        // Wait for 5 seconds before restoring the original values
        yield return new WaitForSeconds(5f);

        Debug.Assert(agent.zoneComponents.Length == originalDamageValues.Length, "Zone components length mismatch.");       

        // Restore the original damage values back to their respective components
        for (var i = 0; i < agent.zoneComponents.Length; i++)
        {
            agent.zoneComponents[i].damageOfZone = originalDamageValues[i];  // Restore the original value
        }


    }

    private void OnTriggerEnter(Collider other)
    {
        agent = other.GetComponentInParent<RobotAgent>();
        robot = other.GetComponentInParent<RobotMovement>();
        // vymazat robot != null pod prvomif 
        if (agent != null & robot != null) {
            if (reward == true)
            {
                agent.AddReward(1000f);
            }
            if (reward == false)
            {
                agent.AddReward(-1000f);
            }

            if (trapFreeze==true)
            {
                robot.StartCoroutine(SetFreezeForTime(robot, 5f));
            }

            if (trapCatapult == true) {
                var rigidbody = other.GetComponentInParent<Rigidbody>();
                if (rigidbody != null) {
                    rigidbody.AddForce(catapultForce, ForceMode.Impulse);
                }
            }
            if (rewardHeal && agent.zoneComponents != null)
            {
                // Heal 20% of current health for each zone component
                foreach (var zone in agent.zoneComponents)
                {
                    if (zone == null) continue;
                    float healAmount = zone.CurrentHealth * 0.2f;
                    zone.ChangeHealth(healAmount);
                }
            }

            if (rewardDamagePlus20 && agent.zoneComponents.Length > 0)
            {
                StartCoroutine(SetDamageForTime()); // Start the coroutine to modify damage
            }
        }

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        var rend = GetComponent<Renderer>();
        if (rend != null) rend.enabled = false;

        // Destroy bonus object now
        Destroy(gameObject); 

    }

}*/
    

  



