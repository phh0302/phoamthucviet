using System;
using System.Collections.Generic;

namespace PhoAmThucViet.Core
{
    /// <summary>Trạng thái một quầy trong lượt chơi hiện tại.</summary>
    [Serializable]
    public class StationState
    {
        public string id;
        public int level;
        public bool hasManager;
        /// <summary>Bản cũ (đầu bếp theo quầy), không còn dùng.</summary>
        public int chefs;
        /// <summary>Tiến độ món đang nấu, 0–1.</summary>
        public float cookProgress;
        /// <summary>Tiền khách đã trả nhưng chưa thu (khi chưa có quản lý).</summary>
        public BigNum pending;
        /// <summary>Số món của quầy này đã nấu xong, đang chờ đủ khay để bưng ra (chỉ để hiển thị).</summary>
        public int ready;

        public StationState Clone() => (StationState)MemberwiseClone();
    }

    /// <summary>
    /// Một "lượt chơi" quán: thành phố + mốc, tiền, quầy, người làm, bàn, nâng cấp.
    /// Dùng để cất lượt đang không chơi khi qua lại giữa thành phố và quán sự kiện.
    /// </summary>
    [Serializable]
    public class RunSnapshot
    {
        public string cityId;
        public int venueIndex;
        public float secondsInVenue, secondsInCity;
        public BigNum money, earnedInCity;
        public long customersServed;
        public int waiters, chefs, tables, upgradesBought, recipesBought;
        public List<StationState> stations = new List<StationState>();
    }

    /// <summary>Một món trang bị trong kho. Mỗi lần rơi ra là một món riêng (uid riêng).</summary>
    [Serializable]
    public class OwnedEquipment
    {
        /// <summary>Mã riêng của món này trong kho.</summary>
        public string uid;
        /// <summary>Loại trang bị (EquipmentData.id).</summary>
        public string id;
        public int level = 1;
        /// <summary>Tổng mảnh đã bỏ vào để nâng cấp (phân rã trả lại phần lớn).</summary>
        public int shardsSpent;
    }

    /// <summary>Một nhiệm vụ ngày.</summary>
    [Serializable]
    public class QuestState
    {
        public QuestType type;
        public long target;
        public long progress;
        public int reward;
        public bool claimed;
    }

    /// <summary>Toàn bộ dữ liệu được lưu xuống máy.</summary>
    [Serializable]
    public class GameState
    {
        public int version = 1;
        /// <summary>Tổng thời gian đã chơi (giây), để so sánh bản lưu trên máy và trên mạng.</summary>
        public double totalPlaySeconds;

        public string cityId;
        /// <summary>Mốc đang chơi trong thành phố (0 = xe đẩy).</summary>
        public int venueIndex;
        public float secondsInVenue;
        public BigNum money;
        /// <summary>Tổng tiền đã thu ở mốc hiện tại.</summary>
        public BigNum earnedInCity;
        public long customersServed;
        public float secondsInCity;
        /// <summary>Số phục vụ đã thuê (chưa tính đầu bếp chính, người vừa nấu vừa bưng).</summary>
        public int waiters;
        /// <summary>Số đầu bếp đã thuê, nấu được mọi món (chưa tính đầu bếp chính).</summary>
        public int chefs;
        /// <summary>Số bàn của quán (mốc nào cũng bắt đầu với 1 bàn).</summary>
        public int tables = 1;
        /// <summary>Các cải tiến đã mua trong mốc này (bit i = VenueUpgrades.All[i]).</summary>
        public int upgradesBought;
        /// <summary>Nâng cấp món đã mua trong mốc này (bit quầy × 2 + bậc).</summary>
        public int recipesBought;
        public List<StationState> stations = new List<StationState>();

