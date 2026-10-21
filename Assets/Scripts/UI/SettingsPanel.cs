using System;
using PhoAmThucViet.Audio;
using PhoAmThucViet.Gameplay;
using PhoAmThucViet.Online;
using UnityEngine;
using UnityEngine.UI;

namespace PhoAmThucViet.UI
{
    /// <summary>
    /// Bảng Cài đặt: bật/tắt âm thanh, tài khoản và lưu trên mạng (tạo tài khoản, đăng nhập, lưu ngay).
    /// Kèm hộp "Chọn bản lưu" tự hiện khi bản trên mạng khác bản trên máy.
    /// </summary>
    public class SettingsPanel
    {
        private enum Form { None, Create, SignIn }

        private readonly RestaurantController _game;
        private RectTransform _root, _conflictRoot;
        private UIButton _soundBtn, _createBtn, _signInBtn, _syncBtn, _confirmBtn, _cancelBtn, _retryBtn;
        private Text _status, _hint, _message, _formTitle;
        private RectTransform _form, _actions;
        private InputField _user, _pass;
        private Form _formMode = Form.None;
        private Text _conflictTitle, _conflictInfo, _localText, _cloudText;

        public SettingsPanel(RestaurantController game) => _game = game;

        public bool IsOpen => _root != null && _root.gameObject.activeSelf;

        public void Build(Transform canvas)
        {
            BuildSettings(canvas);
            BuildConflict(canvas);
        }

        public void Open()
        {
            _root.gameObject.SetActive(true);
            SetForm(Form.None);
            CloudSync.Instance?.ClearMessage();
        }

        public void Close() => _root.gameObject.SetActive(false);

        // ================= Dựng =================

