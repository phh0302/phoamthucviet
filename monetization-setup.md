# Gắn quảng cáo, thanh toán và Firebase thật

Game đã có sẵn mọi chỗ gọi quảng cáo, mua hàng và đo lường, đang chạy bằng **bản giả lập**
(`Scripts/Monetization`, `Scripts/Analytics`). Khi có tài khoản, chỉ cần cắm SDK vào, không phải sửa giao diện hay luật chơi.

## 1. Quảng cáo thưởng – Unity LevelPlay
1. Tạo tài khoản Unity LevelPlay (dashboard.unity3d.com → Monetization), tạo app Android, lấy **App Key**.
2. Trong Unity: Window → Package Manager → cài **Ads Mediation (LevelPlay)**.
3. Tạo một Rewarded ad unit trên dashboard.
4. Viết lớp `LevelPlayAdProvider : IAdProvider` (gọi API rewarded của LevelPlay, gọi `onDone(true)` khi người chơi được thưởng),
   rồi lúc khởi động game đặt `AdsService.Provider = new LevelPlayAdProvider();`.

Vị trí quảng cáo đã có (tên dùng để đo lường): `tang_toc` (12 lần/ngày), `offline_x2`, `da_quy_mien_phi` (5 lần/ngày, 20 ♦),
`ruong_mien_phi` (3 lần/ngày, rương 1 món). Giới hạn chỉnh ở `AdsService.DailyLimits`.

## 2. Mua trong app – Unity IAP + Google Play Console
1. Tạo app trên Google Play Console (phí đăng ký 25 USD một lần), tạo các sản phẩm trong app với **đúng mã**:

| Mã | Gói | Giá gợi ý | Loại |
|---|---|---|---|
| `pho_starter_pack` | Gói khởi đầu: 500 ♦ + rương sự kiện 3 món (Sử thi+) + tăng tốc 30 phút | 49.000 ₫ | Mua 1 lần |
| `pho_monthly_card` | Thẻ tháng: 300 ♦ ngay + 50 ♦/ngày × 30 ngày | 99.000 ₫ | Tiêu hao (mua lại để gia hạn) |
| `pho_no_ads` | Bỏ quảng cáo: quà quảng cáo nhận ngay | 79.000 ₫ | Mua 1 lần |
| `pho_gems_100` | 100 ♦ | 22.000 ₫ | Tiêu hao |
| `pho_gems_550` | 550 ♦ | 99.000 ₫ | Tiêu hao |
| `pho_gems_1200` | 1200 ♦ | 199.000 ₫ | Tiêu hao |

2. Package Manager → cài **In-App Purchasing**.
3. Viết lớp `UnityIapStoreProvider : IStoreProvider` (khởi tạo IAP với các mã trên, gọi `onDone(true)` khi mua xong),
   đặt `StoreService.Provider = new UnityIapStoreProvider();` khi khởi động.
4. Gói mua 1 lần (khởi đầu, bỏ quảng cáo) cần **khôi phục giao dịch** khi cài lại máy.

## 3. Đo lường – Firebase Analytics
1. Tạo project trên console.firebase.google.com, thêm app Android (đúng package name), tải `google-services.json` vào `Assets/`.
2. Import **Firebase Analytics** Unity SDK.
3. Project Settings → Player → Scripting Define Symbols: thêm `FIREBASE_ANALYTICS`. Từ đó mọi sự kiện gửi lên Firebase.

Sự kiện đang ghi (bản thử ghi vào `analytics.csv` trong thư mục lưu game):
`first_open`, `session_start(day)` → D1/D7, `venue_complete(city, venue, minutes)`, `city_complete`, `chest_open`,
`quest_claim`, `event_reward`, `ad_start / ad_reward / ad_cancel(placement)`, `iap_start / iap_purchase / iap_cancel(product)`,
`monthly_claim`, `save_reset`.