        // Tiến trình lâu dài, không reset khi sang thành phố mới.
        public long gems;
        public int citiesCompleted;
        /// <summary>Vòng chơi: 0 = vòng 1. Xong thành phố cuối (Phú Quốc) thì quay về Hà Nội, sang vòng sau, mọi giá đắt hơn.</summary>
        public int loop;
        /// <summary>Bước hướng dẫn tân thủ đang làm (Gameplay/Onboarding). Hết bước = xong.</summary>
        public int tutorialStep;
        /// <summary>Đã gặp khách sộp đầu tiên (xuất hiện sớm để người mới thấy tip).</summary>
        public bool firstVipDone;

        // Quán sự kiện: hạng quán đã đạt và tổng cấp cao nhất (tính điểm xếp hạng), theo mã lần sự kiện
        public string eventRankKey, eventBestKey;
        public int eventRank, eventBestLevels;

        // Mượn đầu bếp (RestaurantController.Helper): đầu bếp đang giúp, hết giờ lúc nào (unix giây),
        // đã xem quảng cáo kéo dài chưa, số lượt đã mượn trong ngày.
        public HelperCard helper;
        public long helperEndUnix;
        public bool helperExtended;
        public string helperDay;
        public int helperBorrowsToday;

        // Đầu bếp chính: kho trang bị, uid món đang dùng ở 4 ô (Dao, Nồi, Tạp dề, Mũ),
        // và số mảnh của từng vị trí (mảnh dao, mảnh nồi, mảnh tạp dề, mảnh mũ).
        public List<OwnedEquipment> equipment = new List<OwnedEquipment>();
        public string[] equipped = new string[4];
        public int[] shards = new int[4];
        public int nextEquipmentUid = 1;
        /// <summary>Bản vẽ đồ Tối thượng đang có (id món), nhận từ rương sự kiện.</summary>
        public List<string> blueprints = new List<string>();

        // Nhiệm vụ ngày và điểm danh (tiến trình lâu dài)
        // Sự kiện theo mùa: lần sự kiện đang tính ("tet-2027"), số vật phẩm đã gom, mốc quà đã nhận (bit)
        public string eventKey;
        public int eventTokens;
        public int eventClaimed;

        // Quảng cáo và cửa hàng
        public string adDay;                              // ngày của adLog
        public List<string> adLog = new List<string>();   // các vị trí quảng cáo đã xem hôm nay
        public long adsWatchedTotal;
        public bool noAds;
        /// <summary>Đã tặng bộ Sử thi tân thủ chưa.</summary>
        public bool starterGearGiven;
        /// <summary>Số giây còn lại của gói "lợi nhuận x5" (mua bằng đá quý).</summary>
        public float profitBoostSeconds;
        /// <summary>Cấp nâng "Khách hào phóng" (tỉ lệ tip), mua bằng đá quý, giữ qua mọi quán.</summary>
        public int tipLevel;
        /// <summary>Cấp nâng quảng cáo bằng đá quý: giá trị thưởng / thời lượng tăng tốc. Giữ qua mọi quán.</summary>
        public int adValueLevel, adDurationLevel;
        /// <summary>Skill "Món best seller": mã quầy đang chạy, lúc hết hiệu ứng, lúc hết hồi chiêu (giờ Unix, giây).</summary>
        public string bestSellerStation = "";
        public long bestSellerEndUnix, bestSellerReadyUnix;
        public bool starterPackBought;
        public int monthlyCardDays;                       // số ngày thẻ tháng còn nhận
        public string monthlyLastClaim;

        // Quán sự kiện: đang chơi quán sự kiện không, lượt đang cất (thành phố khi ở quán sự kiện và ngược lại),
        // quán sự kiện thuộc lần sự kiện nào, đã hoàn thành chưa.
        public bool inEventRun;
        public bool hasParkedRun;
        public RunSnapshot parkedRun = new RunSnapshot();
        public string eventRunKey;
        public bool eventRunCompleted;

        /// <summary>Chụp lại lượt đang chơi.</summary>
        public RunSnapshot TakeRun()
        {
            var r = new RunSnapshot
            {
                cityId = cityId, venueIndex = venueIndex, secondsInVenue = secondsInVenue, secondsInCity = secondsInCity,
                money = money, earnedInCity = earnedInCity, customersServed = customersServed,
                waiters = waiters, chefs = chefs, tables = tables, upgradesBought = upgradesBought, recipesBought = recipesBought,
            };
            foreach (var s in stations) r.stations.Add(s.Clone());
            return r;
        }

