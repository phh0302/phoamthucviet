# Đăng nhập & lưu trên mạng (Unity Gaming Services)

Game dùng **Authentication 3.8.0** và **Cloud Save 3.4.2** (đã thêm vào `Packages/manifest.json`).
Chưa làm các bước dưới đây thì game vẫn chạy bình thường, chỉ lưu trên máy (Cài đặt hiện "Chưa kết nối được").

## Việc bạn cần làm một lần

1. **Liên kết project với Unity Cloud**
   - Unity: `Edit > Project Settings > Services`.
   - Chọn tổ chức (Organization), bấm **Create project ID** (hoặc chọn project đã có).
2. **Bật cách đăng nhập** trên https://cloud.unity.com
   - Vào project vừa liên kết → **Player Authentication** (hoặc Products → Authentication) → **Identity providers**.
   - **Anonymous** thường đã bật sẵn.
   - Thêm **Username & Password** (Add identity provider → Username/Password).
3. **Cloud Save**: vào **Cloud Save** trong dashboard, bấm bật nếu được hỏi. Không cần tạo key: game tự ghi key `save`.
4. Bấm Play trong Unity → tab **Cài đặt** phải hiện "Đang chơi bằng tài khoản khách (mã …)".

## Game hoạt động thế nào

| Lúc | Việc |
|---|---|
| Mở game | Tự đăng nhập (khách, hoặc tài khoản đã đăng nhập lần trước) rồi so bản trên mạng với bản trên máy |
| Bản trên mạng do chính máy này đẩy lên | Đẩy bản trên máy lên (mới hơn) |
| Máy mới cài (chơi < 3 phút) | Tự tải bản trên mạng về |
| Hai bản khác nhau | Hiện hộp **Chọn bản lưu**: thành phố, mốc, đá quý, thời gian chơi, giờ lưu của từng bản |
| Đang chơi | Đẩy lên mỗi 2 phút và khi ẩn app |
| Tạo tài khoản | Gắn tên đăng nhập + mật khẩu vào tài khoản khách hiện tại (giữ nguyên tiến trình) |
| Đăng nhập tài khoản khác | Đẩy bản hiện tại lên tài khoản cũ, đăng nhập, rồi hỏi dùng bản nào |

- Đồ mua bằng tiền thật (bỏ quảng cáo, gói tân thủ, thẻ tháng) của cả hai bản đều được giữ khi đổi bản lưu.
- Quy tắc tên đăng nhập: 3–20 ký tự, chữ không dấu, số và `. - @ _`.
- Quy tắc mật khẩu: 8–30 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt.

## File

- `Online/CloudSync.cs`: kết nối, so sánh, tải về / đẩy lên, tạo tài khoản, đăng nhập.
- `UI/SettingsPanel.cs`: bảng Cài đặt (âm thanh + tài khoản) và hộp Chọn bản lưu.
- `RestaurantController.LoadState`: thay tiến trình bằng bản tải về.
- `SaveService.Encode/Decode`: cùng định dạng mã hóa với file lưu trên máy.

## Việc sau

- Đăng nhập Google Play Games (cần tài khoản Google Play Console).
- Kiểm tra trên máy chủ (Cloud Code) trước khi làm bảng xếp hạng: bản lưu vẫn do máy người chơi gửi lên nên có thể bị sửa.
