using System;
using System.Collections.Generic;

namespace PhoAmThucViet.Core
{
    /// <summary>Một sự kiện theo lịch Việt (Tết, Trung Thu...).</summary>
    public class SeasonEvent
    {
        public string id;           // "tet", "trungthu"...
        public string name;         // "Tết Nguyên Đán"
        public string greeting;     // dòng chữ treo trên tường bếp
        public string token;        // tên vật phẩm sự kiện: "bao lì xì"
        public float[] color;       // màu trang trí (r, g, b)
        public DateTime start, end; // khoảng ngày diễn ra (tính cả hai đầu)

        /// <summary>Mã riêng cho từng lần diễn ra, ví dụ "tet-2027". Đổi mã thì tiến độ sự kiện làm lại từ đầu.</summary>
        public string Key => $"{id}-{start.Year}";
    }

    /// <summary>Một mốc quà của sự kiện.</summary>
    public readonly struct EventReward
    {
        public readonly int tokens, gems, chestItems;
        public readonly bool eventChest; // rương sự kiện: chắc chắn 1 món Sử thi trở lên
        public EventReward(int tokens, int gems, int chestItems = 0, bool eventChest = false)
        {
            this.tokens = tokens; this.gems = gems; this.chestItems = chestItems; this.eventChest = eventChest;
        }
        public string Describe => chestItems > 0
            ? (eventChest ? $"Rương sự kiện {chestItems} món (chắc chắn có Sử thi+)" : $"Rương {chestItems} món")
            : $"{gems} ♦";
    }

    /// <summary>
    /// Sự kiện theo mùa. Trong thời gian sự kiện, mỗi lần khách nhận món có cơ hội để lại vật phẩm sự kiện
    /// (bao lì xì, đèn lồng...). Gom đủ thì nhận quà theo mốc; khách cũng trả thêm tiền vì không khí lễ hội.
    /// Ngày Tết / Trung Thu (âm lịch) ghi sẵn theo dương lịch cho 2026–2028.
    /// </summary>
    public static class EventService
    {
        /// <summary>Tỉ lệ một lần bưng món ra bàn rơi vật phẩm sự kiện.</summary>
        public const double TokenChance = 0.3;

        /// <summary>Khách trả thêm khi có sự kiện.</summary>
        public const double ProfitBonus = 1.2;

        public static readonly EventReward[] Rewards =
        {
            new EventReward(30, 30),
            new EventReward(100, 0, 1),
            new EventReward(250, 80),
            new EventReward(500, 0, 2),
            new EventReward(800, 150),
            new EventReward(1200, 0, 3, eventChest: true),
        };

        private static readonly float[] Red = { 0.85f, 0.15f, 0.12f };
        private static readonly float[] Orange = { 1f, 0.6f, 0.15f };
        private static readonly float[] Green = { 0.2f, 0.6f, 0.3f };

        private static SeasonEvent Tet(int lunarNewYearY, int m, int d)
        {
            var day = new DateTime(lunarNewYearY, m, d);
            return new SeasonEvent
            {
                id = "tet", name = "Tết Nguyên Đán", greeting = "CHÚC MỪNG NĂM MỚI", token = "bao lì xì",
                color = Red, start = day.AddDays(-10), end = day.AddDays(6),
            };
        }

        private static SeasonEvent TrungThu(int y, int m, int d)
        {
            var day = new DateTime(y, m, d);
            return new SeasonEvent
            {
                id = "trungthu", name = "Tết Trung Thu", greeting = "VUI TẾT TRUNG THU", token = "đèn lồng",
                color = Orange, start = day.AddDays(-7), end = day.AddDays(1),
            };
        }

        private static SeasonEvent QuocKhanh(int y) => new SeasonEvent
        {
            id = "quockhanh", name = "Lễ Quốc khánh 2/9", greeting = "MỪNG QUỐC KHÁNH 2/9", token = "cờ đỏ sao vàng",
            color = Red, start = new DateTime(y, 8, 30), end = new DateTime(y, 9, 4),
        };

        private static SeasonEvent GiangSinh(int y) => new SeasonEvent
        {
            id = "noel", name = "Giáng sinh", greeting = "GIÁNG SINH AN LÀNH", token = "chuông bạc",
            color = Green, start = new DateTime(y, 12, 20), end = new DateTime(y, 12, 27),
        };

