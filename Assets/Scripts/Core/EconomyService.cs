using System;

namespace PhoAmThucViet.Core
{
    /// <summary>
    /// Mọi công thức kinh tế của game nằm ở đây, đúng theo tài liệu thiết kế:
    ///   Giá nâng từ cấp n lên n+1:  C(n) = C0 × r^n × hệ số mốc × giảm giá trang bị
    ///   Lợi nhuận mỗi món ở cấp n:  P(n) = P0 × n × 2^(số mốc nhân đôi đã đạt) × hệ số mốc × khẩu vị × trang bị
    ///   (mốc nhân đôi: cấp 10, 25, 50, 75, 100, 150, 200, 250)
    ///   Thời gian một lượt nấu:     T = T0 × (1 − tốc độ nấu trang bị) / tốc độ phục vụ
    ///   Mỗi lượt ra số món = số đầu bếp ở quầy.
    /// Tham số ChefBonuses để trống (default) nghĩa là không có trang bị.
    /// Không phụ thuộc scene, nên dùng được cả trong game lẫn công cụ mô phỏng.
    /// </summary>
    public static class EconomyService
    {
        /// <summary>Hệ số tiền offline so với khi đang chơi.</summary>
        public const double OfflineRate = 0.5;

        /// <summary>Thời gian offline tối đa được tính (giây). Tài liệu: 1 giờ, nâng cấp được tới 4 giờ.</summary>
        public const double MaxOfflineSeconds = 3600;

        /// <summary>
        /// Tiền offline tối đa bằng số tiền kiếm được trong chừng này giây chơi thật,
        /// để người chơi không qua màn khi tắt game (một mốc dài 5–13 phút).
        /// </summary>
        public const double OfflineCapActiveSeconds = 180;

        // ---------- Giá ----------

        public static BigNum UnlockCost(StationData s, VenueData venue, ChefBonuses b = default) =>
            new BigNum(s.unlockCost) * venue.multiplier * b.CostMultiplier;

        public static BigNum ManagerCost(StationData s, VenueData venue) =>
            new BigNum(s.managerCost) * venue.multiplier;

        /// <summary>Giá nâng từ cấp <paramref name="level"/> lên cấp kế tiếp.</summary>
        public static BigNum UpgradeCost(StationData s, VenueData venue, int level, ChefBonuses b = default) =>
            new BigNum(s.baseCost) * venue.multiplier * b.CostMultiplier * BigNum.Pow(s.costGrowth, level);

        /// <summary>Tổng giá nâng <paramref name="count"/> cấp liên tiếp (tổng cấp số nhân).</summary>
        public static BigNum UpgradeCost(StationData s, VenueData venue, int level, int count, ChefBonuses b = default)
        {
            if (count <= 0) return BigNum.Zero;
            double r = s.costGrowth;
            // C(n) × (r^count − 1) / (r − 1)
            return UpgradeCost(s, venue, level, b) * ((BigNum.Pow(r, count) - BigNum.One) / (r - 1));
        }

        /// <summary>Số cấp tối đa mua được với số tiền hiện có (không vượt maxLevel).</summary>
        public static int MaxAffordableLevels(StationData s, VenueData venue, int level, BigNum money, ChefBonuses b = default)
        {
            int room = s.maxLevel - level;
            if (room <= 0) return 0;
            BigNum first = UpgradeCost(s, venue, level, b);
            if (money < first) return 0;

            double r = s.costGrowth;
            // money ≥ C(n) × (r^k − 1)/(r − 1)  ⇔  k ≤ log_r(money × (r − 1)/C(n) + 1)
            double ratioLog = (money / first).Log10;
            double inner = ratioLog > 300 ? double.MaxValue : Math.Pow(10, ratioLog) * (r - 1) + 1;
            int k = (int)Math.Floor(Math.Log(inner) / Math.Log(r));
            k = Math.Max(1, Math.Min(k, room));
            // Chống sai số làm tròn ở biên.
            while (k > 1 && UpgradeCost(s, venue, level, k, b) > money) k--;
            return k;
        }

        /// <summary>
        /// Giá thuê thêm một đầu bếp khi quầy đang có <paramref name="chefs"/> người.
        /// Neo theo giá nâng cấp ở mốc maxLevel × chefs / maxChefs, nên luôn hợp túi tiền ở giai đoạn đó.
        /// </summary>
        public static BigNum ChefCost(StationData s, VenueData venue, int chefs, ChefBonuses b = default)
        {
            int anchorLevel = s.maxLevel * Math.Max(1, chefs) / Math.Max(1, s.maxChefs);
            return UpgradeCost(s, venue, anchorLevel, b) * s.chefCostMultiplier;
        }

