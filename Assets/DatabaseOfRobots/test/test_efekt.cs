using UnityEngine;

public class test_efekt : MonoBehaviour
{
    private ParticleSystem efekt;

    private void Start()
    {
        efekt = GetComponent<ParticleSystem>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Robot")
        {
            if (efekt != null)
            {
                efekt.Play();
            }
        }
    }
}