        private void BuildSettings(Transform canvas)
        {
            var dim = UIKit.Panel("Settings", canvas, new Color(0, 0, 0, 0.65f));
            _root = dim.rectTransform;
            _root.Place(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(Close); // chạm ra ngoài để đóng

            var box = UIKit.Panel("Box", _root, Palette.Card, UIKit.Rounded).rectTransform;
            box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
            box.sizeDelta = new Vector2(940, 1240);
            box.gameObject.AddComponent<Button>().transition = Selectable.Transition.None; // chặn chạm xuyên xuống nền

            var header = UIKit.Panel("Header", box, Palette.Red, UIKit.Rounded).rectTransform;
            header.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -120), Vector2.zero);
            var title = UIKit.Label("Title", header, "Cài đặt", 42, Palette.Gold, TextAnchor.MiddleCenter, true);
            title.rectTransform.Place(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Âm thanh
            var soundLabel = UIKit.Label("SoundLabel", box, "Âm thanh", 34, Palette.TextDark, TextAnchor.MiddleLeft, true);
            soundLabel.rectTransform.TopLeft(40, 150, 400, 80);
            _soundBtn = UIKit.MakeButton("Sound", box, "", 30, Palette.Green, () => { SoundFX.Muted = !SoundFX.Muted; SoundFX.Play(Sfx.Click); }, UIKit.Pill);
            _soundBtn.bg.rectTransform.TopRight(40, 150, 240, 80);

            var line = UIKit.Panel("Line", box, new Color(0, 0, 0, 0.12f)).rectTransform;
            line.TopLeft(30, 250, 880, 3);

            // Tài khoản
            var accTitle = UIKit.Label("AccountTitle", box, "Tài khoản & lưu trên mạng", 34, Palette.TextDark, TextAnchor.MiddleLeft, true);
            accTitle.rectTransform.TopLeft(40, 270, 860, 60);
            _status = UIKit.Label("Status", box, "", 28, Palette.TextDark, TextAnchor.UpperLeft);
            _status.rectTransform.TopLeft(40, 340, 860, 140);
            _hint = UIKit.Label("Hint", box, "", 24, Palette.TextSoft, TextAnchor.UpperLeft);
            _hint.rectTransform.TopLeft(40, 485, 860, 110);

            // Các nút thao tác
            _actions = UIKit.NewRect("Actions", box);
            _actions.TopLeft(40, 600, 860, 380);
            _createBtn = UIKit.MakeButton("Create", _actions, "Tạo tài khoản", 30, Palette.Orange, () => SetForm(Form.Create), UIKit.Pill);
            _createBtn.bg.rectTransform.TopLeft(0, 0, 860, 100);
            _signInBtn = UIKit.MakeButton("SignIn", _actions, "Đăng nhập tài khoản khác", 30, Palette.Blue, () => SetForm(Form.SignIn), UIKit.Pill);
            _signInBtn.bg.rectTransform.TopLeft(0, 120, 860, 100);
            _syncBtn = UIKit.MakeButton("Sync", _actions, "Lưu lên mạng ngay", 30, Palette.Green, OnSync, UIKit.Pill);
            _syncBtn.bg.rectTransform.TopLeft(0, 240, 860, 100);
            _retryBtn = UIKit.MakeButton("Retry", _actions, "Thử kết nối lại", 30, Palette.Blue, OnRetry, UIKit.Pill);
            _retryBtn.bg.rectTransform.TopLeft(0, 0, 860, 100);

            // Ô nhập tên + mật khẩu
            _form = UIKit.NewRect("Form", box);
            _form.TopLeft(40, 600, 860, 400);
            _formTitle = UIKit.Label("FormTitle", _form, "", 28, Palette.TextDark, TextAnchor.MiddleLeft, true);
            _formTitle.rectTransform.TopLeft(0, 0, 860, 50);
            _user = MakeInput("User", _form, "Tên đăng nhập", false);
            ((RectTransform)_user.transform).TopLeft(0, 60, 860, 96);
            _pass = MakeInput("Pass", _form, "Mật khẩu", true);
            ((RectTransform)_pass.transform).TopLeft(0, 172, 860, 96);
            _cancelBtn = UIKit.MakeButton("Cancel", _form, "Hủy", 30, Palette.Disabled, () => SetForm(Form.None), UIKit.Pill);
            _cancelBtn.bg.rectTransform.TopLeft(0, 290, 300, 100);
            _confirmBtn = UIKit.MakeButton("Confirm", _form, "Xác nhận", 30, Palette.Orange, OnConfirm, UIKit.Pill);
            _confirmBtn.bg.rectTransform.TopLeft(320, 290, 540, 100);

            _message = UIKit.Label("Message", box, "", 26, Palette.Green, TextAnchor.MiddleCenter, true);
            _message.rectTransform.TopLeft(40, 1010, 860, 90);

            var close = UIKit.MakeButton("Close", box, "Đóng", 32, Palette.Red, Close, UIKit.Pill);
            close.bg.rectTransform.anchorMin = close.bg.rectTransform.anchorMax = new Vector2(0.5f, 0);
            close.bg.rectTransform.pivot = new Vector2(0.5f, 0);
            close.bg.rectTransform.anchoredPosition = new Vector2(0, 30);
            close.bg.rectTransform.sizeDelta = new Vector2(360, 100);

            _root.gameObject.SetActive(false);
        }

        private void BuildConflict(Transform canvas)
        {
            var dim = UIKit.Panel("CloudConflict", canvas, new Color(0, 0, 0, 0.75f));
            _conflictRoot = dim.rectTransform;
            _conflictRoot.Place(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var box = UIKit.Panel("Box", _conflictRoot, Palette.Card, UIKit.Rounded).rectTransform;
            box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
            box.sizeDelta = new Vector2(940, 1180);

            var header = UIKit.Panel("Header", box, Palette.Red, UIKit.Rounded).rectTransform;
            header.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -120), Vector2.zero);
            _conflictTitle = UIKit.Label("Title", header, "Chọn bản lưu", 42, Palette.Gold, TextAnchor.MiddleCenter, true);
            _conflictTitle.rectTransform.Place(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _conflictInfo = UIKit.Label("Info", box, "", 26, Palette.TextSoft, TextAnchor.UpperCenter);
            _conflictInfo.rectTransform.TopLeft(40, 140, 860, 90);

            _localText = ConflictCard(box, 250, "Trên máy này", Palette.Orange, () => CloudSync.Instance?.ResolveConflict(false));
            _cloudText = ConflictCard(box, 690, "Trên mạng", Palette.Blue, () => CloudSync.Instance?.ResolveConflict(true));

            _conflictRoot.gameObject.SetActive(false);
        }

