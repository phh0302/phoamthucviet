using System.Collections.Generic;
using PhoAmThucViet.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PhoAmThucViet.UI
{
    /// <summary>
    /// Icon trang bị: Resources/Sprites/Gear/&lt;mã món&gt;.png (vẽ bằng code, có thể thay bằng ảnh AI cùng tên).
    /// Thiếu ảnh thì trả về null và giao diện chỉ hiện khung màu độ hiếm.
    /// </summary>
    public static class GearIcons
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>Món Thường của từng vị trí, dùng làm hình mờ cho ô trang bị đang trống.</summary>
        private static readonly string[] SlotDefault = { "dao-thai-thit", "noi-nhom", "tap-de-vai", "mu-vai" };

        public static Sprite For(EquipmentData item) => item == null ? null : For(item.id);

        public static Sprite For(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (!Cache.TryGetValue(id, out var s))
            {
                s = Resources.Load<Sprite>("Sprites/Gear/" + id);
                Cache[id] = s;
            }
            return s;
        }

        public static Sprite ForEmptySlot(EquipSlot slot) => For(SlotDefault[(int)slot]);

        /// <summary>Màu nền khung icon theo độ hiếm (nhạt để icon nổi bật).</summary>
        public static Color FrameColor(Rarity r) => Color.Lerp(ChefPage.RarityColors[(int)r], Color.white, 0.55f);

        /// <summary>Khung bo tròn tô màu độ hiếm + ảnh icon bên trong.</summary>
        public static Image MakeFrame(string name, Transform parent, out Image icon)
        {
            var frame = UIKit.Panel(name, parent, Color.white, UIKit.Rounded);
            frame.raycastTarget = false;
            icon = UIKit.Panel("Icon", frame.transform, Color.white);
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            icon.rectTransform.Place(Vector2.zero, Vector2.one, new Vector2(6, 6), new Vector2(-6, -6));
            return frame;
        }

        /// <summary>Đặt hình cho ảnh; không có hình thì ẩn ảnh.</summary>
        public static void Apply(Image icon, Sprite sprite, Color tint)
        {
            icon.sprite = sprite;
            icon.color = tint;
            icon.enabled = sprite != null;
        }
    }
}
