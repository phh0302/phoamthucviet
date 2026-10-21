using System;

namespace PhoAmThucViet.Core
{
    public enum UpgradeKind
    {
        WaiterSpeed, // phục vụ chạy bàn nhanh hơn
        CookSpeed,   // mọi đầu bếp nấu nhanh hơn
    }

    /// <summary>Một cải tiến mua bằng tiền trong mốc (mất khi lên đời, như Eatventure).</summary>
    public readonly struct VenueUpgrade
    {
        public readonly string name;
        public readonly UpgradeKind kind;
        public readonly double multiplier;
        /// <summary>Vị trí giá trong mốc (0 = đầu mốc, 0,9 = gần cuối).</summary>
        public readonly double levelFraction;

        public VenueUpgrade(string name, UpgradeKind kind, double multiplier, double levelFraction)
        {
            this.name = name; this.kind = kind; this.multiplier = multiplier; this.levelFraction = levelFraction;
        }

        public string Effect => kind == UpgradeKind.WaiterSpeed
            ? $"Phục vụ chạy bàn nhanh x{multiplier:0.##}"
            : $"Mọi đầu bếp nấu nhanh x{multiplier:0.##}";
    }

    /// <summary>
    /// Danh sách cải tiến của mỗi mốc. Giá đặt theo "tiến độ" của mốc (EconomyService.PriceAtProgress),
    /// nên được mua rải đều và tự co giãn theo mốc / thành phố.
    /// </summary>
    public static class VenueUpgrades
    {
        public static readonly VenueUpgrade[] All =
        {
            new VenueUpgrade("Giày bata cho phục vụ", UpgradeKind.WaiterSpeed, 1.5, 0.10),
            new VenueUpgrade("Bếp ga lửa lớn", UpgradeKind.CookSpeed, 1.25, 0.25),
            new VenueUpgrade("Xe đẩy chạy món", UpgradeKind.WaiterSpeed, 1.5, 0.40),
            new VenueUpgrade("Nồi áp suất", UpgradeKind.CookSpeed, 1.25, 0.55),
            new VenueUpgrade("Bộ đàm gọi món", UpgradeKind.WaiterSpeed, 1.5, 0.70),
            new VenueUpgrade("Bếp từ công nghiệp", UpgradeKind.CookSpeed, 1.25, 0.85),
        };

        public static bool IsBought(GameState state, int index) => (state.upgradesBought & (1 << index)) != 0;

        /// <summary>Cải tiến kế tiếp có thể mua (theo thứ tự); -1 nếu đã mua hết.</summary>
        public static int NextIndex(GameState state)
        {
            for (int i = 0; i < All.Length; i++) if (!IsBought(state, i)) return i;
            return -1;
        }

        public static BigNum Price(int index, VenueData venue, ChefBonuses b) =>
            EconomyService.PriceAtProgress(venue, All[index].levelFraction, b);

        // ---------- Nâng cấp món (mỗi quầy 2 bậc, mỗi bậc giá món x2) ----------

        public static readonly string[] RecipeTiers = { "Công thức gia truyền", "Nguyên liệu thượng hạng" };
        public const double RecipeBoost = 2;

        public static bool IsRecipeBought(GameState state, int stationIndex, int tier) =>
            (state.recipesBought & (1 << (stationIndex * RecipeTiers.Length + tier))) != 0;

        /// <summary>Bậc nâng cấp món tiếp theo của quầy (0, 1); -1 nếu đã mua hết.</summary>
        public static int NextRecipeTier(GameState state, int stationIndex)
        {
            for (int t = 0; t < RecipeTiers.Length; t++) if (!IsRecipeBought(state, stationIndex, t)) return t;
            return -1;
        }

        public static int RecipesBoughtCount(GameState state, int stationCount)
        {
            int n = 0;
            for (int i = 0; i < stationCount; i++)
                for (int t = 0; t < RecipeTiers.Length; t++) if (IsRecipeBought(state, i, t)) n++;
            return n;
        }

        /// <summary>Hệ số giá món của quầy từ các bậc đã mua.</summary>
        public static double RecipeMultiplier(GameState state, int stationIndex)
        {
            double m = 1;
            for (int t = 0; t < RecipeTiers.Length; t++) if (IsRecipeBought(state, stationIndex, t)) m *= RecipeBoost;
            return m;
        }

        /// <summary>Giá bậc nâng cấp món: quầy sau và bậc sau đắt hơn, rải từ 10% tới 90% tiến độ mốc.</summary>
        public static BigNum RecipePrice(VenueData venue, int stationIndex, int tier, ChefBonuses b)
        {
            double k = (double)stationIndex / Math.Max(1, venue.stations.Count - 1);
            double p = tier == 0 ? Math.Min(0.65, 0.1 + 0.6 * k) : Math.Min(0.9, 0.35 + 0.6 * k);
            return EconomyService.PriceAtProgress(venue, p, b);
        }

        public static double Multiplier(GameState state, UpgradeKind kind)
        {
            double m = 1;
            for (int i = 0; i < All.Length; i++)
                if (All[i].kind == kind && IsBought(state, i)) m *= All[i].multiplier;
            return m;
        }
    }
}
