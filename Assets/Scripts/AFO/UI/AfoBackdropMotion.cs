using UnityEngine;

/// <summary>Shared motion axis for the menu's geometric backdrop.</summary>
public sealed class AfoBackdropMotion : MonoBehaviour
{
    public static bool ReducedMotion { get; set; }
    public RectTransform[] shapes;
    Vector2[] homes;
    Quaternion[] homeRotations;
    float entranceProgress = 1f;
    float selectionTarget;
    float selectionOffset;
    float selectionVelocity;
    float confirmTarget;
    float confirmOffset;
    float confirmVelocity;

    void Awake()
    {
        homes = new Vector2[shapes == null ? 0 : shapes.Length];
        homeRotations = new Quaternion[homes.Length];
        for (int i = 0; i < homes.Length; i++) if (shapes[i])
        {
            homes[i] = shapes[i].anchoredPosition;
            homeRotations[i] = shapes[i].localRotation;
        }
    }

    void Update()
    {
        if (shapes == null) return;
        if (ReducedMotion)
        {
            entranceProgress = 1f;
            selectionOffset = selectionTarget = 0f;
            confirmOffset = confirmTarget = 0f;
            for (int i = 0; i < shapes.Length; i++) if (shapes[i])
            {
                shapes[i].anchoredPosition = homes[i];
                shapes[i].localRotation = homeRotations[i];
            }
            return;
        }

        float time = Time.unscaledTime;
        float delta = Time.unscaledDeltaTime;
        selectionOffset = Mathf.SmoothDamp(selectionOffset, selectionTarget, ref selectionVelocity, 0.24f, Mathf.Infinity, delta);
        confirmOffset = Mathf.SmoothDamp(confirmOffset, confirmTarget, ref confirmVelocity, 0.11f, Mathf.Infinity, delta);
        Vector2 sharedDrift = new Vector2(Mathf.Sin(time * 0.22f) * 2f, Mathf.Cos(time * 0.17f) * 1.25f);
        Vector2 entryOffset = new Vector2(24f, -10f) * (1f - entranceProgress);
        Vector2 selectionMotion = new Vector2(selectionOffset, -selectionOffset * 0.24f);
        Vector2 confirmMotion = new Vector2(-confirmOffset * 18f, confirmOffset * 7f);
        for (int i = 0; i < shapes.Length; i++)
        {
            if (!shapes[i]) continue;
            shapes[i].anchoredPosition = homes[i] + sharedDrift + entryOffset + selectionMotion + confirmMotion;
            float pairedAccent = i < 2 ? Mathf.Sin(time * 0.13f) * 0.22f : 0f;
            shapes[i].localRotation = homeRotations[i] * Quaternion.Euler(0f, 0f, pairedAccent);
        }
    }

    public void SetEntranceProgress(float progress) => entranceProgress = Mathf.Clamp01(progress);

    public void SetSelection(int index)
    {
        selectionTarget = ReducedMotion || index < 0 ? 0f : (index - 1.5f) * 1.25f;
    }

    public void SetConfirming(bool confirming) => confirmTarget = confirming ? 1f : 0f;
}
