using System;
using PhoAmThucViet.Analytics;
using PhoAmThucViet.Core;
using UnityEngine;

namespace PhoAmThucViet.Gameplay
{
    /// <summary>
    /// Quán sự kiện kiểu Eatventure:
    /// - Mốc quà theo cấp quầy: mỗi 25 cấp được vật phẩm sự kiện, mỗi 100 cấp thêm đá quý.
    /// - Hạng quán: 16 hạng theo tổng cấp các quầy, mỗi hạng có quà (vật phẩm, đá quý, rương).
    /// - Cơ chế riêng từng sự kiện: Tết lì xì may mắn, Trung Thu rước đèn, Quốc khánh pháo hoa, Giáng sinh quà ông già Noel.
    /// - Điểm sự kiện để xếp hạng với người chơi khác (Online/EventBoard).
    /// </summary>
    public partial class RestaurantController
    {
        // ================= Mốc quà theo cấp quầy =================

        public const int EventMilestoneStep = 25, EventMilestoneTokens = 2, EventMilestoneGemStep = 100, EventMilestoneGems = 10;

        /// <summary>Quầy sự kiện chạm mốc: (quầy, cấp mốc, chữ phần thưởng).</summary>
        public event Action<StationData, int, string> OnEventMilestone;

        public static int NextEventMilestone(int level) => (level / EventMilestoneStep + 1) * EventMilestoneStep;

        private void EventLevelsGained(StationData data, int before, int after)
        {
            if (!InEventRun) return;
            var e = CurrentEvent;
            int m0 = before / EventMilestoneStep, m1 = after / EventMilestoneStep;
            if (m1 > m0)
            {
                int tokens = (m1 - m0) * EventMilestoneTokens;
                int gems = (after / EventMilestoneGemStep - before / EventMilestoneGemStep) * EventMilestoneGems;
                State.eventTokens += tokens;
                State.gems += gems;
                string tokenName = e?.token ?? "vật phẩm";
                string text = $"Mốc cấp {m1 * EventMilestoneStep}! +{tokens} {tokenName}" + (gems > 0 ? $" · +{gems} ♦" : "");
                OnEventMilestone?.Invoke(data, m1 * EventMilestoneStep, text);
            }
            UpdateEventScore();
            CheckEventRank();
        }

        // ================= Hạng quán sự kiện =================

        /// <summary>Tổng cấp các quầy cần cho từng hạng (5 quầy × cấp 1000 = 5000).</summary>
        public static readonly int[] EventRankLevels = { 25, 75, 150, 250, 400, 600, 850, 1150, 1500, 1900, 2350, 2850, 3400, 4000, 4500, 5000 };

        public event Action<int, string> OnEventRankUp;

        public int EventTotalLevels
        {
            get
            {
                if (!InEventRun) return 0;
                int sum = 0;
                foreach (var s in Venue.stations) sum += State.GetStation(s.id)?.level ?? 0;
                return sum;
            }
        }

        /// <summary>Số hạng đã đạt ở lần sự kiện hiện tại.</summary>
        public int EventRank
        {
            get
            {
                var e = CurrentEvent;
                return e != null && State.eventRankKey == e.Key ? State.eventRank : 0;
            }
        }

        public static (int tokens, int gems, int chest, bool eventChest) EventRankReward(int rankIndex)
        {
            int tokens = 10 + 5 * rankIndex;
            int gems = 10 * (1 + rankIndex / 4);
            bool chestRank = rankIndex % 4 == 3;
            int chest = chestRank ? (rankIndex == EventRankLevels.Length - 1 ? 3 : rankIndex / 4 + 1) : 0;
            return (tokens, gems, chest, chestRank && rankIndex == EventRankLevels.Length - 1);
        }

        private void CheckEventRank()
        {
            var e = CurrentEvent;
            if (!InEventRun || e == null) return;
            if (State.eventRankKey != e.Key) { State.eventRankKey = e.Key; State.eventRank = 0; }
            int total = EventTotalLevels;
            while (State.eventRank < EventRankLevels.Length && total >= EventRankLevels[State.eventRank])
            {
                int i = State.eventRank++;
                var r = EventRankReward(i);
                State.eventTokens += r.tokens;
                State.gems += r.gems;
                string text = $"HẠNG QUÁN {i + 1}! +{r.tokens} {e.token} · +{r.gems} ♦";
                AnalyticsService.Log("event_rank", ("event", e.Key), ("rank", i + 1));
                OnEventRankUp?.Invoke(i + 1, text);
                if (r.chest > 0)
                    OpenChest(r.chest, $"Hạng quán {i + 1} · {e.name} · rương {r.chest} món", r.eventChest ? Rarity.SuThi : Rarity.Thuong);
            }
            UpdateEventScore();
        }

        // ================= Điểm sự kiện (xếp hạng) =================

        /// <summary>Điểm = tổng cấp quầy cao nhất đã đạt + 10 × vật phẩm sự kiện + 100 × hạng quán.</summary>
        public int EventScore
        {
            get
            {
                var e = CurrentEvent;
                if (e == null) return 0;
                int levels = State.eventBestKey == e.Key ? State.eventBestLevels : 0;
                return levels + 10 * State.eventTokens + 100 * EventRank;
            }
        }

