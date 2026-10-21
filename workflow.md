# Phố Ẩm Thực Việt – Workflow

Game idle quản lý nhà hàng trên mobile (Unity 6.3 LTS, Universal 2D, màn hình dọc). Người chơi mở chuỗi quán ăn dọc Việt Nam: Hà Nội → Hạ Long → Huế → ... → Phú Quốc. Mỗi thành phố gồm 5 mốc, từ xe đẩy nhỏ tới nhà hàng lớn, tổng khoảng 50 phút.

Tài liệu thiết kế đầy đủ: https://claude.ai/code/artifact/1f87db10-9278-466c-b654-01ecf47f017b

---

## 1. Tổng quan tiến độ

| Giai đoạn | Nội dung | Trạng thái |
| --- | --- | --- |
| 0. Thiết kế | Tài liệu thiết kế game: vòng lặp, thành phố, kinh tế, kiếm tiền, kiến trúc | Xong |
| 1. Lõi kinh tế | BigNum, dữ liệu quầy/thành phố, công thức giá, lưu game, offline | Xong |
| 2. Chơi thử bằng chữ | Màn chơi thử IMGUI, công cụ mô phỏng nhịp chơi, chỉnh số lần 1 | Xong, đang chơi thử |
| 2b. Trang bị đầu bếp chính | 4 ô trang bị, 16 món, rương mỗi mốc, nâng cấp bằng bản trùng | Xong, chờ chơi thử |
| 2c. Nhân viên + qua màn kiểu Eatventure | Đầu bếp phụ mỗi quầy, phục vụ chung; phải nâng hết mới qua màn | Xong, chờ chơi thử |
| 2d. 5 mốc mỗi thành phố | Xe đẩy → ... → nhà hàng lớn; rương mỗi mốc | Xong, chờ chơi thử |
| 2e. Bỏ sao, thêm phân rã | Món riêng có cấp; phân rã thành mảnh cùng vị trí để nâng cấp | Xong, chờ chơi thử |
| 3. Cảnh quán thật | Hình quán, khách xếp hàng, đầu bếp và phục vụ đi lại | Đang làm: bản hình khối tạm xong |
| 4. Giao diện thật | Canvas UI thay IMGUI, hiệu ứng, âm thanh | Gần xong: còn font đẹp và nhạc nền |
| 5. Giữ chân | Nhiệm vụ ngày, điểm danh, sự kiện theo lịch Việt, hiệu ứng rương | Xong phần code; còn hình trang bị |
| 6. Kiếm tiền và đo | Quảng cáo thưởng, gói mua, Firebase Analytics | Xong phần trong game (giả lập); chờ gắn SDK thật |
| 7. Phát hành thử | Build Android, thử kín 20–50 người, đo D1/D7 | Đang làm: công cụ build + tài liệu xong, chờ tài khoản Play |

---

## 2. Cấu trúc thư mục

```
Assets/
├── Scripts/
│   ├── Core/                  Tầng lõi: không phụ thuộc scene
│   │   ├── BigNum.cs          Số lớn: m × 10^e, hiển thị 1.2K, 3.4M, 5.6aa
│   │   ├── StationData.cs     Cấu hình một quầy món (ScriptableObject)
│   │   ├── CityData.cs        Một thành phố = danh sách 5 mốc + thành phố kế tiếp
│   │   ├── VenueData.cs       Một mốc (xe đẩy → nhà hàng lớn): quầy, phục vụ, số món rương
│   │   ├── EconomyService.cs  Mọi công thức giá, lợi nhuận, offline
│   │   ├── GameState.cs       Dữ liệu được lưu xuống máy
│   │   ├── SaveService.cs     Lưu/đọc JSON mã hóa nhẹ, chống sửa file
│   │   ├── EquipmentData.cs   Một món trang bị (ô, độ hiếm, hiệu ứng theo cấp)
│   │   ├── EquipmentCatalog.cs Danh sách trang bị + tỷ lệ rơi của rương
│   │   ├── ChefBonuses.cs     Tổng hiệu ứng trang bị, có trần
│   │   └── EquipmentService.cs Tính hiệu ứng, mở rương, trang bị, phân rã, nâng cấp bằng mảnh
│   ├── Gameplay/
│   │   └── RestaurantController.cs  Vòng nấu món, thu tiền, nâng cấp, chuyển thành phố
│   ├── UI/
│   │   ├── GameUI.cs          Giao diện thật: thanh trên cùng, cảnh quán toàn màn hình, dải tiến độ, ô nâng cấp nhỏ cạnh quầy, nút lên đời, tab bar
│   │   ├── ChefPage.cs        Tab Đầu bếp chính: ô trang bị, mảnh, mua rương, kho đồ
│   │   ├── QuestPage.cs       Tab Nhiệm vụ: điểm danh 7 ngày, 3 nhiệm vụ ngày
│   │   ├── PopupLayer.cs      Popup tiền offline và mở rương
│   │   ├── UIKit.cs           Công cụ dựng uGUI bằng code (Canvas, nút bo tròn, thanh tiến độ, màu)
│   │   └── PrototypeUI.cs     Giao diện chữ cũ: còn tab Đầu bếp chính + popup (sẽ thay dần)
│   ├── Audio/
│   │   └── SoundFX.cs         Âm thanh tạo bằng code, bật/tắt
│   ├── Visual/                Cảnh quán (nửa trên màn hình), chỉ hiển thị, không đổi kinh tế
│   │   ├── RestaurantView.cs  Quầy, đầu bếp, bàn, khách, phục vụ, tiền bay lên
│   │   ├── SpriteFactory.cs   Tạo hình khối / chữ tạm bằng code (chưa cần file hình)
│   │   ├── FloatingText.cs    Chữ "+1.2K" bay lên rồi mờ dần
│   │   └── SpriteLibrary.cs   Nạp hình vẽ theo tên file, đổi tên món thành tên file không dấu
│   └── Editor/                Chỉ chạy trong Unity Editor
│       ├── PrototypeSetup.cs  Menu tạo dữ liệu mẫu + màn chơi thử
│       └── PacingSimulator.cs Cửa sổ mô phỏng nhịp chơi
└── Data/
    ├── Cities/                ha-noi.asset, ha-long.asset
    ├── Venues/                hn-1 … hn-5, hl-1 … hl-5
    ├── Stations/              hn-1-1 (Phở bò ở xe đẩy), hn-1-2, ... (mỗi mốc có bộ quầy riêng)
    ├── Equipment/             16 món trang bị
    └── equipment-catalog.asset
```

Nguyên tắc: tầng trên chỉ gọi tầng dưới (UI → Gameplay → Core → Data). Thêm thành phố mới chỉ cần tạo dữ liệu, không sửa code.

---

## 3. Những gì đã làm

### 3.1. Lõi kinh tế (`Scripts/Core`)

