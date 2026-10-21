# Phố Ẩm Thực Việt – Bộ câu lệnh tạo hình bằng AI

Game tự nhận hình theo **tên file**. Hình nào chưa có thì game vẫn dùng khối màu tạm, nên bạn làm dần từng hình được.

## 1. Quy trình cho mỗi hình

1. **Tạo hình** bằng một công cụ AI miễn phí, ví dụ:
   - Bing Image Creator (bing.com/create) – dùng DALL·E, miễn phí với tài khoản Microsoft
   - Google Gemini hoặc ImageFX (labs.google/fx) – miễn phí với tài khoản Google
   - Leonardo.ai – có lượt miễn phí mỗi ngày
2. **Dán câu lệnh**: luôn dán **Phong cách chung** (mục 2) rồi tới câu lệnh riêng của hình đó. Dùng cùng một công cụ cho cả bộ để các hình đồng bộ.
3. **Xóa nền** (trừ sàn và tường): remove.bg, Adobe Express "Remove background" hoặc Photopea (photopea.com, miễn phí).
4. **Cắt sát và lưu PNG**: cắt bỏ khoảng trống thừa quanh hình, lưu `.png` nền trong suốt. Khoảng 512 px là đủ, không cần lớn hơn 1024 px.
5. **Đặt tên đúng** (chữ thường, không dấu) và thả vào đúng thư mục trong `Assets/Resources/Sprites/` (mục 3). Unity tự nhập đúng kiểu, bấm Play là thấy.

Mẹo:
- Muốn các hình giống nhau hơn: tạo 1 hình ưng ý trước (ví dụ đầu bếp), rồi ở các hình sau thêm câu "same art style as the previous image".
- Hình ra có chữ, viền khung hay bóng đổ thì thêm vào cuối câu lệnh: "no text, no frame, no drop shadow".

## 2. Phong cách chung (dán đầu mọi câu lệnh)

```
Cute 2D mobile game asset, chibi cartoon style, thick clean dark-brown outlines, soft cel shading,
bright warm colors, Vietnamese street food theme, slightly top-down 3/4 view, single object centered,
isolated on a plain white background, no text, no frame, no drop shadow.
```

Riêng **sàn** và **tường** dùng câu này thay cho câu trên:

```
2D mobile game background texture, cute cartoon style, flat top-down view, soft warm colors,
Vietnamese street food theme, seamless and evenly lit, no objects, no people, no text.
```

## 3. Danh sách hình

### Nhân vật – thư mục `Assets/Resources/Sprites/characters/`

| Tên file | Câu lệnh riêng |
| --- | --- |
| `chef.png` | A cheerful Vietnamese street food cook, full body, white chef hat, white apron over a simple shirt, holding a ladle, standing pose |
| `head_chef.png` | The head chef and restaurant owner: a confident Vietnamese chef, full body, tall white chef hat with a gold band, golden-yellow chef jacket, red neckerchief, arms crossed with a proud smile, standing pose (bigger and more distinctive than the regular cooks) |
| `waiter.png` | A friendly young Vietnamese waiter, full body, blue polo shirt and black apron, carrying an empty round tray, walking pose |
| `customer_1.png` | A Vietnamese office worker customer, full body, white shirt and tie, standing pose |
| `customer_2.png` | A Vietnamese student customer, full body, white school-style shirt and backpack, standing pose |
| `customer_3.png` | An elderly Vietnamese grandmother customer, full body, traditional áo bà ba and conical hat nón lá, standing pose |
| `customer_4.png` | A tourist customer, full body, sun hat, camera around the neck, colorful t-shirt, standing pose |
| `customer_5.png` | A Vietnamese motorbike delivery driver customer, full body, green jacket and helmet, standing pose |
| `customer_6.png` | A young Vietnamese woman customer in a modern áo dài, full body, standing pose |

### Quán theo mốc – thư mục `Assets/Resources/Sprites/venues/`

Số `_1` … `_5` là mốc 1 (xe đẩy) … mốc 5 (nhà hàng lớn). Thành phố nào cũng dùng chung bộ này.

