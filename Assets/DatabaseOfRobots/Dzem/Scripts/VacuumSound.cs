using UnityEngine;

public class VacuumSound : MonoBehaviour
{
    [SerializeField] private Rigidbody Vacuum;
    [SerializeField] private AudioSource VacuumAudioSource;
    [SerializeField] private int maxSpeed;

    void Update()
    {
        float speed = Vacuum.linearVelocity.magnitude;
        float t = Mathf.Clamp01(speed / 2.2f);

        float targetPitch = Mathf.Lerp(0.90f, 1.05f, t);
        float targetVolume = Mathf.Lerp(0.8f, 1f, t);

        float smooth = 10f;

        VacuumAudioSource.pitch = Mathf.Lerp(
            VacuumAudioSource.pitch,
            targetPitch,
            Time.deltaTime * smooth
        );

        VacuumAudioSource.volume = Mathf.Lerp(
            VacuumAudioSource.volume,
            targetVolume,
            Time.deltaTime * smooth
        );
    }
}
