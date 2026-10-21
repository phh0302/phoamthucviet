using System.Collections.Generic;
using UnityEngine;

namespace PhoAmThucViet.Visual
{
    public enum ChibiKind { HeadChef, Chef, Waiter, Customer }

    /// <summary>Tư thế do RestaurantView đặt. Auto = tự đi/đứng theo chuyển động.</summary>
    public enum ChibiAction { Auto, Cook, Carry, Sit, Eat, Cheer }

    /// <summary>
    /// Nhân vật chibi 3D dựng hoàn toàn bằng code (đầu to, thân ngắn, viền đen kiểu hoạt hình).
    /// RestaurantView chỉ cần đặt vị trí (x, y) như trước; Chibi tự tính độ sâu, hướng mặt và hoạt ảnh đi.
    ///
    /// Thay bằng mô hình thật: đặt prefab vào Resources/Models/characters/ với tên
    /// head_chef, chef, waiter, customer_1 … customer_8 (mặt quay theo +Z, chân ở gốc, cao khoảng 2,3 đơn vị).
    /// Nếu prefab có Animator, các tham số sau được đặt nếu có: Speed (float), Cook, Carry, Sit, Eat (bool).
    /// </summary>
    public class Chibi : MonoBehaviour
    {
        /// <summary>Góc nghiêng người về phía camera, để thấy được đỉnh đầu như góc nhìn từ trên xuống.</summary>
        public const float TiltDegrees = -28f;

        /// <summary>
        /// Độ sâu theo vị trí dọc, khớp đúng góc nghiêng: đi sâu vào sàn 1 đơn vị thì lên màn hình sin(28°)
        /// và xa camera cos(28°). Nhờ vậy nhân vật, bàn, quầy 3D che nhau đúng như cảnh thật.
        /// Cộng 25 để mọi thứ nằm sau mặt phẳng sprite (z = 0).
        /// </summary>
        public static float DepthFor(float y) => 25f + DepthPerY * y;

        public static readonly float DepthPerY = 1f / Mathf.Tan(-TiltDegrees * Mathf.Deg2Rad);

        public ChibiKind kind;
        public ChibiAction action;
        /// <summary>Hướng mặt khi đứng yên (độ). 180 = nhìn ra camera.</summary>
        public float faceYaw = 180f;
        /// <summary>Có ghế cố định ở bàn thì không hiện ghế nhựa riêng của khách.</summary>
        public bool useStool = true;
        /// <summary>Nâng người lên khi ngồi (đơn vị mô hình) cho khớp mặt ghế.</summary>
        public float seatLift;

        private Transform _tilt, _yaw, _body, _armL, _armR, _legL, _legR, _stool;
        private Vector2 _last;
        private bool _hasLast;
        private float _speed, _phase, _yawNow = 180f, _moveYaw = 180f, _seed;
        private Animator _anim;
        private HashSet<string> _animParams;

        // ================= Tạo nhân vật =================

        public static Chibi Create(Transform parent, ChibiKind kind, int variant, Vector2 at)
        {
            var go = new GameObject(kind.ToString());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(at.x, at.y, DepthFor(at.y));
            float scale = kind == ChibiKind.HeadChef ? 0.46f : kind == ChibiKind.Customer ? 0.40f : 0.42f;
            go.transform.localScale = Vector3.one * scale;

            var c = go.AddComponent<Chibi>();
            c.kind = kind;
            c._seed = Random.value * 10f;
            c.Build(variant);
            return c;
        }

        /// <summary>Điểm giữa hai tay khi bưng khay (toạ độ thế giới), để đặt đĩa món ăn.</summary>
        public Vector3 CarryPoint => _yaw.TransformPoint(new Vector3(0, 1.0f, 0.55f));

        private static string ModelName(ChibiKind kind, int variant)
        {
            switch (kind)
            {
                case ChibiKind.HeadChef: return "head_chef";
                case ChibiKind.Chef: return "chef";
                case ChibiKind.Waiter: return "waiter";
                default: return $"customer_{variant % ChibiLooks.CustomerVariants + 1}";
            }
        }

        private void Build(int variant)
        {
            // Bóng dưới chân (sprite vẽ trước nhân vật nên luôn nằm dưới)
            SpriteFactory.Block("Shadow", transform, new Vector2(0, 0.02f), new Vector2(1.5f, 0.55f), new Color(0, 0, 0, 0.22f), 3, circle: true);

            _tilt = Child("Tilt", transform, Vector3.zero);
            _tilt.localRotation = Quaternion.Euler(TiltDegrees, 0, 0);

            if (kind == ChibiKind.Customer)
            {
                _stool = Part("Stool", _tilt, ChibiLooks.StoolMesh(variant), Vector3.zero);
                _stool.gameObject.SetActive(false);
            }

            _yaw = Child("Yaw", _tilt, Vector3.zero);

            var prefab = Resources.Load<GameObject>("Models/characters/" + ModelName(kind, variant));
            if (prefab != null)
            {
                var model = Instantiate(prefab, _yaw, false);
                foreach (var r in model.GetComponentsInChildren<Renderer>()) r.sortingOrder = 6;
                _anim = model.GetComponentInChildren<Animator>();
                if (_anim != null)
                {
                    _animParams = new HashSet<string>();
                    foreach (var p in _anim.parameters) _animParams.Add(p.name);
                }
                return;
            }

            var look = ChibiLooks.For(kind, variant);
            _body = Child("Body", _yaw, Vector3.zero);
            Part("Torso", _body, ChibiLooks.TorsoMesh(look), Vector3.zero);
            var arm = ChibiLooks.ArmMesh(look);
            _armL = Part("ArmL", _body, arm, new Vector3(-0.36f, 0.95f, 0));
            _armR = Part("ArmR", _body, arm, new Vector3(0.36f, 0.95f, 0));
            var leg = ChibiLooks.LegMesh(look);
            _legL = Part("LegL", _yaw, leg, new Vector3(-0.16f, 0.42f, 0));
            _legR = Part("LegR", _yaw, leg, new Vector3(0.16f, 0.42f, 0));
        }

