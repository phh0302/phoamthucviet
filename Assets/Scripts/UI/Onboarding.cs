using System;
using PhoAmThucViet.Audio;
using PhoAmThucViet.Core;
using PhoAmThucViet.Gameplay;
using PhoAmThucViet.Visual;
using UnityEngine;
using UnityEngine.UI;

namespace PhoAmThucViet.UI
{
    /// <summary>
    /// Hướng dẫn tân thủ bằng chuỗi mục tiêu ngắn (thay cho chữ hướng dẫn dài): thẻ "MỤC TIÊU" dưới dải tiến độ,
    /// mũi tên vàng chỉ đúng chỗ cần chạm, mỗi bước xong được ít đá quý rồi sang bước kế ngay.
    /// Chỉ chạy ở mốc đầu tiên của Hà Nội (vòng 1); người chơi đã đi xa hơn thì coi như xong.
    /// Tiến độ lưu ở GameState.tutorialStep.
    /// Thêm: dòng gợi ý "Còn ~N giây nữa đủ tiền …" khi chưa mua được gì (mọi mốc).
    /// </summary>
    public class Onboarding
    {
        private class Step
        {
            public Func<string> text;               // tính lúc hiện (tên quầy theo quán hiện tại)
            public Func<string> progress;          // chữ phụ (ví dụ "3/5"), null = không có
            public Func<bool> done;
            public RestaurantView.GuideTarget guide;
            public int guideIndex;
            public bool guideUpgradesButton;       // nhấp nháy nút "Nâng cấp quán"
            public Action onEnter;
            public int gems;
        }

        private readonly RestaurantController _game;
        private readonly RestaurantView _view;
        private readonly RectTransform _upgradesBtn;
        private readonly Step[] _steps;

        private RectTransform _card, _uiArrow;
        private Image _cardBg;
        private Text _title, _text, _reward;
        private int _enteredStep = -1;
        private float _celebrateUntil;
        private Vector3 _upgradesBtnScale = Vector3.one;

        private Text _hint;
        private float _hintTimer;

        public Onboarding(RestaurantController game, RestaurantView view, RectTransform layer, RectTransform upgradesButton,
                          float cardTop, float hintBottom)
        {
            _game = game;
            _view = view;
            _upgradesBtn = upgradesButton;
            _upgradesBtnScale = upgradesButton.localScale;
            _steps = BuildSteps();
            Build(layer, cardTop, hintBottom);
        }

        // ================= Các bước =================

        private StationData StationAt(int i) => i < _game.Venue.stations.Count ? _game.Venue.stations[i] : null;
        private int LevelAt(int i) { var d = StationAt(i); return d == null ? 0 : _game.State.GetStation(d.id)?.level ?? 0; }
        private string NameAt(int i) => StationAt(i)?.displayName ?? "quầy";

        private Step[] BuildSteps() => new[]
        {
            new Step
            {
                text = () => $"Chạm vào quầy {NameAt(0)}", guide = RestaurantView.GuideTarget.Station, guideIndex = 0,
                done = () => _view.Selected == StationAt(0) || LevelAt(0) >= 2,
            },
            new Step
            {
                text = () => $"Nâng {NameAt(0)} lên cấp 5", guide = RestaurantView.GuideTarget.Station, guideIndex = 0, gems = 5,
                progress = () => $"{Mathf.Min(LevelAt(0), 5)}/5", done = () => LevelAt(0) >= 5,
            },
            new Step
            {
                text = () => "Chạm đầu bếp chính lúc đang nấu để hối làm nhanh", guide = RestaurantView.GuideTarget.HeadChef, gems = 5,
                onEnter = () => _view.ClearSelection(), done = () => _game.HurryCount > 0,
            },
            new Step
            {
                text = () => $"Mở quầy {NameAt(1)}", guide = RestaurantView.GuideTarget.Station, guideIndex = 1, gems = 5,
                done = () => StationAt(1) == null || LevelAt(1) > 0,
            },
            new Step
            {
                text = () => "Khách sộp sắp tới! Chạm đồng xu tip trên bàn để nhặt", guide = RestaurantView.GuideTarget.Tip, gems = 5,
                onEnter = () => _game.RequestVip(),
                progress = () => _view.HasTipWaiting ? "có tip trên bàn!" : "đợi khách ăn xong…",
                done = () => _game.TipsCollected > 0,
            },
            new Step
            {
                text = () => "Mở \"Nâng cấp quán\": thuê người hoặc mua thêm bàn", guideUpgradesButton = true, gems = 10,
                done = () => { var s = _game.State; return s.chefs > 0 || s.waiters > 0 || s.tables > 1 || s.upgradesBought != 0; },
            },
            new Step
            {
                text = () => "Nâng hết quầy, người và cải tiến để LÊN ĐỜI!",
                progress = () => { var p = _game.GetProgress(); return p.levelsTotal > 0 ? $"{100 * p.levelsDone / p.levelsTotal}%" : null; },
                done = () => _game.State.venueIndex > 0 || _game.State.citiesCompleted > 0,
            },
        };

