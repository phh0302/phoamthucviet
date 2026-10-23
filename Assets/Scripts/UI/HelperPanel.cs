using System.Collections.Generic;
using PhoAmThucViet.Audio;
using PhoAmThucViet.Core;
using PhoAmThucViet.Gameplay;
using PhoAmThucViet.Monetization;
using PhoAmThucViet.Online;
using UnityEngine;
using UnityEngine.UI;

namespace PhoAmThucViet.UI
{
    /// <summary>
    /// Bảng "Mượn đầu bếp": danh sách 4 đầu bếp (người chơi thật trên mạng trước, thiếu thì đầu bếp có sẵn),
    /// mỗi người hiện 4 món đang mặc + tóm tắt hiệu ứng, nút Mượn 5 phút. Khi đang có người giúp: đếm ngược
    /// và nút xem quảng cáo +3 phút.
    /// </summary>
    public class HelperPanel
    {
        private class Row
        {
            public Image bg;
            public Image[] frames = new Image[4], icons = new Image[4];
            public Text name, tag, summary;
            public UIButton borrow;
            public HelperCard card;
        }

        private readonly RestaurantController _game;
        private RectTransform _panel;
        private Text _info, _activeName, _activeInfo;
        private RectTransform _activeBox;
        private UIButton _extend, _refresh;
        private readonly Image[] _activeFrames = new Image[4], _activeIcons = new Image[4];
        private readonly List<Row> _rows = new List<Row>();
        private List<HelperCard> _choices;
        private bool _loading;

        public bool IsOpen => _panel != null && _panel.gameObject.activeSelf;

        public HelperPanel(RestaurantController game) => _game = game;

        public void Build(RectTransform layer, float bottom, float top)
        {
            var bg = UIKit.Panel("HelperPanel", layer, Palette.PanelBg, UIKit.Rounded);
            _panel = bg.rectTransform;
            _panel.Place(Vector2.zero, Vector2.one, new Vector2(20, bottom), new Vector2(-20, -top));
            bg.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0, -6);