        /// <summary>Giá thuê thêm một phục vụ khi quán đang có <paramref name="waiters"/> người.</summary>
        public static BigNum WaiterCost(VenueData venue, int waiters, ChefBonuses b = default)
        {
            int i = Math.Max(1, waiters) - 1;
            if (venue.waiterCosts == null || i >= venue.waiterCosts.Count) return BigNum.Zero;
            return new BigNum(venue.waiterCosts[i]) * venue.multiplier * b.CostMultiplier;
        }

        /// <summary>Hệ số tốc độ do phục vụ: 1 người = 1, mỗi người thêm +waiterSpeedBonus.</summary>
        public static double ServiceSpeed(VenueData venue, int waiters) =>
            1 + venue.waiterSpeedBonus * (Math.Max(1, waiters) - 1);

        // ---------- Lợi nhuận ----------

        /// <summary>Số mốc nhân đôi lợi nhuận quầy đã đạt ở cấp <paramref name="level"/>.</summary>
        public static int DoublingsReached(StationData s, int level)
        {
            int count = 0;
            if (s.doubleProfitLevels != null)
                foreach (int m in s.doubleProfitLevels) if (level >= m) count++;
            return count;
        }

        /// <summary>Tiền thu được mỗi món ở cấp <paramref name="level"/> (0 nếu chưa mở).</summary>
        /// <param name="recipe">Hệ số từ nâng cấp món (công thức gia truyền, nguyên liệu thượng hạng).</param>
        public static BigNum ProfitPerDish(StationData s, VenueData venue, int level, ChefBonuses b = default, double recipe = 1)
        {
            if (level <= 0) return BigNum.Zero;
            double doubling = Math.Pow(2, DoublingsReached(s, level));
            double favorite = venue.IsFavorite(s) ? venue.favoriteMultiplier : 1;
            return new BigNum(s.baseProfit) * venue.multiplier * (level * doubling * favorite * b.ProfitMultiplier * DishValue(venue) * recipe);
        }

        // ---------- Khách, bàn và nhân sự dùng chung ----------
        // Mỗi bàn gọi một phần mọi món đang bán. Đầu bếp (ai cũng nấu được mọi món) nấu từng món,
        // đủ món thì phục vụ bưng cả khay ra; khách nhận món mới trả tiền, ăn xong thì bàn đón khách mới.
        // Đầu bếp chính có sẵn từ đầu: vừa nấu vừa bưng khi chưa có phục vụ rảnh.

        public const float CustomerArriveSeconds = 1.5f;
        public const float EatSeconds = 2f;
        public const float BaseTripSeconds = 2.4f;

        /// <summary>Số bàn tối đa theo mốc (xe đẩy → nhà hàng lớn). Mốc nào cũng bắt đầu với 1 bàn.</summary>
        public static readonly int[] DefaultTables = { 3, 4, 6, 8, 10 };

        /// <summary>
        /// Hệ số giá mỗi món theo mốc, chỉnh bằng mô phỏng để mỗi mốc dài 3 / 5 / 12,5 / 17,5 / 17,5 phút
        /// (không trang bị, đã tính mua đủ nâng cấp món và cải tiến).
        /// </summary>
        public static readonly double[] DefaultDishValue = { 3.8, 2.26, 1.44, 0.86, 0.75 };

        public static double DishValue(VenueData venue) => venue.dishValue > 0 ? venue.dishValue : 1;

        /// <summary>Đầu bếp thuê thêm tối đa (chưa tính đầu bếp chính): 2 người mỗi quầy.</summary>
        public static int MaxChefs(VenueData venue) => 2 * venue.stations.Count;

        /// <summary>Phục vụ tối đa: 1 người mỗi quầy.</summary>
        public static int MaxHiredWaiters(VenueData venue) => venue.stations.Count;

        /// <summary>
        /// Giá một món đồ đặt ở "tiến độ" p (0–1) của mốc: nội suy theo thang log từ 3 lần giá nâng cấp
        /// quầy đầu tới giá nâng cấp quầy cuối ở 90% cấp tối đa. Nhờ vậy đầu bếp, phục vụ, bàn và cải tiến
        /// được mua rải đều suốt mốc, và tự co giãn theo mốc / thành phố.
        /// </summary>
        public static BigNum PriceAtProgress(VenueData venue, double p, ChefBonuses b = default)
        {
            var first = venue.stations[0];
            var last = venue.stations[venue.stations.Count - 1];
            BigNum lo = UpgradeCost(first, venue, 0, b) * 3;
            BigNum hi = UpgradeCost(last, venue, (int)Math.Round(last.maxLevel * 0.9), b);
            double span = (hi / lo).Log10;
            return lo * BigNum.Pow(10, span * Math.Max(0, Math.Min(1, p)));
        }

        private static double Spread(int index, int count, double start) =>
            count <= 1 ? start : start + (0.9 - start) * index / (count - 1);

