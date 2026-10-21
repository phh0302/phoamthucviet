using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PhoAmThucViet.UI
{
    /// <summary>Bảng màu chung của giao diện (ấm, kiểu quán ăn Việt).</summary>
    public static class Palette
    {
        public static readonly Color PanelBg = new Color(0.20f, 0.13f, 0.09f);
        public static readonly Color Card = new Color(0.99f, 0.95f, 0.86f);
        public static readonly Color CardSelected = new Color(1f, 0.87f, 0.55f);
        public static readonly Color CardLocked = new Color(0.80f, 0.76f, 0.70f);
        public static readonly Color TextDark = new Color(0.27f, 0.16f, 0.08f);
        public static readonly Color TextSoft = new Color(0.45f, 0.33f, 0.22f);
        public static readonly Color Green = new Color(0.30f, 0.68f, 0.30f);
        public static readonly Color Orange = new Color(0.95f, 0.58f, 0.18f);
        public static readonly Color Blue = new Color(0.25f, 0.55f, 0.90f);
        public static readonly Color Red = new Color(0.80f, 0.22f, 0.18f);
        public static readonly Color Gold = new Color(1f, 0.80f, 0.20f);
        public static readonly Color Disabled = new Color(0.62f, 0.60f, 0.58f);
        public static readonly Color HudBg = new Color(0f, 0f, 0f, 0.55f);
    }

    /// <summary>Một nút gồm nền bo tròn + chữ, đổi màu theo trạng thái bấm được hay không.</summary>
    public class UIButton
    {
        public Button button;
        public Image bg;
        public Text label;
        public Color color;

        public void Set(string text, bool interactable)
        {
            if (label.text != text) label.text = text;
            button.interactable = interactable;
            bg.color = interactable ? color : Palette.Disabled;
        }
    }

    /// <summary>
    /// Tạo giao diện uGUI hoàn toàn bằng code (không cần prefab): Canvas, khung bo tròn, chữ, nút.
    /// Chữ dùng font hệ thống nên hiện được tiếng Việt.
    /// </summary>
    public static class UIKit
    {
        public const float RefWidth = 1080f, RefHeight = 1920f;

        private static Font _font;
        private static Sprite _rounded, _pill;

        public static Font Font => _font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        /// <summary>Khung bo góc vừa, cắt 9 mảnh để giãn không méo.</summary>
        public static Sprite Rounded => _rounded ??= MakeRounded(96, 28);

        /// <summary>Khung bo góc tròn hẳn (viên thuốc), cho nút nhỏ và thanh tiến độ.</summary>
        public static Sprite Pill => _pill ??= MakeRounded(96, 46);

        private static Sprite MakeRounded(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                byte a = (byte)(Mathf.Clamp01(radius - d + 0.5f) * 255);
                px[y * size + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        // ---------- Canvas ----------

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand; // cùng tỉ lệ với cảnh quán
            EnsureEventSystem();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null || UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
            var module = es.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        // ---------- Khối cơ bản ----------

        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Đặt neo theo tỉ lệ (0–1) của cha và lề theo pixel.</summary>
        public static RectTransform Place(this RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
                                          Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        /// <summary>Đặt theo góc trên-trái của cha: (x, y) tính từ góc đó, y dương đi xuống.</summary>
        public static RectTransform TopLeft(this RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        /// <summary>Đặt theo góc trên-phải của cha.</summary>
        public static RectTransform TopRight(this RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static Image Panel(string name, Transform parent, Color color, Sprite sprite = null)
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            return img;
        }

        public static Text Label(string name, Transform parent, string text, int size, Color color,
                                 TextAnchor align = TextAnchor.MiddleLeft, bool bold = false)
        {
            var rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.fontSize = size;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.color = color;
            t.alignment = align;
            t.text = text;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.resizeTextForBestFit = true; // chữ dài tự thu nhỏ cho vừa khung
            t.resizeTextMinSize = Mathf.Max(12, size / 2);
            t.resizeTextMaxSize = size;
            return t;
        }

        public static UIButton MakeButton(string name, Transform parent, string text, int fontSize, Color color, Action onClick,
                                      Sprite sprite = null)
        {
            var bg = Panel(name, parent, color, sprite ?? Rounded);
            var btn = bg.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            var colors = btn.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.disabledColor = Color.white; // màu xám đã đặt trên nền
            btn.colors = colors;

            var label = Label("Text", bg.transform, text, fontSize, Color.white, TextAnchor.MiddleCenter, true);
            label.rectTransform.Place(Vector2.zero, Vector2.one, new Vector2(8, 4), new Vector2(-8, -4));
            label.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(1.5f, -1.5f);

            var ui = new UIButton { button = btn, bg = bg, label = label, color = color };
            btn.onClick.AddListener(() =>
            {
                UIPunch.Play(bg.rectTransform);
                onClick?.Invoke();
            });
            return ui;
        }

        /// <summary>Thanh tiến độ: nền + phần lấp đầy (đặt fillAmount 0–1).</summary>
        public static Image ProgressBar(string name, Transform parent, Color bgColor, Color fillColor, out RectTransform root)
        {
            var bg = Panel(name, parent, bgColor, Pill);
            root = bg.rectTransform;
            var fill = Panel("Fill", bg.transform, fillColor);
            fill.rectTransform.Place(Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-4, -4));
            fill.sprite = PhoAmThucViet.Visual.SpriteFactory.Square; // Filled cần có sprite
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0;
            return fill;
        }
    }

    /// <summary>Hiệu ứng nảy nhẹ khi bấm nút (không cần thư viện tween).</summary>
    public class UIPunch : MonoBehaviour
    {
        private float _t = 1f;
        private const float Duration = 0.18f;

        public static void Play(RectTransform target)
        {
            var p = target.GetComponent<UIPunch>();
            if (p == null) p = target.gameObject.AddComponent<UIPunch>();
            p._t = 0;
        }

        private void Update()
        {
            if (_t >= 1f) return;
            _t = Mathf.Min(1f, _t + Time.unscaledDeltaTime / Duration);
            float k = 1f + Mathf.Sin(_t * Mathf.PI) * 0.08f;
            transform.localScale = new Vector3(k, k, 1);
        }
    }
}
