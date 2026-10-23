using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Builds the responsive AFO menu using Unity's built-in uGUI components.</summary>
public static class BuildPutterMenuUI
{
    const string ScenePath = "Assets/Putter/Putter.unity";
    static readonly Color Midnight = Hex("071426");
    static readonly Color Navy = Hex("0B1D36");
    static readonly Color Ink = Hex("05070B");
    static readonly Color Paper = Hex("F4F7FF");
    static readonly Color Blue = Hex("246BFD");
    static readonly Font UiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    [MenuItem("Tools/AFO/Rebuild Main Menu UI")]
    public static void Build()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        foreach (var root in scene.GetRootGameObjects())
            if (root.name == "MainMenuCanvas" || root.name == "EventSystem") Object.DestroyImmediate(root);

        BuildEventSystem(scene);
        var canvasObject = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        SceneManager.MoveGameObjectToScene(canvasObject, scene);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;
        var canvasGroup = canvasObject.GetComponent<CanvasGroup>();

        BuildBackdrop(canvasObject.transform);
        var logo = BuildLogo(canvasObject.transform);
        var buttons = new Button[4];
        var styles = new MenuButton[4];
        string[] names = { "PLAY", "SETTINGS", "CREDITS", "QUIT" };
        string[] icons = { "01", "02", "03", "04" };
        float[] widths = { 510f, 455f, 480f, 410f };
        float[] offsets = { 0f, 30f, 10f, 48f };
        for (int i = 0; i < names.Length; i++)
            buttons[i] = BuildMenuButton(canvasObject.transform, names[i], icons[i], 96f + offsets[i], -390f - i * 101f, widths[i], 82f, out styles[i]);

        BuildFooter(canvasObject.transform);
        var settings = BuildSettingsPanel(canvasObject.transform, out var settingsBack, out var volume, out var volumeText, out var fullscreen, out var reducedMotion, out var qualityButton, out var qualityText, out var resolutionButton, out var resolutionText);
        var credits = BuildCreditsPanel(canvasObject.transform, out var creditsBack);
        settings.SetActive(false);
        credits.SetActive(false);