- [x] **BigNum**: tiền trong game idle vượt giới hạn `double`, nên lưu dạng phần định trị + số mũ. Có cộng, trừ, nhân, chia, so sánh, lũy thừa và hiển thị K, M, B, T, aa, ab...
- [x] **StationData / CityData**: số liệu nằm trong file `.asset`, chỉnh trực tiếp trong Inspector, không cần sửa code.
- [x] **EconomyService**: công thức theo tài liệu thiết kế
  - Giá nâng từ cấp n lên n+1: `C(n) = giá gốc × r^n × hệ số thành phố`
  - Lợi nhuận mỗi món: `P(n) = lợi nhuận gốc × n × 2^(số mốc nhân đôi đã đạt) × hệ số mốc × khẩu vị × trang bị`
  - **Lợi nhuận quầy nhân đôi ở cấp 10, 25, 50, 75, 100, 150, 200, 250** (mỗi mốc ×2, cộng dồn; quầy chỉ có các mốc ≤ cấp tối đa của nó)
  - Mua x1, x10, tối đa (tính bằng tổng cấp số nhân, không lặp)
- [x] **Thu nhập offline**: chỉ quầy có quản lý, 50% tốc độ, tối đa 1 giờ, và không vượt số tiền của 3 phút chơi thật (để không qua màn khi tắt game). Chặn trường hợp chỉnh lùi đồng hồ.
- [x] **Lưu game**: JSON → XOR → Base64 + mã kiểm tra FNV. Ghi file tạm rồi thay thế để không hỏng save khi tắt app giữa chừng. Tự lưu mỗi 10 giây và khi app vào nền.

### 3.2. Màn chơi thử (`Scripts/Gameplay`, `Scripts/UI`)

- [x] Quầy tự nấu món theo thời gian nấu; mỗi món = 1 khách được phục vụ.
- [x] Chưa có quản lý: tiền nằm chờ ở quầy, bấm "Thu tiền". Có quản lý: tự thu.
- [x] Mở quầy theo thứ tự, nâng cấp x1 / x10 / tối đa.
- [x] Nút tăng tốc x2 trong 5 phút (giả lập xem quảng cáo).
- [x] Popup tiền offline, có lựa chọn nhận gấp đôi (giả lập quảng cáo).
- [x] Qua màn khi đã nâng hết (xem 3.6); hiện nút "Lên xe đi Hạ Long", thưởng 50 đá quý + rương, tiền, quầy và nhân viên reset.

### 3.3. Công cụ Editor (menu **Phố Ẩm Thực**)

| Menu | Tác dụng |
| --- | --- |
| Tạo dữ liệu mẫu và màn chơi thử | Tạo Hà Nội, Hạ Long (mỗi nơi 5 quầy) nếu chưa có, đặt đối tượng `Restaurant (prototype)` vào scene |
| Cập nhật số liệu mẫu (ghi đè) | Ghi đè số liệu về bản mẫu mới nhất và xóa file lưu |
| Mô phỏng nhịp chơi | Giả lập người chơi tích cực, in số phút xong mỗi thành phố và lúc mở từng quầy |
| Xóa file lưu game | Chơi lại từ đầu |

### 3.4. Chỉnh số lần 1 (sau phản hồi "chờ hơi lâu") – đã thay bằng bảng ở 3.6

| | Trước | Sau |
| --- | --- | --- |
| Lần mua đầu tiên | sau 22 giây | sau 6 giây |
| Lần chờ lâu nhất | 73 giây | 17 giây |
| Hệ số tăng giá r | 1.08 | 1.07 |
| Thời gian nấu | 2–4 giây | 1–2,6 giây |
| Thuê quản lý Phở bò | 50 | 20 |
| Mục tiêu tiền Hà Nội | 1M | 10M |
| Xong Hà Nội / Hạ Long | ~14 / ~16 phút | ~13 / ~15 phút |

### 3.5. Trang bị đầu bếp chính

Đầu bếp chính đi cùng người chơi suốt hành trình, **không reset** khi sang thành phố mới. Nguyên tắc cân bằng: mốc 15–20 phút mỗi thành phố tính cho người **chưa có** trang bị; trang bị làm màn nhanh hơn.

| Ô | Tác dụng | Thường | Hiếm | Sử thi | Huyền thoại |
| --- | --- | --- | --- | --- | --- |
| Dao | Lợi nhuận mỗi món | Dao thái thịt | Dao Đa Sỹ | Dao phở gia truyền | Dao vàng ngự thiện |
| Nồi | Giảm thời gian nấu | Nồi nhôm | Chảo gang | Nồi nước dùng 100 lít | Nồi đồng cung đình |
| Tạp dề | Giảm giá mở quầy, nâng cấp | Tạp dề vải | Tạp dề bò | Tạp dề lụa Hà Đông | Tạp dề thêu rồng |
| Mũ | Tiền offline + thời gian tăng tốc | Mũ vải | Nón lá | Mũ đầu bếp trắng | Khăn xếp ngự thiện |

- Cấp tối đa: Thường 10, Hiếm 20, Sử thi 30, Huyền thoại 40.
- Mỗi lần rơi ra là **một món riêng**, bắt đầu ở cấp 1 (không gộp bản trùng).
- **Phân rã** món không đang dùng → **mảnh của đúng vị trí đó** (mảnh dao, mảnh nồi, mảnh tạp dề, mảnh mũ). Món cấp 1 cho 10 / 25 / 60 / 200 mảnh (Thường / Hiếm / Sử thi / Huyền thoại), cộng lại 80% số mảnh đã bỏ vào món đó.
- **Nâng cấp** chỉ tốn mảnh cùng vị trí: từ cấp n lên n+1 tốn (1 / 2 / 3 / 5) × n mảnh. Không tốn đá quý.

| Nâng tới cấp tối đa | Thường (10) | Hiếm (20) | Sử thi (30) | Huyền thoại (40) |
| --- | --- | --- | --- | --- |
| Tổng mảnh | 45 | 380 | 1.305 | 3.900 |
| Ước tính số thành phố (chỉ từ rương mốc) | dưới 1 | ~6 | ~20 | ~58 (cấp 10: ~3,5) |

Mỗi thành phố cho 9 món (rương 5 mốc), phân rã hết được khoảng 270 mảnh, chia đều 4 vị trí ≈ 67 mảnh mỗi vị trí. Rương mua bằng đá quý làm nhanh hơn.
- Trần hiệu ứng: lợi nhuận +160%, nấu nhanh 35%, giảm giá 35%, offline/tăng tốc +200%.

| Huyền thoại | Cấp 1 | Cấp 40 (tối đa) |
| --- | --- | --- |
| Dao vàng ngự thiện (lợi nhuận) | +60% | +157,5% |
| Nồi đồng cung đình (nấu nhanh) | 15% | 34,5% |
| Tạp dề thêu rồng (giảm giá) | 15% | 34,5% |
| Khăn xếp ngự thiện (offline, tăng tốc) | +100% | +197,5% |

