using System.Collections;
using System.Collections.Generic;
using PhoAmThucViet.Audio;
using PhoAmThucViet.Core;
using PhoAmThucViet.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PhoAmThucViet.Visual
{
    /// <summary>
    /// Vẽ cảnh quán: quầy, bàn, tiền bay lên bằng sprite 2D; đầu bếp, phục vụ, khách là nhân vật chibi 3D (Chibi.cs).
    /// Chỉ đọc trạng thái từ RestaurantController và không đổi kinh tế; khách và phục vụ là hoạt cảnh.
    /// Khi có hình thật: thay SpriteFactory.Block bằng sprite/prefab tương ứng, giữ nguyên logic di chuyển.
    /// </summary>
    [RequireComponent(typeof(RestaurantController))]
    public class RestaurantView : MonoBehaviour
    {
        /// <summary>Cảnh quán chiếm toàn màn hình; giao diện chỉ nằm đè lên ở mép trên và mép dưới.</summary>
        public const float SceneFraction = 1f;

        [Tooltip("Nửa chiều cao vùng nhìn tối thiểu (đơn vị thế giới).")]
        public float orthoSize = 5.6f;

        [Tooltip("Nửa chiều rộng muốn nhìn thấy (đơn vị thế giới). Màn dọc sẽ tự tăng chiều cao vùng nhìn.")]
        public float targetHalfWidth = 5.0f; // lùi camera: quán nhỏ lại, thấy cả đường phố phía trước

        /// <summary>Phần màn hình bị giao diện che ở mép trên / mép dưới (0–1). GameUI đặt giá trị này.</summary>
        public float topInset = 0.12f, bottomInset = 0.14f;
        private float _builtTopInset, _builtBottomInset;

        // ---------- Màu theo mốc (xe đẩy → nhà hàng lớn) ----------
        private static readonly Color[] FloorColors =
        {
            new Color(0.42f, 0.45f, 0.50f), // vỉa hè
            new Color(0.62f, 0.55f, 0.45f), // quán cóc
            new Color(0.76f, 0.64f, 0.48f), // ngõ nhỏ
            new Color(0.82f, 0.78f, 0.70f), // mặt phố, gạch hoa
            new Color(0.55f, 0.18f, 0.16f), // nhà hàng lớn, thảm đỏ
        };
        private static readonly Color[] WallColors =
        {
            new Color(0.25f, 0.27f, 0.30f),
            new Color(0.80f, 0.62f, 0.25f),
            new Color(0.85f, 0.70f, 0.30f),
            new Color(0.88f, 0.75f, 0.35f),
            new Color(0.30f, 0.10f, 0.08f),
        };

        private static readonly Color CounterColor = new Color(0.55f, 0.33f, 0.18f);
        private static readonly Color LockedColor = new Color(0.35f, 0.35f, 0.38f, 0.8f);
        private static readonly Color TableColor = new Color(0.45f, 0.28f, 0.15f);
        private static readonly Color ChefColor = new Color(0.95f, 0.95f, 0.95f);
        private static readonly Color WaiterColor = new Color(0.20f, 0.45f, 0.85f);
        private static readonly Color Gold = new Color(1f, 0.85f, 0.25f);
        private static readonly Color[] CustomerColors =
        {
            new Color(0.95f, 0.55f, 0.35f), new Color(0.40f, 0.75f, 0.45f), new Color(0.85f, 0.45f, 0.70f),
            new Color(0.95f, 0.80f, 0.30f), new Color(0.55f, 0.55f, 0.90f), new Color(0.30f, 0.70f, 0.75f),
        };

        // ---------- Đối tượng trong cảnh ----------

        private class StationView
        {
            public StationData data;
            public Vector2 pos, pickup;
            public float width;
            public SpriteRenderer counter, barFill, highlight;
            public bool counterIsPicture;
            public Vector3 counterScale;
            public float pulse;
            public TextMesh name, level, collect;
            public readonly List<SpriteRenderer> plates = new List<SpriteRenderer>();
            public readonly List<SpriteRenderer> stars = new List<SpriteRenderer>();
            public float starPulse; // nảy ngôi sao vừa sáng
            public SpriteRenderer plaque; // bảng tên món trên mặt quầy
            public GameObject prop, propLocked; // quầy 3D (đang mở / chưa mở); null nếu dùng hình vẽ counter_*.png
            public Transform bestBadge; // bảng "BÁN CHẠY" khi quầy đang là best seller
            public TextMesh bestText;
        }

        /// <summary>Một bàn trên sàn (kể cả bàn chưa mua, vẽ mờ) và khách đang ngồi.</summary>
        private class TableView
        {
            public Vector2 pos, seat;
            public SpriteRenderer sprite;
            public GameObject prop; // bàn 3D (hiện khi đã mua); null nếu dùng hình vẽ table_*.png
            public GameObject chair; // ghế 3D bên trái bàn (hiện khi đã mua bàn)
            public Color baseColor;
            public Transform customer;
            public Vector3 customerScale;
            public TextMesh bubble;
            public RestaurantController.TableStage lastStage;
            public Vector2 arriveFrom; // điểm trên vỉa hè khách bắt đầu rẽ vào quán
            public SpriteRenderer coin; // đồng xu tip trên mặt bàn
            public float coinPop;       // nảy khi vừa có tip
        }

        /// <summary>Hình một người làm, đi theo đúng việc của RestaurantController.Worker.</summary>
        private class WorkerView
        {
            public RestaurantController.Worker data;
            public Transform t;
            public Chibi chibi;
            public SpriteRenderer plate;
            public int taskId;
            public Vector2 startPos;
        }

        private RestaurantController _game;
        private Camera _cam;
        private Transform _root;
        private VenueData _builtVenue;
        private float _halfW;
        private Vector2 _door, _waiterHome, _chefHome, _passPoint;
        private readonly List<StationView> _stations = new List<StationView>();
        private readonly List<TableView> _tables = new List<TableView>();
        /// <summary>Khách ra về: 0 đi tới cửa → 1 ra vỉa hè → 2 đi dọc phố rồi biến mất ở mép màn hình.</summary>
        private class Leaver { public Transform t; public int stage; public float dir; }
        private readonly List<Leaver> _leaving = new List<Leaver>();

        /// <summary>Người đi đường / xe máy chạy qua phố (chỉ để trang trí).</summary>
        private class Walker { public Chibi chibi; public GameObject bike; public float dir, speed, y; }
        private readonly List<Walker> _walkers = new List<Walker>();
        private float _sidewalkY, _roadY, _streetEdge, _pedTimer, _bikeTimer;
        private int _walkerVariant;
        public const float StreetHeight = 1.9f; // lòng đường + vỉa hè ở đáy màn hình
        /// <summary>Tường chỉ còn dải mỏng trên cùng; khu bếp sâu hơn để đầu bếp không bị che khi nấu.</summary>
        private const float WallVisible = 0.6f;
        /// <summary>Khoảng từ mép trên vùng nhìn thấy xuống tâm dãy quầy.</summary>
        private const float CounterDepthFromTop = 2.9f;
        /// <summary>Đầu bếp đứng nấu cách tâm quầy (lùi vào bếp, không đè lên thanh nấu / đĩa món).</summary>
        private const float CookSpotBehind = 0.95f;
        private const float BarY = 0.38f; // thanh nấu: mép sau mặt quầy
        private readonly List<WorkerView> _workers = new List<WorkerView>();
        private int _colorIndex;
        private int _tier; // mốc quán đang dựng (0–4), chọn kiểu ghế cho khách

        /// <summary>Quầy người chơi vừa chạm vào (bảng dưới hiện bảng nâng cấp của quầy này).</summary>
        public StationData Selected { get; private set; }
        public void ClearSelection() => Selected = null;

        private void Awake()
        {
            _game = GetComponent<RestaurantController>();
            // Scene cũ có thể đã lưu giá trị 4,2 trên component: ép lùi camera để thấy đường phố
            if (targetHalfWidth < 5f) targetHalfWidth = 5f;
            _game.OnDelivered += HandleDelivered;
            _game.OnEventToken += HandleEventToken;
            _game.OnDishStarUp += HandleStarUp;
            _game.OnPerfectDish += HandlePerfectDish;
            _game.OnTipLeft += HandleTipLeft;
        }

        private void OnDestroy()
        {
            if (_game != null)
            {
                _game.OnDelivered -= HandleDelivered;
                _game.OnEventToken -= HandleEventToken;
                _game.OnDishStarUp -= HandleStarUp;
                _game.OnPerfectDish -= HandlePerfectDish;
                _game.OnTipLeft -= HandleTipLeft;
            }
        }

        private void Start() => SetupCamera();

        private void SetupCamera()
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                _cam = go.AddComponent<Camera>();
            }
            _cam.orthographic = true;
            _cam.orthographicSize = DesiredOrtho();
            _cam.transform.position = new Vector3(0, 0, -10);
            _cam.transform.rotation = Quaternion.identity;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
            _cam.rect = new Rect(0, 0, 1, 1);
        }

        private float DesiredOrtho() => _cam == null ? orthoSize : Mathf.Max(orthoSize, targetHalfWidth / Mathf.Max(0.1f, _cam.aspect));

        /// <summary>Đổi khung hình hoặc vùng giao diện che: cần dựng lại bố cục.</summary>
        private bool LayoutChanged()
        {
            if (_cam == null) return false;
            float want = DesiredOrtho();
            if (Mathf.Abs(_cam.orthographicSize - want) > 0.01f) _cam.orthographicSize = want;
            return Mathf.Abs(_cam.orthographicSize * _cam.aspect - _halfW) > 0.01f
                || Mathf.Abs(topInset - _builtTopInset) > 0.002f || Mathf.Abs(bottomInset - _builtBottomInset) > 0.002f;
        }

        /// <summary>
        /// Vị trí trên màn hình (pixel) của mép trên và mép dưới một quầy, để GameUI đặt ô nâng cấp nhỏ cạnh quầy.
        /// </summary>
        public bool TryGetStationScreenRect(StationData data, out Vector2 top, out Vector2 bottom)
        {
            top = bottom = Vector2.zero;
            if (_cam == null || _transitioning) return false;
            foreach (var sv in _stations)
            {
                if (sv.data != data) continue;
                top = _cam.WorldToScreenPoint(sv.pos + new Vector2(0, 2.0f)); // trên đầu đầu bếp
                bottom = _cam.WorldToScreenPoint(sv.pos + new Vector2(0, -1.0f));
                return true;
            }
            return false;
        }

        private void Update()
        {
            if (_game.State == null || _game.City == null || _transitioning) return;
            if (LayoutChanged() || _builtRun != _game.RunVersion)
                Build(_game.Venue); // đổi khung hình: dựng lại ngay, không hiệu ứng
            else if (_builtVenue != _game.Venue)
            {
                if (_builtVenue == null) Build(_game.Venue); // lần đầu mở game
                else
                {
                    // Vừa qua mốc (hoặc sang thành phố mới): chạy hiệu ứng lên đời
                    StartCoroutine(PlayLevelUp(_game.Venue, _builtCity != _game.City));
                    return;
                }
            }

            float dt = Time.deltaTime;
            if (EventService.Current?.Key != _decoKey) BuildEventDeco();
            HandleTap();
            UpdateStations(dt);
            UpdateTables();
            UpdateLeaving(dt);
            UpdateStreet(dt);
            SyncWorkers();
            UpdateWorkers(dt);
        }

        // ================= Dựng cảnh =================

        private void Build(VenueData venue)
        {
            if (_root != null) Destroy(_root.gameObject);
            _stations.Clear(); _tables.Clear(); _leaving.Clear(); _workers.Clear(); _walkers.Clear();
            Selected = null;
            _root = new GameObject("Restaurant Scene").transform;
            _builtVenue = venue;
            _builtCity = _game.City;
            _tier = Mathf.Clamp(_game.State.venueIndex, 0, FloorColors.Length - 1);
            _builtRun = _game.RunVersion;
            _decoKey = null; // trang trí sự kiện dựng lại cùng cảnh

            int vi = Mathf.Clamp(_game.State.venueIndex, 0, FloorColors.Length - 1);
            _halfW = _cam != null ? _cam.orthographicSize * _cam.aspect : 6f;
            float halfH = _cam != null ? _cam.orthographicSize : orthoSize;
            _builtTopInset = topInset; _builtBottomInset = bottomInset;
            float usable = Mathf.Min(_halfW, 7f) * 2f - 0.8f;
            // Vùng nhìn thấy được (không bị thanh trên cùng / thanh tab che)
            float top = halfH - 2 * halfH * topInset;
            float bottom = -halfH + 2 * halfH * bottomInset;

            // Sàn và tường bếp
            int tier = vi + 1; // tên file hình theo mốc: floor_1 … floor_5
            var floorSize = new Vector2(_halfW * 2 + 1, halfH * 2 + 1);
            if (SpriteLibrary.Get($"venues/floor_{tier}") != null)
                SpriteFactory.Picture("Floor", _root, Vector2.zero, floorSize, FloorColors[vi], 0, $"venues/floor_{tier}", out _, stretch: true);
            else
                SceneArt.Tiled("Floor", _root, Vector2.zero, floorSize, SceneArt.Floor(vi), 0); // nền vẽ bằng code theo mốc
            // Ba khu tách bạch: tường + sàn bếp phía sau quầy, dải sàn trước quầy, rồi sảnh ăn.
            // Tường bếp kéo lên tận mép trên màn hình (phía sau thanh thông tin)
            float wallBottom = top - WallVisible, wallTop = halfH + 0.5f;
            BuildZones(vi, top, _halfW * 2 + 1);
            _layoutTop = top;
            var wallPos = new Vector2(0, (wallTop + wallBottom) / 2);
            var wallSize = new Vector2(_halfW * 2 + 1, wallTop - wallBottom);
            if (SpriteLibrary.Get($"venues/wall_{tier}") != null)
                SpriteFactory.Picture("Kitchen", _root, wallPos, wallSize, WallColors[vi], 1, $"venues/wall_{tier}", out _, stretch: true);
            else
                BuildWallArt(vi, wallPos, wallSize, wallBottom, top, venue, usable);
            // Tên quán hiện trên thanh thông tin (GameUI) nên không vẽ bảng hiệu trong cảnh nữa.

            // Quầy
            int n = venue.stations.Count;
            float slot = usable / Mathf.Max(1, n);
            float counterW = Mathf.Min(slot - 0.2f, 2.4f);
            for (int i = 0; i < n; i++)
            {
                var sv = new StationView { data = venue.stations[i], width = counterW };
                sv.pos = new Vector2(-usable / 2 + slot * (i + 0.5f), top - CounterDepthFromTop);
                sv.pickup = sv.pos + new Vector2(0, -0.9f);
                sv.highlight = SpriteFactory.Block("Highlight", _root, sv.pos + new Vector2(0, 0.25f), new Vector2(counterW + 0.25f, 1.55f), Gold, 3);
                sv.highlight.gameObject.SetActive(false);
                sv.counter = SpriteFactory.Picture("Counter " + sv.data.displayName, _root, sv.pos, new Vector2(counterW, 0.8f),
                    CounterColor, 3, $"venues/counter_{tier}", out sv.counterIsPicture);
                sv.counterScale = sv.counter.transform.localScale;
                if (!sv.counterIsPicture)
                {
                    // Quầy 3D: bản đang mở (có nồi/chảo/xửng/vỉ nướng xoay vòng theo quầy) và bản chưa mở (xám)
                    sv.counter.enabled = false;
                    sv.prop = Props.Counter(_root, sv.pos, vi, counterW, i, false);
                    sv.propLocked = Props.Counter(_root, sv.pos, vi, counterW, i, true);
                }
                // Hình món ăn trên quầy (food/<tên không dấu>.png), nếu có
                var food = SpriteFactory.Picture("Food", _root, sv.pos + new Vector2(0, 0.05f), new Vector2(0.7f, 0.6f), Color.clear, 7,
                    "food/" + SpriteLibrary.Slug(sv.data.displayName), out bool hasFood);
                if (!hasFood) Destroy(food.gameObject);
                // Bảng tên món gắn trên mặt trước quầy (giấy viền đỏ / ván gỗ / biển men xanh / sơn mài đỏ)
                int nameLen = Mathf.Max(4, sv.data.displayName.Length);
                float nameSize = Mathf.Min(0.22f, counterW * 0.82f / (nameLen * 0.55f));
                Vector2 namePos = sv.pos + new Vector2(0, -0.18f);
                float plaqueW = Mathf.Min(counterW * 0.95f, nameLen * nameSize * 0.55f + 0.32f);
                sv.plaque = SceneArt.Sliced("Plaque", _root, namePos, new Vector2(plaqueW, 0.34f), SceneArt.Plaque(vi), 9);
                sv.name = SpriteFactory.Text("Name", _root, namePos + new Vector2(0, 0.01f), sv.data.displayName, nameSize, SceneArt.PlaqueText(vi), 10);
                sv.level = SpriteFactory.Text("Level", _root, sv.pos + new Vector2(0, -0.6f), "", 0.2f, Color.white, 10);
                // Thanh nấu nằm sát mép sau mặt quầy (không đè lên người đầu bếp)
                SpriteFactory.Block("BarBg", _root, sv.pos + new Vector2(0, BarY), new Vector2(counterW * 0.9f, 0.08f), new Color(0, 0, 0, 0.35f), 9);
                sv.barFill = SpriteFactory.Block("BarFill", _root, sv.pos + new Vector2(0, BarY), new Vector2(0, 0.08f), Gold, 10);
                sv.collect = SpriteFactory.Text("Collect", _root, sv.pos + new Vector2(0, 0.95f), "", 0.26f, Gold, 11);
                // Bảng "BÁN CHẠY" trên đầu đầu bếp (skill best seller)
                var badge = new GameObject("BestSeller").transform;
                badge.SetParent(_root, false);
                badge.localPosition = sv.pos + new Vector2(0, 2.2f);
                SpriteFactory.Block("Bg", badge, Vector2.zero, new Vector2(1.75f, 0.4f), new Color(0.85f, 0.15f, 0.10f, 0.95f), 12, circle: false);
                sv.bestText = SpriteFactory.Text("Text", badge, new Vector2(0, 0.01f), "BÁN CHẠY", 0.21f, Gold, 13);
                badge.gameObject.SetActive(false);
                sv.bestBadge = badge;
                // 3 ô sao món ăn ngay dưới dòng cấp
                float starY = sv.pos.y - 0.86f;
                for (int k = 0; k < DishStars.StarsPerTier; k++)
                {
                    var star = SpriteFactory.Block("Star", _root, new Vector2(sv.pos.x + (k - 1) * 0.26f, starY), new Vector2(0.24f, 0.24f), Color.white, 11);
                    SpriteFactory.SetSprite(star, SpriteFactory.Star);
                    sv.stars.Add(star);
                }
                // Đĩa món đã nấu xong nằm chờ trên quầy (tối đa 3 đĩa hiện ra)
                for (int k = 0; k < 3; k++)
                {
                    float px = sv.pos.x + (k - 1) * Mathf.Min(0.32f, counterW / 3.2f);
                    var plate = SpriteFactory.Picture("ReadyPlate", _root, new Vector2(px, sv.pos.y + 0.18f), new Vector2(0.26f, 0.26f),
                        Color.white, 8, "props/plate", out _, circle: true);
                    plate.gameObject.SetActive(false);
                    sv.plates.Add(plate);
                }
                _stations.Add(sv);
            }

            // Bàn
            int tables = _game.MaxTables; // vẽ đủ chỗ; bàn chưa mua hiện mờ
            // Màn dọc: ít cột, nhiều hàng, giãn đều trong khoảng giữa quầy và cửa
            int cols = Mathf.Min(tables, usable >= 9f ? 5 : usable >= 6.5f ? 4 : 3);
            int rows = Mathf.CeilToInt(tables / (float)cols);
            float colW = usable / cols;
            float streetTop = bottom + StreetHeight; // mặt tiền quán
            float areaTop = top - CounterDepthFromTop - 2.1f, areaBottom = streetTop + 1.45f;
            float rowGap = rows > 1 ? Mathf.Clamp((areaTop - areaBottom) / (rows - 1), 1.5f, 2.6f) : 0;
            float firstRow = Mathf.Min(areaTop, (areaTop + areaBottom) / 2 + rowGap * (rows - 1) / 2);
            for (int t = 0; t < tables; t++)
            {
                int r = t / cols, c = t % cols;
                int inRow = Mathf.Min(cols, tables - r * cols); // hàng cuối ít bàn hơn thì canh giữa
                float rowW = colW * inRow;
                var pos = new Vector2(-rowW / 2 + colW * (c + 0.5f), firstRow - r * rowGap);
                // Bàn hẹp hơn một chút để chừa chỗ đặt ghế bên trái
                float tableW = Mathf.Clamp(colW - 0.7f, 0.6f, 1.0f);
                var sprite = SpriteFactory.Picture("Table", _root, pos, new Vector2(tableW, 0.8f), TableColor, 2,
                    $"venues/table_{tier}", out bool tablePic);
                var bubble = SpriteFactory.Text("Bubble", _root, pos + new Vector2(0, 0.75f), "", 0.24f, Color.white, 12);
                var tv = new TableView
                {
                    pos = pos, seat = pos + new Vector2(-(tableW / 2 + 0.2f), -0.1f), sprite = sprite, bubble = bubble,
                    baseColor = tablePic ? Color.white : TableColor,
                };
                if (!tablePic) tv.prop = Props.Table(_root, pos, vi, tableW); // bàn 3D theo mốc
                tv.chair = Props.Chair(_root, tv.seat, vi, t);
                tv.coin = SceneArt.Place("TipCoin", _root, CoinPos(tv), SceneArt.Coin(), 0.34f, 12);
                tv.coin.gameObject.SetActive(false);
                _tables.Add(tv);
            }

            // Đường phố, mặt tiền và cửa ra vào
            BuildStreet(vi, bottom, streetTop, halfH);
            _door = new Vector2(0, streetTop + 0.1f);
            SceneArt.Place("DoorMat", _root, _door + new Vector2(0, 0.3f), SceneArt.DoorMat(vi), 0.45f, 1);
            // Chậu cây hai bên cửa (trong quán) và ở hai góc
            float inner = Mathf.Min(_halfW, 7f) - 0.55f;
            Props.Plant(_root, _door + new Vector2(-1.25f, 0.35f), vi, vi);
            Props.Plant(_root, _door + new Vector2(1.25f, 0.35f), vi, vi + 1);
            Props.Plant(_root, new Vector2(-inner, streetTop + 0.45f), vi, vi + 2, 1.1f);
            Props.Plant(_root, new Vector2(inner, streetTop + 0.45f), vi, vi + 2, 1.1f);
            float counterY = top - CounterDepthFromTop;
            _waiterHome = new Vector2(-usable / 2 + 0.4f, counterY - 1.15f);  // phục vụ chờ trên dải trước quầy
            _chefHome = new Vector2(usable / 2 - 0.35f, counterY + 1.25f);    // đầu bếp rảnh tay đứng chờ trong bếp
            _passPoint = new Vector2(0, counterY - 0.8f); // chỗ lấy khay trước dãy quầy
        }

        // ================= Đường phố =================

        /// <summary>
        /// Lòng đường (vạch kẻ giữa), lề đường, vỉa hè lát gạch và bức tường mặt tiền thấp chừa lối vào ở giữa.
        /// Vỉa hè có cây xanh, xe máy đỗ; vỉa hè / quán cóc thêm chồng két nhựa.
        /// </summary>
        private void BuildStreet(int vi, float bottom, float streetTop, float halfH)
        {
            float width = _halfW * 2 + 1;
            float curbY = bottom + 1.05f;
            _roadY = bottom + 0.52f;
            _sidewalkY = (curbY + streetTop) / 2;
            _streetEdge = _halfW + 0.8f;

            // Lòng đường kéo xuống tận đáy màn hình (phần dưới nằm sau giao diện)
            float roadBottom = -halfH - 1f;
            SpriteFactory.Block("RoadFill", _root, new Vector2(0, (roadBottom + bottom + 0.02f) / 2), new Vector2(width, bottom + 0.02f - roadBottom),
                new Color(0.40f, 0.41f, 0.43f), 1);
            SceneArt.Tiled("Road", _root, new Vector2(0, _roadY), new Vector2(width, 1.0f), SceneArt.Road(), 1);
            SpriteFactory.Block("Curb", _root, new Vector2(0, curbY), new Vector2(width, 0.08f), new Color(0.72f, 0.72f, 0.70f), 2);
            SpriteFactory.Block("CurbShadow", _root, new Vector2(0, curbY - 0.07f), new Vector2(width, 0.06f), new Color(0, 0, 0, 0.15f), 2);
            SceneArt.Tiled("Sidewalk", _root, new Vector2(0, (curbY + streetTop) / 2), new Vector2(width, streetTop - curbY), SceneArt.Sidewalk(), 1);

            // Tường mặt tiền thấp, chừa lối vào rộng 1,6 ở giữa
            const float gap = 0.8f, wallH = 0.26f;
            float segW = width / 2 - gap;
            var wallTex = SceneArt.Wall(vi);
            var cap = vi == 4 ? new Color(0.85f, 0.68f, 0.32f) : new Color(0.82f, 0.78f, 0.72f);
            for (int side = -1; side <= 1; side += 2)
            {
                float cx = side * (gap + segW / 2);
                SceneArt.Tiled("Facade", _root, new Vector2(cx, streetTop + wallH / 2), new Vector2(segW, wallH), wallTex, 2);
                SpriteFactory.Block("FacadeCap", _root, new Vector2(cx, streetTop + wallH + 0.03f), new Vector2(segW, 0.06f), cap, 2);
                SpriteFactory.Block("DoorPost", _root, new Vector2(side * gap, streetTop + 0.2f), new Vector2(0.1f, 0.46f), new Color(0.32f, 0.22f, 0.15f), 2);
            }
            SpriteFactory.Block("FacadeShadow", _root, new Vector2(0, streetTop - 0.05f), new Vector2(width, 0.1f), new Color(0, 0, 0, 0.12f), 1);

            // Đồ trên vỉa hè
            float edge = _halfW - 0.6f;
            Props.Tree(_root, new Vector2(-edge, curbY + 0.12f), 0);
            Props.Tree(_root, new Vector2(edge, curbY + 0.12f), 1);
            Props.Motorbike(_root, new Vector2(edge - 1.0f, curbY + 0.2f), vi, false);
            if (vi <= 2) Props.Motorbike(_root, new Vector2(-edge + 1.0f, curbY + 0.2f), vi + 1, true);
            if (vi <= 1) Props.Crates(_root, new Vector2(-edge + 1.9f, streetTop - 0.25f));
        }

        /// <summary>Người đi bộ dọc vỉa hè và xe máy chạy trên đường, sinh ra ở mép màn hình rồi đi qua.</summary>
        private void UpdateStreet(float dt)
        {
            if (_root == null) return;
            _pedTimer -= dt;
            _bikeTimer -= dt;
            int peds = 0, bikes = 0;
            foreach (var w in _walkers) { if (w.bike != null) bikes++; else peds++; }
            if (_pedTimer <= 0 && peds < 4)
            {
                _pedTimer = Random.Range(1.8f, 4.5f);
                float dir = Random.value < 0.5f ? 1 : -1;
                float y = _sidewalkY + Random.Range(-0.12f, 0.12f);
                var c = Chibi.Create(_root, ChibiKind.Customer, _walkerVariant++ % ChibiLooks.CustomerVariants, new Vector2(-dir * _streetEdge, y));
                _walkers.Add(new Walker { chibi = c, dir = dir, speed = Random.Range(0.9f, 1.5f), y = y });
            }
            if (_bikeTimer <= 0 && bikes < 2)
            {
                _bikeTimer = Random.Range(3.5f, 8f);
                float dir = Random.value < 0.5f ? 1 : -1;
                float y = _roadY + (dir > 0 ? -0.22f : 0.2f); // đi bên phải đường
                var start = new Vector2(-dir * _streetEdge, y);
                var bike = Props.Motorbike(_root, start, Random.Range(0, 4), dir > 0);
                var rider = Chibi.Create(_root, ChibiKind.Customer, _walkerVariant++ % ChibiLooks.CustomerVariants, start);
                rider.useStool = false;
                rider.seatLift = Props.BikeSeatHeight / rider.transform.localScale.y - 0.08f;
                rider.faceYaw = dir > 0 ? 90f : 270f;
                rider.action = ChibiAction.Sit;
                _walkers.Add(new Walker { chibi = rider, bike = bike, dir = dir, speed = Random.Range(3.2f, 4.8f), y = y });
            }

            for (int i = _walkers.Count - 1; i >= 0; i--)
            {
                var w = _walkers[i];
                if (w.chibi == null) { if (w.bike != null) Destroy(w.bike); _walkers.RemoveAt(i); continue; }
                var p = (Vector2)w.chibi.transform.localPosition;
                p.x += w.dir * w.speed * dt;
                p.y = w.y;
                w.chibi.transform.localPosition = new Vector3(p.x, p.y, 0);
                if (w.bike != null) Props.MoveTo(w.bike.transform, p + new Vector2(-w.dir * 0.03f, 0));
                if (Mathf.Abs(p.x) > _streetEdge + 0.1f && Mathf.Sign(p.x) == Mathf.Sign(w.dir))
                {
                    Destroy(w.chibi.gameObject);
                    if (w.bike != null) Destroy(w.bike);
                    _walkers.RemoveAt(i);
                }
            }
        }

        /// <summary>Đưa khách vào danh sách ra về (đi ra cửa, rồi ra phố).</summary>
        private void Leave(Transform t)
        {
            if (t == null) return;
            _leaving.Add(new Leaver { t = t, dir = t.localPosition.x < 0 ? -1 : 1 });
        }

        // ================= Tường vẽ bằng code =================

        private static readonly string[] ScrollWords = { "Phúc", "Lộc", "Thọ", "An", "Khang" };

        /// <summary>
        /// Tường bếp theo mốc: cửa cuốn / mành tre / tường vôi vàng / gạch men / ốp gỗ đỏ,
        /// kèm chân tường, mái hiên sọc (vỉa hè, quán cóc) và đồ treo giữa các quầy (đèn, kệ, cửa sổ, bảng thực đơn…).
        /// </summary>
        private void BuildWallArt(int vi, Vector2 wallPos, Vector2 wallSize, float wallBottom, float top, VenueData venue, float usable)
        {
            SceneArt.Tiled("Kitchen", _root, wallPos, wallSize, SceneArt.Wall(vi), 1);
            // Chân tường tối màu
            var baseColor = vi == 4 ? new Color(0.22f, 0.06f, 0.04f) : vi == 3 ? new Color(0.45f, 0.47f, 0.50f) : new Color(0.30f, 0.22f, 0.16f);
            SpriteFactory.Block("Baseboard", _root, new Vector2(0, wallBottom + 0.07f), new Vector2(wallSize.x, 0.14f), baseColor, 2);
            if (vi <= 1)
                SceneArt.Tiled("Awning", _root, new Vector2(0, top - 0.06f), new Vector2(wallSize.x, 0.32f), SceneArt.Awning(vi), 2);

            // Đồ treo ở khoảng giữa các quầy (và hai đầu dãy)
            int n = venue.stations.Count;
            float slot = usable / Mathf.Max(1, n);
            var set = SceneArt.WallDecoSet(vi);
            float y = top - (vi <= 1 ? 0.38f : 0.3f); // tường chỉ còn dải mỏng: đồ treo nhỏ gọn
            for (int g = 0; g <= n; g++)
            {
                if (n >= 3 && g % 2 == 1) continue; // bớt đồ treo cho tường thoáng, đỡ rối mắt
                float x = -usable / 2 + slot * g;
                string kind = set[g % set.Length];
                var sprite = SceneArt.Deco(kind);
                switch (kind)
                {
                    case "bulb":
                        SceneArt.Place("Bulb", _root, new Vector2(x, top - (vi <= 1 ? 0.2f : 0.02f)), sprite, 0.42f, 2);
                        break;
                    case "lantern":
                        SceneArt.Place("Lantern", _root, new Vector2(x, top - 0.02f), sprite, 0.45f, 2);
                        break;
                    case "menu":
                    {
                        SceneArt.Place("Menu", _root, new Vector2(x, y), sprite, 0.5f, 2);
                        SpriteFactory.Text("MenuTitle", _root, new Vector2(x, y + 0.08f), "THỰC", 0.09f, new Color(1f, 0.9f, 0.5f), 3);
                        SpriteFactory.Text("MenuTitle2", _root, new Vector2(x, y - 0.06f), "ĐƠN", 0.09f, new Color(1f, 0.9f, 0.5f), 3);
                        break;
                    }
                    case "scroll":
                        SceneArt.Place("Scroll", _root, new Vector2(x, y), sprite, 0.55f, 2);
                        SpriteFactory.Text("Word", _root, new Vector2(x, y), ScrollWords[g % ScrollWords.Length], 0.11f, new Color(1f, 0.85f, 0.4f), 3);
                        break;
                    case "window_green":
                    case "window_glass":
                    case "window_round":
                        SceneArt.Place("Window", _root, new Vector2(x, y), sprite, Mathf.Min(0.52f, slot * 0.5f), 2);
                        break;
                    case "shelf":
                        SceneArt.Place("Shelf", _root, new Vector2(x, y), sprite, Mathf.Min(0.36f, slot * 0.3f), 2);
                        break;
                    case "clock":
                        SceneArt.Place("Clock", _root, new Vector2(x, y), sprite, 0.34f, 2);
                        break;
                    default:
                        SceneArt.Place(kind, _root, new Vector2(x, y), sprite, 0.4f, 2);
                        break;
                }
            }
        }

        /// <summary>
        /// Sàn khu bếp (sau quầy) và dải sàn trước quầy, có gờ viền để tách khỏi sảnh ăn:
        ///   tường (đến top − 1,15) · sàn bếp (đến top − 2,52, ngay mép trước quầy) · dải quầy (đến top − 3,2) · sảnh.
        /// </summary>
        private void BuildZones(int vi, float top, float width)
        {
            float counterY = top - CounterDepthFromTop;
            float kitchenTop = top - WallVisible, counterFront = counterY - 0.42f, stripBottom = counterY - 1.1f;
            SceneArt.Tiled("KitchenFloor", _root, new Vector2(0, (kitchenTop + counterFront) / 2), new Vector2(width, kitchenTop - counterFront),
                SceneArt.KitchenFloor(vi), 1);
            SceneArt.Tiled("CounterStrip", _root, new Vector2(0, (counterFront + stripBottom) / 2), new Vector2(width, counterFront - stripBottom),
                SceneArt.ServiceStrip(vi), 1);
            // Gờ viền: mép trên dải quầy (sáng) và bậc xuống sảnh (bóng tối nhẹ + chỉ màu theo mốc)
            var trim = vi == 4 ? new Color(0.85f, 0.68f, 0.32f) : vi == 3 ? new Color(0.55f, 0.62f, 0.70f) : new Color(0.45f, 0.32f, 0.22f);
            SpriteFactory.Block("StripEdgeTop", _root, new Vector2(0, counterFront - 0.02f), new Vector2(width, 0.05f), new Color(1, 1, 1, 0.25f), 2);
            SpriteFactory.Block("StripEdge", _root, new Vector2(0, stripBottom + 0.03f), new Vector2(width, 0.07f), trim, 2);
            SpriteFactory.Block("StripShadow", _root, new Vector2(0, stripBottom - 0.07f), new Vector2(width, 0.14f), new Color(0, 0, 0, 0.12f), 1);
        }

        // ================= Hiệu ứng lên đời =================

        private bool _transitioning;
        private CityData _builtCity;
        private int _builtRun = -1;

        // ================= Trang trí sự kiện theo mùa =================

        private float _layoutTop;
        private string _decoKey;
        private Transform _deco;

        /// <summary>Có sự kiện thì treo dòng chúc mừng và dây đèn lồng trên tường bếp.</summary>
        private void BuildEventDeco()
        {
            if (_deco != null) Destroy(_deco.gameObject);
            _deco = null;
            var e = EventService.Current;
            _decoKey = e?.Key;
            if (e == null || _root == null) return;

            _deco = new GameObject("Event Deco").transform;
            _deco.SetParent(_root, false);
            var c = new Color(e.color[0], e.color[1], e.color[2]);
            float y = _layoutTop - 0.3f;
            SpriteFactory.Block("Banner", _deco, new Vector2(0, y), new Vector2(Mathf.Min(_halfW * 2 - 0.4f, 7f), 0.5f), c, 4);
            SpriteFactory.Text("Greeting", _deco, new Vector2(0, y), e.greeting, 0.26f, new Color(1f, 0.9f, 0.4f), 5);
            // Dây đèn lồng dọc tường
            int count = Mathf.Max(4, Mathf.RoundToInt(_halfW * 2 / 0.9f));
            for (int i = 0; i < count; i++)
            {
                float x = -_halfW + 0.45f + i * (_halfW * 2 - 0.9f) / Mathf.Max(1, count - 1);
                if (Mathf.Abs(x) < 2.2f) continue; // chừa chỗ dòng chúc mừng
                var lamp = SpriteFactory.Block("Lantern", _deco, new Vector2(x, y), new Vector2(0.22f, 0.28f),
                    i % 2 == 0 ? c : new Color(1f, 0.8f, 0.2f), 4);
                SpriteFactory.SetSprite(lamp, SpriteFactory.Circle);
            }
        }

        // ================= Món lên sao =================

        /// <summary>Màu sao theo bậc: xanh lục, lam, vàng, tím, đỏ.</summary>
        private static readonly Color[] StarColors =
        {
            new Color(0.35f, 0.9f, 0.35f), new Color(0.3f, 0.6f, 1f), new Color(1f, 0.85f, 0.2f),
            new Color(0.8f, 0.45f, 1f), new Color(1f, 0.3f, 0.25f),
        };

        private void HandleStarUp(StationData data, int doublings, int gems)
        {
            foreach (var sv in _stations)
            {
                if (sv.data != data) continue;
                sv.starPulse = 0.6f;
                StartCoroutine(StarBurst(sv, doublings, gems));
                return;
            }
        }

        /// <summary>Ngôi sao lớn bung ra trên quầy, xoay rồi mờ dần; chữ "x2 giá món · +1 ♦" bay lên.</summary>
        private IEnumerator StarBurst(StationView sv, int doublings, int gems)
        {
            var color = StarColors[Mathf.Min(DishStars.Tier(doublings), StarColors.Length - 1)];
            bool newColor = doublings > 1 && DishStars.Lit(doublings) == 1;
            SoundFX.Play(Sfx.LevelUp, 0.6f);
            string text = newColor ? $"Sao đổi màu {DishStars.TierNames[Mathf.Min(DishStars.Tier(doublings), DishStars.TierNames.Length - 1)]}! +{gems} ♦" : $"Món lên sao! Giá x2 · +{gems} ♦";
            FloatingText.Spawn(_root, sv.pos + new Vector2(0, 2.0f), text, color, 0.3f);

            var root = _root;
            var big = SpriteFactory.Block("StarBurst", root, sv.pos + new Vector2(0, 0.6f), Vector2.one, color, 30);
            SpriteFactory.SetSprite(big, SpriteFactory.Star);
            var ring = SpriteFactory.Block("StarRing", root, sv.pos + new Vector2(0, 0.6f), Vector2.one, new Color(color.r, color.g, color.b, 0.5f), 29, circle: true);
            const float dur = 0.9f;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                if (big == null || root == null) yield break; // cảnh vừa dựng lại
                float k = t / dur;
                float s = k < 0.3f ? Mathf.Lerp(0, 1.6f, EaseOutBack(k / 0.3f)) : 1.6f;
                big.transform.localScale = new Vector3(s, s, 1);
                big.transform.localRotation = Quaternion.Euler(0, 0, k * 180f);
                big.color = new Color(color.r, color.g, color.b, k < 0.6f ? 1 : 1 - (k - 0.6f) / 0.4f);
                float r = Mathf.Lerp(0.5f, 3f, k);
                ring.transform.localScale = new Vector3(r, r, 1);
                ring.color = new Color(color.r, color.g, color.b, 0.5f * (1 - k));
                yield return null;
            }
            if (big != null) Destroy(big.gameObject);
            if (ring != null) Destroy(ring.gameObject);
        }

        // ================= Món hoàn hảo =================

        private readonly Dictionary<StationData, float> _perfectShownAt = new Dictionary<StationData, float>();

        /// <summary>Món hoàn hảo (bán x6): chữ hồng bay lên ở quầy, mỗi quầy tối đa 1 lần / 0,6 giây cho đỡ rối.</summary>
        private void HandlePerfectDish(StationData data, RestaurantController.Worker w)
        {
            if (_root == null) return;
            if (_perfectShownAt.TryGetValue(data, out float last) && Time.time - last < 0.6f) return;
            _perfectShownAt[data] = Time.time;
            foreach (var sv in _stations)
                if (sv.data == data)
                {
                    FloatingText.Spawn(_root, sv.pos + new Vector2(0, 2.0f), "Hoàn hảo! x6", new Color(1f, 0.45f, 0.75f), 0.24f);
                    return;
                }
        }

        private void HandleEventToken(RestaurantController.TableRun table, int amount, SeasonEvent e)
        {
            if (_root == null || table.index >= _tables.Count) return;
            var c = new Color(e.color[0], e.color[1], e.color[2]);
            FloatingText.Spawn(_root, _tables[table.index].pos + new Vector2(0, 1.25f), $"+{amount} {e.token}", Color.Lerp(c, Color.white, 0.3f), 0.26f);
        }

        private static readonly Color[] ConfettiColors =
        {
            new Color(1f, 0.85f, 0.25f), new Color(0.95f, 0.35f, 0.30f), new Color(0.35f, 0.75f, 0.45f),
            new Color(0.40f, 0.60f, 0.95f), new Color(0.95f, 0.55f, 0.80f), Color.white,
        };

        private struct Confetti
        {
            public Transform t;
            public Vector2 velocity;
            public float spin;
        }

        /// <summary>
        /// Quán cũ thu nhỏ và tối dần → bảng "LÊN ĐỜI!" bật ra kèm pháo giấy → quán mới phóng to hiện ra.
        /// Sang thành phố mới thì bảng ghi "LÊN XE ĐI ...!".
        /// </summary>
        private IEnumerator PlayLevelUp(VenueData next, bool newCity)
        {
            _transitioning = true;
            var fx = new GameObject("Level Up FX").transform;
            float halfH = _cam != null ? _cam.orthographicSize : orthoSize;
            float w = _halfW * 2 + 2, h = halfH * 2 + 2;
            var overlay = SpriteFactory.Block("Overlay", fx, Vector2.zero, new Vector2(w, h), new Color(0, 0, 0, 0), 100);

            // 1) Quán cũ thu nhỏ, màn hình tối dần
            var oldRoot = _root;
            for (float t = 0; t < 0.45f; t += Time.deltaTime)
            {
                float k = t / 0.45f;
                if (oldRoot != null) oldRoot.localScale = Vector3.one * Mathf.Lerp(1f, 0.85f, k);
                overlay.color = new Color(0, 0, 0, 0.75f * k);
                yield return null;
            }

            // Dựng quán mới (ẩn nhỏ phía sau lớp tối)
            Build(next);
            _root.localScale = Vector3.one * 0.6f;

            // 2) Bảng "LÊN ĐỜI!" + pháo giấy
            string title = !newCity ? "LÊN ĐỜI!"
                : _game.Loop > 0 && _game.City == _game.startCity ? $"VÒNG {_game.LoopNumber}: {_game.City.displayName.ToUpper()}!"
                : $"LÊN XE ĐI {_game.City.displayName.ToUpper()}!";
            var banner = SpriteFactory.Text("Banner", fx, new Vector2(0, 0.6f), title, newCity ? 0.75f : 1.0f, Gold, 102);
            var sub = SpriteFactory.Text("Sub", fx, new Vector2(0, -0.5f), next.displayName, 0.5f, Color.white, 102);
            var bg = SpriteFactory.Block("BannerBg", fx, new Vector2(0, 0.1f), new Vector2(w, 2.6f), new Color(0.55f, 0.12f, 0.10f, 0.95f), 101);
            SoundFX.Play(Sfx.Chest);

            var confetti = new List<Confetti>();
            for (int i = 0; i < 45; i++)
            {
                var piece = SpriteFactory.Block("Confetti", fx, new Vector2(Random.Range(-_halfW, _halfW), halfH + Random.Range(0f, 2f)),
                    new Vector2(0.18f, 0.28f), ConfettiColors[i % ConfettiColors.Length], 103);
                confetti.Add(new Confetti
                {
                    t = piece.transform,
                    velocity = new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(-1f, 0.5f)),
                    spin = Random.Range(-540f, 540f),
                });
            }

            for (float t = 0; t < 1.7f; t += Time.deltaTime)
            {
                // Bảng bật ra: 0 → 1,15 → 1
                float pop = t < 0.18f ? Mathf.Lerp(0, 1.15f, t / 0.18f) : Mathf.Lerp(1.15f, 1f, Mathf.Min(1, (t - 0.18f) / 0.15f));
                banner.transform.localScale = Vector3.one * pop;
                sub.transform.localScale = Vector3.one * Mathf.Min(1, pop);
                bg.transform.localScale = new Vector3(w, 2.6f * Mathf.Min(1, pop), 1);
                UpdateConfetti(confetti, Time.deltaTime);
                yield return null;
            }

            // 3) Quán mới phóng to hiện ra, lớp tối và bảng mờ dần
            for (float t = 0; t < 0.5f; t += Time.deltaTime)
            {
                float k = t / 0.5f;
                _root.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, EaseOutBack(k));
                float a = 1 - k;
                overlay.color = new Color(0, 0, 0, 0.75f * a);
                banner.color = new Color(Gold.r, Gold.g, Gold.b, a);
                sub.color = new Color(1, 1, 1, a);
                bg.color = new Color(0.55f, 0.12f, 0.10f, 0.95f * a);
                UpdateConfetti(confetti, Time.deltaTime);
                yield return null;
            }

            _root.localScale = Vector3.one;
            Destroy(fx.gameObject);
            _transitioning = false;
        }

        private void UpdateConfetti(List<Confetti> pieces, float dt)
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                var c = pieces[i];
                c.velocity += Vector2.down * (4f * dt);           // trọng lực nhẹ
                c.velocity.x += Mathf.Sin(Time.time * 3 + i) * dt; // lắc lư khi rơi
                c.t.localPosition += (Vector3)(c.velocity * dt);
                c.t.Rotate(0, 0, c.spin * dt);
                pieces[i] = c;
            }
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1;
            return 1 + c3 * Mathf.Pow(x - 1, 3) + c1 * Mathf.Pow(x - 1, 2);
        }

        // ================= Quầy =================

        private void UpdateStations(float dt)
        {
            foreach (var sv in _stations)
            {
                var st = _game.State.GetStation(sv.data.id);
                int level = st?.level ?? 0;
                bool unlocked = level > 0;
                sv.counter.color = sv.counterIsPicture
                    ? (unlocked ? Color.white : new Color(0.45f, 0.45f, 0.45f, 0.9f))
                    : (unlocked ? CounterColor : LockedColor);
                sv.plaque.color = unlocked ? Color.white : new Color(0.62f, 0.62f, 0.62f, 1f);
                if (sv.prop != null)
                {
                    if (sv.prop.activeSelf != unlocked) sv.prop.SetActive(unlocked);
                    if (sv.propLocked.activeSelf == unlocked) sv.propLocked.SetActive(!unlocked);
                }
                sv.level.text = unlocked ? $"Cấp {level}/{sv.data.maxLevel}" : "Chưa mở";

                // Thanh nấu: tiến độ món đang nấu ở quầy này (ai nấu cũng được)
                float p = 0;
                foreach (var w in _game.Workers)
                    if (w.task == RestaurantController.TaskKind.Cook && w.station == sv.data) p = Mathf.Max(p, w.Progress);
                float full = sv.width * 0.9f;
                sv.barFill.transform.localScale = new Vector3(full * p, 0.08f, 1);
                sv.barFill.transform.localPosition = sv.pos + new Vector2(-full / 2 + full * p / 2, BarY);


                // Sao món ăn: sáng theo số mốc x2 đã đạt, màu theo bậc (xanh lục → lam → vàng → tím → đỏ)
                int doublings = unlocked ? EconomyService.DoublingsReached(sv.data, level) : 0;
                int lit = DishStars.Lit(doublings);
                int slots = DishStars.Slots(doublings, EconomyService.DoublingsReached(sv.data, sv.data.maxLevel));
                var starColor = StarColors[Mathf.Min(DishStars.Tier(doublings), StarColors.Length - 1)];
                sv.starPulse = Mathf.Max(0, sv.starPulse - dt);
                for (int si = 0; si < sv.stars.Count; si++)
                {
                    var starSr = sv.stars[si];
                    starSr.gameObject.SetActive(unlocked && si < slots);
                    // Canh giữa các ô sao đang hiện
                    var sp = starSr.transform.localPosition;
                    starSr.transform.localPosition = new Vector3(sv.pos.x + (si - (slots - 1) / 2f) * 0.26f, sp.y, sp.z);
                    bool starOn = si < lit;
                    starSr.color = starOn ? starColor : new Color(0, 0, 0, 0.35f);
                    float pop = starOn && si == lit - 1 && sv.starPulse > 0 ? 1 + Mathf.Sin(sv.starPulse / 0.6f * Mathf.PI) * 0.9f : 1f;
                    starSr.transform.localScale = new Vector3(0.24f * pop, 0.24f * pop, 1);
                }

                // Món đã nấu xong chờ bưng: hiện tối đa 3 đĩa trên quầy
                int platesShown = unlocked ? Mathf.Min(3, st.ready) : 0;
                for (int pi = 0; pi < sv.plates.Count; pi++) sv.plates[pi].gameObject.SetActive(pi < platesShown);

                sv.highlight.gameObject.SetActive(Selected == sv.data);
                bool best = _game.BestSellerStation == sv.data;
                if (sv.bestBadge.gameObject.activeSelf != best) sv.bestBadge.gameObject.SetActive(best);
                if (best)
                {
                    long left = _game.BestSellerSecondsLeft;
                    sv.bestText.text = $"BÁN CHẠY {left / 60}:{left % 60:00}";
                    float bk = 1 + Mathf.Sin(Time.time * 5f) * 0.06f;
                    sv.bestBadge.localScale = new Vector3(bk, bk, 1);
                }
                // Nảy nhẹ khi vừa được chạm
                sv.pulse = Mathf.Max(0, sv.pulse - dt);
                float k = 1 + Mathf.Sin(sv.pulse / 0.2f * Mathf.PI) * 0.12f;
                sv.counter.transform.localScale = sv.counterScale * k;
                if (sv.prop != null) (unlocked ? sv.prop : sv.propLocked).transform.localScale = Vector3.one * k;

            }
        }

        // ================= Chạm vào quầy =================

        private void HandleTap()
        {
            var pointer = Pointer.current;
            if (pointer == null || _cam == null || !pointer.press.wasPressedThisFrame) return;
            // Đang có popup (tiền offline, rương, lên đời) thì bỏ qua chạm vào cảnh.
            if (!_game.PendingOffline.IsZero || _game.LastRewards.Count > 0 || _game.IsVenueComplete()) return;

            Vector2 screen = pointer.position.ReadValue();
            // Chạm trúng nút giao diện nằm đè lên cảnh (thanh trên cùng, nút tăng tốc) thì bỏ qua
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            Vector2 world = _cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10));

            // Đồng xu tip trên bàn được ưu tiên (không đổi quầy đang chọn)
            if (TryCollectTip(world)) return;

            foreach (var sv in _stations)
            {
                // Vùng chạm: quầy + đầu bếp phía sau + chữ phía trên
                var area = new Rect(sv.pos.x - sv.width / 2 - 0.1f, sv.pos.y - 0.8f, sv.width + 0.2f, 2.6f);
                if (!area.Contains(world)) continue;
                OnStationTapped(sv);
                return;
            }
            Selected = null; // chạm ra chỗ trống thì bỏ chọn
        }

        private void OnStationTapped(StationView sv)
        {
            Selected = sv.data;
            sv.pulse = 0.2f;
        }

        /// <summary>Khách nhận khay: tiền cộng vào túi và bay lên ở bàn.</summary>
        private void HandleDelivered(RestaurantController.Worker w, RestaurantController.TableRun table, BigNum total)
        {
            if (_root == null || table.index >= _tables.Count || total.IsZero) return;
            FloatingText.Spawn(_root, _tables[table.index].pos + new Vector2(0, 0.8f), "+" + total, Gold);
            SoundFX.Play(Sfx.Coin, 0.25f);
        }

        // ================= Bàn và khách =================

        private void UpdateTables()
        {
            var runs = _game.Tables;
            int owned = _game.State.tables;
            for (int i = 0; i < _tables.Count; i++)
            {
                var tv = _tables[i];
                bool own = i < owned;
                UpdateCoin(tv, own && i < runs.Count ? runs[i].tip : BigNum.Zero, Time.deltaTime);
                var c = tv.baseColor;
                tv.sprite.color = own ? c : new Color(c.r, c.g, c.b, 0.22f);
                if (tv.prop != null)
                {
                    // Bàn đã mua: bàn 3D; chưa mua: chỉ còn bóng mờ
                    if (tv.prop.activeSelf != own) tv.prop.SetActive(own);
                    tv.sprite.enabled = !own;
                }
                if (tv.chair != null && tv.chair.activeSelf != own) tv.chair.SetActive(own);
                if (!own || i >= runs.Count)
                {
                    if (tv.customer != null) { Leave(tv.customer); tv.customer = null; }
                    tv.bubble.text = "";
                    continue;
                }

                var run = runs[i];
                if (run.stage == RestaurantController.TableStage.Arriving)
                {
                    // Khách cũ ăn xong thì đi về, khách mới từ cửa đi vào
                    if (tv.lastStage == RestaurantController.TableStage.Eating && tv.customer != null)
                    {
                        Leave(tv.customer);
                        tv.customer = null;
                    }
                    if (tv.customer == null)
                    {
                        // Khách đang đi trên vỉa hè (bên trái hoặc phải cửa) rẽ vào quán
                        tv.arriveFrom = new Vector2(_door.x + (Random.value < 0.5f ? -1.5f : 1.5f), _sidewalkY);
                        SpawnCustomer(tv, tv.arriveFrom);
                    }
                    float k = 1f - Mathf.Clamp01(run.timer / EconomyService.CustomerArriveSeconds);
                    Vector2 pos = AlongPath(tv.arriveFrom, new Vector2(_door.x, _sidewalkY), tv.seat, k);
                    tv.customer.localPosition = new Vector3(pos.x, pos.y, 0);
                    SetAction(tv.customer, ChibiAction.Auto);
                }
                else
                {
                    if (tv.customer == null) SpawnCustomer(tv, tv.seat);
                    tv.customer.localPosition = new Vector3(tv.seat.x, tv.seat.y, 0);
                    SetAction(tv.customer, run.stage == RestaurantController.TableStage.Eating ? ChibiAction.Eat : ChibiAction.Sit);
                }

                tv.bubble.text = run.stage == RestaurantController.TableStage.Ordered ? $"{run.cooked}/{run.order.Count} món"
                               : run.stage == RestaurantController.TableStage.Ready ? "Chờ bưng!"
                               : "";
                tv.lastStage = run.stage;
            }
        }

        // ================= Tip =================

        private static Vector2 CoinPos(TableView tv) => tv.pos + new Vector2(0.12f, 0.32f);

        private void HandleTipLeft(RestaurantController.TableRun table, BigNum amount)
        {
            if (table.index >= _tables.Count) return;
            _tables[table.index].coinPop = 0.35f;
            SoundFX.Play(Sfx.Coin, 0.35f);
        }

        /// <summary>Đồng xu hiện khi bàn có tip: nhấp nhô, vừa có tip thì nảy lên; tip cộng dồn thì xu to dần.</summary>
        private void UpdateCoin(TableView tv, BigNum tip, float dt)
        {
            bool show = !tip.IsZero;
            if (tv.coin.gameObject.activeSelf != show) tv.coin.gameObject.SetActive(show);
            if (!show) return;
            tv.coinPop = Mathf.Max(0, tv.coinPop - dt);
            float pop = tv.coinPop > 0 ? 1 + Mathf.Sin(tv.coinPop / 0.35f * Mathf.PI) * 0.5f : 1f;
            float bob = Mathf.Sin(Time.time * 3f + tv.pos.x) * 0.04f;
            tv.coin.transform.localPosition = CoinPos(tv) + new Vector2(0, bob);
            float k = 0.34f / Mathf.Max(0.01f, tv.coin.sprite.bounds.size.y) * pop;
            tv.coin.transform.localScale = new Vector3(k, k, 1);
        }

        /// <summary>Chạm trúng đồng xu tip nào thì nhận tiền; trả về true nếu đã nhận.</summary>
        private bool TryCollectTip(Vector2 world)
        {
            var runs = _game.Tables;
            for (int i = 0; i < _tables.Count && i < runs.Count; i++)
            {
                var tv = _tables[i];
                if (runs[i].tip.IsZero || !tv.coin.gameObject.activeSelf) continue;
                if (Vector2.Distance(world, CoinPos(tv)) > 0.45f) continue;
                var amount = _game.CollectTip(i);
                if (amount.IsZero) continue;
                FloatingText.Spawn(_root, CoinPos(tv) + new Vector2(0, 0.3f), $"Tip +{amount}", Gold, 0.3f);
                SoundFX.Play(Sfx.Coin, 0.8f);
                tv.coin.gameObject.SetActive(false);
                return true;
            }
            return false;
        }

        private void SpawnCustomer(TableView tv, Vector2 at)
        {
            int ci = _colorIndex++;
            // Khách chibi 3D, mỗi người một kiểu (nón lá, mũ lưỡi trai, tóc búi…)
            var chibi = Chibi.Create(_root, ChibiKind.Customer, ci % ChibiLooks.CustomerVariants, at);
            if (tv.chair != null)
            {
                // Ngồi ghế của bàn, quay mặt sang phải vào bàn
                chibi.useStool = false;
                chibi.seatLift = Props.ChairSeatHeight(_tier) / chibi.transform.localScale.y - 0.08f;
                chibi.faceYaw = 90f;
            }
            tv.customer = chibi.transform;
            tv.customerScale = chibi.transform.localScale;
        }

        private static void SetAction(Transform t, ChibiAction action)
        {
            var c = t != null ? t.GetComponent<Chibi>() : null;
            if (c != null) c.action = action;
        }

        /// <summary>Điểm trên đường gấp khúc a → b (cửa, phía vỉa hè) → c theo tỉ lệ quãng đường k (0–1).</summary>
        private Vector2 AlongPath(Vector2 a, Vector2 b, Vector2 c, float k)
        {
            // Đi qua cửa: thêm điểm ngay trong cửa để không đi xuyên tường mặt tiền
            Vector2 d = _door + new Vector2(0, 0.35f);
            float l1 = Vector2.Distance(a, b), l2 = Vector2.Distance(b, d), l3 = Vector2.Distance(d, c);
            float s = k * (l1 + l2 + l3);
            if (s <= l1) return Vector2.Lerp(a, b, l1 > 0 ? s / l1 : 1);
            s -= l1;
            if (s <= l2) return Vector2.Lerp(b, d, l2 > 0 ? s / l2 : 1);
            s -= l2;
            return Vector2.Lerp(d, c, l3 > 0 ? s / l3 : 1);
        }

        private void UpdateLeaving(float dt)
        {
            for (int i = _leaving.Count - 1; i >= 0; i--)
            {
                var l = _leaving[i];
                if (l.t == null) { _leaving.RemoveAt(i); continue; }
                SetAction(l.t, ChibiAction.Auto);
                switch (l.stage)
                {
                    case 0: if (MoveTo(l.t, _door + new Vector2(0, 0.35f), 3f, dt)) l.stage = 1; break;
                    case 1: if (MoveTo(l.t, new Vector2(_door.x, _sidewalkY), 2.5f, dt)) l.stage = 2; break;
                    default:
                        if (MoveTo(l.t, new Vector2(l.dir * _streetEdge, _sidewalkY), 1.6f, dt))
                        {
                            Destroy(l.t.gameObject);
                            _leaving.RemoveAt(i);
                        }
                        break;
                }
            }
        }

        // ================= Đầu bếp và phục vụ =================

        private void SyncWorkers()
        {
            var list = _game.Workers;
            // Danh sách người làm vừa được tạo lại (lên đời, xóa save): dựng lại hình
            if (_workers.Count > list.Count || (_workers.Count > 0 && _workers[0].data != list[0]))
            {
                foreach (var wv in _workers)
                {
                    if (wv.t != null) Destroy(wv.t.gameObject);
                    if (wv.plate != null) Destroy(wv.plate.gameObject);
                }
                _workers.Clear();
            }
            while (_workers.Count < list.Count)
            {
                var data = list[_workers.Count];
                _workers.Add(CreateWorker(data, Home(data)));
            }
            for (int i = 0; i < _workers.Count; i++) _workers[i].data = list[i];
        }

        private WorkerView CreateWorker(RestaurantController.Worker data, Vector2 at)
        {
            var kind = data.kind == RestaurantController.WorkerKind.Waiter ? ChibiKind.Waiter
                     : data.kind == RestaurantController.WorkerKind.HeadChef ? ChibiKind.HeadChef
                     : ChibiKind.Chef;
            var chibi = Chibi.Create(_root, kind, data.index, at);
            // Khay trên tay khi bưng món (vẽ đè lên nhân vật, vị trí theo hai tay)
            var plate = SpriteFactory.Picture("Plate", _root, at, new Vector2(0.34f, 0.34f), Color.white, 8, "props/plate", out _, circle: true);
            plate.gameObject.SetActive(false);
            return new WorkerView { data = data, t = chibi.transform, chibi = chibi, plate = plate };
        }

        /// <summary>Chỗ đứng khi rảnh: đầu bếp đứng bên phải dãy quầy, phục vụ bên trái.</summary>
        private Vector2 Home(RestaurantController.Worker w)
        {
            switch (w.kind)
            {
                case RestaurantController.WorkerKind.HeadChef: return _chefHome;
                case RestaurantController.WorkerKind.Chef: return _chefHome + new Vector2(-(w.index % 6 + 1) * 0.45f, -(w.index / 6) * 0.5f);
                default: return _waiterHome + new Vector2((w.index % 6) * 0.5f, -(w.index / 6) * 0.5f);
            }
        }

        /// <summary>
        /// Nấu: đi tới quầy (¼ thời gian đầu) rồi đứng nấu. Bưng: tới chỗ lấy khay (35% đầu) rồi bưng ra bàn.
        /// </summary>
        private void UpdateWorkers(float dt)
        {
            foreach (var wv in _workers)
            {
                var w = wv.data;
                if (w.kind == RestaurantController.WorkerKind.HeadChef) ApplyHeadChefGear(wv.chibi);
                if (w.task == RestaurantController.TaskKind.Idle)
                {
                    wv.taskId = 0;
                    wv.plate.gameObject.SetActive(false);
                    wv.chibi.action = ChibiAction.Auto;
                    MoveTo(wv.t, Home(w), 3f, dt);
                    continue;
                }
                if (wv.taskId != w.taskId) { wv.taskId = w.taskId; wv.startPos = wv.t.localPosition; }
                float p = w.Progress;
                Vector2 pos;
                if (w.task == RestaurantController.TaskKind.Cook)
                {
                    Vector2 spot = wv.startPos;
                    foreach (var sv in _stations)
                        if (sv.data == w.station) { spot = sv.pos + new Vector2(((w.index % 3) - 1) * 0.25f, CookSpotBehind); break; }
                    pos = p < 0.25f ? Vector2.Lerp(wv.startPos, spot, p / 0.25f)
                                    : spot;
                    wv.chibi.action = p < 0.25f ? ChibiAction.Auto : ChibiAction.Cook;
                    wv.plate.gameObject.SetActive(false);
                }
                else
                {
                    Vector2 dest = w.table != null && w.table.index < _tables.Count ? _tables[w.table.index].pos + new Vector2(0.6f, 0) : _door;
                    const float pickupShare = 0.35f;
                    pos = p < pickupShare ? Vector2.Lerp(wv.startPos, _passPoint, p / pickupShare)
                                          : Vector2.Lerp(_passPoint, dest, (p - pickupShare) / (1 - pickupShare));
                    bool carrying = p >= pickupShare;
                    wv.chibi.action = carrying ? ChibiAction.Carry : ChibiAction.Auto;
                    wv.plate.gameObject.SetActive(carrying);
                }
                wv.t.localPosition = new Vector3(pos.x, pos.y, 0);
            }
        }

        /// <summary>Đầu bếp chính mặc đúng đồ đang trang bị: mũ, tạp dề, dao (tay phải), nồi/chảo (tay trái khi nấu).</summary>
        private void ApplyHeadChefGear(Chibi chibi)
        {
            var s = _game.State;
            if (chibi == null || s == null || s.equipped == null || s.equipped.Length < 4) return;
            chibi.SetGear(GearId(s, 3), GearId(s, 2), GearId(s, 0), GearId(s, 1));
        }

        private string GearId(PhoAmThucViet.Core.GameState s, int slot)
        {
            var uid = s.equipped[slot];
            return string.IsNullOrEmpty(uid) ? null : _game.ItemOf(s.GetEquipment(uid))?.id;
        }

        /// <summary>Đĩa món đi theo hai tay nhân vật (sau khi Chibi cập nhật tư thế).</summary>
        private void LateUpdate()
        {
            if (_root == null) return;
            foreach (var wv in _workers)
            {
                if (wv.plate == null || wv.chibi == null || !wv.plate.gameObject.activeSelf) continue;
                Vector3 hand = _root.InverseTransformPoint(wv.chibi.CarryPoint);
                wv.plate.transform.localPosition = new Vector3(hand.x, hand.y, 0);
            }
        }

        /// <summary>Đi thẳng tới đích; trả về true khi đã tới.</summary>
        private static bool MoveTo(Transform t, Vector2 target, float speed, float dt)
        {
            Vector2 pos = t.localPosition;
            Vector2 next = Vector2.MoveTowards(pos, target, speed * dt);
            t.localPosition = new Vector3(next.x, next.y, 0);
            return (next - target).sqrMagnitude < 0.0004f;
        }
    }
}
