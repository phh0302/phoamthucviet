using System.Collections.Generic;
using PhoAmThucViet.Audio;
using PhoAmThucViet.Core;
using PhoAmThucViet.Gameplay;
using PhoAmThucViet.Monetization;
using PhoAmThucViet.Visual;
using UnityEngine;
using UnityEngine.UI;

namespace PhoAmThucViet.UI
{
    /// <summary>
    /// Giao diện thật (uGUI, dựng bằng code). Cảnh quán chiếm toàn màn hình; giao diện chỉ nằm đè ở mép:
    /// thanh thông tin + dải tiến độ lên đời ở trên, nút phục vụ / tăng tốc / LÊN ĐỜI và thanh tab ở dưới.
    /// Chạm vào một quầy → ô nâng cấp nhỏ hiện ngay cạnh quầy đó. Tab Đầu bếp / Nhiệm vụ mở trang phủ lên cảnh.
    /// </summary>
    [RequireComponent(typeof(RestaurantController))]
    public class GameUI : MonoBehaviour
    {
        public enum Tab { Restaurant, Chef, Quests, Shop }

        public const float TabBarHeight = 130f;
        public const float HudHeight = 120f;
        private const float StripHeight = 110f;     // dải tiến độ dưới thanh thông tin
        private const float DebugRowHeight = 64f;   // hàng nút thử nghiệm dưới dải tiến độ
        private const float BottomRowHeight = 100f; // hàng nút phục vụ / tăng tốc trên thanh tab
        private const float PopupWidth = 600f;
        private const float PopupHeight = 340f, PopupHeightLocked = 250f;

        public Tab ActiveTab { get; private set; } = Tab.Restaurant;

        private RestaurantController _game;
        private RestaurantView _view;
        private Onboarding _onboarding;
        private HelperPanel _helperPanel;
        private UIButton _helperBtn;
        private LeaderboardPanel _boardPanel;
        private UIButton _boardBtn;
        private Canvas _canvas;
        private RectTransform _canvasRect;

        // Thanh trên cùng
        private Text _cityText, _venueText, _moneyText, _gemText;

        // Lớp nổi của tab Quán
        private RectTransform _restaurantLayer;
        private Text _progressTitle, _progressPct;
        private Image _progressFill;
        private readonly Text[] _chips = new Text[5];
        private UIButton _levelUpBtn, _boostBtn, _upgradesBtn, _eventRunBtn, _bestSellerBtn;
        /// <summary>Hủy best seller phải bấm 2 lần trong 2 giây (tránh bấm nhầm mất 10 phút hồi chiêu).</summary>
        private float _bestSellerCancelUntil;
        private static readonly Color BestSellerColor = new Color(0.90f, 0.35f, 0.10f);

        // Bảng cải tiến (phục vụ chạy nhanh, đầu bếp nấu nhanh...)
        private RectTransform _upgradePanel;
        private class UpgradeRow { public Image bg; public Text name, effect; public UIButton buy; }
        private readonly List<UpgradeRow> _upgradeRows = new List<UpgradeRow>();   // cải tiến tốc độ
        private UpgradeRow _chefRow, _waiterRow, _tableRow;                         // người và bàn
        private readonly List<UpgradeRow> _recipeRows = new List<UpgradeRow>();    // nâng cấp món (1 hàng / quầy)
        private Text _headStaff, _headRecipes, _headSpeed;
        private RectTransform _upgradeList;
        private ScrollRect _upgradeScroll;
        private Text _upgradeInfo;
        private bool _upgradesOpen;

        // Trang phủ (Đầu bếp, Nhiệm vụ) và thanh tab
        private RectTransform _pages, _chefPageRoot, _questPageRoot;
        private ChefPage _chefPage;
        private QuestPage _questPage;
        private UIButton _tabRestaurant, _tabChef, _tabQuests, _tabShop, _soundBtn;
        private RectTransform _shopPageRoot;
        private ShopPage _shopPage;
        private PopupLayer _popups;
        private SettingsPanel _settings;

        /// <summary>Nút Nâng tự chuyển sang +10 / +5 khi đủ tiền; không thì +1.</summary>
        private static readonly int[] UpgradeSteps = { 10, 5 };

        /// <summary>Ô nâng cấp nhỏ hiện cạnh quầy đang chọn.</summary>
        private class StationPopup
        {
            public RectTransform root, arrow;
            public Image bg;
            public Text name, level, info;
            public Image levelFill;
            public UIButton upgrade, unlock, bestSeller;
            public int upgradeCount = 1; // số cấp nút Nâng đang mua
            public StationData shownFor;
            public float openTime;
        }

        private StationPopup _pop;

        private void Awake()
        {
            _game = GetComponent<RestaurantController>();
            _view = GetComponent<RestaurantView>();
            if (_view == null) _view = gameObject.AddComponent<RestaurantView>();
        }

        private void Start() => Build();

        // ================= Dựng giao diện =================

        private void Build()
        {
            _canvas = UIKit.CreateCanvas("Game UI", 10);
            _canvasRect = (RectTransform)_canvas.transform;
            var root = _canvas.transform;

            BuildRestaurantLayer(root);
            BuildPages(root);
            BuildHud(root);
            BuildTabBar(root);
            _popups = new PopupLayer(_game);
            _popups.Build(root); // dựng sau cùng để nằm trên mọi thứ
            _settings = new SettingsPanel(_game);
            _settings.Build(root);
            SelectTab(Tab.Restaurant);
        }

        private void BuildHud(Transform root)
        {
            var hud = UIKit.Panel("HUD", root, Palette.HudBg).rectTransform;
            hud.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -HudHeight), Vector2.zero);

            _cityText = UIKit.Label("City", hud, "", 30, Color.white, TextAnchor.MiddleLeft, true);
            _cityText.rectTransform.TopLeft(28, 10, 520, 46);
            _venueText = UIKit.Label("Venue", hud, "", 26, Palette.Gold, TextAnchor.MiddleLeft);
            _venueText.rectTransform.TopLeft(28, 58, 520, 44);