So với Sử thi cấp tối đa (+44% lợi nhuận, 13,7% nấu nhanh và giảm giá), Huyền thoại mạnh gấp 2–3 lần.
- **Không còn hệ thống sao.** Rương nhận khi hoàn thành mỗi mốc, số món theo mốc: mốc 1–2: 1 món, mốc 3–4: 2 món, mốc 5: 3 món. Mua thêm rương 2 món bằng 100 đá quý. Mỗi mốc +10 đá quý, xong thành phố +50.

| Tỷ lệ rơi | Thường | Hiếm | Sử thi | Huyền thoại |
| --- | --- | --- | --- | --- |
| Mọi rương | 50% | 33% | 12,5% | 4,5% |

Ảnh hưởng tới thời gian xong Hà Nội (mô phỏng, đeo đủ 4 ô cùng độ hiếm ở cấp tối đa):

| Trang bị | Không có | Thường | Hiếm | Sử thi | Huyền thoại |
| --- | --- | --- | --- | --- | --- |
| Thời gian | ~13 phút | ~11,5 phút | ~9,5 phút | ~7,5 phút | ~5,5 phút |

Trong game: tab **Đầu bếp chính** để xem hiệu ứng, trang bị, nâng cấp, mua rương. Nút "+200 đá quý (chỉ để thử)" chỉ có trong bản chơi thử.
Cửa sổ **Mô phỏng nhịp chơi** có thanh trượt để giả định trang bị và xem màn ngắn đi bao nhiêu.

### 3.6. Nhân viên và qua màn kiểu Eatventure

Dùng tiền trong màn để nâng cấp quầy **và** thuê nhân viên. **Muốn qua màn phải nâng hết tất cả:**

- [x] Mở hết quầy, nâng mọi quầy lên **cấp tối đa** (mỗi quầy có cấp tối đa riêng)
- [x] Mỗi quầy đủ **4 đầu bếp** (nấu song song: mỗi lượt ra số món = số đầu bếp)
- [x] Quán đủ **5 phục vụ** (chung mọi quầy, mỗi người thêm +20% tốc độ)
- [x] Thuê **quản lý** cho mọi quầy

Giá đầu bếp thứ k+1 = 3 × giá nâng cấp ở cấp (cấp tối đa × k / 4), nên luôn vừa túi tiền ở giai đoạn đó. Giá phục vụ thứ 2–5 bằng giá mở quầy 2–5.

Số liệu Hà Nội hiện tại:

| Quầy | Giá mở | Quản lý | Giá nâng gốc | Hệ số tăng giá | Cấp tối đa | Lợi nhuận gốc | Nấu (giây) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Phở bò | 0 | 20 | 5 | 1.07 | 50 | 1 | 1.0 |
| Trà đá | 1,000 | 1,500 | 300 | 1.07 | 40 | 6 | 1.4 |
| Bún chả | 10,000 | 15,000 | 3,500 | 1.07 | 40 | 36 | 1.8 |
| Nem rán | 125,000 | 187,500 | 42,500 | 1.06 | 30 | 216 | 2.2 |
| Phở gà | 500,000 | 750,000 | 500,000 | 1.05 | 25 | 1,300 | 2.6 |

Phục vụ thứ 2–5: 1K, 10K, 125K, 500K. Hạ Long: mọi giá gốc ×1,15, hệ số thành phố ×60.

Kết quả mô phỏng (người chơi tích cực, chưa tính tiền quản lý):

| Thành phố | Thời gian nâng hết | Lần mua đầu | Chờ lâu nhất | Sao (không trang bị) |
| --- | --- | --- | --- | --- |
| Hà Nội | ~13,5 phút | 6 giây | ~40 giây | (bản cũ, xem 3.7) |
| Hạ Long | ~15,5 phút | 7 giây | ~47 giây | (bản cũ, xem 3.7) |

Với trang bị (Hà Nội): Thường ~11 phút, Hiếm ~9 phút, Sử thi ~7 phút, Huyền thoại ~5 phút.

### 3.7. Năm mốc mỗi thành phố (xe đẩy → nhà hàng lớn)

Mỗi thành phố có 5 mốc. Mỗi mốc áp dụng luật "nâng hết mới lên đời" (3.6). Lên đời thì tiền, quầy, nhân viên reset; đá quý và trang bị được giữ. Xong mốc 5 thì lên xe sang thành phố mới.

| Mốc | Hà Nội | Hạ Long | Số quầy | Phục vụ tối đa | Cấp tối đa mỗi quầy | Mốc lợi nhuận ×2 | Giá tăng mỗi cấp | Mục tiêu | Mô phỏng HN / HL |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Xe đẩy phở | Xe đẩy mực nướng | 2 | 2 | 25 | 10, 25 | 16% | 2–4 phút | 3,2 / 3,5 phút |
| 2 | Quán cóc vỉa hè | Quán cóc bờ biển | 3 | 3 | 50 | 10, 25, 50 | 8,5% | 4–6 phút | 4,7 / 5,2 phút |
| 3 | Quán phở ngõ nhỏ | Quán hải sản bến tàu | 4 | 4 | 150 | 10 … 150 (6 lần) | 4,2% | 10–15 phút | 13,4 / 14,8 phút |
| 4 | Quán phở mặt phố | Nhà hàng ven vịnh | 5 | 5 | 250 | 10 … 250 (8 lần) | 3,2% | 15–20 phút | 15,8 / 17,3 phút |
| 5 | Nhà hàng phở Hà Thành | Nhà hàng du thuyền | 6 | 6 | 250 | 10 … 250 (8 lần) | 3% | 15–20 phút | 16,1 / 17,6 phút |
| | | | | | | | | **Cả thành phố** | **~53 / ~58 phút** |

- **Mọi quầy trong cùng một mốc có chung cấp tối đa và chung mức tăng giá**: 25 / 50 / 150 / 250 / 250 cấp. Mốc nhiều cấp thì giá tăng chậm hơn mỗi cấp để lợi nhuận theo kịp. Cấu hình ở bảng `Specs` trong `PrototypeSetup.cs`.
- Số liệu sinh từ một "khung" chung (quầy thứ i dùng giá mở, giá nâng, lợi nhuận, thời gian nấu của hàng i), nhân hệ số k riêng của mốc (1,5 / 1,5 / 3,5 / 1,7 / 1,7).
- Lần mua đầu ở mọi mốc sau khoảng 6 giây; chờ lâu nhất giữa 2 lần mua chỉ 16–25 giây.
- Số tiền hiển thị to dần: mốc sau ×10 so với mốc trước, Hạ Long ×100.000 so với Hà Nội (chỉ để cảm giác lớn mạnh, không đổi thời gian chơi).