        private void UpdateEventScore()
        {
            var e = CurrentEvent;
            if (e == null || !InEventRun) return;
            if (State.eventBestKey != e.Key) { State.eventBestKey = e.Key; State.eventBestLevels = 0; }
            State.eventBestLevels = Mathf.Max(State.eventBestLevels, EventTotalLevels);
        }

        // ================= Cơ chế riêng từng sự kiện =================

        public enum EventMechanic { None, LiXi, Lantern, Fireworks, Gift }

        public EventMechanic Mechanic
        {
            get
            {
                if (!InEventRun) return EventMechanic.None;
                switch (CurrentEvent?.id)
                {
                    case "tet": return EventMechanic.LiXi;
                    case "trungthu": return EventMechanic.Lantern;
                    case "quockhanh": return EventMechanic.Fireworks;
                    case "noel": return EventMechanic.Gift;
                    default: return EventMechanic.None;
                }
            }
        }

        /// <summary>Vật phẩm chạm được đang hiện trong quán (lì xì / đèn lồng / hộp quà).</summary>
        public bool PickupVisible => PickupSecondsLeft > 0;
        public float PickupSecondsLeft { get; private set; }
        /// <summary>Đổi mỗi lần vật phẩm mới xuất hiện (cảnh quán dùng để đặt chỗ mới).</summary>
        public int PickupSerial { get; private set; }
        public const float PickupLifetime = 9f;
        private float _pickupTimer = 8f;

        /// <summary>Tăng tốc nấu x2 (đèn lồng Trung Thu / quà Noel).</summary>
        public float EventSpeedSecondsLeft { get; private set; }
        public float EventSpeedMultiplier => EventSpeedSecondsLeft > 0 ? 2f : 1f;
        /// <summary>Pháo hoa Quốc khánh: tiền món x3.</summary>
        public float FireworksSecondsLeft { get; private set; }
        public double FireworksProfit => FireworksSecondsLeft > 0 ? 3 : 1;
        /// <summary>Thanh "Niềm tự hào" (0–1) của Quốc khánh: mỗi khay bưng ra +4%, đầy thì bắn pháo hoa.</summary>
        public float Pride { get; private set; }
        public const float PridePerDelivery = 0.04f, FireworksSeconds = 15f;

        public event Action OnFireworks;

        private void UpdateEventFun(float dt)
        {
            if (EventSpeedSecondsLeft > 0) EventSpeedSecondsLeft = Mathf.Max(0, EventSpeedSecondsLeft - dt);
            if (FireworksSecondsLeft > 0) FireworksSecondsLeft = Mathf.Max(0, FireworksSecondsLeft - dt);
            var m = Mechanic;
            if (m == EventMechanic.None || m == EventMechanic.Fireworks) { PickupSecondsLeft = 0; return; }
            if (PickupSecondsLeft > 0)
            {
                PickupSecondsLeft = Mathf.Max(0, PickupSecondsLeft - dt);
                return;
            }
            _pickupTimer -= dt;
            if (_pickupTimer > 0) return;
            PickupSerial++;
            PickupSecondsLeft = PickupLifetime;
            float min = m == EventMechanic.LiXi ? 25 : m == EventMechanic.Lantern ? 50 : 40;
            float max = m == EventMechanic.LiXi ? 40 : m == EventMechanic.Lantern ? 80 : 60;
            _pickupTimer = PickupLifetime + min + (float)_rng.NextDouble() * (max - min);
        }

        /// <summary>Bưng xong một khay trong quán sự kiện Quốc khánh: tích "niềm tự hào".</summary>
        private void GainPride()
        {
            if (Mechanic != EventMechanic.Fireworks || FireworksSecondsLeft > 0) return;
            Pride += PridePerDelivery;
            if (Pride < 1f) return;
            Pride = 0;
            FireworksSecondsLeft = FireworksSeconds;
            OnFireworks?.Invoke();
        }

        private BigNum IncomeFor(float seconds)
        {
            var income = EstimatedIncome;
            return income.IsZero ? new BigNum(100) : income * seconds;
        }

        /// <summary>Chạm vào vật phẩm sự kiện: nhận quà, trả về chữ hiện lên (null nếu không có gì để nhặt).</summary>
        public string CollectPickup()
        {
            if (!PickupVisible) return null;
            PickupSecondsLeft = 0;
            var e = CurrentEvent;
            string token = e?.token ?? "vật phẩm";
            switch (Mechanic)
            {
                case EventMechanic.LiXi:
                {
                    var money = IncomeFor(20 + (float)_rng.NextDouble() * 40);
                    AddMoney(money);
                    State.eventTokens += 1;
                    UpdateEventScore();
                    return $"Lì xì may mắn! +{money} · +1 {token}";
                }
                case EventMechanic.Lantern:
                    EventSpeedSecondsLeft = 20;
                    State.eventTokens += 1;
                    return $"Rước đèn! Nấu nhanh x2 trong 20 giây · +1 {token}";
                case EventMechanic.Gift:
                {
                    State.eventTokens += 2;
                    double roll = _rng.NextDouble();
                    if (roll < 0.2) { State.gems += 5; return $"Quà ông già Noel: +5 ♦ · +2 {token}"; }
                    if (roll < 0.5) { EventSpeedSecondsLeft = 15; return $"Quà ông già Noel: nấu nhanh x2 15 giây · +2 {token}"; }
                    var money = IncomeFor(45);
                    AddMoney(money);
                    return $"Quà ông già Noel: +{money} · +2 {token}";
                }
            }
            return null;
        }
    }
}
