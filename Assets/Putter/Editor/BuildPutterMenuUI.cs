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
    static readonly Font DisplayFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Putter/Fonts/Oswald-Variable.ttf");

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

        var backdropMotion = BuildBackdrop(canvasObject.transform);
        var titleObject = new GameObject("TitleComposition", typeof(RectTransform), typeof(CanvasGroup));
        titleObject.transform.SetParent(canvasObject.transform, false);
        var titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        BuildLogo(titleObject.transform);
        var buttons = new Button[4];
        var styles = new MenuButton[4];
        string[] names = { "PLAY", "SETTINGS", "CREDITS", "QUIT" };
        string[] icons = { "01", "02", "03", "04" };
        float[] x = { 102f, 118f, 480f, 870f };
        float[] y = { -406f, -704f, -728f, -692f };
        float[] widths = { 1010f, 340f, 365f, 260f };
        float[] heights = { 238f, 80f, 80f, 80f };
        for (int i = 0; i < names.Length; i++)
            buttons[i] = BuildMenuButton(canvasObject.transform, names[i], icons[i], x[i], y[i], widths[i], heights[i], out styles[i]);
        var buttonRects = new RectTransform[buttons.Length];
        var buttonGroups = new CanvasGroup[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            buttonRects[i] = buttons[i].GetComponent<RectTransform>();
            buttonGroups[i] = buttons[i].GetComponent<CanvasGroup>();
        }

        BuildFooter(canvasObject.transform);
        var settings = BuildSettingsPanel(canvasObject.transform, out var settingsHeader, out var settingsBack, out var volume, out var volumeText, out var fullscreen, out var reducedMotion, out var qualityButton, out var qualityText, out var resolutionButton, out var resolutionText);
        var credits = BuildCreditsPanel(canvasObject.transform, out var creditsHeader, out var creditsBack);
        settings.SetActive(false);
        credits.SetActive(false);
        settingsHeader.SetActive(false);
        creditsHeader.SetActive(false);

        var transition = BuildTransition(canvasObject.transform);
        var controller = canvasObject.AddComponent<MainMenuUI>();
        controller.menuButtons = buttons;
        controller.styledButtons = styles;
        controller.playSceneName = "Nate";
        controller.mainCanvasGroup = canvasGroup;
        controller.transition = transition;
        var menuMotion = canvasObject.AddComponent<AfoMenuMotion>();
        menuMotion.canvasGroup = canvasGroup;
        menuMotion.title = titleRect;
        menuMotion.titleGroup = titleObject.GetComponent<CanvasGroup>();
        menuMotion.buttons = buttonRects;
        menuMotion.buttonGroups = buttonGroups;
        menuMotion.backdrop = backdropMotion;
        controller.menuMotion = menuMotion;
        controller.settingsPanel = settings;
        controller.creditsPanel = credits;
        controller.settingsHeaderPanel = settingsHeader;
        controller.creditsHeaderPanel = creditsHeader;
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
        EnsureBuildScene(ScenePath, 0);
        EnsureBuildScene("Assets/Nate/Nate.unity", 1);
        Debug.Log("[AFO] Rebuilt keyboard-and-mouse main menu, settings, credits, and transitions.");
    }

    static void BuildEventSystem(Scene scene)
    {
        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        SceneManager.MoveGameObjectToScene(eventSystem, scene);
        var module = eventSystem.GetComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
    }

    static AfoBackdropMotion BuildBackdrop(Transform parent)
    {
        var background = ImageRect(parent, "MidnightBackground", Midnight, new Vector2(0f, 0f), Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero);
        SetRaycast(background, false);

        // A quiet orbital motif balances the hard panels with round silhouettes.
        var orbitOuter = ShapeRect(parent, "SignalOrbitOuter", AfoShapeImage.Shape.Circle, new Color32(36, 107, 253, 25),
            new Vector2(0.79f, 0.54f), new Vector2(0.79f, 0.54f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(390f, 390f));
        SetRaycast(orbitOuter, false);
        var orbitInner = ShapeRect(parent, "SignalOrbitInner", AfoShapeImage.Shape.Circle, new Color32(5, 12, 25, 255),
            new Vector2(0.79f, 0.54f), new Vector2(0.79f, 0.54f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(376f, 376f));
        SetRaycast(orbitInner, false);
        var orbitCore = ShapeRect(parent, "SignalOrbitCore", AfoShapeImage.Shape.Circle, new Color32(36, 107, 253, 120),
            new Vector2(0.79f, 0.54f), new Vector2(0.79f, 0.54f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(11f, 11f));
        SetRaycast(orbitCore, false);

        RectTransform[] motion = new RectTransform[9];
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
        motion[4] = orbitOuter;
        motion[5] = orbitInner;
        motion[6] = orbitCore;
        motion[7] = null;
        motion[8] = null;
        var mot = parent.gameObject.AddComponent<AfoBackdropMotion>();

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

        MakeText(parent, "WorldIndex", "AFO    /    01", 16, Blue, FontStyle.Bold,
            new Vector2(0.88f, 0.91f), new Vector2(0.88f, 0.91f), new Vector2(0f, 1f), Vector2.zero, new Vector2(250f, 30f), TextAnchor.MiddleLeft);
        var indexMark = MakeText(parent, "SignalIndex", "01", 150, new Color32(36, 107, 253, 28), FontStyle.Bold,
            new Vector2(0.84f, 0.48f), new Vector2(0.84f, 0.48f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280f, 200f), TextAnchor.MiddleCenter);
        indexMark.transform.localRotation = Quaternion.Euler(0f, 0f, -7f);
        motion[7] = indexMark.rectTransform;
        var slash = ShapeRect(parent, "SignalHex", AfoShapeImage.Shape.Hexagon, new Color32(36, 107, 253, 20),
            new Vector2(0.73f, 0.15f), new Vector2(0.73f, 0.15f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 210f));
        slash.localRotation = Quaternion.Euler(0f, 0f, 18f);
        SetRaycast(slash, false);
        motion[8] = slash;
        mot.shapes = motion;
        return mot;
    }

    static RectTransform BuildLogo(Transform parent)
    {
        var slash = ShapeRect(parent, "TitleSlash", AfoShapeImage.Shape.Parallelogram, new Color32(36, 107, 253, 72),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(370f, -120f), new Vector2(930f, 132f));
        slash.localRotation = Quaternion.Euler(0f, 0f, -4.5f);
        SetRaycast(slash, false);
        var mark = ImageRect(parent, "LogoMark", Blue, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(84f, -78f), new Vector2(8f, 38f));
        SetRaycast(mark, false);
        var eyebrow = MakeText(parent, "LogoEyebrow", "AFO     /     A NEW CURRENT", 18, Blue, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(108f, -69f), new Vector2(580f, 35f), TextAnchor.MiddleLeft);
        eyebrow.font = DisplayFont ? DisplayFont : UiFont;
        var against = MakeText(parent, "LogoAgainst", "AGAINST", 164, Paper, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(82f, -88f), new Vector2(1010f, 207f), TextAnchor.MiddleLeft);
        against.font = DisplayFont ? DisplayFont : UiFont;
        against.verticalOverflow = VerticalWrapMode.Overflow;
        against.horizontalOverflow = HorizontalWrapMode.Overflow;
        against.transform.localRotation = Quaternion.Euler(0f, 0f, -1.6f);
        var smallThe = MakeText(parent, "LogoThe", "THE", 31, Blue, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(116f, -290f), new Vector2(120f, 42f), TextAnchor.MiddleLeft);
        smallThe.font = DisplayFont ? DisplayFont : UiFont;
        smallThe.verticalOverflow = VerticalWrapMode.Overflow;
        var current = MakeText(parent, "LogoTheCurrent", "CURRENT", 111, Paper, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(204f, -250f), new Vector2(940f, 139f), TextAnchor.MiddleLeft);
        current.font = DisplayFont ? DisplayFont : UiFont;
        current.verticalOverflow = VerticalWrapMode.Overflow;
        current.horizontalOverflow = HorizontalWrapMode.Overflow;
        current.transform.localRotation = Quaternion.Euler(0f, 0f, 1.1f);
        var spark = ShapeRect(parent, "TitleSpark", AfoShapeImage.Shape.Parallelogram, Paper,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(1090f, -222f), new Vector2(125f, 18f));
        spark.localRotation = Quaternion.Euler(0f, 0f, -12f);
        SetRaycast(spark, false);
        return mark;
    }

    static Button BuildMenuButton(Transform parent, string label, string iconText, float x, float y, float width, float height, out MenuButton style)
    {
        bool primary = label == "PLAY";
        var root = new GameObject("Btn" + label, typeof(RectTransform), typeof(AfoShapeImage), typeof(Button), typeof(MenuButton), typeof(CanvasGroup));
        root.transform.SetParent(parent, false);
        var rect = root.GetComponent<RectTransform>();
        CenterPivot(rect, x, y, width, height);
        var silhouetteGraphic = root.GetComponent<AfoShapeImage>();
        silhouetteGraphic.shape = AfoShapeImage.Shape.Rectangle;
        silhouetteGraphic.color = Color.clear;
        var button = root.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = silhouetteGraphic;
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        Image plate;
        Image shadow = null;
        if (primary)
        {
            var shadowRect = ShapeRect(root.transform, "PlayShadow", AfoShapeImage.Shape.Parallelogram, new Color32(0, 0, 0, 190),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(15f, -13f), new Vector2(760f, 176f));
            shadowRect.SetAsFirstSibling();
            shadow = shadowRect.GetComponent<Image>();
            SetRaycast(shadowRect, false);
            var plateRect = ShapeRect(root.transform, "PlayPlate", AfoShapeImage.Shape.Parallelogram, Blue,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 176f));
            plateRect.SetAsFirstSibling();
            plate = plateRect.GetComponent<Image>();
            SetRaycast(plateRect, false);
            var echo = MakeText(root.transform, "PlayEcho", "PLAY", 190, new Color32(36, 107, 253, 75), FontStyle.Bold,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(140f, -13f), new Vector2(790f, 216f), TextAnchor.MiddleLeft);
            echo.font = DisplayFont ? DisplayFont : UiFont;
            echo.horizontalOverflow = HorizontalWrapMode.Overflow;
        }
        else
        {
            var slash = ShapeRect(root.transform, "OptionSlash", AfoShapeImage.Shape.Parallelogram, new Color32(36, 107, 253, 0),
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(5f, 0f), new Vector2(8f, 0f));
            plate = slash.GetComponent<Image>();
            SetRaycast(slash, false);
        }
        var flash = ShapeRect(root.transform, "ImpactFlash", AfoShapeImage.Shape.Parallelogram, new Color32(244, 247, 255, 0),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(primary ? 760f : width, height * 0.65f));
        SetRaycast(flash, false);
        var index = MakeText(root.transform, "Index", iconText, primary ? 24 : 15, primary ? Paper : Blue, FontStyle.Bold,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(primary ? 76f : 0f, primary ? 1f : 0f), new Vector2(primary ? 55f : 44f, 36f), TextAnchor.MiddleLeft);
        index.font = DisplayFont ? DisplayFont : UiFont;
        var labelText = MakeText(root.transform, "Label", label, primary ? 190 : 44, primary ? Ink : Paper, FontStyle.Bold,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(primary ? 140f : 50f, 0f), new Vector2(primary ? 790f : width - 54f, primary ? 216f : 72f), TextAnchor.MiddleLeft);
        labelText.font = DisplayFont ? DisplayFont : UiFont;
        labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
        labelText.verticalOverflow = VerticalWrapMode.Overflow;
        if (!primary) labelText.transform.localRotation = Quaternion.Euler(0f, 0f, label == "SETTINGS" ? -1.4f : label == "CREDITS" ? 0.7f : -2f);
        var tag = MakeText(root.transform, "ActionTag", primary ? "ENTER  →" : "", 15, primary ? Ink : Blue, FontStyle.Bold,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(primary ? -100f : -5f, 0f), new Vector2(140f, 34f), TextAnchor.MiddleRight);
        tag.font = DisplayFont ? DisplayFont : UiFont;

        style = root.GetComponent<MenuButton>();
        style.background = plate;
        style.accent = null;
        style.shadow = shadow;
        style.impactFlash = flash.GetComponent<Image>();
        style.icon = index;
        style.label = labelText;
        style.actionHint = tag;
        style.textLed = !primary;
        style.spotlight = primary;
        style.selectionOffset = primary ? 20f : 12f;
        style.initiallySelected = primary;
        style.hoverRotationDegrees = label == "PLAY" || label == "CREDITS" ? -1.2f : 1.2f;
        plate.color = primary ? Blue : new Color32(36, 107, 253, 0);
        return button;
    }

    static void BuildFooter(Transform parent)
    {
        MakeText(parent, "ControlsHint", "W / S   OR   ↑ / ↓   NAVIGATE     ·     ENTER / SPACE   CONFIRM", 15, new Color32(244, 247, 255, 180), FontStyle.Bold,
            new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, new Vector2(98f, 96f), new Vector2(900f, 30f), TextAnchor.MiddleLeft);
        MakeText(parent, "MouseHint", "MOUSE   MOVE / CLICK", 13, new Color32(36, 107, 253, 255), FontStyle.Bold,
            new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, new Vector2(98f, 50f), new Vector2(500f, 24f), TextAnchor.MiddleLeft);
        MakeText(parent, "VersionText", "AFO   /   MENU SYSTEM 01", 13, new Color32(244, 247, 255, 95), FontStyle.Normal,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-54f, 34f), new Vector2(280f, 26f), TextAnchor.MiddleRight);
    }

    static GameObject BuildSettingsPanel(Transform parent, out GameObject headerGroup, out Button back, out Slider volume, out Text volumeText, out Toggle fullscreen, out Toggle reducedMotion, out Button qualityButton, out Text qualityText, out Button resolutionButton, out Text resolutionText)
    {
        var panel = MakePanelRoot(parent, "SettingsPanel");
        AddPanelGraphics(panel.transform, "02", new Vector2(0.83f, 0.22f));
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

        Card(content.transform, "DisplayCard", "DISPLAY", "SCREEN MODE", new Vector2(90f, -482f), new Vector2(870f, 112f));
        resolutionButton = BuildValueButton(content.transform, "ResolutionValueButton", new Vector2(600f, -510f), new Vector2(310f, 54f), "RESOLUTION");
        resolutionText = resolutionButton.transform.Find("Label").GetComponent<Text>();
        fullscreen = BuildToggle(content.transform, "FullscreenToggle", "FULLSCREEN", new Vector2(385f, -510f), new Vector2(195f, 54f));

        Card(content.transform, "AccessCard", "ACCESSIBILITY", "REDUCED MOTION", new Vector2(90f, -614f), new Vector2(870f, 112f));
        reducedMotion = BuildToggle(content.transform, "ReducedMotionToggle", "REDUCED", new Vector2(670f, -641f), new Vector2(240f, 54f));

        var controlCard = ShapeRect(content.transform, "KeyboardMouseCard", AfoShapeImage.Shape.Octagon, new Color32(5, 12, 25, 245), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(1010f, -218f), new Vector2(820f, 508f));
        controlCard.GetComponent<AfoShapeImage>().cornerCut = 0.07f;
        SetRaycast(controlCard, false);
        var outline = controlCard.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color32(36, 107, 253, 100);
        outline.effectDistance = new Vector2(1f, -1f);
        MakeText(controlCard, "InputHeading", "KEYBOARD  /  MOUSE", 23, Paper, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(80f, -28f), new Vector2(680f, 42f), TextAnchor.MiddleLeft);
        var rule = ImageRect(controlCard, "InputRule", Blue, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(80f, -83f), new Vector2(675f, 2f));
        SetRaycast(rule, false);
        MakeText(controlCard, "InputRows", "MOUSE MOVE       Highlight option\nLEFT CLICK         Activate option\nW / S   ·   ↑ / ↓    Navigate vertically\nA / D   ·   ← / →    Navigate / adjust\nENTER / SPACE       Confirm selection\nESC / BACKSPACE    Return to menu\nMOUSE WHEEL        Scroll long lists", 20, Paper, FontStyle.Normal,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(80f, -115f), new Vector2(680f, 300f), TextAnchor.UpperLeft);
        MakeText(controlCard, "InputFootnote", "Game-specific movement controls are not configured in this build.", 15, new Color32(244, 247, 255, 145), FontStyle.Normal,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(80f, 24f), new Vector2(-130f, 40f), TextAnchor.MiddleLeft);
        headerGroup = CreateHeaderGroup(parent, "SettingsHeaderGroup");
        Header(headerGroup.transform, "SYSTEM SETTINGS", "ACTUAL OPTIONS AVAILABLE IN THIS BUILD", out back);
        return panel;
    }

    static GameObject BuildCreditsPanel(Transform parent, out GameObject headerGroup, out Button back)
    {
        var panel = MakePanelRoot(parent, "CreditsPanel");
        AddPanelGraphics(panel.transform, "03", new Vector2(0.78f, 0.50f));
        var card = ShapeRect(panel.transform, "CreditsBlock", AfoShapeImage.Shape.Octagon, new Color32(5, 12, 25, 245), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(105f, -240f), new Vector2(980f, 400f));
        card.GetComponent<AfoShapeImage>().cornerCut = 0.07f;
        SetRaycast(card, false);
        var accent = ImageRect(card, "CreditAccent", Blue, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(9f, 0f));
        SetRaycast(accent, false);
        MakeText(card, "CreditTitle", "AFO", 58, Paper, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(46f, -48f), new Vector2(360f, 76f), TextAnchor.MiddleLeft);
        MakeText(card, "CreditGame", "AGAINST THE CURRENT", 27, Blue, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -138f), new Vector2(700f, 52f), TextAnchor.MiddleLeft);
        MakeText(card, "CreditThanks", "THANK YOU FOR PLAYING", 18, new Color32(244, 247, 255, 180), FontStyle.Normal,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -214f), new Vector2(600f, 40f), TextAnchor.MiddleLeft);
        MakeText(panel.transform, "CreditReturnHint", "ESC   /   BACKSPACE     RETURN", 15, new Color32(244, 247, 255, 155), FontStyle.Bold,
            new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, new Vector2(105f, 62f), new Vector2(420f, 30f), TextAnchor.MiddleLeft);
        headerGroup = CreateHeaderGroup(parent, "CreditsHeaderGroup");
        Header(headerGroup.transform, "CREDITS", "AGAINST THE CURRENT", out back, 54f);
        return panel;
    }

    static GameObject CreateHeaderGroup(Transform parent, string name)
    {
        var header = new GameObject(name, typeof(RectTransform));
        header.transform.SetParent(parent, false);
        var rect = header.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        return header;
    }

    static GameObject MakePanelRoot(Transform parent, string name)
    {
        var root = ImageRect(parent, name, new Color32(7, 20, 38, 255), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero);
        SetRaycast(root, true);
        var topBar = ImageRect(root, "PanelBar", Blue, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, 5f));
        SetRaycast(topBar, false);
        return root.gameObject;
    }

    static void AddPanelGraphics(Transform parent, string index, Vector2 markerAnchor)
    {
        var slash = ImageRect(parent, "PanelSlashA", new Color32(36, 107, 253, 20), new Vector2(0.78f, 0.08f), new Vector2(0.88f, 0.93f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 0f));
        slash.localRotation = Quaternion.Euler(0f, 0f, 13f);
        SetRaycast(slash, false);
        var slash2 = ImageRect(parent, "PanelSlashB", new Color32(36, 107, 253, 12), new Vector2(0.92f, 0.12f), new Vector2(0.97f, 0.88f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18f, 0f));
        slash2.localRotation = Quaternion.Euler(0f, 0f, 13f);
        SetRaycast(slash2, false);
        var marker = MakeText(parent, "PanelIndex", index, 142, new Color32(36, 107, 253, 28), FontStyle.Bold,
            markerAnchor, markerAnchor, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 190f), TextAnchor.MiddleCenter);
        marker.transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
    }

    static void Header(Transform parent, string title, string subtitle, out Button back, float verticalOffset = 0f)
    {
        MakeText(parent, "PanelEyebrow", "AFO   /   SYSTEM", 16, Blue, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(96f, -42f - verticalOffset), new Vector2(450f, 30f), TextAnchor.MiddleLeft);
        MakeText(parent, "PanelTitle", title, 48, Paper, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(94f, -73f - verticalOffset), new Vector2(850f, 72f), TextAnchor.MiddleLeft);
        MakeText(parent, "PanelSubtitle", subtitle, 15, new Color32(244, 247, 255, 155), FontStyle.Normal,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(96f, -143f - verticalOffset), new Vector2(880f, 30f), TextAnchor.MiddleLeft);
        back = BuildValueButton(parent, "BackButton", new Vector2(0f, -50f), new Vector2(198f, 54f), "←   BACK");
        var rt = back.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-96f, -46f - verticalOffset);
    }

    static void Card(Transform parent, string name, string heading, string subheading, Vector2 position, Vector2 size)
    {
        var card = ShapeRect(parent, name, AfoShapeImage.Shape.Octagon, Navy, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);
        card.GetComponent<AfoShapeImage>().cornerCut = 0.07f;
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
        var go = new GameObject("MasterVolumeSlider", typeof(RectTransform), typeof(Slider), typeof(MenuButton));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        CenterPivot(rect, topLeft.x, topLeft.y, 210f, 32f);
        var background = ShapeRect(go.transform, "Background", AfoShapeImage.Shape.Oval, new Color32(5, 7, 11, 255), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero);
        SetRaycast(background, true);
        var fill = ImageRect(go.transform, "Fill", Blue, Vector2.zero, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero, Vector2.zero);
        SetRaycast(fill, false);
        var handle = ShapeRect(go.transform, "Handle", AfoShapeImage.Shape.Circle, Paper, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 34f));
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
        go.GetComponent<MenuButton>().hoverRotationDegrees = 0.55f;
        return slider;
    }

    static Toggle BuildToggle(Transform parent, string name, string label, Vector2 topLeft, Vector2 size)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(AfoShapeImage), typeof(Toggle), typeof(MenuButton));
        root.transform.SetParent(parent, false);
        var rt = root.GetComponent<RectTransform>();
        CenterPivot(rt, topLeft.x, topLeft.y, size.x, size.y);
        var image = root.GetComponent<Image>();
        image.color = Navy;
        root.GetComponent<AfoShapeImage>().shape = AfoShapeImage.Shape.RoundedRectangle;
        var flash = ShapeRect(root.transform, "ImpactFlash", AfoShapeImage.Shape.Hexagon, new Color32(244, 247, 255, 0),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 34f));
        SetRaycast(flash, false);
        var indicator = ImageRect(root.transform, "Indicator", Blue, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(7f, 0f));
        SetRaycast(indicator, false);
        var check = ShapeRect(root.transform, "Check", AfoShapeImage.Shape.Circle, Paper, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(23f, 0f), new Vector2(14f, 14f));
        SetRaycast(check, false);
        MakeText(root.transform, "Label", label, 14, Paper, FontStyle.Bold, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), new Vector2(47f, 0f), new Vector2(-8f, 0f), TextAnchor.MiddleLeft);
        var toggle = root.GetComponent<Toggle>();
        toggle.targetGraphic = image;
        toggle.graphic = check.GetComponent<Image>();
        toggle.isOn = false;
        toggle.transition = Selectable.Transition.None;
        toggle.navigation = new Navigation { mode = Navigation.Mode.None };
        var hoverStyle = root.GetComponent<MenuButton>();
        hoverStyle.background = image;
        hoverStyle.accent = indicator.GetComponent<Image>();
        hoverStyle.label = root.transform.Find("Label").GetComponent<Text>();
        hoverStyle.impactFlash = flash.GetComponent<Image>();
        hoverStyle.hoverRotationDegrees = 0.9f;
        return toggle;
    }

    static Button BuildValueButton(Transform parent, string name, Vector2 topLeft, Vector2 size, string label)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(AfoShapeImage), typeof(Button), typeof(MenuButton));
        root.transform.SetParent(parent, false);
        var rt = root.GetComponent<RectTransform>();
        CenterPivot(rt, topLeft.x, topLeft.y, size.x, size.y);
        var image = root.GetComponent<Image>();
        image.color = new Color32(5, 7, 11, 255);
        root.GetComponent<AfoShapeImage>().shape = AfoShapeImage.Shape.RoundedRectangle;
        var flash = ShapeRect(root.transform, "ImpactFlash", AfoShapeImage.Shape.Hexagon, new Color32(244, 247, 255, 0),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 34f));
        SetRaycast(flash, false);
        var button = root.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var edge = ShapeRect(root.transform, "ValueEdge", AfoShapeImage.Shape.Circle, Blue, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(18f, 0f), new Vector2(9f, 9f));
        SetRaycast(edge, false);
        MakeText(root.transform, "Label", label, 14, Paper, FontStyle.Bold, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(-12f, 0f), TextAnchor.MiddleCenter);
        var hoverStyle = root.GetComponent<MenuButton>();
        hoverStyle.background = image;
        hoverStyle.accent = edge.GetComponent<Image>();
        hoverStyle.label = root.transform.Find("Label").GetComponent<Text>();
        hoverStyle.impactFlash = flash.GetComponent<Image>();
        hoverStyle.hoverRotationDegrees = 0.9f;
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
        var colors = new[] { Ink, Blue, Paper, Midnight };
        var angles = new[] { -8f, 5f, -2f, 9f };
        var strips = new RectTransform[colors.Length];
        var images = new Image[colors.Length];
        for (int i = 0; i < strips.Length; i++)
        {
            var image = ImageRect(root, "WipeBlock" + i, colors[i], new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(2300f, 0f), new Vector2(2200f - i * 70f, 0f));
            image.localRotation = Quaternion.Euler(0f, 0f, angles[i]);
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

    static RectTransform ShapeRect(Transform parent, string name, AfoShapeImage.Shape shape, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(AfoShapeImage));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var graphic = go.GetComponent<AfoShapeImage>();
        graphic.shape = shape;
        graphic.color = color;
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

    static void CenterPivot(RectTransform rect, float x, float y, float width, float height)
    {
        TopLeft(rect, x, y, width, height);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x + width * 0.5f, y - height * 0.5f);
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

    static void EnsureBuildScene(string path, int position)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        int currentIndex = scenes.FindIndex(scene => scene.path == path);
        var entry = currentIndex >= 0 ? scenes[currentIndex] : new EditorBuildSettingsScene(path, true);
        entry.enabled = true;
        if (currentIndex >= 0) scenes.RemoveAt(currentIndex);
        scenes.Insert(Mathf.Clamp(position, 0, scenes.Count), entry);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString("#" + value, out var color);
        return color;
    }
}