**Phần thưởng mỗi mốc:** 10 đá quý + rương (mốc 1–2: 1 món, mốc 3–4: 2 món, mốc 5: 3 món). Xong mốc 5 thưởng thêm 50 đá quý. Không phụ thuộc chơi nhanh hay chậm.

**Thời lượng trên là khi CHƯA có trang bị.** Trang bị đầu bếp chính rút ngắn theo tỷ lệ gần như bảng dưới (bảng đo với bộ cấp tối đa cũ, tỷ lệ rút ngắn vẫn tương tự):

| Trang bị | Mốc 1 | Mốc 2 | Mốc 3 | Mốc 4 | Mốc 5 | Cả thành phố |
| --- | --- | --- | --- | --- | --- | --- |
| Không có | 3,0 | 4,7 | 10,7 | 16,1 | 16,1 | ~50 phút |
| Thường | 2,5 | 4,0 | 9,1 | 13,6 | 13,7 | ~43 phút |
| Hiếm | 2,1 | 3,2 | 7,4 | 11,1 | 11,1 | ~35 phút |
| Sử thi | 1,6 | 2,5 | 5,6 | 8,4 | 8,4 | ~27 phút |
| Huyền thoại cấp 1 | 1,4 | 2,2 | 4,9 | 7,3 | 7,3 | ~23 phút |
| Huyền thoại cấp 40 | 0,6 | 0,8 | 1,9 | 2,8 | 2,8 | ~9 phút |


Bảng số liệu Hà Nội ở 3.6 là bản cũ, đã được thay bằng mục này.

### Cảnh quán vẽ bằng code (thay mảng màu)

- Mỗi mốc một phong cách (không cần file ảnh):
  | Mốc | Nền | Tường | Quầy 3D | Bàn 3D | Đồ treo tường |
  |---|---|---|---|---|---|
  | 1 Xe đẩy vỉa hè | gạch lát xám so le | cửa cuốn sắt + mái hiên sọc đỏ | xe đẩy inox có bánh | bàn nhựa xanh thấp | bóng đèn, tờ quảng cáo, đồng hồ |
  | 2 Quán cóc | gạch tàu đỏ | mành tre + mái hiên xanh | quầy gỗ | bàn gỗ vuông | bóng đèn, kệ hũ gia vị, bảng thực đơn |
  | 3 Ngõ nhỏ | gạch ca rô kem - nâu | tường vôi vàng loang | quầy ốp gạch, mặt gỗ | bàn tròn gỗ | cửa sổ chớp xanh, kệ, đồng hồ, thực đơn |
  | 4 Mặt phố | gạch bông xanh - đỏ | gạch men trắng | bếp inox | bàn tròn mặt đá | cửa sổ kính, thực đơn, kệ, đồng hồ |
  | 5 Nhà hàng lớn | thảm đỏ hoa văn | ốp gỗ đỏ nẹp vàng | quầy gỗ đỏ viền vàng, mặt đá | bàn khăn đỏ, mâm xoay vàng, ấm trà | câu đối Phúc/Lộc/Thọ, cửa sổ tròn, đèn lồng |
- **Bố cục dọc (RestaurantView: WallVisible 0,6 · CounterDepthFromTop 2,9 · CookSpotBehind 0,95 · BarY 0,38):** tường chỉ còn dải mỏng 0,6 trên cùng với đồ treo nhỏ; khu bếp sâu ~2 đơn vị; đầu bếp nấu đứng lùi sau quầy 0,95 nên không bị thanh nấu / đĩa món / chữ đè lên; thanh nấu và đĩa món nằm trên mặt quầy; đầu bếp rảnh đứng chờ trong bếp, phục vụ chờ ở dải trước quầy.
- **Ba khu tách bạch** (từ trên xuống): tường bếp (thấp, chỉ phần trên) → **sàn bếp** gạch nhỏ vuông (xi măng / men xám / men trắng / đá xám đậm) nơi đầu bếp đứng → **dải sàn trước quầy** (ván gỗ ở 3 mốc đầu, **đá mài** ở mặt phố, đá hoa cương chỉ vàng ở nhà hàng lớn) có gờ viền và bậc bóng xuống → **sảnh ăn** với sàn riêng.
- **Bảng tên món** gắn trên mặt quầy, co giãn theo độ dài tên: giấy trắng viền đỏ viết tay (vỉa hè), ván gỗ đóng đinh (quán cóc, ngõ nhỏ), **biển men xanh viền trắng kiểu biển số nhà Hà Nội** (mặt phố), sơn mài đỏ viền vàng (nhà hàng lớn). Quầy chưa mở thì bảng xám.
- Phong cách dịu mắt: nền/tường giảm tương phản và độ rực (SceneArt.WithStyle), shader 3D bớt rực 20%, sáng nhẹ, viền nâu mảnh, đồ treo tường giảm một nửa.
- Trên quầy có nồi nước dùng / chảo / xửng hấp / vỉ nướng (xoay vòng theo quầy) + thớt và rau thơm; quầy chưa mở màu xám.
- Trên bàn: ống đũa tre + chai tương ớt. Bàn chưa mua chỉ còn bóng mờ.
- **Đường phố trước quán** (camera lùi ra: nửa bề ngang nhìn thấy 4,2 → 5,0 nên quán nhỏ lại): lòng đường nhựa có vạch giữa, lề đường, vỉa hè gạch be, tường mặt tiền thấp chừa lối vào 1,6 ở giữa, cây xanh, xe máy đỗ (vỉa hè/quán cóc thêm két nhựa).
  - Người đi bộ (tối đa 4) đi dọc vỉa hè, xe máy có người lái (tối đa 2) chạy hai chiều đúng làn phải; chỉ để trang trí.
  - Khách tới: xuất hiện trên vỉa hè cạnh cửa → rẽ vào cửa → tới ghế (vẫn trong 1,5 giây đi vào như kinh tế cũ). Khách về: ra cửa → ra vỉa hè → đi dọc phố khuất mép màn hình.
- **Ghế cố định** bên trái mỗi bàn (hiện khi đã mua bàn); khách ngồi nghiêng, quay mặt vào bàn: ghế nhựa thấp đỏ/xanh/lục (vỉa hè) · đẩu gỗ (quán cóc) · ghế gỗ tựa lưng (ngõ nhỏ) · ghế gỗ uốn chân sắt (mặt phố) · ghế sơn mài đỏ tựa cao viền vàng (nhà hàng lớn). Bàn hẹp lại một chút để chừa chỗ ghế.
- Cửa: thảm chùi chân, chậu cây hai bên; vỉa hè/quán cóc có chồng két nhựa, mốc cao có bình gốm men lam.
- Bảng thực đơn ghi tên các món của quán.
- Bàn, quầy, cây là khối 3D cùng kiểu nhân vật; độ sâu z = 25 + y / tan(28°) khớp đúng góc nghiêng nên người đứng sau quầy / ngồi trước bàn che nhau đúng.
- Vẫn ưu tiên hình thật nếu có: `venues/floor_N`, `wall_N`, `counter_N`, `table_N` (N = 1…5).
- File: `Visual/SceneArt.cs` (nền, tường, đồ treo vẽ điểm ảnh), `Visual/Props.cs` (bàn, quầy, cây, két 3D), `MeshKit.Box`.

