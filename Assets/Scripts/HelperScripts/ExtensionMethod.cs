using System.Linq;
using UnityEngine;

public static class ExtensionMethod {

    /// <summary>The lowest health among the zones; 1 (full) for a part without damage zones.</summary>
    public static float MinHealth(this CollisionZoneBehaviour[] zones) {
        if (zones == null || zones.Length == 0) return 1f;
        return zones.Min(z => z.CurrentHealth);
    }

    /// <summary>
    /// Components under a robot part that belong to that part, not to another part nested below it.
    /// All parts sit under the body, so a plain GetComponentsInChildren on the body would also return
    /// the sensors and damage zones of the wheels, arm and bumpers.
    /// </summary>
    public static T[] OwnParts<T>(this MonoBehaviour part) where T : Component {
        return part.GetComponentsInChildren<T>()
            .Where(c => ReferenceEquals(c.GetComponentInParent<IHealth>(), part))
            .ToArray();
    }

    /// <summary>
    /// Sets a sensor's value, optionally normalizing it to the range [-1, 1].
    /// </summary>
    /// <param name="sensor">The sensor to which we want to send our value.</param>
    /// <param name="value">The raw, unprocessed sensor value.</param>
    /// <returns>The final value assigned to the sensor (normalized if enabled).</returns>
    public static float SetSensorValue(this SensorComponent sensor, float value) {
        if (sensor != null) {
            if (sensor.normalized == true) {
                value = Mathf.Clamp(value, sensor.minSensorValue, sensor.maxSensorValue);
                value = Mathf.InverseLerp(sensor.minSensorValue, sensor.maxSensorValue, value) * 2f - 1f;
            }
            sensor.SensorValue = value;
        }
        return value;
    }

    public static void SetRangeValues(this SensorComponent sensor, float minValue, float maxValue) {
        if (sensor != null) {
            sensor.normalized = true;
            sensor.minSensorValue = minValue;
            sensor.maxSensorValue = maxValue;
        }
    }
}
