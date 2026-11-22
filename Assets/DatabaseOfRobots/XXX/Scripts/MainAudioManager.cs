using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class MainAudioManager : MonoBehaviour
{
    public static MainAudioManager Instance;

    public GameObject spawnAudio;

    private void Awake(){
        if (Instance != null){
            Destroy(gameObject);
        } else {
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
    }
    public void SpawnAudio(AudioClip[] clips, float volume, Vector2 position, bool spatial = false, float distance = 100)
    {
        GameObject g = Instantiate(spawnAudio ,position, Quaternion.identity);

        g.GetComponent<AudioSource>().clip = clips[Random.Range(0, clips.Length)];
        g.GetComponent<AudioSource>().volume = volume;
        g.GetComponent<AudioSource>().spatialBlend = spatial ? 1 : 0;
        g.GetComponent<AudioSource>().maxDistance = distance;
        g.GetComponent<AudioSource>().minDistance = distance / 2;

        g.GetComponent<AudioSource>().Play();
        Destroy(g,g.GetComponent<AudioSource>().clip.length + 1);

    }
    public void SpawnAudio(AudioClip clips, float volume, Vector2 position, bool spatial = false, float distance = 100)
    {
        GameObject g = Instantiate(spawnAudio ,position, Quaternion.identity);

        g.GetComponent<AudioSource>().clip = clips;
        g.GetComponent<AudioSource>().volume = volume;
        g.GetComponent<AudioSource>().spatialBlend = spatial ? 1 : 0;
        g.GetComponent<AudioSource>().maxDistance = distance;
        g.GetComponent<AudioSource>().minDistance = distance / 2;

        g.GetComponent<AudioSource>().Play();
        Destroy(g,g.GetComponent<AudioSource>().clip.length + 1);

    }
    public void SpawnAudio(AudioClip[] clips, float volume, bool spatial = false, float distance = 100)
    {
        GameObject g = Instantiate(spawnAudio ,Vector2.zero, Quaternion.identity);

        g.GetComponent<AudioSource>().clip = clips[Random.Range(0, clips.Length)];
        g.GetComponent<AudioSource>().volume = volume;
        g.GetComponent<AudioSource>().spatialBlend = spatial ? 1 : 0;
        g.GetComponent<AudioSource>().maxDistance = distance;
        g.GetComponent<AudioSource>().minDistance = distance / 2;

        g.GetComponent<AudioSource>().Play();
        Destroy(g,g.GetComponent<AudioSource>().clip.length + 1);

    }
    public void SpawnAudio(AudioClip clips, float volume, bool spatial = false, float distance = 100)
    {
        GameObject g = Instantiate(spawnAudio ,Vector2.zero, Quaternion.identity);

        g.GetComponent<AudioSource>().clip = clips;
        g.GetComponent<AudioSource>().volume = volume;
        g.GetComponent<AudioSource>().spatialBlend = spatial ? 1 : 0;
        g.GetComponent<AudioSource>().maxDistance = distance;
        g.GetComponent<AudioSource>().minDistance = distance / 2;
        
        g.GetComponent<AudioSource>().Play();
        Destroy(g,g.GetComponent<AudioSource>().clip.length + 1);

    }
}
