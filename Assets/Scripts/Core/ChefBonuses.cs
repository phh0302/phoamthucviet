using System;

namespace PhoAmThucViet.Core
{
    /// <summary>
    /// Tổng hiệu ứng từ trang bị của đầu bếp chính. default(ChefBonuses) = không có trang bị.
    /// Món hoàn hảo bán x6 giá. Tốc độ dạng "+1 = nhanh gấp đôi". Đầu bếp chính được cộng cả phần "mọi đầu bếp" lẫn phần riêng.
    /// </summary>
    [Serializable]
    public struct ChefBonuses
    {
        public const double MaxProfit = 1.6;     // lợi nhuận tối đa +160%
        public const double MaxDiscount = 0.45;  // giá giảm tối đa 45%
        public const double MaxCookSpeed = 5.0;  // nấu nhanh tối đa +500% (gấp 6)
        public const double MaxRunSpeed = 3.0;   // chạy nhanh tối đa +300% (gấp 4)
        public const double PerfectValue = 6;    // món hoàn hảo bán gấp 6

        public double profit;
        public double discount;
        public double perfectAll, perfectHead;
        public double cookAll, cookHead;
        public double runAll, runHead;

        /// <summary>Bản cũ (công cụ mô phỏng trong Editor): tốc độ nấu dạng giảm thời gian 0–1.</summary>
        public double cookSpeed { set => cookAll = value >= 1 ? MaxCookSpeed : value / (1 - value); }

        public double ProfitMultiplier => 1 + Clamp(profit, MaxProfit);
        public double CostMultiplier => 1 - Clamp(discount, MaxDiscount);

        /// <summary>Thời gian nấu của mọi đầu bếp (nhân với thời gian gốc).</summary>
        public double CookTimeMultiplier => 1 / (1 + Clamp(cookAll, MaxCookSpeed));

        /// <summary>Đầu bếp chính: nhân thêm vào thời gian nấu (đã tính phần "mọi đầu bếp").</summary>
        public double HeadCookFactor => (1 + Clamp(cookAll, MaxCookSpeed)) / (1 + Clamp(cookAll + cookHead, MaxCookSpeed));

        /// <summary>Thời gian đi lại của mọi nhân viên (nhân với thời gian gốc).</summary>
        public double TripTimeMultiplier => 1 / (1 + Clamp(runAll, MaxRunSpeed));

        public double HeadTripFactor => (1 + Clamp(runAll, MaxRunSpeed)) / (1 + Clamp(runAll + runHead, MaxRunSpeed));

        public double PerfectChanceAll => Clamp(perfectAll, 1);
        public double PerfectChanceHead => Clamp(perfectAll + perfectHead, 1);

        /// <summary>Đã bỏ (mũ không còn tăng tiền offline / thời gian tăng tốc).</summary>
        public double OfflineMultiplier => 1;
        public double BoostDurationMultiplier => 1;

        private static double Clamp(double v, double max) => Math.Max(0, Math.Min(v, max));
    }
}
