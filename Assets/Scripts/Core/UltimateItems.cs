using System;
using System.Collections.Generic;
using UnityEngine;

namespace PhoAmThucViet.Core
{
    /// <summary>
    /// Đồ Tối thượng theo sự kiện (4 sự kiện × 4 vị trí). Không rơi từ rương thường:
    /// rương sự kiện cho <b>bản vẽ</b> của một món, rồi ghép bản vẽ + 2 món Huyền thoại chỉ định (cùng vị trí)
    /// + 2 Sử thi + 3 Hiếm (vị trí bất kỳ, không đang mặc) thành món Tối thượng. Hiệu ứng ở GearBook, cấp tối đa 50.
    /// </summary>
    public static class UltimateItems
    {
        public const int NeedEpic = 2, NeedRare = 3;

        /// <summary>2 món Huyền thoại cần để ghép mỗi món Tối thượng.</summary>
        public static readonly Dictionary<string, string[]> Recipes = new Dictionary<string, string[]>
        {
            { "ult-tet-0", new[] { "dao-vang-ngu-thien", "dao-than-bep" } },
            { "ult-trungthu-0", new[] { "dao-ngoc-bich", "dao-ngu-thien-phong" } },
            { "ult-quockhanh-0", new[] { "dao-ngu-thien-phong", "dao-lieu-diep" } },
            { "ult-noel-0", new[] { "dao-than-bep", "dao-ngoc-bich" } },
            { "ult-tet-1", new[] { "noi-dong-cung-dinh", "bep-lo-hoang-cung" } },
            { "ult-trungthu-1", new[] { "noi-ap-suat-than-toc", "chao-lua-rong" } },
            { "ult-quockhanh-1", new[] { "bep-lo-hoang-cung", "noi-ap-suat-than-toc" } },
            { "ult-noel-1", new[] { "noi-dong-cung-dinh", "chao-lua-rong" } },
            { "ult-tet-2", new[] { "tap-de-gam-hoang-gia", "tap-de-lua-van-phuc" } },
            { "ult-trungthu-2", new[] { "tap-de-gam-hoang-gia", "tap-de-theu-rong" } },
            { "ult-quockhanh-2", new[] { "tap-de-tho-cam", "tap-de-theu-rong" } },
            { "ult-noel-2", new[] { "tap-de-lua-van-phuc", "tap-de-tho-cam" } },
            { "ult-tet-3", new[] { "khan-xep-ngu-thien", "mu-phuong-hoang" } },
            { "ult-trungthu-3", new[] { "non-quai-thao", "mu-tong-bep-truong" } },
            { "ult-quockhanh-3", new[] { "mu-tong-bep-truong", "khan-xep-ngu-thien" } },
            { "ult-noel-3", new[] { "mu-phuong-hoang", "non-quai-thao" } },
        };
        public const int DuplicateBlueprintGems = 100; // đã có đủ bản vẽ / món của sự kiện thì đổi thành đá quý

        public static readonly string[] EventIds = { "tet", "trungthu", "quockhanh", "noel" };

        private static readonly Dictionary<string, string[]> Names = new Dictionary<string, string[]>
        {
            // Dao, Nồi, Tạp dề, Mũ
            { "tet", new[] { "Dao Long Phụng", "Nồi Đồng Ngũ Phúc", "Tạp dề Hoa Mai Vàng", "Mũ Thần Tài" } },
            { "trungthu", new[] { "Dao Cung Trăng", "Nồi Chị Hằng", "Tạp dề Đèn Ông Sao", "Mũ Chú Cuội" } },
            { "quockhanh", new[] { "Dao Sao Vàng", "Nồi Độc Lập", "Tạp dề Cờ Hồng", "Mũ Hòa Bình" } },
            { "noel", new[] { "Dao Băng Tuyết", "Nồi Tuần Lộc", "Tạp dề Ông Già Noel", "Mũ Giáng Sinh" } },
        };


        private static List<EquipmentData> _all;

        public static List<EquipmentData> All
        {
            get
            {
                if (_all != null) return _all;
                _all = new List<EquipmentData>();
                foreach (var ev in EventIds)
                    for (int slot = 0; slot < 4; slot++)
                    {
                        var e = ScriptableObject.CreateInstance<EquipmentData>();
                        e.id = $"ult-{ev}-{slot}";
                        e.name = e.id;
                        e.displayName = Names[ev][slot];
                        e.slot = (EquipSlot)slot;
                        e.rarity = Rarity.ToiThuong;
                        _all.Add(e);
                    }
                return _all;
            }
        }

        public static EquipmentData Find(string id)
        {
            if (string.IsNullOrEmpty(id) || !id.StartsWith("ult-")) return null;
            foreach (var e in All) if (e.id == id) return e;
            return null;
        }