        private Text ConflictCard(RectTransform box, float y, string title, Color color, Action onPick)
        {
            var card = UIKit.Panel(title, box, new Color(1, 1, 1, 0.7f), UIKit.Rounded).rectTransform;
            card.TopLeft(40, y, 860, 410);
            var t = UIKit.Label("Title", card, title, 32, color, TextAnchor.MiddleLeft, true);
            t.rectTransform.TopLeft(30, 16, 800, 54);
            var info = UIKit.Label("Info", card, "", 28, Palette.TextDark, TextAnchor.UpperLeft);
            info.rectTransform.TopLeft(30, 80, 800, 180);
            var pick = UIKit.MakeButton("Pick", card, "Dùng bản này", 30, color, () => { SoundFX.Play(Sfx.Click); onPick(); }, UIKit.Pill);
            pick.bg.rectTransform.TopLeft(30, 286, 800, 100);
            return info;
        }

        private static InputField MakeInput(string name, Transform parent, string placeholder, bool password)
        {
            var bg = UIKit.Panel(name, parent, Color.white, UIKit.Rounded);
            var input = bg.gameObject.AddComponent<InputField>();

            var text = UIKit.Label("Text", bg.transform, "", 32, Palette.TextDark);
            text.resizeTextForBestFit = false;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.rectTransform.Place(Vector2.zero, Vector2.one, new Vector2(28, 8), new Vector2(-28, -8));

            var ph = UIKit.Label("Placeholder", bg.transform, placeholder, 32, new Color(0.55f, 0.5f, 0.45f));
            ph.resizeTextForBestFit = false;
            ph.fontStyle = FontStyle.Italic;
            ph.rectTransform.Place(Vector2.zero, Vector2.one, new Vector2(28, 8), new Vector2(-28, -8));

            input.targetGraphic = bg;
            input.textComponent = text;
            input.placeholder = ph;
            input.characterLimit = 30;
            input.lineType = InputField.LineType.SingleLine;
            input.contentType = password ? InputField.ContentType.Password : InputField.ContentType.Standard;
            if (!password) input.keyboardType = TouchScreenKeyboardType.ASCIICapable;
            return input;
        }

        // ================= Thao tác =================

        private void SetForm(Form f)
        {
            _formMode = f;
            if (_pass != null) _pass.text = "";
            if (f != Form.None && _user != null) _user.text = "";
            _formTitle.text = f == Form.Create ? "Đặt tên đăng nhập và mật khẩu" : "Đăng nhập tài khoản đã có";
            _confirmBtn.label.text = f == Form.Create ? "Tạo tài khoản" : "Đăng nhập";
        }

        private async void OnConfirm()
        {
            var cloud = CloudSync.Instance;
            if (cloud == null || cloud.Busy) return;
            SoundFX.Play(Sfx.Click);
            string user = _user.text.Trim(), pass = _pass.text;
            bool ok = _formMode == Form.Create ? await cloud.CreateAccount(user, pass) : await cloud.SignIn(user, pass);
            if (ok) SetForm(Form.None);
        }

        private async void OnSync()
        {
            var cloud = CloudSync.Instance;
            if (cloud == null) return;
            SoundFX.Play(Sfx.Click);
            await cloud.Upload(true);
        }

        private async void OnRetry()
        {
            var cloud = CloudSync.Instance;
            if (cloud == null) return;
            SoundFX.Play(Sfx.Click);
            await cloud.Connect();
        }

        // ================= Cập nhật =================

