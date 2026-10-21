using System;
using System.Collections.Generic;

namespace PhoAmThucViet.Core
{
    public enum QuestType
    {
        UpgradeLevels = 0,   // nâng tổng cộng N cấp quầy
        HireStaff = 1,       // thuê N người (đầu bếp, phục vụ, quản lý)
        ServeCustomers = 2,  // phục vụ N khách
        OpenStations = 3,    // mở N quầy
        CompleteVenues = 4,  // lên đời N lần
        DismantleItems = 5,  // phân rã N món trang bị
    }

    /// <summary>
    /// Nhiệm vụ ngày (3 nhiệm vụ mỗi ngày, làm mới lúc 0 giờ theo giờ máy) và quà điểm danh 7 ngày.
    /// Phần thưởng là đá quý; ngày thứ 7 điểm danh được rương 3 món.
    /// </summary>
    public static class QuestService
    {
        public const int QuestsPerDay = 3;

        /// <summary>Quà điểm danh ngày 1–6 (đá quý). Ngày 7 là rương.</summary>
        public static readonly int[] LoginGems = { 20, 30, 40, 50, 60, 80 };
        public const int LoginChestItems = 3;

        private static DateTime _cachedDate;
        private static string _cachedToday;

        /// <summary>Ngày hôm nay dạng yyyy-MM-dd (lưu đệm, vì được gọi mỗi lần nấu xong món).</summary>
        public static string Today
        {
            get
            {
                var d = DateTime.Today;
                if (_cachedToday == null || d != _cachedDate) { _cachedDate = d; _cachedToday = d.ToString("yyyy-MM-dd"); }
                return _cachedToday;
            }
        }

        /// <summary>Thời gian còn lại tới lúc làm mới nhiệm vụ (0 giờ hôm sau).</summary>
        public static TimeSpan UntilReset => DateTime.Today.AddDays(1) - DateTime.Now;

        // ---------- Nhiệm vụ ngày ----------

        /// <summary>Sang ngày mới thì tạo 3 nhiệm vụ mới (cùng một ngày luôn ra cùng bộ nhiệm vụ).</summary>
        public static void EnsureDaily(GameState state)
        {
            if (state.quests == null) state.quests = new List<QuestState>();
            if (state.questDay == Today && state.quests.Count == QuestsPerDay) return;

            state.questDay = Today;
            state.quests.Clear();
            var rng = new Random(int.Parse(DateTime.Now.ToString("yyyyMMdd"))); // hạt giống ổn định theo ngày
            var types = new List<QuestType>((QuestType[])Enum.GetValues(typeof(QuestType)));
            for (int i = 0; i < QuestsPerDay; i++)
            {
                var type = types[rng.Next(types.Count)];
                types.Remove(type);
                int tier = rng.Next(2); // 0 = dễ, 1 = khó
                state.quests.Add(new QuestState
                {
                    type = type,
                    target = Target(type, tier),
                    reward = tier == 0 ? 20 : 40,
                });
            }
        }

        private static long Target(QuestType type, int tier) => type switch
        {
            QuestType.UpgradeLevels => tier == 0 ? 100 : 300,
            QuestType.HireStaff => tier == 0 ? 5 : 12,
            QuestType.ServeCustomers => tier == 0 ? 1000 : 5000,
            QuestType.OpenStations => tier == 0 ? 3 : 8,
            QuestType.CompleteVenues => tier == 0 ? 1 : 3,
            _ => tier == 0 ? 2 : 5,
        };

        public static string Describe(QuestState q) => q.type switch
        {
            QuestType.UpgradeLevels => $"Nâng tổng cộng {q.target} cấp quầy",
            QuestType.HireStaff => $"Thuê {q.target} nhân viên (đầu bếp, phục vụ, quản lý)",
            QuestType.ServeCustomers => $"Phục vụ {q.target:N0} khách",
            QuestType.OpenStations => $"Mở {q.target} quầy mới",
            QuestType.CompleteVenues => $"Lên đời {q.target} lần",
            _ => $"Phân rã {q.target} món trang bị",
        };

        /// <summary>Ghi nhận tiến độ cho mọi nhiệm vụ cùng loại chưa nhận thưởng.</summary>
        public static void Track(GameState state, QuestType type, long amount)
        {
            if (amount <= 0) return;
            EnsureDaily(state);
            foreach (var q in state.quests)
                if (q.type == type && !q.claimed && q.progress < q.target)
                    q.progress = Math.Min(q.target, q.progress + amount);
        }

        public static bool CanClaim(QuestState q) => !q.claimed && q.progress >= q.target;

        public static bool Claim(GameState state, int index)
        {
            EnsureDaily(state);
            if (index < 0 || index >= state.quests.Count || !CanClaim(state.quests[index])) return false;
            var q = state.quests[index];
            q.claimed = true;
            state.gems += q.reward;
            return true;
        }

        // ---------- Điểm danh 7 ngày ----------

        public static bool CanClaimLogin(GameState state) => state.lastLoginClaimDay != Today;

        /// <summary>Ngày trong vòng 7 ngày sẽ nhận tiếp theo (0–6).</summary>
        public static int LoginDayIndex(GameState state) => state.loginDays % 7;

        /// <summary>Nhận quà điểm danh hôm nay. Trả về true nếu là ngày 7 (cần mở rương).</summary>
        public static bool ClaimLogin(GameState state, out int gems)
        {
            gems = 0;
            if (!CanClaimLogin(state)) return false;
            int day = LoginDayIndex(state);
            state.lastLoginClaimDay = Today;
            state.loginDays++;
            if (day < LoginGems.Length)
            {
                gems = LoginGems[day];
                state.gems += gems;
                return false;
            }
            return true;
        }

        public static bool HasSomethingToClaim(GameState state)
        {
            EnsureDaily(state);
            if (CanClaimLogin(state)) return true;
            foreach (var q in state.quests) if (CanClaim(q)) return true;
            return false;
        }
    }
}
