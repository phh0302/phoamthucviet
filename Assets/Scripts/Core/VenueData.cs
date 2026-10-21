using System.Collections.Generic;
using UnityEngine;

namespace PhoAmThucViet.Core
{
    /// <summary>
    /// Một mốc trong thành phố: từ xe đẩy nhỏ tới nhà hàng lớn. Mỗi mốc có quầy, nhân viên,
    /// điều kiện qua mốc và phần thưởng riêng. Qua mốc thì tiền, quầy, nhân viên reset và nhận rương.
    /// </summary>
    [CreateAssetMenu(fileName = "Venue", menuName = "Phố Ẩm Thực/Mốc (cơ sở)")]
    public class VenueData : ScriptableObject
    {
        public string id = "hn-1";
        public string displayName = "Xe đẩy phở";

        [Tooltip("Nhân mọi giá và lợi nhuận gốc của mốc này (gồm cả hệ số thành phố). " +
                 "Chỉ làm số to dần cho cảm giác lớn mạnh, không đổi thời gian chơi.")]
        public double multiplier = 1;

        [Tooltip("Các quầy theo thứ tự mở khóa.")]
        public List<StationData> stations = new List<StationData>();

        [Header("Khẩu vị khách")]
        [Tooltip("Món được yêu thích bán giá cao hơn.")]
        public List<StationData> favoriteStations = new List<StationData>();
        public double favoriteMultiplier = 2;

        [Tooltip("Ghi đè hệ số giá mỗi món của mốc (0 = dùng giá trị chuẩn theo thứ tự mốc, EconomyService.DefaultDishValue).")]
        public double dishValueOverride = 0;

        /// <summary>Hệ số giá món đang dùng (RestaurantController đặt khi chạy game).</summary>
        [System.NonSerialized] public double dishValue;

        [Header("Phục vụ (bản cũ, không còn dùng: phục vụ giờ thuê theo giá chung của mốc)")]
        [Tooltip("Giá thuê phục vụ thứ 2, 3, 4... (giá gốc, nhân hệ số). Quán bắt đầu với 1 phục vụ.")]
        public List<double> waiterCosts = new List<double>();
        [Tooltip("Mỗi phục vụ thêm làm mọi quầy chạy nhanh hơn bao nhiêu (0.2 = 20%).")]
        public float waiterSpeedBonus = 0.2f;

        public int MaxWaiters => (waiterCosts?.Count ?? 0) + 1;

        [Header("Điều kiện qua mốc")]
        [Tooltip("Phải mở hết quầy, nâng mọi quầy lên cấp tối đa, thuê đủ đầu bếp phụ và phục vụ.")]
        public bool requireAllMaxed = true;
        [Tooltip("Bản cũ, không còn dùng (đã bỏ quản lý: tiền tự cộng khi khách nhận món).")]
        public bool requireAllManagers = true;

        [Header("Phần thưởng khi hoàn thành mốc")]
        [Tooltip("Số món trong rương nhận được khi hoàn thành mốc này.")]
        [Range(1, 10)]
        public int chestItems = 1;

        public bool IsFavorite(StationData s) => favoriteStations != null && favoriteStations.Contains(s);
    }
}
