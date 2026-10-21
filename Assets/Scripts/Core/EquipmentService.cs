using System;
using System.Collections.Generic;

namespace PhoAmThucViet.Core
{
    /// <summary>
    /// Logic trang bị: tính tổng hiệu ứng, mở rương, trang bị, phân rã, nâng cấp.
    /// Mỗi món rơi ra là một món riêng (cấp 1). Phân rã món thừa thành mảnh của đúng vị trí đó
    /// (Dao → mảnh dao...), rồi dùng mảnh để nâng cấp món khác cùng vị trí.
    /// Trang bị là tiến trình lâu dài: không reset khi sang mốc hay thành phố mới.
    /// </summary>
    public static class EquipmentService
    {
        /// <summary>Tổng hiệu ứng của các món đang trang bị.</summary>
        public static ChefBonuses Compute(GameState state, EquipmentCatalog catalog)
        {
            var b = new ChefBonuses();
            if (catalog == null || state.equipped == null) return b;

            foreach (string uid in state.equipped)
            {
                var owned = state.GetEquipment(uid);
                var item = owned != null ? catalog.Find(owned.id) : null;
                if (item == null) continue;

                var s = GearBook.Get(item);
                b.profit += GearBook.ProfitAt(item, owned.level);
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

        // ---------- Rương ----------

        /// <summary>Mở rương <paramref name="count"/> món: thêm vào kho và trả về danh sách để hiện lên.</summary>
        /// <param name="guaranteedRarity">Món đầu tiên chắc chắn đạt ít nhất độ hiếm này (rương sự kiện).</param>
        public static List<OwnedEquipment> OpenChest(int count, EquipmentCatalog catalog, GameState state, Random rng,
                                                     Rarity guaranteedRarity = Rarity.Thuong, float[] weights = null)
        {
            var result = new List<OwnedEquipment>();
            if (catalog == null || catalog.items.Count == 0) return result;

            for (int i = 0; i < count; i++)
            {
                var rarity = PickRarity(weights ?? catalog.dropWeights, rng);
                if (i == 0 && rarity < guaranteedRarity) rarity = guaranteedRarity;
                var item = RollItem(catalog, rarity, rng);
                if (item != null) result.Add(Add(state, item));
            }
            return result;
        }

        private static Rarity PickRarity(float[] weights, Random rng)
        {
            float total = 0;
            foreach (float w in weights) total += Math.Max(0, w);
            double roll = rng.NextDouble() * total;
            for (int i = 0; i < weights.Length; i++)
            {
                roll -= Math.Max(0, weights[i]);
                if (roll < 0) return (Rarity)i;
            }
            return Rarity.Thuong;
        }

        /// <summary>Chọn ngẫu nhiên một món đúng độ hiếm; không có thì lùi xuống độ hiếm thấp hơn.</summary>
        private static EquipmentData RollItem(EquipmentCatalog catalog, Rarity rarity, Random rng)
        {
            for (int r = (int)rarity; r >= 0; r--)
            {
                var pool = catalog.items.FindAll(e => e != null && (int)e.rarity == r);
                foreach (var e in GearBook.ExtraLegendaries) if ((int)e.rarity == r) pool.Add(e); // 13 Huyền thoại mới
                if (pool.Count > 0) return pool[rng.Next(pool.Count)];
            }
            return null;
        }

        /// <summary>Thêm một món mới (cấp 1) vào kho; tự trang bị nếu ô đó đang trống.</summary>
        public static OwnedEquipment Add(GameState state, EquipmentData item)
        {
            state.EnsureEquippedSlots();
            var owned = new OwnedEquipment { uid = state.NewEquipmentUid(), id = item.id, level = 1 };
            state.equipment.Add(owned);
            if (state.GetEquipment(state.equipped[(int)item.slot]) == null) state.equipped[(int)item.slot] = owned.uid;
            return owned;
        }

        // ---------- Trang bị ----------

        public static void Equip(GameState state, OwnedEquipment owned, EquipmentData item)
        {
            if (owned == null || item == null) return;
            state.EnsureEquippedSlots();
            state.equipped[(int)item.slot] = owned.uid;
        }

        public static bool IsEquipped(GameState state, OwnedEquipment owned) =>
            owned != null && state.equipped != null && Array.IndexOf(state.equipped, owned.uid) >= 0;

        // ---------- Phân rã ----------

        /// <summary>Số mảnh nhận được khi phân rã: mảnh gốc theo độ hiếm + phần lớn số mảnh đã bỏ vào món.</summary>
        public static int DismantleValue(EquipmentCatalog catalog, OwnedEquipment owned, EquipmentData item) =>
            EquipmentCatalog.ByRarity(catalog.dismantleBase, item.rarity, 2.5f) + (int)Math.Floor(owned.shardsSpent * catalog.dismantleRefund);

        /// <summary>Phân rã một món không đang dùng thành mảnh của đúng vị trí đó.</summary>
        public static bool Dismantle(GameState state, EquipmentCatalog catalog, OwnedEquipment owned)
        {
            var item = owned != null ? catalog.Find(owned.id) : null;
            if (item == null || IsEquipped(state, owned)) return false;
            state.EnsureEquippedSlots();
            state.shards[(int)item.slot] += DismantleValue(catalog, owned, item);
            state.equipment.Remove(owned);
            return true;
        }

        // ---------- Nâng cấp ----------

        /// <summary>Số mảnh cùng vị trí để nâng từ cấp n lên n+1.</summary>
        public static int LevelUpCost(EquipmentCatalog catalog, EquipmentData item, int level) =>
            EquipmentCatalog.ByRarity(catalog.levelUpCostPerLevel, item.rarity, 2f) * Math.Max(1, level);

        public static bool CanLevelUp(GameState state, EquipmentCatalog catalog, OwnedEquipment owned, out string reason)
        {
            reason = "";
            var item = owned != null ? catalog.Find(owned.id) : null;
            if (item == null) { reason = "Không có"; return false; }
            if (owned.level >= item.MaxLevel) { reason = "Cấp tối đa"; return false; }
            state.EnsureEquippedSlots();
            if (state.shards[(int)item.slot] < LevelUpCost(catalog, item, owned.level)) { reason = "Thiếu mảnh"; return false; }
            return true;
        }

        public static bool LevelUp(GameState state, EquipmentCatalog catalog, OwnedEquipment owned)
        {
            if (!CanLevelUp(state, catalog, owned, out _)) return false;
            var item = catalog.Find(owned.id);
            int cost = LevelUpCost(catalog, item, owned.level);
            state.shards[(int)item.slot] -= cost;
            owned.shardsSpent += cost;
            owned.level++;
            return true;
        }
    }
}
