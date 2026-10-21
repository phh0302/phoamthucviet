using System.Collections.Generic;
using UnityEngine;

namespace PhoAmThucViet.Core
{
    /// <summary>
    /// Một thành phố gồm 5 mốc, từ xe đẩy nhỏ tới nhà hàng lớn (tổng khoảng 40–50 phút).
    /// Xong mốc cuối thì lên xe sang thành phố tiếp theo.
    /// </summary>
    [CreateAssetMenu(fileName = "City", menuName = "Phố Ẩm Thực/Thành phố")]
    public class CityData : ScriptableObject
    {
        public string id = "ha-noi";
        public string displayName = "Hà Nội";

        [Tooltip("Các mốc theo thứ tự: xe đẩy → ... → nhà hàng lớn.")]
        public List<VenueData> venues = new List<VenueData>();

        [Tooltip("Đá quý thưởng thêm khi xong cả thành phố.")]
        public int cityCompleteGems = 50;

        [Header("Hành trình")]
        public CityData nextCity;
    }
}
