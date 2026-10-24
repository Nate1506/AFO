using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reveals one screen-wide guide line from the side of the previous selection
/// whenever the selected main-menu item changes.
/// </summary>
public sealed class AfoMenuLineTurn : MonoBehaviour
{
    public RectTransform canvasRect;
    // Existing scene references are reused: horizontal is the resting track,
    // vertical is the width-reveal graphic, and corner is a fixed end marker.
    public RectTransform horizontal;
    public RectTransform vertical;
    public RectTransform corner;

    static readonly Color Blue = new Color32(36, 107, 253, 255);
    static readonly Color White = new Color32(244, 247, 255, 255);
    const float Stroke = 3f;
    const float CornerSize = 6f;
    const float LeftScreenFraction = 70f / 1920f;
    const float RightScreenFraction = 1810f / 1920f;
    const float LineYScreenFraction = 402f / 1080f;
    const float SweepDuration = 0.32f;

    Image trackImage;
    Image sweepImage;
    Image cornerImage;
    Coroutine drawRoutine;
    int lastSelectionIndex = -1;
    float lastSelectionScreenX;
    bool hasSelection;
    float cachedCanvasWidth = -1f;
    float cachedCanvasHeight = -1f;
    float lineStartX;
    float lineEndX;
    float lineY;

    void Awake()
    {
        CacheImages();
        RefreshLayout();
        RestoreRestingLine();
    }

    void Update()
    {
        if (!canvasRect) return;
        Rect canvasBounds = canvasRect.rect;
        if (!Mathf.Approximately(canvasBounds.width, cachedCanvasWidth) ||
            !Mathf.Approximately(canvasBounds.height, cachedCanvasHeight))
            RefreshLayout();
    }

    void CacheImages()
    {
        trackImage = horizontal ? horizontal.GetComponent<Image>() : null;
        sweepImage = vertical ? vertical.GetComponent<Image>() : null;
        cornerImage = corner ? corner.GetComponent<Image>() : null;
        if (trackImage) trackImage.raycastTarget = false;
        if (sweepImage) sweepImage.raycastTarget = false;
        if (cornerImage) cornerImage.raycastTarget = false;
    }

    /// <summary>Lay out the line in reference-resolution proportions; called by the scene builder too.</summary>
    public void RefreshLayout()
    {
        if (!canvasRect || !horizontal || !vertical || !corner) return;
        if (drawRoutine != null)
        {
            StopCoroutine(drawRoutine);
            drawRoutine = null;
        }

        CacheImages();
        Rect canvasBounds = canvasRect.rect;
        float width = Mathf.Max(1f, canvasBounds.width);
        float height = Mathf.Max(1f, canvasBounds.height);
        cachedCanvasWidth = width;
        cachedCanvasHeight = height;

        // These fractions correspond to about x=70..1810 at 1920x1080.
        lineStartX = -width * 0.5f + width * LeftScreenFraction;
        lineEndX = -width * 0.5f + width * RightScreenFraction;
        lineY = height * (0.5f - LineYScreenFraction);
        float lineWidth = Mathf.Max(1f, lineEndX - lineStartX);

        ConfigureRect(horizontal, new Vector2(0f, 0.5f), new Vector2(lineStartX, lineY), new Vector2(lineWidth, Stroke));
        ConfigureRect(vertical, new Vector2(0f, 0.5f), new Vector2(lineStartX, lineY), new Vector2(0f, Stroke));
        ConfigureRect(corner, new Vector2(0.5f, 0.5f), new Vector2(lineEndX, lineY), new Vector2(CornerSize, CornerSize));
        RestoreRestingLine();
    }

    public void SetSelected(RectTransform item, int selectionIndex, bool instant)
    {
        if (!canvasRect || !horizontal || !vertical || !corner || !item) return;
        if (!trackImage || !sweepImage || !cornerImage) CacheImages();

        Camera uiCamera = canvasRect.GetComponentInParent<Canvas>()?.rootCanvas.worldCamera;
        float selectedScreenX = RectTransformUtility.WorldToScreenPoint(uiCamera, item.position).x;
        if (hasSelection && selectionIndex == lastSelectionIndex &&
            Mathf.Abs(selectedScreenX - lastSelectionScreenX) < 1f)
            return;

        int indexDelta = hasSelection ? selectionIndex - lastSelectionIndex : 0;
        float screenDelta = hasSelection ? selectedScreenX - lastSelectionScreenX : 0f;
        int indexDirection = indexDelta != 0 ? (indexDelta > 0 ? 1 : -1) : 0;
        // Follow the actual on-screen button positions. Menu order is only a
        // fallback when the two selected items share the same horizontal axis.
        int direction = Mathf.Abs(screenDelta) > 1f
            ? (screenDelta > 0f ? 1 : -1)
            : indexDirection;

        lastSelectionIndex = selectionIndex;
        lastSelectionScreenX = selectedScreenX;
        hasSelection = true;

        if (drawRoutine != null)
        {
            StopCoroutine(drawRoutine);
            drawRoutine = null;
        }

        if (instant || direction == 0 || MenuButton.ReducedMotion)
        {
            RestoreRestingLine();
            return;
        }

        drawRoutine = StartCoroutine(DrawSweep(direction));
    }

    IEnumerator DrawSweep(int direction)
    {
        float from = direction > 0 ? lineStartX : lineEndX;
        float to = direction > 0 ? lineEndX : lineStartX;
        vertical.pivot = new Vector2(direction > 0 ? 0f : 1f, 0.5f);
        vertical.anchoredPosition = new Vector2(from, lineY);
        vertical.sizeDelta = new Vector2(0f, Stroke);
        SetAlpha(trackImage, Blue, 0f);
        SetAlpha(sweepImage, Blue, 0.96f);

        float elapsed = 0f;
        while (elapsed < SweepDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / SweepDuration);
            float eased = progress * progress * (3f - 2f * progress);
            float visibleLength = Mathf.Abs(Mathf.Lerp(from, to, eased) - from);
            vertical.sizeDelta = new Vector2(visibleLength, Stroke);
            yield return null;
        }

        RestoreRestingLine();
        drawRoutine = null;
    }

    void RestoreRestingLine()
    {
        if (vertical)
        {
            vertical.sizeDelta = new Vector2(0f, Stroke);
            vertical.anchoredPosition = new Vector2(lineStartX, lineY);
        }
        SetAlpha(sweepImage, Blue, 0f);
        SetAlpha(trackImage, Blue, 0.36f);
        if (corner) corner.anchoredPosition = new Vector2(lineEndX, lineY);
        SetAlpha(cornerImage, White, 0.9f);
    }

    static void ConfigureRect(RectTransform rect, Vector2 pivot, Vector2 position, Vector2 size)
    {
        if (!rect) return;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static void SetAlpha(Image image, Color color, float alpha)
    {
        if (!image) return;
        color.a = alpha;
        image.color = color;
    }
}
