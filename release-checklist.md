# Phát hành thử trên Google Play – từng bước

Những gì đã có sẵn trong project:
- Icon app `Assets/Art/Icon/app_icon.png` (1024×1024) và ảnh bìa cửa hàng `Assets/Art/Icon/feature_graphic.png` (1024×500).
- Menu **Phố Ẩm Thực → Phát hành**: áp cài đặt Android (tên app, mã app, màn dọc, IL2CPP + ARM64, API mới nhất, màn hình chờ đỏ son, icon), build AAB, build APK.
- Bản phát hành tự ẩn các nút "Thử: ..." và "+200 ♦".

## A. Một lần trên máy bạn
1. **Unity Hub → Installs → ⚙ bản Unity đang dùng → Add modules → Android Build Support** (tích cả OpenJDK, Android SDK & NDK Tools) → Install.
2. Mở project, vào **Phố Ẩm Thực → Phát hành → 1. Áp dụng cài đặt Android**.
   - Muốn đổi mã app (mặc định `com.phoamthuc.phoamthucviet`) thì sửa `PackageName` trong `Assets/Scripts/Editor/ReleaseSetup.cs` **trước lần tải lên đầu tiên**.
3. **Tạo keystore**: Project Settings → Player → Android → Publishing Settings → Keystore Manager → Create New → đặt mật khẩu.
   Lưu file keystore và mật khẩu ở nơi an toàn (Google Drive riêng + giấy). Mất keystore = không cập nhật được app.
4. Cắm điện thoại Android (bật Developer options → USB debugging) hoặc chạy **3. Build APK** rồi gửi file APK qua Zalo/Drive để cài thử.

## B. Google Play Console
1. Đăng ký tài khoản nhà phát triển tại play.google.com/console (phí 25 USD một lần, cần xác minh danh tính).
   Tài khoản cá nhân mới phải **thử nghiệm kín với ít nhất 12 người trong 14 ngày** trước khi được phát hành rộng.
2. **Tạo ứng dụng**: tên "Phố Ẩm Thực Việt", ngôn ngữ mặc định Tiếng Việt, loại Trò chơi, miễn phí.
3. **Thiết lập ứng dụng** (mục Dashboard):
   - Chính sách quyền riêng tư: đăng nội dung `privacy-policy.md` lên một trang web công khai (Google Sites miễn phí), dán link.
   - Quảng cáo: chọn "Có" (game có quảng cáo thưởng).
   - Phân loại nội dung: làm bảng hỏi IARC (game nấu ăn, không bạo lực) → thường ra 3+.
   - Đối tượng: 13 tuổi trở lên (tránh chính sách Gia đình khi có quảng cáo).
   - An toàn dữ liệu: khai thu thập "Mã định danh thiết bị" và "Tương tác với ứng dụng" (cho quảng cáo và Firebase), mục đích Phân tích + Quảng cáo, có mã hóa khi truyền.
4. **Trang cửa hàng**: mô tả ngắn/dài (gợi ý bên dưới), icon 512×512 (thu nhỏ từ `app_icon.png`), ảnh bìa `feature_graphic.png`, ít nhất 2 ảnh chụp màn hình điện thoại (chụp trong Unity Game view tỉ lệ 9:16 hoặc trên máy thật).
5. **Thử nghiệm → Thử nghiệm kín → Tạo bản phát hành**: chạy **2. Build AAB**, tải file `Builds/PhoAmThucViet-x.y.z-N.aab` lên, thêm danh sách email người thử (≥ 12 người, mục tiêu 20–50), gửi link tham gia cho họ.
6. Mỗi lần sửa game: build AAB lại (menu tự tăng số phiên bản), tải lên bản mới.

## C. Trong lúc thử kín (14 ngày)
- Xem `analytics.csv` (máy bạn) hoặc Firebase (sau khi gắn) để theo dõi: D1, D7, số phút qua từng mốc, tỉ lệ qua Hà Nội, số quảng cáo/người/ngày.
- Hỏi người thử: đoạn nào chán, chờ lâu, khó hiểu; giá cửa hàng có hợp lý không.
- Chỉnh số rồi phát hành bản mới.

## Gợi ý chữ cho trang cửa hàng
**Mô tả ngắn (≤ 80 ký tự):** Mở quán ăn khắp Việt Nam, từ xe đẩy vỉa hè tới nhà hàng lớn!

**Mô tả dài:**
Bắt đầu với một xe đẩy phở nhỏ ở Hà Nội, thuê đầu bếp, phục vụ, mua thêm bàn và nâng cấp món ăn để quán đông khách hơn.
Mỗi thành phố đi qua 5 mốc: xe đẩy, quán cóc, quán trong ngõ, quán mặt phố và nhà hàng lớn – rồi "lên xe" tới thành phố tiếp theo:
Hạ Long, Huế, Hội An, Đà Nẵng, Nha Trang, Đà Lạt, Sài Gòn, Cần Thơ, Phú Quốc.
• Hơn 50 món đặc sản ba miền
• Trang bị cho đầu bếp chính: dao, nồi, tạp dề, mũ – 4 độ hiếm, nâng cấp bằng mảnh
• Nhiệm vụ mỗi ngày, quà điểm danh 7 ngày
• Sự kiện Tết, Trung Thu với bao lì xì, đèn lồng đổi quà
• Kiếm tiền cả khi tắt game
