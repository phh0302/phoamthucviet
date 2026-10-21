using System.Collections.Generic;
using UnityEngine;

namespace PhoAmThucViet.Visual
{
    /// <summary>
    /// Hình trang trí vẽ hoàn toàn bằng code (không cần file ảnh), theo 5 mốc quán:
    /// 0 xe đẩy vỉa hè · 1 quán cóc · 2 quán ngõ nhỏ · 3 quán mặt phố · 4 nhà hàng lớn.
    /// Nền và tường là ô lặp (SpriteDrawMode.Tiled); đồ treo tường là sprite riêng. Mỗi hình chỉ vẽ một lần rồi dùng lại.
    /// </summary>
    public static class SceneArt
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        // ================= Bộ vẽ điểm ảnh =================

        /// <summary>Tấm vẽ nhỏ: tô hình chữ nhật, hình tròn (viền mượt), pha màu, nhiễu.</summary>
        private class Paint
        {
            public readonly int w, h;
            private readonly Color[] _px;
            private readonly System.Random _rng;

            public Paint(int w, int h, Color fill, int seed = 1)
            {
                this.w = w; this.h = h;
                _px = new Color[w * h];
                for (int i = 0; i < _px.Length; i++) _px[i] = fill;
                _rng = new System.Random(seed);
            }

            public float Rand() => (float)_rng.NextDouble();

            public void Blend(int x, int y, Color c, float a = 1f)
            {
                if (x < 0 || y < 0 || x >= w || y >= h) return;
                a *= c.a;
                if (a <= 0) return;
                int i = y * w + x;
                var d = _px[i];
                float outA = a + d.a * (1 - a);
                if (outA <= 0) return;
                _px[i] = new Color(
                    (c.r * a + d.r * d.a * (1 - a)) / outA,
                    (c.g * a + d.g * d.a * (1 - a)) / outA,
                    (c.b * a + d.b * d.a * (1 - a)) / outA, outA);
            }

            /// <summary>Hình chữ nhật, góc dưới-trái (x, y).</summary>
            public void Rect(float x, float y, float rw, float rh, Color c)
            {
                for (int py = Mathf.FloorToInt(y); py < Mathf.CeilToInt(y + rh); py++)
                for (int px = Mathf.FloorToInt(x); px < Mathf.CeilToInt(x + rw); px++)
                {
                    float cover = Mathf.Clamp01(Mathf.Min(px + 1, x + rw) - Mathf.Max(px, x)) * Mathf.Clamp01(Mathf.Min(py + 1, y + rh) - Mathf.Max(py, y));
                    Blend(px, py, c, cover);
                }
            }

            /// <summary>Hình chữ nhật lặp tràn mép (dùng cho ô lặp liền mạch).</summary>
            public void RectWrap(float x, float y, float rw, float rh, Color c)
            {
                for (int ox = -1; ox <= 1; ox++)
                for (int oy = -1; oy <= 1; oy++)
                    if (x + ox * w < w && x + rw + ox * w > 0 && y + oy * h < h && y + rh + oy * h > 0)
                        Rect(x + ox * w, y + oy * h, rw, rh, c);
            }

            public void Ellipse(float cx, float cy, float rx, float ry, Color c)
            {
                for (int py = Mathf.FloorToInt(cy - ry - 1); py <= Mathf.CeilToInt(cy + ry + 1); py++)
                for (int px = Mathf.FloorToInt(cx - rx - 1); px <= Mathf.CeilToInt(cx + rx + 1); px++)
                {
                    float dx = (px + 0.5f - cx) / rx, dy = (py + 0.5f - cy) / ry;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float edge = (1 - d) * Mathf.Min(rx, ry);
                    Blend(px, py, c, Mathf.Clamp01(edge + 0.5f));
                }
            }

            public void Circle(float cx, float cy, float r, Color c) => Ellipse(cx, cy, r, r, c);

            /// <summary>Xóa (trong suốt) mọi điểm ảnh ngoài hình tròn.</summary>
            public void ClearOutside(float cx, float cy, float r)
            {
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                    float keep = Mathf.Clamp01(r - d + 0.5f);
                    var c = _px[y * w + x];
                    _px[y * w + x] = new Color(c.r, c.g, c.b, c.a * keep);
                }
            }

            /// <summary>Vòng tròn rỗng (viền dày t).</summary>
            public void Ring(float cx, float cy, float r, float t, Color c)
            {
                for (int py = Mathf.FloorToInt(cy - r - 1); py <= Mathf.CeilToInt(cy + r + 1); py++)
                for (int px = Mathf.FloorToInt(cx - r - 1); px <= Mathf.CeilToInt(cx + r + 1); px++)
                {
                    float d = Mathf.Sqrt((px + 0.5f - cx) * (px + 0.5f - cx) + (py + 0.5f - cy) * (py + 0.5f - cy));
                    float a = Mathf.Clamp01(t / 2 + 0.5f - Mathf.Abs(d - (r - t / 2)));
                    Blend(px, py, c, a);
                }
            }

            public void RoundRect(float x, float y, float rw, float rh, float r, Color c)
            {
                r = Mathf.Min(r, rw / 2, rh / 2);
                Rect(x + r, y, rw - 2 * r, rh, c);
                Rect(x, y + r, r, rh - 2 * r, c);
                Rect(x + rw - r, y + r, r, rh - 2 * r, c);
                Circle(x + r, y + r, r, c);
                Circle(x + rw - r, y + r, r, c);
                Circle(x + r, y + rh - r, r, c);
                Circle(x + rw - r, y + rh - r, r, c);
            }

