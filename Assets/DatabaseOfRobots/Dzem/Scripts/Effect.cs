using UnityEngine;

public abstract class Effect : MonoBehaviour
{

    protected abstract Reward Reward { get; }

    protected abstract void ApplyEffect(Collider other);

    private void OnTriggerEnter(Collider other)
    {
        RobotAgent agent = other.GetComponentInParent<RobotAgent>();
        if (agent != null)
        {
            agent.AddReward((int)Reward);
        }

        ApplyEffect(other);
    }


}