            var gemPill = UIKit.Panel("Gems", hud, new Color(0.15f, 0.35f, 0.55f, 0.95f), UIKit.Pill).rectTransform;
            gemPill.TopRight(24, 22, 170, 76);
            _gemText = UIKit.Label("Text", gemPill, "", 30, Color.white, TextAnchor.MiddleCenter, true);
            _gemText.rectTransform.Place(Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0));

            var moneyPill = UIKit.Panel("Money", hud, new Color(0.45f, 0.30f, 0.05f, 0.95f), UIKit.Pill).rectTransform;
            moneyPill.TopRight(210, 22, 300, 76);
            _moneyText = UIKit.Label("Text", moneyPill, "", 36, Palette.Gold, TextAnchor.MiddleCenter, true);
            _moneyText.rectTransform.Place(Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0));
        }

        /// <summary>Mọi thứ nổi trên cảnh quán khi đang ở tab Quán.</summary>
        private void BuildRestaurantLayer(Transform root)
        {
            _restaurantLayer = UIKit.NewRect("RestaurantLayer", root);
            _restaurantLayer.Place(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var layer = _restaurantLayer;

            // ----- Dải tiến độ lên đời (ngay dưới thanh thông tin) -----
            var strip = UIKit.Panel("Progress", layer, new Color(0, 0, 0, 0.55f), UIKit.Rounded).rectTransform;
            strip.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -(HudHeight + 6 + StripHeight)), new Vector2(-12, -(HudHeight + 6)));
            _progressTitle = UIKit.Label("Title", strip, "", 26, Color.white, TextAnchor.MiddleLeft, true);
            _progressTitle.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -42), new Vector2(-130, -6));
            _progressPct = UIKit.Label("Pct", strip, "", 26, Palette.Gold, TextAnchor.MiddleRight, true);
            _progressPct.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-130, -42), new Vector2(-20, -6));
            _progressFill = UIKit.ProgressBar("Bar", strip, new Color(0, 0, 0, 0.45f), Palette.Gold, out var bar);
            bar.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -66), new Vector2(-20, -46));
            for (int i = 0; i < _chips.Length; i++)
            {
                float w = 1f / _chips.Length;
                _chips[i] = UIKit.Label("Chip" + i, strip, "", 20, Color.white, TextAnchor.MiddleCenter);
                _chips[i].rectTransform.Place(new Vector2(i * w, 0), new Vector2((i + 1) * w, 0), new Vector2(3, 6), new Vector2(-3, 40));
            }

            // ----- Nút thử nghiệm (chỉ trong bản chơi thử) -----
            float debugY = HudHeight + 6 + StripHeight + 8;
            var skip = UIKit.MakeButton("DebugSkip", layer, "Thử: qua mốc", 20, new Color(0.3f, 0.3f, 0.3f, 0.8f),
                () => { SoundFX.Play(Sfx.LevelUp); _game.DebugSkipVenue(); }, UIKit.Pill);
            skip.bg.rectTransform.TopLeft(12, debugY, 190, DebugRowHeight - 12);
            var reset = UIKit.MakeButton("DebugReset", layer, "Thử: xóa save", 20, new Color(0.3f, 0.3f, 0.3f, 0.8f),
                () => { SoundFX.Play(Sfx.Click); _game.ResetSave(); }, UIKit.Pill);
            reset.bg.rectTransform.TopLeft(212, debugY, 190, DebugRowHeight - 12);
            skip.bg.gameObject.SetActive(DebugTools.Enabled);

            // Nút qua lại quán sự kiện (chỉ hiện khi có sự kiện)
            _eventRunBtn = UIKit.MakeButton("EventRun", layer, "", 22, new Color(0.8f, 0.18f, 0.15f), OnEventRun, UIKit.Pill);
            _eventRunBtn.bg.rectTransform.TopRight(12, debugY, 330, DebugRowHeight - 12);
            // Bảng xếp hạng sự kiện (cạnh nút quán sự kiện, chỉ hiện khi có sự kiện)
            _boardBtn = UIKit.MakeButton("Leaderboard", layer, "Xếp hạng", 22, new Color(0.55f, 0.35f, 0.75f), ToggleBoard, UIKit.Pill);
            _boardBtn.bg.rectTransform.TopRight(352, debugY, 200, DebugRowHeight - 12);
            _boardBtn.label.color = Palette.Gold;
            _eventRunBtn.label.color = Palette.Gold;
            reset.bg.gameObject.SetActive(DebugTools.Enabled);

            // ----- Hàng nút phía dưới (trên thanh tab) -----
            _upgradesBtn = UIKit.MakeButton("Upgrades", layer, "Nâng cấp quán", 28, Palette.Orange, ToggleUpgrades, UIKit.Pill);
            PlaceBottom(_upgradesBtn.bg.rectTransform, 0, 16, 380);
            _boostBtn = UIKit.MakeButton("Boost", layer, "", 26, Palette.Blue, OnBoost, UIKit.Pill);
            PlaceBottom(_boostBtn.bg.rectTransform, 1, -16, 280);
            // Best seller đang chạy / đang hồi chiêu: ô giữa hàng dưới, chạm để hủy
            _bestSellerBtn = UIKit.MakeButton("BestSeller", layer, "", 24, BestSellerColor, OnBestSellerHud, UIKit.Pill);
            var bs = _bestSellerBtn.bg.rectTransform;
            bs.anchorMin = bs.anchorMax = new Vector2(0.5f, 0);
            bs.pivot = new Vector2(0.5f, 0);
            bs.anchoredPosition = new Vector2(0, TabBarHeight + 14);
            bs.sizeDelta = new Vector2(370, BottomRowHeight - 10);
            _bestSellerBtn.label.color = Palette.Gold;

            // Nút LÊN ĐỜI to, đập nhịp, chỉ hiện khi đã nâng hết
            _levelUpBtn = UIKit.MakeButton("LevelUp", layer, "", 38, Palette.Red, OnLevelUp);
            var lu = _levelUpBtn.bg.rectTransform;
            lu.anchorMin = lu.anchorMax = new Vector2(0.5f, 0);
            lu.pivot = new Vector2(0.5f, 0);
            lu.anchoredPosition = new Vector2(0, TabBarHeight + BottomRowHeight + 30);
            lu.sizeDelta = new Vector2(760, 130);
            _levelUpBtn.label.color = Palette.Gold;

            // Mục tiêu tân thủ + gợi ý (dựng trước popup quầy và bảng nâng cấp để nằm dưới, không che chúng)
            _onboarding = new Onboarding(_game, _view, layer, _upgradesBtn.bg.rectTransform,
                HudHeight + 6 + StripHeight + DebugRowHeight + 4, TabBarHeight + BottomRowHeight + 16);
            // Nút mượn đầu bếp (góc phải, ngang thẻ mục tiêu)
            _helperBtn = UIKit.MakeButton("Helper", layer, "", 22, new Color(0.13f, 0.52f, 0.47f), ToggleHelper, UIKit.Pill);
            _helperBtn.bg.rectTransform.TopRight(12, HudHeight + 6 + StripHeight + DebugRowHeight + 4, 300, 88);
            BuildStationPopup(layer);
            BuildUpgradePanel(layer);
            _helperPanel = new HelperPanel(_game);
            _helperPanel.Build(layer, TabBarHeight + BottomRowHeight + 20, HudHeight + 6 + StripHeight + 8);
            _boardPanel = new LeaderboardPanel(_game);
            _boardPanel.Build(layer, TabBarHeight + BottomRowHeight + 20, HudHeight + 6 + StripHeight + 8);
        }

        private void BuildUpgradePanel(RectTransform layer)
        {
            var bg = UIKit.Panel("UpgradePanel", layer, Palette.PanelBg, UIKit.Rounded);
            _upgradePanel = bg.rectTransform;
            _upgradePanel.Place(Vector2.zero, Vector2.one, new Vector2(20, TabBarHeight + BottomRowHeight + 20),
                new Vector2(-20, -(HudHeight + 6 + StripHeight + 8)));
            bg.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0, -6);

            var title = UIKit.Label("Title", _upgradePanel, "Nâng cấp quán", 36, Palette.Gold, TextAnchor.MiddleLeft, true);
            title.rectTransform.TopLeft(28, 16, 600, 56);
            var close = UIKit.MakeButton("Close", _upgradePanel, "Đóng", 28, new Color(0.55f, 0.45f, 0.38f), ToggleUpgrades);
            close.bg.rectTransform.TopRight(20, 14, 170, 64);
            _upgradeInfo = UIKit.Label("Info", _upgradePanel, "", 22, new Color(1, 1, 1, 0.75f), TextAnchor.MiddleLeft);
            _upgradeInfo.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -128), new Vector2(-28, -80));

            // Danh sách cuộn: Người và bàn → Nâng cấp món → Cải tiến tốc độ
            var viewport = UIKit.Panel("Viewport", _upgradePanel, new Color(1, 1, 1, 0.01f)).rectTransform;
            viewport.Place(Vector2.zero, Vector2.one, new Vector2(0, 12), new Vector2(0, -136));
            viewport.gameObject.AddComponent<RectMask2D>();
            _upgradeList = UIKit.NewRect("List", viewport);
            _upgradeList.anchorMin = new Vector2(0, 1);
            _upgradeList.anchorMax = new Vector2(1, 1);
            _upgradeList.pivot = new Vector2(0.5f, 1);
            _upgradeList.offsetMin = _upgradeList.offsetMax = Vector2.zero;
            _upgradeScroll = viewport.gameObject.AddComponent<ScrollRect>();
            _upgradeScroll.content = _upgradeList;
            _upgradeScroll.viewport = viewport;
            _upgradeScroll.horizontal = false;
            _upgradeScroll.movementType = ScrollRect.MovementType.Clamped;
            _upgradeScroll.scrollSensitivity = 40;

            _headStaff = SectionHeader("Người và bàn");
            _chefRow = MakeUpgradeRow("Thuê đầu bếp", OnHireChef, new Color(1f, 0.93f, 0.80f));
            _waiterRow = MakeUpgradeRow("Thuê phục vụ", OnHireWaiter, new Color(1f, 0.93f, 0.80f));
            _tableRow = MakeUpgradeRow("Mua thêm bàn", OnBuyTable, new Color(1f, 0.93f, 0.80f));
            _headRecipes = SectionHeader("Nâng cấp món (mỗi bậc giá món x2)");
            _headSpeed = SectionHeader("Cải tiến tốc độ");
            for (int i = 0; i < VenueUpgrades.All.Length; i++)
            {
                int index = i;
                var r = MakeUpgradeRow(VenueUpgrades.All[i].name, () => OnBuyUpgrade(index), Palette.Card);
                r.effect.text = VenueUpgrades.All[i].Effect;
                _upgradeRows.Add(r);
            }
            _upgradePanel.gameObject.SetActive(false);
        }

        private Text SectionHeader(string text)
        {
            var t = UIKit.Label("Section", _upgradeList, text, 26, Palette.Gold, TextAnchor.MiddleLeft, true);
            return t;
        }

        private const float RowHeight = 108f, RowGap = 10f, HeaderHeight = 50f;

        /// <summary>Xếp các mục theo chiều dọc trong danh sách cuộn, trả về vị trí y tiếp theo.</summary>
        private static float Stack(RectTransform rt, float y, float height, float indent = 20)
        {
            rt.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(indent, -(y + height)), new Vector2(-20, -y));
            return y + height + RowGap;
        }

        private UpgradeRow MakeUpgradeRow(string name, System.Action onBuy, Color color)
        {
            var r = new UpgradeRow();
            r.bg = UIKit.Panel("Row", _upgradeList, color, UIKit.Rounded);
            r.name = UIKit.Label("Name", r.bg.transform, name, 28, Palette.TextDark, TextAnchor.MiddleLeft, true);
            r.name.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0.62f, 1), new Vector2(22, 0), new Vector2(0, -6));
            r.effect = UIKit.Label("Effect", r.bg.transform, "", 22, Palette.TextSoft, TextAnchor.MiddleLeft);
            r.effect.rectTransform.Place(new Vector2(0, 0), new Vector2(0.62f, 0.5f), new Vector2(22, 6), Vector2.zero);
            r.buy = UIKit.MakeButton("Buy", r.bg.transform, "", 24, Palette.Green, () => onBuy());
            r.buy.bg.rectTransform.Place(new Vector2(0.62f, 0), new Vector2(1, 1), new Vector2(8, 12), new Vector2(-16, -12));
            r.bg.color = color;
            return r;
        }

        /// <summary>Hàng mua lặp lại (đầu bếp / phục vụ / bàn): x/tối đa, giá, hết thì "Đã đủ".</summary>
        private static void StaffRow(UpgradeRow r, string effect, bool canBuy, BigNum price, BigNum money)
        {
            r.effect.text = effect;
            if (canBuy) r.buy.Set($"Mua\n{price}", money >= price);
            else r.buy.Set("Đã đủ", false);
        }

        private void UpdateUpgrades(GameState state)
        {
            bool any = _game.CanBuyAnyUpgrade;
            _upgradesBtn.Set(any ? "Nâng cấp quán (!)" : "Nâng cấp quán", true);
            float k = any && !_upgradesOpen ? 1 + Mathf.Sin(Time.time * 5) * 0.04f : 1f;
            _upgradesBtn.bg.rectTransform.localScale = new Vector3(k, k, 1);

            _upgradePanel.gameObject.SetActive(_upgradesOpen);
            if (!_upgradesOpen) return;
            _upgradeInfo.text = $"Thu nhập ~{_game.EstimatedIncome}/giây · bưng món {_game.TripSeconds:0.0} giây/chuyến · bếp nhanh x{_game.CookSpeedUpgrade:0.##} · mất khi lên đời";

            // Xếp lại danh sách (số quầy đổi theo mốc)
            var stations = _game.Venue.stations;
            while (_recipeRows.Count < stations.Count)
            {
                int index = _recipeRows.Count;
                _recipeRows.Add(MakeUpgradeRow("", () => OnBuyRecipe(index), new Color(0.95f, 0.88f, 1f)));
            }
            float y = 0;
            y = Stack(_headStaff.rectTransform, y, HeaderHeight, 28);
            y = Stack(_chefRow.bg.rectTransform, y, RowHeight);
            y = Stack(_waiterRow.bg.rectTransform, y, RowHeight);
            y = Stack(_tableRow.bg.rectTransform, y, RowHeight);
            y = Stack(_headRecipes.rectTransform, y, HeaderHeight, 28);
            for (int i = 0; i < _recipeRows.Count; i++)
            {
                var r = _recipeRows[i];
                bool show = i < stations.Count;
                r.bg.gameObject.SetActive(show);
                if (!show) continue;
                y = Stack(r.bg.rectTransform, y, RowHeight);
                UpdateRecipeRow(r, stations[i], state);
            }
            y = Stack(_headSpeed.rectTransform, y, HeaderHeight, 28);
            foreach (var r in _upgradeRows) y = Stack(r.bg.rectTransform, y, RowHeight);
            _upgradeList.sizeDelta = new Vector2(0, y + 10);

            StaffRow(_chefRow, $"{state.chefs}/{_game.MaxChefs} người · nấu được mọi món", _game.CanHireChef, _game.ChefPrice, state.money);
            StaffRow(_waiterRow, $"{state.waiters}/{_game.MaxWaiters} người · bưng món ra bàn", _game.CanHireWaiter, _game.WaiterPrice, state.money);
            StaffRow(_tableRow, $"{state.tables}/{_game.MaxTables} bàn · thêm khách cùng lúc", _game.CanBuyTable, _game.TablePrice, state.money);
            for (int i = 0; i < _upgradeRows.Count; i++)
            {
                var r = _upgradeRows[i];
                if (_game.IsUpgradeBought(i))
                {
                    r.bg.color = Palette.CardLocked;
                    r.buy.Set("Đã mua", false);
                }
                else
                {
                    r.bg.color = Palette.Card;
                    var price = _game.UpgradeItemPrice(i);
                    r.buy.Set($"Mua\n{price}", state.money >= price);
                }
            }
        }

        /// <summary>Hàng nâng cấp món của một quầy: bậc tiếp theo, giá; quầy chưa mở hoặc đã nâng hết thì khóa nút.</summary>
        private void UpdateRecipeRow(UpgradeRow r, StationData data, GameState state)
        {
            r.name.text = data.displayName;
            int tier = _game.NextRecipeTier(data);
            int max = VenueUpgrades.RecipeTiers.Length;
            int done = tier < 0 ? max : tier;
            string now = $"giá món đang x{1 << done}";
            if (!_game.IsUnlocked(data))
            {
                r.effect.text = "Mở quầy này trước";
                r.buy.Set("Chưa mở", false);
                r.bg.color = Palette.CardLocked;
            }
            else if (tier < 0)
            {
                r.effect.text = $"Đã nâng hết {max}/{max} bậc · {now}";
                r.buy.Set("Đã mua hết", false);
                r.bg.color = Palette.CardLocked;
            }
            else
            {
                r.effect.text = $"Bậc {tier + 1}/{max}: {VenueUpgrades.RecipeTiers[tier]} · {now}";
                var price = _game.RecipePrice(data, tier);
                r.buy.Set($"Mua\n{price}", state.money >= price);
                r.bg.color = new Color(0.95f, 0.88f, 1f);
            }
        }

        private void ToggleBoard()
        {
            if (!_boardPanel.IsOpen) { _upgradesOpen = false; _view.ClearSelection(); if (_helperPanel.IsOpen) _helperPanel.Close(); }
            _boardPanel.Toggle();
        }

        private void ToggleHelper()
        {
            if (!_helperPanel.IsOpen) { _upgradesOpen = false; _view.ClearSelection(); if (_boardPanel.IsOpen) _boardPanel.Close(); }
            _helperPanel.Toggle();
        }

        /// <summary>Dòng trạng thái cơ chế riêng của quán sự kiện (null nếu không có gì để hiện).</summary>
        private string EventBuffText()
        {
            if (!_game.InEventRun) return null;
            if (_game.FireworksSecondsLeft > 0)
                return $"<color=#FF8A80>PHÁO HOA · tiền x3 · còn {Mathf.CeilToInt(_game.FireworksSecondsLeft)}s</color>";
            if (_game.EventSpeedSecondsLeft > 0)
                return $"<color=#FFD54F>Nấu nhanh x2 · còn {Mathf.CeilToInt(_game.EventSpeedSecondsLeft)}s</color>";
            switch (_game.Mechanic)
            {
                case RestaurantController.EventMechanic.Fireworks:
                    return $"Niềm tự hào <color=#FF8A80>{Mathf.FloorToInt(_game.Pride * 100)}%</color> · đầy thì bắn pháo hoa";
                case RestaurantController.EventMechanic.LiXi:
                    return _game.PickupVisible ? "<color=#FFD54F>Có lì xì rơi! Chạm để nhặt</color>" : "Thỉnh thoảng có lì xì may mắn rơi trong quán";
                case RestaurantController.EventMechanic.Lantern:
                    return _game.PickupVisible ? "<color=#FFD54F>Đèn lồng bay qua! Chạm để rước đèn</color>" : "Thỉnh thoảng có đèn lồng bay qua quán";
                case RestaurantController.EventMechanic.Gift:
                    return _game.PickupVisible ? "<color=#FFD54F>Có quà ông già Noel ở cửa! Chạm để mở</color>" : "Thỉnh thoảng ông già Noel để quà ở cửa";
            }
            return null;
        }

        /// <summary>Dải tiến độ quán sự kiện: hạng quán và số cấp cần cho hạng kế.</summary>
        private string EventRankTitle()
        {
            int rank = _game.EventRank, total = _game.EventTotalLevels, max = RestaurantController.EventRankLevels.Length;
            double diff = _game.EventDifficulty;
            string hard = diff > 1.001 ? $" · <color=#FF8A80>độ khó x{diff:0.#}</color>" : "";
            if (rank >= max) return $"Hạng quán {max}/{max} (tối đa) · nâng hết để nhận quà lớn{hard}";
            int need = RestaurantController.EventRankLevels[rank];
            var r = RestaurantController.EventRankReward(rank);
            string gift = r.chest > 0 ? $"rương {r.chest} món" : $"{r.gems} ♦";
            return $"Hạng quán {rank}/{max} · {total}/{need} cấp → {gift}{hard}";
        }

        private void UpdateHelperButton()
        {
            if (_game.HelperActive)
            {
                int s = Mathf.CeilToInt(_game.HelperSecondsLeft);
                _helperBtn.Set($"{_game.ActiveHelper.name}\nđang giúp · {s / 60}:{s % 60:00}", true);
            }
            else if (_game.HelperCooldownLeft > 0)
            {
                int s = Mathf.CeilToInt(_game.HelperCooldownLeft);
                _helperBtn.Set($"Mượn đầu bếp\nchờ {s / 60}:{s % 60:00}", true);
            }
            else _helperBtn.Set($"Mượn đầu bếp\n(còn {_game.HelperBorrowsLeft} lượt)", true);
            _helperPanel.Update();
            _boardPanel.Update();
        }

        private void ToggleUpgrades()
        {
            _upgradesOpen = !_upgradesOpen;
            if (_upgradesOpen) { _view.ClearSelection(); if (_helperPanel.IsOpen) _helperPanel.Close(); }
            SoundFX.Play(Sfx.Click);
        }

        private void OnBuyUpgrade(int index)
        {
            if (_game.TryBuyUpgrade(index)) SoundFX.Play(Sfx.LevelUp, 0.5f);
        }

        /// <summary>Đặt nút trong hàng ngay trên thanh tab, neo trái (side = 0) hoặc phải (side = 1).</summary>
        private static void PlaceBottom(RectTransform rt, float side, float x, float w)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(side, 0);
            rt.pivot = new Vector2(side, 0);
            rt.anchoredPosition = new Vector2(x, TabBarHeight + 14);
            rt.sizeDelta = new Vector2(w, BottomRowHeight - 10);
        }

        private void BuildStationPopup(RectTransform layer)
        {
            var p = new StationPopup();
            p.bg = UIKit.Panel("StationPopup", layer, Palette.Card, UIKit.Rounded);
            p.root = p.bg.rectTransform;
            p.root.anchorMin = p.root.anchorMax = Vector2.zero; // toạ độ tính từ góc dưới-trái canvas
            p.root.sizeDelta = new Vector2(PopupWidth, PopupHeight);
            p.bg.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0, -6);

            // Mũi nhọn chỉ vào quầy
            var arrow = UIKit.Panel("Arrow", p.root, Palette.Card);
            arrow.sprite = SpriteFactory.Square;
            p.arrow = arrow.rectTransform;
            p.arrow.sizeDelta = new Vector2(34, 34);
            p.arrow.localRotation = Quaternion.Euler(0, 0, 45);
            p.arrow.SetAsFirstSibling();

            var t = p.root;
            p.name = UIKit.Label("Name", t, "", 32, Palette.TextDark, TextAnchor.MiddleLeft, true);
            p.name.rectTransform.TopLeft(20, 10, 390, 50);
            p.level = UIKit.Label("Level", t, "", 26, Palette.TextSoft, TextAnchor.MiddleRight, true);
            p.level.rectTransform.TopRight(20, 10, 170, 50);
            p.levelFill = UIKit.ProgressBar("LevelBar", t, new Color(0, 0, 0, 0.12f), Palette.Green, out var bar);
            bar.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -82), new Vector2(-20, -62));
            p.info = UIKit.Label("Info", t, "", 22, Palette.TextSoft, TextAnchor.MiddleLeft);
            p.info.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -128), new Vector2(-20, -88));

            p.upgrade = UIKit.MakeButton("Upgrade", t, "", 32, Palette.Green, () => OnUpgrade(_view.Selected, _pop.upgradeCount));
            p.upgrade.bg.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -234), new Vector2(-16, -136));
            p.unlock = UIKit.MakeButton("Unlock", t, "", 32, Palette.Green, () => OnUnlock(_view.Selected));
            p.unlock.bg.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -234), new Vector2(-16, -136));
            p.bestSeller = UIKit.MakeButton("BestSeller", t, "", 26, BestSellerColor, () => OnBestSellerPopup(_view.Selected), UIKit.Pill);
            p.bestSeller.bg.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -326), new Vector2(-16, -246));
            p.bestSeller.label.color = Palette.Gold;

            p.root.gameObject.SetActive(false);
            _pop = p;
        }

        private void BuildPages(Transform root)
        {
            // Trang phủ lên cảnh, nằm giữa thanh thông tin và thanh tab
            _pages = UIKit.Panel("Pages", root, Palette.PanelBg).rectTransform;
            _pages.Place(Vector2.zero, Vector2.one, new Vector2(0, TabBarHeight), new Vector2(0, -HudHeight));

            _chefPageRoot = UIKit.NewRect("ChefPage", _pages);
            _chefPageRoot.Place(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _chefPage = new ChefPage(_game);
            _chefPage.Build(_chefPageRoot);

            _questPageRoot = UIKit.NewRect("QuestPage", _pages);
            _questPageRoot.Place(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _questPage = new QuestPage(_game);
            _questPage.Build(_questPageRoot);

            _shopPageRoot = UIKit.NewRect("ShopPage", _pages);
            _shopPageRoot.Place(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _shopPage = new ShopPage(_game);
            _shopPage.Build(_shopPageRoot);
        }

        private void BuildTabBar(Transform root)
        {
            var tabs = UIKit.Panel("Tabs", root, new Color(0.13f, 0.08f, 0.05f, 0.96f)).rectTransform;
            tabs.Place(Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, TabBarHeight));
            _tabRestaurant = UIKit.MakeButton("TabRestaurant", tabs, "Quán", 30, Palette.Orange, () => SelectTab(Tab.Restaurant));
            _tabRestaurant.bg.rectTransform.Place(new Vector2(0, 0), new Vector2(0.2f, 1), new Vector2(14, 14), new Vector2(-5, -14));
            _tabChef = UIKit.MakeButton("TabChef", tabs, "Đầu bếp", 30, Palette.Orange, () => SelectTab(Tab.Chef));
            _tabChef.bg.rectTransform.Place(new Vector2(0.2f, 0), new Vector2(0.42f, 1), new Vector2(5, 14), new Vector2(-5, -14));
            _tabQuests = UIKit.MakeButton("TabQuests", tabs, "Nhiệm vụ", 30, Palette.Orange, () => SelectTab(Tab.Quests));
            _tabQuests.bg.rectTransform.Place(new Vector2(0.42f, 0), new Vector2(0.66f, 1), new Vector2(5, 14), new Vector2(-5, -14));
            _tabShop = UIKit.MakeButton("TabShop", tabs, "Cửa hàng", 30, Palette.Orange, () => SelectTab(Tab.Shop));
            _tabShop.bg.rectTransform.Place(new Vector2(0.66f, 0), new Vector2(0.88f, 1), new Vector2(5, 14), new Vector2(-5, -14));
            _soundBtn = UIKit.MakeButton("Settings", tabs, "Cài\nđặt", 22, new Color(0.35f, 0.28f, 0.22f), OpenSettings);
            _soundBtn.bg.rectTransform.Place(new Vector2(0.88f, 0), new Vector2(1, 1), new Vector2(5, 14), new Vector2(-14, -14));
        }

        // ================= Cập nhật mỗi khung hình =================

        private void Update()
        {
            if (_canvas == null || _game.State == null) return;
            var state = _game.State;
            var venue = _game.Venue;
            _popups.Update();
            _settings.Update();

            // Cho cảnh quán biết phần mép bị giao diện che để xếp quầy/bàn vào vùng nhìn thấy
            float h = _canvasRect.rect.height;
            if (h > 1)
            {
                bool row = DebugTools.Enabled || EventService.Current != null; // hàng nút thử / nút quán sự kiện
                _view.topInset = (HudHeight + 6 + StripHeight + (row ? DebugRowHeight : 12)) / h;
                _view.bottomInset = (TabBarHeight + BottomRowHeight + 10) / h;
            }

            // Thanh trên cùng
            _cityText.text = _game.InEventRun
                ? $"{_game.CurrentEvent?.name ?? "Sự kiện"} · Quán sự kiện"
                : $"{_game.City.displayName} · Mốc {_game.VenueNumber}/{_game.VenueCount}" + (_game.Loop > 0 ? $" · Vòng {_game.LoopNumber}" : "");
            UpdateEventButton();
            string eventBuff = EventBuffText();
            _venueText.text = eventBuff != null ? eventBuff
                : _game.RushActive
                ? $"<color=#FFB74D>GIỜ CAO ĐIỂM · tiền x{RestaurantController.RushProfit:0} · còn {Mathf.CeilToInt(_game.RushSecondsLeft)}s</color>"
                : _game.ProfitBoostSecondsLeft > 0
                ? $"{venue.displayName} · <color=#7CFC7C>tiền x5 còn {Clock(_game.ProfitBoostSecondsLeft)}</color>"
                : venue.displayName;
            _moneyText.text = state.money.ToString();
            _gemText.text = $"♦ {state.gems}";

            // Ẩn/hiện theo tab
            bool restaurant = ActiveTab == Tab.Restaurant;
            _restaurantLayer.gameObject.SetActive(restaurant);
            _pages.gameObject.SetActive(!restaurant);
            _chefPageRoot.gameObject.SetActive(ActiveTab == Tab.Chef);
            _questPageRoot.gameObject.SetActive(ActiveTab == Tab.Quests);
            _shopPageRoot.gameObject.SetActive(ActiveTab == Tab.Shop);
            _tabShop.bg.color = ActiveTab == Tab.Shop ? Palette.Orange : new Color(0.35f, 0.28f, 0.22f);
            _tabShop.label.text = _shopPage.HasFreeReward ? "Cửa hàng (!)" : "Cửa hàng";
            var off = new Color(0.35f, 0.28f, 0.22f);
            _tabRestaurant.bg.color = restaurant ? Palette.Orange : off;
            _tabChef.bg.color = ActiveTab == Tab.Chef ? Palette.Orange : off;
            _tabQuests.bg.color = ActiveTab == Tab.Quests ? Palette.Orange : off;
            // Có quà/nhiệm vụ chờ nhận thì tab Nhiệm vụ hiện dấu "!"
            _tabQuests.label.text = QuestService.HasSomethingToClaim(state) || EventService.HasClaimable(state) ? "Nhiệm vụ (!)" : "Nhiệm vụ";
            var cloud = Online.CloudSync.Instance;
            _soundBtn.label.text = cloud != null && cloud.IsGuest ? "Cài\nđặt (!)" : "Cài\nđặt";
            if (ActiveTab == Tab.Chef) { _chefPage.Update(); return; }
            if (ActiveTab == Tab.Quests) { _questPage.Update(); return; }
            if (ActiveTab == Tab.Shop) { _shopPage.Update(); return; }

            int boostLeft = AdsService.Remaining(state, AdsService.Boost);
            string bx = $"x{_game.AdBoostMultiplier:0.#}";
            if (_game.BoostSecondsLeft > 0) _boostBtn.Set($"x{_game.SpeedMultiplier:0.#} còn {Clock(_game.BoostSecondsLeft)}", false);
            else if (boostLeft <= 0) _boostBtn.Set($"Tăng tốc {bx}\nhết lượt hôm nay", false);
            else _boostBtn.Set(state.noAds ? $"Tăng tốc {bx}\n(còn {boostLeft} lần)" : $"Tăng tốc {bx}\n(xem QC · {boostLeft})", !AdsService.Busy);
            UpdateBestSellerHud();
            UpdateProgress(venue);
            UpdateUpgrades(state);
            UpdateStationPopup(state, venue);
            UpdateHelperButton();
            _onboarding?.Update(_view.Selected != null || _upgradesOpen || _helperPanel.IsOpen || _boardPanel.IsOpen);
        }

        private void UpdateProgress(VenueData venue)
        {
            var p = _game.GetProgress();
            float pct = p.levelsTotal > 0 ? (float)p.levelsDone / p.levelsTotal : 0;
            string next = _game.InEventRun ? EventRankTitle()
                : _game.IsLastVenue
                ? (_game.City.nextCity != null ? $"Lên xe đi {_game.City.nextCity.displayName}"
                    : $"Xong hành trình → vòng {_game.LoopNumber + 1} (giá +{(RestaurantController.LoopCostFactor(_game.Loop + 1) - 1) * 100:0}%)")
                : $"Lên đời → {_game.NextVenue.displayName}";
            _progressTitle.text = next;
            _progressPct.text = $"{pct * 100:0}%";
            _progressFill.fillAmount = pct;

            Chip(0, "Quầy max", p.stationsMaxed, p.stationCount);
            Chip(1, "Bếp", p.chefs, p.maxChefs);
            Chip(2, "Phục vụ", p.waiters, p.maxWaiters);
            Chip(3, "Bàn", p.tables, p.maxTables);
            Chip(4, "Nâng cấp", p.upgrades, p.maxUpgrades);

            bool done = _game.IsVenueComplete();
            _levelUpBtn.bg.gameObject.SetActive(done);
            if (done)
            {
                string label = _game.InEventRun ? "NHẬN QUÀ QUÁN SỰ KIỆN!"
                    : _game.IsLastVenue
                    ? (_game.City.nextCity != null ? $"LÊN XE ĐI {_game.City.nextCity.displayName.ToUpper()}!" : $"VÒNG {_game.LoopNumber + 1}: VỀ LẠI {_game.startCity.displayName.ToUpper()}!")
                    : $"LÊN ĐỜI: {_game.NextVenue.displayName.ToUpper()}!";
                _levelUpBtn.Set(label, true);
                // Nhịp đập để mời bấm
                float k = 1 + Mathf.Sin(Time.time * 5) * 0.04f;
                _levelUpBtn.bg.rectTransform.localScale = new Vector3(k, k, 1);
            }
        }

        private void Chip(int i, string name, int have, int need)
        {
            bool ok = have >= need;
            _chips[i].text = $"{name} {have}/{need}";
            _chips[i].color = ok ? new Color(0.55f, 0.95f, 0.55f) : Color.white;
        }

        private void UpdateStationPopup(GameState state, VenueData venue)
        {
            var p = _pop;
            var data = _view != null ? _view.Selected : null;
            if (data != null && !venue.stations.Contains(data)) data = null;
            Vector2 topPx = default, bottomPx = default;
            if (data != null && _helperPanel.IsOpen) _helperPanel.Close(); // chạm quầy thì đóng bảng mượn đầu bếp
            bool show = data != null && !_upgradesOpen && !_game.IsVenueComplete() && _view.TryGetStationScreenRect(data, out topPx, out bottomPx);
            p.root.gameObject.SetActive(show);
            if (!show) { p.shownFor = null; return; }

            var b = _game.Bonuses;
            var st = state.GetStation(data.id);
            int level = st?.level ?? 0;
            bool unlocked = level > 0;

            // ----- Nội dung -----
            p.name.text = data.displayName;
            p.levelFill.fillAmount = (float)level / data.maxLevel;
            p.upgrade.bg.gameObject.SetActive(unlocked);
            p.bestSeller.bg.gameObject.SetActive(unlocked);
            p.unlock.bg.gameObject.SetActive(!unlocked);

            if (!unlocked)
            {
                bool canShow = _game.CanShowUnlock(data);
                p.level.text = "Chưa mở";
                p.info.text = canShow ? $"Mỗi món bán được {_game.DishPrice(data, 1)}" : "Quầy không thuộc quán này";
                var cost = _game.UnlockPrice(data);
                p.unlock.Set(canShow ? $"Mở quầy · {cost}" : "Đang khóa", canShow && state.money >= cost);
            }
            else
            {
                p.level.text = $"Cấp {level}/{data.maxLevel}";
                int nextDouble = EconomyService.NextMilestone(data, level);
                string dish = $"Mỗi món {_game.DishPrice(data, level)}";
                p.info.text = nextDouble > 0 ? $"{dish}  ·  cấp {nextDouble}: giá x2, +{DishStars.GemsPerStar} ♦" : $"{dish}  ·  đã nhân đôi hết";

                UpdateUpgradeRow(p, data, state, level);
                UpdateBestSellerRow(p, data);

            }

            // ----- Vị trí: ưu tiên phía trên quầy, không đủ chỗ thì xuống dưới -----
            float scale = _canvas.scaleFactor > 0 ? _canvas.scaleFactor : 1;
            float cw = _canvasRect.rect.width, ch = _canvasRect.rect.height;
            float height = unlocked ? PopupHeight : PopupHeightLocked;
            p.root.sizeDelta = new Vector2(PopupWidth, height);
            Vector2 top = topPx / scale, bottom = bottomPx / scale;
            const float gap = 26f;
            float ceiling = ch - HudHeight - 6;
            float floor = TabBarHeight + 8;
            bool above = top.y + gap + height <= ceiling;
            float x = Mathf.Clamp(top.x, PopupWidth / 2 + 10, cw - PopupWidth / 2 - 10);
            float y;
            if (above)
            {
                p.root.pivot = new Vector2(0.5f, 0);
                y = top.y + gap;
            }
            else
            {
                p.root.pivot = new Vector2(0.5f, 1);
                y = Mathf.Max(bottom.y - gap, floor + height);
            }
            p.root.anchoredPosition = new Vector2(x, y);

            // Mũi nhọn chỉ đúng vào quầy
            float arrowX = Mathf.Clamp(top.x - x, -PopupWidth / 2 + 40, PopupWidth / 2 - 40);
            p.arrow.anchorMin = p.arrow.anchorMax = new Vector2(0.5f, above ? 0 : 1);
            p.arrow.anchoredPosition = new Vector2(arrowX, 0);

            // Bật ra khi vừa mở / đổi quầy
            if (p.shownFor != data) { p.shownFor = data; p.openTime = Time.unscaledTime; }
            float t = Time.unscaledTime - p.openTime;
            float s = t < 0.12f ? Mathf.Lerp(0.75f, 1.04f, t / 0.12f) : Mathf.Lerp(1.04f, 1f, Mathf.Min(1, (t - 0.12f) / 0.08f));
            p.root.localScale = new Vector3(s, s, 1);
        }

        /// <summary>
        /// Một nút Nâng duy nhất: đủ tiền nâng 10 cấp thì thành "Nâng +10", đủ 5 cấp thì "Nâng +5", còn lại "Nâng +1".
        /// </summary>
        private void UpdateUpgradeRow(StationPopup p, StationData data, GameState state, int level)
        {
            if (level >= data.maxLevel) { p.upgradeCount = 1; p.upgrade.Set("Cấp tối đa", false); return; }

            int count = 1;
            foreach (int n in UpgradeSteps)
                if (level + n <= data.maxLevel && state.money >= _game.UpgradePrice(data, n)) { count = n; break; }
            p.upgradeCount = count;

            var price = _game.UpgradePrice(data, count);
            p.upgrade.Set($"Nâng +{count}\n{price}", state.money >= price);
            p.upgrade.bg.color = !p.upgrade.button.interactable ? p.upgrade.bg.color
                : count >= 10 ? new Color(0.10f, 0.50f, 0.20f) : count >= 5 ? new Color(0.20f, 0.60f, 0.25f) : Palette.Green;
        }

        // ================= Skill "Món best seller" =================

        private void UpdateBestSellerRow(StationPopup p, StationData data)
        {
            var active = _game.BestSellerStation;
            long cd = _game.BestSellerCooldownLeft;
            if (active == data)
            {
                bool confirm = Time.unscaledTime < _bestSellerCancelUntil;
                p.bestSeller.Set(confirm ? "Bấm lần nữa để hủy (hồi 10 phút)" : $"Best seller còn {Clock(_game.BestSellerSecondsLeft)} · Hủy", true);
                p.bestSeller.bg.color = Palette.Red;
            }
            else if (active != null) { p.bestSeller.Set($"Đang best seller: {active.displayName}", false); }
            else if (cd > 0) { p.bestSeller.Set($"Best seller hồi chiêu {Clock(cd)}", false); }
            else
            {
                p.bestSeller.Set($"Best seller {RestaurantController.BestSellerSeconds / 60} phút: {(int)(RestaurantController.BestSellerChance * 100)}% khách gọi món này", true);
                p.bestSeller.bg.color = BestSellerColor;
            }
        }

        private void UpdateBestSellerHud()
        {
            var active = _game.BestSellerStation;
            long cd = _game.BestSellerCooldownLeft;
            bool show = active != null || cd > 0;
            _bestSellerBtn.bg.gameObject.SetActive(show);
            if (!show) return;
            if (active != null)
            {
                bool confirm = Time.unscaledTime < _bestSellerCancelUntil;
                _bestSellerBtn.Set(confirm ? "Bấm lần nữa\nđể hủy" : $"Bán chạy: {active.displayName} {Clock(_game.BestSellerSecondsLeft)}\nchạm để hủy", true);
                _bestSellerBtn.bg.color = confirm ? Palette.Red : BestSellerColor;
                float k = 1 + Mathf.Sin(Time.time * 5) * 0.03f;
                _bestSellerBtn.bg.rectTransform.localScale = new Vector3(k, k, 1);
            }
            else
            {
                _bestSellerBtn.Set($"Best seller\nhồi chiêu {Clock(cd)}", false);
                _bestSellerBtn.bg.rectTransform.localScale = Vector3.one;
            }
        }

        private void OnBestSellerPopup(StationData data)
        {
            if (data == null) return;
            if (_game.BestSellerStation == data) { TryCancelBestSeller(); return; }
            if (_game.StartBestSeller(data)) SoundFX.Play(Sfx.LevelUp, 0.5f);
        }

        private void OnBestSellerHud()
        {
            if (_game.BestSellerStation != null) TryCancelBestSeller();
        }

        private void TryCancelBestSeller()
        {
            if (Time.unscaledTime >= _bestSellerCancelUntil)
            {
                _bestSellerCancelUntil = Time.unscaledTime + 2f;
                SoundFX.Play(Sfx.Click);
                return;
            }
            _bestSellerCancelUntil = 0;
            if (_game.CancelBestSeller()) SoundFX.Play(Sfx.Click);
        }

        // ================= Hành động =================

        private void SelectTab(Tab tab)
        {
            ActiveTab = tab;
            SoundFX.Play(Sfx.Click);
        }

        private void UpdateEventButton()
        {
            var e = _game.CurrentEvent;
            bool show = e != null || _game.InEventRun;
            _eventRunBtn.bg.gameObject.SetActive(show && ActiveTab == Tab.Restaurant);
            _boardBtn.bg.gameObject.SetActive(e != null && ActiveTab == Tab.Restaurant);
            if (!show) return;
            if (_game.InEventRun) _eventRunBtn.Set($"◄ Về {_game.City.displayName}", true);
            else if (_game.EventRunCompleted) _eventRunBtn.Set("Quán sự kiện: đã xong ✓", false);
            else
            {
                _eventRunBtn.Set($"{e.name}: vào quán sự kiện ►", true);
                float k = 1 + Mathf.Sin(Time.time * 4) * 0.04f;
                _eventRunBtn.bg.rectTransform.localScale = new Vector3(k, k, 1);
            }
        }

        private void OnEventRun()
        {
            _view.ClearSelection();
            _upgradesOpen = false;
            if (_game.InEventRun) _game.LeaveEventRun();
            else _game.EnterEventRun();
            SoundFX.Play(Sfx.LevelUp, 0.5f);
        }

        private void OpenSettings()
        {
            SoundFX.Play(Sfx.Click);
            _settings.Open();
        }

        private void OnBoost()
        {
            AdsService.ShowRewarded(_game.State, AdsService.Boost, () =>
            {
                _game.StartBoost(_game.AdBoostSeconds, _game.AdBoostMultiplier); // theo nâng cấp quảng cáo
                SoundFX.Play(Sfx.Upgrade);
            });
        }

        private void OnLevelUp()
        {
            if (!_game.IsVenueComplete()) return;
            SoundFX.Play(Sfx.LevelUp);
            _game.CompleteVenue();
        }

        private void OnHireWaiter()
        {
            if (_game.TryHireWaiter()) SoundFX.Play(Sfx.Hire);
        }

        private void OnUpgrade(StationData d, int count)
        {
            if (d == null) return;
            if (_game.TryUpgrade(d, count)) SoundFX.Play(Sfx.Upgrade);
        }

        private void OnHireChef()
        {
            if (_game.TryHireChef()) SoundFX.Play(Sfx.Hire);
        }

        private void OnBuyRecipe(int stationIndex)
        {
            var stations = _game.Venue.stations;
            if (stationIndex < stations.Count && _game.TryBuyRecipe(stations[stationIndex])) SoundFX.Play(Sfx.LevelUp, 0.5f);
        }

        private void OnBuyTable()
        {
            if (_game.TryBuyTable()) SoundFX.Play(Sfx.Upgrade);
        }

        private void OnUnlock(StationData d)
        {
            if (d == null) return;
            if (_game.TryUnlock(d)) SoundFX.Play(Sfx.LevelUp, 0.4f);
        }

        private static string Clock(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{s / 60}:{s % 60:00}";
        }
    }
}
