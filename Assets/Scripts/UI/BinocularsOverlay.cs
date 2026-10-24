using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SayItAgain.UI
{
    /// <summary>Two-circle vignette and landmark name while the binoculars are raised.</summary>
    public sealed class BinocularsOverlay : MonoBehaviour
    {
        public Binoculars binoculars;

        CanvasGroup group;
        TextMeshProUGUI label;

        void Start()
        {
            if (binoculars == null) binoculars = GetComponentInParent<Binoculars>();
            var canvas = UiKit.CreateCanvas("BinocularsOverlay", 10);
            canvas.transform.SetParent(transform, false);
            group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;

            var mask = UiKit.Stretch("Vignette", canvas.transform).gameObject.AddComponent<RawImage>();
            mask.texture = BuildMaskTexture(512, 256);
            mask.raycastTarget = false;

            var plate = UiKit.Rect("NamePlate", canvas.transform, new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.12f), new Vector2(-360, -40), new Vector2(360, 40));
            plate.gameObject.AddComponent<Image>().color = new Color(UiKit.Navy.r, UiKit.Navy.g, UiKit.Navy.b, 0.85f);
            plate.localRotation = Quaternion.Euler(0, 0, 2f);
            label = UiKit.Label(plate, null, 40, UiKit.Yellow, TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Bold;
            plate.gameObject.SetActive(false);

            binoculars.FocusChanged += lm =>
            {
                plate.gameObject.SetActive(lm != null);
                if (lm != null) label.text = lm.DisplayName;
            };
        }

        void Update()
        {
            if (group != null) group.alpha = binoculars.ZoomAmount;
        }

        /// <summary>Black everywhere except two overlapping soft circles, like looking through binoculars.</summary>
        static Texture2D BuildMaskTexture(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[width * height];
            float r = height * 0.46f;
            var left = new Vector2(width * 0.5f - r * 0.62f, height * 0.5f);
            var right = new Vector2(width * 0.5f + r * 0.62f, height * 0.5f);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var p = new Vector2(x, y);
                float d = Mathf.Min(Vector2.Distance(p, left), Vector2.Distance(p, right));
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(r - 10f, r + 4f, d));
                pixels[y * width + x] = new Color32(0, 0, 0, (byte)(a * 255));
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }
    }
}
