using UnityEngine;
//using UnityEngine.ProBuilder.Shapes;

public class ThrowNerf : Effect
{
    

    public float launchForce = 2f;
    public Vector3 throwDirection = new Vector3(2, 1, 2);

    protected override Reward Reward
    {
        get
        {
            return Reward.Negative;
        }
    }

    protected override void ApplyEffect(Collider other)
    {
        Rigidbody rb = other.GetComponentInParent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(throwDirection * launchForce, ForceMode.Impulse);
        }

        Destroy(gameObject);
    }
}