        var transition = BuildTransition(canvasObject.transform);
        var controller = canvasObject.AddComponent<MainMenuUI>();
        controller.menuButtons = buttons;
        controller.styledButtons = styles;
        controller.playSceneName = "Nate";
        controller.mainCanvasGroup = canvasGroup;
        controller.transition = transition;
        controller.settingsPanel = settings;
        controller.creditsPanel = credits;
        controller.settingsBackButton = settingsBack;
        controller.creditsBackButton = creditsBack;
        controller.masterVolumeSlider = volume;
        controller.volumeValueText = volumeText;
        controller.fullscreenToggle = fullscreen;
        controller.reducedMotionToggle = reducedMotion;
        controller.qualityButton = qualityButton;
        controller.qualityValueText = qualityText;
        controller.resolutionButton = resolutionButton;
        controller.resolutionValueText = resolutionText;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[AFO] Rebuilt keyboard-and-mouse main menu, settings, credits, and transitions.");
    }

    static void BuildEventSystem(Scene scene)
    {
        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        SceneManager.MoveGameObjectToScene(eventSystem, scene);
        var module = eventSystem.GetComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
    }

    static void BuildBackdrop(Transform parent)
    {
        var background = ImageRect(parent, "MidnightBackground", Midnight, new Vector2(0f, 0f), Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero);
        SetRaycast(background, false);
        var leftPanel = ImageRect(parent, "LeftPanel", Navy, new Vector2(0f, 0f), new Vector2(0.55f, 1f), Vector2.zero, Vector2.zero, Vector2.zero);
        SetRaycast(leftPanel, false);
        var rightPanel = ImageRect(parent, "RightPanel", new Color32(5, 12, 25, 255), new Vector2(0.57f, 0f), Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero);
        SetRaycast(rightPanel, false);

        RectTransform[] motion = new RectTransform[4];
        var slashA = ImageRect(parent, "BluePanelA", new Color32(36, 107, 253, 34), new Vector2(0.68f, 0.07f), new Vector2(0.80f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, 150f));
        slashA.localRotation = Quaternion.Euler(0f, 0f, 14f);
        motion[0] = slashA;
        var slashB = ImageRect(parent, "BluePanelB", new Color32(36, 107, 253, 22), new Vector2(0.85f, 0.14f), new Vector2(0.96f, 0.90f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14f, 90f));
        slashB.localRotation = Quaternion.Euler(0f, 0f, 14f);
        motion[1] = slashB;
        var block = ImageRect(parent, "BlueSignal", new Color32(36, 107, 253, 100), new Vector2(0.73f, 0.38f), new Vector2(0.77f, 0.385f), Vector2.zero, Vector2.zero, new Vector2(90f, 5f));
        motion[2] = block;
        var block2 = ImageRect(parent, "BlueSignal2", new Color32(36, 107, 253, 55), new Vector2(0.78f, 0.30f), new Vector2(0.88f, 0.305f), Vector2.zero, Vector2.zero, new Vector2(180f, 4f));
        motion[3] = block2;
        var mot = parent.gameObject.AddComponent<AfoBackdropMotion>();
        mot.shapes = motion;

        // Sparse halftone dots add texture without competing with text.
        for (int i = 0; i < 42; i++)
        {
            int col = i % 7;
            int row = i / 7;
            var dot = ImageRect(parent, "Halftone" + i, new Color32(36, 107, 253, (byte)(18 + (i % 3) * 6)),
                new Vector2(0.79f, 0.20f), new Vector2(0.79f, 0.20f), new Vector2(0.5f, 0.5f),
                new Vector2(col * 18f, row * 18f), new Vector2(3.5f, 3.5f));
            SetRaycast(dot, false);
        }

        MakeText(parent, "WorldIndex", "AFO   /   01", 18, Blue, FontStyle.Bold,
            new Vector2(0.74f, 0.89f), new Vector2(0.74f, 0.89f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(300f, 34f), TextAnchor.MiddleLeft);
        MakeText(parent, "Edition", "AGAINST THE CURRENT", 18, Paper, FontStyle.Bold,
            new Vector2(0.70f, 0.835f), new Vector2(0.70f, 0.835f), new Vector2(0f, 1f), Vector2.zero, new Vector2(380f, 42f), TextAnchor.MiddleLeft);
        MakeText(parent, "MenuSideNote", "CHOOSE YOUR NEXT MOVE", 15, new Color32(244, 247, 255, 130), FontStyle.Normal,
            new Vector2(0.70f, 0.78f), new Vector2(0.70f, 0.78f), new Vector2(0f, 1f), Vector2.zero, new Vector2(340f, 30f), TextAnchor.MiddleLeft);
    }

    static RectTransform BuildLogo(Transform parent)
    {
        var mark = ImageRect(parent, "LogoMark", Blue, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(97f, -86f), new Vector2(12f, 200f));
        SetRaycast(mark, false);
        MakeText(parent, "LogoEyebrow", "AFO   /   A NEW CURRENT", 17, Blue, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(124f, -76f), new Vector2(570f, 36f), TextAnchor.MiddleLeft);
        MakeText(parent, "LogoAgainst", "AGAINST", 69, Paper, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(118f, -105f), new Vector2(710f, 94f), TextAnchor.MiddleLeft);
        MakeText(parent, "LogoTheCurrent", "THE CURRENT", 54, Paper, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(122f, -189f), new Vector2(690f, 78f), TextAnchor.MiddleLeft);
        var underline = ImageRect(parent, "LogoRule", Blue, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(122f, -276f), new Vector2(486f, 3f));
        SetRaycast(underline, false);
        return mark;
    }

    static Button BuildMenuButton(Transform parent, string label, string iconText, float x, float y, float width, float height, out MenuButton style)
    {
        var shadow = ImageRect(parent, label + "Shadow", new Color32(5, 7, 11, 220), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x + 9f, y - 9f), new Vector2(width, height));
        SetRaycast(shadow, false);
        var root = new GameObject("Btn" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(MenuButton));
        root.transform.SetParent(parent, false);
        var rect = root.GetComponent<RectTransform>();
        TopLeft(rect, x, y, width, height);
        var bg = root.GetComponent<Image>();
        bg.color = Navy;
        var button = root.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = bg;
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        var edge = ImageRect(root.transform, "ElectricEdge", new Color32(36, 107, 253, 150), Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(8f, 0f));
        SetRaycast(edge, false);
        MakeText(root.transform, "Index", iconText, 17, Paper, FontStyle.Bold,
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(48f, 0f), TextAnchor.MiddleCenter);
        MakeText(root.transform, "Label", label, 27, Paper, FontStyle.Bold,
            new Vector2(0f, 0f), Vector2.one, new Vector2(0f, 0.5f), new Vector2(90f, 0f), new Vector2(-24f, 0f), TextAnchor.MiddleLeft);
        var tag = MakeText(root.transform, "ActionTag", label == "PLAY" ? "ENTER" : "OPEN", 12, new Color32(244, 247, 255, 125), FontStyle.Bold,
            new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(60f, 0f), TextAnchor.MiddleRight);

        style = root.GetComponent<MenuButton>();
        style.background = bg;
        style.accent = edge.GetComponent<Image>();
        style.shadow = shadow.GetComponent<Image>();
        style.icon = root.transform.Find("Index").GetComponent<Text>();
        style.label = root.transform.Find("Label").GetComponent<Text>();
        return button;
    }

    static void BuildFooter(Transform parent)
    {
        MakeText(parent, "ControlsHint", "W / S   OR   ↑ / ↓   NAVIGATE     ·     ENTER / SPACE   CONFIRM", 15, new Color32(244, 247, 255, 180), FontStyle.Bold,
            new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, new Vector2(98f, 62f), new Vector2(840f, 32f), TextAnchor.MiddleLeft);
        MakeText(parent, "MouseHint", "MOUSE   MOVE / CLICK", 13, new Color32(36, 107, 253, 255), FontStyle.Bold,
            new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, new Vector2(98f, 31f), new Vector2(500f, 24f), TextAnchor.MiddleLeft);
        MakeText(parent, "VersionText", "AFO   /   MENU SYSTEM 01", 13, new Color32(244, 247, 255, 95), FontStyle.Normal,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-54f, 34f), new Vector2(280f, 26f), TextAnchor.MiddleRight);
    }

    static GameObject BuildSettingsPanel(Transform parent, out Button back, out Slider volume, out Text volumeText, out Toggle fullscreen, out Toggle reducedMotion, out Button qualityButton, out Text qualityText, out Button resolutionButton, out Text resolutionText)
    {
        var panel = MakePanelRoot(parent, "SettingsPanel");
        Header(panel.transform, "SYSTEM SETTINGS", "ACTUAL OPTIONS AVAILABLE IN THIS BUILD", out back);
        var content = new GameObject("ResponsiveSettingsContent", typeof(RectTransform), typeof(AfoResponsiveFit));
        content.transform.SetParent(panel.transform, false);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = contentRect.anchorMax = new Vector2(0f, 1f);
        contentRect.pivot = new Vector2(0f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(1920f, 1080f);

        Card(content.transform, "AudioCard", "AUDIO", "MASTER VOLUME", new Vector2(90f, -218f), new Vector2(870f, 112f));
        volume = BuildVolumeSlider(content.transform, new Vector2(600f, -257f));
        volumeText = MakeText(content.transform, "VolumeValue", "100%", 14, Paper, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(840f, -267f), new Vector2(110f, 28f), TextAnchor.MiddleRight);

        Card(content.transform, "GraphicsCard", "GRAPHICS", "QUALITY PRESET", new Vector2(90f, -350f), new Vector2(870f, 112f));
        qualityButton = BuildValueButton(content.transform, "QualityValueButton", new Vector2(600f, -377f), new Vector2(310f, 55f), "QUALITY");
        qualityText = qualityButton.transform.Find("Label").GetComponent<Text>();

        Card(content.transform, "DisplayCard", "DISPLAY", "RESOLUTION  /  FULLSCREEN", new Vector2(90f, -482f), new Vector2(870f, 112f));
        resolutionButton = BuildValueButton(content.transform, "ResolutionValueButton", new Vector2(600f, -510f), new Vector2(310f, 54f), "RESOLUTION");
        resolutionText = resolutionButton.transform.Find("Label").GetComponent<Text>();
        fullscreen = BuildToggle(content.transform, "FullscreenToggle", "FULLSCREEN", new Vector2(385f, -510f), new Vector2(195f, 54f));

        Card(content.transform, "AccessCard", "ACCESSIBILITY", "REDUCED MOTION", new Vector2(90f, -614f), new Vector2(870f, 112f));
        reducedMotion = BuildToggle(content.transform, "ReducedMotionToggle", "REDUCED", new Vector2(670f, -641f), new Vector2(240f, 54f));

        var controlCard = ImageRect(content.transform, "KeyboardMouseCard", new Color32(5, 12, 25, 245), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(1010f, -218f), new Vector2(820f, 508f));
        SetRaycast(controlCard, false);
        var outline = controlCard.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color32(36, 107, 253, 100);
        outline.effectDistance = new Vector2(1f, -1f);
        MakeText(controlCard, "InputHeading", "KEYBOARD  /  MOUSE", 23, Paper, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -28f), new Vector2(730f, 42f), TextAnchor.MiddleLeft);
        var rule = ImageRect(controlCard, "InputRule", Blue, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -83f), new Vector2(745f, 2f));
        SetRaycast(rule, false);
        MakeText(controlCard, "InputRows", "MOUSE MOVE       Highlight option\nLEFT CLICK         Activate option\nW / S   ·   ↑ / ↓    Navigate vertically\nA / D   ·   ← / →    Navigate / adjust\nENTER / SPACE       Confirm selection\nESC / BACKSPACE    Return to menu\nMOUSE WHEEL        Scroll long lists", 20, Paper, FontStyle.Normal,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -115f), new Vector2(740f, 300f), TextAnchor.UpperLeft);
        MakeText(controlCard, "InputFootnote", "Game-specific movement controls are not configured in this build.", 15, new Color32(244, 247, 255, 145), FontStyle.Normal,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(34f, 24f), new Vector2(-58f, 40f), TextAnchor.MiddleLeft);
        return panel;
    }

    static GameObject BuildCreditsPanel(Transform parent, out Button back)
    {
        var panel = MakePanelRoot(parent, "CreditsPanel");
        Header(panel.transform, "CREDITS", "AGAINST THE CURRENT", out back);
        var card = ImageRect(panel.transform, "CreditsBlock", new Color32(5, 12, 25, 245), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(105f, -240f), new Vector2(980f, 400f));
        SetRaycast(card, false);
        var accent = ImageRect(card, "CreditAccent", Blue, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(9f, 0f));
        SetRaycast(accent, false);
        MakeText(card, "CreditTitle", "AFO", 58, Paper, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(46f, -48f), new Vector2(360f, 76f), TextAnchor.MiddleLeft);
        MakeText(card, "CreditGame", "AGAINST THE CURRENT", 27, Blue, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -138f), new Vector2(700f, 52f), TextAnchor.MiddleLeft);
        MakeText(card, "CreditThanks", "THANK YOU FOR PLAYING", 18, new Color32(244, 247, 255, 180), FontStyle.Normal,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -214f), new Vector2(600f, 40f), TextAnchor.MiddleLeft);
        MakeText(parent, "CreditReturnHint", "ESC   /   BACKSPACE     RETURN", 15, new Color32(244, 247, 255, 155), FontStyle.Bold,
            new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, new Vector2(105f, 62f), new Vector2(420f, 30f), TextAnchor.MiddleLeft);
        return panel;
    }

    static GameObject MakePanelRoot(Transform parent, string name)
    {
        var root = ImageRect(parent, name, new Color32(7, 20, 38, 255), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero);
        SetRaycast(root, true);
        var topBar = ImageRect(root, "PanelBar", Blue, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, 5f));
        SetRaycast(topBar, false);
        return root.gameObject;
    }

    static void Header(Transform parent, string title, string subtitle, out Button back)
    {
        MakeText(parent, "PanelEyebrow", "AFO   /   SYSTEM", 16, Blue, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(96f, -42f), new Vector2(450f, 30f), TextAnchor.MiddleLeft);
        MakeText(parent, "PanelTitle", title, 48, Paper, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(94f, -73f), new Vector2(850f, 72f), TextAnchor.MiddleLeft);
        MakeText(parent, "PanelSubtitle", subtitle, 15, new Color32(244, 247, 255, 155), FontStyle.Normal,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(96f, -143f), new Vector2(880f, 30f), TextAnchor.MiddleLeft);
        back = BuildValueButton(parent, "BackButton", new Vector2(0f, -50f), new Vector2(198f, 54f), "←   BACK");
        var rt = back.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-96f, -46f);
    }

    static void Card(Transform parent, string name, string heading, string subheading, Vector2 position, Vector2 size)
    {
        var card = ImageRect(parent, name, Navy, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);
        SetRaycast(card, false);
        var edge = ImageRect(card, "CardEdge", Blue, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(5f, 0f));
        SetRaycast(edge, false);
        MakeText(card, "CardHeading", heading, 14, Blue, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -14f), new Vector2(380f, 26f), TextAnchor.MiddleLeft);
        MakeText(card, "CardSubheading", subheading, 20, Paper, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -50f), new Vector2(470f, 40f), TextAnchor.MiddleLeft);
    }

    static Slider BuildVolumeSlider(Transform parent, Vector2 topLeft)
    {
        var go = new GameObject("MasterVolumeSlider", typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        TopLeft(rect, topLeft.x, topLeft.y, 210f, 32f);
        var background = ImageRect(go.transform, "Background", new Color32(5, 7, 11, 255), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero);
        SetRaycast(background, true);
        var fill = ImageRect(go.transform, "Fill", Blue, Vector2.zero, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero, Vector2.zero);
        SetRaycast(fill, false);
        var handle = ImageRect(go.transform, "Handle", Paper, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18f, 38f));
        SetRaycast(handle, true);
        var slider = go.GetComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = AudioListener.volume;
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.transition = Selectable.Transition.None;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        return slider;
    }

    static Toggle BuildToggle(Transform parent, string name, string label, Vector2 topLeft, Vector2 size)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Toggle));
        root.transform.SetParent(parent, false);
        var rt = root.GetComponent<RectTransform>();
        TopLeft(rt, topLeft.x, topLeft.y, size.x, size.y);
        var image = root.GetComponent<Image>();
        image.color = new Color32(5, 7, 11, 255);
        var indicator = ImageRect(root.transform, "Indicator", Blue, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(7f, 0f));
        SetRaycast(indicator, false);
        var check = ImageRect(root.transform, "Check", Paper, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(23f, 0f), new Vector2(12f, 12f));
        SetRaycast(check, false);
        MakeText(root.transform, "Label", label, 14, Paper, FontStyle.Bold, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), new Vector2(47f, 0f), new Vector2(-8f, 0f), TextAnchor.MiddleLeft);
        var toggle = root.GetComponent<Toggle>();
        toggle.targetGraphic = image;
        toggle.graphic = check.GetComponent<Image>();
        toggle.isOn = false;
        toggle.transition = Selectable.Transition.None;
        toggle.navigation = new Navigation { mode = Navigation.Mode.None };
        return toggle;
    }

    static Button BuildValueButton(Transform parent, string name, Vector2 topLeft, Vector2 size, string label)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        root.transform.SetParent(parent, false);
        var rt = root.GetComponent<RectTransform>();
        TopLeft(rt, topLeft.x, topLeft.y, size.x, size.y);
        var image = root.GetComponent<Image>();
        image.color = new Color32(5, 7, 11, 255);
        var button = root.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.normalColor = new Color32(5, 7, 11, 255);
        colors.highlightedColor = new Color32(14, 43, 78, 255);
        colors.pressedColor = Blue;
        colors.selectedColor = new Color32(14, 43, 78, 255);
        colors.fadeDuration = 0.16f;
        button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var edge = ImageRect(root.transform, "ValueEdge", Blue, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(5f, 0f));
        SetRaycast(edge, false);
        MakeText(root.transform, "Label", label, 14, Paper, FontStyle.Bold, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(-12f, 0f), TextAnchor.MiddleCenter);
        return button;
    }

    static UITransition BuildTransition(Transform parent)
    {
        var root = ImageRect(parent, "TransitionOverlay", Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero);
        root.transform.SetAsLastSibling();
        SetRaycast(root, false);
        var group = root.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        var colors = new[] { Ink, Blue, Paper };
        var widths = new[] { 1720f, 1500f, 1320f };
        var strips = new RectTransform[3];
        var images = new Image[3];
        for (int i = 0; i < strips.Length; i++)
        {
            var image = ImageRect(root, "WipeBlock" + i, colors[i], new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(2300f, 0f), new Vector2(widths[i], 0f));
            SetRaycast(image, false);
            strips[i] = image;
            images[i] = image.GetComponent<Image>();
        }
        var transition = root.gameObject.AddComponent<UITransition>();
        transition.overlayGroup = group;
        transition.strips = strips;
        transition.stripImages = images;
        return transition;
    }

    static RectTransform ImageRect(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        return rect;
    }

    static Text MakeText(Transform parent, string name, string value, int fontSize, Color color, FontStyle fontStyle,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size, TextAnchor alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var text = go.GetComponent<Text>();
        text.text = value;
        text.font = UiFont;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        SetRaycast(text, false);
        return text;
    }

    static void TopLeft(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    static void SetRaycast(RectTransform rect, bool enabled)
    {
        if (!rect) return;
        var graphic = rect.GetComponent<Graphic>();
        if (graphic) graphic.raycastTarget = enabled;
    }

    static void SetRaycast(Graphic graphic, bool enabled)
    {
        if (graphic) graphic.raycastTarget = enabled;
    }

    static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString("#" + value, out var color);
        return color;
    }
}

