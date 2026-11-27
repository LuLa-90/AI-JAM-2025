using System.Collections;
using UnityEngine;

public class DisableArmNerf : Effect
{
    public float duration = 3f;

    protected override Reward Reward
    {
        get
        {
            return Reward.Negative;
        }
    }

    protected override void ApplyEffect(Collider other)
    {
        RobotMovement movement = other.GetComponentInParent<RobotMovement>();
        if (movement != null)
        {
            StartCoroutine(TemporarilyDisableArm(movement));
        }

        // Destroy(gameObject);
    }

    IEnumerator TemporarilyDisableArm(RobotMovement movement)
    {
        float endTime = Time.time + duration;

        while (Time.time < endTime)
        {
            movement.ApplyArmInput(0);
            yield return new WaitForFixedUpdate();
        }

        movement.ApplyArmInput(1);
        //float elapsed = 0f;

        //while (elapsed < duration)
        //{
        //    movement.ApplyArmInput(0);
        //    elapsed += Time.fixedDeltaTime;
        //    yield return new WaitForFixedUpdate();
        //}

        //movement.ApplyArmInput(1);
    }
}