        /// <summary>Hướng dẫn chỉ dành cho mốc đầu Hà Nội ở vòng 1.</summary>
        private bool InFirstVenue => !_game.InEventRun && _game.Loop == 0 && _game.State.citiesCompleted == 0
                                     && _game.City == _game.startCity && _game.State.venueIndex == 0;

        // ================= Dựng giao diện =================

        private void Build(RectTransform layer, float cardTop, float hintBottom)
        {
            _cardBg = UIKit.Panel("Goal", layer, new Color(0.12f, 0.09f, 0.07f, 0.88f), UIKit.Rounded);
            _card = _cardBg.rectTransform;
            _card.TopLeft(12, cardTop, 640, 96);
            _cardBg.raycastTarget = false;
            _title = UIKit.Label("Title", _card, "", 20, Palette.Gold, TextAnchor.MiddleLeft, true);
            _title.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(18, -34), new Vector2(-120, -4));
            _text = UIKit.Label("Text", _card, "", 25, Color.white, TextAnchor.MiddleLeft, true);
            _text.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 1), new Vector2(18, 6), new Vector2(-120, -34));
            _reward = UIKit.Label("Reward", _card, "", 24, new Color(0.55f, 0.85f, 1f), TextAnchor.MiddleRight, true);
            _reward.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 1), new Vector2(-118, 6), new Vector2(-16, -6));

            // Mũi tên trên giao diện (chỉ vào nút "Nâng cấp quán")
            var arrow = UIKit.Panel("GuideArrow", layer, Color.white);
            arrow.sprite = RestaurantView.ArrowSprite();
            arrow.preserveAspect = true;
            arrow.raycastTarget = false;
            _uiArrow = arrow.rectTransform;
            _uiArrow.sizeDelta = new Vector2(70, 70);
            _uiArrow.gameObject.SetActive(false);

            // Dòng gợi ý phía trên hàng nút dưới
            var hintBg = UIKit.Panel("Hint", layer, new Color(0, 0, 0, 0.55f), UIKit.Pill);
            hintBg.raycastTarget = false;
            var hr = hintBg.rectTransform;
            hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 0);
            hr.pivot = new Vector2(0.5f, 0);
            hr.anchoredPosition = new Vector2(0, hintBottom);
            hr.sizeDelta = new Vector2(820, 56);
            _hint = UIKit.Label("Text", hr, "", 24, Color.white, TextAnchor.MiddleCenter, true);
            _hint.rectTransform.Place(Vector2.zero, Vector2.one, new Vector2(20, 0), new Vector2(-20, 0));
            _hint.supportRichText = true;
            hr.gameObject.SetActive(false);
        }

        // ================= Cập nhật =================

        public void Update(bool popupOpen)
        {
            UpdateGoals(popupOpen);
            UpdateHint(popupOpen);
        }

        private void UpdateGoals(bool popupOpen)
        {
            var s = _game.State;
            bool celebrating = Time.unscaledTime < _celebrateUntil;
            if (!celebrating && s.tutorialStep < _steps.Length - 1 && !InFirstVenue)
            {
                // Người chơi đã ở xa hơn mốc đầu (save cũ): bỏ qua hướng dẫn, không làm phiền.
                // Bước cuối "lên đời" thì để chạy tiếp bên dưới: vừa lên đời xong là được chúc mừng.
                s.tutorialStep = _steps.Length;
            }
            bool active = s.tutorialStep < _steps.Length || celebrating;
            _card.gameObject.SetActive(active);
            if (!active)
            {
                Hide();
                return;
            }
            if (celebrating) { Hide(keepCard: true); return; }

            var step = _steps[s.tutorialStep];
            if (_enteredStep != s.tutorialStep)
            {
                _enteredStep = s.tutorialStep;
                step.onEnter?.Invoke();
            }

            if (step.done())
            {
                // Xong bước: thưởng đá quý, thẻ chuyển xanh một lúc rồi sang bước kế
                s.gems += step.gems;
                s.tutorialStep++;
                _celebrateUntil = Time.unscaledTime + 1.2f;
                _cardBg.color = new Color(0.20f, 0.50f, 0.22f, 0.95f);
                _title.text = s.tutorialStep >= _steps.Length ? "HOÀN THÀNH HƯỚNG DẪN" : "XONG!";
                _text.text = step.text();
                _reward.text = step.gems > 0 ? $"+{step.gems} ♦" : "";
                SoundFX.Play(step.gems > 0 ? Sfx.Coin : Sfx.Upgrade, 0.7f);
                UIPunch.Play(_card);
                SaveService.Save(s);
                Hide(keepCard: true);
                return;
            }

            _cardBg.color = new Color(0.12f, 0.09f, 0.07f, 0.88f);
            _title.text = $"MỤC TIÊU {s.tutorialStep + 1}/{_steps.Length}";
            string prog = step.progress?.Invoke();
            string label = step.text();
            _text.text = string.IsNullOrEmpty(prog) ? label : $"{label} <color=#FFD54F>({prog})</color>";
            _reward.text = step.gems > 0 ? $"+{step.gems} ♦" : "rương";

            // Mũi tên: trong cảnh (quầy / đầu bếp / xu tip) hoặc trên nút "Nâng cấp quán"
            _view.SetGuide(popupOpen && step.guide == RestaurantView.GuideTarget.Station ? RestaurantView.GuideTarget.None : step.guide, step.guideIndex);
            bool uiGuide = step.guideUpgradesButton && !popupOpen;
            _uiArrow.gameObject.SetActive(uiGuide);
            if (uiGuide)
            {
                float bob = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * 18f;
                _uiArrow.position = _upgradesBtn.TransformPoint(_upgradesBtn.rect.center);
                _uiArrow.anchoredPosition += new Vector2(0, _upgradesBtn.rect.height * 0.5f + 46 + bob);
                float k = 1f + 0.06f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f));
                _upgradesBtn.localScale = _upgradesBtnScale * k;
            }
            else _upgradesBtn.localScale = _upgradesBtnScale;
        }

        private void Hide(bool keepCard = false)
        {
            _view.SetGuide(RestaurantView.GuideTarget.None);
            _uiArrow.gameObject.SetActive(false);
            _upgradesBtn.localScale = _upgradesBtnScale;
            if (!keepCard) _card.gameObject.SetActive(false);
        }

        /// <summary>Chưa đủ tiền mua gì: cho biết còn bao lâu nữa mua được món rẻ nhất (để người chơi không phải đoán).</summary>
        private void UpdateHint(bool popupOpen)
        {
            _hintTimer -= Time.unscaledDeltaTime;
            if (_hintTimer > 0) return;
            _hintTimer = 0.25f;
            var go = _hint.transform.parent.gameObject;
            if (popupOpen || _game.IsVenueComplete() || !_game.CheapestNext(out string what, out BigNum price) || _game.State.money >= price)
            {
                go.SetActive(false);
                return;
            }
            double secs = _game.SecondsUntil(price);
            if (secs < 0 || secs < 2.5) { go.SetActive(false); return; }
            string when = secs < 60 ? $"~{Math.Ceiling(secs)} giây" : secs < 3600 ? $"~{Math.Ceiling(secs / 60)} phút" : "hơn 1 giờ";
            string tipLine = secs > 45 && _game.BoostSecondsLeft <= 0 ? " · <color=#90CAF9>bấm Tăng tốc cho nhanh</color>"
                           : secs > 8 ? " · <color=#FFD54F>chạm nhân viên để hối</color>" : "";
            _hint.text = $"Còn {when} nữa đủ tiền {what}{tipLine}";
            go.SetActive(true);
        }
    }
}