        public void Update()
        {
            UpdateConflict();
            if (!IsOpen) return;

            _soundBtn.Set(SoundFX.Muted ? "Đang tắt" : "Đang bật", true);
            _soundBtn.bg.color = SoundFX.Muted ? Palette.Disabled : Palette.Green;

            var cloud = CloudSync.Instance;
            bool online = cloud != null && cloud.SignedIn;
            bool busy = cloud == null || cloud.Busy;

            if (cloud == null || cloud.Status == CloudStatus.Connecting)
            {
                _status.text = "Đang kết nối máy chủ…";
                _hint.text = "";
            }
            else if (cloud.Status == CloudStatus.Offline)
            {
                _status.text = "<color=#B03A2E>Chưa kết nối được</color>\n" + cloud.OfflineReason;
                _hint.text = "Game vẫn lưu trên máy này. Khi có mạng, tiến trình sẽ tự lưu lên.";
            }
            else
            {
                string id = cloud.PlayerId.Length > 8 ? cloud.PlayerId.Substring(0, 8) : cloud.PlayerId;
                string when = cloud.LastUpload.HasValue ? $"Lần lưu lên mạng gần nhất: {cloud.LastUpload.Value:HH:mm}" : "Đang chờ lưu lên mạng…";
                _status.text = cloud.IsGuest
                    ? $"Đang chơi bằng tài khoản <b>khách</b> (mã {id})\n{when}"
                    : $"Tài khoản: <b>{cloud.Username}</b>\n{when}";
                _hint.text = cloud.IsGuest
                    ? "Tài khoản khách chỉ dùng được trên máy này. Hãy tạo tài khoản để không mất tiến trình khi đổi hoặc mất máy."
                    : "Đăng nhập tên này trên máy khác để chơi tiếp tiến trình.";
            }

            bool showForm = _formMode != Form.None && online;
            _form.gameObject.SetActive(showForm);
            _actions.gameObject.SetActive(!showForm);
            bool offline = cloud != null && cloud.Status == CloudStatus.Offline;
            _retryBtn.bg.gameObject.SetActive(offline);
            _createBtn.bg.gameObject.SetActive(online && cloud.IsGuest);
            _signInBtn.bg.gameObject.SetActive(online);
            _syncBtn.bg.gameObject.SetActive(online);
            // Khi đã có tài khoản thì không còn nút Tạo: dồn 2 nút còn lại lên
            float y = online && cloud.IsGuest ? 120 : 0;
            _signInBtn.bg.rectTransform.anchoredPosition = new Vector2(0, -y);
            _syncBtn.bg.rectTransform.anchoredPosition = new Vector2(0, -(y + 120));
            _createBtn.Set("Tạo tài khoản", !busy);
            _signInBtn.Set("Đăng nhập tài khoản khác", !busy);
            _syncBtn.Set(busy ? "Đang lưu…" : "Lưu lên mạng ngay", !busy);
            _retryBtn.Set("Thử kết nối lại", cloud != null && cloud.Status == CloudStatus.Offline);
            _confirmBtn.Set(busy ? "Đang xử lý…" : _formMode == Form.Create ? "Tạo tài khoản" : "Đăng nhập", !busy);

            string msg = cloud?.Message ?? "";
            if (showForm && string.IsNullOrEmpty(msg))
                msg = _formMode == Form.Create
                    ? "Tên 3–20 ký tự (chữ không dấu, số, . - @ _). Mật khẩu 8–30 ký tự có chữ hoa, thường, số và ký tự đặc biệt."
                    : _game.State != null && cloud != null && cloud.IsGuest
                        ? "Lưu ý: tài khoản khách hiện tại chưa có tên đăng nhập, sau khi đổi sẽ không quay lại được."
                        : "";
            _message.text = msg;
            _message.color = cloud != null && cloud.MessageIsError ? Palette.Red : showForm && string.IsNullOrEmpty(cloud?.Message) ? Palette.TextSoft : Palette.Green;
        }

        private void UpdateConflict()
        {
            var p = CloudSync.Instance?.Pending;
            bool show = p != null;
            if (_conflictRoot.gameObject.activeSelf != show) _conflictRoot.gameObject.SetActive(show);
            if (!show) return;
            _conflictRoot.SetAsLastSibling();
            _conflictInfo.text = p.afterSignIn
                ? "Tài khoản này đã có tiến trình trên mạng. Bạn muốn chơi tiếp bản nào? Bản không chọn sẽ bị thay thế."
                : "Tiến trình trên mạng khác với máy này (có thể bạn đã chơi trên máy khác). Chọn bản muốn chơi tiếp.";
            _localText.text = CloudSync.Instance.Describe(_game.State);
            _cloudText.text = CloudSync.Instance.Describe(p.cloud);
        }
    }
}