            var title = UIKit.Label("Title", _panel, "Mượn đầu bếp", 36, Palette.Gold, TextAnchor.MiddleLeft, true);
            title.rectTransform.TopLeft(28, 16, 600, 56);
            var close = UIKit.MakeButton("Close", _panel, "Đóng", 28, new Color(0.55f, 0.45f, 0.38f), Close);
            close.bg.rectTransform.TopRight(20, 14, 170, 64);
            _info = UIKit.Label("Info", _panel, "", 22, new Color(1, 1, 1, 0.8f), TextAnchor.UpperLeft);
            _info.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -150), new Vector2(-28, -84));

            // Ô người đang giúp
            var ab = UIKit.Panel("Active", _panel, new Color(0.18f, 0.40f, 0.30f), UIKit.Rounded);
            _activeBox = ab.rectTransform;
            _activeBox.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -380), new Vector2(-20, -160));
            _activeName = UIKit.Label("Name", _activeBox, "", 30, Color.white, TextAnchor.MiddleLeft, true);
            _activeName.rectTransform.TopLeft(20, 12, 640, 46);
            _activeInfo = UIKit.Label("Info", _activeBox, "", 21, new Color(1, 1, 1, 0.9f), TextAnchor.UpperLeft);
            _activeInfo.rectTransform.TopLeft(20, 58, 640, 70);
            for (int i = 0; i < 4; i++)
            {
                _activeFrames[i] = GearIcons.MakeFrame("Gear" + i, _activeBox, out _activeIcons[i]);
                _activeFrames[i].rectTransform.TopLeft(20 + i * 86, 132, 78, 78);
            }
            _extend = UIKit.MakeButton("Extend", _activeBox, "", 26, Palette.Blue, OnExtend);
            _extend.bg.rectTransform.TopRight(16, 30, 290, 160);

            // Danh sách để chọn
            for (int i = 0; i < 4; i++) _rows.Add(MakeRow(i));
            _refresh = UIKit.MakeButton("Refresh", _panel, "Tìm đầu bếp khác", 26, Palette.Orange, () => { SoundFX.Play(Sfx.Click); Refresh(); }, UIKit.Pill);
            _refresh.bg.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-220, 18), new Vector2(220, 92));
            _panel.gameObject.SetActive(false);
        }

        private Row MakeRow(int i)
        {
            var r = new Row();
            r.bg = UIKit.Panel("Row" + i, _panel, Palette.Card, UIKit.Rounded);
            r.bg.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -(160 + (i + 1) * 196)), new Vector2(-20, -(160 + i * 196 + 8)));
            var t = r.bg.transform;
            r.name = UIKit.Label("Name", t, "", 28, Palette.TextDark, TextAnchor.MiddleLeft, true);
            r.name.rectTransform.TopLeft(20, 8, 470, 42);
            r.tag = UIKit.Label("Tag", t, "", 20, new Color(0.15f, 0.55f, 0.30f), TextAnchor.MiddleLeft, true);
            r.tag.rectTransform.TopLeft(490, 10, 220, 40);
            r.summary = UIKit.Label("Summary", t, "", 20, Palette.TextSoft, TextAnchor.UpperLeft);
            r.summary.rectTransform.TopLeft(20, 50, 690, 48);
            for (int k = 0; k < 4; k++)
            {
                r.frames[k] = GearIcons.MakeFrame("Gear" + k, t, out r.icons[k]);
                r.frames[k].rectTransform.TopLeft(20 + k * 84, 100, 76, 76);
            }
            var row = r;
            r.borrow = UIKit.MakeButton("Borrow", t, "", 26, Palette.Green, () => OnBorrow(row));
            r.borrow.bg.rectTransform.TopRight(14, 24, 250, 132);
            return r;
        }

        // ================= Mở / đóng =================

        public void Open()
        {
            _panel.gameObject.SetActive(true);
            _panel.SetAsLastSibling();
            SoundFX.Play(Sfx.Click);
            if (_choices == null) Refresh();
        }

        public void Close()
        {
            _panel.gameObject.SetActive(false);
            SoundFX.Play(Sfx.Click);
        }

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        private async void Refresh()
        {
            if (_loading) return;
            _loading = true;
            var online = await HelperDirectory.Fetch(4);
            _choices = _game.MakeHelperChoices(online, 4);
            _loading = false;
        }

        private void OnBorrow(Row r)
        {
            if (r.card == null || !_game.BorrowHelper(r.card)) return;
            SoundFX.Play(Sfx.Hire);
            _choices = null; // lần mượn sau tìm danh sách mới
        }

        private void OnExtend()
        {
            _game.ExtendHelperWithAd(() => SoundFX.Play(Sfx.LevelUp));
        }

        // ================= Cập nhật =================

        private static void ShowGear(HelperCard card, Image[] frames, Image[] icons, EquipmentCatalog catalog)
        {
            for (int k = 0; k < 4; k++)
            {
                var item = catalog != null ? catalog.Find(card?.ItemAt(k)) : null;
                frames[k].color = item != null ? GearIcons.FrameColor(item.rarity) : new Color(1, 1, 1, 0.15f);
                GearIcons.Apply(icons[k], item != null ? GearIcons.For(item) : GearIcons.ForEmptySlot((EquipSlot)k),
                    item != null ? Color.white : new Color(0, 0, 0, 0.3f));
            }
        }

        private static string Clock(float s)
        {
            int v = Mathf.Max(0, Mathf.CeilToInt(s));
            return $"{v / 60}:{v % 60:00}";
        }

        public void Update()
        {
            if (!IsOpen) return;
            var catalog = _game.equipmentCatalog;
            bool active = _game.HelperActive;
            int left = _game.HelperBorrowsLeft;
            float cooldown = _game.HelperCooldownLeft;
            _info.text = (HelperDirectory.Online ? "" : "<color=#FFB74D>Chưa kết nối mạng: chỉ có đầu bếp có sẵn trong game.</color>\n") +
                         $"Đầu bếp mượn vào phụ nấu {RestaurantController.HelperSeconds / 60} phút, trang bị của họ cộng thêm vào của bạn. " +
                         $"Còn {left}/{RestaurantController.HelperDailyLimit} lượt hôm nay, mượn xong chờ {RestaurantController.HelperCooldownSeconds / 60} phút mới mượn tiếp." +
                         (cooldown > 0 ? $"\n<color=#FFD54F>Mượn tiếp sau {Clock(cooldown)}</color>" : "");

            _activeBox.gameObject.SetActive(active);
            if (active)
            {
                var h = _game.ActiveHelper;
                _activeName.text = $"{h.name} đang giúp · còn {Clock(_game.HelperSecondsLeft)}";
                _activeInfo.text = "Cộng thêm: " + HelperChefs.Summary(_game.HelperBonuses);
                ShowGear(h, _activeFrames, _activeIcons, catalog);
                if (_game.HelperCanExtend)
                    _extend.Set($"Xem QC\n+{RestaurantController.HelperExtendSeconds / 60} phút", !AdsService.Busy);
                else _extend.Set("Đã kéo dài", false);
            }

            float listTop = active ? 400 : 160;
            for (int i = 0; i < _rows.Count; i++)
            {
                var r = _rows[i];
                var card = _choices != null && i < _choices.Count ? _choices[i] : null;
                bool show = card != null && !_loading;
                r.bg.gameObject.SetActive(show);
                if (!show) continue;
                r.bg.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -(listTop + (i + 1) * 196)), new Vector2(-20, -(listTop + i * 196 + 8)));
                r.card = card;
                r.name.text = card.name;
                r.tag.text = card.isPlayer ? "★ Người chơi" : "";
                var b = HelperChefs.Compute(card, catalog);
                r.summary.text = HelperChefs.Summary(b);
                ShowGear(card, r.frames, r.icons, catalog);
                if (active) r.borrow.Set("Đang có\nngười giúp", false);
                else if (left <= 0) r.borrow.Set("Hết lượt\nhôm nay", false);
                else if (cooldown > 0) r.borrow.Set($"Chờ\n{Clock(cooldown)}", false);
                else r.borrow.Set($"Mượn\n{RestaurantController.HelperSeconds / 60} phút", true);
            }
            _refresh.Set(_loading ? "Đang tìm đầu bếp…" : "Tìm đầu bếp khác", !_loading);
        }
    }
}