        /// <summary>Lịch sự kiện (sắp theo ngày bắt đầu).</summary>
        public static readonly List<SeasonEvent> Calendar = BuildCalendar();

        private static List<SeasonEvent> BuildCalendar()
        {
            var list = new List<SeasonEvent>
            {
                // Âm lịch đổi sang dương lịch (Tết: 6/2/2027, 26/1/2028; Trung Thu: 25/9/2026, 15/9/2027, 3/10/2028)
                Tet(2027, 2, 6), Tet(2028, 1, 26),
                TrungThu(2026, 9, 25), TrungThu(2027, 9, 15), TrungThu(2028, 10, 3),
            };
            for (int y = 2026; y <= 2028; y++) { list.Add(QuocKhanh(y)); list.Add(GiangSinh(y)); }
            list.Sort((a, b) => a.start.CompareTo(b.start));
            return list;
        }

        // ---------- Thử nghiệm: bật sự kiện bất kỳ lúc nào ----------

        /// <summary>-1 = theo lịch; 0.. = ép sự kiện thứ i trong ForcedChoices (nút "Thử: sự kiện").</summary>
        public static int DebugForced = -1;
        private static readonly string[] ForcedChoices = { "tet", "trungthu", "quockhanh", "noel" };

        /// <summary>Đổi lần lượt: theo lịch → Tết → Trung Thu → Quốc khánh → Giáng sinh → theo lịch.</summary>
        public static string CycleDebug()
        {
            DebugForced = DebugForced + 1 >= ForcedChoices.Length ? -1 : DebugForced + 1;
            return DebugForced < 0 ? "theo lịch" : Find(ForcedChoices[DebugForced])?.name;
        }

        private static SeasonEvent Find(string id)
        {
            foreach (var e in Calendar) if (e.id == id) return e;
            return null;
        }

        /// <summary>Sự kiện đang diễn ra (null nếu không có).</summary>
        public static SeasonEvent Current
        {
            get
            {
                if (DebugForced >= 0)
                {
                    var forced = Find(ForcedChoices[DebugForced]);
                    if (forced == null) return null;
                    var today = DateTime.Today;
                    // Bản thử: kéo dài 3 ngày tính từ hôm nay, mã riêng để không lẫn với sự kiện thật
                    return new SeasonEvent
                    {
                        id = forced.id, name = forced.name + " (thử)", greeting = forced.greeting, token = forced.token,
                        color = forced.color, start = today.AddDays(-1), end = today.AddDays(2),
                    };
                }
                var now = DateTime.Today;
                foreach (var e in Calendar) if (now >= e.start && now <= e.end) return e;
                return null;
            }
        }

        /// <summary>Sự kiện sắp tới gần nhất (null nếu hết lịch).</summary>
        public static SeasonEvent Next
        {
            get
            {
                var now = DateTime.Today;
                foreach (var e in Calendar) if (e.start > now) return e;
                return null;
            }
        }

        /// <summary>Số ngày còn lại của sự kiện (tính cả hôm nay).</summary>
        public static int DaysLeft(SeasonEvent e) => Math.Max(0, (e.end - DateTime.Today).Days + 1);

        /// <summary>Sang lần sự kiện mới thì làm lại tiến độ (vật phẩm, quà đã nhận).</summary>
        public static void Sync(GameState state, SeasonEvent e)
        {
            if (e == null || state.eventKey == e.Key) return;
            state.eventKey = e.Key;
            state.eventTokens = 0;
            state.eventClaimed = 0;
        }

        public static bool IsClaimed(GameState state, int i) => (state.eventClaimed & (1 << i)) != 0;

        public static bool CanClaim(GameState state, int i) =>
            !IsClaimed(state, i) && state.eventTokens >= Rewards[i].tokens;

        public static bool HasClaimable(GameState state)
        {
            var e = Current;
            if (e == null || state.eventKey != e.Key) return false;
            for (int i = 0; i < Rewards.Length; i++) if (CanClaim(state, i)) return true;
            return false;
        }

        /// <summary>Mốc quà tiếp theo chưa đạt (-1 nếu đã đạt hết).</summary>
        public static int NextReward(GameState state)
        {
            for (int i = 0; i < Rewards.Length; i++) if (state.eventTokens < Rewards[i].tokens) return i;
            return -1;
        }
    }
}
