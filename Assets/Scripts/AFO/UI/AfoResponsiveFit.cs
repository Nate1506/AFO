using UnityEngine;

/// <summary>Scales a top-left anchored panel group down on narrower displays.</summary>
public sealed class AfoResponsiveFit : MonoBehaviour
{
    public float designWidth = 1920f;
    float previousWidth = -1f;

    void LateUpdate()
    {
        var rect = transform.parent as RectTransform;
        if (!rect || Mathf.Approximately(previousWidth, rect.rect.width)) return;
        previousWidth = rect.rect.width;
        float scale = Mathf.Min(1f, rect.rect.width / designWidth);
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}
