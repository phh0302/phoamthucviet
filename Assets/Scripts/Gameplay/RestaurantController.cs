using System;
using System.Collections.Generic;
using PhoAmThucViet.Analytics;
using PhoAmThucViet.Core;
using UnityEngine;

namespace PhoAmThucViet.Gameplay
{
    /// <summary>
    /// Bộ điều khiển nhà hàng: chạy vòng nấu món, thu tiền, nâng cấp, nhân viên, qua mốc và thành phố,
    /// trang bị đầu bếp chính, lưu game và thu nhập offline.
    /// Không phụ thuộc giao diện; UI chỉ đọc và gọi hàm.
    /// </summary>
    public partial class RestaurantController : MonoBehaviour
    {
        [Tooltip("Thành phố bắt đầu (thường là Hà Nội).")]
        public CityData startCity;

        [Tooltip("Tất cả thành phố, để tìm lại thành phố đang chơi khi mở save.")]
        public CityData[] allCities;

        [Tooltip("Danh mục trang bị của đầu bếp chính.")]
        public EquipmentCatalog equipmentCatalog;

        [Tooltip("Tự lưu mỗi bao nhiêu giây.")]
        public float autoSaveInterval = 10f;

        public GameState State { get; private set; }
        public CityData City { get; private set; }

        /// <summary>Mốc đang chơi: xe đẩy → ... → nhà hàng lớn (hoặc quán sự kiện khi đang ở đó).</summary>
        public VenueData Venue => InEventRun && EventVenue != null
            ? EventVenue
            : City.venues[Mathf.Clamp(State.venueIndex, 0, City.venues.Count - 1)];
        public int VenueNumber => State.venueIndex + 1;
        public int VenueCount => City.venues.Count;
        public bool IsLastVenue => InEventRun || State.venueIndex >= City.venues.Count - 1;
        public VenueData NextVenue => IsLastVenue ? null : City.venues[State.venueIndex + 1];

        // ---------- Quán sự kiện ----------

        /// <summary>Đang chơi quán sự kiện (tiền, quầy, người làm riêng; quầy lên tới cấp 1000).</summary>
        public bool InEventRun => State != null && State.inEventRun;

        /// <summary>Tăng mỗi lần qua lại giữa thành phố và quán sự kiện (cảnh quán dựng lại ngay, không hiệu ứng lên đời).</summary>
        public int RunVersion { get; private set; }

        private VenueData _eventVenue;
        private string _eventVenueKey;

        /// <summary>Quán sự kiện của lần sự kiện đang diễn ra (null nếu không có sự kiện).</summary>
        public VenueData EventVenue
        {
            get
            {
                var e = EventService.Current;
                string key = e != null ? e.Key : State?.eventRunKey;
                if (e == null && !InEventRun) return null;
                if (_eventVenue == null || _eventVenueKey != key)
                {
                    if (e == null) return null;
                    _eventVenue = EventVenueFactory.Create(e);
                    _eventVenueKey = key;
                }
                return _eventVenue;
            }
        }

        public bool EventRunCompleted => CurrentEvent != null && State.eventRunCompleted && State.eventRunKey == CurrentEvent.Key;

        public bool CanEnterEventRun => !InEventRun && CurrentEvent != null && !EventRunCompleted;

        /// <summary>Vào quán sự kiện: cất lượt thành phố, lấy lại quán sự kiện đang chơi dở (hoặc mở quán mới).</summary>
        public void EnterEventRun()
        {
            var e = CurrentEvent;
            if (!CanEnterEventRun) return;
            string mainCity = State.cityId;
            var main = State.TakeRun();
            bool resume = State.hasParkedRun && State.eventRunKey == e.Key;
            if (resume) State.LoadRun(State.parkedRun);
            else
            {
                State.eventRunKey = e.Key;
                State.eventRunCompleted = false;
                State.inEventRun = true; // để Venue trả về quán sự kiện khi dựng quầy
                State.venueIndex = 4;    // giao diện nhà hàng lớn
                State.StartVenue(EventVenue);
            }
            State.cityId = mainCity;
            State.parkedRun = main;
            State.hasParkedRun = true;
            State.inEventRun = true;
            AfterRunSwitch();
            AnalyticsService.Log("event_run_enter", ("event", e.Key), ("resume", resume ? 1 : 0));
        }

        /// <summary>Về thành phố: cất quán sự kiện (chơi tiếp được khi sự kiện còn).</summary>
        public void LeaveEventRun(bool discard = false)
        {
            if (!InEventRun) return;
            var eventRun = State.TakeRun();
            if (State.hasParkedRun) State.LoadRun(State.parkedRun);
            State.parkedRun = discard ? new RunSnapshot() : eventRun;
            State.hasParkedRun = !discard;
            State.inEventRun = false;
            AfterRunSwitch();
            AnalyticsService.Log("event_run_leave", ("discard", discard ? 1 : 0));
        }

        private void AfterRunSwitch()
        {
            City = FindCity(State.cityId) ?? City;
            PendingOffline = BigNum.Zero;
            _completedNotified = false;
            ResetRun();
            EnsureStations();
            RunVersion++;
            SaveService.Save(State);
        }

        /// <summary>Hết sự kiện khi đang ở quán sự kiện: tự về thành phố.</summary>
        private void CheckEventRun()
        {
            if (!InEventRun) return;
            var e = EventService.Current;
            if (e == null || e.Key != State.eventRunKey) LeaveEventRun(discard: true);
        }

        private float _eventCheckTimer;

        /// <summary>Tổng hiệu ứng từ trang bị đang dùng.</summary>
        private ChefBonuses _gearBonuses;

        /// <summary>Hiệu ứng trang bị + độ khó vòng chơi (giá đắt hơn ở vòng sau; quán sự kiện không bị ảnh hưởng).</summary>
        public ChefBonuses Bonuses
        {
            get
            {
                var b = HelperActive ? HelperChefs.Add(_gearBonuses, HelperBonuses) : _gearBonuses;
                // Vòng chơi làm thành phố chính khó hơn; quán sự kiện thì khó theo đồ Tối thượng của sự kiện khác
                b.loopCost = InEventRun ? EventDifficulty - 1 : LoopCostFactor(Loop) - 1;
                return b;
            }
            private set => _gearBonuses = value;
        }

        // ---------- Vòng chơi ----------
        /// <summary>Mỗi vòng mọi giá mở quầy / nâng cấp / thuê người / mua bàn / cải tiến đắt hơn 15% (cộng dồn kiểu nhân).</summary>
        public const double LoopCostGrowth = 1.15;
        public int Loop => State != null ? Mathf.Max(0, State.loop) : 0;
        /// <summary>Số vòng hiển thị (bắt đầu từ 1).</summary>
        public int LoopNumber => Loop + 1;
        public static double LoopCostFactor(int loop) => Math.Pow(LoopCostGrowth, Math.Max(0, loop));
        /// <summary>Thành phố cuối hành trình: xong thì quay về thành phố đầu, sang vòng sau.</summary>
        public bool IsLastCity => City != null && City.nextCity == null;

        /// <summary>Hệ số tăng tốc (ví dụ x2 khi xem quảng cáo).</summary>
        public float SpeedMultiplier { get; private set; } = 1f;
        public float BoostSecondsLeft { get; private set; }

        /// <summary>Tiền offline chờ người chơi nhận (hiện popup).</summary>
        public BigNum PendingOffline { get; private set; }
        public double OfflineSeconds { get; private set; }

        /// <summary>Phần thưởng rương vừa mở, chờ UI hiện lên. Rỗng = không có gì để hiện.</summary>
        public List<OwnedEquipment> LastRewards { get; } = new List<OwnedEquipment>();
        public string LastRewardTitle { get; private set; } = "";

        public event Action OnVenueCompleted;

        // ---------- Bàn, khách, đầu bếp và phục vụ (chạy theo thời gian thực, không lưu) ----------