### Nhân vật chibi 3D (dựng bằng code)

- Đầu bếp chính, đầu bếp phụ, phục vụ và 8 kiểu khách là nhân vật 3D đầu to, viền đen kiểu hoạt hình (giống Eatventure), không cần file mô hình.
  - Đầu bếp chính: mũ cao, khăn cổ đỏ, ria mép, áo hai hàng cúc.
  - Đầu bếp phụ: mũ thấp, khăn và tạp dề cam.
  - Phục vụ: gi-lê xanh, nơ đỏ, tạp dề đen.
  - Khách: nón lá, mũ lưỡi trai, tóc búi, tóc dài, đeo kính…; ngồi ghế nhựa đỏ/xanh kiểu quán vỉa hè.
- Hoạt ảnh: đi (vung tay chân, nhún), đứng thở, đảo chảo khi nấu, hai tay đỡ khay khi bưng, ngồi và đưa đũa lên miệng khi ăn. Tự quay mặt theo hướng đi.
- File: `Visual/Chibi.cs` (dựng + hoạt ảnh), `Visual/ChibiLooks.cs` (trang phục, màu), `Visual/MeshKit.cs` (khối cầu/trụ/nón), `Resources/Shaders/ChibiToon.shader` (tô kiểu toon + viền).
- Nhân vật xen giữa các sprite 2D bằng sortingOrder 6 và độ sâu z = 4 + 0,25·y (người thấp hơn trên màn hình đứng trước).
- **Thay bằng mô hình thật:** đặt prefab vào `Assets/Resources/Models/characters/` với tên `head_chef`, `chef`, `waiter`, `customer_1` … `customer_8` (mặt quay +Z, chân ở gốc, cao ~2,3 đơn vị). Có Animator thì game tự đặt tham số `Speed`, `Cook`, `Carry`, `Sit`, `Eat` nếu có.
- Hình sprite `characters/*.png` cũ không còn được dùng.

### Đăng nhập & lưu trên mạng

- Unity Gaming Services: Authentication 3.8.0 + Cloud Save 3.4.2. Hướng dẫn liên kết và cách hoạt động: `online-setup.md`.
- Tự đăng nhập khách khi mở game; tạo tài khoản (tên + mật khẩu) để giữ tiến trình khi đổi máy; đăng nhập trên máy khác.
- Tự đẩy bản lưu lên mỗi 2 phút và khi ẩn app; bản trên mạng khác bản trên máy thì hiện hộp **Chọn bản lưu**.
- Nút "Âm" trên thanh tab đổi thành **Cài đặt** (âm thanh + tài khoản); hiện "(!)" khi còn là tài khoản khách.
- `GameState.totalPlaySeconds` mới để so sánh hai bản lưu.

---

## 4. Cách chạy

1. Mở project trong Unity Hub, chờ biên dịch xong (không có lỗi đỏ ở Console).
2. Lần đầu: **Phố Ẩm Thực → Tạo dữ liệu mẫu và màn chơi thử**.
   Khi số mẫu thay đổi: **Phố Ẩm Thực → Cập nhật số liệu mẫu (ghi đè)**.
3. Tab **Game** → đặt khung hình 1080 × 1920.
4. Bấm **Play**.

Chỉnh số: chọn file trong `Assets/Data/Stations` hoặc `Assets/Data/Cities`, sửa trong Inspector, rồi chạy **Mô phỏng nhịp chơi** để xem số phút thay đổi thế nào.

---

## 5. Việc sẽ làm tiếp

### Giai đoạn 2 – Chơi thử (đang làm)
- [ ] Chơi thử Hà Nội với số liệu mới, ghi lại đoạn nào chán / chờ lâu / tiền lên quá nhanh
- [ ] Chơi thử trang bị: rương có hấp dẫn không, nâng cấp có đủ bản trùng và đá quý không
- [ ] Chơi thử luật "nâng hết mới qua màn": đoạn cuối màn có bị chờ lâu không
- [ ] Chơi thử 5 mốc: cảm giác lên đời có đã không, thời lượng từng mốc có đúng ý không
- [ ] Chỉnh số lần 2 nếu cần
- [ ] Bật "món khách thích" (bán giá x2, đổi theo giờ trong ngày) rồi chỉnh lại nhịp

### Giai đoạn 3 – Cảnh quán thật
- [x] Chia màn hình: nửa trên cảnh quán, nửa dưới bảng điều khiển
- [x] Quầy theo mốc: khối màu + tên + cấp + thanh nấu; quầy chưa mở màu xám
- [x] Đầu bếp phụ hiện sau quầy đúng số đã thuê (1–4), nhún nhẹ khi nấu
- [x] Bàn theo mốc: 2 / 4 / 6 / 8 / 10 bàn; màu sàn và tường riêng từng mốc
- [x] Khách: vào từ cửa → tìm bàn trống → chờ → được mang món → ăn → "Ngon!" → về
- [x] Phục vụ hiện đúng số đã thuê (1–6), chạy từ quầy mang đĩa ra bàn, nhanh hơn khi có nhiều phục vụ
- [x] Tiền thật bay lên trên quầy khi quản lý tự thu ("+1.2K"); quầy chưa có quản lý hiện "Thu ..."
- [x] Chạm vào quầy: thu tiền đang chờ (tiền bay lên), viền vàng quanh quầy được chọn, bảng dưới hiện bảng nâng cấp riêng của quầy đó (nút "Đóng" để quay lại); chạm chỗ trống để bỏ chọn. Dùng Input System mới, chạy cả chuột lẫn cảm ứng
- [x] Game tự nhận hình vẽ theo tên file trong `Assets/Resources/Sprites/` (có hình thì dùng, chưa có thì dùng khối màu tạm); ảnh thả vào tự được nhập đúng kiểu Sprite
- [ ] Tạo hình bằng AI theo `art-prompts.md` (8 nhân vật, quầy/bàn/sàn/tường cho 5 mốc, 19 món ăn, đĩa)
- [x] Hiệu ứng lên đời: quán cũ thu nhỏ và tối dần → bảng "LÊN ĐỜI!" (hoặc "LÊN XE ĐI HẠ LONG!") bật ra kèm pháo giấy → quán mới phóng to nảy ra (~2,6 giây). Nút "Qua mốc ngay (chỉ để thử)" để xem nhanh

