using System.Collections.Generic;
using System.IO;
using PhoAmThucViet.Core;
using PhoAmThucViet.Gameplay;
using PhoAmThucViet.UI;
using PhoAmThucViet.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PhoAmThucViet.EditorTools
{
    /// <summary>
    /// Một lệnh tạo dữ liệu mẫu (Hà Nội, Hạ Long, mỗi nơi 5 mốc từ xe đẩy tới nhà hàng lớn)
    /// và đặt màn chơi thử vào scene đang mở.
    /// Mọi quầy trong một mốc chung cấp tối đa: 25 / 50 / 150 / 250 / 250. Số liệu đã mô phỏng
    /// (qua mốc = nâng hết, chưa có trang bị, nhân đôi lợi nhuận ở cấp 10/25/50/75/100/150/200/250):
    /// Hà Nội khoảng 3,2 / 4,7 / 13,4 / 15,8 / 16,1 phút, tổng khoảng 53 phút; chờ lâu nhất khoảng 25 giây.
    /// Lệnh "Tạo..." KHÔNG ghi đè dữ liệu đã có (bạn chỉnh số trong Inspector thoải mái);
    /// lệnh "Cập nhật số liệu mẫu" thì ghi đè số liệu về bản mẫu mới nhất.
    /// </summary>
    public static class PrototypeSetup
    {
        private const string StationsDir = "Assets/Data/Stations";
        private const string VenuesDir = "Assets/Data/Venues";
        private const string CitiesDir = "Assets/Data/Cities";
        private const string EquipmentDir = "Assets/Data/Equipment";
        private const string CatalogPath = "Assets/Data/equipment-catalog.asset";

        private static bool _overwrite;

        [MenuItem("Phố Ẩm Thực/Cập nhật số liệu mẫu (ghi đè)")]
        public static void UpdateNumbers()
        {
            if (!EditorUtility.DisplayDialog("Cập nhật số liệu mẫu",
                    "Ghi đè toàn bộ số liệu mẫu (thành phố, 5 mốc, quầy, nhân viên, mốc sao, trang bị) về bản mới nhất? " +
                    "Số bạn đã tự chỉnh trong Inspector sẽ mất. File lưu game cũng sẽ bị xóa.", "Ghi đè", "Hủy"))
                return;
            _overwrite = true;
            try { Setup(); }
            finally { _overwrite = false; }
            SaveService.Delete();
        }

        // ---------- Khung số liệu chung cho mọi mốc ----------
        // Quầy thứ i trong một mốc dùng hàng i của bảng: giá mở, giá nâng gốc, lợi nhuận gốc, thời gian nấu.
        // Quầy đầu luôn rẻ (mua được sau ~6 giây); các quầy sau và phục vụ nhân hệ số k của mốc.
        private static readonly double[] LadderUnlock = { 0, 1_000, 10_000, 125_000, 500_000, 3_000_000 };
        private static readonly double[] LadderCost = { 5, 300, 3_500, 42_500, 500_000, 3_000_000 };
        private static readonly double[] LadderProfit = { 1, 6, 36, 216, 1_300, 7_800 };
        private static readonly float[] LadderCook = { 1.0f, 1.4f, 1.8f, 2.2f, 2.6f, 3.0f };

        /// <summary>
        /// Thông số của 5 mốc. Mọi quầy trong cùng một mốc có chung cấp tối đa và chung hệ số tăng giá.
        /// Mốc càng lớn càng nhiều cấp; giá tăng chậm hơn mỗi cấp để lợi nhuận theo kịp.
        /// </summary>
        private struct VenueSpec
        {
            public int stations; public int maxLevel; public float growth; public double k;
            public VenueSpec(int n, int maxLevel, float growth, double k)
            { stations = n; this.maxLevel = maxLevel; this.growth = growth; this.k = k; }
        }

        private static readonly VenueSpec[] Specs =
        {
            //            quầy  cấp tối đa  tăng giá  hệ số k     mô phỏng Hà Nội (chưa trang bị)
            new VenueSpec(2,    25,         1.16f,    1.5),    // ~3,2 phút
            new VenueSpec(3,    50,         1.085f,   1.5),    // ~4,7 phút
            new VenueSpec(4,    150,        1.042f,   3.5),    // ~13,4 phút
            new VenueSpec(5,    250,        1.032f,   1.7),    // ~15,8 phút
            new VenueSpec(6,    250,        1.03f,    1.7),    // ~16,1 phút
        };

        /// <summary>Hệ số hiển thị của từng mốc: số tiền to dần cho cảm giác lớn mạnh (không đổi thời gian chơi).</summary>
        private static readonly double[] VenueDisplayMultiplier = { 1, 10, 100, 1_000, 10_000 };

        /// <summary>Số món trong rương khi hoàn thành từng mốc.</summary>
        private static readonly int[] ChestItems = { 1, 1, 2, 2, 3 };

        [MenuItem("Phố Ẩm Thực/Tạo dữ liệu mẫu và màn chơi thử")]
        public static void Setup()
        {
            EnsureFolder(StationsDir);
            EnsureFolder(VenuesDir);
            EnsureFolder(CitiesDir);
            EnsureFolder(EquipmentDir);

            var haNoi = City("ha-noi", "Hà Nội", "hn", 1, 1.0,
                new[] { "Xe đẩy phở", "Quán cóc vỉa hè", "Quán phở ngõ nhỏ", "Quán phở mặt phố", "Nhà hàng phở Hà Thành" },
                new[]
                {
                    new[] { "Phở bò", "Trà đá" },
                    new[] { "Phở bò", "Trà đá", "Quẩy nóng" },
                    new[] { "Phở bò", "Trà chanh", "Phở gà", "Quẩy nóng" },
                    new[] { "Phở bò", "Trà chanh", "Phở gà", "Bún chả", "Nem rán" },
                    new[] { "Phở bò tái lăn", "Trà sen", "Phở gà", "Bún chả", "Nem rán", "Chả cá Lã Vọng" },
                });

            // Hạ Long: giá cao hơn Hà Nội 1,1 lần (dài hơn một chút), số tiền lớn hơn 100.000 lần.
            var haLong = City("ha-long", "Hạ Long", "hl", 100_000, 1.1,
                new[] { "Xe đẩy mực nướng", "Quán cóc bờ biển", "Quán hải sản bến tàu", "Nhà hàng ven vịnh", "Nhà hàng du thuyền" },
                new[]
                {
                    new[] { "Mực nướng", "Bia hơi" },
                    new[] { "Mực nướng", "Bia hơi", "Ốc luộc" },
                    new[] { "Chả mực", "Bia hơi", "Ốc luộc", "Sá sùng" },
                    new[] { "Chả mực", "Bia hơi", "Sá sùng", "Ngán hấp", "Tôm hấp" },
                    new[] { "Chả mực", "Bia hơi", "Sá sùng", "Ngán hấp", "Tôm hùm", "Cua bể" },
                });

            // Các thành phố tiếp theo dọc Việt Nam. Số tiền mỗi thành phố lớn hơn thành phố trước 100.000 lần;
            // giá tăng nhẹ dần (hệ số costScale) để mỗi thành phố dài hơn chút: ~53 → ~65 phút khi chưa có trang bị.
            var hue = City("hue", "Huế", "hu", 1e10, 1.12,
                new[] { "Gánh bún bò", "Quán cóc bên sông Hương", "Quán bún bò Đông Ba", "Quán cơm hến mặt phố", "Nhà hàng cung đình Huế" },
                new[]
                {
                    new[] { "Bún bò Huế", "Nước sâm" },
                    new[] { "Bún bò Huế", "Nước sâm", "Bánh bèo" },
                    new[] { "Bún bò Huế", "Chè Huế", "Bánh bèo", "Cơm hến" },
                    new[] { "Bún bò Huế", "Chè Huế", "Bánh bèo", "Cơm hến", "Bánh khoái" },
                    new[] { "Bún bò giò heo", "Trà cung đình", "Bánh bèo", "Cơm hến", "Bánh khoái", "Nem lụi" },
                });
            var hoiAn = City("hoi-an", "Hội An", "ha", 1e15, 1.14,
                new[] { "Xe bánh mì phố cổ", "Quán cóc bên sông Hoài", "Quán cao lầu ngõ nhỏ", "Quán mì Quảng đèn lồng", "Nhà hàng phố Hội" },
                new[]
                {
                    new[] { "Bánh mì", "Nước mót" },
                    new[] { "Bánh mì", "Nước mót", "Bánh bao bánh vạc" },
                    new[] { "Cao lầu", "Nước mót", "Bánh bao bánh vạc", "Bánh mì" },
                    new[] { "Cao lầu", "Nước mót", "Bánh bao bánh vạc", "Mì Quảng", "Cơm gà" },
                    new[] { "Cao lầu", "Chè bắp", "Bánh bao bánh vạc", "Mì Quảng", "Cơm gà", "Hoành thánh chiên" },
                });
            var daNang = City("da-nang", "Đà Nẵng", "dn", 1e20, 1.16,
                new[] { "Xe bánh tráng cuốn", "Quán cóc bãi biển", "Quán bún chả cá", "Quán hải sản Mỹ Khê", "Nhà hàng bên cầu Rồng" },
                new[]
                {
                    new[] { "Bánh tráng cuốn thịt heo", "Nước mía" },
                    new[] { "Bánh tráng cuốn thịt heo", "Nước mía", "Bánh xèo" },
                    new[] { "Bún chả cá", "Nước mía", "Bánh xèo", "Bánh tráng cuốn thịt heo" },
                    new[] { "Bún chả cá", "Nước dừa", "Bánh xèo", "Mì Quảng", "Ốc hút" },
                    new[] { "Bún chả cá", "Nước dừa", "Bánh xèo", "Mì Quảng", "Ghẹ hấp", "Tôm nướng" },
                });
            var nhaTrang = City("nha-trang", "Nha Trang", "nt", 1e25, 1.18,
                new[] { "Xe nem nướng", "Quán cóc bên biển", "Quán bún cá sứa", "Quán hải sản Hòn Chồng", "Nhà hàng du thuyền vịnh" },
                new[]
                {
                    new[] { "Nem nướng", "Nước dừa" },
                    new[] { "Nem nướng", "Nước dừa", "Bánh căn" },
                    new[] { "Bún cá sứa", "Nước dừa", "Bánh căn", "Nem nướng" },
                    new[] { "Bún cá sứa", "Sinh tố bơ", "Bánh căn", "Nem nướng", "Mực một nắng" },
                    new[] { "Bún cá sứa", "Sinh tố bơ", "Bánh căn", "Nem nướng", "Tôm hùm nướng", "Cua hấp" },
                });
            var daLat = City("da-lat", "Đà Lạt", "dl", 1e30, 1.20,
                new[] { "Xe bánh tráng nướng", "Gánh chợ đêm", "Quán lẩu gà lá é", "Quán nướng đồi thông", "Nhà hàng biệt thự Đà Lạt" },
                new[]
                {
                    new[] { "Bánh tráng nướng", "Sữa đậu nành" },
                    new[] { "Bánh tráng nướng", "Sữa đậu nành", "Bánh ướt lòng gà" },
                    new[] { "Lẩu gà lá é", "Sữa đậu nành", "Bánh ướt lòng gà", "Bánh tráng nướng" },
                    new[] { "Lẩu gà lá é", "Trà atiso", "Bánh ướt lòng gà", "Bánh tráng nướng", "Xiên nướng" },
                    new[] { "Lẩu gà lá é", "Trà atiso", "Bánh ướt lòng gà", "Bánh tráng nướng", "Xiên nướng", "Dâu tây dầm" },
                });
            var saiGon = City("sai-gon", "Sài Gòn", "sg", 1e35, 1.22,
                new[] { "Xe hủ tiếu gõ", "Quán cóc vỉa hè Sài Gòn", "Quán cơm tấm trong hẻm", "Quán ăn đêm Quận 1", "Nhà hàng rooftop Sài Gòn" },
                new[]
                {
                    new[] { "Hủ tiếu gõ", "Cà phê sữa đá" },
                    new[] { "Hủ tiếu gõ", "Cà phê sữa đá", "Bánh tráng trộn" },
                    new[] { "Cơm tấm", "Cà phê sữa đá", "Bánh tráng trộn", "Hủ tiếu gõ" },
                    new[] { "Cơm tấm", "Cà phê sữa đá", "Bánh tráng trộn", "Hủ tiếu Nam Vang", "Bánh xèo miền Nam" },
                    new[] { "Cơm tấm sườn bì chả", "Cà phê muối", "Bánh tráng trộn", "Hủ tiếu Nam Vang", "Bánh xèo miền Nam", "Lẩu mắm" },
                });
            var canTho = City("can-tho", "Cần Thơ", "ct", 1e40, 1.24,
                new[] { "Ghe bánh mì chợ nổi", "Quán cóc bến Ninh Kiều", "Quán bún riêu", "Quán lẩu mắm miệt vườn", "Nhà hàng sông nước Cần Thơ" },
                new[]
                {
                    new[] { "Bánh mì", "Nước mía" },
                    new[] { "Bánh mì", "Nước mía", "Bánh cống" },
                    new[] { "Bún riêu", "Nước mía", "Bánh cống", "Bánh mì" },
                    new[] { "Bún riêu", "Trái cây dĩa", "Bánh cống", "Lẩu mắm", "Cá lóc nướng trui" },
                    new[] { "Bún riêu", "Trái cây dĩa", "Bánh cống", "Lẩu mắm", "Cá lóc nướng trui", "Vịt nấu chao" },
                });
            var phuQuoc = City("phu-quoc", "Phú Quốc", "pq", 1e45, 1.26,
                new[] { "Xe gỏi cá trích", "Quán cóc chợ đêm", "Quán bún kèn", "Quán hải sản Hàm Ninh", "Nhà hàng resort Phú Quốc" },
                new[]
                {
                    new[] { "Gỏi cá trích", "Nước dừa" },
                    new[] { "Gỏi cá trích", "Nước dừa", "Bánh canh ghẹ" },
                    new[] { "Bún kèn", "Nước dừa", "Bánh canh ghẹ", "Gỏi cá trích" },
                    new[] { "Bún kèn", "Rượu sim", "Bánh canh ghẹ", "Gỏi cá trích", "Nhum nướng mỡ hành" },
                    new[] { "Bún kèn", "Rượu sim", "Bánh canh ghẹ", "Gỏi cá trích", "Nhum nướng mỡ hành", "Tôm hùm nướng" },
                });

            // Nối hành trình: Hà Nội → Hạ Long → Huế → Hội An → Đà Nẵng → Nha Trang → Đà Lạt → Sài Gòn → Cần Thơ → Phú Quốc
            var journey = new[] { haNoi, haLong, hue, hoiAn, daNang, nhaTrang, daLat, saiGon, canTho, phuQuoc };
            for (int i = 0; i < journey.Length - 1; i++)
            {
                if (journey[i].nextCity == null || _overwrite)
                {
                    journey[i].nextCity = journey[i + 1];
                    EditorUtility.SetDirty(journey[i]);
                }
            }

            var catalog = CreateEquipment();
            AssetDatabase.SaveAssets();

            PlaceInScene(haNoi, journey, catalog);

            Debug.Log("[Phố Ẩm Thực] Đã tạo dữ liệu mẫu (10 thành phố × 5 mốc) và màn chơi thử. Bấm Play để chơi. " +
                      "Mở menu Phố Ẩm Thực → Mô phỏng nhịp chơi để kiểm tra số phút từng mốc.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static CityData City(string id, string name, string prefix, double cityMultiplier, double costScale,
                                     string[] venueNames, string[][] stationNames)
        {
            string path = $"{CitiesDir}/{id}.asset";
            var c = AssetDatabase.LoadAssetAtPath<CityData>(path);
            if (c != null && !_overwrite && c.venues.Count > 0) return c;

            bool isNew = c == null;
            if (isNew) c = ScriptableObject.CreateInstance<CityData>();
            c.id = id;
            c.displayName = name;
            c.cityCompleteGems = 50;
            c.venues.Clear();
            for (int v = 0; v < Specs.Length; v++)
                c.venues.Add(Venue($"{prefix}-{v + 1}", venueNames[v], prefix, v, cityMultiplier, costScale, stationNames[v]));

            if (isNew) AssetDatabase.CreateAsset(c, path);
            else EditorUtility.SetDirty(c);
            return c;
        }

        private static VenueData Venue(string id, string name, string prefix, int index, double cityMultiplier,
                                       double costScale, string[] stationNames)
        {
            var spec = Specs[index];
            string path = $"{VenuesDir}/{id}.asset";
            var v = AssetDatabase.LoadAssetAtPath<VenueData>(path);
            bool isNew = v == null;
            if (isNew) v = ScriptableObject.CreateInstance<VenueData>();
            else if (!_overwrite) return v;

            v.id = id;
            v.displayName = name;
            v.multiplier = cityMultiplier * VenueDisplayMultiplier[index];
            v.waiterSpeedBonus = 0.2f;
            v.requireAllMaxed = true;
            v.requireAllManagers = false; // đã bỏ quản lý: tiền tự cộng khi khách nhận món
            // Rương khi xong mốc: mốc 1–2 một món, mốc 3–4 hai món, mốc 5 ba món.
            v.chestItems = ChestItems[index];
            v.dishValueOverride = 0; // dùng hệ số giá món chuẩn theo mốc (EconomyService.DefaultDishValue)

            v.stations.Clear();
            v.favoriteStations.Clear();
            v.waiterCosts.Clear();
            for (int i = 0; i < spec.stations; i++)
            {
                double k = (i == 0 ? 1 : spec.k) * costScale;
                v.stations.Add(Station($"{id}-{i + 1}", stationNames[i],
                    LadderUnlock[i] * k, i == 0 ? 20 * costScale : LadderUnlock[i] * k * 1.5,
                    LadderCost[i] * k, LadderProfit[i], LadderCook[i], spec.growth, spec.maxLevel));
                if (i > 0) v.waiterCosts.Add(LadderUnlock[i] * spec.k * costScale);
            }

            if (isNew) AssetDatabase.CreateAsset(v, path);
            else EditorUtility.SetDirty(v);
            return v;
        }

        private static StationData Station(string id, string name, double unlock, double manager,
                                           double baseCost, double baseProfit, float cookTime,
                                           float costGrowth, int maxLevel)
        {
            string path = $"{StationsDir}/{id}.asset";
            var s = AssetDatabase.LoadAssetAtPath<StationData>(path);
            bool isNew = s == null;
            if (isNew) s = ScriptableObject.CreateInstance<StationData>();
            s.id = id;
            s.displayName = name;
            s.unlockCost = unlock;
            s.managerCost = manager;
            s.baseCost = baseCost;
            s.costGrowth = costGrowth;
            s.maxLevel = maxLevel;
            s.maxChefs = 4;
            s.chefCostMultiplier = 3;
            s.baseProfit = baseProfit;
            s.cookTime = cookTime;
            s.doubleProfitLevels = new[] { 10, 25, 50, 75, 100, 150, 200, 250 };
            if (isNew) AssetDatabase.CreateAsset(s, path);
            else EditorUtility.SetDirty(s);
            return s;
        }

        // ---------- Trang bị đầu bếp ----------
        // Hiệu ứng (cấp 1 → mỗi cấp thêm). Huyền thoại mạnh vượt trội: cấp 40 đạt +160% lợi nhuận, 35% nấu nhanh, 35% giảm giá.

        private static EquipmentCatalog CreateEquipment()
        {
            var items = new[]
            {
                // Dao: lợi nhuận
                Equip("dao-thai-thit", "Dao thái thịt", EquipSlot.Dao, Rarity.Thuong, 0.05f, 0.005f),
                Equip("dao-da-sy", "Dao Đa Sỹ", EquipSlot.Dao, Rarity.Hiem, 0.10f, 0.0075f),
                Equip("dao-pho-gia-truyen", "Dao phở gia truyền", EquipSlot.Dao, Rarity.SuThi, 0.18f, 0.009f),
                Equip("dao-vang-ngu-thien", "Dao vàng ngự thiện", EquipSlot.Dao, Rarity.HuyenThoai, 0.60f, 0.025f),
                // Nồi: tốc độ nấu
                Equip("noi-nhom", "Nồi nhôm", EquipSlot.Noi, Rarity.Thuong, 0.02f, 0.002f),
                Equip("chao-gang", "Chảo gang", EquipSlot.Noi, Rarity.Hiem, 0.03f, 0.0025f),
                Equip("noi-nuoc-dung", "Nồi nước dùng 100 lít", EquipSlot.Noi, Rarity.SuThi, 0.05f, 0.003f),
                Equip("noi-dong-cung-dinh", "Nồi đồng cung đình", EquipSlot.Noi, Rarity.HuyenThoai, 0.15f, 0.005f),
                // Tạp dề: giảm giá
                Equip("tap-de-vai", "Tạp dề vải", EquipSlot.TapDe, Rarity.Thuong, 0.02f, 0.002f),
                Equip("tap-de-bo", "Tạp dề bò", EquipSlot.TapDe, Rarity.Hiem, 0.03f, 0.0025f),
                Equip("tap-de-lua-ha-dong", "Tạp dề lụa Hà Đông", EquipSlot.TapDe, Rarity.SuThi, 0.05f, 0.003f),
                Equip("tap-de-theu-rong", "Tạp dề thêu rồng", EquipSlot.TapDe, Rarity.HuyenThoai, 0.15f, 0.005f),
                // Mũ: offline và thời gian tăng tốc
                Equip("mu-vai", "Mũ vải", EquipSlot.Mu, Rarity.Thuong, 0.10f, 0.02f),
                Equip("non-la", "Nón lá", EquipSlot.Mu, Rarity.Hiem, 0.20f, 0.02f),
                Equip("mu-dau-bep-trang", "Mũ đầu bếp trắng", EquipSlot.Mu, Rarity.SuThi, 0.30f, 0.02f),
                Equip("khan-xep-ngu-thien", "Khăn xếp ngự thiện", EquipSlot.Mu, Rarity.HuyenThoai, 1.00f, 0.025f),
            };

            var catalog = AssetDatabase.LoadAssetAtPath<EquipmentCatalog>(CatalogPath);
            bool isNew = catalog == null;
            if (isNew) catalog = ScriptableObject.CreateInstance<EquipmentCatalog>();
            if (isNew || _overwrite)
            {
                catalog.items.Clear();
                catalog.items.AddRange(items);
                // Tỷ lệ độ hiếm (Thường, Hiếm, Sử thi, Huyền thoại): Huyền thoại 4,5%.
                catalog.dropWeights = new[] { 50f, 33f, 12.5f, 4.5f };
                catalog.gemChestPrice = 100;
                catalog.gemChestItems = 2;
                // Mảnh: phân rã món cấp 1 cho 10 / 25 / 60 / 200 mảnh; nâng từ cấp n tốn (1 / 2 / 3 / 5) × n mảnh.
                catalog.dismantleBase = new[] { 10, 25, 60, 200 };
                catalog.dismantleRefund = 0.8f;
                catalog.levelUpCostPerLevel = new[] { 1, 2, 3, 5 };
                if (isNew) AssetDatabase.CreateAsset(catalog, CatalogPath);
                else EditorUtility.SetDirty(catalog);
            }
            return catalog;
        }

        private static EquipmentData Equip(string id, string name, EquipSlot slot, Rarity rarity, float baseBonus, float perLevel)
        {
            string path = $"{EquipmentDir}/{id}.asset";
            var e = AssetDatabase.LoadAssetAtPath<EquipmentData>(path);
            if (e != null && !_overwrite) return e;

            bool isNew = e == null;
            if (isNew) e = ScriptableObject.CreateInstance<EquipmentData>();
            e.id = id;
            e.displayName = name;
            e.slot = slot;
            e.rarity = rarity;
            e.baseBonus = baseBonus;
            e.bonusPerLevel = perLevel;
            if (isNew) AssetDatabase.CreateAsset(e, path);
            else EditorUtility.SetDirty(e);
            return e;
        }

        private static void PlaceInScene(CityData start, CityData[] all, EquipmentCatalog catalog)
        {
            var controller = Object.FindFirstObjectByType<RestaurantController>();
            GameObject go;
            if (controller == null)
            {
                go = new GameObject("Restaurant (prototype)");
                Undo.RegisterCreatedObjectUndo(go, "Tạo màn chơi thử");
                controller = go.AddComponent<RestaurantController>();
            }
            else go = controller.gameObject;

            if (go.GetComponent<PrototypeUI>() == null) go.AddComponent<PrototypeUI>();
            if (go.GetComponent<RestaurantView>() == null) go.AddComponent<RestaurantView>();
            controller.startCity = start;
            controller.allCities = all;
            controller.equipmentCatalog = catalog;
            EditorUtility.SetDirty(controller);

            var scene = go.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = go;
        }

        [MenuItem("Phố Ẩm Thực/Xóa file lưu game")]
        public static void DeleteSave()
        {
            SaveService.Delete();
            Debug.Log("[Phố Ẩm Thực] Đã xóa file lưu: " + SaveService.SavePath);
        }
    }
}
