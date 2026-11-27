using UnityEngine;

public class ArenaEffectZone : MonoBehaviour
{
    public bool reward = true;
    public float amount = 0.2f;

    private bool used = false;
    public static bool hitted;

    private void OnTriggerEnter(Collider other)
    {
        if (used)
        {
            Destroy(gameObject);
            return;
        }

        var agent = other.GetComponentInParent<RobotAgent>();
        if (agent == null) return;

        float hpChange = reward ? amount : -amount;

        foreach (var zone in agent.GetComponentsInChildren<CollisionZoneBehaviour>())
        {
            zone.ChangeHealth(hpChange);
            hitted = false;
            //zone.ChangeHealth(hp / zoneComponents.Length);
        }
        if (reward)
            agent.AddReward(1000);
        else
            agent.AddReward(-1000);

        used = true;
        Destroy(gameObject);
    }
}