### Giai đoạn 4 – Giao diện thật
- [x] Giao diện uGUI dựng bằng code (`UI/UIKit.cs`, `UI/GameUI.cs`), chữ tiếng Việt bằng font hệ thống, khung bo tròn tự tạo
- [x] Thanh thông tin trên cùng: thành phố · mốc, tên quán, tiền (viên vàng), đá quý (viên xanh)
- [x] Nút nổi "Tăng tốc x2" ở góc cảnh quán, hiện thời gian còn lại
- [x] Thẻ tiến độ lên đời: % tổng cấp, 4 chỉ số (quầy max, đầu bếp, phục vụ, quản lý); đủ điều kiện thì hiện nút đỏ "LÊN ĐỜI: …!" nhấp nháy
- [x] Chế độ mua x1 / x10 / TỐI ĐA, dòng thuê phục vụ
- [x] Bỏ bảng điều khiển dưới: cảnh quán chiếm toàn màn hình (camera tự giãn theo màn dọc, bàn xếp nhiều hàng); giao diện chỉ nằm đè ở mép (thanh thông tin + dải tiến độ ở trên; nút phục vụ, tăng tốc, LÊN ĐỜI và thanh tab ở dưới)
- [x] Chạm quầy → ô nâng cấp nhỏ hiện ngay trên quầy (thiếu chỗ thì hiện bên dưới), có mũi nhọn chỉ vào quầy: tên, cấp + thanh cấp, thu nhập/giây, mốc x2 kế tiếp; nút Nâng tự thành +10 / +5 khi đủ tiền (không thì +1), +Đầu bếp, Quản lý/Thu; quầy chưa mở có nút "Mở quầy"; chạm chỗ trống để đóng
- [x] Tab Đầu bếp / Nhiệm vụ mở trang phủ lên cảnh
- [x] Mô hình quán mới (thay đầu bếp theo quầy): mỗi mốc bắt đầu chỉ có **đầu bếp chính** (vừa nấu vừa bưng) và 1 bàn. Mỗi bàn gọi một phần mọi món đang bán → đầu bếp (ai cũng nấu được mọi món) nấu từng món → đủ món thì phục vụ bưng cả khay → khách nhận món mới trả tiền → ăn xong, khách mới vào
- [x] Bảng "Nâng cấp quán" (nút dưới-trái): thuê đầu bếp (tối đa 2 người/quầy), thuê phục vụ (1 người/quầy), mua bàn (3/4/6/8/10 bàn theo mốc), 6 cải tiến tốc độ (phục vụ x1,5 ×3, bếp x1,25 ×3). Giá rải đều suốt mốc (EconomyService.PriceAtProgress). Qua mốc phải mua đủ người và bàn
- [x] Cảnh: đầu bếp chính (to, áo vàng), đầu bếp, phục vụ đi theo đúng việc; khách đi vào từng bàn, bong bóng "x/y món", "Chờ bưng!"; bàn chưa mua vẽ mờ; tiền bay lên ở bàn
- [x] Mô phỏng (scratchpad simpool.py, người chơi mua món rẻ nhất): Hà Nội 3,0 / 5,0 / 12,5 / 17,5 / 17,5 phút ≈ 55 phút; hệ số giá món theo mốc 9,4 / 6,0 / 4,3 / 2,5 / 2,1
- [x] Bỏ quản lý: tiền tự cộng vào túi ngay khi khách nhận khay (không còn chạm quầy để thu); qua mốc không cần quản lý; tiền offline tính cho mọi quầy; ô nâng cấp quầy chỉ còn nút Nâng
- [x] Sao món ăn (`Core/DishStars.cs`): mỗi mốc x2 giá món (cấp 10, 25, 50, 75, 100, 150, 200, 250) món lên 1 sao + thưởng 1 ♦; 3 ô sao dưới mỗi quầy, qua 3 sao thì đổi màu (xanh lục → lam → vàng → tím → đỏ); hiệu ứng sao lớn bung ra + vòng sáng + chữ "Món lên sao! Giá x2 · +1 ♦" (≈ 125 ♦ mỗi thành phố)
- [x] Nâng cấp món: mỗi quầy 2 bậc "Công thức gia truyền" và "Nguyên liệu thượng hạng", mỗi bậc giá món x2 (tối đa x4); nằm trong bảng "Nâng cấp quán" (danh sách cuộn: Người và bàn → Nâng cấp món → Cải tiến tốc độ), mỗi quầy một hàng; qua mốc phải mua hết nâng cấp món và cải tiến (ô "Nâng cấp" trên dải tiến độ). Mô phỏng lại: hệ số giá món 3,8 / 2,26 / 1,44 / 0,86 / 0,75, đồ đầu mốc rẻ hơn (từ 3 lần giá cấp 1) → vẫn 3 / 5 / 12,5 / 17,5 / 17,5 phút
- [ ] Công cụ "Mô phỏng nhịp chơi" trong Unity vẫn dùng mô hình cũ, cần viết lại theo mô hình bàn + đầu bếp chung
- [x] Thanh tab dưới cùng: Quán · Đầu bếp chính · Âm: Bật/Tắt
- [x] Nút nảy nhẹ khi bấm (tự viết, không cần DOTween)
- [x] Âm thanh tạo bằng code (`Audio/SoundFX.cs`): bấm, "ting" thu tiền, nâng cấp, thuê người, lên đời, mở rương; bật/tắt được lưu lại
- [x] Tab Đầu bếp chính bằng uGUI (`UI/ChefPage.cs`): tổng hiệu ứng, 4 ô trang bị (bấm để lọc), số mảnh từng vị trí, mua rương, lọc kho, thẻ món tô màu theo độ hiếm; nút Trang bị / Nâng cấp (mảnh) / Phân rã (đồ Sử thi, Huyền thoại phải bấm 2 lần)
- [x] Popup uGUI (`UI/PopupLayer.cs`): "Chào mừng trở lại" (nhận / xem QC nhận gấp đôi) và mở rương (liệt kê món theo màu độ hiếm), nền tối, khung bật ra, có âm thanh
- [x] Bỏ hẳn giao diện chữ cũ (PrototypeUI chỉ còn làm dự phòng); nút thử "qua mốc" và "xóa save" chuyển xuống góc dưới-trái cảnh quán
- [ ] Font tiếng Việt đẹp (TextMeshPro + font Việt hóa, ví dụ Be Vietnam Pro / Nunito)
- [ ] Nhạc nền nhạc cụ dân tộc, âm thanh thật thay âm thanh tạo bằng code

