using PhoAmThucViet.Gameplay;
using PhoAmThucViet.Visual;
using UnityEngine;

namespace PhoAmThucViet.UI
{
    /// <summary>
    /// Điểm gắn của màn chơi thử (do menu "Phố Ẩm Thực/Tạo dữ liệu mẫu..." đặt vào scene).
    /// Giao diện chữ IMGUI cũ đã được thay hẳn bằng GameUI; thành phần này chỉ còn tự gắn
    /// cảnh quán (RestaurantView) và giao diện thật (GameUI) nếu scene chưa có.
    /// </summary>
    [RequireComponent(typeof(RestaurantController))]
    public class PrototypeUI : MonoBehaviour
    {
        private void Awake()
        {
            if (GetComponent<RestaurantView>() == null) gameObject.AddComponent<RestaurantView>();
            if (GetComponent<GameUI>() == null) gameObject.AddComponent<GameUI>();
        }
    }
}
