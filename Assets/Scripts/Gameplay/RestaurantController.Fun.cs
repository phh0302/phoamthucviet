using System;
using PhoAmThucViet.Analytics;
using PhoAmThucViet.Core;
using UnityEngine;

namespace PhoAmThucViet.Gameplay
{
    /// <summary>
    /// Những thứ làm vài phút đầu bớt nhàm: chạm hối nhân viên, giờ cao điểm, khách sộp, gợi ý việc tiếp theo.
    /// </summary>
    public partial class RestaurantController
    {
        // ================= Chạm hối nhân viên =================

        /// <summary>Chạm vào người đang làm: làm nhanh gấp 2,5 trong 3 giây.</summary>
        public const float HurrySeconds = 3f, HurrySpeed = 2.5f;

        public event Action<Worker> OnHurried;
        /// <summary>Số lần đã hối trong phiên chơi (dùng cho hướng dẫn tân thủ).</summary>
        public int HurryCount { get; private set; }
        public int TipsCollected { get; private set; }

        public static bool IsHurried(Worker w) => w != null && Time.time < w.hurryUntil;

        /// <summary>Hối một người đang nấu / bưng. Trả về false nếu người đó đang rảnh.</summary>
        public bool Hurry(Worker w)
        {
            if (w == null || w.task == TaskKind.Idle) return false;
            w.hurryUntil = Time.time + HurrySeconds;
            HurryCount++;
            OnHurried?.Invoke(w);
            return true;
        }

        private float WorkSpeed(Worker w) => (IsHurried(w) ? HurrySpeed : 1f) * (RushActive ? RushSpeed : 1f) * EventSpeedMultiplier;

        // ================= Giờ cao điểm =================

        /// <summary>20 giây: mọi người làm nhanh x1,5 và tiền món x2. Lần đầu ở phút thứ 2,5 rồi cứ 6–10 phút một lần.</summary>
        public const float RushDuration = 20f, RushSpeed = 1.5f, FirstRushAt = 150f, RushGapMin = 360f, RushGapMax = 600f;
        public const double RushProfit = 2;

        public float RushSecondsLeft { get; private set; }
        public bool RushActive => RushSecondsLeft > 0;
        public double RushProfitMultiplier => RushActive ? RushProfit : 1;
        public event Action OnRushStarted;
        private float _nextRushIn = -1;

        private void UpdateRush(float dt)
        {
            if (State == null) return;
            if (RushSecondsLeft > 0)
            {
                RushSecondsLeft = Mathf.Max(0, RushSecondsLeft - dt);
                return;
            }
            if (_nextRushIn < 0)
                _nextRushIn = (float)(State.totalPlaySeconds < FirstRushAt ? FirstRushAt - State.totalPlaySeconds : NextRushGap());
            _nextRushIn -= dt;
            if (_nextRushIn <= 0 && !IsVenueComplete()) StartRush();
        }

        private float NextRushGap() => RushGapMin + (float)_rng.NextDouble() * (RushGapMax - RushGapMin);

        public void StartRush()
        {
            RushSecondsLeft = RushDuration;
            _nextRushIn = NextRushGap();
            OnRushStarted?.Invoke();
            AnalyticsService.Log("rush_start", ("minutes", Math.Round(State.totalPlaySeconds / 60f, 1)));
        }

        // ================= Khách sộp =================

        /// <summary>Khách sộp để lại tip = 4 lần tiền bữa. Lần đầu xuất hiện sau ~50 giây chơi, sau đó 1,5% mỗi lượt khách.</summary>
        public const double VipTipMultiplier = 4, VipChance = 0.015;
        public const float FirstVipAt = 50f;

        public event Action<TableRun> OnVipArrived;
        private bool _vipRequested;

        /// <summary>Khách tiếp theo chắc chắn là khách sộp (hướng dẫn tân thủ dùng để dạy nhặt tip).</summary>
        public void RequestVip() => _vipRequested = true;

        private bool RollVip()
        {
            if (_vipRequested) { _vipRequested = false; State.firstVipDone = true; return true; }
            if (!State.firstVipDone && State.totalPlaySeconds >= FirstVipAt) { State.firstVipDone = true; return true; }
            return State.firstVipDone && _rng.NextDouble() < VipChance;
        }

        // ================= Gợi ý việc tiếp theo =================

        /// <summary>
        /// Thứ rẻ nhất còn mua được trong quán (nâng quầy, mở quầy, thuê người, mua bàn, cải tiến, nâng cấp món).
        /// Trả về false khi đã mua hết.
        /// </summary>
        public bool CheapestNext(out string what, out BigNum price)
        {
            string bestName = null;
            BigNum best = BigNum.Zero;
            bool found = false;
            void Consider(string name, BigNum p)
            {
                if (!found || p < best) { found = true; bestName = name; best = p; }
            }
            foreach (var data in Venue.stations)
            {
                var st = State.GetStation(data.id);
                int level = st?.level ?? 0;
                if (level <= 0) Consider($"mở {data.displayName}", UnlockPrice(data));
                else if (level < data.maxLevel) Consider($"nâng {data.displayName}", UpgradePrice(data, 1));
                int tier = level > 0 ? NextRecipeTier(data) : -1;
                if (tier >= 0) Consider($"nâng cấp món {data.displayName}", RecipePrice(data, tier));
            }
            if (CanHireChef) Consider("thuê đầu bếp", ChefPrice);
            if (CanHireWaiter) Consider("thuê phục vụ", WaiterPrice);
            if (CanBuyTable) Consider("mua thêm bàn", TablePrice);
            for (int i = 0; i < VenueUpgrades.All.Length; i++)
                if (!IsUpgradeBought(i)) Consider(VenueUpgrades.All[i].name.ToLower(), UpgradeItemPrice(i));
            what = bestName;
            price = best;
            return found;
        }

        /// <summary>Số giây ước tính để đủ tiền (theo thu nhập trung bình hiện tại); -1 nếu chưa có thu nhập.</summary>
        public double SecondsUntil(BigNum price)
        {
            if (State.money >= price) return 0;
            var income = EstimatedIncome;
            if (income.IsZero) return -1;
            return ((price - State.money) / income).ToDouble();
        }
    }
}
