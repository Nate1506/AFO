using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Draws one crisp L-shaped selection mark around the active menu hit target.</summary>
public sealed class AfoMenuLineTurn : MonoBehaviour
{
    public RectTransform canvasRect;
    public RectTransform horizontal;
    public RectTransform vertical;
    public RectTransform corner;

    static readonly Color Blue = new Color32(36, 107, 253, 255);
    static readonly Color White = new Color32(244, 247, 255, 255);
    const float HorizontalLength = 64f;
    const float VerticalLength = 34f;
    const float Stroke = 3f;

    Image horizontalImage;
    Image verticalImage;
    Image cornerImage;
    Coroutine drawRoutine;

    void Awake()
    {
        horizontalImage = horizontal ? horizontal.GetComponent<Image>() : null;
        verticalImage = vertical ? vertical.GetComponent<Image>() : null;
        cornerImage = corner ? corner.GetComponent<Image>() : null;
        Hide();
    }

    public void SetSelected(RectTransform item, bool instant)
    {
        if (!canvasRect || !horizontal || !vertical || !corner || !item) return;
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect, item);
        Vector2 turn = new Vector2(bounds.min.x - 8f, bounds.max.y + 5f);
        if (drawRoutine != null) StopCoroutine(drawRoutine);
        if (instant || MenuButton.ReducedMotion)
        {
            Draw(turn, 1f);
            return;
        }
        drawRoutine = StartCoroutine(DrawTurn(turn));
    }

    IEnumerator DrawTurn(Vector2 turn)
    {
        SetPosition(turn);
        SetColors(0f);
        horizontal.sizeDelta = new Vector2(0f, Stroke);
        vertical.sizeDelta = new Vector2(Stroke, 0f);
        const float duration = 0.14f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            horizontal.sizeDelta = new Vector2(HorizontalLength * t, Stroke);
            vertical.sizeDelta = new Vector2(Stroke, VerticalLength * t);
            SetColors(t);
            yield return null;
        }
        Draw(turn, 1f);
        drawRoutine = null;
    }

    void Draw(Vector2 turn, float alpha)
    {
        SetPosition(turn);
        horizontal.sizeDelta = new Vector2(HorizontalLength, Stroke);
        vertical.sizeDelta = new Vector2(Stroke, VerticalLength);
        SetColors(alpha);
    }

    void SetPosition(Vector2 turn)
    {
        horizontal.anchoredPosition = turn;
        vertical.anchoredPosition = turn;
        corner.anchoredPosition = turn + new Vector2(0f, -1f);
    }

    void SetColors(float alpha)
    {
        SetAlpha(horizontalImage, Blue, alpha * 0.92f);
        SetAlpha(verticalImage, Blue, alpha * 0.92f);
        SetAlpha(cornerImage, White, alpha);
    }

    void Hide()
    {
        if (horizontal) horizontal.sizeDelta = Vector2.zero;
        if (vertical) vertical.sizeDelta = Vector2.zero;
        SetColors(0f);
    }

    static void SetAlpha(Image image, Color color, float alpha)
    {
        if (!image) return;
        color.a = alpha;
        image.color = color;
    }
}
