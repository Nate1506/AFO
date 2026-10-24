using UnityEngine;

/// <summary>
/// Emergency light flicker — simulates red warning light
/// </summary>
public class EmergencyLightFlicker : MonoBehaviour
{
    public float flickerRate = 1.2f;
    public float intensityMin = 0.5f;
    public float intensityMax = 3f;

    private Light _light;
    private float _time;

    void Start() => _light = GetComponent<Light>();

    void Update()
    {
        if (_light == null) return;
        _time += Time.deltaTime;
        float t = (Mathf.Sin(_time * flickerRate * Mathf.PI * 2f) + 1f) * 0.5f;
        _light.intensity = Mathf.Lerp(intensityMin, intensityMax, t);
    }
}
