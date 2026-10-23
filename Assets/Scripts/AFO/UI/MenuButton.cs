using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Shared visual state and motion for the solid menu blocks.</summary>
[RequireComponent(typeof(RectTransform))]
public sealed class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    static readonly Color Navy = new Color32(11, 29, 54, 255);
    static readonly Color NavyHover = new Color32(14, 43, 78, 255);
    static readonly Color White = new Color32(244, 247, 255, 255);
    static readonly Color Blue = new Color32(36, 107, 253, 255);
    static readonly Color Ink = new Color32(5, 7, 11, 255);

    public static bool ReducedMotion { get; set; }
    public Image background;
    public Image accent;
    public Image shadow;
    public Text label;
    public Text icon;
    public Action OnHovered;

    RectTransform rect;
    Vector2 home;
    bool selected;
    bool hovered;
    Coroutine moveRoutine;
    Coroutine tintRoutine;
    Coroutine pulseRoutine;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        home = rect.anchoredPosition;
        Paint(false, true);
    }

    public void SetSelected(bool value, bool instant = false)
    {
        selected = value;
        Paint(instant, false);
        Move(hovered || selected ? home + Vector2.right * 14f : home);
    }

    public void Pulse()
    {
        if (!rect) rect = GetComponent<RectTransform>();
        if (ReducedMotion) return;
        if (pulseRoutine != null) StopCoroutine(pulseRoutine);
        pulseRoutine = StartCoroutine(Punch());
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        OnHovered?.Invoke();
        Paint(false, false);
        Move(home + Vector2.right * 14f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        Paint(false, false);
        Move(selected ? home + Vector2.right * 14f : home);
    }

    void Paint(bool instant, bool initial)
    {
        Color fill = selected ? White : (hovered ? NavyHover : Navy);
        Color ink = selected ? Ink : White;
        Color edge = selected || hovered ? Blue : new Color32(36, 107, 253, 150);
        if (initial || instant || ReducedMotion)
        {
            if (background) background.color = fill;
            if (accent) accent.color = edge;
            if (label) label.color = ink;
            if (icon) icon.color = selected ? Blue : White;
            return;
        }
        if (tintRoutine != null) StopCoroutine(tintRoutine);
        tintRoutine = StartCoroutine(Tint(fill, ink, edge));
    }

    void Move(Vector2 target)
    {
        if (!rect) rect = GetComponent<RectTransform>();
        if (ReducedMotion) { rect.anchoredPosition = target; return; }
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(Slide(target));
    }

    IEnumerator Slide(Vector2 target)
    {
        Vector2 start = rect.anchoredPosition;
        const float duration = 0.19f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rect.anchoredPosition = Vector2.Lerp(start, target, 1f - (1f - t) * (1f - t));
            yield return null;
        }
        rect.anchoredPosition = target;
        moveRoutine = null;
    }

    IEnumerator Tint(Color fill, Color ink, Color edge)
    {
        Color fromFill = background ? background.color : fill;
        Color fromLabel = label ? label.color : ink;
        Color fromIcon = icon ? icon.color : ink;
        Color fromAccent = accent ? accent.color : edge;
        const float duration = 0.17f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (background) background.color = Color.Lerp(fromFill, fill, t);
            if (label) label.color = Color.Lerp(fromLabel, ink, t);
            if (icon) icon.color = Color.Lerp(fromIcon, selected ? Blue : ink, t);
            if (accent) accent.color = Color.Lerp(fromAccent, edge, t);
            yield return null;
        }
        if (background) background.color = fill;
        if (label) label.color = ink;
        if (icon) icon.color = selected ? Blue : ink;
        if (accent) accent.color = edge;
        tintRoutine = null;
    }

    IEnumerator Punch()
    {
        const float duration = 0.065f;
        Vector3 from = rect.localScale;
        Vector3 pressed = new Vector3(0.975f, 0.94f, 1f);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            rect.localScale = Vector3.Lerp(from, pressed, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            rect.localScale = Vector3.Lerp(pressed, Vector3.one, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        rect.localScale = Vector3.one;
        pulseRoutine = null;
    }
}
