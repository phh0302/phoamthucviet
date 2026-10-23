using PhoAmThucViet.Core;
using PhoAmThucViet.Gameplay;
using UnityEditor;
using UnityEngine;

namespace PhoAmThucViet.EditorTools
{
    /// <summary>
    /// Menu Phố Ẩm Thực → Thử sự kiện: bật một sự kiện (Tết, Trung Thu, Quốc khánh, Giáng sinh) bất kể ngày,
    /// tự bấm Play nếu chưa chạy rồi vào thẳng quán sự kiện. Lựa chọn được nhớ qua các lần Play;
    /// chọn "Tắt thử (theo lịch)" để trở lại bình thường.
    /// </summary>
    [InitializeOnLoad]
    public static class EventTestMenu
    {
        private const string EnterKey = "PhoAmThuc.EnterEventOnPlay";
        private static double _deadline;

        static EventTestMenu()
        {
            EditorApplication.playModeStateChanged += s =>
            {
                if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(EnterKey, false)) WaitAndEnter();
            };
        }

        [MenuItem("Phố Ẩm Thực/Thử sự kiện/Tết Nguyên Đán", priority = 200)] private static void Tet() => Start(0);
        [MenuItem("Phố Ẩm Thực/Thử sự kiện/Tết Trung Thu", priority = 201)] private static void TrungThu() => Start(1);
        [MenuItem("Phố Ẩm Thực/Thử sự kiện/Quốc khánh", priority = 202)] private static void QuocKhanh() => Start(2);
        [MenuItem("Phố Ẩm Thực/Thử sự kiện/Giáng sinh", priority = 203)] private static void Noel() => Start(3);

        [MenuItem("Phố Ẩm Thực/Thử sự kiện/Tắt thử (theo lịch)", priority = 220)]
        private static void Off()
        {
            EventService.DebugForced = -1;
            SessionState.SetBool(EnterKey, false);
            Debug.Log("[Phố Ẩm Thực] Đã tắt thử sự kiện, sự kiện chạy theo lịch thật.");
        }

        private static void Start(int index)
        {
            EventService.DebugForced = index;
            Debug.Log($"[Phố Ẩm Thực] Thử sự kiện: {EventService.DebugLabel}. Vào quán sự kiện…");
            if (EditorApplication.isPlaying) WaitAndEnter();
            else
            {
                SessionState.SetBool(EnterKey, true);
                EditorApplication.isPlaying = true;
            }
        }

        /// <summary>Đợi game nạp xong (tối đa 8 giây) rồi vào quán sự kiện.</summary>
        private static void WaitAndEnter()
        {
            SessionState.SetBool(EnterKey, false);
            _deadline = EditorApplication.timeSinceStartup + 8;
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup > _deadline)
            {
                EditorApplication.update -= Poll;
                return;
            }
            var game = Object.FindFirstObjectByType<RestaurantController>();
            if (game == null || game.State == null || game.City == null) return;
            EditorApplication.update -= Poll;
            if (game.InEventRun) return;
            if (game.EventRunCompleted)
            {
                Debug.Log("[Phố Ẩm Thực] Quán sự kiện này đã nâng hết rồi (dùng nút \"Thử: xóa save\" để chơi lại từ đầu).");
                return;
            }
            if (game.CanEnterEventRun) game.EnterEventRun();
            else Debug.LogWarning("[Phố Ẩm Thực] Chưa vào được quán sự kiện (game chưa sẵn sàng).");
        }
    }
}
