using System;
using PhoAmThucViet.UI;
using UnityEngine;
using UnityEngine.UI;

namespace PhoAmThucViet.Monetization
{
    /// <summary>
    /// Màn che dùng cho quảng cáo / mua hàng giả lập: tiêu đề, nội dung, đếm ngược (tùy chọn) và 2 nút.
    /// Nằm trên mọi giao diện khác (canvas riêng, thứ tự vẽ 200).
    /// </summary>
    public class SimOverlay : MonoBehaviour
    {
        private Canvas _canvas;
        private Text _title, _body;
        private UIButton _ok, _cancel;
        private Action _onOk, _onCancel;
        private float _countdown;
        private string _okLabel;

        public bool IsOpen => _canvas != null && _canvas.gameObject.activeSelf;

        private void Build()
        {
            if (_canvas != null) return;
            _canvas = UIKit.CreateCanvas("Simulated Overlay", 200);
            DontDestroyOnLoad(_canvas.gameObject);
            var dim = UIKit.Panel("Dim", _canvas.transform, new Color(0, 0, 0, 0.85f)).rectTransform;
            dim.Place(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var box = UIKit.Panel("Box", dim, new Color(0.13f, 0.1f, 0.08f), UIKit.Rounded).rectTransform;
            box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
            box.sizeDelta = new Vector2(900, 760);

            _title = UIKit.Label("Title", box, "", 42, Palette.Gold, TextAnchor.MiddleCenter, true);
            _title.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -130), new Vector2(-30, -30));
            _body = UIKit.Label("Body", box, "", 30, Color.white, TextAnchor.UpperCenter);
            _body.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 1), new Vector2(40, 290), new Vector2(-40, -150));

            _ok = UIKit.MakeButton("Ok", box, "", 32, Palette.Green, () => Close(true));
            _ok.bg.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 150), new Vector2(-40, 260));
            _cancel = UIKit.MakeButton("Cancel", box, "", 28, new Color(0.45f, 0.38f, 0.32f), () => Close(false));
            _cancel.bg.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 30), new Vector2(-40, 130));
            _canvas.gameObject.SetActive(false);
        }

        /// <param name="countdown">Số giây phải chờ trước khi nút đồng ý bấm được (quảng cáo); 0 = bấm ngay.</param>
        public void Show(string title, string body, string okLabel, string cancelLabel, float countdown, Action onOk, Action onCancel)
        {
            Build();
            _title.text = title;
            _body.text = body;
            _okLabel = okLabel;
            _cancel.Set(cancelLabel, true);
            _onOk = onOk;
            _onCancel = onCancel;
            _countdown = countdown;
            _canvas.gameObject.SetActive(true);
            UpdateOk();
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (_countdown > 0) _countdown -= Time.unscaledDeltaTime;
            UpdateOk();
        }

        private void UpdateOk()
        {
            if (_countdown > 0) _ok.Set($"{_okLabel} ({Mathf.CeilToInt(_countdown)})", false);
            else _ok.Set(_okLabel, true);
        }

        private void Close(bool ok)
        {
            if (ok && _countdown > 0) return;
            _canvas.gameObject.SetActive(false);
            var cb = ok ? _onOk : _onCancel;
            _onOk = _onCancel = null;
            cb?.Invoke();
        }

        private static SimOverlay _instance;

        public static SimOverlay Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("Simulated Overlay Driver");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<SimOverlay>();
                }
                return _instance;
            }
        }
    }
}
