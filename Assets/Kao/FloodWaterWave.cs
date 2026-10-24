using UnityEngine;

/// <summary>
/// Animates the flood water surface with a gentle wave effect.
/// </summary>
[ExecuteAlways]
public class FloodWaterWave : MonoBehaviour
{
    [Header("Wave Settings")]
    public float waveSpeed     = 0.4f;
    public float waveAmplitude = 0.06f;
    public float waveFrequency = 1.2f;
    public float bobSpeed      = 0.8f;

    private Vector3 _startPos;

    void Start()
    {
        _startPos = transform.position;
    }

    void Update()
    {
        float t = Time.time;
        // Gentle vertical bob
        float yOff = Mathf.Sin(t * bobSpeed) * waveAmplitude;
        transform.position = _startPos + new Vector3(0f, yOff, 0f);

        // Tilt slightly like real water
        float tiltX = Mathf.Sin(t * waveSpeed * 0.7f) * 0.4f;
        float tiltZ = Mathf.Cos(t * waveSpeed * 0.5f) * 0.3f;
        transform.rotation = Quaternion.Euler(tiltX, 0f, tiltZ);

        // Animate UV offset on material if it uses a texture
        var mr = GetComponent<MeshRenderer>();
        if (mr != null && mr.sharedMaterial != null)
        {
            var mat = Application.isPlaying ? mr.material : mr.sharedMaterial;
            Vector2 offset = new Vector2(t * waveSpeed * 0.05f, t * waveSpeed * 0.03f);
            mat.mainTextureOffset = offset;
        }
    }
}
