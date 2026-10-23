using System;
using System.Collections.Generic;
using PhoAmThucViet.Analytics;
using PhoAmThucViet.Core;
using UnityEngine;

namespace PhoAmThucViet.Gameplay
{
    /// <summary>
    /// Mượn đầu bếp của người khác (kiểu Eatventure): đầu bếp mượn vào quán phụ nấu 5 phút,
    /// hiệu ứng trang bị của họ cộng dồn với của mình. Xem quảng cáo để kéo dài thêm 3 phút (1 lần mỗi lượt).
    /// Mỗi ngày mượn được 5 lượt; đầu bếp về rồi phải chờ 30 phút mới mượn tiếp. Thời gian tính theo giờ thực (vẫn trôi khi tắt game).
    /// </summary>
    public partial class RestaurantController
    {
        public const int HelperSeconds = 300, HelperExtendSeconds = 180, HelperDailyLimit = 5;
        /// <summary>Hồi chiêu tính từ lúc đầu bếp mượn về (đã gồm phần kéo dài bằng quảng cáo).</summary>
        public const int HelperCooldownSeconds = 1800;
        public const string HelperAdPlacement = "muon_dau_bep";

        private Worker _helper;
        private HelperCard _helperBonusCard;
        private ChefBonuses _helperBonuses;

        private static long NowUnix => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public HelperCard ActiveHelper => HelperActive ? State.helper : null;
        public bool HelperActive => State != null && State.helper != null && NowUnix < State.helperEndUnix;
        public float HelperSecondsLeft => HelperActive ? State.helperEndUnix - NowUnix : 0;
        public bool HelperCanExtend => HelperActive && !State.helperExtended;

        /// <summary>Còn bao nhiêu giây nữa mới mượn được người mới (0 = mượn được ngay).</summary>
        public float HelperCooldownLeft =>
            State == null || HelperActive || State.helperEndUnix <= 0 ? 0
                : Mathf.Max(0, State.helperEndUnix + HelperCooldownSeconds - NowUnix);

        /// <summary>Hiệu ứng trang bị của đầu bếp đang mượn (tính một lần rồi giữ).</summary>
        public ChefBonuses HelperBonuses
        {
            get
            {
                if (State?.helper == null) return default;
                if (_helperBonusCard != State.helper)
                {
                    _helperBonusCard = State.helper;
                    _helperBonuses = HelperChefs.Compute(State.helper, equipmentCatalog);
                }
                return _helperBonuses;
            }
        }

        private static string Today => DateTime.Today.ToString("yyyy-MM-dd");

        public int HelperBorrowsLeft
        {
            get
            {
                if (State == null) return 0;
                return State.helperDay == Today ? Mathf.Max(0, HelperDailyLimit - State.helperBorrowsToday) : HelperDailyLimit;
            }
        }

        /// <summary>Mượn một đầu bếp (5 phút). Không mượn được khi đang có người giúp hoặc hết lượt hôm nay.</summary>
        public bool BorrowHelper(HelperCard card)
        {
            if (card == null || HelperActive || HelperCooldownLeft > 0 || HelperBorrowsLeft <= 0) return false;
            if (State.helperDay != Today) { State.helperDay = Today; State.helperBorrowsToday = 0; }
            State.helperBorrowsToday++;
            State.helper = card;
            State.helperEndUnix = NowUnix + HelperSeconds;
            State.helperExtended = false;
            _helperBonusCard = null;
            RefreshBonuses();
            AnalyticsService.Log("helper_borrow", ("player", card.isPlayer ? 1 : 0), ("power", HelperChefs.Power(HelperBonuses)));
            SaveService.Save(State);
            return true;
        }

        /// <summary>Xem quảng cáo: đầu bếp ở lại thêm 3 phút (mỗi lượt mượn 1 lần).</summary>
        public void ExtendHelperWithAd(Action onDone = null)
        {
            if (!HelperCanExtend) return;
            Monetization.AdsService.ShowRewarded(State, HelperAdPlacement, () =>
            {
                if (!HelperActive) return;
                State.helperEndUnix += HelperExtendSeconds;
                State.helperExtended = true;
                SaveService.Save(State);
                onDone?.Invoke();
            });
        }

        // ================= Danh sách đầu bếp có thể mượn =================

        private readonly System.Random _helperRng = new System.Random();

        /// <summary>
        /// Danh sách để chọn: người chơi thật (lấy từ mạng, nếu có) trước, còn thiếu thì bù đầu bếp có sẵn trong game.
        /// </summary>
        public List<HelperCard> MakeHelperChoices(List<HelperCard> online, int count = 4)
        {
            var list = new List<HelperCard>();
            var names = new HashSet<string>();
            if (online != null)
                foreach (var c in online)
                {
                    if (list.Count >= count) break;
                    if (c == null || string.IsNullOrEmpty(c.name)) continue;
                    list.Add(c);
                    names.Add(c.name);
                }
            while (list.Count < count) list.Add(HelperChefs.MakeNpc(State, equipmentCatalog, _helperRng, names));
            return list;
        }
    }
}
