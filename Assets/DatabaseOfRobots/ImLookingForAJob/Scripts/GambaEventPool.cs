using System.Collections.Generic;
using UnityEngine;

public class GambaEventPool : MonoBehaviour
{
    [Header("Good events for green box")]
    public List<GambaEventDefinition> goodEvents = new List<GambaEventDefinition>();

    [Header("Bad events for red box")]
    public List<GambaEventDefinition> badEvents = new List<GambaEventDefinition>();

    public GambaEventDefinition GetRandomEvent(bool good)
    {
        var list = good ? goodEvents : badEvents;

        if (list == null || list.Count == 0)
        {
            Debug.LogWarning("GambaEventPool: No events in " + (good ? "good" : "bad") + " list.");
            return null;
        }

        int index = Random.Range(0, list.Count);
        return list[index];
    }
}