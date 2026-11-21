using UnityEngine;

/// <summary>
/// Gently bobs and rotates floating background office decor props in the void.
/// Used only on non-walkable DECOR_FLOATING background props.
/// </summary>
public class FloatingDecorBob : MonoBehaviour
{
    public float bobAmplitude = 0.6f;
    public float bobFrequency = 0.45f;
    public Vector3 rotationDegreesPerSecond = new Vector3(6f, 11f, 4f);

    private Vector3 startPos;
    private float phaseOffset;

    private void Awake()
    {
        startPos = transform.position;
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        float y = Mathf.Sin(Time.time * bobFrequency + phaseOffset) * bobAmplitude;
        transform.position = startPos + new Vector3(0f, y, 0f);
        transform.Rotate(rotationDegreesPerSecond * Time.deltaTime, Space.Self);
    }
}
