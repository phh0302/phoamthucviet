using System;
using System.Collections.Generic;
using PhoAmThucViet.Analytics;
using PhoAmThucViet.Core;

namespace PhoAmThucViet.Monetization
{
    /// <summary>Nguồn quảng cáo thưởng (giả lập hoặc SDK thật).</summary>
    public interface IAdProvider
    {
        /// <summary>Hiện quảng cáo; onDone(true) khi người chơi xem hết và được thưởng.</summary>
        void ShowRewarded(string placement, Action<bool> onDone);
    }

    /// <summary>Quảng cáo giả lập: màn che đếm ngược 3 giây rồi cho nhận thưởng.</summary>
    public class SimulatedAdProvider : IAdProvider
    {
        public void ShowRewarded(string placement, Action<bool> onDone) =>
            SimOverlay.Instance.Show("QUẢNG CÁO THỬ",
                $"Đây là quảng cáo giả lập cho vị trí \"{placement}\".\nKhi gắn Unity LevelPlay, quảng cáo thật sẽ hiện ở đây.",
                "Nhận thưởng", "Đóng (không nhận)", 3f, () => onDone(true), () => onDone(false));
    }

    /// <summary>
    /// Quảng cáo thưởng: giới hạn số lần mỗi ngày theo vị trí, gói "Bỏ quảng cáo" thì nhận thưởng ngay,
    /// ghi đo lường mỗi lần xem.
    /// Gắn quảng cáo thật: import Unity LevelPlay, viết lớp IAdProvider gọi LevelPlay rewarded,
    /// rồi đặt AdsService.Provider = new LớpĐó() khi khởi động game.
    /// </summary>
    public static class AdsService
    {
        public const string Boost = "tang_toc";
        public const string OfflineDouble = "offline_x2";
        public const string FreeGems = "da_quy_mien_phi";
        public const string FreeChest = "ruong_mien_phi";

        /// <summary>Số lần tối đa mỗi ngày theo vị trí (không có trong bảng = không giới hạn).</summary>
        public static readonly Dictionary<string, int> DailyLimits = new Dictionary<string, int>
        {
            { Boost, 12 },
            { FreeGems, 5 },
            { FreeChest, 3 },
        };

        public const int FreeGemsAmount = 20;

        public static IAdProvider Provider = new SimulatedAdProvider();

        public static bool Busy { get; private set; }

        private static string Today => DateTime.Today.ToString("yyyy-MM-dd");

        private static void RollDay(GameState state)
        {
            if (state.adDay == Today && state.adLog != null) return;
            state.adDay = Today;
            state.adLog = new List<string>();
        }

        public static int WatchedToday(GameState state, string placement)
        {
            RollDay(state);
            int n = 0;
            foreach (var p in state.adLog) if (p == placement) n++;
            return n;
        }

        /// <summary>Số lần còn xem được hôm nay (int.MaxValue nếu không giới hạn).</summary>
        public static int Remaining(GameState state, string placement) =>
            DailyLimits.TryGetValue(placement, out int max) ? Math.Max(0, max - WatchedToday(state, placement)) : int.MaxValue;

        public static bool CanShow(GameState state, string placement) => !Busy && Remaining(state, placement) > 0;

        /// <summary>Xem quảng cáo thưởng; onReward chỉ chạy khi xem hết (hoặc có gói Bỏ quảng cáo).</summary>
        public static void ShowRewarded(GameState state, string placement, Action onReward)
        {
            if (!CanShow(state, placement)) return;
            if (state.noAds)
            {
                Record(state, placement, skipped: true);
                onReward?.Invoke();
                return;
            }
            Busy = true;
            AnalyticsService.Log("ad_start", ("placement", placement));
            Provider.ShowRewarded(placement, ok =>
            {
                Busy = false;
                if (!ok) { AnalyticsService.Log("ad_cancel", ("placement", placement)); return; }
                Record(state, placement, skipped: false);
                onReward?.Invoke();
            });
        }

        private static void Record(GameState state, string placement, bool skipped)
        {
            RollDay(state);
            state.adLog.Add(placement);
            state.adsWatchedTotal++;
            AnalyticsService.Log("ad_reward", ("placement", placement), ("no_ads", skipped ? 1 : 0),
                ("today", WatchedToday(state, placement)));
        }
    }
}
