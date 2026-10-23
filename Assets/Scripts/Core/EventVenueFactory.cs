using System.Collections.Generic;
using UnityEngine;

namespace PhoAmThucViet.Core
{
    /// <summary>
    /// Dựng quán sự kiện (chỉ trong bộ nhớ, không tạo file asset): 5 quầy món theo mùa, quầy lên tới cấp 1000,
    /// 15 mốc x2 giá món (đủ 5 màu sao × 3: xanh lục, lam, vàng, tím, đỏ).
    /// Mô phỏng (người chơi mua món rẻ nhất, không trang bị): giá tăng 1,7%/cấp → nâng hết khoảng 5,5 giờ chơi
    /// (bản cũ 1,6%/cấp ≈ 2,4 giờ; rất nhạy: 1,65% ≈ 3,6 giờ, 1,75% ≈ 8,4 giờ).
    /// </summary>
    public static class EventVenueFactory
    {
        public const int MaxLevel = 1000;
        public const float CostGrowth = 1.017f;
        public const double DishValue = 0.5;
        public const int Tables = 10;
        public const int CompleteGems = 100;
        public const int CompleteChestItems = 3;

        public static readonly int[] DoubleLevels = { 10, 25, 50, 75, 100, 150, 200, 250, 400, 500, 600, 700, 800, 900, 1000 };

        // Thang số giống mốc 4–5 của thành phố (k = 1,7)
        private static readonly double[] Unlock = { 0, 1e3, 1e4, 1.25e5, 5e5 };
        private static readonly double[] Cost = { 5, 300, 3500, 42500, 5e5 };
        private static readonly double[] Profit = { 1, 6, 36, 216, 1300 };
        private static readonly float[] Cook = { 1.0f, 1.4f, 1.8f, 2.2f, 2.6f };
        private const double K = 1.7;

        private static (string venue, string[] dishes) Theme(string eventId) => eventId switch
        {
            "tet" => ("Quán Tết Sum Vầy", new[] { "Bánh chưng", "Giò lụa", "Thịt kho trứng", "Canh măng", "Mứt Tết" }),
            "trungthu" => ("Quán Trăng Rằm", new[] { "Bánh nướng", "Bánh dẻo", "Trà sen", "Chè hạt sen", "Xôi gấc" }),
            "quockhanh" => ("Quán Mừng Độc Lập", new[] { "Phở bò", "Bánh mì", "Bún chả", "Gỏi cuốn", "Cà phê sữa đá" }),
            "noel" => ("Quán Giáng Sinh", new[] { "Bánh quy gừng", "Gà quay", "Bánh bông lan", "Súp kem", "Sô cô la nóng" }),
            _ => ("Quán Lễ Hội", new[] { "Món 1", "Món 2", "Món 3", "Món 4", "Món 5" }),
        };

        public static VenueData Create(SeasonEvent e)
        {
            var (venueName, dishes) = Theme(e.id);
            var v = ScriptableObject.CreateInstance<VenueData>();
            v.name = v.id = "event-" + e.Key;
            v.displayName = venueName;
            v.multiplier = 1000; // số to cho cảm giác lễ hội; không đổi thời gian chơi
            v.dishValue = DishValue;
            v.requireAllMaxed = true;
            v.requireAllManagers = false;
            v.chestItems = CompleteChestItems;
            v.stations = new List<StationData>();
            for (int i = 0; i < dishes.Length; i++)
            {
                double k = i == 0 ? 1 : K;
                var s = ScriptableObject.CreateInstance<StationData>();
                s.name = s.id = $"{v.id}-{i + 1}";
                s.displayName = dishes[i];
                s.unlockCost = Unlock[i] * k;
                s.managerCost = 0;
                s.baseCost = Cost[i] * k;
                s.costGrowth = CostGrowth;
                s.maxLevel = MaxLevel;
                s.maxChefs = 4;
                s.baseProfit = Profit[i];
                s.cookTime = Cook[i];
                s.doubleProfitLevels = DoubleLevels;
                v.stations.Add(s);
            }
            return v;
        }
    }
}