        private static Transform Child(string name, Transform parent, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            return t;
        }

        private static Transform Part(string name, Transform parent, Mesh mesh, Vector3 pos)
        {
            var t = Child(name, parent, pos);
            t.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = t.gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = ChibiLooks.Material;
            mr.sortingOrder = 6;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return t;
        }

        // ================= Hoạt ảnh =================

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || _yaw == null) return;

            // RestaurantView đặt (x, y); ở đây đổi z theo chiều sâu và đo tốc độ đi
            Vector2 p = transform.localPosition;
            Vector2 d = _hasLast ? p - _last : Vector2.zero;
            _last = p;
            _hasLast = true;
            if (d.sqrMagnitude > 2.25f) d = Vector2.zero; // dịch chuyển tức thời, không tính là đi
            _speed = Mathf.Lerp(_speed, d.magnitude / dt, 1f - Mathf.Exp(-12f * dt));
            transform.localPosition = new Vector3(p.x, p.y, DepthFor(p.y));

            bool seated = action == ChibiAction.Sit || action == ChibiAction.Eat;
            bool walking = !seated && _speed > 0.3f;
            if (walking && d.sqrMagnitude > 1e-7f) _moveYaw = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
            float targetYaw = walking ? _moveYaw : faceYaw;
            _yawNow = Mathf.MoveTowardsAngle(_yawNow, targetYaw, 900f * dt);
            _yaw.localRotation = Quaternion.Euler(0, _yawNow, 0);
            if (_stool != null) _stool.gameObject.SetActive(seated && useStool);
            _yaw.localPosition = new Vector3(0, seated ? seatLift : 0f, 0);

            if (_anim != null) { DriveAnimator(walking, seated); return; }

            float t = Time.time + _seed;
            if (walking) _phase += dt * Mathf.Clamp(_speed, 1.5f, 9f) * 3.4f;
            float swing = walking ? Mathf.Sin(_phase) : 0f;

            float legL = swing * 38f, legR = -swing * 38f;
            float armL = -swing * 34f, armR = swing * 34f;
            float bodyY = walking ? Mathf.Abs(Mathf.Cos(_phase)) * 0.08f : 0f;
            float lean = walking ? 6f : 0f;
            float hip = 0f;
            float breathe = 1f + Mathf.Sin(t * 2.4f) * 0.015f;

            switch (action)
            {
                case ChibiAction.Cook: // đảo chảo hai tay so le
                    armL = -60f + Mathf.Sin(t * 14f) * 24f;
                    armR = -60f + Mathf.Sin(t * 14f + Mathf.PI) * 24f;
                    bodyY = Mathf.Abs(Mathf.Sin(t * 7f)) * 0.035f;
                    lean = 8f;
                    break;
                case ChibiAction.Carry: // hai tay đưa ra trước đỡ khay
                    armL = armR = -80f + swing * 4f;
                    break;
                case ChibiAction.Sit:
                case ChibiAction.Eat:
                    hip = -0.3f;
                    legL = legR = -85f;
                    armL = armR = -25f;
                    bodyY = 0f;
                    if (action == ChibiAction.Eat)
                    {
                        armR = -115f + Mathf.Sin(t * 9f) * 28f; // đưa đũa lên miệng
                        armL = -40f;
                        bodyY = Mathf.Abs(Mathf.Sin(t * 9f)) * 0.03f;
                    }
                    break;
                case ChibiAction.Cheer: // giơ hai tay, nhún nhảy
                    armL = armR = -165f + Mathf.Sin(t * 12f) * 10f;
                    bodyY = Mathf.Abs(Mathf.Sin(t * 8f)) * 0.15f;
                    break;
            }

            _body.localPosition = new Vector3(0, bodyY + hip, 0);
            _body.localRotation = Quaternion.Euler(lean, 0, 0);
            _body.localScale = new Vector3(1f, breathe, 1f);
            _armL.localRotation = Quaternion.Euler(armL, 0, -10f);
            _armR.localRotation = Quaternion.Euler(armR, 0, 10f);
            _legL.localPosition = new Vector3(-0.16f, 0.42f + hip, 0);
            _legR.localPosition = new Vector3(0.16f, 0.42f + hip, 0);
            _legL.localRotation = Quaternion.Euler(legL, 0, 0);
            _legR.localRotation = Quaternion.Euler(legR, 0, 0);
        }

        private void DriveAnimator(bool walking, bool seated)
        {
            SetParam("Speed", walking ? _speed : 0f);
            SetParam("Cook", action == ChibiAction.Cook);
            SetParam("Carry", action == ChibiAction.Carry);
            SetParam("Sit", seated);
            SetParam("Eat", action == ChibiAction.Eat);
        }

        private void SetParam(string name, float v) { if (_animParams.Contains(name)) _anim.SetFloat(name, v); }
        private void SetParam(string name, bool v) { if (_animParams.Contains(name)) _anim.SetBool(name, v); }
    }
}
