using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace PhoAmThucViet.Analytics
{
    /// <summary>
    /// Ghi sự kiện đo lường (D1/D7, qua màn, quảng cáo, mua hàng...).
    /// Bản hiện tại: in ra Console và ghi file analytics.csv trong Application.persistentDataPath để xem lại khi chơi thử.
    /// Khi gắn Firebase: thêm define FIREBASE_ANALYTICS (Project Settings → Player → Scripting Define Symbols)
    /// sau khi import Firebase Analytics SDK, mọi sự kiện sẽ gửi lên Firebase.
    /// </summary>
    public static class AnalyticsService
    {
        private const string FirstOpenKey = "analytics_first_open";
        private static string _path;

        public static string LogPath => _path ??= Path.Combine(Application.persistentDataPath, "analytics.csv");

        /// <summary>Số ngày kể từ lần mở game đầu tiên (0 = ngày đầu; 1 = D1; 7 = D7).</summary>
        public static int DaysSinceFirstOpen
        {
            get
            {
                string first = PlayerPrefs.GetString(FirstOpenKey, "");
                if (!DateTime.TryParse(first, out var d)) return 0;
                return Math.Max(0, (DateTime.Today - d.Date).Days);
            }
        }

        /// <summary>Gọi một lần khi mở game.</summary>
        public static void SessionStart()
        {
            if (string.IsNullOrEmpty(PlayerPrefs.GetString(FirstOpenKey, "")))
            {
                PlayerPrefs.SetString(FirstOpenKey, DateTime.Today.ToString("yyyy-MM-dd"));
                PlayerPrefs.Save();
                Log("first_open");
            }
            Log("session_start", ("day", DaysSinceFirstOpen));
        }

        public static void Log(string name, params (string key, object value)[] parameters)
        {
            var sb = new StringBuilder();
            sb.Append(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")).Append(',').Append(name);
            foreach (var (key, value) in parameters) sb.Append(',').Append(key).Append('=').Append(value);
            string line = sb.ToString();
            Debug.Log("[Analytics] " + line);
            try { File.AppendAllText(LogPath, line + "\n"); }
            catch (Exception) { /* không ghi được file thì bỏ qua, không làm hỏng game */ }

#if FIREBASE_ANALYTICS
            var ps = new Firebase.Analytics.Parameter[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var (key, value) = parameters[i];
                ps[i] = value switch
                {
                    int v => new Firebase.Analytics.Parameter(key, v),
                    long v => new Firebase.Analytics.Parameter(key, v),
                    float v => new Firebase.Analytics.Parameter(key, v),
                    double v => new Firebase.Analytics.Parameter(key, v),
                    _ => new Firebase.Analytics.Parameter(key, value?.ToString() ?? ""),
                };
            }
            Firebase.Analytics.FirebaseAnalytics.LogEvent(name, ps);
#endif
        }
    }
}
