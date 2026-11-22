using UnityEngine;

public class BackgroundMusicController : MonoBehaviour
{
    public AudioClip backgroundMusic;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.clip = backgroundMusic;

        audioSource.loop = true;

        audioSource.Play();
    }

    void OnApplicationQuit()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}
