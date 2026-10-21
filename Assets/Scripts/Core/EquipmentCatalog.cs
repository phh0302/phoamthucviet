using System.Collections.Generic;
using UnityEngine;

namespace PhoAmThucViet.Core
{
    /// <summary>
    /// Danh sách toàn bộ trang bị, tỷ lệ rơi, và bảng mảnh dùng cho phân rã / nâng cấp.
    /// </summary>
    [CreateAssetMenu(fileName = "EquipmentCatalog", menuName = "Phố Ẩm Thực/Danh mục trang bị")]
    public class EquipmentCatalog : ScriptableObject
    {
        public List<EquipmentData> items = new List<EquipmentData>();

        [Header("Tỷ lệ độ hiếm khi mở rương (Thường, Hiếm, Sử thi, Huyền thoại)")]
        public float[] dropWeights = { 50, 33, 12.5f, 4.5f };

        [Header("Cửa hàng")]
        [Tooltip("Giá một rương mua bằng đá quý.")]
        public int gemChestPrice = 100;
        [Tooltip("Số món trong rương mua bằng đá quý.")]
        public int gemChestItems = 2;

        [Header("Mảnh (Thường, Hiếm, Sử thi, Huyền thoại)")]
        [Tooltip("Số mảnh nhận được khi phân rã một món cấp 1.")]
        public int[] dismantleBase = { 10, 25, 60, 200 };

        [Tooltip("Phân rã món đã nâng cấp trả lại tỷ lệ này số mảnh đã bỏ vào nó.")]
        [Range(0, 1)]
        public float dismantleRefund = 0.8f;

        [Tooltip("Nâng từ cấp n lên n+1 tốn (hệ số × n) mảnh cùng vị trí.")]
        public int[] levelUpCostPerLevel = { 1, 2, 3, 5 };

        public EquipmentData Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var e in items) if (e != null && e.id == id) return e;
            // Huyền thoại mới và đồ Tối thượng dựng trong code, không nằm trong danh mục asset
            return GearBook.FindExtra(id) ?? UltimateItems.Find(id);
        }

        /// <summary>Đọc bảng theo độ hiếm; độ hiếm mới hơn bảng (Tối thượng) thì lấy ô cuối × hệ số.</summary>
        public static int ByRarity(int[] table, Rarity r, float beyondFactor)
        {
            int i = (int)r;
            if (table == null || table.Length == 0) return 0;
            return i < table.Length ? table[i] : (int)(table[table.Length - 1] * beyondFactor);
        }

        public static string ShardName(EquipSlot slot) => "mảnh " + EquipmentData.SlotName(slot).ToLower();
    }
}
