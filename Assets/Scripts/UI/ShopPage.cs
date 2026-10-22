using System.Collections.Generic;
using PhoAmThucViet.Audio;
using PhoAmThucViet.Core;
using PhoAmThucViet.Gameplay;
using PhoAmThucViet.Monetization;
using UnityEngine;
using UnityEngine.UI;

namespace PhoAmThucViet.UI
{
    /// <summary>
    /// Tab Cửa hàng: quà xem quảng cáo (đá quý, rương), thẻ tháng, gói khởi đầu, bỏ quảng cáo, gói đá quý.
    /// Bản chơi thử: quảng cáo và thanh toán đều giả lập (SimOverlay).
    /// </summary>
    public class ShopPage
    {
        private readonly RestaurantController _game;

        private class Card { public Image bg; public Text title, desc; public UIButton buy; public string productId; }
        private Card _freeGems, _freeChest, _monthly, _starter, _noAds, _smallChest, _bigChest;
        private Text _smallRates, _bigRates;
        private Card _profit, _tip, _adValue, _adDuration;
        private readonly List<Card> _gemCards = new List<Card>();
        private Text _note;

        public ShopPage(RestaurantController game) => _game = game;

        /// <summary>Có quà miễn phí chờ nhận (để tab hiện "(!)").</summary>
        public bool HasFreeReward
        {
            get
            {
                var s = _game.State;
                return StoreService.CanClaimMonthly(s) || AdsService.Remaining(s, AdsService.FreeGems) > 0 ||
                       AdsService.Remaining(s, AdsService.FreeChest) > 0;
            }
        }

