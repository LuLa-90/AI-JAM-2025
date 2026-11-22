using System.Collections;
using UnityEngine;

public class Svetielka : MonoBehaviour
{
    private GameObject[] Lights;
    private bool coroutineIsRunning;

    void Awake()
    {
        var list = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "Area Light")
                list.Add(child.gameObject);
        }
        Lights = list.ToArray();
    }

    void Update()
    {
        if (!coroutineIsRunning)
        {
            coroutineIsRunning = true;
            StartCoroutine(SwitchLights());
        }
    }

    IEnumerator SwitchLights()
    {
        foreach(GameObject light in Lights)
        {
            light.SetActive(Random.Range(0, 2) == 1);
        }
        yield return new WaitForSeconds(0.4f);
        coroutineIsRunning = false;
    }
}
