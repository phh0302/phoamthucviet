using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PhoAmThucViet.Analytics;
using PhoAmThucViet.Core;
using PhoAmThucViet.Gameplay;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;

namespace PhoAmThucViet.Online
{
    public enum CloudStatus { Connecting, Online, Offline }

    /// <summary>Bản lưu trên mạng khác bản trên máy: chờ người chơi chọn.</summary>
    public class CloudConflict
    {
        public GameState cloud;
        /// <summary>true khi vừa đăng nhập tài khoản khác (không phải lúc mở game).</summary>
        public bool afterSignIn;
    }

    /// <summary>
    /// Đăng nhập và lưu tiến trình trên mạng bằng Unity Gaming Services (Authentication + Cloud Save).
    ///
    /// - Mở game: tự đăng nhập (khách, hoặc tài khoản đã đăng nhập lần trước), so bản lưu trên mạng với bản trên máy.
    /// - Đang chơi: cứ <see cref="UploadInterval"/> giây và khi ẩn app thì đẩy bản lưu lên.
    /// - Tạo tài khoản (tên đăng nhập + mật khẩu) để giữ tiến trình khi đổi máy; đăng nhập trên máy khác để tải về.
    /// Không có mạng hoặc chưa liên kết Unity Cloud thì game vẫn chơi bình thường, chỉ lưu trên máy.
    /// </summary>
    [RequireComponent(typeof(RestaurantController))]
    public class CloudSync : MonoBehaviour
    {
        public static CloudSync Instance { get; private set; }

        private const string SaveKey = "save";
        private const float UploadInterval = 120f;

        public CloudStatus Status { get; private set; } = CloudStatus.Connecting;
        /// <summary>Lý do không kết nối được (hiện trong Cài đặt).</summary>
        public string OfflineReason { get; private set; } = "";
        public bool Busy { get; private set; }
        public CloudConflict Pending { get; private set; }
        /// <summary>Lần đẩy lên mạng gần nhất (giờ máy), null nếu chưa có.</summary>
        public DateTime? LastUpload { get; private set; }

        /// <summary>Thông báo gần nhất cho người chơi (thành công / lỗi).</summary>
        public string Message { get; private set; } = "";
        public bool MessageIsError { get; private set; }

        private RestaurantController _game;
        private float _timer;
        private bool _ready; // đã so xong bản trên mạng, được phép đẩy lên

        private static IAuthenticationService Auth => AuthenticationService.Instance;

        public bool SignedIn => Status == CloudStatus.Online && Auth.IsSignedIn;
        public string Username => SignedIn ? Auth.PlayerInfo?.Username : null;
        public bool IsGuest => SignedIn && string.IsNullOrEmpty(Username);
        public string PlayerId => SignedIn ? Auth.PlayerId : "";

        private void Awake()
        {
            Instance = this;
            _game = GetComponent<RestaurantController>();
        }

