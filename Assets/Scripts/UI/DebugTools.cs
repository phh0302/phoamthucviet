using UnityEngine;

namespace PhoAmThucViet.UI
{
    /// <summary>
    /// Công cụ thử nghiệm (nút "Thử: qua mốc", "Thử: xóa save", "+200 ♦", "Thử: sự kiện") chỉ hiện trong Unity Editor
    /// và bản build Development. Bản phát hành (AAB gửi Google Play) tự ẩn hết.
    /// </summary>
    public static class DebugTools
    {
        public static bool Enabled => Application.isEditor || Debug.isDebugBuild;
    }
}
