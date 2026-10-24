using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Shared visual state and motion for the solid AFO interface controls.</summary>
[RequireComponent(typeof(RectTransform))]
public sealed class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, ISelectHandler, IDeselectHandler
{
    static readonly Color Navy = new Color32(11, 29, 54, 255);
    static readonly Color NavyHover = new Color32(14, 43, 78, 255);
    static readonly Color White = new Color32(244, 247, 255, 255);
    static readonly Color Blue = new Color32(36, 107, 253, 255);
    static readonly Color BlueHover = new Color32(59, 130, 255, 255);
    static readonly Color Ink = new Color32(5, 7, 11, 255);

    public static bool ReducedMotion { get; set; }
    public Image background;
    public Image accent;
    public Image shadow;
    public Text label;
    public Text icon;
    public Text actionHint;
    public Image impactFlash;
    public bool initiallySelected;
    public bool textLed;
    public bool spotlight;
    [Tooltip("Use a menu-owned line-turn indicator instead of the shared punch and slide feedback.")]
    public bool lineTurnMode;
    public float selectionOffset = 14f;
    [Range(-4f, 4f)] public float hoverRotationDegrees = 1.2f;
    public RectTransform pressSpinTarget;
    public Action OnInteracted;

    RectTransform rect;
    RectTransform labelRect;
    RectTransform iconRect;
    RectTransform accentRect;
    RectTransform shadowRect;
    RectTransform flashRect;
    Vector2 home;
    Vector2 labelHome;
    Vector2 iconHome;
    Vector2 shadowHome;
    Vector2 accentHomeSize;
    Vector3 scaleHome;
    float homeAngle;
    bool selected;
    bool hovered;
    bool focused;
    Coroutine moveRoutine;
    Coroutine tintRoutine;
    Coroutine pulseRoutine;
    Coroutine flashRoutine;
    Coroutine spinRoutine;