        private async void Start() => await Connect();

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!_ready || Busy || !SignedIn || Pending != null) return;
            _timer += Time.unscaledDeltaTime;
            if (_timer >= UploadInterval) _ = Upload(false);
        }

        private void OnApplicationPause(bool paused)
        {
            // RestaurantController đã lưu xuống máy; gửi luôn bản mới lên mạng (cố gắng trước khi app bị dừng)
            if (paused && _ready && SignedIn && Pending == null && !Busy) _ = Upload(false);
        }

        // ================= Kết nối =================

        public async Task Connect()
        {
            Status = CloudStatus.Connecting;
            _ready = false;
            try
            {
                if (Application.internetReachability == NetworkReachability.NotReachable)
                    throw new Exception("Không có kết nối mạng");
                if (UnityServices.State != ServicesInitializationState.Initialized)
                    await UnityServices.InitializeAsync();
                // Có phiên cũ (khách hoặc tài khoản) thì vào lại đúng người chơi đó
                if (!Auth.IsSignedIn) await Auth.SignInAnonymouslyAsync();
                await Auth.GetPlayerInfoAsync();
                Status = CloudStatus.Online;
                OfflineReason = "";
                await Reconcile(false);
            }
            catch (Exception ex)
            {
                GoOffline(ex);
            }
        }

        private void GoOffline(Exception ex)
        {
            Status = CloudStatus.Offline;
            OfflineReason = Application.internetReachability == NetworkReachability.NotReachable
                ? "Không có kết nối mạng"
                : "Không kết nối được máy chủ (" + Short(ex.Message) + ")";
            Debug.LogWarning("[Cloud] " + ex);
        }

        /// <summary>So bản trên mạng với bản trên máy rồi quyết định tải về, đẩy lên, hoặc hỏi người chơi.</summary>
        private async Task Reconcile(bool afterSignIn)
        {
            var cloud = await DownloadSave();
            if (cloud == null)
            {
                _ready = true;
                await Upload(false);
                if (afterSignIn) Say("Tài khoản chưa có dữ liệu: đã lưu tiến trình hiện tại lên.", false);
                return;
            }

            var local = _game.State;
            bool cloudIsOurs = cloud.lastSaveUnix <= UploadedStamp(); // bản trên mạng do chính máy này đẩy lên
            if (!afterSignIn && cloudIsOurs)
            {
                _ready = true;
                await Upload(false);
                return;
            }
            if (!afterSignIn && IsFresh(local))
            {
                ApplyCloud(cloud);
                Say("Đã tải tiến trình của bạn từ máy chủ.", false);
                return;
            }
            Pending = new CloudConflict { cloud = cloud, afterSignIn = afterSignIn };
        }

        /// <summary>Bản trên máy gần như mới tinh (vừa cài game): tải bản trên mạng về không cần hỏi.</summary>
        private static bool IsFresh(GameState s) =>
            s == null || (s.totalPlaySeconds < 180 && s.secondsInCity < 180 && s.venueIndex == 0 && s.citiesCompleted == 0);

        /// <summary>Người chơi chọn bản lưu khi có khác biệt.</summary>
        public async void ResolveConflict(bool useCloud)
        {
            var p = Pending;
            if (p == null) return;
            Pending = null;
            if (useCloud)
            {
                ApplyCloud(p.cloud);
                Say("Đã dùng bản lưu trên mạng.", false);
            }
            else
            {
                _ready = true;
                await Upload(true);
                Say("Đã giữ bản trên máy này và lưu đè lên mạng.", false);
            }
        }

        private void ApplyCloud(GameState cloud)
        {
            _game.LoadState(cloud);
            SetUploadedStamp(_game.State.lastSaveUnix);
            _ready = true;
            _timer = 0;
        }

        // ================= Lưu / tải =================

        private async Task<GameState> DownloadSave()
        {
            var items = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { SaveKey });
            if (items == null || !items.TryGetValue(SaveKey, out var item)) return null;
            var state = SaveService.Decode(item.Value.GetAs<string>());
            if (state == null) Debug.LogWarning("[Cloud] Bản lưu trên mạng bị hỏng, bỏ qua.");
            return state;
        }

        /// <summary>Đẩy bản hiện tại lên mạng. userAction = người chơi tự bấm (hiện thông báo).</summary>
        public async Task Upload(bool userAction)
        {
            if (!SignedIn || Busy || Pending != null) return;
            _timer = 0;
            Busy = true;
            try
            {
                SaveService.Save(_game.State);
                long stamp = _game.State.lastSaveUnix;
                var data = new Dictionary<string, object> { { SaveKey, SaveService.Encode(_game.State) } };
                await CloudSaveService.Instance.Data.Player.SaveAsync(data);
                SetUploadedStamp(stamp);
                _ready = true;
                // Đăng thẻ đầu bếp công khai để người khác mượn (chỉ gửi khi đồ đang mặc thay đổi)
                await HelperDirectory.Publish(_game.State, _game.equipmentCatalog);
                LastUpload = DateTime.Now;
                if (userAction) Say("Đã lưu lên mạng.", false);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Cloud] Lưu lên mạng thất bại: " + ex.Message);
                if (userAction) Say("Chưa lưu được lên mạng, thử lại sau.", true);
            }
            finally
            {
                Busy = false;
            }
        }

        /// <summary>Mốc thời gian của bản mà máy này đã đẩy lên cho người chơi hiện tại.</summary>
        private long UploadedStamp() =>
            long.TryParse(PlayerPrefs.GetString("cloud_up_" + PlayerId, "0"), out long v) ? v : 0;

        private void SetUploadedStamp(long v)
        {
            PlayerPrefs.SetString("cloud_up_" + PlayerId, v.ToString());
            PlayerPrefs.Save();
        }

        // ================= Tài khoản =================

        private static readonly Regex UserRule = new Regex(@"^[A-Za-z0-9.\-@_]{3,20}$");

        /// <summary>Kiểm tra trước khi gửi; trả về lỗi (tiếng Việt) hoặc null nếu hợp lệ.</summary>
        public static string ValidateCredentials(string user, string pass)
        {
            if (string.IsNullOrEmpty(user) || !UserRule.IsMatch(user))
                return "Tên đăng nhập 3–20 ký tự, chỉ gồm chữ không dấu, số và . - @ _";
            if (string.IsNullOrEmpty(pass) || pass.Length < 8 || pass.Length > 30
                || !Regex.IsMatch(pass, "[A-Z]") || !Regex.IsMatch(pass, "[a-z]")
                || !Regex.IsMatch(pass, "[0-9]") || !Regex.IsMatch(pass, "[^A-Za-z0-9]"))
                return "Mật khẩu 8–30 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt (vd: Pho@2026)";
            return null;
        }

        /// <summary>Gắn tên đăng nhập + mật khẩu cho tài khoản khách hiện tại (giữ nguyên tiến trình).</summary>
        public async Task<bool> CreateAccount(string user, string pass)
        {
            string err = ValidateCredentials(user, pass);
            if (err != null) { Say(err, true); return false; }
            if (!SignedIn) { Say("Chưa kết nối máy chủ.", true); return false; }
            Busy = true;
            try
            {
                await Auth.AddUsernamePasswordAsync(user, pass);
                await Auth.GetPlayerInfoAsync();
                AnalyticsService.Log("account_created");
                Say($"Đã tạo tài khoản \"{user}\". Đăng nhập tên này trên máy khác để chơi tiếp.", false);
                Busy = false;
                await Upload(false);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Cloud] Tạo tài khoản thất bại: " + ex);
                Say(NoNetwork() ?? (IsAlreadyLinked(ex)
                    ? "Tài khoản này đã có tên đăng nhập."
                    : "Không tạo được: tên đăng nhập có thể đã có người dùng."), true);
                return false;
            }
            finally
            {
                Busy = false;
            }
        }

        /// <summary>Đăng nhập tài khoản khác (ví dụ trên máy mới), rồi hỏi dùng bản lưu nào.</summary>
        public async Task<bool> SignIn(string user, string pass)
        {
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass)) { Say("Nhập tên đăng nhập và mật khẩu.", true); return false; }
            if (Status != CloudStatus.Online) { Say("Chưa kết nối máy chủ.", true); return false; }
            Message = "";
            Busy = true;
            _ready = false;
            try
            {
                // Đẩy bản hiện tại lên tài khoản đang dùng trước khi đổi
                if (Auth.IsSignedIn && Pending == null)
                {
                    Busy = false;
                    await Upload(false);
                    Busy = true;
                }
                Auth.SignOut(); // giữ phiên cũ: nếu đăng nhập sai thì quay lại được người chơi cũ
                Pending = null;
                await Auth.SignInWithUsernamePasswordAsync(user, pass);
                await Auth.GetPlayerInfoAsync();
                AnalyticsService.Log("account_sign_in");
                Busy = false;
                await Reconcile(true);
                if (Pending == null && string.IsNullOrEmpty(Message)) Say($"Đã đăng nhập \"{user}\".", false);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Cloud] Đăng nhập thất bại: " + ex);
                Say(NoNetwork() ?? "Sai tên đăng nhập hoặc mật khẩu.", true);
                try
                {
                    if (!Auth.IsSignedIn) await Auth.SignInAnonymouslyAsync();
                    await Auth.GetPlayerInfoAsync();
                    _ready = true;
                }
                catch (Exception inner) { GoOffline(inner); }
                return false;
            }
            finally
            {
                Busy = false;
            }
        }

        // ================= Tiện ích =================

        private void Say(string text, bool error)
        {
            Message = text;
            MessageIsError = error;
        }

        public void ClearMessage() => Message = "";

        private static string NoNetwork() =>
            Application.internetReachability == NetworkReachability.NotReachable ? "Không có kết nối mạng." : null;

        private static bool IsAlreadyLinked(Exception ex) =>
            ex is AuthenticationException ae && ae.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked;

        private static string Short(string s)
        {
            if (string.IsNullOrEmpty(s)) return "lỗi không rõ";
            s = s.Split('\n')[0];
            return s.Length > 80 ? s.Substring(0, 80) + "…" : s;
        }

        /// <summary>Tóm tắt một bản lưu để người chơi so sánh.</summary>
        public string Describe(GameState s)
        {
            if (s == null) return "(trống)";
            var city = _game.CityById(s.cityId);
            string where = city != null ? $"{city.displayName} · mốc {s.venueIndex + 1}/{city.venues.Count}" : "Đang chơi";
            double secs = Math.Max(s.totalPlaySeconds, s.secondsInCity);
            int h = (int)(secs / 3600), m = (int)(secs % 3600 / 60);
            string played = secs > 0 ? (h > 0 ? $"{h} giờ {m} phút" : $"{m} phút") : "chưa rõ";
            string saved = s.lastSaveUnix > 0
                ? DateTimeOffset.FromUnixTimeSeconds(s.lastSaveUnix).ToLocalTime().ToString("dd/MM HH:mm")
                : "?";
            return $"{where}\nĐã xong {s.citiesCompleted} thành phố · ♦ {s.gems}\nĐã chơi {played} · lưu lúc {saved}";
        }
    }
}
