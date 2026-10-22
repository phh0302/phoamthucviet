using System.Collections.Generic;
using PhoAmThucViet.Audio;
using PhoAmThucViet.Core;
using PhoAmThucViet.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace PhoAmThucViet.UI
{
    /// <summary>
    /// Tab Đầu bếp chính (uGUI): tổng hiệu ứng, 4 ô trang bị kèm số mảnh, mua rương, lọc kho,
    /// danh sách món với nút Trang bị / Nâng cấp / Phân rã.
    /// </summary>
    public class ChefPage
    {
        public static readonly Color[] RarityColors =
        {
            new Color(0.55f, 0.55f, 0.55f), // Thường
            new Color(0.25f, 0.55f, 0.95f), // Hiếm
            new Color(0.65f, 0.35f, 0.90f), // Sử thi
            new Color(1.00f, 0.55f, 0.10f), // Huyền thoại
            new Color(0.95f, 0.12f, 0.12f), // Tối thượng (đồ sự kiện): đỏ
        };

        private readonly RestaurantController _game;
        private RectTransform _list;
        private ScrollRect _scroll;
        private readonly Text[] _bonusTexts = new Text[4];

        private class SlotView { public Image bg, frame, icon; public Text title, item, detail, shards; }
        private readonly SlotView[] _slots = new SlotView[4];

        private UIButton _buyChest, _freeGems, _blueprintBtn;

        // Xưởng đồ Tối thượng
        private RectTransform _forge, _forgeList;
        private Text _forgeInfo;
        private bool _forgeOpen;
        private class ForgeRow { public string id; public Image bg, icon; public Text name, info; public UIButton craft; }
        private readonly List<ForgeRow> _forgeRows = new List<ForgeRow>();
        private readonly UIButton[] _filters = new UIButton[5];
        private Text _countText;

        // Phân rã nhanh: chọn độ hiếm (Thường, Hiếm, Sử thi, Huyền thoại, Tối thượng), xem trước, xác nhận
        private UIButton _quickBtn, _qdConfirm, _quickLevelBtn;
        private readonly int[] _quickLevelSlots = new int[4];
        private float _quickLevelShownUntil;
        private string _quickLevelResult = "";
        private RectTransform _qdPanel;
        private Text _qdPreview;
        private readonly UIButton[] _qdToggles = new UIButton[5];
        private readonly bool[] _qdRarity = new bool[5];
        private bool _qdOpen;
        private float _qdConfirmUntil;
        private int _filter = -1; // -1 = tất cả, 0..3 = một vị trí

        private class ItemCard
        {
            public OwnedEquipment owned;
            public EquipmentData item;
            public Image bg, frame, icon;
            public Text name, info, bonus;
            public UIButton equip, levelUp, dismantle;
            public float confirmUntil; // đồ hiếm: bấm Phân rã lần 2 trong 2 giây mới phân rã
        }

        private readonly List<ItemCard> _cards = new List<ItemCard>();
        private int _builtSignature = int.MinValue;

        public ChefPage(RestaurantController game) => _game = game;

        // ================= Dựng =================

        public void Build(RectTransform page)
        {
            // Tổng hiệu ứng
            var bonus = UIKit.Panel("Bonus", page, new Color(1, 1, 1, 0.08f), UIKit.Rounded).rectTransform;
            bonus.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -124), new Vector2(-20, -16));
            for (int i = 0; i < 4; i++)
            {
                _bonusTexts[i] = UIKit.Label("Bonus" + i, bonus, "", 26, Color.white, TextAnchor.MiddleLeft, true);
                float x = i % 2 == 0 ? 0 : 0.5f;
                float y = i < 2 ? 0.5f : 0f;
                _bonusTexts[i].rectTransform.Place(new Vector2(x, y), new Vector2(x + 0.5f, y + 0.5f), new Vector2(24, 0), new Vector2(-8, 0));
            }

            // 4 ô trang bị
            var slots = UIKit.NewRect("Slots", page);
            slots.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -360), new Vector2(-20, -136));
            for (int i = 0; i < 4; i++)
            {
                int slot = i;
                var sv = new SlotView();
                var btn = UIKit.MakeButton("Slot" + i, slots, "", 20, new Color(0.30f, 0.22f, 0.16f), () => SetFilter(slot));
                btn.label.gameObject.SetActive(false);
                sv.bg = btn.bg;
                sv.bg.rectTransform.Place(new Vector2(i * 0.25f, 0), new Vector2((i + 1) * 0.25f, 1), new Vector2(6, 0), new Vector2(-6, 0));
                var t = sv.bg.transform;
                sv.title = UIKit.Label("Title", t, EquipmentData.SlotName((EquipSlot)i).ToUpper(), 22, Palette.Gold, TextAnchor.MiddleCenter, true);
                sv.title.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -38), new Vector2(-8, -4));
                // Icon món đang mặc (ô trống: hình mờ món Thường)
                sv.frame = GearIcons.MakeFrame("Frame", t, out sv.icon);
                sv.frame.rectTransform.anchorMin = sv.frame.rectTransform.anchorMax = new Vector2(0.5f, 1);
                sv.frame.rectTransform.pivot = new Vector2(0.5f, 1);
                sv.frame.rectTransform.anchoredPosition = new Vector2(0, -38);
                sv.frame.rectTransform.sizeDelta = new Vector2(84, 84);
                sv.item = UIKit.Label("Item", t, "", 21, Color.white, TextAnchor.MiddleCenter, true);
                sv.item.rectTransform.Place(new Vector2(0, 0.30f), new Vector2(1, 0.46f), new Vector2(6, 0), new Vector2(-6, 0));
                sv.detail = UIKit.Label("Detail", t, "", 18, new Color(1, 1, 1, 0.85f), TextAnchor.MiddleCenter);
                sv.detail.rectTransform.Place(new Vector2(0, 0.155f), new Vector2(1, 0.31f), new Vector2(6, 0), new Vector2(-6, 0));
                sv.shards = UIKit.Label("Shards", t, "", 19, new Color(0.6f, 0.9f, 1f), TextAnchor.MiddleCenter, true);
                sv.shards.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0.16f), new Vector2(6, 4), new Vector2(-6, 0));
                _slots[i] = sv;
            }

            // Mua rương
            var shop = UIKit.NewRect("Shop", page);
            shop.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -470), new Vector2(-20, -374));
            _buyChest = UIKit.MakeButton("BuyChest", shop, "", 26, Palette.Red, OnBuyChest);
            _buyChest.label.color = Palette.Gold;
            _blueprintBtn = UIKit.MakeButton("Blueprints", shop, "", 24, new Color(0.75f, 0.18f, 0.42f), () => { _forgeOpen = true; SoundFX.Play(Sfx.Click); });
            _blueprintBtn.label.color = Palette.Gold;
            _freeGems = UIKit.MakeButton("FreeGems", shop, "+200 ♦\n(chỉ để thử)", 22, new Color(0.35f, 0.28f, 0.22f),
                () => { _game.DebugAddGems(200); SoundFX.Play(Sfx.Coin); });
            if (DebugTools.Enabled)
            {
                _buyChest.bg.rectTransform.Place(new Vector2(0, 0), new Vector2(0.44f, 1), Vector2.zero, new Vector2(-6, 0));
                _blueprintBtn.bg.rectTransform.Place(new Vector2(0.44f, 0), new Vector2(0.76f, 1), new Vector2(6, 0), new Vector2(-6, 0));
                _freeGems.bg.rectTransform.Place(new Vector2(0.76f, 0), new Vector2(1, 1), new Vector2(6, 0), Vector2.zero);
            }
            else
            {
                // Bản phát hành: bỏ nút +200 ♦
                _freeGems.bg.gameObject.SetActive(false);
                _buyChest.bg.rectTransform.Place(new Vector2(0, 0), new Vector2(0.58f, 1), Vector2.zero, new Vector2(-6, 0));
                _blueprintBtn.bg.rectTransform.Place(new Vector2(0.58f, 0), new Vector2(1, 1), new Vector2(6, 0), Vector2.zero);
            }

            // Lọc kho
            var filters = UIKit.NewRect("Filters", page);
            filters.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -562), new Vector2(-20, -484));
            string[] names = { "Tất cả", "Dao", "Nồi", "Tạp dề", "Mũ" };
            for (int i = 0; i < 5; i++)
            {
                int f = i - 1;
                _filters[i] = UIKit.MakeButton("Filter" + i, filters, names[i], 26, Palette.Orange, () => SetFilter(f));
                _filters[i].bg.rectTransform.Place(new Vector2(i * 0.2f, 0), new Vector2((i + 1) * 0.2f, 1), new Vector2(4, 0), new Vector2(-4, 0));
            }

            _countText = UIKit.Label("Count", page, "", 22, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleLeft);
            _countText.rectTransform.Place(new Vector2(0, 1), new Vector2(0.36f, 1), new Vector2(26, -606), new Vector2(0, -568));
            _quickLevelBtn = UIKit.MakeButton("QuickLevel", page, "Nâng nhanh", 22, Palette.Green, OnQuickLevel, UIKit.Pill);
            _quickLevelBtn.bg.rectTransform.TopRight(330, 568, 300, 40);
            _quickBtn = UIKit.MakeButton("QuickDismantle", page, "Phân rã nhanh", 22, new Color(0.55f, 0.25f, 0.2f), ToggleQuick, UIKit.Pill);
            _quickBtn.bg.rectTransform.TopRight(20, 568, 300, 40);

            // Danh sách kho
            var viewport = UIKit.Panel("Viewport", page, new Color(1, 1, 1, 0.01f)).rectTransform;
            viewport.Place(Vector2.zero, Vector2.one, new Vector2(20, 12), new Vector2(-20, -612));
            viewport.gameObject.AddComponent<RectMask2D>();
            _list = UIKit.NewRect("List", viewport);
            _list.anchorMin = new Vector2(0, 1);
            _list.anchorMax = new Vector2(1, 1);
            _list.pivot = new Vector2(0.5f, 1);
            _list.offsetMin = _list.offsetMax = Vector2.zero;
            var layout = _list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            _list.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _scroll = viewport.gameObject.AddComponent<ScrollRect>();
            _scroll.content = _list;
            _scroll.viewport = viewport;
            _scroll.horizontal = false;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = 40;

            BuildForge(page);
            BuildQuickDismantle(page);
        }

        // ================= Phân rã nhanh =================

        private const string QuickPrefsKey = "quick_dismantle_rarities";

        private void BuildQuickDismantle(RectTransform page)
        {
            // Lần trước chọn gì thì nhớ lại (mặc định chỉ Thường)
            string saved = PlayerPrefs.GetString(QuickPrefsKey, "10000");
            for (int i = 0; i < _qdRarity.Length; i++) _qdRarity[i] = i < saved.Length && saved[i] == '1';

            var bg = UIKit.Panel("QuickDismantle", page, new Color(0.18f, 0.12f, 0.09f, 0.98f), UIKit.Rounded);
            _qdPanel = bg.rectTransform;
            _qdPanel.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -1060), new Vector2(-30, -470));
            bg.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0, -6);

            var title = UIKit.Label("Title", _qdPanel, "Phân rã nhanh", 34, Palette.Gold, TextAnchor.MiddleLeft, true);
            title.rectTransform.TopLeft(26, 16, 500, 54);
            var close = UIKit.MakeButton("Close", _qdPanel, "Đóng", 26, new Color(0.45f, 0.38f, 0.32f), ToggleQuick);
            close.bg.rectTransform.TopRight(18, 14, 160, 60);
            var hint = UIKit.Label("Hint", _qdPanel, "Chọn độ hiếm muốn phân rã. Món đang mặc luôn được giữ lại.", 22,
                new Color(1, 1, 1, 0.75f), TextAnchor.MiddleLeft);
            hint.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(26, -120), new Vector2(-26, -80));

            var row = UIKit.NewRect("Toggles", _qdPanel);
            row.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -230), new Vector2(-20, -132));
            for (int i = 0; i < _qdToggles.Length; i++)
            {
                int r = i;
                _qdToggles[i] = UIKit.MakeButton("R" + i, row, EquipmentData.RarityName((Rarity)i), 22, RarityColors[i], () => ToggleRarity(r));
                _qdToggles[i].bg.rectTransform.Place(new Vector2(i / 5f, 0), new Vector2((i + 1) / 5f, 1), new Vector2(4, 0), new Vector2(-4, 0));
            }

            _qdPreview = UIKit.Label("Preview", _qdPanel, "", 24, Color.white, TextAnchor.UpperLeft);
            _qdPreview.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 1), new Vector2(26, 130), new Vector2(-26, -244));

            _qdConfirm = UIKit.MakeButton("Confirm", _qdPanel, "", 30, Palette.Red, OnQuickConfirm);
            _qdConfirm.bg.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(26, 24), new Vector2(-26, 114));
            _qdPanel.gameObject.SetActive(false);
        }

        private void ToggleQuick()
        {
            _qdOpen = !_qdOpen;
            _qdConfirmUntil = 0;
            if (_qdOpen) _qdPanel.SetAsLastSibling();
            SoundFX.Play(Sfx.Click);
        }

        private void ToggleRarity(int r)
        {
            _qdRarity[r] = !_qdRarity[r];
            _qdConfirmUntil = 0;
            var sb = new System.Text.StringBuilder();
            foreach (bool b in _qdRarity) sb.Append(b ? '1' : '0');
            PlayerPrefs.SetString(QuickPrefsKey, sb.ToString());
            SoundFX.Play(Sfx.Click);
        }

        private void UpdateQuickDismantle()
        {
            _qdPanel.gameObject.SetActive(_qdOpen);
            if (!_qdOpen) return;
            for (int i = 0; i < _qdToggles.Length; i++)
            {
                var t = _qdToggles[i];
                t.Set(EquipmentData.RarityName((Rarity)i) + (_qdRarity[i] ? "\nđã chọn" : "\n—"), true);
                t.bg.color = _qdRarity[i] ? RarityColors[i] : RarityColors[i] * 0.35f + new Color(0, 0, 0, 0.65f);
            }

            var list = _game.QuickDismantleList(_qdRarity);
            var shards = _game.DismantlePreview(list);
            var counts = new int[5];
            foreach (var o in list) { var it = _game.ItemOf(o); if (it != null) counts[(int)it.rarity]++; }
            var sb = new System.Text.StringBuilder();
            sb.Append($"Sẽ phân rã <b>{list.Count}</b> món");
            var parts = new List<string>();
            for (int i = 0; i < counts.Length; i++) if (counts[i] > 0) parts.Add($"{counts[i]} {EquipmentData.RarityName((Rarity)i)}");
            if (parts.Count > 0) sb.Append(": ").Append(string.Join(", ", parts));
            sb.Append("\nNhận về: ");
            var gain = new List<string>();
            for (int s = 0; s < 4; s++) if (shards[s] > 0) gain.Add($"+{shards[s]} {EquipmentCatalog.ShardName((EquipSlot)s)}");
            sb.Append(gain.Count > 0 ? string.Join(" · ", gain) : "không có gì");
            bool risky = _qdRarity[(int)Rarity.SuThi] || _qdRarity[(int)Rarity.HuyenThoai] || _qdRarity[(int)Rarity.ToiThuong];
            if (risky) sb.Append("\n<color=#FFB060>Có chọn đồ Sử thi trở lên: bấm 2 lần để xác nhận.</color>");
            _qdPreview.text = sb.ToString();

            bool confirming = Time.unscaledTime < _qdConfirmUntil;
            if (list.Count == 0) _qdConfirm.Set("Không có món nào để phân rã", false);
            else _qdConfirm.Set(confirming ? $"Bấm lần nữa để phân rã {list.Count} món" : $"Phân rã {list.Count} món", true);
        }

        private void OnQuickConfirm()
        {
            bool risky = _qdRarity[(int)Rarity.SuThi] || _qdRarity[(int)Rarity.HuyenThoai] || _qdRarity[(int)Rarity.ToiThuong];
            if (risky && Time.unscaledTime >= _qdConfirmUntil) { _qdConfirmUntil = Time.unscaledTime + 2.5f; SoundFX.Play(Sfx.Click); return; }
            _qdConfirmUntil = 0;
            int n = _game.QuickDismantle(_qdRarity);
            if (n > 0)
            {
                _builtSignature = int.MinValue;
                SoundFX.Play(Sfx.Upgrade);
            }
        }

        // ================= Xưởng đồ Tối thượng =================

        private void BuildForge(RectTransform page)
        {
            var bg = UIKit.Panel("Forge", page, new Color(0.16f, 0.08f, 0.12f, 0.98f), UIKit.Rounded);
            _forge = bg.rectTransform;
            _forge.Place(Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -12));

            var title = UIKit.Label("Title", _forge, "Xưởng đồ Tối thượng", 36, RarityColors[4], TextAnchor.MiddleLeft, true);
            title.rectTransform.TopLeft(26, 14, 640, 56);
            var close = UIKit.MakeButton("Close", _forge, "Đóng", 28, new Color(0.45f, 0.35f, 0.4f), () => { _forgeOpen = false; SoundFX.Play(Sfx.Click); });
            close.bg.rectTransform.TopRight(18, 12, 170, 64);
            _forgeInfo = UIKit.Label("Info", _forge, "", 22, new Color(1, 1, 1, 0.85f), TextAnchor.UpperLeft);
            _forgeInfo.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(26, -196), new Vector2(-26, -82));

            float listTop = 206;
            if (DebugTools.Enabled)
            {
                var kit = UIKit.MakeButton("Kit", _forge, "Thử: nhận 1 bản vẽ + đủ nguyên liệu", 22, new Color(0.3f, 0.3f, 0.3f, 0.85f),
                    () => { _game.DebugUltimateKit(); _builtSignature = int.MinValue; SoundFX.Play(Sfx.Coin); }, UIKit.Pill);
                kit.bg.rectTransform.TopLeft(26, 200, 520, 56);
                listTop = 266;
            }

            var viewport = UIKit.Panel("Viewport", _forge, new Color(1, 1, 1, 0.01f)).rectTransform;
            viewport.Place(Vector2.zero, Vector2.one, new Vector2(16, 16), new Vector2(-16, -listTop));
            viewport.gameObject.AddComponent<RectMask2D>();
            _forgeList = UIKit.NewRect("List", viewport);
            _forgeList.anchorMin = new Vector2(0, 1);
            _forgeList.anchorMax = new Vector2(1, 1);
            _forgeList.pivot = new Vector2(0.5f, 1);
            _forgeList.offsetMin = _forgeList.offsetMax = Vector2.zero;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = _forgeList;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40;

            float y = 0;
            foreach (var item in UltimateItems.All)
            {
                var r = new ForgeRow { id = item.id };
                r.bg = UIKit.Panel("Row", _forgeList, new Color(0.3f, 0.16f, 0.22f), UIKit.Rounded);
                r.bg.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -(y + 190)), new Vector2(0, -y));
                var frame = GearIcons.MakeFrame("Frame", r.bg.transform, out r.icon);
                frame.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, -75), new Vector2(164, 75));
                frame.color = GearIcons.FrameColor(Rarity.ToiThuong);
                GearIcons.Apply(r.icon, GearIcons.For(item), Color.white);
                r.name = UIKit.Label("Name", r.bg.transform, item.displayName, 28, RarityColors[4], TextAnchor.MiddleLeft, true);
                r.name.rectTransform.Place(new Vector2(0, 1), new Vector2(0.66f, 1), new Vector2(178, -50), new Vector2(0, -6));
                r.info = UIKit.Label("Info", r.bg.transform, "", 19, new Color(1, 1, 1, 0.85f), TextAnchor.UpperLeft);
                r.info.rectTransform.Place(new Vector2(0, 0), new Vector2(0.66f, 1), new Vector2(178, 6), new Vector2(0, -52));
                string id = item.id;
                r.craft = UIKit.MakeButton("Craft", r.bg.transform, "", 24, Palette.Green, () => OnCraft(id));
                r.craft.bg.rectTransform.Place(new Vector2(0.66f, 0), new Vector2(1, 1), new Vector2(8, 14), new Vector2(-14, -14));
                _forgeRows.Add(r);
                y += 202;
            }
            _forgeList.sizeDelta = new Vector2(0, y);
            _forge.gameObject.SetActive(false);
        }

        private void UpdateForge(GameState state, EquipmentCatalog catalog)
        {
            int epic = UltimateItems.Available(state, catalog, Rarity.SuThi);
            int rare = UltimateItems.Available(state, catalog, Rarity.Hiem);
            string Need(int have, int need) => have >= need ? $"<color=#7CFC7C>{have}/{need}</color>" : $"<color=#FF8080>{have}/{need}</color>";
            _forgeInfo.text =
                "Bản vẽ chỉ có trong <b>rương sự kiện</b> (mốc 1200 vật phẩm và quà hoàn thành quán sự kiện), mỗi rương 1 bản vẽ của sự kiện đó.\n" +
                $"Ghép = bản vẽ + <b>2 món Huyền thoại chỉ định</b> (ghi ở từng món) + {UltimateItems.NeedEpic} Sử thi + {UltimateItems.NeedRare} Hiếm (đồ không đang mặc).\n" +
                $"Đang có: Sử thi {Need(epic, UltimateItems.NeedEpic)} · Hiếm {Need(rare, UltimateItems.NeedRare)} · đủ 4 món Tối thượng cùng set = 100% món hoàn hảo, nấu nhanh +500%, chạy nhanh +300%";

            foreach (var r in _forgeRows)
            {
                var item = UltimateItems.Find(r.id);
                string ev = UltimateItems.EventName(UltimateItems.EventOf(r.id));
                string stats = $"{ev} · {EquipmentData.SlotName(item.slot)} · lợi nhuận +{Pct(item.BonusAt(1))} → +{Pct(item.BonusAt(item.MaxLevel))}\n" +
                               GearBook.Describe(item, 1, withProfit: false);
                if (UltimateItems.Recipes.TryGetValue(r.id, out var need))
                {
                    var parts = new List<string>();
                    foreach (var lid in need)
                    {
                        var li = catalog.Find(lid);
                        string nm = li != null ? li.displayName : lid;
                        parts.Add(UltimateItems.HasLegendary(state, lid) ? $"<color=#7CFC7C>{nm} (có)</color>" : $"<color=#FF9090>{nm}</color>");
                    }
                    stats += "\nCần: " + string.Join(" + ", parts);
                }
                bool owned = state.equipment.Exists(o => o.id == r.id);
                bool hasBp = state.blueprints.Contains(r.id);
                r.info.text = stats;
                if (owned)
                {
                    r.bg.color = new Color(0.22f, 0.32f, 0.22f);
                    r.craft.Set("Đã sở hữu", false);
                }
                else if (hasBp)
                {
                    bool can = UltimateItems.CanCraft(state, catalog, r.id);
                    r.bg.color = Color.Lerp(new Color(0.45f, 0.15f, 0.3f), new Color(0.6f, 0.2f, 0.4f), can ? Mathf.Abs(Mathf.Sin(Time.time * 3)) : 0);
                    r.craft.Set(can ? "Ghép!" : "Có bản vẽ\nthiếu nguyên liệu", can);
                }
                else
                {
                    r.bg.color = new Color(0.2f, 0.14f, 0.17f);
                    r.craft.Set($"Chưa có bản vẽ\n(rương {ev})", false);
                }
            }
        }

        private void OnCraft(string id)
        {
            if (!_game.CraftUltimate(id)) return;
            _builtSignature = int.MinValue;
            SoundFX.Play(Sfx.LevelUp);
        }

        private void SetFilter(int f)
        {
            _filter = _filter == f && f >= 0 ? -1 : f; // bấm lại ô đang lọc thì bỏ lọc
            _builtSignature = int.MinValue;
            SoundFX.Play(Sfx.Click);
        }

        // ================= Cập nhật =================

        public void Update()
        {
            var state = _game.State;
            var catalog = _game.equipmentCatalog;
            if (catalog == null) return;
            state.EnsureEquippedSlots();

            var b = _game.Bonuses;
            _bonusTexts[0].text = $"Lợi nhuận +{Pct(b.ProfitMultiplier - 1)} · Giảm giá {Pct(1 - b.CostMultiplier)}";
            _bonusTexts[1].text = $"Món hoàn hảo x6: mọi đầu bếp {Pct(b.PerfectChanceAll)} · chính {Pct(b.PerfectChanceHead)}";
            double cookAll = 1 / b.CookTimeMultiplier - 1, cookHead = 1 / (b.CookTimeMultiplier * b.HeadCookFactor) - 1;
            double runAll = 1 / b.TripTimeMultiplier - 1, runHead = 1 / (b.TripTimeMultiplier * b.HeadTripFactor) - 1;
            _bonusTexts[2].text = $"Nấu nhanh +{Pct(cookAll)} · chính +{Pct(cookHead)}";
            _bonusTexts[3].text = $"Chạy nhanh +{Pct(runAll)} · chính +{Pct(runHead)}";

            for (int i = 0; i < 4; i++)
            {
                var sv = _slots[i];
                var owned = state.GetEquipment(state.equipped[i]);
                var item = _game.ItemOf(owned);
                if (item == null)
                {
                    sv.item.text = "Trống";
                    sv.item.color = new Color(1, 1, 1, 0.5f);
                    sv.detail.text = EquipmentData.EffectName((EquipSlot)i);
                    sv.frame.color = new Color(1, 1, 1, 0.12f);
                    GearIcons.Apply(sv.icon, GearIcons.ForEmptySlot((EquipSlot)i), new Color(0, 0, 0, 0.35f));
                }
                else
                {
                    sv.item.text = item.displayName;
                    sv.item.color = Color.Lerp(RarityColors[(int)item.rarity], Color.white, 0.25f);
                    sv.detail.text = $"Cấp {owned.level} · lợi nhuận +{Pct(item.BonusAt(owned.level))}";
                    sv.frame.color = GearIcons.FrameColor(item.rarity);
                    GearIcons.Apply(sv.icon, GearIcons.For(item), Color.white);
                }
                sv.shards.text = $"{state.shards[i]} mảnh";
                sv.bg.color = _filter == i ? new Color(0.55f, 0.38f, 0.18f) : new Color(0.30f, 0.22f, 0.16f);
            }

            var small = ChestTypes.Small;
            _buyChest.Set($"Mua {small.name.ToLower()} {small.items} món · {small.price} ♦", state.gems >= small.price);

            // Xưởng đồ Tối thượng
            bool canCraft = false;
            foreach (var id in state.blueprints) if (UltimateItems.CanCraft(state, catalog, id)) { canCraft = true; break; }
            _blueprintBtn.Set(canCraft ? $"Bản vẽ ({state.blueprints.Count})\nghép được!" : $"Đồ Tối thượng\nbản vẽ: {state.blueprints.Count}", true);
            _forge.gameObject.SetActive(_forgeOpen);
            if (_forgeOpen) UpdateForge(state, catalog);
            UpdateQuickDismantle();
            UpdateQuickLevel();
            for (int i = 0; i < 5; i++)
                _filters[i].bg.color = _filter == i - 1 ? Palette.Orange : new Color(0.35f, 0.28f, 0.22f);

            int signature = Signature(state);
            if (signature != _builtSignature) RebuildList(state, catalog, signature);
            UpdateCards(state, catalog);
        }

        /// <summary>Thay đổi khi kho thêm/bớt món hoặc đổi bộ lọc, để biết lúc nào phải dựng lại danh sách.</summary>
        private int Signature(GameState state)
        {
            unchecked
            {
                int h = 17 * 31 + _filter;
                foreach (var e in state.equipment) h = h * 31 + e.uid.GetHashCode();
                return h;
            }
        }

        private void RebuildList(GameState state, EquipmentCatalog catalog, int signature)
        {
            foreach (Transform child in _list) Object.Destroy(child.gameObject);
            _cards.Clear();
            _builtSignature = signature;

            // Hiếm trước, cùng độ hiếm thì cấp cao trước
            var items = new List<(OwnedEquipment owned, EquipmentData item)>();
            foreach (var owned in state.equipment)
            {
                var item = catalog.Find(owned.id);
                if (item == null || (_filter >= 0 && (int)item.slot != _filter)) continue;
                items.Add((owned, item));
            }
            items.Sort((a, c) => a.item.rarity != c.item.rarity ? c.item.rarity.CompareTo(a.item.rarity) : c.owned.level.CompareTo(a.owned.level));

            _countText.text = state.equipment.Count == 0
                ? "Kho trống: mở rương để nhận đồ."
                : $"Kho: {items.Count}/{state.equipment.Count} món";

            foreach (var (owned, item) in items)
            {
                var c = new ItemCard { owned = owned, item = item };
                c.bg = UIKit.Panel("Item " + item.displayName, _list, Palette.Card, UIKit.Rounded);
                c.bg.gameObject.AddComponent<LayoutElement>().preferredHeight = 172;
                var t = c.bg.transform;

                // Icon trong khung màu độ hiếm
                c.frame = GearIcons.MakeFrame("Frame", t, out c.icon);
                c.frame.rectTransform.TopLeft(12, 14, 130, 144);
                c.frame.color = GearIcons.FrameColor(item.rarity);
                GearIcons.Apply(c.icon, GearIcons.For(item), Color.white);
                c.name = UIKit.Label("Name", t, item.displayName, 30, RarityColors[(int)item.rarity] * 0.8f + new Color(0, 0, 0, 0.2f), TextAnchor.MiddleLeft, true);
                c.name.rectTransform.TopLeft(154, 10, 320, 46);
                c.info = UIKit.Label("Info", t, "", 22, Palette.TextSoft);
                c.info.rectTransform.TopLeft(154, 56, 320, 36);
                c.bonus = UIKit.Label("Bonus", t, "", 20, Palette.TextDark, TextAnchor.UpperLeft);
                c.bonus.rectTransform.TopLeft(154, 92, 320, 74);

                var card = c;
                c.equip = UIKit.MakeButton("Equip", t, "", 24, Palette.Blue, () => OnEquip(card));
                c.equip.bg.rectTransform.TopRight(392, 14, 160, 122);
                c.levelUp = UIKit.MakeButton("LevelUp", t, "", 23, Palette.Green, () => OnLevelUp(card));
                c.levelUp.bg.rectTransform.TopRight(200, 14, 180, 122);
                c.dismantle = UIKit.MakeButton("Dismantle", t, "", 23, Palette.Red, () => OnDismantle(card));
                c.dismantle.bg.rectTransform.TopRight(14, 14, 174, 122);
                _cards.Add(c);
            }
        }

        private void UpdateCards(GameState state, EquipmentCatalog catalog)
        {
            foreach (var c in _cards)
            {
                bool equipped = EquipmentService.IsEquipped(state, c.owned);
                int level = c.owned.level;
                c.bg.color = equipped ? Palette.CardSelected : Palette.Card;
                c.info.text = $"{EquipmentData.SlotName(c.item.slot)} · {EquipmentData.RarityName(c.item.rarity)} · Cấp {level}/{c.item.MaxLevel}";
                string next = level < c.item.MaxLevel ? $" (cấp sau +{Pct(c.item.BonusAt(level + 1))})" : "";
                string effects = GearBook.Describe(c.item, level, withProfit: false);
                c.bonus.text = $"<b>{GearBook.Scope(c.item)}</b> · Lợi nhuận +{Pct(c.item.BonusAt(level))}{next}" +
                               (effects.Length > 0 ? $"\n{effects}" : "");

                c.equip.Set(equipped ? "Đang\ndùng" : "Trang\nbị", !equipped);

                if (level >= c.item.MaxLevel) c.levelUp.Set("Cấp\ntối đa", false);
                else
                {
                    int cost = EquipmentService.LevelUpCost(catalog, c.item, level);
                    bool can = EquipmentService.CanLevelUp(state, catalog, c.owned, out _);
                    c.levelUp.Set($"Nâng cấp\n{cost} mảnh", can);
                }

                int value = EquipmentService.DismantleValue(catalog, c.owned, c.item);
                bool confirming = Time.unscaledTime < c.confirmUntil;
                c.dismantle.Set(equipped ? "Đang\ndùng" : confirming ? "Chắc\nchưa?" : $"Phân rã\n+{value} mảnh", !equipped);
            }
        }

        // ================= Hành động =================

        private void OnEquip(ItemCard c)
        {
            _game.Equip(c.owned);
            SoundFX.Play(Sfx.Hire);
        }

        /// <summary>Nút Nâng nhanh: hiện số cấp sẽ nâng được cho đồ đang mặc; vừa bấm thì hiện kết quả 2 giây.</summary>
        private void UpdateQuickLevel()
        {
            if (Time.unscaledTime < _quickLevelShownUntil)
            {
                _quickLevelBtn.Set(_quickLevelResult, false);
                return;
            }
            int n = _game.QuickLevelUp(false);
            _quickLevelBtn.Set(n > 0 ? $"Nâng nhanh (+{n} cấp)" : "Nâng nhanh: thiếu mảnh", n > 0);
        }

        private void OnQuickLevel()
        {
            int n = _game.QuickLevelUp(true, _quickLevelSlots);
            if (n <= 0) return;
            SoundFX.Play(Sfx.Upgrade);
            var parts = new List<string>();
            string[] shortNames = { "Dao", "Nồi", "Tạp dề", "Mũ" };
            for (int i = 0; i < 4; i++) if (_quickLevelSlots[i] > 0) parts.Add($"{shortNames[i]} +{_quickLevelSlots[i]}");
            _quickLevelResult = string.Join(" · ", parts);
            _quickLevelShownUntil = Time.unscaledTime + 2f;
        }

        private void OnLevelUp(ItemCard c)
        {
            if (_game.LevelUpEquipment(c.owned)) SoundFX.Play(Sfx.Upgrade);
        }

        private void OnDismantle(ItemCard c)
        {
            // Đồ Sử thi và Huyền thoại phải bấm 2 lần để tránh phân rã nhầm
            if (c.item.rarity >= Rarity.SuThi && Time.unscaledTime >= c.confirmUntil)
            {
                c.confirmUntil = Time.unscaledTime + 2f;
                SoundFX.Play(Sfx.Click);
                return;
            }
            if (_game.Dismantle(c.owned)) SoundFX.Play(Sfx.Coin);
        }

        private void OnBuyChest()
        {
            if (_game.TryBuyChest(ChestTypes.Small)) SoundFX.Play(Sfx.Chest);
        }

        private static string Pct(double v) => $"{v * 100:0.#}%";
    }
}