            /// <summary>Đường thẳng dày t.</summary>
            public void Line(float x0, float y0, float x1, float y1, float t, Color c)
            {
                float len = Mathf.Max(1, Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1)));
                for (float s = 0; s <= len; s += 0.5f)
                {
                    float k = s / len;
                    Circle(Mathf.Lerp(x0, x1, k), Mathf.Lerp(y0, y1, k), t / 2, c);
                }
            }

            /// <summary>Làm sáng/tối ngẫu nhiên từng điểm ảnh cho bớt phẳng.</summary>
            public void Noise(float amount)
            {
                for (int i = 0; i < _px.Length; i++)
                {
                    if (_px[i].a <= 0) continue;
                    float n = (Rand() - 0.5f) * 2 * amount;
                    var c = _px[i];
                    _px[i] = new Color(Mathf.Clamp01(c.r + n), Mathf.Clamp01(c.g + n), Mathf.Clamp01(c.b + n), c.a);
                }
            }

            /// <summary>Mảng loang lớn (vết ố tường vôi), lặp liền mạch.</summary>
            public void Blotches(int count, float minR, float maxR, Color c)
            {
                for (int i = 0; i < count; i++)
                {
                    float cx = Rand() * w, cy = Rand() * h, r = Mathf.Lerp(minR, maxR, Rand());
                    for (int ox = -1; ox <= 1; ox++)
                    for (int oy = -1; oy <= 1; oy++)
                        Ellipse(cx + ox * w, cy + oy * h, r, r * Mathf.Lerp(0.6f, 1f, Rand()), c);
                }
            }

            /// <summary>
            /// Làm dịu mắt: kéo mọi điểm ảnh về màu trung bình (giảm tương phản) và bớt độ rực.
            /// contrast 1 = giữ nguyên, 0 = một màu phẳng; saturation 1 = giữ nguyên, 0 = xám.
            /// </summary>
            public void Soften(float contrast, float saturation, float lift)
            {
                if (contrast >= 0.999f && saturation >= 0.999f && lift <= 0.001f) return;
                float r = 0, g = 0, b = 0, wsum = 0;
                foreach (var c in _px) { r += c.r * c.a; g += c.g * c.a; b += c.b * c.a; wsum += c.a; }
                if (wsum <= 0) return;
                var mean = new Color(r / wsum, g / wsum, b / wsum);
                for (int i = 0; i < _px.Length; i++)
                {
                    var c = _px[i];
                    if (c.a <= 0) continue;
                    var k = Color.Lerp(mean, c, contrast);
                    float l = k.r * 0.299f + k.g * 0.587f + k.b * 0.114f;
                    k = Color.Lerp(new Color(l, l, l), k, saturation);
                    k = Color.Lerp(k, Color.white, lift);
                    _px[i] = new Color(k.r, k.g, k.b, c.a);
                }
            }

            /// <summary>Sprite co giãn 9 ô (giữ nguyên viền border điểm ảnh), dùng cho bảng tên món dài ngắn khác nhau.</summary>
            public Sprite ToSlicedSprite(float pixelsPerUnit, float border)
            {
                Soften(StyleContrast, StyleSaturation, StyleLift);
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                tex.SetPixels(_px);
                tex.Apply(false, true);
                return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), pixelsPerUnit, 0,
                    SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            }

            public Sprite ToSprite(float pixelsPerUnit, bool tiled, Vector2 pivot)
            {
                Soften(StyleContrast, StyleSaturation, StyleLift);
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                };
                tex.SetPixels(_px);
                tex.Apply(false, true);
                return Sprite.Create(tex, new Rect(0, 0, w, h), pivot, pixelsPerUnit, 0,
                    tiled ? SpriteMeshType.FullRect : SpriteMeshType.Tight);
            }
        }

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
        private static Color Shade(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);

        // ---------- Phong cách dịu mắt ----------
        // Nền / tường: tương phản thấp, ít rực, sáng nhẹ (để nhân vật và món ăn nổi bật hơn).
        // Đồ treo tường: giữ rõ hơn một chút nhưng cũng bớt rực.
        private static float StyleContrast = 1f, StyleSaturation = 1f, StyleLift = 0f;

        private static Sprite WithStyle(float contrast, float saturation, float lift, System.Func<Sprite> make)
        {
            StyleContrast = contrast; StyleSaturation = saturation; StyleLift = lift;
            try { return make(); }
            finally { StyleContrast = 1f; StyleSaturation = 1f; StyleLift = 0f; }
        }

        private static Sprite Cached(string key, System.Func<Sprite> make)
        {
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            s = make();
            Cache[key] = s;
            return s;
        }

        // ================= Đặt vào cảnh =================

        /// <summary>Sprite lặp phủ kín vùng (w, h) đơn vị, tâm tại pos.</summary>
        public static SpriteRenderer Tiled(string name, Transform parent, Vector2 pos, Vector2 size, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = SpriteFactory.MaterialFor(sprite);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = size;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>Sprite co giãn 9 ô với kích thước (w, h) đơn vị.</summary>
        public static SpriteRenderer Sliced(string name, Transform parent, Vector2 pos, Vector2 size, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = SpriteFactory.MaterialFor(sprite);
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>Đặt một hình trang trí, co theo chiều cao mong muốn (đơn vị).</summary>
        public static SpriteRenderer Place(string name, Transform parent, Vector2 pos, Sprite sprite, float height, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            float k = height / Mathf.Max(0.01f, sprite.bounds.size.y);
            go.transform.localScale = new Vector3(k, k, 1);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = SpriteFactory.MaterialFor(sprite);
            sr.sortingOrder = order;
            return sr;
        }

        // ================= Nền =================

        public static Sprite Floor(int tier) => Cached("floor" + tier, () => WithStyle(0.45f, 0.65f, 0.06f, () =>
        {
            switch (tier)
            {
                case 0: return Pavers();
                case 1: return Terracotta();
                case 2: return Checker();
                case 3: return GachBong();
                default: return Carpet();
            }
        }));

        /// <summary>Gạch lát vỉa hè xám, xếp so le.</summary>
        private static Sprite Pavers()
        {
            var p = new Paint(128, 128, C(0.36f, 0.37f, 0.40f), 11);
            for (int row = 0; row < 4; row++)
            for (int col = -1; col < 3; col++)
            {
                float x = col * 64 + (row % 2) * 32, y = row * 32;
                float g = 0.53f + p.Rand() * 0.10f;
                p.RectWrap(x + 2, y + 2, 60, 28, C(g, g * 1.01f, g * 1.05f));
                p.RectWrap(x + 2, y + 26, 60, 4, C(g * 1.08f, g * 1.09f, g * 1.12f)); // mép sáng
            }
            p.Noise(0.035f);
            return p.ToSprite(80, true, new Vector2(0.5f, 0.5f));
        }

        /// <summary>Gạch tàu đỏ cam (quán cóc).</summary>
        private static Sprite Terracotta()
        {
            var p = new Paint(128, 128, C(0.50f, 0.32f, 0.24f), 12);
            for (int ty = 0; ty < 2; ty++)
            for (int tx = 0; tx < 2; tx++)
            {
                float k = 0.92f + p.Rand() * 0.14f;
                p.Rect(tx * 64 + 3, ty * 64 + 3, 58, 58, C(0.80f * k, 0.44f * k, 0.28f * k));
                p.Rect(tx * 64 + 3, ty * 64 + 56, 58, 5, C(0.88f * k, 0.52f * k, 0.34f * k));
            }
            p.Blotches(5, 4, 10, C(0.45f, 0.25f, 0.15f, 0.12f));
            p.Noise(0.04f);
            return p.ToSprite(95, true, new Vector2(0.5f, 0.5f));
        }

        /// <summary>Gạch ca rô kem - nâu (quán ngõ nhỏ).</summary>
        private static Sprite Checker()
        {
            var p = new Paint(128, 128, C(0.62f, 0.55f, 0.45f), 13);
            for (int ty = 0; ty < 2; ty++)
            for (int tx = 0; tx < 2; tx++)
            {
                bool light = (tx + ty) % 2 == 0;
                var c = light ? C(0.93f, 0.88f, 0.76f) : C(0.74f, 0.60f, 0.42f);
                p.Rect(tx * 64 + 1.5f, ty * 64 + 1.5f, 61, 61, c);
            }
            p.Noise(0.03f);
            return p.ToSprite(64, true, new Vector2(0.5f, 0.5f));
        }

        /// <summary>Gạch bông (gạch hoa xi măng) xanh - đỏ - kem, đặc trưng nhà phố Việt.</summary>
        private static Sprite GachBong()
        {
            var cream = C(0.95f, 0.91f, 0.82f);
            var blue = C(0.20f, 0.42f, 0.62f);
            var red = C(0.78f, 0.27f, 0.22f);
            var p = new Paint(128, 128, cream, 14);
            // Góc ô: 4 phần tư hình tròn xanh ghép thành hoa tròn ở chỗ nối 4 viên gạch
            for (int cx = 0; cx <= 128; cx += 128)
            for (int cy = 0; cy <= 128; cy += 128)
            {
                p.Circle(cx, cy, 34, blue);
                p.Circle(cx, cy, 24, cream);
                p.Circle(cx, cy, 12, red);
            }
            // Bông hoa 4 cánh ở giữa
            for (int k = 0; k < 4; k++)
            {
                float a = k * Mathf.PI / 2 + Mathf.PI / 4;
                p.Ellipse(64 + Mathf.Cos(a) * 18, 64 + Mathf.Sin(a) * 18, 15, 15, red);
            }
            p.Circle(64, 64, 10, C(0.95f, 0.75f, 0.25f));
            for (int k = 0; k < 4; k++)
            {
                float a = k * Mathf.PI / 2;
                p.Circle(64 + Mathf.Cos(a) * 40, 64 + Mathf.Sin(a) * 40, 6, blue);
            }
            // Ron gạch
            p.Rect(0, 0, 128, 1.5f, C(0.6f, 0.58f, 0.55f));
            p.Rect(0, 0, 1.5f, 128, C(0.6f, 0.58f, 0.55f));
            p.Noise(0.025f);
            return p.ToSprite(90, true, new Vector2(0.5f, 0.5f));
        }

        /// <summary>Thảm đỏ hoa văn trám, chấm vàng (nhà hàng lớn).</summary>
        private static Sprite Carpet()
        {
            var p = new Paint(128, 128, C(0.58f, 0.10f, 0.10f), 15);
            var line = C(0.46f, 0.07f, 0.08f);
            for (int i = -128; i <= 256; i += 64)
            {
                p.Line(i, 0, i + 128, 128, 3, line);
                p.Line(i, 128, i + 128, 0, 3, line);
            }
            var gold = C(0.95f, 0.75f, 0.30f);
            for (int x = 0; x <= 128; x += 64)
            for (int y = 0; y <= 128; y += 64)
            {
                p.Circle(x, y, 5, gold);
                p.Circle(x + 32, y + 32, 3, C(0.85f, 0.55f, 0.25f));
            }
            p.Noise(0.03f);
            return p.ToSprite(70, true, new Vector2(0.5f, 0.5f));
        }

        // ================= Tường =================

        public static Sprite Wall(int tier) => Cached("wall" + tier, () => WithStyle(0.45f, 0.6f, 0.10f, () =>
        {
            switch (tier)
            {
                case 0: return Shutter();
                case 1: return Bamboo();
                case 2: return LimeWall();
                case 3: return SubwayTiles();
                default: return WoodPanel();
            }
        }));

        /// <summary>Cửa cuốn sắt của dãy nhà phố phía sau xe đẩy.</summary>
        private static Sprite Shutter()
        {
            var p = new Paint(128, 64, C(0.48f, 0.52f, 0.56f), 21);
            for (int y = 0; y < 64; y += 8)
            {
                p.Rect(0, y, 128, 2, C(0.36f, 0.39f, 0.43f));
                p.Rect(0, y + 5, 128, 2, C(0.62f, 0.66f, 0.70f));
            }
            p.Noise(0.02f);
            return p.ToSprite(100, true, new Vector2(0.5f, 0.5f));
        }

        /// <summary>Mành tre (quán cóc).</summary>
        private static Sprite Bamboo()
        {
            var p = new Paint(128, 128, C(0.55f, 0.40f, 0.22f), 22);
            for (int x = 0; x < 128; x += 8)
            {
                float k = 0.9f + p.Rand() * 0.15f;
                p.Rect(x + 1, 0, 6, 128, C(0.86f * k, 0.70f * k, 0.42f * k));
                p.Rect(x + 1, 0, 2, 128, C(0.93f * k, 0.80f * k, 0.52f * k));
            }
            for (int y = 20; y < 128; y += 54) p.Rect(0, y, 128, 3, C(0.40f, 0.26f, 0.14f)); // dây buộc
            p.Noise(0.03f);
            return p.ToSprite(90, true, new Vector2(0.5f, 0.5f));
        }

        /// <summary>Tường vôi vàng có vết ố (ngõ phố cổ Hà Nội).</summary>
        private static Sprite LimeWall()
        {
            var p = new Paint(128, 128, C(0.93f, 0.77f, 0.36f), 23);
            p.Blotches(10, 8, 26, C(0.80f, 0.62f, 0.26f, 0.18f));
            p.Blotches(6, 4, 12, C(1f, 0.90f, 0.55f, 0.20f));
            p.Noise(0.03f);
            return p.ToSprite(64, true, new Vector2(0.5f, 0.5f));
        }

        /// <summary>Gạch men trắng ốp bếp (quán mặt phố).</summary>
        private static Sprite SubwayTiles()
        {
            var p = new Paint(128, 64, C(0.70f, 0.72f, 0.74f), 24);
            for (int row = 0; row < 4; row++)
            for (int col = -1; col < 3; col++)
            {
                float x = col * 64 + (row % 2) * 32, y = row * 16;
                p.RectWrap(x + 1, y + 1, 62, 14, C(0.96f, 0.97f, 0.97f));
                p.RectWrap(x + 3, y + 10, 30, 3, C(1f, 1f, 1f, 0.8f)); // vệt bóng
            }
            return p.ToSprite(90, true, new Vector2(0.5f, 0.5f));
        }

        /// <summary>Ốp gỗ đỏ sẫm có nẹp vàng (nhà hàng lớn).</summary>
        private static Sprite WoodPanel()
        {
            var p = new Paint(128, 128, C(0.30f, 0.10f, 0.07f), 25);
            for (int x = 0; x < 128; x += 32)
            {
                float k = 0.9f + p.Rand() * 0.15f;
                p.Rect(x + 1, 0, 30, 128, C(0.45f * k, 0.15f * k, 0.10f * k));
                for (int g = 0; g < 5; g++) // vân gỗ
                {
                    float gx = x + 4 + p.Rand() * 24;
                    p.Rect(gx, 0, 1, 128, C(0.36f, 0.11f, 0.08f, 0.6f));
                }
            }
            p.Rect(0, 60, 128, 6, C(0.85f, 0.65f, 0.25f));
            p.Rect(0, 62, 128, 2, C(1f, 0.85f, 0.45f));
            return p.ToSprite(72, true, new Vector2(0.5f, 0.5f));
        }

        // ================= Mái hiên sọc (vỉa hè, quán cóc) =================

        public static Sprite Awning(int tier) => Cached("awning" + tier, () => WithStyle(0.55f, 0.6f, 0.08f, () =>
        {
            var a = tier == 0 ? C(0.85f, 0.20f, 0.18f) : C(0.20f, 0.50f, 0.35f);
            var p = new Paint(64, 48, C(0, 0, 0, 0), 31);
            for (int x = 0; x < 64; x += 16)
            {
                var c = (x / 16) % 2 == 0 ? a : C(0.97f, 0.95f, 0.90f);
                p.Rect(x, 14, 16, 34, c);
                p.Circle(x + 8, 14, 8, c); // mép lượn sóng
            }
            p.Rect(0, 40, 64, 8, Shade(a, 0.7f));
            return p.ToSprite(64, true, new Vector2(0.5f, 0.5f));
        }));

        // ================= Đồ treo tường =================

        /// <summary>Tên các đồ treo tường theo mốc (xoay vòng giữa các quầy).</summary>
        public static string[] WallDecoSet(int tier)
        {
            switch (tier)
            {
                case 0: return new[] { "bulb", "poster", "bulb", "clock" };
                case 1: return new[] { "bulb", "shelf", "bulb", "menu" };
                case 2: return new[] { "window_green", "shelf", "clock", "menu" };
                case 3: return new[] { "window_glass", "menu", "shelf", "clock" };
                default: return new[] { "scroll", "window_round", "lantern", "scroll" };
            }
        }

        public static Sprite Deco(string kind) => Cached("deco_" + kind, () => WithStyle(0.85f, 0.7f, 0.04f, () =>
        {
            switch (kind)
            {
                case "bulb": return Bulb();
                case "poster": return Poster();
                case "clock": return Clock();
                case "shelf": return Shelf();
                case "menu": return MenuBoard();
                case "window_green": return WindowShutters();
                case "window_glass": return WindowGlass();
                case "window_round": return WindowRound();
                case "lantern": return Lantern();
                default: return Scroll();
            }
        }));

        /// <summary>Bóng đèn tròn treo dây, ánh vàng ấm.</summary>
        private static Sprite Bulb()
        {
            var p = new Paint(48, 128, C(0, 0, 0, 0), 41);
            p.Rect(23, 40, 2, 88, C(0.15f, 0.12f, 0.10f));
            p.Circle(24, 26, 22, C(1f, 0.85f, 0.4f, 0.25f)); // quầng sáng
            p.Rect(19, 36, 10, 10, C(0.35f, 0.33f, 0.30f));
            p.Circle(24, 26, 11, C(1f, 0.92f, 0.60f));
            p.Circle(21, 29, 4, C(1f, 1f, 0.9f));
            return p.ToSprite(100, false, new Vector2(0.5f, 1f));
        }

        /// <summary>Tờ quảng cáo dán tường (giấy ố, vài dòng chữ giả).</summary>
        private static Sprite Poster()
        {
            var p = new Paint(64, 84, C(0, 0, 0, 0), 42);
            p.Rect(2, 2, 60, 80, C(0.96f, 0.93f, 0.82f));
            p.Rect(2, 64, 60, 18, C(0.85f, 0.20f, 0.18f));
            for (int i = 0; i < 5; i++) p.Rect(8, 12 + i * 10, 30 + p.Rand() * 18, 4, C(0.25f, 0.25f, 0.30f, 0.8f));
            p.Rect(26, 76, 12, 6, C(0.9f, 0.9f, 0.8f, 0.5f)); // băng dính
            return p.ToSprite(100, false, new Vector2(0.5f, 0.5f));
        }

        private static Sprite Clock()
        {
            var p = new Paint(64, 64, C(0, 0, 0, 0), 43);
            p.Circle(32, 32, 30, C(0.25f, 0.18f, 0.12f));
            p.Circle(32, 32, 25, C(0.98f, 0.97f, 0.92f));
            for (int k = 0; k < 12; k++)
            {
                float a = k * Mathf.PI / 6;
                p.Circle(32 + Mathf.Cos(a) * 20, 32 + Mathf.Sin(a) * 20, k % 3 == 0 ? 2.2f : 1.3f, C(0.2f, 0.2f, 0.2f));
            }
            p.Line(32, 32, 32, 49, 3, C(0.15f, 0.15f, 0.15f));
            p.Line(32, 32, 44, 28, 3, C(0.15f, 0.15f, 0.15f));
            p.Circle(32, 32, 3, C(0.8f, 0.2f, 0.2f));
            return p.ToSprite(100, false, new Vector2(0.5f, 0.5f));
        }

        /// <summary>Kệ gỗ với hũ gia vị, chai nước mắm.</summary>
        private static Sprite Shelf()
        {
            var p = new Paint(128, 72, C(0, 0, 0, 0), 44);
            Color[] jars = { C(0.85f, 0.25f, 0.15f), C(0.95f, 0.75f, 0.25f), C(0.35f, 0.60f, 0.30f), C(0.55f, 0.35f, 0.20f), C(0.90f, 0.90f, 0.85f) };
            float x = 8;
            int i = 0;
            while (x < 112)
            {
                var c = jars[i % jars.Length];
                if (i % 3 == 1)
                {
                    // chai cao
                    p.RoundRect(x, 14, 14, 36, 5, Shade(c, 0.9f));
                    p.Rect(x + 4, 48, 6, 12, Shade(c, 0.7f));
                    p.Rect(x + 3, 26, 8, 10, C(0.95f, 0.92f, 0.80f)); // nhãn
                    x += 20;
                }
                else
                {
                    // hũ thấp có nắp
                    p.RoundRect(x, 14, 22, 24, 6, c);
                    p.Rect(x + 2, 36, 18, 6, C(0.30f, 0.22f, 0.15f));
                    p.Rect(x + 4, 20, 4, 12, C(1f, 1f, 1f, 0.35f));
                    x += 28;
                }
                i++;
            }
            p.Rect(0, 4, 128, 10, C(0.55f, 0.34f, 0.18f));
            p.Rect(0, 12, 128, 2, C(0.70f, 0.48f, 0.28f));
            p.Rect(10, 0, 6, 6, C(0.35f, 0.22f, 0.12f));
            p.Rect(112, 0, 6, 6, C(0.35f, 0.22f, 0.12f));
            return p.ToSprite(100, false, new Vector2(0.5f, 0.5f));
        }

        /// <summary>Bảng đen ghi thực đơn (chữ thêm bằng TextMesh khi đặt vào cảnh).</summary>
        private static Sprite MenuBoard()
        {
            var p = new Paint(112, 128, C(0, 0, 0, 0), 45);
            p.Rect(0, 0, 112, 128, C(0.55f, 0.36f, 0.20f));
            p.Rect(6, 6, 100, 116, C(0.16f, 0.25f, 0.20f));
            p.Blotches(4, 6, 14, C(1f, 1f, 1f, 0.04f));
            return p.ToSprite(100, false, new Vector2(0.5f, 0.5f));
        }

        /// <summary>Cửa sổ khung kem, hai cánh chớp xanh lá mở ra (nhà phố cổ).</summary>
        private static Sprite WindowShutters()
        {
            var p = new Paint(150, 120, C(0, 0, 0, 0), 46);
            var green = C(0.22f, 0.50f, 0.36f);
            // khung + kính
            p.Rect(36, 6, 78, 108, C(0.93f, 0.90f, 0.80f));
            p.Rect(42, 12, 66, 96, C(0.55f, 0.78f, 0.90f));
            p.Rect(42, 60, 66, 48, C(0.70f, 0.87f, 0.96f));
            p.Rect(73, 12, 4, 96, C(0.93f, 0.90f, 0.80f));
            p.Rect(42, 58, 66, 4, C(0.93f, 0.90f, 0.80f));
            // hai cánh chớp
            for (int s = 0; s < 2; s++)
            {
                float x = s == 0 ? 2 : 116;
                p.Rect(x, 8, 32, 104, Shade(green, 0.8f));
                p.Rect(x + 3, 11, 26, 98, green);
                for (int y = 16; y < 106; y += 8) p.Rect(x + 4, y, 24, 3, Shade(green, 0.7f));
            }
            p.Rect(32, 0, 86, 6, C(0.85f, 0.82f, 0.72f)); // bậu cửa
            return p.ToSprite(100, false, new Vector2(0.5f, 0.5f));
        }

        private static Sprite WindowGlass()
        {
            var p = new Paint(120, 110, C(0, 0, 0, 0), 47);
            var frame = C(0.35f, 0.24f, 0.16f);
            p.Rect(0, 0, 120, 110, frame);
            p.Rect(6, 6, 108, 98, C(0.62f, 0.82f, 0.94f));
            p.Rect(6, 55, 108, 49, C(0.75f, 0.90f, 0.98f));
            p.Rect(57, 6, 6, 98, frame);
            p.Rect(6, 52, 108, 6, frame);
            p.Line(14, 64, 34, 96, 5, C(1, 1, 1, 0.45f)); // ánh phản chiếu
            p.Line(72, 14, 88, 40, 4, C(1, 1, 1, 0.35f));
            return p.ToSprite(100, false, new Vector2(0.5f, 0.5f));
        }

        /// <summary>Cửa sổ tròn song gỗ kiểu cung đình (nhà hàng lớn).</summary>
        private static Sprite WindowRound()
        {
            var p = new Paint(120, 120, C(0, 0, 0, 0), 48);
            p.Circle(60, 60, 58, C(0.55f, 0.12f, 0.08f));
            p.Circle(60, 60, 50, C(0.95f, 0.85f, 0.60f));
            var wood = C(0.45f, 0.10f, 0.07f);
            for (int i = 16; i < 110; i += 16)
            {
                p.Rect(i, 12, 4, 96, wood);
                p.Rect(12, i, 96, 4, wood);
            }
            p.ClearOutside(60, 60, 58);
            p.Ring(60, 60, 58, 8, C(0.55f, 0.12f, 0.08f));
            p.Ring(60, 60, 52, 5, C(0.85f, 0.65f, 0.25f));
            return p.ToSprite(100, false, new Vector2(0.5f, 0.5f));
        }

        private static Sprite Lantern()
        {
            var p = new Paint(64, 120, C(0, 0, 0, 0), 49);
            p.Rect(31, 96, 2, 24, C(0.2f, 0.12f, 0.08f));
            p.Ellipse(32, 62, 28, 32, C(0.85f, 0.12f, 0.10f));
            for (int k = -2; k <= 2; k++) p.Ellipse(32 + k * 10, 62, 2, 30 - Mathf.Abs(k) * 4, C(0.65f, 0.08f, 0.06f));
            p.Ellipse(24, 72, 6, 12, C(1f, 0.5f, 0.4f, 0.4f));
            p.Rect(20, 90, 24, 8, C(0.90f, 0.70f, 0.25f));
            p.Rect(20, 26, 24, 8, C(0.90f, 0.70f, 0.25f));
            for (int k = 0; k < 5; k++) p.Rect(25 + k * 3, 4, 2, 22, C(0.95f, 0.75f, 0.30f)); // tua
            return p.ToSprite(100, false, new Vector2(0.5f, 1f));
        }

        /// <summary>Tranh câu đối đỏ viền vàng (chữ thêm bằng TextMesh).</summary>
        private static Sprite Scroll()
        {
            var p = new Paint(56, 150, C(0, 0, 0, 0), 50);
            p.Rect(0, 140, 56, 10, C(0.35f, 0.18f, 0.10f));
            p.Rect(0, 0, 56, 8, C(0.35f, 0.18f, 0.10f));
            p.Rect(4, 8, 48, 132, C(0.90f, 0.70f, 0.28f));
            p.Rect(8, 12, 40, 124, C(0.78f, 0.12f, 0.10f));
            return p.ToSprite(100, false, new Vector2(0.5f, 0.5f));
        }

        // ================= Khu bếp và dãy quầy (tách khỏi sảnh ăn) =================

        /// <summary>Sàn khu bếp phía sau quầy: gạch nhỏ vuông, khác hẳn sàn sảnh.</summary>
        public static Sprite KitchenFloor(int tier) => Cached("kfloor" + tier, () => WithStyle(0.5f, 0.6f, 0.08f, () =>
        {
            Color tile, grout;
            switch (tier)
            {
                case 0: tile = C(0.55f, 0.58f, 0.60f); grout = C(0.42f, 0.44f, 0.47f); break;   // nền xi măng
                case 1: tile = C(0.66f, 0.62f, 0.55f); grout = C(0.50f, 0.46f, 0.40f); break;   // láng xi măng cũ
                case 2: tile = C(0.86f, 0.86f, 0.84f); grout = C(0.66f, 0.66f, 0.64f); break;   // gạch men xám nhạt
                case 3: tile = C(0.92f, 0.93f, 0.94f); grout = C(0.70f, 0.72f, 0.75f); break;   // gạch men trắng
                default: tile = C(0.30f, 0.30f, 0.33f); grout = C(0.20f, 0.20f, 0.22f); break;  // đá xám đậm
            }
            var p = new Paint(64, 64, grout, 70 + tier);
            for (int ty = 0; ty < 4; ty++)
            for (int tx = 0; tx < 4; tx++)
            {
                float k = 0.97f + p.Rand() * 0.06f;
                p.Rect(tx * 16 + 1, ty * 16 + 1, 14, 14, Shade(tile, k));
            }
            return p.ToSprite(70, true, new Vector2(0.5f, 0.5f));
        }));

        /// <summary>
        /// Dải sàn ngay trước dãy quầy (chỗ phục vụ lấy món): ván gỗ / gạch đá mài / đá hoa cương viền vàng.
        /// Tạo thành một "khu quầy" riêng giữa bếp và sảnh.
        /// </summary>
        public static Sprite ServiceStrip(int tier) => Cached("strip" + tier, () => WithStyle(0.55f, 0.6f, 0.06f, () =>
        {
            Paint p;
            if (tier <= 2)
            {
                // Ván gỗ nằm ngang
                var wood = tier == 0 ? C(0.60f, 0.45f, 0.30f) : tier == 1 ? C(0.66f, 0.48f, 0.30f) : C(0.72f, 0.54f, 0.34f);
                p = new Paint(128, 64, Shade(wood, 0.7f), 80 + tier);
                for (int row = 0; row < 4; row++)
                {
                    float off = (row % 2) * 48;
                    for (int x = -1; x < 2; x++)
                    {
                        float k = 0.92f + p.Rand() * 0.14f;
                        p.RectWrap(off + x * 96 + 1, row * 16 + 1, 94, 14, Shade(wood, k));
                    }
                }
                return p.ToSprite(110, true, new Vector2(0.5f, 0.5f));
            }
            if (tier == 3)
            {
                // Đá mài (terrazzo) kem lấm tấm, đặc trưng nhà phố thập niên 90
                p = new Paint(96, 96, C(0.86f, 0.83f, 0.76f), 83);
                Color[] chips = { C(0.55f, 0.50f, 0.45f), C(0.75f, 0.55f, 0.45f), C(0.40f, 0.42f, 0.45f), C(0.95f, 0.95f, 0.92f) };
                for (int i = 0; i < 70; i++)
                {
                    float x = p.Rand() * 96, y = p.Rand() * 96, r = 1.2f + p.Rand() * 2.2f;
                    var c = chips[i % chips.Length];
                    for (int ox = -1; ox <= 1; ox++)
                    for (int oy = -1; oy <= 1; oy++)
                        p.Ellipse(x + ox * 96, y + oy * 96, r, r * (0.6f + p.Rand() * 0.4f), c);
                }
                return p.ToSprite(80, true, new Vector2(0.5f, 0.5f));
            }
            // Đá hoa cương trắng vân xám, viền chỉ vàng
            p = new Paint(128, 64, C(0.92f, 0.91f, 0.88f), 84);
            for (int i = 0; i < 4; i++)
                p.Line(p.Rand() * 128, 0, p.Rand() * 128, 64, 1.5f, C(0.72f, 0.72f, 0.74f, 0.6f));
            p.Rect(0, 0, 1.5f, 64, C(0.85f, 0.68f, 0.30f));
            return p.ToSprite(90, true, new Vector2(0.5f, 0.5f));
        }));

        /// <summary>Bảng tên món gắn trên mặt quầy, mỗi mốc một kiểu (co giãn theo độ dài tên).</summary>
        public static Sprite Plaque(int tier) => Cached("plaque" + tier, () => WithStyle(0.9f, 0.75f, 0.03f, () =>
        {
            var p = new Paint(64, 40, C(0, 0, 0, 0), 90 + tier);
            switch (tier)
            {
                case 0: // bìa giấy trắng viền đỏ, viết tay
                    p.RoundRect(0, 0, 64, 40, 6, C(0.80f, 0.25f, 0.20f));
                    p.RoundRect(3, 3, 58, 34, 4, C(0.98f, 0.96f, 0.90f));
                    break;
                case 1: // tấm ván gỗ đóng đinh
                case 2:
                    p.RoundRect(0, 0, 64, 40, 6, C(0.35f, 0.22f, 0.12f));
                    p.RoundRect(3, 3, 58, 34, 4, tier == 1 ? C(0.62f, 0.42f, 0.24f) : C(0.70f, 0.50f, 0.30f));
                    p.Circle(9, 20, 2.2f, C(0.30f, 0.30f, 0.32f));
                    p.Circle(55, 20, 2.2f, C(0.30f, 0.30f, 0.32f));
                    break;
                case 3: // biển men xanh viền trắng (kiểu biển số nhà Hà Nội)
                    p.RoundRect(0, 0, 64, 40, 8, C(0.95f, 0.95f, 0.95f));
                    p.RoundRect(3, 3, 58, 34, 6, C(0.18f, 0.36f, 0.62f));
                    p.RoundRect(5, 5, 54, 30, 5, C(0.95f, 0.95f, 0.95f));
                    p.RoundRect(7, 7, 50, 26, 4, C(0.18f, 0.36f, 0.62f));
                    break;
                default: // sơn mài đỏ viền vàng
                    p.RoundRect(0, 0, 64, 40, 6, C(0.85f, 0.65f, 0.25f));
                    p.RoundRect(3, 3, 58, 34, 4, C(0.55f, 0.08f, 0.07f));
                    p.Rect(6, 6, 52, 1.5f, C(0.90f, 0.72f, 0.30f));
                    p.Rect(6, 32.5f, 52, 1.5f, C(0.90f, 0.72f, 0.30f));
                    break;
            }
            return p.ToSlicedSprite(100, 12);
        }));

        /// <summary>Màu chữ trên bảng tên món theo mốc.</summary>
        public static Color PlaqueText(int tier)
        {
            switch (tier)
            {
                case 0: return C(0.70f, 0.18f, 0.15f);
                case 1:
                case 2: return C(1f, 0.95f, 0.82f);
                case 3: return Color.white;
                default: return C(1f, 0.85f, 0.45f);
            }
        }

        // ================= Đường phố trước quán =================

        /// <summary>Mặt đường nhựa có vạch đứt quãng ở giữa ô (ô cao 1 đơn vị, vạch nằm giữa).</summary>
        public static Sprite Road() => Cached("road", () => WithStyle(0.7f, 0.6f, 0.04f, () =>
        {
            var p = new Paint(128, 64, C(0.34f, 0.35f, 0.37f), 101);
            p.Noise(0.03f);
            p.Rect(20, 30, 56, 4, C(0.92f, 0.90f, 0.82f));
            return p.ToSprite(64, true, new Vector2(0.5f, 0.5f));
        }));

        /// <summary>Vỉa hè lát gạch vuông màu be (khác gạch sàn quán).</summary>
        public static Sprite Sidewalk() => Cached("sidewalk", () => WithStyle(0.5f, 0.6f, 0.06f, () =>
        {
            var p = new Paint(64, 64, C(0.58f, 0.55f, 0.50f), 102);
            for (int ty = 0; ty < 2; ty++)
            for (int tx = 0; tx < 2; tx++)
            {
                float k = 0.95f + p.Rand() * 0.08f;
                p.Rect(tx * 32 + 1, ty * 32 + 1, 30, 30, C(0.78f * k, 0.74f * k, 0.66f * k));
            }
            p.Noise(0.02f);
            return p.ToSprite(80, true, new Vector2(0.5f, 0.5f));
        }));

        /// <summary>Đồng xu tip vàng (vành đậm, mặt dập nổi, vệt sáng).</summary>
        public static Sprite Coin() => Cached("coin", () =>
        {
            var p = new Paint(64, 64, C(0, 0, 0, 0), 120);
            p.Circle(32, 30, 28, C(0.70f, 0.48f, 0.10f));         // cạnh xu (dày, hơi lệch xuống)
            p.Circle(32, 33, 27, C(0.98f, 0.78f, 0.22f));         // mặt xu
            p.Ring(32, 33, 20, 3, C(0.85f, 0.60f, 0.12f));         // vòng dập nổi
            p.Rect(29, 22, 6, 22, C(0.85f, 0.60f, 0.12f));        // chữ "đ" giản lược
            p.Rect(25, 38, 14, 3, C(0.85f, 0.60f, 0.12f));
            p.Circle(25, 31, 5, C(0.85f, 0.60f, 0.12f));
            p.Circle(25, 31, 2.5f, C(0.98f, 0.78f, 0.22f));
            p.Line(18, 46, 26, 54, 4, C(1f, 1f, 0.9f, 0.7f));    // vệt sáng
            return p.ToSprite(64, false, new Vector2(0.5f, 0.5f));
        });

        /// <summary>Thảm chùi chân ở cửa.</summary>
        public static Sprite DoorMat(int tier) => Cached("mat" + tier, () => WithStyle(0.6f, 0.6f, 0.05f, () =>
        {
            var p = new Paint(128, 48, C(0, 0, 0, 0), 60);
            var c = tier >= 4 ? C(0.80f, 0.60f, 0.20f) : tier >= 2 ? C(0.55f, 0.30f, 0.18f) : C(0.35f, 0.38f, 0.42f);
            p.RoundRect(0, 0, 128, 48, 10, Shade(c, 0.75f));
            p.RoundRect(6, 6, 116, 36, 7, c);
            for (int x = 14; x < 116; x += 10) p.Rect(x, 10, 4, 28, Shade(c, 0.85f));
            return p.ToSprite(80, false, new Vector2(0.5f, 0.5f));
        }));
    }
}
