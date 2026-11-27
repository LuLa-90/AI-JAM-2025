using UnityEngine;

enum Axis
{
    x,
    y,
    z
};

public class Autorotation : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private Axis osTocenia;

    private float x;
    private float y;
    private float z;

    void Start()
    {
        if (osTocenia == Axis.x) { x = speed; }
        if (osTocenia == Axis.y) { y = speed; }
        if (osTocenia == Axis.z) { z = speed; }
    }

    void Update()
    {
        transform.Rotate(x, y, z);
    }
}
