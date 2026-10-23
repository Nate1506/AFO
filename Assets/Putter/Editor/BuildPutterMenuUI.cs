using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

/// <summary>
/// Editor utility to build the "Against the Current" main menu UI
/// inside the Putter scene. Run via Tools > AFO > Build Putter Menu UI.
/// </summary>
public static class BuildPutterMenuUI
{
    private const string SCENE_PATH = "Assets/Putter/Putter.unity";

    [MenuItem("Tools/AFO/Build Putter Menu UI")]
    public static void Build()
    {
        // --- Open the Putter scene ---
        var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

        // =========================================================
        // CANVAS
        // =========================================================
        var canvasGO = new GameObject("MainMenuCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // CanvasGroup for fade-in
        var cg = canvasGO.AddComponent<CanvasGroup>();

        // EventSystem (if absent)
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }

        // =========================================================
        // BACKGROUND (dark full-screen panel)
        // =========================================================
        var bgGO = MakeImage(canvasGO.transform, "Background",
            new Color(0.07f, 0.08f, 0.10f, 1f));
        RectFull(bgGO.GetComponent<RectTransform>());

        // Left dark vignette-style gradient panel (simulates the dark left side)
        var leftPanel = MakeImage(canvasGO.transform, "LeftOverlay",
            new Color(0.04f, 0.05f, 0.07f, 0.92f));
        var leftRT = leftPanel.GetComponent<RectTransform>();
        leftRT.anchorMin = new Vector2(0, 0);
        leftRT.anchorMax = new Vector2(0.42f, 1);
        leftRT.offsetMin = leftRT.offsetMax = Vector2.zero;

        // =========================================================
        // LOGO PANEL  (top-left region)
        // =========================================================
        var logoContainer = MakeRect(canvasGO.transform, "LogoContainer");
        var logoRT = logoContainer.GetComponent<RectTransform>();
        logoRT.anchorMin = new Vector2(0f, 0.55f);
        logoRT.anchorMax = new Vector2(0.40f, 1f);
        logoRT.offsetMin = new Vector2(40, 0);
        logoRT.offsetMax = new Vector2(-20, -30);

        // "AGAINST THE CURRENT" text logo (3 stacked text blocks)
        var line1 = MakeText(logoContainer.transform, "LogoLine1", "AGAINST", 70,
            FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
        SetRect(line1, new Vector2(0, 0.62f), new Vector2(1, 1f), Vector2.zero, Vector2.zero);

        var line2 = MakeText(logoContainer.transform, "LogoLine2", "THE", 44,
            FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
        SetRect(line2, new Vector2(0, 0.38f), new Vector2(0.55f, 0.65f), Vector2.zero, Vector2.zero);

        var line3 = MakeText(logoContainer.transform, "LogoLine3", "CURRENT", 70,
            FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
        SetRect(line3, new Vector2(0, 0f), new Vector2(1, 0.42f), Vector2.zero, Vector2.zero);

        // Decorative underline
        var underline = MakeImage(logoContainer.transform, "LogoUnderline",
            new Color(1f, 1f, 1f, 0.6f));
        var ulRT = underline.GetComponent<RectTransform>();
        ulRT.anchorMin = new Vector2(0, -0.06f);
        ulRT.anchorMax = new Vector2(0.9f, -0.02f);
        ulRT.offsetMin = ulRT.offsetMax = Vector2.zero;

        // =========================================================
        // BUTTONS PANEL  (left-center)
        // =========================================================
        var buttonsPanel = MakeRect(canvasGO.transform, "ButtonsPanel");
        var bpRT = buttonsPanel.GetComponent<RectTransform>();
        bpRT.anchorMin = new Vector2(0.02f, 0.15f);
        bpRT.anchorMax = new Vector2(0.40f, 0.58f);
        bpRT.offsetMin = new Vector2(40, 0);
        bpRT.offsetMax = new Vector2(-10, 0);

        var vLayout = buttonsPanel.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 12;
        vLayout.childForceExpandHeight = true;
        vLayout.childForceExpandWidth  = true;
        vLayout.childAlignment = TextAnchor.UpperLeft;
        vLayout.padding = new RectOffset(0, 0, 0, 0);

        // =========================================================
        // Individual Buttons
        // =========================================================
        var btnPlay     = MakeMenuButton(buttonsPanel.transform, "BtnPlay",     "▶  PLAY",
            new Color(1f, 1f, 1f, 1f), new Color(0.1f, 0.1f, 0.1f, 1f), true);
        var btnSettings = MakeMenuButton(buttonsPanel.transform, "BtnSettings", "⚙  SETTINGS",
            new Color(1f, 1f, 1f, 0f), Color.white, false);
        var btnCredits  = MakeMenuButton(buttonsPanel.transform, "BtnCredits",  "≡  CREDITS",
            new Color(1f, 1f, 1f, 0f), Color.white, false);
        var btnQuit     = MakeMenuButton(buttonsPanel.transform, "BtnQuit",     "↺  QUIT",
            new Color(1f, 1f, 1f, 0f), Color.white, false);

        // =========================================================
        // VERSION TEXT  (bottom-left)
        // =========================================================
        var versionText = MakeText(canvasGO.transform, "VersionText", "v0.1.0 ALPHA",
            16, FontStyle.Normal, new Color(1f, 1f, 1f, 0.35f), TextAnchor.LowerLeft);
        var vtRT = versionText.GetComponent<RectTransform>();
        vtRT.anchorMin = new Vector2(0, 0);
        vtRT.anchorMax = new Vector2(0.3f, 0.08f);
        vtRT.offsetMin = new Vector2(45, 15);
        vtRT.offsetMax = new Vector2(0, 0);

        // =========================================================
        // Wire up MainMenuUI MonoBehaviour
        // =========================================================
        var menuController = canvasGO.AddComponent<MainMenuUI>();
        menuController.mainCanvasGroup = cg;
        menuController.logoRect        = logoRT;
        menuController.playButton      = btnPlay.GetComponent<Button>();
        menuController.settingsButton  = btnSettings.GetComponent<Button>();
        menuController.creditsButton   = btnCredits.GetComponent<Button>();
        menuController.quitButton      = btnQuit.GetComponent<Button>();

        // =========================================================
        // Save scene
        // =========================================================
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[AFO] Putter main menu UI built and scene saved.");
        EditorUtility.DisplayDialog("Done", "Putter scene main menu UI built successfully!", "OK");
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static GameObject MakeRect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject MakeImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static GameObject MakeText(Transform parent, string name, string text,
        int fontSize, FontStyle style, Color color, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.text      = text;
        t.fontSize  = fontSize;
        t.fontStyle = style;
        t.color     = color;
        t.alignment = anchor;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
        return go;
    }

    /// <summary>Creates a stylised main-menu button.</summary>
    private static GameObject MakeMenuButton(Transform parent, string name, string label,
        Color bgColor, Color textColor, bool highlighted)
    {
        // Container
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var img = go.GetComponent<Image>();
        img.color = highlighted ? new Color(0.95f, 0.95f, 0.95f, 1f) : new Color(0f, 0f, 0f, 0f);

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor      = img.color;
        colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 0.25f);
        colors.pressedColor     = new Color(0.65f, 0.65f, 0.65f, 0.4f);
        btn.colors              = colors;

        // Label
        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelGO.transform.SetParent(go.transform, false);
        var lRT = labelGO.GetComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero;
        lRT.anchorMax = Vector2.one;
        lRT.offsetMin = new Vector2(20, 0);
        lRT.offsetMax = new Vector2(-10, 0);

        var txt = labelGO.GetComponent<Text>();
        txt.text      = label;
        txt.fontSize  = highlighted ? 28 : 24;
        txt.fontStyle = FontStyle.Bold;
        txt.color     = highlighted ? new Color(0.1f, 0.1f, 0.1f, 1f) : new Color(1f, 1f, 1f, 0.85f);
        txt.alignment = TextAnchor.MiddleLeft;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Left accent bar for highlighted button
        if (highlighted)
        {
            var accent = new GameObject("AccentBar", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(go.transform, false);
            var aRT = accent.GetComponent<RectTransform>();
            aRT.anchorMin = new Vector2(0, 0);
            aRT.anchorMax = new Vector2(0, 1);
            aRT.offsetMin = new Vector2(0, 0);
            aRT.offsetMax = new Vector2(6, 0);
            accent.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);
        }

        // Set the button's target graphic
        btn.targetGraphic = img;

        return go;
    }

    private static void RectFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void SetRect(GameObject go,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }
}
