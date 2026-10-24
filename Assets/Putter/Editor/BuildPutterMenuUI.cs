using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Stage C3: Full, cohesive redesign pass starter (editor-driven, non-runtime changes).
public static class BuildPutterMenuUI
{
    const string ScenePath = "Assets/Putter/Putter.unity";

    [MenuItem("Tools/AFO/Rebuild Main Menu UI")]
    public static void Build()
    {
        // Open the Putter scene
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // Enhance the PLAY button and tighter typography integration with the title/graphic
        Button playBtn = null;
        foreach (var btn in Object.FindObjectsOfType<Button>())
        {
            var t = btn.GetComponentInChildren<Text>();
            if (t != null && t.text.ToUpper().Contains("PLAY")) { playBtn = btn; break; }
        }
        if (playBtn != null)
        {
            // Create a behind plate for the PLAY button
            var plateObj = new GameObject("PlayPlate", typeof(RectTransform), typeof(AfoShapeImage));
            plateObj.transform.SetParent(playBtn.transform, false);
            var plateRect = plateObj.GetComponent<RectTransform>();
            var btnRect = playBtn.GetComponent<RectTransform>();
            plateRect.anchorMin = new Vector2(0.5f, 0.5f);
            plateRect.anchorMax = new Vector2(0.5f, 0.5f);
            plateRect.anchoredPosition = Vector2.zero;
            plateRect.sizeDelta = new Vector2(btnRect.sizeDelta.x * 1.04f, btnRect.sizeDelta.y * 0.92f);
            var plateImage = plateObj.GetComponent<AfoShapeImage>();
            plateImage.shape = AfoShapeImage.Shape.Parallelogram;
            plateImage.color = new Color32(36, 107, 253, 180);
            plateObj.transform.SetAsFirstSibling();

            // Improve PLAY label readability (muted by default; emphasis on hover/selection will occur later)
            var text = playBtn.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.fontSize = 110;
                text.color = Color.white;
                text.fontStyle = FontStyle.Normal;
            }
        }
        else
        {
            Debug.Log("[AFO] PLAY button not found for enhancement.");
        }

        // Stage C3: Right-side composition to unify the right artwork with the title and PLAY button
        var canvasRoot = GameObject.Find("MainMenuCanvas");
        if (canvasRoot != null)
        {
            var rightRoot = new GameObject("RightGraphicComposer", typeof(RectTransform));
            rightRoot.transform.SetParent(canvasRoot.transform, false);
            var rt = rightRoot.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.65f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var panel = new GameObject("RightPanel", typeof(RectTransform), typeof(AfoShapeImage));
            panel.transform.SetParent(rightRoot.transform, false);
            var panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;
            var shape = panel.GetComponent<AfoShapeImage>();
            shape.shape = AfoShapeImage.Shape.Parallelogram;
            shape.color = new Color32(36, 107, 253, 120);

            var indexObj = new GameObject("RightIndex", typeof(RectTransform), typeof(Text));
            // Stage C4: small refinements to right-side index
            indexObj.transform.localScale = Vector3.one;
            indexObj.transform.SetParent(panel.transform, false);
            var txt = indexObj.AddComponent<Text>();
            txt.font = (Font)Resources.GetBuiltinResource(typeof(Font), "LegacyRuntime.ttf");
            txt.text = "01";
            txt.fontSize = 72;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            var txtRT = indexObj.GetComponent<RectTransform>();
            txtRT.anchorMin = new Vector2(0.5f, 0.5f);
            txtRT.anchorMax = new Vector2(0.5f, 0.5f);
            txtRT.anchoredPosition = Vector2.zero;
            txtRT.sizeDelta = new Vector2(120f, 120f);
        }

        Debug.Log("[AFO] Stage C3: Right-side graphic added and PLAY styling updated.");
    }
}
