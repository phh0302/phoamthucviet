using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace PhoAmThucViet.Core
{
    /// <summary>
    /// Lưu và đọc GameState dạng JSON, mã hóa nhẹ (XOR + Base64) kèm mã kiểm tra
    /// để chặn sửa file bằng tay đơn giản. Không phải bảo mật thật.
    /// </summary>
    public static class SaveService
    {
        private const string FileName = "save.dat";
        private const string Key = "pho-am-thuc-viet";

        public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public static long NowUnix => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>Báo sau mỗi lần lưu xuống máy (CloudSync dùng để biết có dữ liệu mới).</summary>
        public static event Action<GameState> OnSaved;

        public static void Save(GameState state)
        {
            state.lastSaveUnix = NowUnix;
            string content = Encode(state);

            // Ghi file tạm rồi thay thế, tránh hỏng save nếu tắt app giữa chừng.
            string tmp = SavePath + ".tmp";
            File.WriteAllText(tmp, content);
            if (File.Exists(SavePath)) File.Delete(SavePath);
            File.Move(tmp, SavePath);
            OnSaved?.Invoke(state);
        }

        /// <summary>Đóng gói trạng thái thành chuỗi (mã kiểm tra + dữ liệu), dùng cho file lưu và lưu trên mạng.</summary>
        public static string Encode(GameState state)
        {
            string json = JsonUtility.ToJson(state);
            string payload = Convert.ToBase64String(Xor(Encoding.UTF8.GetBytes(json)));
            return Checksum(payload) + "\n" + payload;
        }

        /// <summary>Mở chuỗi từ Encode; null nếu hỏng hoặc bị sửa.</summary>
        public static GameState Decode(string content)
        {
            try
            {
                if (string.IsNullOrEmpty(content)) return null;
                string[] parts = content.Split('\n');
                if (parts.Length != 2 || parts[0] != Checksum(parts[1])) return null;
                string json = Encoding.UTF8.GetString(Xor(Convert.FromBase64String(parts[1])));
                return JsonUtility.FromJson<GameState>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Trả về null nếu chưa có save hoặc file bị hỏng/bị sửa.</summary>
        public static GameState Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return null;
                var state = Decode(File.ReadAllText(SavePath));
                if (state == null) Debug.LogWarning("[Save] File lưu không hợp lệ, bắt đầu lại.");
                return state;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Save] Không đọc được file lưu: " + ex.Message);
                return null;
            }
        }

        public static void Delete()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }

        /// <summary>
        /// Số giây đã rời game. Trả về 0 nếu đồng hồ máy bị chỉnh lùi.
        /// Bản phát hành nên lấy giờ máy chủ thay cho giờ máy.
        /// </summary>
        public static double SecondsSinceLastSave(GameState state)
        {
            if (state.lastSaveUnix <= 0) return 0;
            long diff = NowUnix - state.lastSaveUnix;
            return diff > 0 ? diff : 0;
        }

        private static byte[] Xor(byte[] data)
        {
            byte[] key = Encoding.UTF8.GetBytes(Key);
            var result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++) result[i] = (byte)(data[i] ^ key[i % key.Length]);
            return result;
        }

        private static string Checksum(string payload)
        {
            unchecked
            {
                // FNV-1a 32 bit
                uint hash = 2166136261;
                foreach (char c in payload + Key) { hash ^= c; hash *= 16777619; }
                return hash.ToString("x8");
            }
        }
    }
}
