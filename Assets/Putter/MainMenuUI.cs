using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Runtime behavior for the AFO keyboard-and-mouse menu.</summary>
public sealed class MainMenuUI : MonoBehaviour
{
    [Header("Menu")]
    public Button[] menuButtons;
    public MenuButton[] styledButtons;
    public string playSceneName = "Nate";
    public CanvasGroup mainCanvasGroup;
    public UITransition transition;
    public AfoMenuMotion menuMotion;
    public AfoMenuLineTurn lineTurn;

    [Header("Screens")]
    public GameObject settingsPanel;
    public GameObject creditsPanel;
    public GameObject settingsHeaderPanel;
    public GameObject creditsHeaderPanel;
    public Button settingsBackButton;
    public Button creditsBackButton;

    [Header("Settings controls")]
    public Slider masterVolumeSlider;
    public Text volumeValueText;
    public Toggle fullscreenToggle;
    public Toggle reducedMotionToggle;
    public Button qualityButton;
    public Text qualityValueText;
    public Button resolutionButton;
    public Text resolutionValueText;

    int selectedMenuIndex;
    GameObject activePanel;
    bool transitioning;
    bool entering;
    Resolution[] resolutions;
    int resolutionIndex;

    void Start()
    {
        if (menuButtons == null || menuButtons.Length == 0) return;
        if (!lineTurn) lineTurn = GetComponent<AfoMenuLineTurn>();
        if (styledButtons == null) styledButtons = new MenuButton[menuButtons.Length];
        int savedQuality = PlayerPrefs.GetInt("AFO_QualityLevel", -1);
        if (savedQuality >= 0 && savedQuality < QualitySettings.names.Length) QualitySettings.SetQualityLevel(savedQuality, true);
        int savedFullscreen = PlayerPrefs.GetInt("AFO_Fullscreen", -1);
        if (savedFullscreen >= 0) Screen.fullScreen = savedFullscreen != 0;
        if (PlayerPrefs.HasKey("AFO_MasterVolume")) AudioListener.volume = PlayerPrefs.GetFloat("AFO_MasterVolume");
        for (int i = 0; i < menuButtons.Length; i++)
        {
            int index = i;
            if (menuButtons[i]) menuButtons[i].onClick.AddListener(() => ActivateMenuItem(index));
            if (i < styledButtons.Length && styledButtons[i]) styledButtons[i].OnInteracted += () => SelectMenuItem(index, false, false);
        }

        if (settingsBackButton)
        {
            settingsBackButton.onClick.AddListener(() => settingsBackButton.GetComponent<MenuButton>()?.Pulse());
            settingsBackButton.onClick.AddListener(ShowMainMenu);
        }
        if (creditsBackButton)
        {
            creditsBackButton.onClick.AddListener(() => creditsBackButton.GetComponent<MenuButton>()?.Pulse());
            creditsBackButton.onClick.AddListener(ShowMainMenu);
        }
        if (masterVolumeSlider)
        {
            masterVolumeSlider.value = PlayerPrefs.GetFloat("AFO_MasterVolume", AudioListener.volume);
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }
        if (fullscreenToggle)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(_ => fullscreenToggle.GetComponent<MenuButton>()?.Pulse());
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }
        if (reducedMotionToggle)
        {
            reducedMotionToggle.isOn = PlayerPrefs.GetInt("AFO_ReducedMotion", 0) != 0;
            reducedMotionToggle.onValueChanged.AddListener(_ => reducedMotionToggle.GetComponent<MenuButton>()?.Pulse());
            reducedMotionToggle.onValueChanged.AddListener(SetReducedMotion);
        }
        if (qualityButton)
        {
            qualityButton.onClick.AddListener(() => qualityButton.GetComponent<MenuButton>()?.Pulse());
            qualityButton.onClick.AddListener(CycleQuality);
            bool canChangeQuality = QualitySettings.names.Length > 1;
            qualityButton.interactable = canChangeQuality;
            if (!canChangeQuality && qualityButton.TryGetComponent(out MenuButton qualityStyle))
            {
                qualityStyle.enabled = false;
                if (qualityStyle.background) qualityStyle.background.color = new Color32(11, 29, 54, 175);
                if (qualityStyle.accent) qualityStyle.accent.color = new Color32(36, 107, 253, 90);
                if (qualityStyle.label) qualityStyle.label.color = new Color32(244, 247, 255, 145);
            }
        }
        var availableResolutions = Screen.resolutions;
        resolutions = availableResolutions
            .Where(resolution => resolution.width >= 800 && resolution.height >= 600)
            .GroupBy(resolution => new Vector2Int(resolution.width, resolution.height))
            .Select(group => group.First())
            .OrderByDescending(resolution => (long)resolution.width * resolution.height)
            .ThenByDescending(resolution => resolution.width)
            .ToArray();
        if (resolutions.Length == 0)
            resolutions = availableResolutions
                .GroupBy(resolution => new Vector2Int(resolution.width, resolution.height))
                .Select(group => group.First())
                .OrderByDescending(resolution => (long)resolution.width * resolution.height)
                .ThenByDescending(resolution => resolution.width)
                .ToArray();
        resolutionIndex = FindCurrentResolution();
        if (resolutionButton)
        {
            resolutionButton.onClick.AddListener(() => resolutionButton.GetComponent<MenuButton>()?.Pulse());
            resolutionButton.onClick.AddListener(CycleResolution);
        }
        RefreshSettingLabels();

