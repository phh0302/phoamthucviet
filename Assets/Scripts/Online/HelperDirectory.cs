using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PhoAmThucViet.Core;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using UnityEngine;

namespace PhoAmThucViet.Online
{
    /// <summary>
    /// Danh bạ đầu bếp trên mạng (Unity Cloud Save, dữ liệu công khai của người chơi):
    /// - Đăng: lưu "chef_card" (JSON thẻ đầu bếp) và "chef_power" (số) ở mức công khai.
    /// - Tìm: truy vấn ngẫu nhiên người chơi có chef_power ≥ 0, lấy chef_card.
    /// Cần tạo index cho khóa chef_power (Player data · Public) trên Unity Dashboard → Cloud Save → Queries,
    /// nếu chưa có thì truy vấn lỗi và game tự dùng đầu bếp có sẵn.
    /// </summary>
    public static class HelperDirectory
    {
        public const string CardKey = "chef_card", PowerKey = "chef_power";

        public static bool Online => CloudSync.Instance != null && CloudSync.Instance.SignedIn;

        private static string _lastPublished = "";

        /// <summary>Tên hiện cho người khác: tên tài khoản, khách thì "Đầu bếp #xxxx".</summary>
        public static string DisplayName()
        {
            var cs = CloudSync.Instance;
            if (cs == null) return "Đầu bếp";
            if (!string.IsNullOrEmpty(cs.Username)) return cs.Username;
            string id = cs.PlayerId ?? "";
            return "Đầu bếp #" + (id.Length >= 4 ? id.Substring(id.Length - 4).ToUpper() : "0000");
        }

        /// <summary>Đăng thẻ đầu bếp của mình (bỏ qua nếu không đổi so với lần trước).</summary>
        public static async Task Publish(GameState state, EquipmentCatalog catalog)
        {
            if (!Online || state == null) return;
            try
            {
                var card = HelperChefs.FromState(state, DisplayName(), CloudSync.Instance.PlayerId);
                string json = JsonUtility.ToJson(card);
                if (json == _lastPublished) return;
                int power = HelperChefs.Power(HelperChefs.Compute(card, catalog));
                var data = new Dictionary<string, object> { { CardKey, json }, { PowerKey, power } };
                await CloudSaveService.Instance.Data.Player.SaveAsync(data,
                    new Unity.Services.CloudSave.Models.Data.Player.SaveOptions(new Unity.Services.CloudSave.Models.Data.Player.PublicWriteAccessClassOptions()));
                _lastPublished = json;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Cloud] Chưa đăng được thẻ đầu bếp: " + ex.Message);
            }
        }

        /// <summary>Lấy ngẫu nhiên tối đa <paramref name="count"/> đầu bếp của người chơi khác. Lỗi / không mạng → danh sách rỗng.</summary>
        public static async Task<List<HelperCard>> Fetch(int count)
        {
            var result = new List<HelperCard>();
            if (!Online) return result;
            try
            {
                var filters = new List<FieldFilter> { new FieldFilter(PowerKey, 0, FieldFilter.OpOptions.GE, false) };
                var query = new Query(filters, new HashSet<string> { CardKey }, 0, 20, 20);
                var rows = await CloudSaveService.Instance.Data.Player.QueryAsync(query,
                    new Unity.Services.CloudSave.Models.Data.Player.QueryOptions());
                string me = CloudSync.Instance.PlayerId;
                foreach (var row in rows)
                {
                    if (row == null || row.Id == me || row.Data == null) continue;
                    foreach (var item in row.Data)
                    {
                        if (item.Key != CardKey) continue;
                        var card = JsonUtility.FromJson<HelperCard>(item.Value.GetAs<string>());
                        if (card == null) continue;
                        card.isPlayer = true;
                        card.playerId = row.Id;
                        result.Add(card);
                    }
                }
                // Xáo trộn rồi lấy đủ số cần
                var rng = new System.Random();
                for (int i = result.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (result[i], result[j]) = (result[j], result[i]);
                }
                if (result.Count > count) result.RemoveRange(count, result.Count - count);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Cloud] Chưa tìm được đầu bếp trên mạng (đã tạo index chef_power chưa?): " + ex.Message);
            }
            return result;
        }
    }
}