### Giai đoạn 5 – Giữ chân
- [~] Đầu bếp phụ sưu tầm cho từng quầy: bỏ, vì đầu bếp giờ dùng chung, nấu mọi món (có thể làm lại thành "đầu bếp có tài riêng" sau)
- [x] Hiệu ứng mở rương: từng món lật ra cách nhau 0,4 giây (chữ phóng to rồi thu về), món Sử thi / Huyền thoại có tiếng riêng và quầng sáng màu độ hiếm nhấp nháy sau khung; nhận nhiều rương cùng lúc thì gộp vào một popup
- [ ] Hình ảnh trang bị (cần vẽ bằng AI)
- [x] Nhiệm vụ ngày (`Core/QuestService.cs`, tab **Nhiệm vụ**): 3 nhiệm vụ mỗi ngày, làm mới lúc 0 giờ, chọn từ 6 loại (nâng cấp quầy, thuê nhân viên, phục vụ khách, mở quầy, lên đời, phân rã), mức dễ thưởng 20 ♦, mức khó 40 ♦
- [x] Quà điểm danh 7 ngày: 20 / 30 / 40 / 50 / 60 / 80 ♦, ngày 7 rương 3 món; vòng lặp lại sau 7 lần nhận; tab hiện "(!)" khi có quà chờ nhận
- [x] Thêm thành phố 3–10 (Huế, Hội An, Đà Nẵng, Nha Trang, Đà Lạt, Sài Gòn, Cần Thơ, Phú Quốc), mỗi thành phố 5 mốc với tên quán và món đặc sản riêng; số tiền mỗi thành phố ×100.000, giá tăng nhẹ dần (hệ số 1,12 → 1,26)
- [x] Sự kiện theo mùa (`Core/EventService.cs`, thẻ sự kiện ở tab **Nhiệm vụ**): Tết Nguyên Đán (10 ngày trước → 6 ngày sau Tết; 6/2/2027, 26/1/2028), Tết Trung Thu (7 ngày trước → 1 ngày sau; 25/9/2026, 15/9/2027, 3/10/2028), Quốc khánh (30/8–4/9), Giáng sinh (20–27/12)
  - Trong sự kiện: mỗi lần bưng món ra bàn có 30% khách để lại vật phẩm (bao lì xì / đèn lồng / cờ / chuông bạc), khách trả thêm 20%, tường bếp treo dòng chúc mừng và dây đèn lồng
  - 6 mốc quà: 30 → 30 ♦, 100 → rương 1 món, 250 → 80 ♦, 500 → rương 2 món, 800 → 150 ♦, 1200 → rương sự kiện 3 món (chắc chắn 1 món Sử thi trở lên); tiến độ làm lại mỗi lần sự kiện
  - Ngoài sự kiện: thẻ hiện sự kiện sắp tới và số ngày còn lại; nút "Thử: sự kiện" bật thử Tết → Trung Thu → Quốc khánh → Giáng sinh → theo lịch
- [x] Quán sự kiện riêng (`Core/EventVenueFactory.cs`): trong sự kiện có nút "vào quán sự kiện" (góc phải dưới dải tiến độ); quán riêng 5 món theo mùa (Tết: bánh chưng, giò lụa...; Trung Thu: bánh nướng, bánh dẻo...; Quốc khánh; Giáng sinh), tiền / người / bàn / nâng cấp riêng, qua lại thành phố bất cứ lúc nào (tiến độ hai bên được cất giữ); quầy lên tới cấp 1000 với 15 mốc x2 = đủ 5 màu sao (sao tím, đỏ chỉ có ở đây); nâng hết nhận 100 ♦ + rương sự kiện 3 món (Sử thi+); hết sự kiện thì tự về thành phố. Mô phỏng: nâng hết ≈ 2,5 giờ chơi
- [x] Đồ Tối thượng (`Core/UltimateItems.cs`, độ hiếm thứ 5, màu hồng đỏ): 4 sự kiện × 4 vị trí = 16 món (Tết: Dao Long Phụng, Nồi Đồng Ngũ Phúc, Tạp dề Hoa Mai Vàng, Mũ Thần Tài; Trung Thu: Dao Cung Trăng, Nồi Chị Hằng, Tạp dề Đèn Ông Sao, Mũ Chú Cuội; Quốc khánh: Dao Sao Vàng, Nồi Độc Lập, Tạp dề Cờ Hồng, Mũ Hòa Bình; Giáng sinh: Dao Băng Tuyết, Nồi Tuần Lộc, Tạp dề Ông Già Noel, Mũ Giáng Sinh); chỉ số ×1,5 Huyền thoại, cấp tối đa 50; bản vẽ chỉ rơi từ rương sự kiện (mốc 1200 vật phẩm + hoàn thành quán sự kiện, mỗi rương 1 bản vẽ chưa có; đủ 4 rồi thì +100 ♦); ghép = bản vẽ + 2 Huyền thoại + 2 Sử thi + 3 Hiếm (không đang mặc, vị trí nào cũng được, trả lại 80% mảnh đã bỏ vào); "Xưởng đồ Tối thượng" ở tab Đầu bếp; trần chỉ số trang bị nâng lên (lợi nhuận +250%, nấu nhanh / giảm giá 45%, offline +300%) – đồ Huyền thoại không đổi
- [x] Phân rã nhanh (tab Đầu bếp, nút cạnh dòng đếm kho): chọn một hoặc nhiều độ hiếm (nhớ lựa chọn lần trước), xem trước số món và số mảnh từng vị trí, món đang mặc luôn giữ lại; chọn Sử thi trở lên phải bấm xác nhận 2 lần
- [x] **Tip**: khách ăn xong đứng dậy có tỉ lệ (gốc 6%, nâng bằng đá quý ở Cửa hàng: thẻ "Khách hào phóng" mỗi cấp +2%, 47 cấp từ 6% lên 100%, giá tăng cấp số nhân 10 ♦ × 1,16^cấp (10, 12, 13… → cấp cuối 9.250 ♦; tổng ≈ 67.000 ♦; mốc: 26% ≈ 210 ♦, 46% ≈ 1.150 ♦, 66% ≈ 5.300 ♦, 86% ≈ 23.600 ♦); GameState.tipLevel giữ qua mọi quán) để lại tip = 50% tiền bữa vừa trả (bữa đắt hơn khi quầy được nâng cấp → tip to dần). Đồng xu vàng hiện trên mặt bàn, nhấp nhô, nảy khi có tip mới; chưa nhận thì tip cộng dồn. Chạm vào xu → "Tip +X" bay lên, cộng vào túi (chạm xu được ưu tiên, không đổi quầy đang chọn). Không tính vào thu nhập offline; tip chưa nhận mất khi lên đời / tắt game. Hằng số: RestaurantController.TipChance, TipFraction
- [x] Skill "Món best seller": chạm quầy đã mở → nút Best seller. Trong 3 phút, 95% khách mới vào chỉ gọi đúng món đó (5% còn lại gọi combo như thường). Hết giờ hồi chiêu 10 phút. Hủy bất cứ lúc nào (bấm 2 lần trong 2 giây; hồi chiêu tính từ lúc hủy) ở popup quầy hoặc ô giữa hàng nút dưới. Quầy đang chạy có bảng "BÁN CHẠY m:ss" trên đầu đầu bếp. Thời gian tính theo giờ thực (vẫn trôi khi tắt game); chỉ áp dụng ở quán chứa quầy đó. GameState: bestSellerStation, bestSellerEndUnix, bestSellerReadyUnix
- [x] Nâng nhanh (tab Đầu bếp, cạnh nút Phân rã nhanh): nâng 4 món đang mặc hết mức số mảnh của từng vị trí; nút hiện trước tổng số cấp sẽ lên, bấm xong hiện kết quả từng món (Dao +3 · Mũ +1…). Đồ trong kho không bị đụng tới
- [x] Hệ trang bị mới (`Core/GearBook.cs`): hiệu ứng cố định, nâng cấp chỉ tăng lợi nhuận. Thường/Hiếm/Sử thi chỉ buff đầu bếp chính (dao: món hoàn hảo, nồi: nấu nhanh, mũ: chạy nhanh; tạp dề giảm giá). Món hoàn hảo bán x6. 17 món Huyền thoại (5 dao, 4 nồi, 4 tạp dề, 4 mũ – 13 món mới dựng trong code) mang hiệu ứng nhỏ của đồ Tối thượng (mọi đầu bếp / đầu bếp chính hoàn hảo, nấu nhanh, chạy nhanh, tạp dề giảm giá), lợi nhuận +8% → +20%. Tối thượng: mỗi set sự kiện chia khác nhau, đủ 4 món = 100% món hoàn hảo, nấu nhanh +500%, chạy nhanh +300%, đầu bếp chính 100%; tạp dề giảm giá 30–45%. Ghép Tối thượng cần 2 Huyền thoại chỉ định (bảng `UltimateItems.Recipes`) + 2 Sử thi + 3 Hiếm. Mũ bỏ tiền offline và kéo dài tăng tốc
- [x] Mô phỏng (`gearsim5.py`): không đồ 55 phút / thành phố; Sử thi 30: 30; Huyền thoại 40: ~14–15; 2 món Tối thượng: 8,5; đủ set Tết cấp 50: 2,5 phút (quán sự kiện 4,4 phút)
- [x] Quà tân thủ: bộ 4 món Sử thi cấp 1 tặng khi bắt đầu chơi (người đang chơi dở cũng nhận 1 lần), tự mặc, hiện popup mở quà; Hà Nội ~55 phút → ~36 phút (mốc 1 còn ~2 phút)
- [ ] Thêm lịch Tết / Trung Thu cho các năm sau 2028

