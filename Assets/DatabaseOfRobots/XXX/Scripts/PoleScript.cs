using System.Collections;
using UnityEngine;

public class PoleScript : MonoBehaviour
{
    [SerializeField]
    private AudioClip audioClip;
    [SerializeField]
    private float timer;
    void Start()
    {
        StartCoroutine(Delay(timer));
    }
    IEnumerator Delay(float timer)
    {
        yield return new WaitForSeconds(timer);
        MainAudioManager.Instance.SpawnAudio(audioClip, 0.8f);
        yield return new WaitForSeconds(10f);
        RockedTrapSkript.isActive = true;

    }
}
