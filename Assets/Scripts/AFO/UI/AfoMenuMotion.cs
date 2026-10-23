using System.Collections;
using UnityEngine;

/// <summary>Coordinates the menu entrance around one quiet diagonal motion axis.</summary>
public sealed class AfoMenuMotion : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public RectTransform title;
    public CanvasGroup titleGroup;
    public RectTransform[] buttons;
    public CanvasGroup[] buttonGroups;
    public AfoBackdropMotion backdrop;

    Vector2 titleHome;
    Quaternion titleRotation;
    Vector2[] buttonHomes;
    Quaternion[] buttonRotations;
    Vector3[] buttonScales;

    void Awake()
    {
        if (title)
        {
            titleHome = title.anchoredPosition;
            titleRotation = title.localRotation;
        }

        int count = buttons == null ? 0 : buttons.Length;
        buttonHomes = new Vector2[count];
        buttonRotations = new Quaternion[count];
        buttonScales = new Vector3[count];
        for (int i = 0; i < count; i++) if (buttons[i])
        {
            buttonHomes[i] = buttons[i].anchoredPosition;
            buttonRotations[i] = buttons[i].localRotation;
            buttonScales[i] = buttons[i].localScale;
        }
    }

    public IEnumerator PlayEntrance(bool reducedMotion)
    {
        RestoreLayout();
        if (reducedMotion)
        {
            SetAlpha(1f);
            if (backdrop) backdrop.SetEntranceProgress(1f);
            yield break;
        }

        SetAlpha(0f);
        if (canvasGroup)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        if (titleGroup) titleGroup.alpha = 0f;
        if (title) title.anchoredPosition = titleHome + new Vector2(-26f, 10f);
        for (int i = 0; buttonGroups != null && i < buttonGroups.Length; i++)
            if (buttonGroups[i]) buttonGroups[i].alpha = 0f;
        for (int i = 0; buttons != null && i < buttons.Length; i++)
            if (buttons[i]) buttons[i].anchoredPosition = buttonHomes[i] + new Vector2(-22f, 7f);
        if (backdrop) backdrop.SetEntranceProgress(0f);

        const float duration = 0.27f;
        const float stagger = 0.045f;
        int buttonCount = buttons == null ? 0 : buttons.Length;
        float total = duration + stagger * Mathf.Max(0, buttonCount - 1);
        float elapsed = 0f;
        while (elapsed < total)
        {
            float rootFade = Smooth(elapsed / 0.20f);
            SetAlpha(rootFade);
            float titleT = Smooth(elapsed / duration);
            SetTitle(titleT);
            if (titleGroup) titleGroup.alpha = titleT;
            for (int i = 0; i < buttonCount; i++)
            {
                float t = Smooth((elapsed - stagger * i) / duration);
                SetButton(i, t);
                if (buttonGroups != null && i < buttonGroups.Length && buttonGroups[i])
                    buttonGroups[i].alpha = t;
            }
            if (backdrop) backdrop.SetEntranceProgress(Smooth((elapsed - 0.025f) / 0.34f));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        RestoreLayout();
        SetAlpha(1f);
        if (titleGroup) titleGroup.alpha = 1f;
        if (buttonGroups != null)
            foreach (var group in buttonGroups) if (group) group.alpha = 1f;
        if (backdrop) backdrop.SetEntranceProgress(1f);
        if (canvasGroup)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    public void SetSelection(int index)
    {
        if (backdrop) backdrop.SetSelection(index);
    }

    public void PlayConfirm()
    {
        if (backdrop) backdrop.SetConfirming(true);
    }

    void RestoreLayout()
    {
        if (title)
        {
            title.anchoredPosition = titleHome;
            title.localRotation = titleRotation;
        }
        if (buttons == null) return;
        for (int i = 0; i < buttons.Length; i++) if (buttons[i])
        {
            buttons[i].anchoredPosition = buttonHomes[i];
            buttons[i].localRotation = buttonRotations[i];
            buttons[i].localScale = buttonScales[i];
        }
    }

    void SetTitle(float t)
    {
        if (title) title.anchoredPosition = Vector2.LerpUnclamped(titleHome + new Vector2(-26f, 10f), titleHome, t);
    }

    void SetButton(int index, float t)
    {
        if (buttons == null || index >= buttons.Length || !buttons[index]) return;
        buttons[index].anchoredPosition = Vector2.LerpUnclamped(buttonHomes[index] + new Vector2(-22f, 7f), buttonHomes[index], t);
        buttons[index].localScale = Vector3.LerpUnclamped(buttonScales[index] * 0.985f, buttonScales[index], t);
    }

    void SetAlpha(float alpha)
    {
        if (canvasGroup) canvasGroup.alpha = alpha;
    }

    static float Smooth(float value)
    {
        float t = Mathf.Clamp01(value);
        return t * t * (3f - 2f * t);
    }
}
