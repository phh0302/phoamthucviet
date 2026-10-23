using PhoAmThucViet.Audio;
using PhoAmThucViet.Core;
using PhoAmThucViet.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace PhoAmThucViet.UI
{
    /// <summary>Tab Nhiệm vụ: lịch điểm danh 7 ngày và 3 nhiệm vụ ngày.</summary>
    public class QuestPage
    {
        private readonly RestaurantController _game;

        private class DayCell { public Image bg; public Text day, reward; }
        private readonly DayCell[] _days = new DayCell[7];
        private UIButton _loginBtn;

        private class QuestCard { public Image bg; public Text desc, progressText; public Image fill; public UIButton claim; }
        private readonly QuestCard[] _quests = new QuestCard[QuestService.QuestsPerDay];
        private Text _resetText;

        // Sự kiện theo mùa
        private Image _eventBg;
        private Text _eventTitle, _eventSub, _eventTokens;
        private Image _eventFill;
        private class RewardCell { public Image bg; public Text need, reward; }
        private readonly RewardCell[] _rewardCells = new RewardCell[EventService.Rewards.Length];
        private UIButton _eventClaim, _eventDebug;
        private string _debugLabel => EventService.DebugLabel ?? "theo lịch";

        public QuestPage(RestaurantController game) => _game = game;

        public void Build(RectTransform page)
        {
            // ----- Điểm danh 7 ngày -----
            var login = UIKit.Panel("Login", page, new Color(1, 1, 1, 0.08f), UIKit.Rounded).rectTransform;
            login.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -380), new Vector2(-20, -16));

            var title = UIKit.Label("Title", login, "Quà điểm danh 7 ngày", 32, Palette.Gold, TextAnchor.MiddleLeft, true);
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -64), new Vector2(-24, -10));

            var row = UIKit.NewRect("Days", login);
            row.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -236), new Vector2(-16, -72));
            for (int i = 0; i < 7; i++)
            {
                var c = new DayCell();
                c.bg = UIKit.Panel("Day" + i, row, new Color(0.30f, 0.22f, 0.16f), UIKit.Rounded);
                c.bg.rectTransform.Place(new Vector2(i / 7f, 0), new Vector2((i + 1) / 7f, 1), new Vector2(5, 0), new Vector2(-5, 0));
                c.day = UIKit.Label("Day", c.bg.transform, $"Ngày {i + 1}", 22, Color.white, TextAnchor.MiddleCenter, true);
                c.day.rectTransform.Place(new Vector2(0, 0.6f), Vector2.one, new Vector2(4, 0), new Vector2(-4, -6));
                string reward = i < QuestService.LoginGems.Length ? $"{QuestService.LoginGems[i]}\n♦" : "Rương\n3 món";
                c.reward = UIKit.Label("Reward", c.bg.transform, reward, 26, Palette.Gold, TextAnchor.MiddleCenter, true);
                c.reward.rectTransform.Place(Vector2.zero, new Vector2(1, 0.62f), new Vector2(4, 6), new Vector2(-4, 0));
                _days[i] = c;
            }

            _loginBtn = UIKit.MakeButton("Claim", login, "", 32, Palette.Green, OnClaimLogin);
            _loginBtn.bg.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(24, 18), new Vector2(-24, 112));

            // ----- Nhiệm vụ ngày -----
            var header = UIKit.Label("QuestTitle", page, "Nhiệm vụ hôm nay", 32, Palette.Gold, TextAnchor.MiddleLeft, true);
            header.rectTransform.Place(new Vector2(0, 1), new Vector2(0.6f, 1), new Vector2(28, -448), new Vector2(0, -392));
            _resetText = UIKit.Label("Reset", page, "", 24, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleRight);
            _resetText.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(1, 1), new Vector2(0, -448), new Vector2(-28, -392));

            for (int i = 0; i < _quests.Length; i++)
            {
                var q = new QuestCard();
                q.bg = UIKit.Panel("Quest" + i, page, Palette.Card, UIKit.Rounded);
                float top = 460 + i * 162;
                q.bg.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -(top + 150)), new Vector2(-20, -top));
                var t = q.bg.transform;
                q.desc = UIKit.Label("Desc", t, "", 28, Palette.TextDark, TextAnchor.MiddleLeft, true);
                q.desc.rectTransform.TopLeft(24, 12, 680, 52);
                q.fill = UIKit.ProgressBar("Bar", t, new Color(0, 0, 0, 0.12f), Palette.Green, out var bar);
                bar.TopLeft(24, 72, 560, 24);
                q.progressText = UIKit.Label("Progress", t, "", 24, Palette.TextSoft);
                q.progressText.rectTransform.TopLeft(24, 102, 680, 36);
                int index = i;
                q.claim = UIKit.MakeButton("Claim", t, "", 28, Palette.Green, () => OnClaimQuest(index));
                q.claim.bg.rectTransform.TopRight(16, 14, 250, 122);
                _quests[i] = q;
            }

            BuildEvent(page, 460 + _quests.Length * 162 + 12);
        }

        private void BuildEvent(RectTransform page, float top)
        {
            _eventBg = UIKit.Panel("Event", page, new Color(0.55f, 0.12f, 0.10f, 0.9f), UIKit.Rounded);
            var t = _eventBg.rectTransform;
            t.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -(top + 470)), new Vector2(-20, -top));

            _eventTitle = UIKit.Label("Title", t, "", 32, Palette.Gold, TextAnchor.MiddleLeft, true);
            _eventTitle.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -60), new Vector2(-24, -10));
            _eventSub = UIKit.Label("Sub", t, "", 22, new Color(1, 1, 1, 0.85f), TextAnchor.MiddleLeft);
            _eventSub.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -100), new Vector2(-24, -62));
            _eventTokens = UIKit.Label("Tokens", t, "", 28, Color.white, TextAnchor.MiddleLeft, true);
            _eventTokens.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -146), new Vector2(-24, -104));
            _eventFill = UIKit.ProgressBar("Bar", t, new Color(0, 0, 0, 0.35f), Palette.Gold, out var bar);
            bar.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -178), new Vector2(-24, -152));

            var row = UIKit.NewRect("Rewards", t);
            row.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -340), new Vector2(-16, -192));
            int n = _rewardCells.Length;
            for (int i = 0; i < n; i++)
            {
                var c = new RewardCell();
                c.bg = UIKit.Panel("Cell" + i, row, new Color(0.30f, 0.22f, 0.16f), UIKit.Rounded);
                c.bg.rectTransform.Place(new Vector2((float)i / n, 0), new Vector2((float)(i + 1) / n, 1), new Vector2(4, 0), new Vector2(-4, 0));
                c.need = UIKit.Label("Need", c.bg.transform, EventService.Rewards[i].tokens.ToString(), 24, Palette.Gold, TextAnchor.MiddleCenter, true);
                c.need.rectTransform.Place(new Vector2(0, 0.66f), Vector2.one, new Vector2(4, 0), new Vector2(-4, -4));
                var r = EventService.Rewards[i];
                string text = r.chestItems > 0 ? (r.eventChest ? $"Rương\n{r.chestItems} món\nSử thi+" : $"Rương\n{r.chestItems} món") : $"{r.gems}\n♦";
                c.reward = UIKit.Label("Reward", c.bg.transform, text, 22, Color.white, TextAnchor.MiddleCenter, true);
                c.reward.rectTransform.Place(Vector2.zero, new Vector2(1, 0.68f), new Vector2(4, 4), new Vector2(-4, 0));
                _rewardCells[i] = c;
            }

            _eventClaim = UIKit.MakeButton("Claim", t, "", 30, Palette.Green, OnClaimEvent);
            _eventClaim.bg.rectTransform.Place(new Vector2(0, 0), new Vector2(0.64f, 0), new Vector2(24, 20), new Vector2(-6, 110));
            _eventDebug = UIKit.MakeButton("Debug", t, "", 20, new Color(0.3f, 0.3f, 0.3f, 0.85f), OnDebugEvent, UIKit.Pill);
            _eventDebug.bg.rectTransform.Place(new Vector2(0.64f, 0), new Vector2(1, 0), new Vector2(6, 20), new Vector2(-24, 110));
            if (!DebugTools.Enabled)
            {
                _eventDebug.bg.gameObject.SetActive(false);
                _eventClaim.bg.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(24, 20), new Vector2(-24, 110));
            }
        }

        private void UpdateEvent()
        {
            var state = _game.State;
            var e = _game.CurrentEvent;
            _eventDebug.Set($"Thử: sự kiện\n({_debugLabel})", true);
            if (e == null)
            {
                var upcoming = EventService.Next;
                _eventBg.color = new Color(0.30f, 0.22f, 0.16f, 0.9f);
                _eventTitle.text = upcoming != null
                    ? $"Sự kiện sắp tới: {upcoming.name} ({upcoming.start:dd/MM})"
                    : "Chưa có sự kiện";
                _eventSub.text = upcoming != null ? $"Còn {(upcoming.start - System.DateTime.Today).Days} ngày nữa. Trong sự kiện, khách để lại {upcoming.token} để đổi quà." : "";
                _eventTokens.text = "";
                _eventFill.fillAmount = 0;
                foreach (var c in _rewardCells) c.bg.color = new Color(0.25f, 0.2f, 0.16f);
                _eventClaim.Set("Chưa đến sự kiện", false);
                return;
            }

            var color = new Color(e.color[0], e.color[1], e.color[2]);
            _eventBg.color = Color.Lerp(color, Color.black, 0.35f);
            _eventTitle.text = $"{e.name} · còn {EventService.DaysLeft(e)} ngày";
            _eventSub.text = $"Khách để lại {e.token} khi nhận món · khách trả thêm {(EventService.ProfitBonus - 1) * 100:0}% trong sự kiện";
            int next = EventService.NextReward(state);
            int goal = next >= 0 ? EventService.Rewards[next].tokens : EventService.Rewards[EventService.Rewards.Length - 1].tokens;
            int prev = next > 0 ? EventService.Rewards[next - 1].tokens : 0;
            _eventTokens.text = next >= 0 ? $"{state.eventTokens} {e.token} · mốc tiếp theo {goal}" : $"{state.eventTokens} {e.token} · đã đạt mọi mốc!";
            _eventFill.fillAmount = next >= 0 ? Mathf.Clamp01((float)(state.eventTokens - prev) / Mathf.Max(1, goal - prev)) : 1f;

            int claimable = 0;
            for (int i = 0; i < _rewardCells.Length; i++)
            {
                var c = _rewardCells[i];
                if (EventService.IsClaimed(state, i)) c.bg.color = new Color(0.25f, 0.40f, 0.25f);
                else if (EventService.CanClaim(state, i))
                {
                    claimable++;
                    c.bg.color = Color.Lerp(Palette.Orange, Palette.Gold, Mathf.Abs(Mathf.Sin(Time.time * 4)));
                }
                else c.bg.color = new Color(0.30f, 0.22f, 0.16f);
            }
            _eventClaim.Set(claimable > 0 ? $"Nhận quà ({claimable})" : "Gom thêm để nhận quà", claimable > 0);
        }

        private void OnClaimEvent()
        {
            bool any = false;
            for (int i = 0; i < EventService.Rewards.Length; i++)
                if (_game.ClaimEventReward(i)) any = true;
            if (any) SoundFX.Play(Sfx.Chest);
        }

        private void OnDebugEvent()
        {
            EventService.CycleDebug();
            SoundFX.Play(Sfx.Click);
        }

        public void Update()
        {
            var state = _game.State;
            var quests = _game.Quests;

            // Điểm danh
            int current = QuestService.LoginDayIndex(state);
            bool canLogin = QuestService.CanClaimLogin(state);
            // Hôm nay đã nhận thì ô vừa nhận là ô trước đó
            int highlight = canLogin ? current : (current + 6) % 7;
            for (int i = 0; i < 7; i++)
            {
                // Đã nhận trong vòng 7 ngày này (vừa nhận xong ngày 7 thì cả 7 ô đều đã nhận)
                bool claimed = i < current || (!canLogin && current == 0);
                Color c = i == highlight ? (canLogin ? Palette.Orange : Palette.Green)
                        : claimed ? new Color(0.25f, 0.40f, 0.25f)
                        : new Color(0.30f, 0.22f, 0.16f);
                _days[i].bg.color = c;
            }
            _loginBtn.Set(canLogin ? $"Nhận quà ngày {current + 1}" : "Đã nhận hôm nay, quay lại ngày mai nhé!", canLogin);
            if (canLogin)
            {
                float k = 1 + Mathf.Sin(Time.time * 5) * 0.02f;
                _loginBtn.bg.rectTransform.localScale = new Vector3(k, k, 1);
            }
            else _loginBtn.bg.rectTransform.localScale = Vector3.one;

            // Nhiệm vụ
            var left = QuestService.UntilReset;
            _resetText.text = $"Làm mới sau {(int)left.TotalHours}:{left.Minutes:00}";
            for (int i = 0; i < _quests.Length; i++)
            {
                var card = _quests[i];
                if (i >= quests.Count) { card.bg.gameObject.SetActive(false); continue; }
                card.bg.gameObject.SetActive(true);
                var q = quests[i];
                card.desc.text = QuestService.Describe(q);
                card.fill.fillAmount = q.target > 0 ? (float)q.progress / q.target : 0;
                card.progressText.text = $"{q.progress:N0}/{q.target:N0}  ·  thưởng {q.reward} ♦";
                card.bg.color = q.claimed ? Palette.CardLocked : Palette.Card;
                if (q.claimed) card.claim.Set("Đã nhận", false);
                else if (QuestService.CanClaim(q)) card.claim.Set($"Nhận\n{q.reward} ♦", true);
                else card.claim.Set("Chưa xong", false);
            }

            UpdateEvent();
        }

        private void OnClaimLogin()
        {
            if (_game.ClaimLogin()) SoundFX.Play(Sfx.Chest);
        }

        private void OnClaimQuest(int index)
        {
            if (_game.ClaimQuest(index)) SoundFX.Play(Sfx.Coin);
        }
    }
}
