using PhoAmThucViet.Audio;
using PhoAmThucViet.Core;
using PhoAmThucViet.Gameplay;
using PhoAmThucViet.Monetization;
using UnityEngine;
using UnityEngine.UI;

namespace PhoAmThucViet.UI
{
    /// <summary>
    /// Popup uGUI: "Chào mừng trở lại" (tiền offline) và mở rương. Nền tối phủ màn hình, khung bật ra.
    /// Ưu tiên: tiền offline trước, rương sau.
    /// </summary>
    public class PopupLayer
    {
        private readonly RestaurantController _game;
        private RectTransform _root, _box;
        private Text _title, _body;
        private UIButton _primary, _secondary;
        private readonly Text[] _rewardRows = new Text[6];
        private readonly int[] _rowRarity = new int[6];
        private readonly Image[] _rowFrames = new Image[6], _rowIcons = new Image[6];
        private int _revealed;           // số món rương đã lật
        private Image _glow;             // quầng sáng sau khung, màu theo món hiếm nhất đã lật
        private const float RevealStart = 0.35f, RevealGap = 0.4f;
        private int _shownRewards;
        private enum Kind { None, Offline, Reward }
        private Kind _showing = Kind.None;
        private float _openTime;

        public PopupLayer(RestaurantController game) => _game = game;

        public bool IsOpen => _showing != Kind.None;

        public void Build(Transform canvas)
        {
            var dim = UIKit.Panel("Popup", canvas, new Color(0, 0, 0, 0.65f));
            _root = dim.rectTransform;
            _root.Place(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _glow = UIKit.Panel("Glow", _root, new Color(1, 1, 1, 0), UIKit.Rounded);
            _glow.rectTransform.anchorMin = _glow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _glow.rectTransform.sizeDelta = new Vector2(980, 900);
            _glow.raycastTarget = false;

            _box = UIKit.Panel("Box", _root, Palette.Card, UIKit.Rounded).rectTransform;
            _box.anchorMin = _box.anchorMax = new Vector2(0.5f, 0.5f);
            _box.sizeDelta = new Vector2(900, 820);

            var header = UIKit.Panel("Header", _box, Palette.Red, UIKit.Rounded).rectTransform;
            header.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -130), Vector2.zero);
            _title = UIKit.Label("Title", header, "", 42, Palette.Gold, TextAnchor.MiddleCenter, true);
            _title.rectTransform.Place(Vector2.zero, Vector2.one, new Vector2(24, 0), new Vector2(-24, 0));