        public void Build(RectTransform root)
        {
            // Trang cuộn được (cửa hàng dài hơn một màn hình)
            var viewport = UIKit.Panel("Viewport", root, new Color(1, 1, 1, 0.01f)).rectTransform;
            viewport.Place(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();
            var page = UIKit.NewRect("Content", viewport);
            page.anchorMin = new Vector2(0, 1);
            page.anchorMax = new Vector2(1, 1);
            page.pivot = new Vector2(0.5f, 1);
            page.offsetMin = page.offsetMax = Vector2.zero;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = page;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40;

            var title = UIKit.Label("Title", page, "Cửa hàng", 40, Palette.Gold, TextAnchor.MiddleLeft, true);
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -76), new Vector2(-28, -16));
            _note = UIKit.Label("Note", page, "", 22, new Color(1, 1, 1, 0.65f), TextAnchor.MiddleLeft);
            _note.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -116), new Vector2(-28, -78));

            float y = 130;
            // Quà xem quảng cáo: 2 thẻ cạnh nhau
            _freeGems = MakeCard(page, 0, 0.5f, y, 210, new Color(0.20f, 0.42f, 0.62f), OnFreeGems);
            _freeChest = MakeCard(page, 0.5f, 1, y, 210, new Color(0.45f, 0.30f, 0.62f), OnFreeChest);
            y += 226;
            // Rương đá quý: rương nhỏ / rương lớn, có bảng tỉ lệ
            _smallChest = MakeChestCard(page, 0, 0.5f, y, ChestTypes.Small, new Color(0.45f, 0.30f, 0.16f), out _smallRates);
            _bigChest = MakeChestCard(page, 0.5f, 1, y, ChestTypes.Big, new Color(0.62f, 0.30f, 0.10f), out _bigRates);
            y += 396;
            _profit = MakeCard(page, 0, 1, y, 174, new Color(0.18f, 0.50f, 0.25f), OnBuyProfit);
            y += 190;
            _tip = MakeCard(page, 0, 1, y, 174, new Color(0.60f, 0.42f, 0.08f), OnUpgradeTip);
            y += 190;
            _adValue = MakeCard(page, 0, 1, y, 174, new Color(0.18f, 0.40f, 0.58f), () => { if (_game.TryUpgradeAdValue()) SoundFX.Play(Sfx.Upgrade); });
            y += 190;
            _adDuration = MakeCard(page, 0, 1, y, 174, new Color(0.30f, 0.32f, 0.60f), () => { if (_game.TryUpgradeAdDuration()) SoundFX.Play(Sfx.Upgrade); });
            y += 190;
            _monthly = MakeCard(page, 0, 1, y, 190, new Color(0.62f, 0.45f, 0.12f), OnMonthly);
            y += 206;
            _starter = MakeCard(page, 0, 1, y, 210, new Color(0.70f, 0.20f, 0.18f), () => Buy(StoreService.Starter));
            _starter.productId = StoreService.Starter;
            y += 226;
            _noAds = MakeCard(page, 0, 1, y, 150, new Color(0.30f, 0.30f, 0.34f), () => Buy(StoreService.NoAds));
            _noAds.productId = StoreService.NoAds;
            y += 166;
            string[] gems = { StoreService.Gems100, StoreService.Gems550, StoreService.Gems1200 };
            for (int i = 0; i < gems.Length; i++)
            {
                string id = gems[i];
                var c = MakeCard(page, i / 3f, (i + 1) / 3f, y, 230, new Color(0.15f, 0.35f, 0.55f), () => Buy(id), vertical: true);
                c.productId = id;
                var p = StoreService.Find(id);
                c.title.text = p.title;
                c.desc.text = p.description;
                _gemCards.Add(c);
            }
            page.sizeDelta = new Vector2(0, y + 230 + 30);
        }

        private Card MakeChestCard(RectTransform page, float x0, float x1, float y, ChestType type, Color color, out Text rates)
        {
            var c = MakeCard(page, x0, x1, y, 380, color, () => OnBuyChest(type), vertical: true);
            c.title.text = type.name;
            c.desc.text = type.Rate(Rarity.HuyenThoai) > 0
                ? $"{type.items} món · có Huyền thoại"
                : $"{type.items} món · không ra Huyền thoại";
            c.desc.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -96), new Vector2(-10, -58));
            rates = UIKit.Label("Rates", c.bg.rectTransform, RatesText(type), 22, Color.white, TextAnchor.UpperLeft);
            rates.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 1), new Vector2(24, 100), new Vector2(-16, -104));
            return c;
        }

        private static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(Color.Lerp(c, Color.white, 0.35f));

        /// <summary>Bảng tỉ lệ mỗi món theo độ hiếm (tô màu độ hiếm), kèm tỉ lệ cả rương có Huyền thoại.</summary>
        private static string RatesText(ChestType type)
        {
            var sb = new System.Text.StringBuilder("Tỉ lệ mỗi món:\n");
            for (int r = 0; r < 4; r++)
            {
                var rarity = (Rarity)r;
                sb.Append($"<color=#{Hex(ChefPage.RarityColors[r])}>{EquipmentData.RarityName(rarity)}</color>: {type.Rate(rarity) * 100:0.#}%\n");
            }
            double legend = type.AtLeastOne(Rarity.HuyenThoai);
            sb.Append(legend > 0 ? $"Cả rương có Huyền thoại: ~{legend * 100:0}%" : "Không có Huyền thoại");
            return sb.ToString();
        }

        private Card MakeCard(RectTransform page, float x0, float x1, float top, float height, Color color,
                              System.Action onBuy, bool vertical = false)
        {
            var c = new Card();
            c.bg = UIKit.Panel("Card", page, color, UIKit.Rounded);
            c.bg.rectTransform.Place(new Vector2(x0, 1), new Vector2(x1, 1), new Vector2(x0 == 0 ? 20 : 8, -(top + height)),
                new Vector2(x1 == 1 ? -20 : -8, -top));
            var t = c.bg.rectTransform;
            c.title = UIKit.Label("Title", t, "", 30, Palette.Gold, TextAnchor.MiddleLeft, true);
            c.desc = UIKit.Label("Desc", t, "", 22, Color.white, TextAnchor.UpperLeft);
            c.buy = UIKit.MakeButton("Buy", t, "", 26, Palette.Green, () => onBuy());
            if (vertical)
            {
                c.title.alignment = TextAnchor.MiddleCenter;
                c.desc.alignment = TextAnchor.UpperCenter;
                c.title.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -56), new Vector2(-10, -10));
                c.desc.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 1), new Vector2(10, 100), new Vector2(-10, -60));
                c.buy.bg.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(14, 14), new Vector2(-14, 90));
            }
            else
            {
                c.title.rectTransform.Place(new Vector2(0, 1), new Vector2(0.66f, 1), new Vector2(22, -56), new Vector2(0, -10));
                c.desc.rectTransform.Place(new Vector2(0, 0), new Vector2(0.66f, 1), new Vector2(22, 12), new Vector2(0, -60));
                c.buy.bg.rectTransform.Place(new Vector2(0.66f, 0), new Vector2(1, 1), new Vector2(8, 18), new Vector2(-18, -18));
            }
            return c;
        }

        public void Update()
        {
            var s = _game.State;
            _note.text = $"Đang có {s.gems} ♦ · " + (s.noAds
                ? "đã Bỏ quảng cáo · mua hàng giả lập (bản thử)"
                : "quảng cáo và mua hàng giả lập, không trừ tiền thật");

            int gemsLeft = AdsService.Remaining(s, AdsService.FreeGems);
            _freeGems.title.text = $"{_game.AdFreeGems} ♦ miễn phí";
            _freeGems.desc.text = $"Xem quảng cáo · còn {gemsLeft}/{AdsService.DailyLimits[AdsService.FreeGems]} lần hôm nay";
            _freeGems.buy.Set(gemsLeft > 0 ? (s.noAds ? "Nhận" : "Xem QC") : "Hết lượt", gemsLeft > 0 && !AdsService.Busy);
            // Thẻ nhỏ: nút nằm dưới
            int chestLeft = AdsService.Remaining(s, AdsService.FreeChest);
            _freeChest.title.text = "Rương 1 món";
            _freeChest.desc.text = $"Xem quảng cáo · còn {chestLeft}/{AdsService.DailyLimits[AdsService.FreeChest]} lần hôm nay";
            _freeChest.buy.Set(chestLeft > 0 ? (s.noAds ? "Nhận" : "Xem QC") : "Hết lượt", chestLeft > 0 && !AdsService.Busy);

            _smallChest.buy.Set($"Mua · {ChestTypes.Small.price} ♦", s.gems >= ChestTypes.Small.price);
            _bigChest.buy.Set($"Mua · {ChestTypes.Big.price} ♦", s.gems >= ChestTypes.Big.price);

            float left = _game.ProfitBoostSecondsLeft;
            _profit.title.text = left > 0 ? $"Lợi nhuận x5 · còn {(int)left / 60}:{(int)left % 60:00}" : "Lợi nhuận x5 trong 5 phút";
            _profit.desc.text = left > 0 ? "Đang chạy: mọi món bán giá x5. Mua thêm để cộng 5 phút." : "Mọi món bán giá x5 trong 5 phút (mua tiếp thì cộng dồn thời gian).";
            _profit.buy.Set($"{(left > 0 ? "+5 phút" : "Mua")}\n{RestaurantController.ProfitBoostPrice} ♦", s.gems >= RestaurantController.ProfitBoostPrice);

            int tl = _game.TipLevel;
            bool tipMax = tl >= _game.TipMaxLevel;
            _tip.title.text = $"Khách hào phóng · cấp {tl}/{_game.TipMaxLevel}";
            _tip.desc.text = tipMax
                ? $"Mọi khách đều để tip ({_game.TipChance * 100:0}%). Tip = {RestaurantController.TipFraction * 100:0}% tiền bữa, chạm đồng xu trên bàn để nhận."
                : $"Tỉ lệ khách để tip: {_game.TipChance * 100:0}% → {_game.NextTipChance * 100:0}%. Tip = {RestaurantController.TipFraction * 100:0}% tiền bữa, chạm đồng xu trên bàn để nhận.";
            _tip.buy.Set(tipMax ? "Tối đa" : $"Nâng\n{_game.TipUpgradePrice} ♦", !tipMax && s.gems >= _game.TipUpgradePrice);

            // Nâng cấp quảng cáo
            // Nâng vô hạn cấp, giá tăng theo cấp số nhân
            int vl = _game.AdValueLevel, dl = _game.AdDurationLevel;
            _adValue.title.text = $"Quảng cáo hậu hĩnh · cấp {vl}";
            _adValue.desc.text = $"Tăng tốc x{RestaurantController.AdBoostMultiplierAt(vl):0.#} → x{RestaurantController.AdBoostMultiplierAt(vl + 1):0.#} · offline x{RestaurantController.AdBoostMultiplierAt(vl):0.#} → x{RestaurantController.AdBoostMultiplierAt(vl + 1):0.#} · đá quý {RestaurantController.AdFreeGemsAt(vl)} → {RestaurantController.AdFreeGemsAt(vl + 1)} ♦";
            long vp = RestaurantController.AdUpgradeCost(vl);
            _adValue.buy.Set($"Nâng\n{vp:N0} ♦", s.gems >= vp);
            _adDuration.title.text = $"Quảng cáo bền lâu · cấp {dl}";
            _adDuration.desc.text = $"Thời gian tăng tốc khi xem quảng cáo: {RestaurantController.AdBoostSecondsAt(dl) / 60:0.#} → {RestaurantController.AdBoostSecondsAt(dl + 1) / 60:0.#} phút.";
            long dp = RestaurantController.AdUpgradeCost(dl);
            _adDuration.buy.Set($"Nâng\n{dp:N0} ♦", s.gems >= dp);

            var monthly = StoreService.Find(StoreService.MonthlyCard);
            _monthly.title.text = s.monthlyCardDays > 0 ? $"Thẻ tháng · còn {s.monthlyCardDays} ngày" : monthly.title;
            _monthly.desc.text = monthly.description;
            if (StoreService.CanClaimMonthly(s)) _monthly.buy.Set($"Nhận {StoreService.MonthlyCardDailyGems} ♦", true);
            else if (s.monthlyCardDays > 0) _monthly.buy.Set($"Đã nhận hôm nay\nGia hạn {monthly.price}", true);
            else _monthly.buy.Set(monthly.price, true);

            foreach (var c in new[] { _starter, _noAds })
            {
                var p = StoreService.Find(c.productId);
                c.title.text = p.title;
                c.desc.text = p.description;
                bool owned = StoreService.Owned(s, p.id);
                c.buy.Set(owned ? "Đã mua" : p.price, !owned);
            }
            foreach (var c in _gemCards) c.buy.Set(StoreService.Find(c.productId).price, true);
        }

        private void OnFreeGems()
        {
            var s = _game.State;
            AdsService.ShowRewarded(s, AdsService.FreeGems, () =>
            {
                s.gems += _game.AdFreeGems;
                SoundFX.Play(Sfx.Coin);
            });
        }

        private void OnFreeChest()
        {
            AdsService.ShowRewarded(_game.State, AdsService.FreeChest, () => _game.GrantChest(1, "Rương quà quảng cáo · 1 món"));
        }

        private void OnBuyChest(ChestType type)
        {
            if (_game.TryBuyChest(type)) SoundFX.Play(Sfx.Chest);
        }

        private void OnUpgradeTip()
        {
            if (_game.TryUpgradeTip()) SoundFX.Play(Sfx.Upgrade);
        }

        private void OnBuyProfit()
        {
            if (_game.TryBuyProfitBoost()) SoundFX.Play(Sfx.LevelUp, 0.6f);
        }

        private void OnMonthly()
        {
            var s = _game.State;
            if (StoreService.CanClaimMonthly(s)) { if (StoreService.ClaimMonthly(s)) SoundFX.Play(Sfx.Coin); }
            else Buy(StoreService.MonthlyCard);
        }

        private void Buy(string id)
        {
            SoundFX.Play(Sfx.Click);
            StoreService.Buy(_game, id, ok => { if (ok) SoundFX.Play(Sfx.Chest); });
        }
    }
}