        /// <summary>Giá thuê đầu bếp tiếp theo khi đã thuê <paramref name="hired"/> người.</summary>
        public static BigNum ChefHirePrice(VenueData venue, int hired, ChefBonuses b = default) =>
            PriceAtProgress(venue, Spread(hired, MaxChefs(venue), 0), b);

        /// <summary>Giá thuê phục vụ tiếp theo khi đã thuê <paramref name="hired"/> người.</summary>
        public static BigNum WaiterHirePrice(VenueData venue, int hired, ChefBonuses b = default) =>
            PriceAtProgress(venue, Spread(hired, MaxHiredWaiters(venue), 0.05), b);

        /// <summary>Giá mua bàn tiếp theo khi quán đang có <paramref name="tables"/> bàn.</summary>
        public static BigNum TablePrice(VenueData venue, int tables, int maxTables, ChefBonuses b = default) =>
            PriceAtProgress(venue, Spread(tables - 1, maxTables - 1, 0), b);

        /// <summary>
        /// Thu nhập ước tính mỗi giây theo số bàn, đầu bếp, phục vụ (dùng cho tiền offline).
        /// </summary>
        public static BigNum EstimatedIncome(GameState state, VenueData venue, ChefBonuses b, double cookSpeed,
                                             double waiterSpeed)
        {
            int open = 0;
            double work = 0;
            BigNum perCustomer = BigNum.Zero;
            for (int i = 0; i < venue.stations.Count; i++)
            {
                var s = venue.stations[i];
                var st = state.GetStation(s.id);
                if (st == null || st.level <= 0) continue;
                open++;
                work += CookTime(s, b, cookSpeed);
                perCustomer += ProfitPerDish(s, venue, st.level, b, VenueUpgrades.RecipeMultiplier(state, i));
            }
            if (open == 0 || perCustomer.IsZero) return BigNum.Zero;

            double trip = BaseTripSeconds / Math.Max(0.01, waiterSpeed) * b.TripTimeMultiplier;
            perCustomer = perCustomer * (1 + (ChefBonuses.PerfectValue - 1) * b.PerfectChanceAll); // món hoàn hảo
            double cooks = state.chefs + (state.waiters > 0 ? 1 : 0.5);
            double servers = state.waiters > 0 ? state.waiters : 0.5;
            double tableCycle = CustomerArriveSeconds + EatSeconds + work / Math.Max(1, Math.Min(cooks, open)) + trip;
            double rate = Math.Min(Math.Max(1, state.tables) / tableCycle, Math.Min(cooks / work, servers / trip));
            return perCustomer * rate;
        }

        /// <summary>Thời gian một lượt nấu (giây), đã tính trang bị và phục vụ.</summary>
        public static double CookTime(StationData s, ChefBonuses b = default, double serviceSpeed = 1) =>
            Math.Max(0.1, s.cookTime * b.CookTimeMultiplier / Math.Max(0.01, serviceSpeed));

        /// <summary>Thu nhập mỗi giây của một quầy.</summary>
        public static BigNum IncomePerSecond(StationData s, VenueData venue, int level, ChefBonuses b = default,
                                             int chefs = 1, double serviceSpeed = 1) =>
            ProfitPerDish(s, venue, level, b) * Math.Max(1, chefs) / CookTime(s, b, serviceSpeed);

        /// <summary>Cấp nhân đôi lợi nhuận tiếp theo (không vượt cấp tối đa); -1 nếu không còn.</summary>
        public static int NextMilestone(StationData s, int level)
        {
            if (s.doubleProfitLevels == null) return -1;
            int next = -1;
            foreach (int m in s.doubleProfitLevels)
                if (m > level && m <= s.maxLevel && (next < 0 || m < next)) next = m;
            return next;
        }

        // ---------- Offline ----------

        /// <summary>
        /// Tiền nhận được sau <paramref name="secondsAway"/> giây offline.
        /// Chỉ quầy có quản lý mới chạy; tối đa bằng 3 phút chơi thật.
        /// </summary>
        public static BigNum OfflineEarnings(GameState state, VenueData venue, double secondsAway,
                                             ChefBonuses b = default, double maxSeconds = MaxOfflineSeconds,
                                             double cookSpeedMultiplier = 1, double waiterSpeedMultiplier = 1)
        {
            if (secondsAway <= 0) return BigNum.Zero;
            double seconds = Math.Min(secondsAway, maxSeconds);
            BigNum perSecond = EstimatedIncome(state, venue, b, cookSpeedMultiplier, waiterSpeedMultiplier);

            BigNum earned = perSecond * (seconds * OfflineRate * b.OfflineMultiplier);
            BigNum cap = perSecond * (OfflineCapActiveSeconds * b.OfflineMultiplier);
            return BigNum.Min(earned, cap);
        }
    }
}