| Tên file | Câu lệnh riêng |
| --- | --- |
| `counter_1.png` | A small wooden street food push cart with a big steaming soup pot, wide horizontal shape |
| `counter_2.png` | A low sidewalk food stall counter with plastic stools look, red plastic and stainless steel, wide horizontal shape |
| `counter_3.png` | A small alley shop wooden cooking counter with pots and herbs, wide horizontal shape |
| `counter_4.png` | A clean street-front restaurant counter, stainless steel top and yellow tiled front, wide horizontal shape |
| `counter_5.png` | An elegant restaurant kitchen counter, dark lacquered wood with gold trim, wide horizontal shape |
| `table_1.png` | A tiny folding table with two low red plastic stools, seen slightly from above |
| `table_2.png` | A low stainless steel sidewalk table with blue plastic stools, seen slightly from above |
| `table_3.png` | A small wooden table with two wooden stools, seen slightly from above |
| `table_4.png` | A square restaurant table with a checkered tablecloth and two chairs, seen slightly from above |
| `table_5.png` | A round lacquered wooden table with red cloth runner and two carved chairs, seen slightly from above |
| `floor_1.png` | Grey concrete sidewalk with a few hexagonal paving bricks *(dùng câu phong cách sàn/tường, không xóa nền)* |
| `floor_2.png` | Old patterned Vietnamese cement floor tiles, beige and terracotta *(như trên)* |
| `floor_3.png` | Warm wooden floor planks *(như trên)* |
| `floor_4.png` | Classic Vietnamese encaustic tiles with yellow and white flower pattern *(như trên)* |
| `floor_5.png` | Luxurious red carpet with subtle golden oriental pattern *(như trên)* |
| `wall_1.png` | Night street wall with a hanging string of warm light bulbs, wide horizontal *(như trên)* |
| `wall_2.png` | Yellow painted old Hanoi wall with a small hand-painted menu board, wide horizontal *(như trên)* |
| `wall_3.png` | Warm yellow alley wall with hanging herbs and a small altar shelf, wide horizontal *(như trên)* |
| `wall_4.png` | Shop front wall with green wooden shutters and a striped awning, wide horizontal *(như trên)* |
| `wall_5.png` | Elegant restaurant wall with red lanterns and carved wooden panels, wide horizontal *(như trên)* |

### Món ăn – thư mục `Assets/Resources/Sprites/food/`

Tên file là **tên món bỏ dấu, nối bằng gạch ngang** (game tự đổi "Phở bò" thành `pho-bo`). Mẫu câu lệnh: `A bowl/plate of <món>, appetizing, game food icon`.

| Tên file | Món | Câu lệnh riêng |
| --- | --- | --- |
| `pho-bo.png` | Phở bò | A bowl of Vietnamese beef pho with rice noodles, sliced beef, herbs and lime, game food icon |
| `pho-bo-tai-lan.png` | Phở bò tái lăn | A bowl of Vietnamese pho with stir-fried beef and garlic, game food icon |
| `pho-ga.png` | Phở gà | A bowl of Vietnamese chicken pho with shredded chicken and herbs, game food icon |
| `tra-da.png` | Trà đá | A glass of Vietnamese iced green tea, game drink icon |
| `tra-chanh.png` | Trà chanh | A tall glass of Vietnamese lime iced tea with a lime slice, game drink icon |
| `tra-sen.png` | Trà sen | A small ceramic cup of Vietnamese lotus tea with a lotus flower, game drink icon |
| `quay-nong.png` | Quẩy nóng | A plate of Vietnamese fried breadsticks quẩy, golden and crispy, game food icon |
| `bun-cha.png` | Bún chả | Vietnamese bún chả: grilled pork patties in dipping sauce with rice noodles and herbs, game food icon |
| `nem-ran.png` | Nem rán | A plate of crispy Vietnamese fried spring rolls nem rán with herbs, game food icon |
| `cha-ca-la-vong.png` | Chả cá Lã Vọng | A sizzling pan of Hanoi turmeric fish with dill and spring onions, game food icon |
| `muc-nuong.png` | Mực nướng | Grilled squid skewers, glossy and charred, game food icon |
| `bia-hoi.png` | Bia hơi | A glass mug of Vietnamese fresh draft beer with foam, game drink icon |
| `oc-luoc.png` | Ốc luộc | A bowl of boiled sea snails with lemongrass and dipping sauce, game food icon |
| `cha-muc.png` | Chả mực | A plate of golden Ha Long squid cakes, game food icon |
| `sa-sung.png` | Sá sùng | A plate of stir-fried sá sùng (peanut worms) with garlic, game food icon |
| `ngan-hap.png` | Ngán hấp | A plate of steamed ngán clams with lemongrass and herbs, game food icon |
| `tom-hap.png` | Tôm hấp | A plate of steamed pink shrimp with lime, game food icon |
| `tom-hum.png` | Tôm hùm | A grilled lobster on a plate, game food icon |
| `cua-be.png` | Cua bể | A steamed red sea crab on a plate, game food icon |

### Đồ vật – thư mục `Assets/Resources/Sprites/props/`

| Tên file | Câu lệnh riêng |
| --- | --- |
| `plate.png` | A small round white plate with a steaming bowl of noodles on it, game icon |

## 4. Thứ tự nên làm

1. `characters/chef`, `characters/waiter`, 2–3 `customer_*`: thấy quán "sống" ngay.
2. `venues/counter_1`, `table_1`, `floor_1`, `wall_1` và `food/pho-bo`, `food/tra-da`: đủ cho mốc 1 (Xe đẩy phở).
3. Các mốc 2–5 và các món còn lại.

Khi thêm thành phố hoặc món mới, chỉ cần tạo thêm file `food/<tên-mon-khong-dau>.png`.