    bool IsActive => lineTurnMode ? selected || hovered : selected || hovered || focused;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        home = rect.anchoredPosition;
        scaleHome = rect.localScale;
        homeAngle = rect.localEulerAngles.z;
        if (label) { labelRect = label.rectTransform; labelHome = labelRect.anchoredPosition; }
        if (icon) { iconRect = icon.rectTransform; iconHome = iconRect.anchoredPosition; }
        if (accent) { accentRect = accent.rectTransform; accentHomeSize = accentRect.sizeDelta; }
        if (shadow) { shadowRect = shadow.rectTransform; shadowHome = shadowRect.anchoredPosition; }
        if (impactFlash) flashRect = impactFlash.rectTransform;
        selected = initiallySelected;
        Paint(true);
    }

    public void SetSelected(bool value, bool instant = false)
    {
        selected = value;
        Paint(lineTurnMode ? instant : instant || IsActive);
        Move(IsActive ? home + Vector2.right * selectionOffset : home, hovered ? hoverRotationDegrees : 0f);
    }

    public void Pulse()
    {
        if (lineTurnMode) return;
        if (!rect) rect = GetComponent<RectTransform>();
        if (ReducedMotion) return;
        if (pulseRoutine != null) StopCoroutine(pulseRoutine);
        pulseRoutine = StartCoroutine(Punch());
        if (impactFlash)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashImpact());
        }
    }

    public void PlayPressSpin()
    {
        if (!lineTurnMode || !pressSpinTarget) return;
        if (spinRoutine != null) StopCoroutine(spinRoutine);
        spinRoutine = StartCoroutine(SpinPressGraphic());
    }

    IEnumerator SpinPressGraphic()
    {
        Quaternion homeRotation = pressSpinTarget.localRotation;
        Color homeAccent = accent ? accent.color : Blue;
        const float duration = 0.18f;
        const float turnDegrees = 140f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float angle = t < 0.58f
                ? Mathf.Lerp(0f, turnDegrees, Smooth(t / 0.58f))
                : Mathf.Lerp(turnDegrees, 0f, Smooth((t - 0.58f) / 0.42f));
            pressSpinTarget.localRotation = homeRotation * Quaternion.Euler(0f, 0f, angle);
            if (accent) accent.color = Color.Lerp(homeAccent, BlueHover, Mathf.Sin(Mathf.PI * t));
            yield return null;
        }
        pressSpinTarget.localRotation = homeRotation;
        if (accent) accent.color = homeAccent;
        spinRoutine = null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        OnInteracted?.Invoke();
        Paint(lineTurnMode ? false : IsActive);
        Move(home + Vector2.right * selectionOffset, hoverRotationDegrees);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        Paint(lineTurnMode ? false : IsActive);
        Move(IsActive ? home + Vector2.right * selectionOffset : home, 0f);
    }

    public void OnPointerDown(PointerEventData eventData) => Pulse();

    public void OnSelect(BaseEventData eventData)
    {
        focused = true;
        OnInteracted?.Invoke();
        Paint(lineTurnMode ? false : true);
        Move(home + Vector2.right * selectionOffset, 0f);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        focused = false;
        Paint(lineTurnMode ? false : IsActive);
        Move(IsActive ? home + Vector2.right * selectionOffset : home, hovered ? hoverRotationDegrees : 0f);
    }

    void Paint(bool instant)
    {
        bool active = IsActive;
        Color fill = lineTurnMode
            ? (spotlight ? (active ? BlueHover : Blue) : (active ? NavyHover : Navy))
            : textLed ? (active ? Blue : new Color32(36, 107, 253, 0)) :
            spotlight ? (active ? Blue : Navy) : selected || focused ? White : (hovered ? NavyHover : Navy);
        Color ink = lineTurnMode ? White : textLed ? (active ? White : new Color32(244, 247, 255, 190)) : spotlight ? White : selected || focused ? Ink : White;
        Color edge = lineTurnMode
            ? (active ? BlueHover : new Color32(36, 107, 253, 145))
            : IsActive ? Blue : new Color32(36, 107, 253, 150);
        // The main-menu mode keeps text and geometry still, but uses the same
        // short tint transition for its independent hover/selection feedback.
        if (instant || ReducedMotion)
        {
            if (background) background.color = fill;
            if (accent) accent.color = edge;
            if (label) label.color = ink;
            if (icon) icon.color = textLed && !active ? Blue : White;
            if (actionHint) actionHint.color = active ? (spotlight ? Ink : Blue) : new Color32(36, 107, 253, 0);
            return;
        }
        if (tintRoutine != null) StopCoroutine(tintRoutine);
        tintRoutine = StartCoroutine(Tint(fill, ink, edge));
    }

    void Move(Vector2 target, float rotationOffset)
    {
        if (!rect) rect = GetComponent<RectTransform>();
        if (lineTurnMode)
        {
            if (moveRoutine != null) StopCoroutine(moveRoutine);
            rect.anchoredPosition = home;
            rect.localRotation = Quaternion.Euler(0f, 0f, homeAngle);
            moveRoutine = null;
            return;
        }
        if (ReducedMotion)
        {
            rect.anchoredPosition = target;
            rect.localRotation = Quaternion.Euler(0f, 0f, homeAngle);
            return;
        }
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(Slide(target, rotationOffset));
    }

    IEnumerator Slide(Vector2 target, float rotationOffset)
    {
        Vector2 start = rect.anchoredPosition;
        float startAngle = rect.localEulerAngles.z;
        if (startAngle > 180f) startAngle -= 360f;
        float targetAngle = homeAngle + rotationOffset;
        Vector2 labelTarget = labelHome + (IsActive ? Vector2.right * 5f : Vector2.zero);
        Vector2 iconTarget = iconHome + (IsActive ? Vector2.left * 2f : Vector2.zero);
        Vector2 shadowTarget = shadowHome + (IsActive ? Vector2.right * 13f : Vector2.zero);
        Vector2 accentTargetSize = accentHomeSize + (IsActive ? Vector2.right * 5f : Vector2.zero);
        Vector2 labelStart = labelRect ? labelRect.anchoredPosition : labelHome;
        Vector2 iconStart = iconRect ? iconRect.anchoredPosition : iconHome;
        Vector2 shadowStart = shadowRect ? shadowRect.anchoredPosition : shadowHome;
        Vector2 accentStartSize = accentRect ? accentRect.sizeDelta : accentHomeSize;
        const float duration = 0.16f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutBack(t);
            rect.anchoredPosition = Vector2.LerpUnclamped(start, target, eased);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(startAngle, targetAngle, eased));
            if (labelRect) labelRect.anchoredPosition = Vector2.LerpUnclamped(labelStart, labelTarget, eased);
            if (iconRect) iconRect.anchoredPosition = Vector2.LerpUnclamped(iconStart, iconTarget, eased);
            if (shadowRect) shadowRect.anchoredPosition = Vector2.LerpUnclamped(shadowStart, shadowTarget, eased);
            if (accentRect) accentRect.sizeDelta = Vector2.LerpUnclamped(accentStartSize, accentTargetSize, eased);
            yield return null;
        }
        rect.anchoredPosition = target;
        rect.localRotation = Quaternion.Euler(0f, 0f, targetAngle);
        if (labelRect) labelRect.anchoredPosition = labelTarget;
        if (iconRect) iconRect.anchoredPosition = iconTarget;
        if (shadowRect) shadowRect.anchoredPosition = shadowTarget;
        if (accentRect) accentRect.sizeDelta = accentTargetSize;
        moveRoutine = null;
    }

    static float EaseOutBack(float t)
    {
        const float overshoot = 1.2f;
        float x = t - 1f;
        return 1f + (overshoot + 1f) * x * x * x + overshoot * x * x;
    }

    static float Smooth(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    IEnumerator Tint(Color fill, Color ink, Color edge)
    {
        Color fromFill = background ? background.color : fill;
        Color fromLabel = label ? label.color : ink;
        Color fromIcon = icon ? icon.color : ink;
        Color fromAccent = accent ? accent.color : edge;
        Color fromHint = actionHint ? actionHint.color : Color.clear;
        const float duration = 0.17f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (background) background.color = Color.Lerp(fromFill, fill, t);
            if (label) label.color = Color.Lerp(fromLabel, ink, t);
            if (icon) icon.color = Color.Lerp(fromIcon, textLed && !IsActive ? Blue : White, t);
            if (accent) accent.color = Color.Lerp(fromAccent, edge, t);
            if (actionHint) actionHint.color = Color.Lerp(fromHint, IsActive ? (spotlight ? Ink : Blue) : new Color32(36, 107, 253, 0), t);
            yield return null;
        }
        if (background) background.color = fill;
        if (label) label.color = ink;
        if (icon) icon.color = textLed && !IsActive ? Blue : White;
        if (accent) accent.color = edge;
        if (actionHint) actionHint.color = IsActive ? (spotlight ? Ink : Blue) : new Color32(36, 107, 253, 0);
        tintRoutine = null;
    }

    IEnumerator Punch()
    {
        const float duration = 0.065f;
        Vector3 pressed = Vector3.Scale(scaleHome, new Vector3(0.975f, 0.92f, 1f));
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            rect.localScale = Vector3.Lerp(scaleHome, pressed, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            rect.localScale = Vector3.Lerp(pressed, scaleHome, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        rect.localScale = scaleHome;
        pulseRoutine = null;
    }

    IEnumerator FlashImpact()
    {
        Color tint = impactFlash.color;
        tint.a = 0.72f;
        impactFlash.color = tint;
        Vector3 startScale = new Vector3(0.3f, 1f, 1f);
        Vector3 endScale = new Vector3(1.35f, 1f, 1f);
        flashRect.localScale = startScale;
        const float duration = 0.12f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            flashRect.localScale = Vector3.Lerp(startScale, endScale, t);
            tint.a = Mathf.Lerp(0.72f, 0f, t);
            impactFlash.color = tint;
            yield return null;
        }
        tint.a = 0f;
        impactFlash.color = tint;
        flashRect.localScale = Vector3.one;
        flashRoutine = null;
    }
}
