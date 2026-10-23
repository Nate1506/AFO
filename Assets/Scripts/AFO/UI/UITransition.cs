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

    public void PlayTransition(string sceneName)
    {
        if (!busy) StartCoroutine(SceneTransition(sceneName));
    }

    public void PlayQuick(Action midpoint)
    {
        if (!busy) StartCoroutine(QuickTransition(midpoint));
    }

    IEnumerator SceneTransition(string sceneName)
    {
        busy = true;
        yield return WipeIn();
        SceneManager.LoadScene(sceneName);
    }

    IEnumerator QuickTransition(Action midpoint)
    {
        busy = true;
        yield return WipeIn();
        midpoint?.Invoke();
        yield return new WaitForSecondsRealtime(0.035f);
        yield return WipeOut();
        busy = false;
    }

    IEnumerator WipeIn()
    {
        if (!overlayGroup || strips == null || strips.Length == 0) yield break;
        float width = overlayGroup.transform is RectTransform root ? root.rect.width : Screen.width;
        overlayGroup.alpha = 1f;
        overlayGroup.blocksRaycasts = true;
        overlayGroup.interactable = false;
        for (int i = 0; i < strips.Length; i++) SetX(strips[i], width + strips[i].rect.width * 0.5f);
        const float duration = 0.31f;
        const float stagger = 0.045f;
        for (int i = 0; i < strips.Length; i++) StartCoroutine(MoveStrip(strips[i], width + strips[i].rect.width * 0.5f, 0f, duration, i * stagger));
        yield return new WaitForSecondsRealtime(duration + stagger * strips.Length + 0.025f);
    }

    IEnumerator WipeOut()
    {
        if (strips == null || strips.Length == 0) yield break;
        float width = overlayGroup && overlayGroup.transform is RectTransform root ? root.rect.width : Screen.width;
        const float duration = 0.27f;
        const float stagger = 0.035f;
        for (int i = 0; i < strips.Length; i++)
            StartCoroutine(MoveStrip(strips[strips.Length - 1 - i], 0f, -width - strips[strips.Length - 1 - i].rect.width * 0.5f, duration, i * stagger));
        yield return new WaitForSecondsRealtime(duration + stagger * strips.Length + 0.025f);
        Hide();
    }

    IEnumerator MoveStrip(RectTransform strip, float from, float to, float duration, float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            SetX(strip, Mathf.Lerp(from, to, t));
            yield return null;
        }
        SetX(strip, to);
    }

    void Hide()
    {
        if (overlayGroup)
        {
            overlayGroup.alpha = 0f;
            overlayGroup.blocksRaycasts = false;
            overlayGroup.interactable = false;
        }
        if (strips == null) return;
        float width = overlayGroup && overlayGroup.transform is RectTransform root ? root.rect.width : Screen.width;
        foreach (var strip in strips) if (strip) SetX(strip, -width - strip.rect.width);
    }

    static void SetX(RectTransform rect, float x)
    {
        if (!rect) return;
        var position = rect.anchoredPosition;
        position.x = x;
        rect.anchoredPosition = position;
    }
}
