using System;
using System.Collections.Generic;

namespace PhoAmThucViet.Core
{
    /// <summary>
    /// Thẻ đầu bếp: 4 món đang mặc (Dao, Nồi, Tạp dề, Mũ) kèm cấp. Dùng để:
    /// - đăng đầu bếp của mình lên mạng (dữ liệu công khai Cloud Save) cho người khác mượn;
    /// - mượn đầu bếp của người khác (hoặc đầu bếp có sẵn trong game khi chưa có mạng / chưa đủ người).
    /// </summary>
    [Serializable]
    public class HelperCard
    {
        public string name = "";
        public string playerId = "";
        /// <summary>true = người chơi thật lấy từ mạng; false = đầu bếp có sẵn trong game.</summary>
        public bool isPlayer;
        /// <summary>Mã món theo vị trí 0..3 (Dao, Nồi, Tạp dề, Mũ); "" = trống.</summary>
        public string[] items = { "", "", "", "" };
        public int[] levels = { 1, 1, 1, 1 };

        public string ItemAt(int slot) => items != null && slot < items.Length && !string.IsNullOrEmpty(items[slot]) ? items[slot] : null;
        public int LevelAt(int slot) => levels != null && slot < levels.Length ? Math.Max(1, levels[slot]) : 1;
    }

    public static class HelperChefs
    {
        /// <summary>Tổng hiệu ứng trang bị của một thẻ đầu bếp (giống EquipmentService.Compute).</summary>
        public static ChefBonuses Compute(HelperCard card, EquipmentCatalog catalog)
        {
            var b = new ChefBonuses();
            if (card == null || catalog == null) return b;
            for (int slot = 0; slot < 4; slot++)
            {
                var item = catalog.Find(card.ItemAt(slot));
                if (item == null) continue;
                int level = Math.Min(card.LevelAt(slot), item.MaxLevel);
                var s = GearBook.Get(item);
                b.profit += GearBook.ProfitAt(item, level);
                b.discount += s.discount;
                b.perfectAll += s.perfectAll;
                b.perfectHead += s.perfectHead;
                b.cookAll += s.cookAll;
                b.cookHead += s.cookHead;
                b.runAll += s.runAll;
                b.runHead += s.runHead;
            }
            return b;
        }

        /// <summary>Cộng dồn hiệu ứng của hai đầu bếp (vẫn bị chặn trần trong ChefBonuses).</summary>
        public static ChefBonuses Add(ChefBonuses a, ChefBonuses b)
        {
            a.profit += b.profit;
            a.discount += b.discount;
            a.perfectAll += b.perfectAll;
            a.perfectHead += b.perfectHead;
            a.cookAll += b.cookAll;
            a.cookHead += b.cookHead;
            a.runAll += b.runAll;
            a.runHead += b.runHead;
            return a;
        }

        /// <summary>Điểm sức mạnh để xếp và tìm kiếm (càng cao càng mạnh).</summary>
        public static int Power(ChefBonuses b) =>
            (int)Math.Round(100 * (b.profit + b.discount * 2 + (b.perfectAll + b.perfectHead) * 3 + (b.cookAll + b.cookHead) * 0.6 + (b.runAll + b.runHead) * 0.6));

        private static string P(double v) => $"{v * 100:0}%";

        /// <summary>Tóm tắt hiệu ứng (một dòng).</summary>
        public static string Summary(ChefBonuses b)
        {
            var parts = new List<string>();
            if (b.profit > 0) parts.Add($"Lợi nhuận +{P(b.profit)}");
            if (b.perfectAll + b.perfectHead > 0) parts.Add($"Hoàn hảo {P(b.perfectAll + b.perfectHead)}");
            if (b.cookAll + b.cookHead > 0) parts.Add($"Nấu nhanh +{P(b.cookAll + b.cookHead)}");
            if (b.runAll + b.runHead > 0) parts.Add($"Chạy nhanh +{P(b.runAll + b.runHead)}");
            if (b.discount > 0) parts.Add($"Giảm giá {P(b.discount)}");
            return parts.Count > 0 ? string.Join(" · ", parts) : "Chưa có trang bị";
        }

