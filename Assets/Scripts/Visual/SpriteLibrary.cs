using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace PhoAmThucViet.Visual
{
    /// <summary>
    /// Nạp hình vẽ theo tên file từ thư mục Assets/Resources/Sprites. Có hình thì dùng, chưa có thì trả về null
    /// để cảnh quán dùng khối màu tạm. Danh sách tên file cần có: xem art-prompts.md ở thư mục gốc project.
    /// </summary>
    public static class SpriteLibrary
    {
        private const string Root = "Sprites/";
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>Ví dụ Get("characters/chef") → Assets/Resources/Sprites/characters/chef.png</summary>
        public static Sprite Get(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (Cache.TryGetValue(path, out var cached)) return cached;

            var sprite = Resources.Load<Sprite>(Root + path);
            if (sprite == null)
            {
                // File chưa được nhập dạng Sprite: tự tạo sprite từ ảnh.
                var tex = Resources.Load<Texture2D>(Root + path);
                if (tex != null)
                    sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
            }
            Cache[path] = sprite;
            return sprite;
        }

        /// <summary>Tên món → tên file không dấu: "Phở bò" → "pho-bo", "Chả cá Lã Vọng" → "cha-ca-la-vong".</summary>
        public static string Slug(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            string decomposed = name.Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            bool dash = false;
            foreach (char ch in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(ch)) { sb.Append(char.ToLowerInvariant(ch)); dash = false; }
                else if (!dash && sb.Length > 0) { sb.Append('-'); dash = true; }
            }
            return sb.ToString().Trim('-');
        }
    }
}