        public static string EventOf(string id)
        {
            var parts = id.Split('-');
            return parts.Length >= 3 ? parts[1] : "";
        }

        public static string EventName(string eventId) => eventId switch
        {
            "tet" => "Tết Nguyên Đán",
            "trungthu" => "Tết Trung Thu",
            "quockhanh" => "Quốc khánh",
            "noel" => "Giáng sinh",
            _ => "Sự kiện",
        };

        private static bool OwnsItem(GameState s, string id)
        {
            foreach (var o in s.equipment) if (o.id == id) return true;
            return false;
        }

        /// <summary>
        /// Mở rương sự kiện: nhận bản vẽ một món của sự kiện chưa có (chưa có bản vẽ, chưa ghép).
        /// Trả về id món, hoặc null nếu đã đủ cả 4 (khi đó đổi thành đá quý).
        /// </summary>
        public static string RollBlueprint(GameState s, string eventId, System.Random rng)
        {
            var pool = new List<string>();
            for (int slot = 0; slot < 4; slot++)
            {
                string id = $"ult-{eventId}-{slot}";
                if (!s.blueprints.Contains(id) && !OwnsItem(s, id)) pool.Add(id);
            }
            if (pool.Count == 0) return null;
            string pick = pool[rng.Next(pool.Count)];
            s.blueprints.Add(pick);
            return pick;
        }

        private static bool Usable(GameState s, OwnedEquipment o) => !EquipmentService.IsEquipped(s, o);

        /// <summary>Món Huyền thoại chỉ định (không đang mặc, cấp thấp nhất trước).</summary>
        public static OwnedEquipment FindLegendary(GameState s, string legendaryId, OwnedEquipment except = null)
        {
            OwnedEquipment best = null;
            foreach (var o in s.equipment)
                if (o.id == legendaryId && o != except && Usable(s, o) && (best == null || o.level < best.level)) best = o;
            return best;
        }

        /// <summary>Các món Sử thi / Hiếm dùng làm nguyên liệu: không đang mặc, ưu tiên cấp thấp, ít mảnh đã bỏ vào.</summary>
        public static List<OwnedEquipment> Ingredients(GameState s, EquipmentCatalog catalog, Rarity rarity, int count)
        {
            var list = new List<OwnedEquipment>();
            foreach (var o in s.equipment)
            {
                var item = catalog.Find(o.id);
                if (item != null && item.rarity == rarity && Usable(s, o)) list.Add(o);
            }
            list.Sort((a, b) => a.level != b.level ? a.level.CompareTo(b.level) : a.shardsSpent.CompareTo(b.shardsSpent));
            if (list.Count > count) list.RemoveRange(count, list.Count - count);
            return list;
        }

        public static int Available(GameState s, EquipmentCatalog catalog, Rarity rarity) =>
            Ingredients(s, catalog, rarity, int.MaxValue).Count;

        public static bool HasLegendary(GameState s, string legendaryId) => FindLegendary(s, legendaryId) != null;

        public static bool CanCraft(GameState s, EquipmentCatalog catalog, string id)
        {
            if (catalog == null || !s.blueprints.Contains(id) || !Recipes.TryGetValue(id, out var need)) return false;
            var a = FindLegendary(s, need[0]);
            var b = FindLegendary(s, need[1], a);
            return a != null && b != null &&
                   Available(s, catalog, Rarity.SuThi) >= NeedEpic &&
                   Available(s, catalog, Rarity.Hiem) >= NeedRare;
        }

        /// <summary>
        /// Ghép: dùng 2 Huyền thoại chỉ định + Sử thi + Hiếm (mảnh đã bỏ vào chúng được trả lại 80% về đúng vị trí),
        /// bỏ bản vẽ, thêm món Tối thượng cấp 1 vào kho.
        /// </summary>
        public static OwnedEquipment Craft(GameState s, EquipmentCatalog catalog, string id)
        {
            if (!CanCraft(s, catalog, id)) return null;
            var need = Recipes[id];
            var used = new List<OwnedEquipment>();
            var a = FindLegendary(s, need[0]);
            used.Add(a);
            used.Add(FindLegendary(s, need[1], a));
            used.AddRange(Ingredients(s, catalog, Rarity.SuThi, NeedEpic));
            used.AddRange(Ingredients(s, catalog, Rarity.Hiem, NeedRare));
            s.EnsureEquippedSlots();
            foreach (var o in used)
            {
                var item = catalog.Find(o.id);
                if (item != null) s.shards[(int)item.slot] += (int)Math.Floor(o.shardsSpent * catalog.dismantleRefund);
                s.equipment.Remove(o);
            }
            s.blueprints.Remove(id);
            return EquipmentService.Add(s, Find(id));
        }
    }
}
