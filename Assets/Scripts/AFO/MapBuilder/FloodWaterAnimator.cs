using UnityEngine;

/// <summary>
/// Animates flood water: wave motion + foam drift
/// Optimized: uses sin/cos in Update with low overhead
/// </summary>
public class FloodWaterAnimator : MonoBehaviour
{
    public float waveSpeed = 0.4f;
    public float waveAmplitude = 0.04f;
    public float foamDriftSpeed = 0.15f;

    private Transform[] _foamObjects;
    private Vector3[] _foamStartPos;
    private float _time;

    void Start()
    {
        var foams = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in transform)
            if (child.name.Contains("Foam"))
                foams.Add(child);
        _foamObjects = foams.ToArray();
        _foamStartPos = new Vector3[_foamObjects.Length];
        for (int i = 0; i < _foamObjects.Length; i++)
            _foamStartPos[i] = _foamObjects[i].localPosition;
    }

    void Update()
    {
        _time += Time.deltaTime;
        var water = transform.Find("Water_Main");
        if (water != null)
        {
            var pos = water.localPosition;
            pos.y = 0.05f + Mathf.Sin(_time * waveSpeed) * waveAmplitude;
            water.localPosition = pos;
        }
        for (int i = 0; i < _foamObjects.Length; i++)
        {
            if (_foamObjects[i] == null) continue;
            float phase = i * 1.2f;
            var p = _foamStartPos[i];
            p.x += Mathf.Sin(_time * foamDriftSpeed + phase) * 0.3f;
            p.z += Mathf.Cos(_time * foamDriftSpeed * 0.7f + phase) * 0.2f;
            p.y = _foamStartPos[i].y + Mathf.Sin(_time * waveSpeed * 1.5f + phase) * 0.02f;
            _foamObjects[i].localPosition = p;
        }
    }
}