### Món ăn của 8 thành phố mới – thư mục `Assets/Resources/Sprites/food/`

Dùng mẫu câu: `A bowl/plate/glass of <tên món tiếng Việt> (Vietnamese <tên món tiếng Anh nếu biết>), appetizing, game food icon`. Món trùng tên với thành phố trước (ví dụ Bánh mì, Nước dừa) dùng chung một hình.

| Thành phố | Tên file | Món |
| --- | --- | --- |
| Huế | `bun-bo-hue.png` | Bún bò Huế |
| Huế | `nuoc-sam.png` | Nước sâm |
| Huế | `banh-beo.png` | Bánh bèo |
| Huế | `che-hue.png` | Chè Huế |
| Huế | `com-hen.png` | Cơm hến |
| Huế | `banh-khoai.png` | Bánh khoái |
| Huế | `bun-bo-gio-heo.png` | Bún bò giò heo |
| Huế | `tra-cung-dinh.png` | Trà cung đình |
| Huế | `nem-lui.png` | Nem lụi |
| Hội An | `banh-mi.png` | Bánh mì |
| Hội An | `nuoc-mot.png` | Nước mót |
| Hội An | `banh-bao-banh-vac.png` | Bánh bao bánh vạc |
| Hội An | `cao-lau.png` | Cao lầu |
| Hội An | `mi-quang.png` | Mì Quảng |
| Hội An | `com-ga.png` | Cơm gà |
| Hội An | `che-bap.png` | Chè bắp |
| Hội An | `hoanh-thanh-chien.png` | Hoành thánh chiên |
| Đà Nẵng | `banh-trang-cuon-thit-heo.png` | Bánh tráng cuốn thịt heo |
| Đà Nẵng | `nuoc-mia.png` | Nước mía |
| Đà Nẵng | `banh-xeo.png` | Bánh xèo |
| Đà Nẵng | `bun-cha-ca.png` | Bún chả cá |
| Đà Nẵng | `nuoc-dua.png` | Nước dừa |
| Đà Nẵng | `oc-hut.png` | Ốc hút |
| Đà Nẵng | `ghe-hap.png` | Ghẹ hấp |
| Đà Nẵng | `tom-nuong.png` | Tôm nướng |
| Nha Trang | `nem-nuong.png` | Nem nướng |
| Nha Trang | `banh-can.png` | Bánh căn |
| Nha Trang | `bun-ca-sua.png` | Bún cá sứa |
| Nha Trang | `sinh-to-bo.png` | Sinh tố bơ |
| Nha Trang | `muc-mot-nang.png` | Mực một nắng |
| Nha Trang | `tom-hum-nuong.png` | Tôm hùm nướng |
| Nha Trang | `cua-hap.png` | Cua hấp |
| Đà Lạt | `banh-trang-nuong.png` | Bánh tráng nướng |
| Đà Lạt | `sua-dau-nanh.png` | Sữa đậu nành |
| Đà Lạt | `banh-uot-long-ga.png` | Bánh ướt lòng gà |
| Đà Lạt | `lau-ga-la-e.png` | Lẩu gà lá é |
| Đà Lạt | `tra-atiso.png` | Trà atiso |
| Đà Lạt | `xien-nuong.png` | Xiên nướng |
| Đà Lạt | `dau-tay-dam.png` | Dâu tây dầm |
| Sài Gòn | `hu-tieu-go.png` | Hủ tiếu gõ |
| Sài Gòn | `ca-phe-sua-da.png` | Cà phê sữa đá |
| Sài Gòn | `banh-trang-tron.png` | Bánh tráng trộn |
| Sài Gòn | `com-tam.png` | Cơm tấm |
| Sài Gòn | `hu-tieu-nam-vang.png` | Hủ tiếu Nam Vang |
| Sài Gòn | `banh-xeo-mien-nam.png` | Bánh xèo miền Nam |
| Sài Gòn | `com-tam-suon-bi-cha.png` | Cơm tấm sườn bì chả |
| Sài Gòn | `ca-phe-muoi.png` | Cà phê muối |
| Sài Gòn | `lau-mam.png` | Lẩu mắm |
| Cần Thơ | `banh-cong.png` | Bánh cống |
| Cần Thơ | `bun-rieu.png` | Bún riêu |
| Cần Thơ | `trai-cay-dia.png` | Trái cây dĩa |
| Cần Thơ | `ca-loc-nuong-trui.png` | Cá lóc nướng trui |
| Cần Thơ | `vit-nau-chao.png` | Vịt nấu chao |
| Phú Quốc | `goi-ca-trich.png` | Gỏi cá trích |
| Phú Quốc | `banh-canh-ghe.png` | Bánh canh ghẹ |
| Phú Quốc | `bun-ken.png` | Bún kèn |
| Phú Quốc | `ruou-sim.png` | Rượu sim |
| Phú Quốc | `nhum-nuong-mo-hanh.png` | Nhum nướng mỡ hành |