            _body = UIKit.Label("Body", _box, "", 32, Palette.TextDark, TextAnchor.UpperCenter);
            _body.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -330), new Vector2(-40, -160));

            for (int i = 0; i < _rewardRows.Length; i++)
            {
                _rewardRows[i] = UIKit.Label("Row" + i, _box, "", 30, Palette.TextDark, TextAnchor.MiddleLeft, true);
                _rewardRows[i].rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1),
                    new Vector2(110, -220 - i * 68), new Vector2(-40, -160 - i * 68));
                // Icon món ở đầu dòng (con của dòng chữ nên cùng hiện / phóng to theo hiệu ứng lật)
                _rowFrames[i] = GearIcons.MakeFrame("Frame", _rewardRows[i].transform, out _rowIcons[i]);
                _rowFrames[i].rectTransform.anchorMin = _rowFrames[i].rectTransform.anchorMax = new Vector2(0, 0.5f);
                _rowFrames[i].rectTransform.pivot = new Vector2(1, 0.5f);
                _rowFrames[i].rectTransform.anchoredPosition = new Vector2(-8, 0);
                _rowFrames[i].rectTransform.sizeDelta = new Vector2(62, 62);
            }

            _primary = UIKit.MakeButton("Primary", _box, "", 34, Palette.Green, OnPrimary);
            _primary.bg.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 150), new Vector2(-40, 260));
            _secondary = UIKit.MakeButton("Secondary", _box, "", 30, Palette.Blue, OnSecondary);
            _secondary.bg.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 30), new Vector2(-40, 130));

            _root.gameObject.SetActive(false);
        }

        public void Update()
        {
            Kind want = !_game.PendingOffline.IsZero ? Kind.Offline
                      : _game.LastRewards.Count > 0 ? Kind.Reward
                      : Kind.None;
            // Nhận thêm rương khi popup rương đang mở (gộp nhiều mốc quà): hiện lại đủ món
            if (want != _showing || (want == Kind.Reward && _game.LastRewards.Count != _shownRewards)) Show(want);
            if (_showing == Kind.None) return;

            // Khung bật ra: 0,7 → 1,05 → 1
            float t = Time.unscaledTime - _openTime;
            float s = t < 0.15f ? Mathf.Lerp(0.7f, 1.05f, t / 0.15f) : Mathf.Lerp(1.05f, 1f, Mathf.Min(1, (t - 0.15f) / 0.1f));
            _box.localScale = new Vector3(s, s, 1);
            if (_showing == Kind.Reward) RevealRewards(t);
            else _glow.color = new Color(1, 1, 1, 0);
        }

        /// <summary>
        /// Lật từng món rương: mỗi món bật ra cách nhau 0,4 giây, chữ phóng to rồi về cỡ thường.
        /// Món Sử thi / Huyền thoại có tiếng riêng và làm quầng sáng sau khung đổi màu, nhấp nháy.
        /// </summary>
        private void RevealRewards(float t)
        {
            int best = -1;
            for (int i = 0; i < _rewardRows.Length; i++)
            {
                var row = _rewardRows[i];
                if (string.IsNullOrEmpty(row.text)) { row.gameObject.SetActive(false); continue; }
                float at = RevealStart + i * RevealGap;
                bool shown = t >= at;
                row.gameObject.SetActive(shown);
                if (!shown) continue;
                if (i >= _revealed)
                {
                    _revealed = i + 1;
                    SoundFX.Play(_rowRarity[i] >= (int)Rarity.SuThi ? Sfx.LevelUp : Sfx.Coin, _rowRarity[i] >= (int)Rarity.SuThi ? 0.7f : 0.4f);
                }
                float k = Mathf.Clamp01((t - at) / 0.2f);
                float scale = k < 1 ? Mathf.Lerp(1.6f, 1f, k) : 1f;
                row.rectTransform.localScale = new Vector3(scale, scale, 1);
                best = Mathf.Max(best, _rowRarity[i]);
            }
            if (best >= (int)Rarity.SuThi)
            {
                var c = ChefPage.RarityColors[best];
                float pulse = 0.35f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3)) * 0.35f;
                _glow.color = new Color(c.r, c.g, c.b, pulse);
                float g = 1 + Mathf.Sin(Time.unscaledTime * 2) * 0.03f;
                _glow.rectTransform.localScale = new Vector3(g, g, 1);
            }
            else _glow.color = new Color(1, 1, 1, 0);
        }

        private void Show(Kind kind)
        {
            _showing = kind;
            _root.gameObject.SetActive(kind != Kind.None);
            _root.SetAsLastSibling();
            _openTime = Time.unscaledTime;
            foreach (var row in _rewardRows) { row.text = ""; row.gameObject.SetActive(true); row.rectTransform.localScale = Vector3.one; }
            foreach (var f in _rowFrames) f.gameObject.SetActive(false);
            _revealed = 0;
            _shownRewards = _game.LastRewards.Count;
            _body.text = "";

            if (kind == Kind.Offline)
            {
                _title.text = "Chào mừng trở lại!";
                _body.text = $"Các quản lý đã kiếm được\n<b>{_game.PendingOffline}</b>\ntrong {Clock(_game.OfflineSeconds)} bạn vắng mặt.";
                string mx = $"x{_game.AdOfflineMultiplier:0.#}";
                _primary.Set(_game.State.noAds ? $"Nhận {mx}" : $"Xem QC: nhận {mx}", true);
                _secondary.Set("Nhận", true);
                _secondary.bg.gameObject.SetActive(true);
                SoundFX.Play(Sfx.Coin);
            }
            else if (kind == Kind.Reward)
            {
                _title.text = _game.LastRewardTitle;
                var catalog = _game.equipmentCatalog;
                int i = 0;
                foreach (var owned in _game.LastRewards)
                {
                    if (i >= _rewardRows.Length) break;
                    var item = catalog != null ? catalog.Find(owned.id) : null;
                    if (item == null) continue;
                    _rewardRows[i].text = $"{EquipmentData.RarityName(item.rarity)}: {item.displayName} · {EquipmentData.SlotName(item.slot)} · {GearBook.Describe(item, 1)}";
                    _rewardRows[i].color = ChefPage.RarityColors[(int)item.rarity] * 0.85f + new Color(0, 0, 0, 0.15f);
                    _rowRarity[i] = (int)item.rarity;
                    SetRowIcon(i, item, item.rarity);
                    i++;
                }
                // Bản vẽ đồ Tối thượng từ rương sự kiện
                if (_game.LastBlueprint != null && i < _rewardRows.Length)
                {
                    var bp = UltimateItems.Find(_game.LastBlueprint);
                    if (bp != null)
                    {
                        _rewardRows[i].text = $"Bản vẽ Tối thượng: {bp.displayName} · {EquipmentData.SlotName(bp.slot)} (ghép ở tab Đầu bếp)";
                        _rewardRows[i].color = ChefPage.RarityColors[(int)Rarity.ToiThuong] * 0.85f + new Color(0, 0, 0, 0.15f);
                        _rowRarity[i] = (int)Rarity.ToiThuong;
                        SetRowIcon(i, bp, Rarity.ToiThuong);
                    }
                }
                _primary.Set("Tuyệt!", true);
                _secondary.bg.gameObject.SetActive(false);
                SoundFX.Play(Sfx.Chest);
                RevealRewards(0);
            }
        }

        private void OnPrimary()
        {
            if (_showing == Kind.Offline)
                AdsService.ShowRewarded(_game.State, AdsService.OfflineDouble, () => { _game.ClaimOffline(true); SoundFX.Play(Sfx.Coin); });
            else if (_showing == Kind.Reward) _game.ClearRewards();
        }

        private void OnSecondary()
        {
            if (_showing == Kind.Offline) { _game.ClaimOffline(false); SoundFX.Play(Sfx.Coin); }
        }

        private static string Clock(double seconds)
        {
            int s = Mathf.Max(0, (int)seconds);
            return s >= 3600 ? $"{s / 3600} giờ {s % 3600 / 60} phút" : $"{s / 60} phút {s % 60} giây";
        }
    
        private void SetRowIcon(int i, EquipmentData item, Rarity rarity)
        {
            var sprite = GearIcons.For(item);
            _rowFrames[i].gameObject.SetActive(sprite != null);
            _rowFrames[i].color = GearIcons.FrameColor(rarity);
            GearIcons.Apply(_rowIcons[i], sprite, Color.white);
        }
    }
}