        public enum TableStage { Arriving, Ordered, Ready, Delivering, Eating }

        /// <summary>Một bàn: khách đi vào → gọi một phần mọi món đang bán → chờ nấu → chờ bưng → ăn → về.</summary>
        public class TableRun
        {
            public int index;
            public TableStage stage;
            public float timer;
            /// <summary>Các món bàn này gọi (mỗi quầy đang mở một món).</summary>
            public readonly List<StationData> order = new List<StationData>();
            public int cooked;
            /// <summary>Các món nấu ra "hoàn hảo" (bán x6) trong khay này.</summary>
            public readonly List<StationData> perfect = new List<StationData>();
            /// <summary>Tiền bữa vừa trả (để tính tip khi khách đứng dậy).</summary>
            public BigNum lastTotal;
            /// <summary>Tip đang nằm trên bàn chờ người chơi chạm nhận (cộng dồn nếu chưa nhận).</summary>
            public BigNum tip;
            /// <summary>Khách sộp: chắc chắn để lại tip rất to.</summary>
            public bool vip;
        }

        public enum WorkerKind { HeadChef, Chef, Waiter, Helper }
        public enum TaskKind { Idle, Cook, Deliver }

        /// <summary>Một người làm: đầu bếp chính (vừa nấu vừa bưng), đầu bếp thuê (nấu mọi món), phục vụ (chỉ bưng).</summary>
        public class Worker
        {
            public int index;           // thứ tự trong cùng loại
            public WorkerKind kind;
            public TaskKind task;
            public int taskId;          // đổi mỗi lần nhận việc mới
            public StationData station; // món đang nấu
            public TableRun table;      // bàn đang nấu cho / đang bưng tới
            public float elapsed, duration;
            /// <summary>Người chơi chạm để hối: làm nhanh hơn tới thời điểm này (Time.time).</summary>
            public float hurryUntil;
            public float Progress => duration > 0 ? Mathf.Clamp01(elapsed / duration) : 1f;
        }

        private readonly List<TableRun> _tables = new List<TableRun>();
        private Worker _headChef;
        private readonly List<Worker> _chefs = new List<Worker>();
        private readonly List<Worker> _waiters = new List<Worker>();
        private readonly List<Worker> _allWorkers = new List<Worker>();
        private readonly List<(TableRun table, StationData dish)> _cookQueue = new List<(TableRun, StationData)>();
        private readonly List<TableRun> _readyTrays = new List<TableRun>();
        private int _nextTaskId = 1;

        public IReadOnlyList<TableRun> Tables => _tables;
        /// <summary>Mọi người làm: [0] là đầu bếp chính, tiếp theo đầu bếp thuê, rồi phục vụ.</summary>
        public IReadOnlyList<Worker> Workers => _allWorkers;

        /// <summary>Một món vừa được nấu "hoàn hảo" (bán x6): (quầy, người nấu).</summary>
        public event Action<StationData, Worker> OnPerfectDish;

        /// <summary>Món vừa lên sao (đạt mốc x2 giá): (quầy, tổng số mốc x2 đã đạt, đá quý thưởng).</summary>
        public event Action<StationData, int, int> OnDishStarUp;

        /// <summary>Có người vừa nhận việc (nấu một món hoặc bưng một khay).</summary>
        public event Action<Worker> OnTaskStarted;

        /// <summary>Khách nhận khay và trả tiền: (người bưng, bàn, số tiền).</summary>
        public event Action<Worker, TableRun, BigNum> OnDelivered;

        private readonly System.Random _rng = new System.Random();
        private float _saveTimer;
        private bool _completedNotified;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            State = SaveService.Load();
            InitLoadedState();
            AnalyticsService.SessionStart();
            GiveStarterGear();
            if (GetComponent<Online.CloudSync>() == null) gameObject.AddComponent<Online.CloudSync>();
        }

        /// <summary>Chuẩn bị State vừa đọc (từ file trên máy hoặc từ máy chủ) để chơi tiếp.</summary>
        private void InitLoadedState()
        {
            City = State != null ? FindCity(State.cityId) : null;

            if (State == null || City == null || City.venues.Count == 0)
            {
                State = new GameState();
                City = startCity;
                State.StartCity(City);
            }
            State.EnsureEquippedSlots();
            RefreshBonuses();

            if (State.tables < 1) State.tables = 1; // save cũ
            // Bản cũ có quản lý / tiền chờ chạm thu: cộng hết vào túi
            foreach (var st in State.stations)
                if (!st.pending.IsZero) { AddMoney(st.pending); st.pending = BigNum.Zero; }
            ApplyDefaultDishValues();
            ResetRun();
            CheckEventRun(); // đang ở quán sự kiện mà sự kiện đã hết: về thành phố

            if (State.lastSaveUnix > 0)
            {
                EnsureStations();
                // Save từ bản cũ (quầy khác tên): quầy đầu chưa mở thì bắt đầu lại mốc này.
                if ((State.GetStation(Venue.stations[0].id)?.level ?? 0) <= 0) State.StartVenue(Venue);
                ComputeOffline();
            }
        }

        /// <summary>
        /// Thay toàn bộ tiến trình bằng bản lưu khác (ví dụ tải từ máy chủ khi đổi máy).
        /// Đồ mua bằng tiền thật của cả hai bản đều được giữ.
        /// </summary>
        public void LoadState(GameState loaded)
        {
            if (loaded == null) return;
            var old = State;
            if (old != null)
            {
                loaded.noAds |= old.noAds;
                loaded.starterPackBought |= old.starterPackBought;
                if (old.monthlyCardDays > loaded.monthlyCardDays)
                {
                    loaded.monthlyCardDays = old.monthlyCardDays;
                    loaded.monthlyLastClaim = old.monthlyLastClaim;
                }
            }
            State = loaded;
            PendingOffline = BigNum.Zero;
            LastRewards.Clear();
            _completedNotified = false;
            InitLoadedState();
            RunVersion++;
            GiveStarterGear();
            SaveService.Save(State);
            AnalyticsService.Log("cloud_load");
        }

        /// <summary>Tìm thành phố theo mã (dùng để hiện tóm tắt bản lưu).</summary>
        public CityData CityById(string id) => FindCity(id);