        /// <summary>Thẻ đầu bếp của chính người chơi (4 món đang mặc).</summary>
        public static HelperCard FromState(GameState state, string name, string playerId)
        {
            var card = new HelperCard { name = name ?? "", playerId = playerId ?? "", isPlayer = true };
            state.EnsureEquippedSlots();
            for (int slot = 0; slot < 4; slot++)
            {
                var owned = state.GetEquipment(state.equipped[slot]);
                card.items[slot] = owned?.id ?? "";
                card.levels[slot] = owned?.level ?? 1;
            }
            return card;
        }

        // ================= Đầu bếp có sẵn trong game =================

        private static readonly string[] Names =
        {
            "Cô Ba Sài Gòn", "Chú Tư Bún Chả", "Bà Năm Phở Gánh", "Anh Hùng Hải Sản", "Chị Lan Bánh Xèo", "Minh Béo",
            "Thảo Bếp Nhà", "Tuấn Cơm Tấm", "Hương Chè Huế", "Long Nướng Than", "Bác Sáu Hủ Tiếu", "Mai Bánh Mì",
            "Đức Lẩu Mắm", "Vy Trà Sữa", "Quang Mì Quảng", "Ngọc Bánh Căn", "Ông Bảy Cá Lóc", "Hạnh Nem Lụi",
        };

        /// <summary>
        /// Sinh đầu bếp có sẵn mạnh ngang người chơi (lệch lên xuống một bậc độ hiếm), thỉnh thoảng có người rất mạnh.
        /// </summary>
        public static HelperCard MakeNpc(GameState state, EquipmentCatalog catalog, Random rng, HashSet<string> usedNames)
        {
            // Độ hiếm trung bình của đồ đang mặc → mức của đầu bếp ảo
            double sum = 0; int n = 0;
            state.EnsureEquippedSlots();
            foreach (var uid in state.equipped)
            {
                var item = catalog?.Find(state.GetEquipment(uid)?.id);
                if (item == null) continue;
                sum += (int)item.rarity; n++;
            }
            int baseTier = n > 0 ? (int)Math.Round(sum / n) : 1;

            string name;
            int guard = 0;
            do name = Names[rng.Next(Names.Length)]; while (usedNames != null && usedNames.Contains(name) && guard++ < 20);
            usedNames?.Add(name);

            var card = new HelperCard { name = name, isPlayer = false };
            bool star = rng.NextDouble() < 0.15; // thỉnh thoảng gặp đầu bếp rất mạnh
            for (int slot = 0; slot < 4; slot++)
            {
                int tier = Math.Max(0, Math.Min(3, baseTier + rng.Next(-1, 2) + (star ? 1 : 0)));
                var r = (Rarity)tier;
                if (star && rng.NextDouble() < 0.25) r = Rarity.ToiThuong;
                var item = Pick(catalog, (EquipSlot)slot, r, rng);
                if (item == null) continue;
                card.items[slot] = item.id;
                card.levels[slot] = Math.Max(1, (int)Math.Round(item.MaxLevel * (0.25 + 0.7 * rng.NextDouble())));
            }
            return card;
        }

        private static EquipmentData Pick(EquipmentCatalog catalog, EquipSlot slot, Rarity rarity, Random rng)
        {
            if (catalog == null) return null;
            for (int r = (int)rarity; r >= 0; r--)
            {
                var pool = new List<EquipmentData>();
                foreach (var e in catalog.items) if (e != null && e.slot == slot && (int)e.rarity == r) pool.Add(e);
                foreach (var e in GearBook.ExtraLegendaries) if (e.slot == slot && (int)e.rarity == r) pool.Add(e);
                if (r == (int)Rarity.ToiThuong) foreach (var e in UltimateItems.All) if (e.slot == slot) pool.Add(e);
                if (pool.Count > 0) return pool[rng.Next(pool.Count)];
            }
            return null;
        }
    }
}
