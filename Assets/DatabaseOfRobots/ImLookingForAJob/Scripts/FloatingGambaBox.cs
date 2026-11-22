using UnityEngine;
public class FloatingGambaBox : MonoBehaviour
{
    [Header("Vertical float")]
    [SerializeField] private float amplitude = 0.2f;
    [SerializeField] private float frequency = 1.5f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 45f;

    private Vector3 startLocalPosition;

    private void Start()
    {
        startLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        float newY = startLocalPosition.y + Mathf.Sin(Time.time * frequency) * amplitude;
        transform.localPosition = new Vector3(
            startLocalPosition.x,
            newY,
            startLocalPosition.z
        );

        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
    }
}
