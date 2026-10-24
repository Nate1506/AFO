using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SayItAgain.UI
{
    /// <summary>Palette and small builders for runtime uGUI screens (lobby, HUD).</summary>
    public static class UiKit
    {
        public static readonly Color Navy = new Color32(0x0E, 0x1A, 0x2B, 0xFF);
        public static readonly Color StormGrey = new Color32(0x3A, 0x45, 0x52, 0xFF);
        public static readonly Color Yellow = new Color32(0xFF, 0xC9, 0x3C, 0xFF);
        public static readonly Color Red = new Color32(0xE6, 0x39, 0x46, 0xFF);
        public static readonly Color White = new Color32(0xF4, 0xF6, 0xFA, 0xFF);
        public static readonly Color Ink = new Color32(0x05, 0x0B, 0x14, 0xFF);

        static TMP_FontAsset font;

        public static TMP_FontAsset Font
        {
            get
            {
                if (font == null) font = Resources.Load<TMP_FontAsset>("Fonts/NotoSansThai SDF");
                return font != null ? font : TMP_Settings.defaultFontAsset;
            }
        }

        public static Canvas CreateCanvas(string name, int sortOrder)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            EnsureEventSystem();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Object.DontDestroyOnLoad(es);
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        public static RectTransform Stretch(string name, Transform parent) =>
            Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        public static Image Panel(string name, Transform parent, Color color)
        {
            var img = Stretch(name, parent).gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static TextMeshProUGUI Label(Transform parent, string locKey, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var rt = Stretch("Label", parent);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = Font;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            if (!string.IsNullOrEmpty(locKey)) rt.gameObject.AddComponent<LocalizedText>().SetKey(locKey);
            return tmp;
        }

        /// <summary>Angular button: slanted yellow highlight on hover/select, tilted label.</summary>
        public static Button Button(Transform parent, string locKey, UnityAction onClick, float fontSize = 40)
        {
            var rt = Stretch("Button_" + (locKey ?? "dynamic"), parent);
            // White base so the button's tint colours show as-is.
            var bg = rt.gameObject.AddComponent<Image>();
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            var colors = button.colors;
            colors.normalColor = Ink;
            colors.highlightedColor = Yellow;
            colors.selectedColor = Yellow;
            colors.pressedColor = White;
            colors.disabledColor = new Color(0.25f, 0.27f, 0.3f, 0.6f);
            colors.fadeDuration = 0.06f;
            button.colors = colors;
            button.onClick.AddListener(onClick);
            rt.localRotation = Quaternion.Euler(0, 0, 2f);
            // Yellow edge keeps dark buttons readable on both navy and grey backgrounds.
            var edge = Rect("Edge", rt, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(8, 0)).gameObject.AddComponent<Image>();
            edge.color = Yellow;
            edge.raycastTarget = false;

            var label = Label(rt, locKey, fontSize, White, TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Bold;
            rt.gameObject.AddComponent<ButtonLabelTint>().label = label;
            return button;
        }

        public static TMP_InputField Input(Transform parent, string placeholderKey, int charLimit)
        {
            var rt = Stretch("Input", parent);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = White;
            var area = Rect("Text Area", rt, Vector2.zero, Vector2.one, new Vector2(20, 6), new Vector2(-20, -6));
            area.gameObject.AddComponent<RectMask2D>();

            var placeholder = Label(area, placeholderKey, 40, new Color(0.3f, 0.33f, 0.38f), TextAlignmentOptions.Center);
            placeholder.fontStyle = FontStyles.Italic;
            var text = Label(area, null, 48, Navy, TextAlignmentOptions.Center);
            text.fontStyle = FontStyles.Bold;

            var field = rt.gameObject.AddComponent<TMP_InputField>();
            field.textViewport = area;
            field.textComponent = text;
            field.placeholder = placeholder;
            field.fontAsset = Font;
            field.characterLimit = charLimit;
            field.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
            return field;
        }
    }

    /// <summary>Flips the label to navy while its button shows the yellow highlight.</summary>
    public sealed class ButtonLabelTint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public TextMeshProUGUI label;
        bool hover, selected;

        public void OnPointerEnter(PointerEventData _) { hover = true; Refresh(); }
        public void OnPointerExit(PointerEventData _) { hover = false; Refresh(); }
        public void OnSelect(BaseEventData _) { selected = true; Refresh(); }
        public void OnDeselect(BaseEventData _) { selected = false; Refresh(); }
        void OnDisable() { hover = selected = false; Refresh(); }

        void Refresh()
        {
            if (label != null) label.color = hover || selected ? UiKit.Navy : UiKit.White;
        }
    }
}
