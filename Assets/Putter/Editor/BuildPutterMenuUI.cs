using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Applies the authored AFO menu treatment to the existing Putter scene.
/// This intentionally updates existing objects instead of creating a second set
/// of artwork every time the command is run.
/// </summary>
public static class BuildPutterMenuUI
{
    const string ScenePath = "Assets/Putter/Putter.unity";
    const string MenuPath = "Tools/AFO/Rebuild Main Menu UI";

    [MenuItem(MenuPath)]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[AFO] Stop Play Mode before rebuilding the Putter menu.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        GameObject canvasObject = GameObject.Find("MainMenuCanvas");
        if (!canvasObject)
        {
            Debug.LogError("[AFO] MainMenuCanvas was not found in Putter.unity; no changes were made.");
            return;
        }

        Transform canvas = canvasObject.transform;
        Undo.RegisterFullObjectHierarchyUndo(canvasObject, "Rebuild AFO Main Menu UI");

        // Carry the title's lower edge into a thin right-angle connection to the current ring.
        ConfigureRect(canvas, "TitleComposition/TitleSlash", new Vector2(82f, -421f), new Vector2(1240f, 3f));
        SetLocalAngle(canvas, "TitleComposition/TitleSlash", 0f);
        SetShape(canvas, "TitleComposition/TitleSlash", AfoShapeImage.Shape.Rectangle, new Color32(36, 107, 253, 215));
        ConfigureCorner(canvas, "TitleComposition/TitleSpark", new Vector2(1322f, -422f), 8f);
        SetShape(canvas, "TitleComposition/TitleSpark", AfoShapeImage.Shape.Rectangle, new Color32(244, 247, 255, 245));
        AfoShapeImage compositionDrop = EnsureShape(canvas, "CompositionDrop");
        ConfigureAnchoredLine(compositionDrop.rectTransform, new Vector2(1322f, -422f), new Vector2(3f, 76f), new Vector2(0.5f, 1f));
        compositionDrop.shape = AfoShapeImage.Shape.Rectangle;
        compositionDrop.color = new Color32(36, 107, 253, 190);
        compositionDrop.raycastTarget = false;

        // Keep PLAY visually dominant while making its hit target track the art.
        Transform playRoot = canvas.Find("BtnPLAY");
        if (playRoot)
        {
            SetRectSize(playRoot, new Vector2(850f, 220f));
            AfoShapeImage[] plates = playRoot.GetComponentsInChildren<AfoShapeImage>(true);
            int plateIndex = 0;
            foreach (AfoShapeImage plate in plates)
            {
                if (!plate || plate.gameObject.name != "PlayPlate") continue;
                bool underlay = plateIndex++ == 0;
                ConfigureCenteredRect(plate.transform, Vector2.zero, underlay ? new Vector2(774f, 184f) : new Vector2(760f, 176f));
                plate.shape = AfoShapeImage.Shape.Parallelogram;
                plate.color = underlay ? new Color32(15, 48, 120, 235) : new Color32(36, 107, 253, 255);
                plate.cornerCut = 0.035f;
                plate.raycastTarget = false;
                plate.SetAllDirty();
            }

            Transform shadowTransform = playRoot.Find("PlayShadow");
            ConfigureCenteredRect(shadowTransform, new Vector2(10f, -9f), new Vector2(760f, 176f));
            SetShape(playRoot, "PlayShadow", AfoShapeImage.Shape.RoundedRectangle, new Color32(5, 7, 11, 165));
            SetCornerCut(playRoot, "PlayShadow", 0.035f);

            MenuButton playStyle = playRoot.GetComponent<MenuButton>();
            if (playStyle)
            {
                playStyle.lineTurnMode = true;
                playStyle.initiallySelected = true;
                playStyle.spotlight = true;
                playStyle.textLed = false;
                playStyle.selectionOffset = 0f;
                playStyle.hoverRotationDegrees = 0f;
                if (playStyle.label)
                {
                    ConfigureText(playStyle.label, "PLAY", 132, new Color32(244, 247, 255, 255));
                    ConfigureChildRect(playStyle.label.rectTransform, new Vector2(52f, 0f), new Vector2(660f, 158f));
                }
                if (playStyle.actionHint)
                    playStyle.actionHint.text = string.Empty;
            }

            Text playIndex = GetText(playRoot, "Index");
            if (playIndex)
            {
                ConfigureText(playIndex, "01", 22, new Color32(244, 247, 255, 255));
                ConfigureChildRect(playIndex.rectTransform, new Vector2(24f, 0f), new Vector2(44f, 36f));
            }

            Text playEcho = GetText(playRoot, "PlayEcho");
            if (playEcho)
            {
                ConfigureText(playEcho, "PLAY", 132, new Color32(36, 107, 253, 58));
                ConfigureChildRect(playEcho.rectTransform, new Vector2(50f, -4f), new Vector2(660f, 158f));
            }
        }
        else
        {
            Debug.LogError("[AFO] BtnPLAY was not found in Putter.unity; no changes were made.");
            return;
        }

