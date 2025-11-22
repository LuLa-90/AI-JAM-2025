using System.Linq;
using UnityEngine;

public interface IHealth {
    public float CurrentHealth { get; }
}

public class BumperComponent : MonoBehaviour, IHealth
{
    private SensorComponent sensorDamage = null;

    private CollisionZoneBehaviour[] zoneComponents = null;

    float IHealth.CurrentHealth => sensorDamage?.SensorValue ?? 1f;

    private void Start() {
        var sensors = GetComponentsInChildren<SensorComponent>();

        foreach (var sensor in sensors) {
            if (sensor.GetSensorType == SensorComponent.SensorType.Damage) {
                sensorDamage = sensor;
                sensorDamage.SetSensorValue(1f);

                zoneComponents = GetComponentsInChildren<CollisionZoneBehaviour>();
                foreach (var zone in zoneComponents) {
                    zone.SetZoneType("bumper");
                }
            }
        }
    }

    private void Update() {
        float minCurrentHealth = zoneComponents?.Min(e => e.CurrentHealth) ?? 1f;
        sensorDamage.SetSensorValue(minCurrentHealth); // Placeholder for damage value
    }
}
