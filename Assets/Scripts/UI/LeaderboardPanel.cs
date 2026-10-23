using System.Collections.Generic;
using PhoAmThucViet.Audio;
using PhoAmThucViet.Gameplay;
using PhoAmThucViet.Online;
using UnityEngine;
using UnityEngine.UI;

namespace PhoAmThucViet.UI
{
    /// <summary>Bảng xếp hạng sự kiện: top 20 người chơi theo điểm sự kiện, dòng của mình tô vàng.</summary>
    public class LeaderboardPanel
    {
        private const int Rows = 20;
        private readonly RestaurantController _game;
        private RectTransform _panel;
        private Text _title, _info, _mine;
        private readonly List<Text> _rank = new List<Text>(), _name = new List<Text>(), _score = new List<Text>();
        private readonly List<Image> _rowBg = new List<Image>();
        private UIButton _refresh;
        private List<EventBoard.Entry> _list;
        private string _error;
        private bool _loading;
        private string _loadedKey;

        public bool IsOpen => _panel != null && _panel.gameObject.activeSelf;

        public LeaderboardPanel(RestaurantController game) => _game = game;

        public void Build(RectTransform layer, float bottom, float top)
        {
            var bg = UIKit.Panel("Leaderboard", layer, Palette.PanelBg, UIKit.Rounded);
            _panel = bg.rectTransform;
            _panel.Place(Vector2.zero, Vector2.one, new Vector2(20, bottom), new Vector2(-20, -top));
            bg.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0, -6);
            _title = UIKit.Label("Title", _panel, "", 34, Palette.Gold, TextAnchor.MiddleLeft, true);
            _title.rectTransform.TopLeft(28, 16, 760, 56);
            var close = UIKit.MakeButton("Close", _panel, "Đóng", 28, new Color(0.55f, 0.45f, 0.38f), Close);
            close.bg.rectTransform.TopRight(20, 14, 170, 64);
            _info = UIKit.Label("Info", _panel, "", 21, new Color(1, 1, 1, 0.8f), TextAnchor.UpperLeft);
            _info.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -150), new Vector2(-28, -84));
            _mine = UIKit.Label("Mine", _panel, "", 26, Palette.Gold, TextAnchor.MiddleLeft, true);
            _mine.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -200), new Vector2(-28, -152));
            for (int i = 0; i < Rows; i++)
            {
                float y = 210 + i * 52;
                var rb = UIKit.Panel("Row" + i, _panel, i % 2 == 0 ? new Color(1, 1, 1, 0.06f) : new Color(1, 1, 1, 0.02f));
                rb.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -(y + 48)), new Vector2(-20, -y));
                rb.raycastTarget = false;
                _rowBg.Add(rb);
                var r = UIKit.Label("Rank", rb.transform, "", 24, Color.white, TextAnchor.MiddleLeft, true);
                r.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 1), new Vector2(16, 0), new Vector2(100, 0));
                var n = UIKit.Label("Name", rb.transform, "", 24, Color.white, TextAnchor.MiddleLeft);
                n.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 1), new Vector2(110, 0), new Vector2(-230, 0));
                var s = UIKit.Label("Score", rb.transform, "", 24, Palette.Gold, TextAnchor.MiddleRight, true);
                s.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 1), new Vector2(-220, 0), new Vector2(-16, 0));
                _rank.Add(r); _name.Add(n); _score.Add(s);
            }
            _refresh = UIKit.MakeButton("Refresh", _panel, "Làm mới", 26, Palette.Orange, () => { SoundFX.Play(Sfx.Click); Refresh(); }, UIKit.Pill);
            _refresh.bg.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-180, 16), new Vector2(180, 88));
            _panel.gameObject.SetActive(false);
        }

        public void Open()
        {
            _panel.gameObject.SetActive(true);
            _panel.SetAsLastSibling();
            SoundFX.Play(Sfx.Click);
            var e = _game.CurrentEvent;
            if (e != null && _loadedKey != e.Key) Refresh();
        }

        public void Close()
        {
            _panel.gameObject.SetActive(false);
            SoundFX.Play(Sfx.Click);
        }

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        private async void Refresh()
        {
            var e = _game.CurrentEvent;
            if (_loading || e == null) return;
            _loading = true;
            await EventBoard.Publish(_game); // đăng điểm mới nhất trước để thấy mình trong bảng
            var (list, error) = await EventBoard.Fetch(e.Key);
            _list = list;
            _error = error;
            _loadedKey = e.Key;
            _loading = false;
        }

        public void Update()
        {
            if (!IsOpen) return;
            var e = _game.CurrentEvent;
            if (e == null) { Close(); return; }
            _title.text = $"Bảng xếp hạng · {e.name}";
            _info.text = $"Điểm = tổng cấp quầy cao nhất trong quán sự kiện + 10 × {e.token} + 100 × hạng quán. Còn {Core.EventService.DaysLeft(e)} ngày." +
                         (_error != null ? $"\n<color=#FFB74D>{_error}</color>" : "");
            int myRank = -1;
            if (_list != null) for (int i = 0; i < _list.Count; i++) if (_list[i].me) { myRank = i + 1; break; }
            _mine.text = $"Điểm của bạn: {_game.EventScore:N0}" +
                         (_list == null ? "" : myRank > 0 ? $" · hạng #{myRank}" : $" · ngoài top {_list.Count}");
            for (int i = 0; i < Rows; i++)
            {
                var entry = _list != null && i < _list.Count ? _list[i] : null;
                _rowBg[i].gameObject.SetActive(entry != null);
                if (entry == null) continue;
                var c = entry.me ? Palette.Gold : Color.white;
                _rank[i].text = $"#{i + 1}";
                _rank[i].color = i == 0 ? new Color(1f, 0.84f, 0.3f) : i == 1 ? new Color(0.85f, 0.88f, 0.92f) : i == 2 ? new Color(0.9f, 0.6f, 0.35f) : Color.white;
                _name[i].text = entry.me ? $"{entry.name} (bạn)" : entry.name;
                _name[i].color = c;
                _score[i].text = entry.score.ToString("N0");
                _rowBg[i].color = entry.me ? new Color(1f, 0.8f, 0.2f, 0.18f) : (i % 2 == 0 ? new Color(1, 1, 1, 0.06f) : new Color(1, 1, 1, 0.02f));
            }
            _refresh.Set(_loading ? "Đang tải…" : _list == null && _error == null ? "Tải bảng xếp hạng" : "Làm mới", !_loading);
        }
    }
}
