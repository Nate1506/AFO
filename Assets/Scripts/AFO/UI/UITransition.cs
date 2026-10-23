using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Layered rectangular wipe used for menu panel and scene changes.</summary>
public sealed class UITransition : MonoBehaviour
{
    public static UITransition Instance { get; private set; }
    public CanvasGroup overlayGroup;
    public RectTransform[] strips;
    public Image[] stripImages;
    public bool IsBusy => busy;

    bool busy;

    void Awake()
    {
        Instance = this;
        Hide();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool PlayTransition(string sceneName)
    {
        if (busy || !overlayGroup || strips == null || strips.Length == 0) return false;
        StartCoroutine(SceneTransition(sceneName));
        return true;
    }

    public bool PlayQuick(Action midpoint)
    {
        if (busy || !overlayGroup || strips == null || strips.Length == 0) return false;
        StartCoroutine(QuickTransition(midpoint));
        return true;
    }

    IEnumerator SceneTransition(string sceneName)
    {
        busy = true;
        yield return Wipe(true);
        SceneManager.LoadScene(sceneName);
    }

    IEnumerator QuickTransition(Action midpoint)
    {
        busy = true;
        yield return Wipe(true);
        try { midpoint?.Invoke(); }
        catch (Exception exception) { Debug.LogException(exception); }
        if (!MenuButton.ReducedMotion) yield return new WaitForSecondsRealtime(0.035f);
        yield return Wipe(false);
        busy = false;
        Hide();
    }

    IEnumerator Wipe(bool entering)
    {
        float width = ((RectTransform)overlayGroup.transform).rect.width;
        float height = ((RectTransform)overlayGroup.transform).rect.height;
        float panelWidth = Mathf.Sqrt(width * width + height * height) + 180f;
        float panelHeight = height + 60f;
        overlayGroup.alpha = 1f;
        overlayGroup.blocksRaycasts = true;
        overlayGroup.interactable = false;

        const float duration = 0.22f;
        float stagger = entering ? 0.035f : 0.03f;
        float total = duration + stagger * Mathf.Max(0, strips.Length - 1);
        for (int i = 0; i < strips.Length; i++)
        {
            var strip = strips[i];
            if (!strip) continue;
            strip.sizeDelta = new Vector2(panelWidth, panelHeight);
            float halfWidth = strip.rect.width * 0.5f;
            SetX(strip, MenuButton.ReducedMotion ? (entering ? 0f : -width - halfWidth) : entering ? width + halfWidth : -width - halfWidth);
        }

        if (MenuButton.ReducedMotion)
        {
            if (entering)
                foreach (var strip in strips) if (strip) SetX(strip, 0f);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < total)
        {
            for (int i = 0; i < strips.Length; i++)
            {
                var strip = strips[i];
                if (!strip) continue;
                float localTime = Mathf.Clamp01((elapsed - i * stagger) / duration);
                float eased = localTime * localTime * (3f - 2f * localTime);
                float halfWidth = strip.rect.width * 0.5f;
                float from = entering ? width + halfWidth : 0f;
                float to = entering ? 0f : -width - halfWidth;
                float back = Mathf.Clamp01(localTime);
                float x = Mathf.Lerp(from, to, eased);
                // A tiny directional overshoot gives the panels a fast snap without exposing the edges.
                x += (entering ? -1f : 1f) * Mathf.Sin(back * Mathf.PI) * 16f;
                SetX(strip, x);
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        foreach (var strip in strips) if (strip) SetX(strip, entering ? 0f : -width - strip.rect.width * 0.5f);
    }

    void Hide()
    {
        if (overlayGroup)
        {
            overlayGroup.alpha = 0f;
            overlayGroup.blocksRaycasts = false;
            overlayGroup.interactable = false;
        }
        if (strips == null || !overlayGroup) return;
        float width = ((RectTransform)overlayGroup.transform).rect.width;
        float height = ((RectTransform)overlayGroup.transform).rect.height;
        float panelWidth = Mathf.Sqrt(width * width + height * height) + 180f;
        foreach (var strip in strips) if (strip)
        {
            strip.sizeDelta = new Vector2(panelWidth, height + 60f);
            SetX(strip, -width - strip.rect.width * 0.5f);
        }
    }

    static void SetX(RectTransform rect, float x)
    {
        if (!rect) return;
        var position = rect.anchoredPosition;
        position.x = x;
        rect.anchoredPosition = position;
    }
}