        // Compact the three supporting choices into one deliberate baseline.
        ConfigureSecondary(canvas, "BtnSETTINGS", "02", "SETTINGS", new Vector2(260f, -750f), 300f);
        ConfigureSecondary(canvas, "BtnCREDITS", "03", "CREDITS", new Vector2(560f, -750f), 300f);
        ConfigureSecondary(canvas, "BtnQUIT", "04", "QUIT", new Vector2(860f, -750f), 300f);

        AfoShapeImage selectionHorizontal = EnsureShape(canvas, "MenuSelectionTurnHorizontal");
        ConfigureCenteredLine(selectionHorizontal.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 3f), new Vector2(0f, 0.5f));
        selectionHorizontal.shape = AfoShapeImage.Shape.Rectangle;
        selectionHorizontal.color = new Color32(36, 107, 253, 0);
        selectionHorizontal.raycastTarget = false;
        AfoShapeImage selectionVertical = EnsureShape(canvas, "MenuSelectionTurnVertical");
        ConfigureCenteredLine(selectionVertical.rectTransform, new Vector2(0f, 0f), new Vector2(3f, 0f), new Vector2(0.5f, 1f));
        selectionVertical.shape = AfoShapeImage.Shape.Rectangle;
        selectionVertical.color = new Color32(36, 107, 253, 0);
        selectionVertical.raycastTarget = false;
        AfoShapeImage selectionCorner = EnsureShape(canvas, "MenuSelectionTurnCorner");
        ConfigureCenteredLine(selectionCorner.rectTransform, new Vector2(0f, -1f), new Vector2(7f, 7f), new Vector2(0.5f, 0.5f));
        selectionCorner.shape = AfoShapeImage.Shape.Rectangle;
        selectionCorner.color = new Color32(244, 247, 255, 0);
        selectionCorner.raycastTarget = false;
        AfoMenuLineTurn lineTurn = canvasObject.GetComponent<AfoMenuLineTurn>();
        if (!lineTurn) lineTurn = canvasObject.AddComponent<AfoMenuLineTurn>();
        lineTurn.canvasRect = canvasObject.GetComponent<RectTransform>();
        lineTurn.horizontal = selectionHorizontal.rectTransform;
        lineTurn.vertical = selectionVertical.rectTransform;
        lineTurn.corner = selectionCorner.rectTransform;
        MainMenuUI menuUI = canvasObject.GetComponent<MainMenuUI>();
        if (menuUI) menuUI.lineTurn = lineTurn;

        // Let the orbit carry the right side; keep the long beams and signal marks subordinate.
        ConfigureFixedRect(canvas, "BluePanelA", new Vector2(0.74f, 0.50f), Vector2.zero, new Vector2(74f, 660f));
        ConfigureFixedRect(canvas, "BluePanelB", new Vector2(0.90f, 0.50f), Vector2.zero, new Vector2(40f, 520f));
        SetLocalAngle(canvas, "BluePanelA", 14f);
        SetLocalAngle(canvas, "BluePanelB", 14f);
        SetImageOpacity(canvas, "BluePanelA", 0.12f);
        SetImageOpacity(canvas, "BluePanelB", 0.08f);
        // Remove the opaque half-screen plate left by the old two-column layout.
        // The right-side signal art remains, but it now shares the same navy field.
        SetShapeOpacity(canvas, "RightGraphicComposer/RightPanel", 0f);
        SetShapeOpacity(canvas, "SignalOrbitOuter", 0.30f);
        SetTextOpacity(canvas, "SignalIndex", 0.42f);
        SetTextSize(canvas, "SignalIndex", 132);
        SetImageOpacity(canvas, "BlueSignal", 0.24f);
        SetImageOpacity(canvas, "BlueSignal2", 0.14f);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
        {
            Debug.LogError("[AFO] The menu was updated in memory, but Putter.unity could not be saved.");
            return;
        }

        Debug.Log("[AFO] Main menu rebuilt and saved to Assets/Putter/Putter.unity.");
    }

    static void ConfigureSecondary(Transform canvas, string objectName, string index, string label, Vector2 position, float width)
    {
        Transform root = canvas.Find(objectName);
        if (!root)
        {
            Debug.LogWarning($"[AFO] {objectName} was not found; the remaining menu items were still updated.");
            return;
        }

        RectTransform rootRect = root.GetComponent<RectTransform>();
        if (rootRect)
        {
            rootRect.anchoredPosition = position;
            rootRect.sizeDelta = new Vector2(width, 80f);
        }

        MenuButton style = root.GetComponent<MenuButton>();
        if (style)
        {
            style.lineTurnMode = true;
            style.textLed = true;
            style.spotlight = false;
            style.initiallySelected = false;
            style.selectionOffset = 0f;
            style.hoverRotationDegrees = 0f;
            if (style.label)
            {
                ConfigureText(style.label, label, 42, new Color32(244, 247, 255, 230));
                ConfigureChildRect(style.label.rectTransform, new Vector2(50f, 0f), new Vector2(width - 58f, 72f));
                style.label.rectTransform.localRotation = Quaternion.identity;
            }
        }

        Text indexText = GetText(root, "Index");
        if (indexText)
        {
            ConfigureText(indexText, index, 15, new Color32(36, 107, 253, 255));
            ConfigureChildRect(indexText.rectTransform, new Vector2(18f, 0f), new Vector2(44f, 36f));
        }
    }

    static Text GetText(Transform root, string path)
    {
        Transform child = root ? root.Find(path) : null;
        return child ? child.GetComponent<Text>() : null;
    }

    static void ConfigureText(Text text, string value, int size, Color color)
    {
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.resizeTextForBestFit = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
    }

    static void ConfigureRect(Transform root, string path, Vector2 position, Vector2 size)
    {
        Transform target = root.Find(path);
        if (!target) return;
        RectTransform rect = target.GetComponent<RectTransform>();
        if (!rect) return;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static void ConfigureFixedRect(Transform root, string path, Vector2 anchor, Vector2 position, Vector2 size)
    {
        Transform target = root.Find(path);
        if (!target) return;
        RectTransform rect = target.GetComponent<RectTransform>();
        if (!rect) return;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    static void ConfigureCenteredRect(Transform target, Vector2 position, Vector2 size)
    {
        if (!target) return;
        RectTransform rect = target.GetComponent<RectTransform>();
        if (!rect) return;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static void ConfigureCorner(Transform root, string path, Vector2 position, float size)
    {
        Transform target = root.Find(path);
        if (!target) return;
        RectTransform rect = target.GetComponent<RectTransform>();
        if (!rect) return;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(size, size);
        target.localRotation = Quaternion.identity;
    }

    static void ConfigureAnchoredLine(RectTransform rect, Vector2 position, Vector2 size, Vector2 pivot)
    {
        if (!rect) return;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static void ConfigureCenteredLine(RectTransform rect, Vector2 position, Vector2 size, Vector2 pivot)
    {
        if (!rect) return;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static AfoShapeImage EnsureShape(Transform parent, string name)
    {
        Transform found = parent.Find(name);
        AfoShapeImage image = found ? found.GetComponent<AfoShapeImage>() : null;
        if (image) return image;
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(AfoShapeImage));
        item.transform.SetParent(parent, false);
        image = item.GetComponent<AfoShapeImage>();
        image.shape = AfoShapeImage.Shape.Rectangle;
        image.raycastTarget = false;
        return image;
    }

    static void ConfigureChildRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        if (!rect) return;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static void SetRectSize(Transform target, Vector2 size)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        if (rect) rect.sizeDelta = size;
    }

    static void SetLocalAngle(Transform root, string path, float degrees)
    {
        Transform target = root.Find(path);
        if (target) target.localRotation = Quaternion.Euler(0f, 0f, degrees);
    }

    static void SetShape(Transform root, string path, AfoShapeImage.Shape shape, Color color)
    {
        Transform target = root.Find(path);
        AfoShapeImage image = target ? target.GetComponent<AfoShapeImage>() : null;
        if (!image) return;
        image.shape = shape;
        image.color = color;
        image.SetAllDirty();
    }

    static void SetCornerCut(Transform root, string path, float value)
    {
        Transform target = root.Find(path);
        AfoShapeImage image = target ? target.GetComponent<AfoShapeImage>() : null;
        if (image) image.cornerCut = value;
    }

    static void SetShapeOpacity(Transform root, string path, float opacity)
    {
        Transform target = root.Find(path);
        AfoShapeImage image = target ? target.GetComponent<AfoShapeImage>() : null;
        if (!image) return;
        Color color = image.color;
        color.a = opacity;
        image.color = color;
    }

    static void SetImageOpacity(Transform root, string path, float opacity)
    {
        Transform target = root.Find(path);
        Image image = target ? target.GetComponent<Image>() : null;
        if (!image) return;
        Color color = image.color;
        color.a = opacity;
        image.color = color;
    }

    static void SetTextOpacity(Transform root, string path, float opacity)
    {
        Transform target = root.Find(path);
        Text text = target ? target.GetComponent<Text>() : null;
        if (!text) return;
        Color color = text.color;
        color.a = opacity;
        text.color = color;
    }

    static void SetTextSize(Transform root, string path, int size)
    {
        Transform target = root.Find(path);
        Text text = target ? target.GetComponent<Text>() : null;
        if (text) text.fontSize = size;
    }
}
