using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Controls the main menu UI for the "Against the Current" game.
/// Manages button hover effects, animations, and scene transitions.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Menu Buttons")]
    public Button playButton;
    public Button settingsButton;
    public Button creditsButton;
    public Button quitButton;

    [Header("Scene Names")]
    public string playSceneName = "Nate";

    [Header("Animation Settings")]
    public float buttonHoverScale = 1.05f;
    public float animationDuration = 0.15f;
    public float fadeInDuration = 1.2f;

    [Header("UI References")]
    public CanvasGroup mainCanvasGroup;
    public RectTransform logoRect;

    private void Start()
    {
        SetupButtonListeners();
        StartCoroutine(FadeIn());
        if (logoRect != null)
            StartCoroutine(LogoFloatAnimation());
    }

    private void SetupButtonListeners()
    {
        if (playButton != null)     { playButton.onClick.AddListener(OnPlayClicked);     AddHoverEffect(playButton); }
        if (settingsButton != null) { settingsButton.onClick.AddListener(OnSettingsClicked); AddHoverEffect(settingsButton); }
        if (creditsButton != null)  { creditsButton.onClick.AddListener(OnCreditsClicked);  AddHoverEffect(creditsButton); }
        if (quitButton != null)     { quitButton.onClick.AddListener(OnQuitClicked);     AddHoverEffect(quitButton); }
    }

    private void AddHoverEffect(Button button)
    {
        var trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();

        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ => StartCoroutine(ScaleButton(button.transform, buttonHoverScale)));
        trigger.triggers.Add(enterEntry);

        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(_ => StartCoroutine(ScaleButton(button.transform, 1f)));
        trigger.triggers.Add(exitEntry);
    }

    private IEnumerator ScaleButton(Transform t, float targetScale)
    {
        Vector3 startScale = t.localScale;
        Vector3 endScale   = Vector3.one * targetScale;
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.Lerp(startScale, endScale, elapsed / animationDuration);
            yield return null;
        }
        t.localScale = endScale;
    }

    private IEnumerator FadeIn()
    {
        if (mainCanvasGroup == null) yield break;
        mainCanvasGroup.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            mainCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        mainCanvasGroup.alpha = 1f;
    }

    private IEnumerator LogoFloatAnimation()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * 0.8f;
            var pos = logoRect.anchoredPosition;
            logoRect.anchoredPosition = new Vector2(pos.x, Mathf.Sin(t) * 6f);
            yield return null;
        }
    }

    public void OnPlayClicked()     => StartCoroutine(TransitionToScene(playSceneName));
    public void OnSettingsClicked() => Debug.Log("[MainMenu] Settings – panel not yet implemented.");
    public void OnCreditsClicked()  => Debug.Log("[MainMenu] Credits – panel not yet implemented.");

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator TransitionToScene(string sceneName)
    {
        if (mainCanvasGroup != null)
        {
            float e = 0f;
            while (e < 0.5f)
            {
                e += Time.deltaTime;
                mainCanvasGroup.alpha = Mathf.Clamp01(1f - e / 0.5f);
                yield return null;
            }
        }
        SceneManager.LoadScene(sceneName);
    }
}