        /// <summary>Lấy lại một lượt đã cất.</summary>
        public void LoadRun(RunSnapshot r)
        {
            cityId = r.cityId; venueIndex = r.venueIndex; secondsInVenue = r.secondsInVenue; secondsInCity = r.secondsInCity;
            money = r.money; earnedInCity = r.earnedInCity; customersServed = r.customersServed;
            waiters = r.waiters; chefs = r.chefs; tables = Math.Max(1, r.tables);
            upgradesBought = r.upgradesBought; recipesBought = r.recipesBought;
            stations.Clear();
            foreach (var s in r.stations) stations.Add(s.Clone());
        }

        public string questDay;
        public List<QuestState> quests = new List<QuestState>();
        /// <summary>Tổng số lần đã nhận quà điểm danh (ngày thứ loginDays % 7 + 1 trong vòng 7 ngày).</summary>
        public int loginDays;
        public string lastLoginClaimDay;

        /// <summary>Thời điểm lưu gần nhất (Unix giây, UTC).</summary>
        public long lastSaveUnix;

        public StationState GetStation(string id)
        {
            foreach (var s in stations) if (s.id == id) return s;
            return null;
        }

        /// <summary>Tìm món trong kho theo uid.</summary>
        public OwnedEquipment GetEquipment(string uid)
        {
            if (string.IsNullOrEmpty(uid) || equipment == null) return null;
            foreach (var e in equipment) if (e.uid == uid) return e;
            return null;
        }

        public string NewEquipmentUid() => "e" + (nextEquipmentUid++);

        /// <summary>Save cũ có thể thiếu mảng ô trang bị, mảng mảnh, hoặc uid.</summary>
        public void EnsureEquippedSlots()
        {
            if (equipment == null) equipment = new List<OwnedEquipment>();
            if (nextEquipmentUid < 1) nextEquipmentUid = 1;
            foreach (var e in equipment) if (string.IsNullOrEmpty(e.uid)) e.uid = NewEquipmentUid();
            if (shards == null || shards.Length != 4)
            {
                var fixedShards = new int[4];
                if (shards != null) Array.Copy(shards, fixedShards, Math.Min(4, shards.Length));
                shards = fixedShards;
            }
            if (equipped == null || equipped.Length != 4)
            {
                var fixedSlots = new string[4];
                if (equipped != null) Array.Copy(equipped, fixedSlots, Math.Min(4, equipped.Length));
                equipped = fixedSlots;
            }
        }

        public StationState GetOrCreateStation(string id)
        {
            var s = GetStation(id);
            if (s != null) return s;
            s = new StationState { id = id };
            stations.Add(s);
            return s;
        }

        /// <summary>Bắt đầu thành phố mới từ mốc đầu tiên (xe đẩy).</summary>
        public void StartCity(CityData city)
        {
            cityId = city.id;
            secondsInCity = 0;
            venueIndex = 0;
            StartVenue(city.venues[0]);
        }

        /// <summary>Bắt đầu một mốc: tiền, quầy, nhân viên reset; giữ đá quý, trang bị và tiến trình lâu dài.</summary>
        public void StartVenue(VenueData venue)
        {
            money = BigNum.Zero;
            earnedInCity = BigNum.Zero;
            customersServed = 0;
            secondsInVenue = 0;
            waiters = 0;
            chefs = 0;
            tables = 1;
            upgradesBought = 0;
            recipesBought = 0;
            stations.Clear();
            for (int i = 0; i < venue.stations.Count; i++)
            {
                var st = GetOrCreateStation(venue.stations[i].id);
                // Quầy đầu tiên mở sẵn ở cấp 1.
                st.level = i == 0 ? 1 : 0;
                st.chefs = 0;
                st.ready = 0;
                st.pending = BigNum.Zero;
            }
        }
    }
}
