using UnityEngine;

/// <summary>
/// Randomly flashes a Directional Light to simulate lightning during the storm.
/// </summary>
[RequireComponent(typeof(Light))]
public class LightningFlashController : MonoBehaviour
{
    [Header("Lightning Settings")]
    public float minInterval    = 3f;   // seconds between flashes
    public float maxInterval    = 12f;
    public float flashDuration  = 0.12f;
    public float flashIntensity = 8f;
    public Color flashColor     = new Color(0.88f, 0.93f, 1f);

    private Light  _light;
    private float  _timer;
    private float  _nextFlash;
    private bool   _flashing;
    private float  _flashTimer;

    void Awake()
    {
        _light = GetComponent<Light>();
        _light.intensity = 0f;
        _nextFlash = Random.Range(minInterval, maxInterval);
    }

    void Update()
    {
        _timer += Time.deltaTime;

        if (_flashing)
        {
            _flashTimer += Time.deltaTime;
            // Double-flash pattern
            float t = _flashTimer / flashDuration;
            if (t < 0.3f)       _light.intensity = flashIntensity;
            else if (t < 0.45f) _light.intensity = 0f;
            else if (t < 0.75f) _light.intensity = flashIntensity * 0.7f;
            else                _light.intensity = Mathf.Lerp(flashIntensity * 0.7f, 0f, (t - 0.75f) / 0.25f);

            if (_flashTimer >= flashDuration)
            {
                _flashing = false;
                _light.intensity = 0f;
                _timer = 0f;
                _nextFlash = Random.Range(minInterval, maxInterval);
            }
        }
        else if (_timer >= _nextFlash)
        {
            _flashing   = true;
            _flashTimer = 0f;
            _light.color = flashColor;
        }
    }
}
