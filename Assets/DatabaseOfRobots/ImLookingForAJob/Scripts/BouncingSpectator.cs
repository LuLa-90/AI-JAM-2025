using UnityEngine;

public class BouncingSpectator : MonoBehaviour
{
    private Vector3 _startLocalPos;
    private float _amplitude;  
    private float _speed;     
    private float _offset;    

    void Start()
    {
        _startLocalPos = transform.localPosition;

        _amplitude = Random.Range(0.05f, 0.25f); 
        _speed = Random.Range(1.0f, 3.0f);      
        _offset = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        float yOffset = Mathf.Sin(Time.time * _speed + _offset) * _amplitude;

        yOffset = Mathf.Abs(yOffset);

        transform.localPosition = _startLocalPos + Vector3.up * yOffset;
    }
}