### Giai đoạn 6 – Kiếm tiền và đo lường
- [x] Hệ thống quảng cáo thưởng (`Monetization/AdsService.cs`): giới hạn mỗi ngày theo vị trí (tăng tốc 12, đá quý 5, rương 3), gói Bỏ quảng cáo nhận thưởng ngay, đang chạy quảng cáo giả lập (đếm ngược 3 giây)
- [ ] Gắn Unity LevelPlay thật (xem `monetization-setup.md`)
- [x] Cửa hàng (tab **Cửa hàng**, `Monetization/StoreService.cs`, `UI/ShopPage.cs`): 20 ♦ và rương 1 món xem quảng cáo, thẻ tháng (300 ♦ + 50 ♦/ngày × 30 ngày), gói khởi đầu 49k, bỏ quảng cáo 79k, 3 gói đá quý, rương nhỏ 2 món 30 ♦ (Thường 52% / Hiếm 35% / Sử thi 13%, không Huyền thoại) và rương lớn 6 món 80 ♦ (50 / 32 / 13 / Huyền thoại 5% mỗi món ≈ 26% cả rương), bảng tỉ lệ hiện ngay trên thẻ (`Core/ChestTypes.cs`); trang cửa hàng cuộn được, lợi nhuận x5 trong 5 phút giá 80 ♦ (mua tiếp cộng dồn, thanh trên cùng hiện thời gian còn lại); mua giả lập có hỏi xác nhận; xóa save vẫn giữ đồ đã mua
- [ ] Gắn Unity IAP + tạo sản phẩm trên Google Play Console (xem `monetization-setup.md`)
- [x] Đo lường (`Analytics/AnalyticsService.cs`): first_open, session_start (ngày thứ mấy → D1/D7), qua mốc / thành phố (số phút), rương, nhiệm vụ, quà sự kiện, quảng cáo, mua hàng; ghi Console + `analytics.csv`; có sẵn nhánh gửi Firebase khi thêm define `FIREBASE_ANALYTICS`
- [ ] Gắn Firebase thật (xem `monetization-setup.md`)
- [ ] Giờ máy chủ cho thu nhập offline (chống chỉnh đồng hồ)

### Giai đoạn 7 – Phát hành thử
- [x] Icon app 1024×1024 và ảnh bìa Google Play 1024×500 (`Assets/Art/Icon/`, vẽ bằng code, có thể thay bằng bản vẽ AI sau)
- [x] Menu **Phố Ẩm Thực → Phát hành** (`Editor/ReleaseSetup.cs`): áp cài đặt Android (tên "Phố Ẩm Thực Việt", mã `com.phoamthuc.phoamthucviet`, màn dọc, IL2CPP + ARM64, API mới nhất, màn hình chờ đỏ son, icon), build AAB (tự tăng số phiên bản), build APK để cài thử
- [x] Bản phát hành tự ẩn công cụ thử (`UI/DebugTools.cs`)
- [x] `release-checklist.md` (cài module Android, tạo keystore, Google Play Console, thử kín, chữ trang cửa hàng) và `privacy-policy.md` (bản nháp, cần điền tên + email rồi đăng lên web)
- [ ] Cài Android Build Support, tạo keystore, build AAB đầu tiên
- [ ] Tạo tài khoản Google Play Console (25 USD), tạo app, điền Data safety / phân loại nội dung, đăng chính sách quyền riêng tư
- [ ] Thử nghiệm kín 20–50 người, đo chỉ số, chỉnh lại

---

## 6. Ghi chú

- Mô phỏng giả định người chơi luôn mua ngay lựa chọn tốt nhất và đã có quản lý. Người chơi thật sẽ chậm hơn một chút ở vài phút đầu vì phải bấm "Thu tiền".
- `PrototypeUI` chỉ để kiểm chứng vòng chơi; sẽ bỏ khi có giao diện thật. `RestaurantController` và `Scripts/Core` giữ nguyên.
- Đổi `id` của quầy hoặc thành phố sau khi phát hành sẽ làm mất tiến trình trong save của người chơi.
