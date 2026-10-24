using UnityEngine;

/// <summary>
/// Animates lightning flash effect — random bursts
/// </summary>
public class LightningAnimator : MonoBehaviour
{
    public float minInterval = 3f;
    public float maxInterval = 8f;
    public float flashDuration = 0.08f;
    public float afterGlowDuration = 0.25f;

    private Light _light;
    private float _nextFlash;
    private float _timer;
    private bool _flashing;
    private float _flashTimer;

    void Start()
    {
        _light = GetComponent<Light>();
        _nextFlash = Random.Range(minInterval, maxInterval);
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_flashing)
        {
            _flashTimer += Time.deltaTime;
            if (_flashTimer < flashDuration)
                _light.intensity = Random.Range(15f, 25f);
            else if (_flashTimer < flashDuration + afterGlowDuration)
                _light.intensity = Mathf.Lerp(5f, 0f, (_flashTimer - flashDuration) / afterGlowDuration);
            else
            {
                _light.intensity = 0f;
                _flashing = false;
                _flashTimer = 0f;
                _timer = 0f;
                _nextFlash = Random.Range(minInterval, maxInterval);
            }
        }
        else if (_timer >= _nextFlash)
        {
            _flashing = true;
            _flashTimer = 0f;
        }
    }
}
