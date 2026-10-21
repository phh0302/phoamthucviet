using UnityEngine;

namespace PhoAmThucViet.Core
{
    /// <summary>4 ô trang bị của đầu bếp chính.</summary>
    public enum EquipSlot
    {
        Dao = 0,    // tăng lợi nhuận mỗi món
        Noi = 1,    // giảm thời gian nấu
        TapDe = 2,  // giảm giá mở quầy và nâng cấp
        Mu = 3      // tăng tiền offline và kéo dài thời gian tăng tốc
    }

    public enum Rarity
    {
        Thuong = 0,
        Hiem = 1,
        SuThi = 2,
        HuyenThoai = 3,
        /// <summary>Đồ sự kiện: chỉ có bằng cách ghép từ bản vẽ (rương sự kiện) + đồ Huyền thoại, Sử thi, Hiếm.</summary>
        ToiThuong = 4
    }

    /// <summary>
    /// Một món trang bị của đầu bếp chính. Hiệu ứng ở cấp n:
    ///   bonus(n) = baseBonus + bonusPerLevel × (n − 1)
    /// Giá trị dạng phân số: 0.1 = 10%.
    /// </summary>
    [CreateAssetMenu(fileName = "Equipment", menuName = "Phố Ẩm Thực/Trang bị đầu bếp")]
    public class EquipmentData : ScriptableObject
    {
        [Tooltip("Mã duy nhất, dùng khi lưu game. Không đổi sau khi đã phát hành.")]
        public string id = "dao-thai-thit";
        public string displayName = "Dao thái thịt";
        public EquipSlot slot = EquipSlot.Dao;
        public Rarity rarity = Rarity.Thuong;

        [Tooltip("Hiệu ứng ở cấp 1 (0.1 = 10%).")]
        public float baseBonus = 0.05f;

        [Tooltip("Hiệu ứng tăng thêm mỗi cấp.")]
        public float bonusPerLevel = 0.005f;

        public int MaxLevel => MaxLevelFor(rarity);

        /// <summary>Lợi nhuận món này cho ở cấp cho trước (các hiệu ứng khác cố định, xem GearBook).</summary>
        public float BonusAt(int level) => GearBook.ProfitAt(this, level);

        public static int MaxLevelFor(Rarity r) => r switch
        {
            Rarity.Thuong => 10,
            Rarity.Hiem => 20,
            Rarity.SuThi => 30,
            Rarity.HuyenThoai => 40,
            _ => 50
        };

        public static string SlotName(EquipSlot s) => s switch
        {
            EquipSlot.Dao => "Dao",
            EquipSlot.Noi => "Nồi",
            EquipSlot.TapDe => "Tạp dề",
            _ => "Mũ"
        };

        public static string EffectName(EquipSlot s) => s switch
        {
            EquipSlot.Dao => "món hoàn hảo",
            EquipSlot.Noi => "nấu nhanh",
            EquipSlot.TapDe => "giảm giá",
            _ => "chạy nhanh"
        };

        public static string RarityName(Rarity r) => r switch
        {
            Rarity.Thuong => "Thường",
            Rarity.Hiem => "Hiếm",
            Rarity.SuThi => "Sử thi",
            Rarity.HuyenThoai => "Huyền thoại",
            _ => "Tối thượng"
        };
    }
}
