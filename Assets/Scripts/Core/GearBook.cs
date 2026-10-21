using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PhoAmThucViet.Core
{
    /// <summary>Hiệu ứng của một món trang bị. Các hiệu ứng cố định; nâng cấp chỉ tăng lợi nhuận (profitMin → profitMax).</summary>
    public class GearStat
    {
        public float perfectAll;   // mọi đầu bếp: tỉ lệ món hoàn hảo (x6 giá)
        public float perfectHead;  // riêng đầu bếp chính: tỉ lệ món hoàn hảo
        public float cookAll;      // mọi đầu bếp nấu nhanh hơn (+1 = nhanh gấp đôi)
        public float runAll;       // mọi nhân viên chạy nhanh hơn
        public float cookHead;     // riêng đầu bếp chính nấu nhanh hơn
        public float runHead;      // riêng đầu bếp chính chạy nhanh hơn
        public float discount;     // giảm giá nâng cấp, mở quầy, thuê người, mua bàn
        public float profitMin, profitMax; // lợi nhuận mọi món ở cấp 1 → cấp tối đa
    }

    /// <summary>
    /// Sổ trang bị: chỉ số của mọi món (theo id, hoặc theo vị trí + độ hiếm với đồ Thường/Hiếm/Sử thi),
    /// 13 món Huyền thoại mới (dựng trong code, rơi từ rương như đồ khác).
    ///   Thường / Hiếm / Sử thi: chỉ buff đầu bếp chính (dao: món hoàn hảo, nồi: nấu nhanh, mũ: chạy nhanh), tạp dề giảm giá.
    ///   Huyền thoại: hiệu ứng nhỏ của đồ Tối thượng (mọi đầu bếp / đầu bếp chính hoàn hảo, nấu nhanh, chạy nhanh; tạp dề giảm giá).
    ///   Tối thượng: xem UltimateItems; đủ 4 món = 100% món hoàn hảo, nấu nhanh +500%, chạy nhanh +300%.
    /// </summary>
    public static class GearBook
    {
        private static GearStat S(float pa = 0, float ph = 0, float cook = 0, float run = 0, float disc = 0,
                                  float cookHead = 0, float runHead = 0, float pMin = 0, float pMax = 0) =>
            new GearStat { perfectAll = pa, perfectHead = ph, cookAll = cook, runAll = run, discount = disc,
                           cookHead = cookHead, runHead = runHead, profitMin = pMin, profitMax = pMax };

        // ---------- Huyền thoại: lợi nhuận +8% → +20% ----------
        private const float LP0 = 0.08f, LP1 = 0.20f;

        private static readonly Dictionary<string, GearStat> ById = new Dictionary<string, GearStat>
        {
            // Dao
            { "dao-vang-ngu-thien", S(pa: 0.05f, ph: 0.15f, cook: 0.20f, pMin: LP0, pMax: LP1) },
            { "dao-ngu-thien-phong", S(pa: 0.10f, cook: 0.15f, pMin: LP0, pMax: LP1) },
            { "dao-than-bep", S(pa: 0.06f, ph: 0.12f, run: 0.15f, pMin: LP0, pMax: LP1) },
            { "dao-ngoc-bich", S(pa: 0.08f, ph: 0.08f, run: 0.10f, pMin: LP0, pMax: LP1) },
            { "dao-lieu-diep", S(pa: 0.06f, cook: 0.25f, run: 0.15f, pMin: LP0, pMax: LP1) },
            // Nồi
            { "noi-dong-cung-dinh", S(ph: 0.05f, cook: 0.50f, pMin: LP0, pMax: LP1) },
            { "bep-lo-hoang-cung", S(cook: 0.40f, run: 0.10f, pMin: LP0, pMax: LP1) },
            { "noi-ap-suat-than-toc", S(pa: 0.03f, cook: 0.45f, pMin: LP0, pMax: LP1) },
            { "chao-lua-rong", S(cook: 0.35f, run: 0.20f, pMin: LP0, pMax: LP1) },
            // Tạp dề
            { "tap-de-theu-rong", S(pa: 0.05f, ph: 0.10f, cook: 0.15f, disc: 0.15f, pMin: LP0, pMax: LP1) },
            { "tap-de-gam-hoang-gia", S(ph: 0.12f, run: 0.10f, disc: 0.10f, pMin: LP0, pMax: LP1) },
            { "tap-de-lua-van-phuc", S(pa: 0.06f, cook: 0.20f, disc: 0.12f, pMin: LP0, pMax: LP1) },
            { "tap-de-tho-cam", S(cook: 0.15f, run: 0.25f, disc: 0.20f, pMin: LP0, pMax: LP1) },
            // Mũ
            { "khan-xep-ngu-thien", S(ph: 0.05f, run: 0.40f, pMin: LP0, pMax: LP1) },
            { "mu-tong-bep-truong", S(cook: 0.15f, run: 0.35f, pMin: LP0, pMax: LP1) },
            { "non-quai-thao", S(pa: 0.04f, run: 0.30f, pMin: LP0, pMax: LP1) },
            { "mu-phuong-hoang", S(ph: 0.08f, run: 0.40f, pMin: LP0, pMax: LP1) },

            // ---------- Tối thượng (cộng 4 món cùng set = 100% hoàn hảo, +500% nấu, +300% chạy, đầu bếp chính 100%) ----------
            // Tết
            { "ult-tet-0", S(pa: 0.50f, ph: 0.30f, cook: 0.50f, run: 0.30f, pMin: 0.20f, pMax: 0.50f) },
            { "ult-tet-1", S(pa: 0.10f, ph: 0.10f, cook: 2.50f, run: 0.30f, pMin: 0.10f, pMax: 0.30f) },
            { "ult-tet-2", S(pa: 0.30f, ph: 0.40f, cook: 1.00f, run: 0.40f, disc: 0.35f, pMin: 0.15f, pMax: 0.40f) },
            { "ult-tet-3", S(pa: 0.10f, ph: 0.20f, cook: 1.00f, run: 2.00f, pMin: 0.12f, pMax: 0.35f) },
            // Trung Thu
            { "ult-trungthu-0", S(pa: 0.40f, ph: 0.20f, cook: 1.00f, run: 0.40f, pMin: 0.20f, pMax: 0.50f) },
            { "ult-trungthu-1", S(pa: 0.20f, ph: 0.10f, cook: 2.00f, run: 0.20f, pMin: 0.10f, pMax: 0.30f) },
            { "ult-trungthu-2", S(pa: 0.20f, ph: 0.50f, cook: 0.50f, run: 0.40f, disc: 0.30f, pMin: 0.15f, pMax: 0.40f) },
            { "ult-trungthu-3", S(pa: 0.20f, ph: 0.20f, cook: 1.50f, run: 2.00f, pMin: 0.12f, pMax: 0.35f) },
            // Quốc khánh
            { "ult-quockhanh-0", S(pa: 0.60f, ph: 0.20f, cook: 0.40f, run: 0.20f, pMin: 0.20f, pMax: 0.50f) },
            { "ult-quockhanh-1", S(pa: 0.10f, ph: 0.20f, cook: 3.00f, run: 0.30f, pMin: 0.10f, pMax: 0.30f) },
            { "ult-quockhanh-2", S(pa: 0.20f, ph: 0.20f, cook: 0.60f, run: 1.00f, disc: 0.40f, pMin: 0.15f, pMax: 0.40f) },
            { "ult-quockhanh-3", S(pa: 0.10f, ph: 0.40f, cook: 1.00f, run: 1.50f, pMin: 0.12f, pMax: 0.35f) },
            // Giáng sinh
            { "ult-noel-0", S(pa: 0.45f, ph: 0.25f, cook: 0.80f, run: 0.50f, pMin: 0.20f, pMax: 0.50f) },
            { "ult-noel-1", S(pa: 0.15f, ph: 0.15f, cook: 2.20f, run: 0.15f, pMin: 0.10f, pMax: 0.30f) },
            { "ult-noel-2", S(pa: 0.25f, ph: 0.35f, cook: 1.20f, run: 0.35f, disc: 0.45f, pMin: 0.15f, pMax: 0.40f) },
            { "ult-noel-3", S(pa: 0.15f, ph: 0.25f, cook: 0.80f, run: 2.00f, pMin: 0.12f, pMax: 0.35f) },
        };

        // ---------- Thường / Hiếm / Sử thi: chỉ buff đầu bếp chính (tạp dề giảm giá cho cả quán) ----------
        private static readonly float[] LowProfitMin = { 0.01f, 0.03f, 0.05f };
        private static readonly float[] LowProfitMax = { 0.03f, 0.06f, 0.12f };
        private static readonly float[] LowPerfect = { 0.04f, 0.08f, 0.12f };   // dao
        private static readonly float[] LowCook = { 0.08f, 0.14f, 0.22f };      // nồi
        private static readonly float[] LowDiscount = { 0.04f, 0.08f, 0.14f };  // tạp dề
        private static readonly float[] LowRun = { 0.10f, 0.20f, 0.30f };       // mũ

        private static GearStat Low(EquipSlot slot, Rarity r)
        {
            int i = Mathf.Clamp((int)r, 0, 2);
            var s = S(pMin: LowProfitMin[i], pMax: LowProfitMax[i]);
            switch (slot)
            {
                case EquipSlot.Dao: s.perfectHead = LowPerfect[i]; break;
                case EquipSlot.Noi: s.cookHead = LowCook[i]; break;
                case EquipSlot.TapDe: s.discount = LowDiscount[i]; break;
                default: s.runHead = LowRun[i]; break;
            }
            return s;
        }

        public static GearStat Get(EquipmentData item)
        {
            if (item == null) return new GearStat();
            return ById.TryGetValue(item.id, out var s) ? s : Low(item.slot, item.rarity);
        }

        public static float ProfitAt(EquipmentData item, int level)
        {
            var s = Get(item);
            int max = item.MaxLevel;
            float t = max <= 1 ? 1 : (Mathf.Clamp(level, 1, max) - 1) / (float)(max - 1);
            return Mathf.Lerp(s.profitMin, s.profitMax, t);
        }

        // ---------- 13 món Huyền thoại mới (dựng trong code) ----------

        private static readonly (string id, string name, EquipSlot slot)[] NewLegendaries =
        {
            ("dao-ngu-thien-phong", "Bộ dao Ngự Thiện Phòng", EquipSlot.Dao),
            ("dao-than-bep", "Dao Thần Bếp", EquipSlot.Dao),
            ("dao-ngoc-bich", "Dao Ngọc Bích", EquipSlot.Dao),
            ("dao-lieu-diep", "Dao Liễu Diệp", EquipSlot.Dao),
            ("bep-lo-hoang-cung", "Bếp lò Hoàng cung", EquipSlot.Noi),
            ("noi-ap-suat-than-toc", "Nồi áp suất Thần tốc", EquipSlot.Noi),
            ("chao-lua-rong", "Chảo lửa Rồng", EquipSlot.Noi),
            ("tap-de-gam-hoang-gia", "Tạp dề Gấm Hoàng gia", EquipSlot.TapDe),
            ("tap-de-lua-van-phuc", "Tạp dề Lụa Vạn Phúc", EquipSlot.TapDe),
            ("tap-de-tho-cam", "Tạp dề Thổ Cẩm", EquipSlot.TapDe),
            ("mu-tong-bep-truong", "Mũ Tổng bếp trưởng", EquipSlot.Mu),
            ("non-quai-thao", "Nón quai thao", EquipSlot.Mu),
            ("mu-phuong-hoang", "Mũ Phượng Hoàng", EquipSlot.Mu),
        };

        private static List<EquipmentData> _extra;

        /// <summary>Huyền thoại mới: rơi từ rương như đồ trong danh mục.</summary>
        public static List<EquipmentData> ExtraLegendaries
        {
            get
            {
                if (_extra != null) return _extra;
                _extra = new List<EquipmentData>();
                foreach (var (id, name, slot) in NewLegendaries)
                {
                    var e = ScriptableObject.CreateInstance<EquipmentData>();
                    e.id = e.name = id;
                    e.displayName = name;
                    e.slot = slot;
                    e.rarity = Rarity.HuyenThoai;
                    _extra.Add(e);
                }
                return _extra;
            }
        }

        public static EquipmentData FindExtra(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var e in ExtraLegendaries) if (e.id == id) return e;
            return null;
        }

        // ---------- Chữ mô tả ----------

        private static string P(float v) => $"{v * 100:0.#}%";

        /// <summary>Mô tả đầy đủ hiệu ứng ở cấp cho trước (nhiều dòng ngăn bằng " · ").</summary>
        public static string Describe(EquipmentData item, int level, bool withProfit = true)
        {
            var s = Get(item);
            var parts = new List<string>();
            if (withProfit) parts.Add($"Lợi nhuận +{P(ProfitAt(item, level))}");
            if (s.perfectAll > 0) parts.Add($"Mọi đầu bếp hoàn hảo {P(s.perfectAll)}");
            if (s.perfectHead > 0) parts.Add($"Đầu bếp chính hoàn hảo {P(s.perfectHead)}");
            if (s.cookAll > 0) parts.Add($"Nấu nhanh +{P(s.cookAll)}");
            if (s.cookHead > 0) parts.Add($"Đầu bếp chính nấu nhanh +{P(s.cookHead)}");
            if (s.runAll > 0) parts.Add($"Chạy nhanh +{P(s.runAll)}");
            if (s.runHead > 0) parts.Add($"Đầu bếp chính chạy nhanh +{P(s.runHead)}");
            if (s.discount > 0) parts.Add($"Giảm giá {P(s.discount)}");
            return string.Join(" · ", parts);
        }

        /// <summary>Phạm vi: chỉ đầu bếp chính hay cả quán.</summary>
        public static string Scope(EquipmentData item) =>
            item.rarity >= Rarity.HuyenThoai ? "Toàn quán" : (item.slot == EquipSlot.TapDe ? "Toàn quán" : "Đầu bếp chính");
    }
}
