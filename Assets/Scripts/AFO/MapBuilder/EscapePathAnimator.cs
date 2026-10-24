using UnityEngine;

/// <summary>
/// Animates escape path line with pulsing glow and arrows bobbing
/// </summary>
public class EscapePathAnimator : MonoBehaviour
{
    public float pulseSpeed = 2f;
    public float pulseMin = 0.5f;
    public float pulseMax = 1f;
    public float arrowBobSpeed = 1.5f;
    public float arrowBobAmp = 0.05f;

    private LineRenderer _lr;
    private Transform[] _arrows;
    private Vector3[] _arrowStartPos;
    private float _time;

    void Start()
    {
        _lr = GetComponent<LineRenderer>();
        var arrowList = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in transform.parent)
            if (child.name.StartsWith("Arrow_"))
                arrowList.Add(child);
        _arrows = arrowList.ToArray();
        _arrowStartPos = new Vector3[_arrows.Length];
        for (int i = 0; i < _arrows.Length; i++)
            _arrowStartPos[i] = _arrows[i].position;
    }

    void Update()
    {
        _time += Time.deltaTime;
        // Pulse line width
        if (_lr != null)
        {
            float w = Mathf.Lerp(pulseMin, pulseMax, (Mathf.Sin(_time * pulseSpeed) + 1f) * 0.5f) * 0.3f;
            _lr.startWidth = w;
            _lr.endWidth = w * 0.5f;
        }
        // Bob arrows
        for (int i = 0; i < _arrows.Length; i++)
        {
            if (_arrows[i] == null) continue;
            float phase = i * 0.8f;
            var p = _arrowStartPos[i];
            p.y += Mathf.Sin(_time * arrowBobSpeed + phase) * arrowBobAmp;
            _arrows[i].position = p;
        }
    }
}
