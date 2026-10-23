using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PhoAmThucViet.Gameplay;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using UnityEngine;

namespace PhoAmThucViet.Online
{
    /// <summary>
    /// Bảng xếp hạng sự kiện (Unity Cloud Save, dữ liệu công khai): mỗi người chơi lưu
    /// ev_key (mã lần sự kiện, ví dụ "tet-2027"), ev_score (điểm), ev_name (tên hiện).
    /// Truy vấn: ev_key = sự kiện hiện tại, sắp theo ev_score giảm dần.
    /// Cần tạo index trên Unity Dashboard → Cloud Save → Queries: Player data · Public · khóa ev_key (chuỗi) và ev_score (số).
    /// </summary>
    public static class EventBoard
    {
        public const string KeyKey = "ev_key", ScoreKey = "ev_score", NameKey = "ev_name";

        public class Entry
        {
            public string name, playerId;
            public int score;
            public bool me;
        }

        private static string _lastPublished = "";

        /// <summary>Đăng điểm sự kiện của mình (bỏ qua nếu không đổi).</summary>
        public static async Task Publish(RestaurantController game)
        {
            var e = game != null ? game.CurrentEvent : null;
            if (!HelperDirectory.Online || e == null) return;
            int score = game.EventScore;
            string name = HelperDirectory.DisplayName();
            string sig = $"{e.Key}:{score}:{name}";
            if (sig == _lastPublished) return;
            try
            {
                var data = new Dictionary<string, object> { { KeyKey, e.Key }, { ScoreKey, score }, { NameKey, name } };
                await CloudSaveService.Instance.Data.Player.SaveAsync(data,
                    new Unity.Services.CloudSave.Models.Data.Player.SaveOptions(new Unity.Services.CloudSave.Models.Data.Player.PublicWriteAccessClassOptions()));
                _lastPublished = sig;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Cloud] Chưa đăng được điểm sự kiện: " + ex.Message);
            }
        }

        /// <summary>Top người chơi của lần sự kiện <paramref name="eventKey"/>. Lỗi thì trả về null kèm lý do.</summary>
        public static async Task<(List<Entry> list, string error)> Fetch(string eventKey, int limit = 30)
        {
            if (!HelperDirectory.Online) return (null, "Cần kết nối mạng để xem bảng xếp hạng.");
            try
            {
                var filters = new List<FieldFilter>
                {
                    new FieldFilter(KeyKey, eventKey, FieldFilter.OpOptions.EQ, true),
                    new FieldFilter(ScoreKey, 0, FieldFilter.OpOptions.GE, false), // false = điểm cao trước
                };
                var query = new Query(filters, new HashSet<string> { NameKey, ScoreKey }, 0, limit);
                var rows = await CloudSaveService.Instance.Data.Player.QueryAsync(query,
                    new Unity.Services.CloudSave.Models.Data.Player.QueryOptions());
                string me = CloudSync.Instance != null ? CloudSync.Instance.PlayerId : "";
                var list = new List<Entry>();
                foreach (var row in rows)
                {
                    if (row?.Data == null) continue;
                    var entry = new Entry { playerId = row.Id, me = row.Id == me, name = "Đầu bếp" };
                    foreach (var item in row.Data)
                    {
                        if (item.Key == NameKey) entry.name = item.Value.GetAs<string>();
                        else if (item.Key == ScoreKey) entry.score = item.Value.GetAs<int>();
                    }
                    list.Add(entry);
                }
                list.Sort((a, b) => b.score.CompareTo(a.score));
                return (list, null);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Cloud] Chưa tải được bảng xếp hạng (đã tạo index ev_key + ev_score chưa?): " + ex.Message);
                return (null, "Chưa tải được bảng xếp hạng, thử lại sau.");
            }
        }
    }
}
