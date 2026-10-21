using UnityEngine;

namespace PhoAmThucViet.Core
{
    /// <summary>
    /// Cấu hình một quầy món (Phở bò, Trà đá...). Tạo bằng
    /// chuột phải → Create → Phố Ẩm Thực → Quầy món.
    /// Mọi giá trị là giá gốc, sẽ nhân với hệ số thành phố.
    /// </summary>
    [CreateAssetMenu(fileName = "Station", menuName = "Phố Ẩm Thực/Quầy món")]
    public class StationData : ScriptableObject
    {
        [Tooltip("Mã duy nhất, dùng khi lưu game. Không đổi sau khi đã phát hành.")]
        public string id = "pho-bo";

        public string displayName = "Phở bò";

        [Header("Mở khóa")]
        [Tooltip("Giá mở quầy. Quầy đầu tiên để 0.")]
        public double unlockCost = 0;

        [Tooltip("Giá thuê quản lý (tự thu tiền, chạy khi offline).")]
        public double managerCost = 20;

        [Header("Nâng cấp: C(n) = baseCost × growth^n")]
        public double baseCost = 5;

        [Tooltip("Hệ số tăng giá mỗi cấp. Tài liệu: 1.07–1.10 cho màn 15–20 phút.")]
        [Range(1.01f, 1.3f)]
        public float costGrowth = 1.07f;

        [Tooltip("Cấp tối đa. Muốn qua màn phải nâng mọi quầy lên cấp này.")]
        [Range(1, 500)]
        public int maxLevel = 50;

        [Header("Lợi nhuận: P(n) = baseProfit × n × 2^(số mốc nhân đôi đã đạt)")]
        public double baseProfit = 1;

        [Tooltip("Thời gian nấu một món (giây).")]
        [Min(0.1f)]
        public float cookTime = 1f;

        [Tooltip("Đạt các cấp này thì lợi nhuận của quầy nhân đôi (mỗi mốc ×2, cộng dồn).")]
        public int[] doubleProfitLevels = { 10, 25, 50, 75, 100, 150, 200, 250 };

        [Header("Đầu bếp phụ: nấu song song, mỗi người thêm 1 món mỗi lượt")]
        [Range(1, 8)]
        public int maxChefs = 4;

        [Tooltip("Giá đầu bếp thứ k+1 = hệ số × giá nâng cấp ở cấp (maxLevel × k / maxChefs).")]
        public double chefCostMultiplier = 3;
    }
}
