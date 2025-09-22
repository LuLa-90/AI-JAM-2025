using UnityEngine;

public class SensorComponent : MonoBehaviour
{
    public enum SensorType {
        Angle,
        Damage,
        VelocityForwards,
        VelocitySideways,
        AzimuthToTarget,
        ProximityBool
    }

    [SerializeField] private SensorType sensorType;

    public SensorType GetSensorType => sensorType;

    public float SensorValue { get; set; }
}
