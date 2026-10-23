using UnityEngine;

/// <summary>Slow, low-amplitude movement for the menu's geometric backdrop.</summary>
public sealed class AfoBackdropMotion : MonoBehaviour
{
    public static bool ReducedMotion { get; set; }
    public RectTransform[] shapes;
    Vector2[] homes;

    void Awake()
    {
        homes = new Vector2[shapes == null ? 0 : shapes.Length];
        for (int i = 0; i < homes.Length; i++) if (shapes[i]) homes[i] = shapes[i].anchoredPosition;
    }

    void Update()
    {
        if (ReducedMotion || shapes == null) return;
        float time = Time.unscaledTime;
        for (int i = 0; i < shapes.Length; i++)
        {
            if (!shapes[i]) continue;
            float phase = i * 1.7f;
            shapes[i].anchoredPosition = homes[i] + new Vector2(Mathf.Sin(time * 0.22f + phase) * 3f, Mathf.Cos(time * 0.17f + phase) * 2f);
        }
    }
}