        if (!transition) transition = GetComponent<UITransition>();
        if (!menuMotion) menuMotion = GetComponent<AfoMenuMotion>();
        SetReducedMotion(reducedMotionToggle && reducedMotionToggle.isOn);
        entering = true;
        StartCoroutine(EnterMenu());
    }

    IEnumerator EnterMenu()
    {
        if (menuMotion)
            yield return StartCoroutine(menuMotion.PlayEntrance(MenuButton.ReducedMotion));
        else if (mainCanvasGroup)
            yield return StartCoroutine(FadeCanvas(0f, 1f, 0.32f));

        if (mainCanvasGroup)
        {
            mainCanvasGroup.interactable = true;
            mainCanvasGroup.blocksRaycasts = true;
        }
        entering = false;
        SelectMenuItem(0, true);
    }

    void Update()
    {
        if (transitioning && transition && !transition.IsBusy) transitioning = false;
        if (transitioning || entering || Keyboard.current == null) return;
        var keyboard = Keyboard.current;

        if (activePanel != null)
        {
            if (Pressed(keyboard.escapeKey, keyboard.backspaceKey)) { ShowMainMenu(); return; }
            if (Pressed(keyboard.upArrowKey, keyboard.wKey)) { MovePanelSelection(-1); return; }
            if (Pressed(keyboard.downArrowKey, keyboard.sKey)) { MovePanelSelection(1); return; }
            if (Pressed(keyboard.leftArrowKey, keyboard.aKey)) { AdjustFocused(-1); return; }
            if (Pressed(keyboard.rightArrowKey, keyboard.dKey)) { AdjustFocused(1); return; }
            // Submit is handled once by InputSystemUIInputModule, avoiding duplicate setting changes.
            return;
        }

        if (Pressed(keyboard.upArrowKey, keyboard.wKey, keyboard.leftArrowKey, keyboard.aKey))
            SelectMenuItem(selectedMenuIndex - 1);
        else if (Pressed(keyboard.downArrowKey, keyboard.sKey, keyboard.rightArrowKey, keyboard.dKey))
            SelectMenuItem(selectedMenuIndex + 1);
        // Enter / Space is handled by InputSystemUIInputModule so each action fires once.
    }

    static bool Pressed(params KeyControl[] keys) => keys.Any(key => key != null && key.wasPressedThisFrame);

    void SelectMenuItem(int index, bool instant = false, bool updateEventSystem = true)
    {
        if (menuButtons == null || menuButtons.Length == 0) return;
        selectedMenuIndex = (index % menuButtons.Length + menuButtons.Length) % menuButtons.Length;
        for (int i = 0; i < styledButtons.Length; i++)
            if (styledButtons[i]) styledButtons[i].SetSelected(i == selectedMenuIndex, instant);
        if (lineTurn && selectedMenuIndex < menuButtons.Length && menuButtons[selectedMenuIndex])
            lineTurn.SetSelected(menuButtons[selectedMenuIndex].GetComponent<RectTransform>(), instant);
        if (menuMotion) menuMotion.SetSelection(selectedMenuIndex);
        if (updateEventSystem && EventSystem.current && menuButtons[selectedMenuIndex] &&
            EventSystem.current.currentSelectedGameObject != menuButtons[selectedMenuIndex].gameObject)
            EventSystem.current.SetSelectedGameObject(menuButtons[selectedMenuIndex].gameObject);
    }

    void ActivateMenuItem(int index)
    {
        if (transitioning || menuButtons == null || index < 0 || index >= menuButtons.Length) return;
        SelectMenuItem(index);
        if (index < styledButtons.Length && styledButtons[index]) styledButtons[index].Pulse();
        switch (index)
        {
            case 0: Play(); break;
            case 1: OpenPanel(settingsPanel); break;
            case 2: OpenPanel(creditsPanel); break;
            case 3: Quit(); break;
        }
    }

    void Play()
    {
        if (!Application.CanStreamedLevelBeLoaded(playSceneName))
        {
            Debug.LogError($"[AFO] Play scene '{playSceneName}' is not in Build Settings.");
            return;
        }
        if (menuMotion) menuMotion.PlayConfirm();
        transitioning = true;
        if (!transition || !transition.PlayTransition(playSceneName)) SceneManager.LoadScene(playSceneName);
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OpenPanel(GameObject panel)
    {
        if (!panel || transitioning) return;
        transitioning = true;
        if (!transition || !transition.PlayQuick(() => ShowPanel(panel)))
        {
            ShowPanel(panel);
            transitioning = false;
        }
    }

    void ShowPanel(GameObject panel)
    {
        if (settingsPanel) settingsPanel.SetActive(panel == settingsPanel);
        if (creditsPanel) creditsPanel.SetActive(panel == creditsPanel);
        if (settingsHeaderPanel) settingsHeaderPanel.SetActive(panel == settingsPanel);
        if (creditsHeaderPanel) creditsHeaderPanel.SetActive(panel == creditsPanel);
        var header = panel == settingsPanel ? settingsHeaderPanel : creditsHeaderPanel;
        if (header) header.transform.SetAsLastSibling();
        if (transition) transition.transform.SetAsLastSibling();
        activePanel = panel;
        RefreshPanelGraphics(panel);
        RefreshPanelGraphics(panel == settingsPanel ? settingsHeaderPanel : creditsHeaderPanel);
        Selectable first = panel == settingsPanel ? settingsBackButton : creditsBackButton;
        if (!first) first = panel.GetComponentInChildren<Selectable>();
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(first ? first.gameObject : null);
    }

    static void RefreshPanelGraphics(GameObject panel)
    {
        if (!panel) return;
        // Re-activating an inactive full-screen uGUI panel can leave edge graphics culled
        // until the Canvas rebuilds. Reorder the top rule to invalidate its draw list,
        // then dirty the panel graphics before the transition reveals the screen.
        var topBar = panel.transform.Find("PanelBar");
        if (topBar)
        {
            topBar.SetAsLastSibling();
            topBar.SetAsFirstSibling();
        }
        else
        {
            string[] headerChildren = { "PanelEyebrow", "PanelTitle", "PanelSubtitle", "BackButton" };
            for (int i = headerChildren.Length - 1; i >= 0; i--)
            {
                var element = panel.transform.Find(headerChildren[i]);
                if (element) element.SetAsFirstSibling();
            }
            for (int i = 0; i < headerChildren.Length; i++)
            {
                var element = panel.transform.Find(headerChildren[i]);
                if (element) element.SetAsLastSibling();
            }
        }
        foreach (var graphic in panel.GetComponentsInChildren<Graphic>(true))
            if (graphic) graphic.SetAllDirty();
        Canvas.ForceUpdateCanvases();
    }

    void ShowMainMenu()
    {
        if (transitioning) return;
        transitioning = true;
        if (!transition || !transition.PlayQuick(SetMainMenuActive))
        {
            SetMainMenuActive();
            transitioning = false;
        }
    }

    void SetMainMenuActive()
    {
        if (settingsPanel) settingsPanel.SetActive(false);
        if (creditsPanel) creditsPanel.SetActive(false);
        if (settingsHeaderPanel) settingsHeaderPanel.SetActive(false);
        if (creditsHeaderPanel) creditsHeaderPanel.SetActive(false);
        activePanel = null;
        SelectMenuItem(selectedMenuIndex, true);
    }

    void MovePanelSelection(int direction)
    {
        if (!activePanel) return;
        var header = activePanel == settingsPanel ? settingsHeaderPanel : creditsHeaderPanel;
        var items = activePanel.GetComponentsInChildren<Selectable>()
            .Concat(header ? header.GetComponentsInChildren<Selectable>() : System.Array.Empty<Selectable>())
            .Where(item => item && item.IsActive() && item.IsInteractable())
            .Distinct()
            .ToArray();
        if (items.Length == 0) return;
        var current = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
        int index = System.Array.FindIndex(items, item => item.gameObject == current);
        index = (index + direction + items.Length) % items.Length;
        items[index].Select();
    }

    void AdjustFocused(int direction)
    {
        var current = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
        if (!current) return;
        if (current.TryGetComponent(out Slider slider))
            slider.value = Mathf.Clamp(slider.value + direction * (slider.maxValue - slider.minValue) * 0.05f, slider.minValue, slider.maxValue);
        else if (current.TryGetComponent(out Toggle toggle))
        {
            current.GetComponent<MenuButton>()?.Pulse();
            toggle.isOn = !toggle.isOn;
        }
        else if (current.TryGetComponent(out Button button))
        {
            current.GetComponent<MenuButton>()?.Pulse();
            button.onClick.Invoke();
        }
    }

    void SetMasterVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("AFO_MasterVolume", value);
        if (volumeValueText) volumeValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    static void SetFullscreen(bool value)
    {
        Screen.fullScreen = value;
        PlayerPrefs.SetInt("AFO_Fullscreen", value ? 1 : 0);
    }

    void SetReducedMotion(bool value)
    {
        PlayerPrefs.SetInt("AFO_ReducedMotion", value ? 1 : 0);
        MenuButton.ReducedMotion = value;
        AfoBackdropMotion.ReducedMotion = value;
    }

    void CycleQuality()
    {
        int count = QualitySettings.names.Length;
        if (count == 0) return;
        int index = (QualitySettings.GetQualityLevel() + 1) % count;
        QualitySettings.SetQualityLevel(index, true);
        PlayerPrefs.SetInt("AFO_QualityLevel", index);
        RefreshSettingLabels();
    }

    int FindCurrentResolution()
    {
        if (resolutions == null || resolutions.Length == 0) return -1;
        for (int i = 0; i < resolutions.Length; i++)
            if (resolutions[i].width == Screen.currentResolution.width && resolutions[i].height == Screen.currentResolution.height) return i;
        return resolutions.Length - 1;
    }

    void CycleResolution()
    {
        if (resolutions == null || resolutions.Length == 0) return;
        resolutionIndex = (resolutionIndex + 1) % resolutions.Length;
        var resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode, resolution.refreshRateRatio);
        RefreshSettingLabels();
    }

    void RefreshSettingLabels()
    {
        if (qualityValueText)
        {
            var levels = QualitySettings.names;
            int index = QualitySettings.GetQualityLevel();
            qualityValueText.text = levels.Length > index && index >= 0 ? levels[index].ToUpperInvariant() : "DEFAULT";
        }
        if (resolutionValueText)
        {
            var resolution = resolutionIndex >= 0 && resolutions != null && resolutionIndex < resolutions.Length
                ? resolutions[resolutionIndex] : Screen.currentResolution;
            resolutionValueText.text = $"{resolution.width} × {resolution.height}";
        }
        if (volumeValueText && masterVolumeSlider) volumeValueText.text = Mathf.RoundToInt(masterVolumeSlider.value * 100f) + "%";
    }

    IEnumerator FadeCanvas(float from, float to, float duration)
    {
        mainCanvasGroup.alpha = from;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            mainCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        mainCanvasGroup.alpha = to;
    }
}


