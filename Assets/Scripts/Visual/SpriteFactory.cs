using UnityEngine;

namespace PhoAmThucViet.Visual
{
    /// <summary>
    /// Tạo hình tạm bằng code (hình vuông, hình tròn) và chữ trong cảnh, để dựng quán khi chưa có file hình.
    /// Khi có hình thật, chỉ cần thay sprite trong RestaurantView.
    /// </summary>
    public static class SpriteFactory
    {
        private static Sprite _square, _circle;
        private static Material _spriteMaterial;
        private static Font _font;

        public static Sprite Square => _square ??= MakeSprite(false);
        public static Sprite Circle => _circle ??= MakeSprite(true);
        private static Sprite _star;

        /// <summary>Ngôi sao 5 cánh (viền mượt), dùng cho sao món ăn.</summary>
        public static Sprite Star => _star ??= MakeStar();

        private static Sprite MakeStar()
        {
            const int size = 64;
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float ang = Mathf.PI / 2 + i * Mathf.PI / 5;
                float rad = i % 2 == 0 ? size * 0.48f : size * 0.2f;
                pts[i] = new Vector2(size / 2f + Mathf.Cos(ang) * rad, size / 2f + Mathf.Sin(ang) * rad);
            }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int inside = 0; // lấy mẫu 4 điểm mỗi điểm ảnh cho viền mượt
                for (int sy = 0; sy < 2; sy++)
                for (int sx = 0; sx < 2; sx++)
                    if (InPolygon(new Vector2(x + 0.25f + sx * 0.5f, y + 0.25f + sy * 0.5f), pts)) inside++;
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(inside * 255 / 4));
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static bool InPolygon(Vector2 p, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            return inside;
        }

        /// <summary>Vật liệu không cần ánh sáng, chạy được với cả renderer 2D lẫn 3D của URP.</summary>
        public static Material SpriteMaterial
        {
            get
            {
                if (_spriteMaterial != null) return _spriteMaterial;
                // "Sprites/Default" luôn có sẵn trong bản build và URP vẽ được (không cần đèn).
                var shader = Shader.Find("Sprites/Default")
                             ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                _spriteMaterial = new Material(shader);
                return _spriteMaterial;
            }
        }

        private static readonly System.Collections.Generic.Dictionary<Texture, Material> Materials =
            new System.Collections.Generic.Dictionary<Texture, Material>();

        /// <summary>
        /// Vật liệu riêng cho từng ảnh (đã gắn sẵn ảnh). Dùng chung một vật liệu cho nhiều ảnh khác nhau
        /// có thể làm Unity gộp lượt vẽ và vẽ nhầm tất cả bằng một ảnh.
        /// </summary>
        public static Material MaterialFor(Sprite sprite)
        {
            if (sprite == null) return SpriteMaterial;
            var tex = sprite.texture;
            if (Materials.TryGetValue(tex, out var m) && m != null) return m;
            m = new Material(SpriteMaterial) { name = "Sprite " + tex.name + " " + tex.GetInstanceID(), mainTexture = tex };
            Materials[tex] = m;
            return m;
        }

        /// <summary>Gán ảnh cho SpriteRenderer kèm đúng vật liệu của ảnh đó.</summary>
        public static void SetSprite(SpriteRenderer sr, Sprite sprite)
        {
            sr.sprite = sprite;
            sr.sharedMaterial = MaterialFor(sprite);
        }

        public static Font Font => _font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        private static Sprite MakeSprite(bool circle)
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color32[size * size];
            float r = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                byte a = 255;
                if (circle)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    a = (byte)(Mathf.Clamp01(r - d) * 255); // viền mượt
                }
                pixels[y * size + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            // 64 px = 1 đơn vị, tâm ở giữa
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>Tạo một khối màu kích thước (w, h) đơn vị.</summary>
        public static SpriteRenderer Block(string name, Transform parent, Vector2 pos, Vector2 size, Color color,
                                           int order, bool circle = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            var sr = go.AddComponent<SpriteRenderer>();
            SetSprite(sr, circle ? Circle : Square);
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>
        /// Dùng hình vẽ thật nếu có file (SpriteLibrary), co giãn vừa khung (w, h) và giữ tỉ lệ;
        /// chưa có file thì tạo khối màu tạm như Block. Trả về true trong isPicture nếu đã dùng hình thật.
        /// </summary>
        public static SpriteRenderer Picture(string name, Transform parent, Vector2 pos, Vector2 size, Color fallbackColor,
                                             int order, string spritePath, out bool isPicture,
                                             bool circle = false, bool stretch = false)
        {
            var sprite = SpriteLibrary.Get(spritePath);
            isPicture = sprite != null;
            if (!isPicture) return Block(name, parent, pos, size, fallbackColor, order, circle);

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            Vector2 b = sprite.bounds.size;
            go.transform.localScale = stretch
                ? new Vector3(size.x / b.x, size.y / b.y, 1)
                : Vector3.one * Mathf.Min(size.x / b.x, size.y / b.y);
            var sr = go.AddComponent<SpriteRenderer>();
            SetSprite(sr, sprite);
            sr.color = Color.white;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>Tạo chữ trong cảnh (hỗ trợ tiếng Việt qua font hệ thống). size = chiều cao chữ (đơn vị).</summary>
        public static TextMesh Text(string name, Transform parent, Vector2 pos, string text, float size, Color color,
                                    int order, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = Font;
            tm.fontSize = 64;
            tm.characterSize = size / 6.4f; // chữ cao khoảng "size" đơn vị
            tm.anchor = anchor;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            tm.text = text;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = Font.material;
            mr.sortingOrder = order;
            return tm;
        }
    }
}
