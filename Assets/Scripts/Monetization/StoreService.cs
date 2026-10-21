using System;
using System.Collections.Generic;
using PhoAmThucViet.Analytics;
using PhoAmThucViet.Core;
using PhoAmThucViet.Gameplay;

namespace PhoAmThucViet.Monetization
{
    /// <summary>Một gói bán trong cửa hàng.</summary>
    public class StoreProduct
    {
        public string id;           // mã sản phẩm trên Google Play / App Store
        public string title;
        public string description;
        public string price;        // giá hiển thị khi chưa lấy được giá từ cửa hàng
        public bool oneTime;        // chỉ mua được 1 lần
    }

    /// <summary>Nguồn thanh toán (giả lập hoặc Unity IAP).</summary>
    public interface IStoreProvider
    {
        void Purchase(StoreProduct product, Action<bool> onDone);
    }

    /// <summary>Mua giả lập: hỏi xác nhận, không trừ tiền thật.</summary>
    public class SimulatedStoreProvider : IStoreProvider
    {
        public void Purchase(StoreProduct product, Action<bool> onDone) =>
            SimOverlay.Instance.Show($"MUA THỬ: {product.title}",
                $"{product.description}\n\nGiá: {product.price}\n\nĐây là bản chơi thử, không trừ tiền thật.\nKhi gắn Unity IAP, cửa sổ thanh toán của Google Play sẽ hiện ở đây.",
                "Mua thử", "Hủy", 0f, () => onDone(true), () => onDone(false));
    }

    /// <summary>
    /// Cửa hàng: gói khởi đầu, thẻ tháng, bỏ quảng cáo, gói đá quý. Phát quà khi mua thành công và ghi đo lường.
    /// Gắn thanh toán thật: cài Unity IAP, tạo các sản phẩm cùng mã trên Google Play Console,
    /// viết lớp IStoreProvider gọi Unity IAP rồi đặt StoreService.Provider khi khởi động.
    /// </summary>
    public static class StoreService
    {
        public const string Starter = "pho_starter_pack";
        public const string MonthlyCard = "pho_monthly_card";
        public const string NoAds = "pho_no_ads";
        public const string Gems100 = "pho_gems_100";
        public const string Gems550 = "pho_gems_550";
        public const string Gems1200 = "pho_gems_1200";

        public const int MonthlyCardDays = 30;
        public const int MonthlyCardInstantGems = 300;
        public const int MonthlyCardDailyGems = 50;

        public static readonly List<StoreProduct> Products = new List<StoreProduct>
        {
            new StoreProduct { id = Starter, title = "Gói khởi đầu", price = "49.000 ₫", oneTime = true,
                description = "500 ♦ + rương sự kiện 3 món (chắc chắn có Sử thi+) + tăng tốc x2 trong 30 phút" },
            new StoreProduct { id = MonthlyCard, title = "Thẻ tháng", price = "99.000 ₫",
                description = $"Nhận ngay {MonthlyCardInstantGems} ♦, sau đó {MonthlyCardDailyGems} ♦ mỗi ngày trong {MonthlyCardDays} ngày" },
            new StoreProduct { id = NoAds, title = "Bỏ quảng cáo", price = "79.000 ₫", oneTime = true,
                description = "Mọi phần thưởng \"xem quảng cáo\" nhận ngay, không phải xem" },
            new StoreProduct { id = Gems100, title = "Túi đá quý", price = "22.000 ₫", description = "100 ♦" },
            new StoreProduct { id = Gems550, title = "Hộp đá quý", price = "99.000 ₫", description = "550 ♦ (tặng thêm 10%)" },
            new StoreProduct { id = Gems1200, title = "Rương đá quý", price = "199.000 ₫", description = "1200 ♦ (tặng thêm 20%)" },
        };

        public static IStoreProvider Provider = new SimulatedStoreProvider();

        public static StoreProduct Find(string id) => Products.Find(p => p.id == id);

        public static bool Owned(GameState state, string id) =>
            (id == Starter && state.starterPackBought) || (id == NoAds && state.noAds);

        public static bool CanBuy(GameState state, StoreProduct p) => !(p.oneTime && Owned(state, p.id));

        public static void Buy(RestaurantController game, string id, Action<bool> onDone = null)
        {
            var p = Find(id);
            if (p == null || !CanBuy(game.State, p)) { onDone?.Invoke(false); return; }
            AnalyticsService.Log("iap_start", ("product", id));
            Provider.Purchase(p, ok =>
            {
                if (ok)
                {
                    Grant(game, p);
                    AnalyticsService.Log("iap_purchase", ("product", id), ("price", p.price));
                }
                else AnalyticsService.Log("iap_cancel", ("product", id));
                onDone?.Invoke(ok);
            });
        }

        private static void Grant(RestaurantController game, StoreProduct p)
        {
            var s = game.State;
            switch (p.id)
            {
                case Starter:
                    s.starterPackBought = true;
                    s.gems += 500;
                    game.StartBoost(1800f);
                    game.GrantChest(3, "Gói khởi đầu · rương sự kiện 3 món", Rarity.SuThi);
                    break;
                case MonthlyCard:
                    s.gems += MonthlyCardInstantGems;
                    s.monthlyCardDays += MonthlyCardDays; // mua thêm thì cộng dồn ngày
                    break;
                case NoAds: s.noAds = true; break;
                case Gems100: s.gems += 100; break;
                case Gems550: s.gems += 550; break;
                case Gems1200: s.gems += 1200; break;
            }
            SaveService.Save(s);
        }

        // ---------- Thẻ tháng: nhận đá quý mỗi ngày ----------

        private static string Today => DateTime.Today.ToString("yyyy-MM-dd");

        public static bool CanClaimMonthly(GameState s) => s.monthlyCardDays > 0 && s.monthlyLastClaim != Today;

        public static bool ClaimMonthly(GameState s)
        {
            if (!CanClaimMonthly(s)) return false;
            s.monthlyLastClaim = Today;
            s.monthlyCardDays--;
            s.gems += MonthlyCardDailyGems;
            AnalyticsService.Log("monthly_claim", ("days_left", s.monthlyCardDays));
            SaveService.Save(s);
            return true;
        }
    }
}
