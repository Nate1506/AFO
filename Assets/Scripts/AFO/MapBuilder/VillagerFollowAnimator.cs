using UnityEngine;

/// <summary>
/// Villager follow-player animation placeholder
/// Villagers wander in group, reacting to player commands
/// </summary>
public class VillagerFollowAnimator : MonoBehaviour
{
    public float wanderRadius = 1.2f;
    public float wanderSpeed = 0.6f;
    public float bobAmplitude = 0.04f;
    public float bobSpeed = 2f;

    private Transform[] _villagers;
    private Vector3[] _startOffsets;
    private float _time;

    void Start()
    {
        var vList = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in transform)
            if (child.name.StartsWith("Villager_"))
                vList.Add(child);
        _villagers = vList.ToArray();
        _startOffsets = new Vector3[_villagers.Length];
        for (int i = 0; i < _villagers.Length; i++)
            _startOffsets[i] = _villagers[i].localPosition;
    }

    void Update()
    {
        _time += Time.deltaTime;
        for (int i = 0; i < _villagers.Length; i++)
        {
            if (_villagers[i] == null) continue;
            float phase = i * 1.5f;
            var p = _startOffsets[i];
            p.x += Mathf.Sin(_time * wanderSpeed + phase) * wanderRadius * 0.15f;
            p.z += Mathf.Cos(_time * wanderSpeed * 0.8f + phase) * wanderRadius * 0.1f;
            p.y = _startOffsets[i].y + Mathf.Abs(Mathf.Sin(_time * bobSpeed + phase)) * bobAmplitude;
            _villagers[i].localPosition = p;
        }
    }
}