        /// <summary>
        /// Quà tân thủ: bộ 4 món Sử thi cấp 1 (dao, nồi, tạp dề, mũ), tự mặc vào ô còn trống / yếu hơn.
        /// Giúp giai đoạn đầu bớt chậm (mô phỏng: Hà Nội ~55 phút → ~36 phút). Mỗi save chỉ nhận 1 lần.
        /// </summary>
        private void GiveStarterGear()
        {
            if (State.starterGearGiven || equipmentCatalog == null) return;
            State.starterGearGiven = true;
            State.EnsureEquippedSlots();
            LastRewards.Clear();
            for (int slot = 0; slot < 4; slot++)
            {
                var item = equipmentCatalog.items.Find(e => e != null && e.rarity == Rarity.SuThi && (int)e.slot == slot);
                if (item == null) continue;
                var owned = EquipmentService.Add(State, item);
                // Ô đang mặc đồ thấp hơn Sử thi thì đổi sang món mới
                var current = ItemOf(State.GetEquipment(State.equipped[slot]));
                if (current == null || current.rarity < Rarity.SuThi) State.equipped[slot] = owned.uid;
                LastRewards.Add(owned);
            }
            LastRewardTitle = "Quà tân thủ · bộ đồ Sử thi cho đầu bếp chính";
            RefreshBonuses();
            AnalyticsService.Log("starter_gear");
            SaveService.Save(State);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (BoostSecondsLeft > 0)
            {
                BoostSecondsLeft -= dt;
                if (BoostSecondsLeft <= 0) { BoostSecondsLeft = 0; SpeedMultiplier = 1f; }
            }
            if (State.profitBoostSeconds > 0) State.profitBoostSeconds = Mathf.Max(0, State.profitBoostSeconds - dt);
            _eventCheckTimer -= dt;
            if (_eventCheckTimer <= 0) { _eventCheckTimer = 1f; CheckEventRun(); }

            UpdateRush(dt);
            UpdateEventFun(dt);
            Tick(dt * SpeedMultiplier);
            State.secondsInCity += dt;
            State.secondsInVenue += dt;
            State.totalPlaySeconds += dt;

            _saveTimer += dt;
            if (_saveTimer >= autoSaveInterval) { _saveTimer = 0; SaveService.Save(State); }

            if (!_completedNotified && IsVenueComplete())
            {
                _completedNotified = true;
                OnVenueCompleted?.Invoke();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveService.Save(State);
            else ComputeOffline();
        }

        private void OnApplicationQuit() => SaveService.Save(State);

        // ---------- Vòng nấu món ----------

        /// <summary>
        /// Bàn gọi món → đầu bếp nấu → đủ món thì bưng ra → khách nhận món mới trả tiền.
        /// </summary>
        private void Tick(float dt)
        {
            SyncRun();

            foreach (var t in _tables)
            {
                if (t.stage == TableStage.Arriving)
                {
                    t.timer -= dt;
                    if (t.timer <= 0) PlaceOrder(t);
                }
                else if (t.stage == TableStage.Eating)
                {
                    t.timer -= dt;
                    if (t.timer <= 0)
                    {
                        // Khách đứng dậy: có tỉ lệ để lại tip trên bàn (giá trị theo bữa ăn → tăng theo cấp quầy)
                        if (!t.lastTotal.IsZero && (t.vip || _rng.NextDouble() < TipChance))
                        {
                            var add = t.lastTotal * (t.vip ? VipTipMultiplier : TipFraction);
                            t.tip += add;
                            OnTipLeft?.Invoke(t, add);
                        }
                        t.vip = false;
                        t.stage = TableStage.Arriving;
                        t.timer = EconomyService.CustomerArriveSeconds;
                    }
                }
            }

            foreach (var w in _allWorkers)
            {
                if (w.task == TaskKind.Idle) continue;
                w.elapsed += dt * WorkSpeed(w);
                if (w.elapsed >= w.duration) FinishTask(w);
            }

            // Giao việc: phục vụ bưng khay, đầu bếp nấu, đầu bếp chính làm chỗ đang thiếu người
            foreach (var w in _waiters)
                if (w.task == TaskKind.Idle && _readyTrays.Count > 0) StartDeliver(w);
            foreach (var w in _chefs)
                if (w.task == TaskKind.Idle && _cookQueue.Count > 0) StartCook(w);
            // Đầu bếp mượn: chỉ nhận việc mới khi còn giờ
            if (_helper != null && HelperActive && _helper.task == TaskKind.Idle && _cookQueue.Count > 0) StartCook(_helper);
            if (_headChef.task == TaskKind.Idle)
            {
                bool waiterFree = false;
                foreach (var w in _waiters) if (w.task == TaskKind.Idle) { waiterFree = true; break; }
                if (_readyTrays.Count > 0 && !waiterFree) StartDeliver(_headChef);
                else if (_cookQueue.Count > 0) StartCook(_headChef);
            }
        }

        /// <summary>Số bàn / đầu bếp / phục vụ trên sàn khớp với số đã mua.</summary>
        private void SyncRun()
        {
            bool changed = false;
            if (_headChef == null) { _headChef = new Worker { kind = WorkerKind.HeadChef }; changed = true; }
            while (_chefs.Count < State.chefs) { _chefs.Add(new Worker { kind = WorkerKind.Chef, index = _chefs.Count }); changed = true; }
            while (_waiters.Count < State.waiters) { _waiters.Add(new Worker { kind = WorkerKind.Waiter, index = _waiters.Count }); changed = true; }
            // Đầu bếp mượn: vào khi còn giờ; hết giờ thì làm nốt món đang nấu rồi mới về
            if (HelperActive && _helper == null) { _helper = new Worker { kind = WorkerKind.Helper }; changed = true; }
            else if (!HelperActive && _helper != null && _helper.task == TaskKind.Idle) { _helper = null; changed = true; }
            while (_tables.Count < Mathf.Max(1, State.tables))
                _tables.Add(new TableRun { index = _tables.Count, stage = TableStage.Arriving, timer = EconomyService.CustomerArriveSeconds });
            if (!changed) return;
            _allWorkers.Clear();
            _allWorkers.Add(_headChef);
            _allWorkers.AddRange(_chefs);
            _allWorkers.AddRange(_waiters);
            if (_helper != null) _allWorkers.Add(_helper);
        }

        /// <summary>Xóa toàn bộ khách và việc đang làm (khi mở game, lên đời, xóa save).</summary>
        private void ResetRun()
        {
            _tables.Clear(); _chefs.Clear(); _waiters.Clear(); _allWorkers.Clear();
            _cookQueue.Clear(); _readyTrays.Clear();
            _headChef = null;
            _helper = null;
            if (State != null) foreach (var st in State.stations) st.ready = 0;
        }

        private void PlaceOrder(TableRun t)
        {
            t.order.Clear();
            // Best seller: 95% khách chỉ gọi đúng món đó
            var best = BestSellerStation;
            if (best != null && _rng.NextDouble() < BestSellerChance) t.order.Add(best);
            else
                foreach (var data in Venue.stations)
                    if (IsUnlocked(data)) t.order.Add(data);
            if (t.order.Count == 0) { t.timer = 1f; return; }
            t.stage = TableStage.Ordered;
            t.vip = RollVip();
            if (t.vip) OnVipArrived?.Invoke(t);
            t.cooked = 0;
            t.perfect.Clear();
            foreach (var dish in t.order) _cookQueue.Add((t, dish));
        }

        private void StartCook(Worker w)
        {
            var (table, dish) = _cookQueue[0];
            _cookQueue.RemoveAt(0);
            w.task = TaskKind.Cook;
            w.taskId = _nextTaskId++;
            w.table = table;
            w.station = dish;
            w.elapsed = 0;
            w.duration = (float)EconomyService.CookTime(dish, Bonuses, CookSpeedUpgrade);
            if (w.kind == WorkerKind.HeadChef) w.duration *= (float)Bonuses.HeadCookFactor; // buff riêng đầu bếp chính
            OnTaskStarted?.Invoke(w);
        }

        private void StartDeliver(Worker w)
        {
            var table = _readyTrays[0];
            _readyTrays.RemoveAt(0);
            table.stage = TableStage.Delivering;
            foreach (var dish in table.order)
            {
                var st = State.GetStation(dish.id);
                if (st != null && st.ready > 0) st.ready--;
            }
            w.task = TaskKind.Deliver;
            w.taskId = _nextTaskId++;
            w.table = table;
            w.station = null;
            w.elapsed = 0;
            w.duration = TripSeconds * (float)Bonuses.TripTimeMultiplier;
            if (w.kind == WorkerKind.HeadChef) w.duration *= (float)Bonuses.HeadTripFactor;
            OnTaskStarted?.Invoke(w);
        }

        private void FinishTask(Worker w)
        {
            var table = w.table;
            if (w.task == TaskKind.Cook)
            {
                var st = State.GetStation(w.station.id);
                if (st != null) st.ready++;
                table.cooked++;
                // Món hoàn hảo (bán x6): đầu bếp chính được cộng cả tỉ lệ riêng
                double chance = w.kind == WorkerKind.HeadChef ? Bonuses.PerfectChanceHead : Bonuses.PerfectChanceAll;
                if (chance > 0 && _rng.NextDouble() < chance)
                {
                    table.perfect.Add(w.station);
                    OnPerfectDish?.Invoke(w.station, w);
                }
                if (table.cooked >= table.order.Count) { table.stage = TableStage.Ready; _readyTrays.Add(table); }
            }
            else if (w.task == TaskKind.Deliver)
            {
                // Khách nhận khay: tiền cộng thẳng vào túi
                BigNum total = BigNum.Zero;
                foreach (var dish in table.order)
                {
                    var st = State.GetStation(dish.id);
                    if (st == null || st.level <= 0) continue;
                    total += DishPrice(dish, st.level);
                }
                foreach (var dish in table.perfect)
                {
                    var st = State.GetStation(dish.id);
                    if (st != null && st.level > 0) total += DishPrice(dish, st.level) * (ChefBonuses.PerfectValue - 1);
                }
                table.perfect.Clear();
                table.lastTotal = total;
                AddMoney(total);
                // Sự kiện: khách có thể để lại bao lì xì / đèn lồng...
                var ev = CurrentEvent;
                if (ev != null && _rng.NextDouble() < EventService.TokenChance)
                {
                    State.eventTokens++;
                    OnEventToken?.Invoke(table, 1, ev);
                }
                State.customersServed++;
                GainPride();
                QuestService.Track(State, QuestType.ServeCustomers, 1);
                table.stage = TableStage.Eating;
                table.timer = EconomyService.EatSeconds;
                OnDelivered?.Invoke(w, table, total);
            }
            w.task = TaskKind.Idle;
            w.table = null;
            w.station = null;
        }

        // ---------- Nâng cấp món ----------

        /// <summary>Giá bán một món ở cấp cho trước, đã tính nâng cấp món.</summary>
        public BigNum DishPrice(StationData data, int level) =>
            EconomyService.ProfitPerDish(data, Venue, level, Bonuses,
                VenueUpgrades.RecipeMultiplier(State, Venue.stations.IndexOf(data)) * EventProfitMultiplier * ProfitBoostMultiplier * RushProfitMultiplier * FireworksProfit);

        // ---------- Lợi nhuận x5 (mua bằng đá quý) ----------

        public const int ProfitBoostPrice = 80;
        public const float ProfitBoostDuration = 300f;
        public const double ProfitBoostValue = 5;

        public float ProfitBoostSecondsLeft => State.profitBoostSeconds;
        public double ProfitBoostMultiplier => State.profitBoostSeconds > 0 ? ProfitBoostValue : 1;

        /// <summary>Mua thêm 5 phút lợi nhuận x5 (đang chạy thì cộng dồn thời gian).</summary>
        public bool TryBuyProfitBoost()
        {
            if (State.gems < ProfitBoostPrice) return false;
            State.gems -= ProfitBoostPrice;
            State.profitBoostSeconds += ProfitBoostDuration;
            AnalyticsService.Log("gem_spend", ("item", "profit_x5"), ("gems", ProfitBoostPrice));
            SaveService.Save(State);
            return true;
        }

        // ---------- Skill "Món best seller" ----------

        public const int BestSellerSeconds = 180;      // hiệu ứng 3 phút
        public const int BestSellerCooldown = 600;     // hồi chiêu 10 phút (tính từ lúc hết hoặc lúc hủy)
        public const double BestSellerChance = 0.95;   // 95% khách mới vào chỉ gọi món này

        /// <summary>Quầy đang là best seller (null nếu không chạy, hoặc quầy không thuộc quán đang ở).</summary>
        public StationData BestSellerStation
        {
            get
            {
                if (State == null || string.IsNullOrEmpty(State.bestSellerStation) || SaveService.NowUnix >= State.bestSellerEndUnix) return null;
                foreach (var data in Venue.stations)
                    if (data.id == State.bestSellerStation) return IsUnlocked(data) ? data : null;
                return null;
            }
        }

        public long BestSellerSecondsLeft => BestSellerStation != null ? State.bestSellerEndUnix - SaveService.NowUnix : 0;
        public long BestSellerCooldownLeft
        {
            get
            {
                if (State == null) return 0;
                long now = SaveService.NowUnix;
                if (now < State.bestSellerEndUnix && BestSellerStation != null) return 0; // đang chạy
                return Math.Max(0, State.bestSellerReadyUnix - now);
            }
        }

        public bool CanStartBestSeller(StationData data) =>
            data != null && IsUnlocked(data) && BestSellerStation == null && BestSellerCooldownLeft <= 0;

        /// <summary>Bật best seller cho một quầy: 3 phút, sau đó hồi chiêu 10 phút.</summary>
        public bool StartBestSeller(StationData data)
        {
            if (!CanStartBestSeller(data)) return false;
            long now = SaveService.NowUnix;
            State.bestSellerStation = data.id;
            State.bestSellerEndUnix = now + BestSellerSeconds;
            State.bestSellerReadyUnix = State.bestSellerEndUnix + BestSellerCooldown;
            AnalyticsService.Log("skill_best_seller", ("station", data.id));
            SaveService.Save(State);
            return true;
        }

        /// <summary>Hủy best seller ngay; hồi chiêu 10 phút tính từ lúc hủy.</summary>
        public bool CancelBestSeller()
        {
            if (BestSellerStation == null) return false;
            long now = SaveService.NowUnix;
            AnalyticsService.Log("skill_best_seller_cancel", ("seconds_left", State.bestSellerEndUnix - now));
            State.bestSellerEndUnix = now;
            State.bestSellerReadyUnix = now + BestSellerCooldown;
            SaveService.Save(State);
            return true;
        }

        // ---------- Sự kiện theo mùa ----------

        /// <summary>Sự kiện đang diễn ra (null nếu không có). Tiến độ tự làm lại khi sang lần sự kiện mới.</summary>
        public SeasonEvent CurrentEvent
        {
            get
            {
                var e = EventService.Current;
                EventService.Sync(State, e);
                return e;
            }
        }

        /// <summary>Có sự kiện thì khách trả thêm.</summary>
        public double EventProfitMultiplier => EventService.Current != null ? EventService.ProfitBonus : 1;

        /// <summary>Khách để lại vật phẩm sự kiện ở bàn: (bàn, số vật phẩm, sự kiện).</summary>
        public event Action<TableRun, int, SeasonEvent> OnEventToken;

        // ---------- Nâng cấp quảng cáo (mua bằng đá quý) ----------

        /// <summary>
        /// Nâng vô hạn cấp; giá tăng theo cấp số nhân: 50 ♦ × 1,25^cấp (làm tròn đẹp).
        /// Tổng tích lũy mỗi loại: cấp 10 ≈ 1.700 ♦ · cấp 20 ≈ 17.000 ♦ · cấp 30 ≈ 160.000 ♦ · cấp 40 ≈ 1,5 triệu ♦.
        /// </summary>
        public static long AdUpgradeCost(int level)
        {
            double c = 50 * Math.Pow(1.25, Math.Max(0, level));
            if (c >= 1e15) return (long)1e15; // chặn tràn số (thực tế không ai tới được)
            if (c >= 1000) return (long)(Math.Round(c / 50) * 50);
            if (c >= 100) return (long)(Math.Round(c / 5) * 5);
            return (long)Math.Round(c);
        }

        public int AdValueLevel => Mathf.Max(0, State?.adValueLevel ?? 0);
        public int AdDurationLevel => Mathf.Max(0, State?.adDurationLevel ?? 0);

        /// <summary>"Quảng cáo hậu hĩnh": mỗi cấp tăng tốc +0,1 (từ x2), tiền offline +0,1 (từ x2), đá quý miễn phí +2 (từ 20).</summary>
        public static float AdBoostMultiplierAt(int level) => 2f + 0.1f * level;
        public static int AdFreeGemsAt(int level) => 20 + 2 * level;
        public float AdBoostMultiplier => AdBoostMultiplierAt(AdValueLevel);
        public double AdOfflineMultiplier => AdBoostMultiplierAt(AdValueLevel);
        public int AdFreeGems => AdFreeGemsAt(AdValueLevel);

        /// <summary>"Quảng cáo bền lâu": tăng tốc từ 5 phút, mỗi cấp +30 giây.</summary>
        public static float AdBoostSecondsAt(int level) => 300f + 30f * level;
        public float AdBoostSeconds => AdBoostSecondsAt(AdDurationLevel);

        public bool TryUpgradeAdValue() => TryUpgradeAd(ref State.adValueLevel, "ad_value");
        public bool TryUpgradeAdDuration() => TryUpgradeAd(ref State.adDurationLevel, "ad_duration");

        private bool TryUpgradeAd(ref int level, string id)
        {
            long price = AdUpgradeCost(level);
            if (State.gems < price) return false;
            State.gems -= price;
            level++;
            AnalyticsService.Log("gem_spend", ("item", $"{id}_{level}"), ("gems", price));
            SaveService.Save(State);
            return true;
        }

        // ---------- Tip ----------

        /// <summary>Tỉ lệ tip gốc 6%, mỗi cấp "Khách hào phóng" +2%, tối đa 100% (47 cấp).</summary>
        public const double TipBaseChance = 0.06, TipChancePerLevel = 0.02;
        public const int TipMaxLevelConst = 47;
        /// <summary>
        /// Giá cấp kế tiếp tăng theo cấp số nhân: 10 ♦ × 1,16^cấp (làm tròn đẹp).
        /// Đầu rẻ (10, 12, 13…), về sau rất đắt: cấp cuối 9.250 ♦; tổng để đạt 100% ≈ 67.000 ♦.
        /// Mốc tổng: 26% ≈ 210 ♦ · 46% ≈ 1.150 ♦ · 66% ≈ 5.300 ♦ · 86% ≈ 23.600 ♦.
        /// </summary>
        public const double TipCostBase = 10, TipCostGrowth = 1.16;

        public static int TipCostAt(int level)
        {
            double c = TipCostBase * Math.Pow(TipCostGrowth, level);
            if (c >= 1000) return (int)(Math.Round(c / 50) * 50);
            if (c >= 100) return (int)(Math.Round(c / 5) * 5);
            return (int)Math.Round(c);
        }

        public int TipLevel => Mathf.Clamp(State?.tipLevel ?? 0, 0, TipMaxLevelConst);
        public int TipMaxLevel => TipMaxLevelConst;
        /// <summary>Tỉ lệ khách để lại tip khi ăn xong (theo cấp hiện tại).</summary>
        public double TipChance => ChanceAt(TipLevel);
        public double NextTipChance => ChanceAt(Mathf.Min(TipLevel + 1, TipMaxLevelConst));
        private static double ChanceAt(int level) => Math.Min(1.0, TipBaseChance + TipChancePerLevel * level);
        public int TipUpgradePrice => TipLevel < TipMaxLevel ? TipCostAt(TipLevel) : 0;

        /// <summary>Nâng tỉ lệ tip bằng đá quý.</summary>
        public bool TryUpgradeTip()
        {
            if (TipLevel >= TipMaxLevel || State.gems < TipUpgradePrice) return false;
            int price = TipUpgradePrice;
            State.gems -= price;
            State.tipLevel = TipLevel + 1;
            AnalyticsService.Log("gem_spend", ("item", "tip_level_" + State.tipLevel), ("gems", price));
            SaveService.Save(State);
            return true;
        }
        /// <summary>Tip = phần trăm tiền bữa ăn (bữa đắt hơn khi quầy được nâng cấp → tip to hơn).</summary>
        public const double TipFraction = 0.5;

        /// <summary>Khách vừa để tip trên bàn: (bàn, số tiền thêm).</summary>
        public event Action<TableRun, BigNum> OnTipLeft;

        /// <summary>Người chơi chạm đồng xu trên bàn: cộng tip vào túi, trả về số tiền nhận được.</summary>
        public BigNum CollectTip(int tableIndex)
        {
            if (tableIndex < 0 || tableIndex >= _tables.Count) return BigNum.Zero;
            var t = _tables[tableIndex];
            if (t.tip.IsZero) return BigNum.Zero;
            var amount = t.tip;
            t.tip = BigNum.Zero;
            TipsCollected++;
            AddMoney(amount);
            return amount;
        }

        public bool ClaimEventReward(int index)
        {
            var e = CurrentEvent;
            if (e == null || index < 0 || index >= EventService.Rewards.Length || !EventService.CanClaim(State, index)) return false;
            var r = EventService.Rewards[index];
            State.eventClaimed |= 1 << index;
            AnalyticsService.Log("event_reward", ("event", e.Key), ("milestone", index), ("tokens", State.eventTokens));
            State.gems += r.gems;
            if (r.chestItems > 0)
            {
                OpenChest(r.chestItems, $"Quà {e.name} · rương {r.chestItems} món",
                    r.eventChest ? Rarity.SuThi : Rarity.Thuong);
                if (r.eventChest) GrantBlueprint(e.id);
            }
            else SaveService.Save(State);
            return true;
        }

        /// <summary>Bậc nâng cấp món tiếp theo của quầy; -1 nếu đã mua hết.</summary>
        public int NextRecipeTier(StationData data) => VenueUpgrades.NextRecipeTier(State, Venue.stations.IndexOf(data));

        public BigNum RecipePrice(StationData data, int tier) =>
            VenueUpgrades.RecipePrice(Venue, Venue.stations.IndexOf(data), tier, Bonuses);

        /// <summary>Quầy đang mở, còn bậc nâng cấp món và đủ tiền mua.</summary>
        public bool CanBuyRecipe(StationData data)
        {
            if (!IsUnlocked(data)) return false;
            int tier = NextRecipeTier(data);
            return tier >= 0 && State.money >= RecipePrice(data, tier);
        }

        public bool TryBuyRecipe(StationData data)
        {
            if (!IsUnlocked(data)) return false;
            int i = Venue.stations.IndexOf(data);
            int tier = NextRecipeTier(data);
            if (i < 0 || tier < 0) return false;
            return TrySpend(RecipePrice(data, tier), () => State.recipesBought |= 1 << (i * VenueUpgrades.RecipeTiers.Length + tier));
        }

        /// <summary>Hệ số tốc độ chạy bàn từ cải tiến.</summary>
        public double WaiterSpeed => VenueUpgrades.Multiplier(State, UpgradeKind.WaiterSpeed);

        /// <summary>Hệ số tốc độ nấu từ cải tiến.</summary>
        public double CookSpeedUpgrade => VenueUpgrades.Multiplier(State, UpgradeKind.CookSpeed);

        public float TripSeconds => (float)(EconomyService.BaseTripSeconds / WaiterSpeed);

        /// <summary>Thu nhập ước tính mỗi giây với số bàn / người hiện có.</summary>
        public BigNum EstimatedIncome => EconomyService.EstimatedIncome(State, Venue, Bonuses, CookSpeedUpgrade, WaiterSpeed);

        // ---------- Cải tiến (mua bằng tiền, mất khi lên đời) ----------

        public bool IsUpgradeBought(int index) => VenueUpgrades.IsBought(State, index);

        public BigNum UpgradeItemPrice(int index) => VenueUpgrades.Price(index, Venue, Bonuses);

        public bool TryBuyUpgrade(int index)
        {
            if (index < 0 || index >= VenueUpgrades.All.Length || IsUpgradeBought(index)) return false;
            return TrySpend(UpgradeItemPrice(index), () => State.upgradesBought |= 1 << index);
        }

        /// <summary>Có món nào trong bảng nâng cấp quán (người, bàn, nâng cấp món, cải tiến) đang mua được không.</summary>
        public bool CanBuyAnyUpgrade
        {
            get
            {
                if (CanHireChef && State.money >= ChefPrice) return true;
                if (CanHireWaiter && State.money >= WaiterPrice) return true;
                if (CanBuyTable && State.money >= TablePrice) return true;
                for (int i = 0; i < VenueUpgrades.All.Length; i++)
                    if (!IsUpgradeBought(i) && State.money >= UpgradeItemPrice(i)) return true;
                foreach (var data in Venue.stations) if (CanBuyRecipe(data)) return true;
                return false;
            }
        }

        private void AddMoney(BigNum amount)
        {
            State.money += amount;
            State.earnedInCity += amount;
        }

        // ---------- Hành động của người chơi ----------

        public bool IsUnlocked(StationData data) => (State.GetStation(data.id)?.level ?? 0) > 0;

        /// <summary>
        /// Mọi quầy của quán đều mở được tự do (không cần mở quầy phía trước trước);
        /// mỗi quầy vẫn có giá mở riêng (quầy càng về sau càng đắt).
        /// </summary>
        public bool CanShowUnlock(StationData data) => data != null && Venue.stations.Contains(data);

        public BigNum UnlockPrice(StationData data) => EconomyService.UnlockCost(data, Venue, Bonuses);

        public bool TryUnlock(StationData data)
        {
            if (IsUnlocked(data) || !CanShowUnlock(data)) return false;
            return TrySpend(UnlockPrice(data), () =>
            {
                State.GetOrCreateStation(data.id).level = 1;
                QuestService.Track(State, QuestType.OpenStations, 1);
            });
        }

        /// <summary>Số cấp sẽ mua với chế độ hiện tại. count = -1 nghĩa là mua tối đa.</summary>
        public int LevelsToBuy(StationData data, int count)
        {
            int level = State.GetStation(data.id)?.level ?? 0;
            return count < 0
                ? Mathf.Max(1, EconomyService.MaxAffordableLevels(data, Venue, level, State.money, Bonuses))
                : Mathf.Min(count, data.maxLevel - level);
        }

        public BigNum UpgradePrice(StationData data, int count)
        {
            int level = State.GetStation(data.id)?.level ?? 0;
            return EconomyService.UpgradeCost(data, Venue, level, LevelsToBuy(data, count), Bonuses);
        }

        public bool TryUpgrade(StationData data, int count)
        {
            var st = State.GetStation(data.id);
            if (st == null || st.level <= 0 || st.level >= data.maxLevel) return false;
            int n = LevelsToBuy(data, count);
            if (n <= 0) return false;
            return TrySpend(EconomyService.UpgradeCost(data, Venue, st.level, n, Bonuses), () =>
            {
                int before = EconomyService.DoublingsReached(data, st.level);
                int levelBefore = st.level;
                st.level += n;
                EventLevelsGained(data, levelBefore, st.level);
                QuestService.Track(State, QuestType.UpgradeLevels, n);
                // Mỗi mốc x2 giá món: món lên 1 sao, thưởng 1 đá quý
                int after = EconomyService.DoublingsReached(data, st.level);
                int gained = after - before;
                if (gained > 0)
                {
                    State.gems += gained * DishStars.GemsPerStar;
                    AnalyticsService.Log("dish_star", ("station", data.id), ("stars", after), ("gems", gained * DishStars.GemsPerStar));
                    OnDishStarUp?.Invoke(data, after, gained * DishStars.GemsPerStar);
                }
            });
        }

        // ---------- Người làm và bàn (chung cả quán) ----------

        /// <summary>Giữ cho code cũ: tốc độ nấu chung (giờ chỉ đến từ cải tiến bếp).</summary>
        public double ServiceSpeed => CookSpeedUpgrade;

        public int MaxChefs => EconomyService.MaxChefs(Venue);
        public bool CanHireChef => State.chefs < MaxChefs;
        public BigNum ChefPrice => EconomyService.ChefHirePrice(Venue, State.chefs, Bonuses);

        public bool TryHireChef()
        {
            if (!CanHireChef) return false;
            return TrySpend(ChefPrice, () =>
            {
                State.chefs++;
                QuestService.Track(State, QuestType.HireStaff, 1);
            });
        }

        public int MaxWaiters => EconomyService.MaxHiredWaiters(Venue);
        public bool CanHireWaiter => State.waiters < MaxWaiters;
        public BigNum WaiterPrice => EconomyService.WaiterHirePrice(Venue, State.waiters, Bonuses);

        public bool TryHireWaiter()
        {
            if (!CanHireWaiter) return false;
            return TrySpend(WaiterPrice, () =>
            {
                State.waiters++;
                QuestService.Track(State, QuestType.HireStaff, 1);
            });
        }

        public int MaxTables => InEventRun
            ? EventVenueFactory.Tables
            : EconomyService.DefaultTables[Mathf.Clamp(State.venueIndex, 0, EconomyService.DefaultTables.Length - 1)];
        public bool CanBuyTable => State.tables < MaxTables;
        public BigNum TablePrice => EconomyService.TablePrice(Venue, State.tables, MaxTables, Bonuses);

        public bool TryBuyTable()
        {
            if (!CanBuyTable) return false;
            return TrySpend(TablePrice, () => State.tables++);
        }

        private bool TrySpend(BigNum cost, Action onBought)
        {
            if (State.money < cost) return false;
            State.money -= cost;
            onBought();
            return true;
        }

        /// <summary>Gọi sau khi người chơi xem xong quảng cáo thưởng. Mũ đầu bếp kéo dài thời gian.</summary>
        public void StartBoost(float seconds = 300f, float multiplier = 2f)
        {
            SpeedMultiplier = multiplier;
            float duration = (float)(seconds * Bonuses.BoostDurationMultiplier);
            BoostSecondsLeft = Mathf.Max(BoostSecondsLeft, duration);
        }

        public void ClaimOffline(bool doubled)
        {
            AddMoney(PendingOffline * (doubled ? AdOfflineMultiplier : 1));
            PendingOffline = BigNum.Zero;
            OfflineSeconds = 0;
        }

        // ---------- Đầu bếp chính ----------

        public void RefreshBonuses() => Bonuses = EquipmentService.Compute(State, equipmentCatalog);

        public EquipmentData ItemOf(OwnedEquipment owned) =>
            owned != null && equipmentCatalog != null ? equipmentCatalog.Find(owned.id) : null;

        public void Equip(OwnedEquipment owned)
        {
            EquipmentService.Equip(State, owned, ItemOf(owned));
            RefreshBonuses();
        }

        public bool LevelUpEquipment(OwnedEquipment owned)
        {
            bool ok = EquipmentService.LevelUp(State, equipmentCatalog, owned);
            if (ok) RefreshBonuses();
            return ok;
        }

        /// <summary>
        /// Nâng nhanh: nâng 4 món đang mặc hết mức số mảnh cho phép (mỗi vị trí dùng mảnh riêng).
        /// apply = false chỉ tính thử. Trả về tổng số cấp nâng được; perSlot = số cấp từng vị trí.
        /// </summary>
        public int QuickLevelUp(bool apply, int[] perSlot = null)
        {
            State.EnsureEquippedSlots();
            int total = 0;
            for (int slot = 0; slot < 4; slot++)
            {
                int gained = 0;
                var owned = State.GetEquipment(State.equipped[slot]);
                var item = ItemOf(owned);
                if (item != null)
                {
                    int shards = State.shards[slot], level = owned.level;
                    while (level < item.MaxLevel)
                    {
                        int cost = EquipmentService.LevelUpCost(equipmentCatalog, item, level);
                        if (shards < cost) break;
                        shards -= cost;
                        level++;
                        gained++;
                    }
                    if (apply) for (int i = 0; i < gained; i++) EquipmentService.LevelUp(State, equipmentCatalog, owned);
                }
                if (perSlot != null && slot < perSlot.Length) perSlot[slot] = gained;
                total += gained;
            }
            if (apply && total > 0)
            {
                RefreshBonuses();
                AnalyticsService.Log("gear_quick_level", ("levels", total));
                SaveService.Save(State);
            }
            return total;
        }

        /// <summary>Phân rã món không đang dùng thành mảnh cùng vị trí.</summary>
        public bool Dismantle(OwnedEquipment owned)
        {
            bool ok = EquipmentService.Dismantle(State, equipmentCatalog, owned);
            if (ok)
            {
                QuestService.Track(State, QuestType.DismantleItems, 1);
                SaveService.Save(State);
            }
            return ok;
        }

        /// <summary>Món sẽ bị phân rã nhanh: mọi món không đang mặc thuộc các độ hiếm được chọn.</summary>
        public List<OwnedEquipment> QuickDismantleList(bool[] rarities)
        {
            var list = new List<OwnedEquipment>();
            if (equipmentCatalog == null) return list;
            foreach (var o in State.equipment)
            {
                var item = equipmentCatalog.Find(o.id);
                if (item == null || EquipmentService.IsEquipped(State, o)) continue;
                int r = (int)item.rarity;
                if (r < rarities.Length && rarities[r]) list.Add(o);
            }
            return list;
        }

        /// <summary>Số mảnh nhận được theo từng vị trí nếu phân rã danh sách này.</summary>
        public int[] DismantlePreview(List<OwnedEquipment> list)
        {
            var shards = new int[4];
            foreach (var o in list)
            {
                var item = equipmentCatalog.Find(o.id);
                if (item != null) shards[(int)item.slot] += EquipmentService.DismantleValue(equipmentCatalog, o, item);
            }
            return shards;
        }

        /// <summary>Phân rã nhanh cả danh sách (lưu game một lần).</summary>
        public int QuickDismantle(bool[] rarities)
        {
            int n = 0;
            foreach (var o in QuickDismantleList(rarities))
                if (EquipmentService.Dismantle(State, equipmentCatalog, o)) n++;
            if (n > 0)
            {
                QuestService.Track(State, QuestType.DismantleItems, n);
                AnalyticsService.Log("quick_dismantle", ("items", n));
                SaveService.Save(State);
            }
            return n;
        }

        /// <summary>Mua rương bằng đá quý (rương nhỏ / rương lớn, tỉ lệ riêng từng loại).</summary>
        public bool TryBuyChest(ChestType type)
        {
            if (equipmentCatalog == null || type == null || State.gems < type.price) return false;
            State.gems -= type.price;
            AnalyticsService.Log("gem_spend", ("item", "chest_" + type.id), ("gems", type.price));
            OpenChest(type.items, $"{type.name} · {type.items} món", Rarity.Thuong, type.weights);
            return true;
        }

        public void ClearRewards() { LastRewards.Clear(); LastBlueprint = null; }

        // ---------- Đồ Tối thượng: bản vẽ và ghép ----------

        /// <summary>Bản vẽ vừa nhận từ rương sự kiện (hiện trong popup rương); null nếu không có.</summary>
        public string LastBlueprint { get; private set; }

        /// <summary>Rương sự kiện: thêm bản vẽ một món Tối thượng của sự kiện (đủ cả 4 rồi thì đổi thành đá quý).</summary>
        private void GrantBlueprint(string eventId)
        {
            string id = UltimateItems.RollBlueprint(State, eventId, _rng);
            if (id == null)
            {
                State.gems += UltimateItems.DuplicateBlueprintGems;
                LastRewardTitle += $" · đủ bản vẽ, +{UltimateItems.DuplicateBlueprintGems} ♦";
                return;
            }
            LastBlueprint = id;
            AnalyticsService.Log("blueprint", ("item", id));
            SaveService.Save(State);
        }

        public bool CanCraftUltimate(string id) => UltimateItems.CanCraft(State, equipmentCatalog, id);

        public bool CraftUltimate(string id)
        {
            var owned = UltimateItems.Craft(State, equipmentCatalog, id);
            if (owned == null) return false;
            RefreshBonuses();
            LastRewards.Clear();
            LastRewards.Add(owned);
            LastRewardTitle = "Ghép thành công đồ Tối thượng!";
            AnalyticsService.Log("ultimate_craft", ("item", id));
            SaveService.Save(State);
            return true;
        }

        /// <summary>Chỉ để thử: nhận 1 bản vẽ + đủ nguyên liệu (2 Huyền thoại, 2 Sử thi, 3 Hiếm).</summary>
        public void DebugUltimateKit()
        {
            if (equipmentCatalog == null) return;
            string ev = CurrentEvent?.id ?? UltimateItems.EventIds[_rng.Next(UltimateItems.EventIds.Length)];
            string bp = UltimateItems.RollBlueprint(State, ev, _rng);
            if (bp == null && State.blueprints.Count > 0) bp = State.blueprints[0];
            void Give(Rarity r, int n)
            {
                var pool = equipmentCatalog.items.FindAll(e => e != null && e.rarity == r);
                for (int i = 0; i < n && pool.Count > 0; i++) EquipmentService.Add(State, pool[_rng.Next(pool.Count)]);
            }
            // 2 món Huyền thoại đúng công thức của bản vẽ
            if (bp != null && UltimateItems.Recipes.TryGetValue(bp, out var need))
                foreach (var id in need) { var item = equipmentCatalog.Find(id); if (item != null) EquipmentService.Add(State, item); }
            Give(Rarity.SuThi, UltimateItems.NeedEpic);
            Give(Rarity.Hiem, UltimateItems.NeedRare);
            RefreshBonuses();
        }

        // ---------- Nhiệm vụ ngày và điểm danh ----------

        public List<QuestState> Quests
        {
            get { QuestService.EnsureDaily(State); return State.quests; }
        }

        public bool ClaimQuest(int index)
        {
            bool ok = QuestService.Claim(State, index);
            if (ok)
            {
                AnalyticsService.Log("quest_claim", ("type", State.quests[index].type.ToString()), ("gems", State.quests[index].reward));
                SaveService.Save(State);
            }
            return ok;
        }

        /// <summary>Nhận quà điểm danh hôm nay; ngày thứ 7 mở rương.</summary>
        public bool ClaimLogin()
        {
            if (!QuestService.CanClaimLogin(State)) return false;
            if (QuestService.ClaimLogin(State, out _))
                OpenChest(QuestService.LoginChestItems, $"Quà điểm danh ngày 7 · rương {QuestService.LoginChestItems} món");
            SaveService.Save(State);
            return true;
        }

        /// <summary>Chỉ để thử nghiệm trong bản chơi thử.</summary>
        public void DebugAddGems(int amount) => State.gems += amount;

        /// <summary>Phát rương từ nguồn ngoài (cửa hàng, quảng cáo).</summary>
        public void GrantChest(int count, string title, Rarity guaranteed = Rarity.Thuong) => OpenChest(count, title, guaranteed);

        private void OpenChest(int count, string title, Rarity guaranteed = Rarity.Thuong, float[] weights = null)
        {
            AnalyticsService.Log("chest_open", ("items", count), ("min_rarity", guaranteed.ToString()));
            // Popup rương trước chưa đóng (ví dụ nhận nhiều mốc quà sự kiện cùng lúc): gộp vào
            bool stacking = LastRewards.Count > 0;
            LastRewards.AddRange(EquipmentService.OpenChest(count, equipmentCatalog, State, _rng, guaranteed, weights));
            LastRewardTitle = stacking ? "Mở nhiều rương" : title;
            RefreshBonuses();
            SaveService.Save(State);
        }

        // ---------- Mục tiêu và chuyển thành phố ----------

        /// <summary>Tiến độ qua mốc: từng hạng mục đã xong / tổng số.</summary>
        public struct Progress
        {
            public int stationsMaxed, stationCount;
            public int upgrades, maxUpgrades; // nâng cấp món + cải tiến tốc độ
            public int chefs, maxChefs, waiters, maxWaiters, tables, maxTables;
            public int levelsDone, levelsTotal;
        }

        public Progress GetProgress()
        {
            var p = new Progress
            {
                stationCount = Venue.stations.Count,
                chefs = State.chefs, maxChefs = MaxChefs,
                waiters = State.waiters, maxWaiters = MaxWaiters,
                tables = State.tables, maxTables = MaxTables,
            };
            foreach (var data in Venue.stations)
            {
                var st = State.GetStation(data.id);
                int level = st?.level ?? 0;
                p.levelsDone += Mathf.Min(level, data.maxLevel);
                p.levelsTotal += data.maxLevel;
                if (level >= data.maxLevel) p.stationsMaxed++;
            }
            p.maxUpgrades = p.stationCount * VenueUpgrades.RecipeTiers.Length + VenueUpgrades.All.Length;
            p.upgrades = VenueUpgrades.RecipesBoughtCount(State, p.stationCount);
            for (int i = 0; i < VenueUpgrades.All.Length; i++) if (IsUpgradeBought(i)) p.upgrades++;
            // Người, bàn và nâng cấp cũng tính vào thanh tiến độ (mỗi món đồ = 1 cấp)
            p.levelsDone += p.chefs + p.waiters + p.tables + p.upgrades;
            p.levelsTotal += p.maxChefs + p.maxWaiters + p.maxTables + p.maxUpgrades;
            return p;
        }

        /// <summary>Qua mốc khi đã nâng hết: mọi quầy cấp tối đa, đủ đầu bếp, phục vụ, bàn, mọi nâng cấp món và cải tiến.</summary>
        public bool IsVenueComplete()
        {
            var p = GetProgress();
            bool maxed = !Venue.requireAllMaxed ||
                         (p.stationsMaxed == p.stationCount && p.chefs >= p.maxChefs &&
                          p.waiters >= p.maxWaiters && p.tables >= p.maxTables && p.upgrades >= p.maxUpgrades);
            return maxed;
        }

        public const int VenueGems = 10;

        /// <summary>
        /// Hoàn thành mốc: nhận rương (số món theo mốc) + đá quý, rồi lên đời sang mốc tiếp theo.
        /// Xong mốc cuối thì thưởng thêm đá quý thành phố và lên xe sang thành phố mới.
        /// </summary>
        public void CompleteVenue()
        {
            if (!IsVenueComplete()) return;
            FinishVenue();
        }

        /// <summary>Chỉ để thử trong bản chơi thử: qua mốc ngay, bỏ qua điều kiện nâng hết.</summary>
        public void DebugSkipVenue() => FinishVenue();

        private void FinishVenue()
        {
            if (InEventRun) { FinishEventRun(); return; }
            QuestService.Track(State, QuestType.CompleteVenues, 1);
            AnalyticsService.Log("venue_complete", ("city", City.id), ("venue", VenueNumber),
                ("minutes", Math.Round(State.secondsInVenue / 60f, 1)), ("gear_profit", Math.Round(Bonuses.ProfitMultiplier, 2)));
            int chestItems = Mathf.Max(1, Venue.chestItems);
            string venueName = Venue.displayName;
            State.gems += VenueGems;

            string extra = "";
            if (!IsLastVenue)
            {
                State.venueIndex++;
                State.StartVenue(Venue);
            }
            else
            {
                State.gems += City.cityCompleteGems;
                State.citiesCompleted++;
                AnalyticsService.Log("city_complete", ("city", City.id), ("minutes", Math.Round(State.secondsInCity / 60f, 1)),
                    ("cities_done", State.citiesCompleted));
                extra = $" · xong {City.displayName}, +{City.cityCompleteGems} đá quý";
                if (City.nextCity != null) City = City.nextCity;
                else
                {
                    // Hết hành trình: quay về thành phố đầu, sang vòng sau, giá đắt hơn
                    State.loop++;
                    City = startCity;
                    extra += $" · bắt đầu VÒNG {LoopNumber} (giá +{(LoopCostFactor(Loop) - 1) * 100:0}%)";
                    AnalyticsService.Log("loop_start", ("loop", LoopNumber));
                }
                State.StartCity(City);
            }
            PendingOffline = BigNum.Zero;
            _completedNotified = false;
            ResetRun();

            OpenChest(chestItems, $"Xong {venueName} · rương {chestItems} món{extra}");
        }

        /// <summary>Nâng hết quán sự kiện: thưởng đá quý + rương sự kiện, về thành phố.</summary>
        private void FinishEventRun()
        {
            var e = CurrentEvent;
            string venueName = Venue.displayName;
            AnalyticsService.Log("event_run_complete", ("event", State.eventRunKey), ("minutes", Math.Round(State.secondsInVenue / 60f, 1)));
            State.gems += EventVenueFactory.CompleteGems;
            State.eventRunCompleted = true;
            LeaveEventRun(discard: true);
            OpenChest(EventVenueFactory.CompleteChestItems,
                $"Xong {venueName} · +{EventVenueFactory.CompleteGems} ♦ · rương sự kiện {EventVenueFactory.CompleteChestItems} món", Rarity.SuThi);
            if (e != null) GrantBlueprint(e.id);
        }

        public void ResetSave()
        {
            AnalyticsService.Log("save_reset");
            var old = State;
            SaveService.Delete();
            State = new GameState();
            // Đồ đã mua bằng tiền thật giữ lại (bản thật sẽ khôi phục từ Google Play)
            if (old != null)
            {
                State.noAds = old.noAds;
                State.starterPackBought = old.starterPackBought;
                State.monthlyCardDays = old.monthlyCardDays;
                State.monthlyLastClaim = old.monthlyLastClaim;
            }
            City = startCity;
            State.StartCity(City);
            State.EnsureEquippedSlots();
            PendingOffline = BigNum.Zero;
            LastRewards.Clear();
            _completedNotified = false;
            ResetRun();
            RefreshBonuses();
            GiveStarterGear();
        }

        // ---------- Nội bộ ----------

        private void ComputeOffline()
        {
            double seconds = SaveService.SecondsSinceLastSave(State);
            // Đánh dấu đã tính, để lần gọi tiếp theo (ví dụ OnApplicationPause ngay khi mở app) không cộng trùng.
            State.lastSaveUnix = SaveService.NowUnix;
            if (seconds < 60) return; // dưới 1 phút thì bỏ qua
            BigNum earned = EconomyService.OfflineEarnings(State, Venue, seconds, Bonuses,
                cookSpeedMultiplier: CookSpeedUpgrade, waiterSpeedMultiplier: WaiterSpeed);
            if (earned.IsZero) return;
            PendingOffline += earned;
            OfflineSeconds = Math.Min(seconds, EconomyService.MaxOfflineSeconds);
        }

        /// <summary>Hệ số giá món theo thứ tự mốc trong thành phố (trừ khi mốc có ghi đè).</summary>
        private void ApplyDefaultDishValues()
        {
            void Apply(CityData c)
            {
                if (c == null) return;
                for (int i = 0; i < c.venues.Count; i++)
                {
                    var v = c.venues[i];
                    if (v == null) continue;
                    v.dishValue = v.dishValueOverride > 0
                        ? v.dishValueOverride
                        : EconomyService.DefaultDishValue[Mathf.Min(i, EconomyService.DefaultDishValue.Length - 1)];
                }
            }
            if (allCities != null) foreach (var c in allCities) Apply(c);
            for (var c = startCity; c != null; c = c.nextCity) Apply(c);
        }

        private CityData FindCity(string id)
        {
            if (allCities != null)
                foreach (var c in allCities) if (c != null && c.id == id) return c;
            for (var c = startCity; c != null; c = c.nextCity) if (c.id == id) return c;
            return null;
        }

        private void EnsureStations()
        {
            // Thêm quầy mới nếu bản cập nhật game bổ sung quầy vào mốc.
            foreach (var s in Venue.stations) State.GetOrCreateStation(s.id);
        }
    }
}
