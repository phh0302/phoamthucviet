using System.Collections.Generic;
using UnityEngine;

namespace PhoAmThucViet.Visual
{
    /// <summary>
    /// Hình 3D các món trang bị mặc trên đầu bếp chính (dựng bằng MeshKit, cùng phong cách chibi):
    /// mũ (thay mũ đầu bếp mặc định), tạp dề (trước bụng), dao (tay phải), nồi/chảo (tay trái, chỉ khi nấu).
    /// Mã món giống mã trang bị (Core/EquipmentData.id). Mã lạ thì dùng hình chung của vị trí đó.
    /// Toạ độ: mũ và tạp dề theo thân nhân vật; dao và nồi theo tay (gốc ở vai, tay buông theo -Y, bàn tay ở y = -0,37).
    /// </summary>
    public static class GearLooks
    {
        private static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

        private static Color C(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);

        private static readonly Color White = new Color(0.97f, 0.97f, 0.95f);
        private static readonly Color Gold = C(246, 200, 76);
        private static readonly Color Red = C(214, 38, 44);
        private static readonly Color Black = C(40, 34, 36);
        private static readonly Color Straw = C(240, 214, 140);
        private static readonly Color Steel = C(206, 212, 222);

        private static Mesh Get(string key, System.Action<MeshKit> build)
        {
            if (Cache.TryGetValue(key, out var m)) return m;
            var k = new MeshKit();
            build(k);
            m = k.Build(key);
            Cache[key] = m;
            return m;
        }

        // ======================= MŨ =======================

        /// <summary>Mũ theo mã món; null = mũ đầu bếp cao mặc định.</summary>
        public static Mesh Hat(string id) => Get("hat:" + (id ?? "default"), k => BuildHat(k, id));

        private static void TallToque(MeshKit k, float y, float scale = 1f)
        {
            k.Cylinder(new Vector3(0, y + 0.24f, -0.02f), 0.37f, 0.36f * scale, White);
            float top = y + 0.24f + 0.36f * scale + 0.14f;
            k.Ellipsoid(new Vector3(0, top, -0.02f), new Vector3(0.47f, 0.30f, 0.47f), White);
            k.Sphere(new Vector3(-0.2f, top + 0.06f, 0.12f), 0.2f, White);
            k.Sphere(new Vector3(0.2f, top + 0.06f, 0.12f), 0.2f, White);
        }

        private static void BuildHat(MeshKit k, string id)
        {
            float y = ChibiLooks.HeadY;
            switch (id)
            {
                case "mu-vai": // mũ lưỡi trai vải
                {
                    var c = C(226, 120, 84);
                    k.Ellipsoid(new Vector3(0, y + 0.15f, -0.02f), new Vector3(0.455f, 0.32f, 0.455f), c);
                    k.Ellipsoid(new Vector3(0, y + 0.17f, 0.36f), new Vector3(0.30f, 0.04f, 0.24f), c * 0.88f);
                    k.Sphere(new Vector3(0, y + 0.47f, -0.02f), 0.05f, c * 0.85f);
                    break;
                }
                case "non-la":
                    k.Cone(new Vector3(0, y + 0.26f, 0), 0.66f, 0.48f, Straw);
                    k.Cylinder(new Vector3(0, y + 0.26f, 0), 0.665f, 0.02f, C(214, 186, 110), outline: false);
                    break;
                case "mu-dau-bep-trang": // mũ đầu bếp thấp
                    k.Cylinder(new Vector3(0, y + 0.24f, -0.02f), 0.37f, 0.22f, White);
                    k.Ellipsoid(new Vector3(0, y + 0.58f, -0.02f), new Vector3(0.44f, 0.24f, 0.44f), White);
                    break;
                case "khan-xep-ngu-thien": // khăn xếp vàng, dải đỏ, ngọc trước trán
                {
                    var gold = C(240, 190, 60);
                    k.Ellipsoid(new Vector3(0, y + 0.22f, -0.02f), new Vector3(0.49f, 0.29f, 0.49f), gold);
                    for (int i = -1; i <= 1; i++) // nếp khăn xếp chéo phía trước
                        k.Ellipsoid(new Vector3(0.14f * i, y + 0.26f + 0.02f * Mathf.Abs(i), 0.40f - 0.06f * Mathf.Abs(i)), new Vector3(0.09f, 0.16f, 0.06f), C(214, 150, 40), outline: false);
                    k.Cylinder(new Vector3(0, y + 0.09f, -0.02f), 0.455f, 0.09f, C(200, 40, 40));
                    k.Sphere(new Vector3(0, y + 0.30f, 0.47f), 0.06f, C(220, 40, 60));
                    break;
                }
                case "mu-tong-bep-truong": // mũ đầu bếp cao hơn, dải vàng, sao
                    TallToque(k, y, 1.35f);
                    k.Cylinder(new Vector3(0, y + 0.23f, -0.02f), 0.385f, 0.11f, Gold);
                    k.Prism(new Vector3(0, y + 0.285f, 0.37f), MeshKit.StarPoints(0.075f), 0.03f, C(255, 246, 200));
                    break;
                case "non-quai-thao": // nón phẳng rộng vành, tua đen hai bên
                {
                    k.Cylinder(new Vector3(0, y + 0.24f, 0), 0.36f, 0.20f, C(214, 180, 104));
                    k.Cylinder(new Vector3(0, y + 0.30f, 0), 0.80f, 0.05f, Straw);
                    k.Cylinder(new Vector3(0, y + 0.35f, 0), 0.30f, 0.06f, C(214, 180, 104));
                    for (int s = -1; s <= 1; s += 2)
                    for (int j = -2; j <= 2; j++)
                        k.Cylinder(new Vector3(s * 0.72f, y - 0.02f, j * 0.06f), 0.022f, 0.32f, Black, outline: false, seg: 6);
                    break;
                }
                case "mu-phuong-hoang": // vương miện vàng, lông vũ đỏ cam
                {
                    Color[] feather = { C(240, 90, 40), C(250, 150, 40), C(250, 200, 60), C(250, 150, 40), C(240, 90, 40) };
                    for (int i = -2; i <= 2; i++)
                    {
                        k.M = Matrix4x4.TRS(new Vector3(i * 0.12f, y + 0.36f, -0.06f), Quaternion.Euler(0, 0, -i * 22f), Vector3.one);
                        k.Ellipsoid(new Vector3(0, 0.28f, 0), new Vector3(0.09f, 0.30f, 0.05f), feather[i + 2]);
                        k.M = Matrix4x4.identity;
                    }
                    k.Ellipsoid(new Vector3(0, y + 0.38f, -0.02f), new Vector3(0.43f, 0.10f, 0.43f), Gold);
                    k.Cylinder(new Vector3(0, y + 0.18f, -0.02f), 0.455f, 0.22f, Gold);
                    k.Sphere(new Vector3(0, y + 0.29f, 0.46f), 0.07f, C(220, 30, 50));
                    break;
                }
                case "ult-tet-3": // Mũ Thần Tài: mũ cánh chuồn đỏ, cánh đen hai bên, đồng tiền vàng
                {
                    k.Ellipsoid(new Vector3(0, y + 0.22f, -0.02f), new Vector3(0.48f, 0.34f, 0.48f), C(214, 34, 40));
                    k.Cylinder(new Vector3(0, y + 0.08f, -0.02f), 0.47f, 0.11f, Black);
                    k.Cylinder(new Vector3(0, y + 0.115f, -0.02f), 0.48f, 0.035f, Gold, outline: false);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        k.Ellipsoid(new Vector3(s * 0.74f, y + 0.36f, -0.12f), new Vector3(0.30f, 0.09f, 0.035f), Black);
                        k.Ellipsoid(new Vector3(s * 0.74f, y + 0.36f, -0.09f), new Vector3(0.22f, 0.055f, 0.02f), C(214, 34, 40), outline: false);
                        k.Cylinder(new Vector3(s * 0.40f, y + 0.30f, -0.12f), 0.035f, 0.10f, Black, outline: false, seg: 8);
                    }
                    k.Ellipsoid(new Vector3(0, y + 0.33f, 0.45f), new Vector3(0.12f, 0.12f, 0.03f), Gold);
                    k.Box(new Vector3(0, y + 0.29f, 0.47f), new Vector3(0.07f, 0.07f, 0.02f), C(170, 110, 30), outline: false);
                    break;
                }
                case "ult-trungthu-3": // Mũ Chú Cuội: mũ rơm vành rộng, dải lá xanh, trăng khuyết
                {
                    k.Cylinder(new Vector3(0, y + 0.20f, 0), 0.74f, 0.04f, Straw);
                    k.Cylinder(new Vector3(0, y + 0.20f, 0), 0.38f, 0.26f, C(242, 210, 120));
                    k.Ellipsoid(new Vector3(0, y + 0.46f, 0), new Vector3(0.38f, 0.07f, 0.38f), C(242, 210, 120));
                    k.Cylinder(new Vector3(0, y + 0.24f, 0), 0.39f, 0.08f, C(70, 150, 80));
                    for (int i = -1; i <= 1; i++)
                        k.Ellipsoid(new Vector3(0.16f * i, y + 0.30f, 0.37f - 0.05f * Mathf.Abs(i)), new Vector3(0.06f, 0.10f, 0.03f), C(110, 190, 100));
                    k.Sphere(new Vector3(0.24f, y + 0.40f, 0.30f), 0.08f, C(255, 230, 120));
                    break;
                }
                case "ult-quockhanh-3": // Mũ Hòa Bình: mũ cối xanh, huy hiệu sao vàng nền đỏ
                {
                    var g = C(110, 140, 80);
                    k.Cylinder(new Vector3(0, y + 0.13f, -0.02f), 0.62f, 0.035f, g * 0.88f);
                    k.Ellipsoid(new Vector3(0, y + 0.22f, -0.02f), new Vector3(0.48f, 0.32f, 0.48f), g);
                    k.Sphere(new Vector3(0, y + 0.55f, -0.02f), 0.05f, g * 0.85f);
                    k.Ellipsoid(new Vector3(0, y + 0.30f, 0.44f), new Vector3(0.11f, 0.11f, 0.03f), Red);
                    k.Prism(new Vector3(0, y + 0.30f, 0.47f), MeshKit.StarPoints(0.08f), 0.02f, C(255, 222, 50));
                    break;
                }
                case "ult-noel-3": // Mũ Giáng Sinh
                {
                    k.M = Matrix4x4.TRS(new Vector3(0, y + 0.28f, -0.02f), Quaternion.Euler(0, 0, -28f), Vector3.one);
                    k.Cone(Vector3.zero, 0.42f, 0.66f, Red);
                    var tip = k.M.MultiplyPoint3x4(new Vector3(0, 0.66f, 0));
                    k.M = Matrix4x4.identity;
                    k.Sphere(tip, 0.12f, White);
                    k.Cylinder(new Vector3(0, y + 0.16f, -0.02f), 0.49f, 0.15f, White);
                    break;
                }
                default:
                    TallToque(k, y);
                    break;
            }
        }

        // ======================= TẠP DỀ =======================

        private const float ApronY = 0.62f, ApronZ = 0.19f, ApronRx = 0.30f, ApronRy = 0.30f, ApronRz = 0.14f;

        /// <summary>Điểm nằm trên mặt trước tạp dề (để dán hoa văn).</summary>
        private static Vector3 OnApron(float x, float y, float lift = 0.006f)
        {
            float u = x / ApronRx, v = (y - ApronY) / ApronRy;
            float z = ApronZ + ApronRz * Mathf.Sqrt(Mathf.Max(0, 1 - u * u - v * v)) + lift;
            return new Vector3(x, y, z);
        }

        private static void Dot(MeshKit k, float x, float y, float r, Color c) =>
            k.Ellipsoid(OnApron(x, y), new Vector3(r, r, 0.02f), c, outline: false, seg: 10, rings: 6);

        private static void Flower(MeshKit k, float x, float y, float r, Color petal, Color center)
        {
            for (int i = 0; i < 5; i++)
            {
                float a = Mathf.PI / 2 + i * Mathf.PI * 2 / 5;
                Dot(k, x + Mathf.Cos(a) * r * 0.6f, y + Mathf.Sin(a) * r * 0.6f, r * 0.45f, petal);
            }
            k.Ellipsoid(OnApron(x, y, 0.012f), new Vector3(r * 0.3f, r * 0.3f, 0.02f), center, outline: false, seg: 8, rings: 5);
        }

        private static void Star(MeshKit k, float x, float y, float r, Color c) =>
            k.Prism(OnApron(x, y, 0.008f), MeshKit.StarPoints(r), 0.02f, c);

        /// <summary>Tạp dề theo mã món; null = không mặc tạp dề.</summary>
        public static Mesh Apron(string id) => id == null ? null : Get("apron:" + id, k => BuildApron(k, id));

        private static void BuildApron(MeshKit k, string id)
        {
            Color main;
            System.Action deco = null;
            switch (id)
            {
                case "tap-de-vai": main = C(234, 218, 184); deco = () => Pocket(k, C(214, 194, 150)); break;
                case "tap-de-bo":
                    main = C(74, 112, 170);
                    deco = () =>
                    {
                        Pocket(k, C(64, 98, 152));
                        Dot(k, -0.08f, 0.60f, 0.014f, C(230, 190, 110));
                        Dot(k, 0.08f, 0.60f, 0.014f, C(230, 190, 110));
                    };
                    break;
                case "tap-de-lua-ha-dong":
                    main = C(238, 158, 196);
                    deco = () =>
                    {
                        Flower(k, -0.12f, 0.55f, 0.07f, C(255, 250, 252), C(250, 210, 120));
                        Flower(k, 0.13f, 0.70f, 0.055f, C(255, 250, 252), C(250, 210, 120));
                        Flower(k, 0.08f, 0.45f, 0.045f, C(255, 250, 252), C(250, 210, 120));
                    };
                    break;
                case "tap-de-theu-rong": // rồng vàng uốn lượn
                    main = C(200, 40, 40);
                    deco = () =>
                    {
                        for (int i = 0; i < 14; i++)
                        {
                            float t = i / 13f;
                            Dot(k, -0.18f + 0.34f * t, 0.42f + 0.38f * t + 0.05f * Mathf.Sin(t * Mathf.PI * 3), 0.028f, Gold);
                        }
                        Dot(k, 0.17f, 0.82f, 0.045f, Gold);
                    };
                    break;
                case "tap-de-gam-hoang-gia": // gấm vàng hoa văn thoi
                    main = C(244, 194, 64);
                    deco = () =>
                    {
                        for (int r = 0; r < 4; r++)
                        for (int c = -2; c <= 2; c++)
                        {
                            float x = c * 0.1f + (r % 2 == 0 ? 0 : 0.05f), yy = 0.42f + r * 0.11f;
                            if (Mathf.Abs(x) > 0.24f) continue;
                            k.Prism(OnApron(x, yy, 0.006f), new[] { new Vector2(0, 0.03f), new Vector2(0.03f, 0), new Vector2(0, -0.03f), new Vector2(-0.03f, 0) }, 0.015f, C(200, 130, 30));
                        }
                    };
                    break;
                case "tap-de-lua-van-phuc":
                    main = C(54, 150, 162);
                    deco = () =>
                    {
                        foreach (var p in new[] { new Vector2(-0.12f, 0.52f), new Vector2(0.12f, 0.52f), new Vector2(0, 0.68f), new Vector2(-0.12f, 0.80f), new Vector2(0.12f, 0.80f) })
                        {
                            Dot(k, p.x, p.y, 0.05f, C(150, 220, 220));
                            k.Ellipsoid(OnApron(p.x, p.y, 0.012f), new Vector3(0.032f, 0.032f, 0.02f), C(60, 150, 160), outline: false, seg: 10, rings: 6);
                        }
                    };
                    break;
                case "tap-de-tho-cam": // nền đen, sọc thổ cẩm nhiều màu
                    main = C(40, 34, 40);
                    deco = () =>
                    {
                        Color[] cols = { C(214, 50, 50), C(250, 196, 60), C(60, 150, 90), C(60, 110, 200), C(240, 240, 230) };
                        for (int i = 0; i < 5; i++)
                        {
                            float yy = 0.40f + i * 0.095f;
                            k.Ellipsoid(OnApron(0, yy, 0.0f), new Vector3(0.29f * Mathf.Sqrt(Mathf.Max(0.1f, 1 - Mathf.Pow((yy - ApronY) / ApronRy, 2))), 0.026f, 0.04f), cols[i], outline: false, seg: 12, rings: 6);
                        }
                    };
                    break;
                case "ult-tet-2": // hoa mai vàng trên nền đỏ
                    main = C(206, 30, 40);
                    deco = () =>
                    {
                        Flower(k, -0.13f, 0.48f, 0.07f, C(255, 216, 50), C(230, 120, 30));
                        Flower(k, 0.10f, 0.62f, 0.08f, C(255, 216, 50), C(230, 120, 30));
                        Flower(k, -0.06f, 0.78f, 0.06f, C(255, 216, 50), C(230, 120, 30));
                        Flower(k, 0.16f, 0.44f, 0.05f, C(255, 216, 50), C(230, 120, 30));
                    };
                    break;
                case "ult-trungthu-2": // đèn ông sao trên nền xanh đêm
                    main = C(40, 46, 104);
                    deco = () =>
                    {
                        Star(k, 0, 0.60f, 0.17f, C(255, 210, 60));
                        k.Prism(OnApron(0, 0.60f, 0.022f), MeshKit.StarPoints(0.10f), 0.02f, C(240, 70, 50));
                        Dot(k, -0.18f, 0.80f, 0.015f, C(255, 250, 200));
                        Dot(k, 0.19f, 0.42f, 0.015f, C(255, 250, 200));
                    };
                    break;
                case "ult-quockhanh-2": // sao vàng nền đỏ
                    main = C(220, 36, 40);
                    deco = () => Star(k, 0, 0.60f, 0.17f, C(255, 222, 50));
                    break;
                case "ult-noel-2": // áo ông già Noel: viền lông trắng, thắt lưng đen khoá vàng
                    main = C(214, 38, 44);
                    deco = () =>
                    {
                        k.Ellipsoid(new Vector3(0, 0.40f, ApronZ), new Vector3(0.27f, 0.07f, 0.13f), White);
                        k.Ellipsoid(new Vector3(0, 0.64f, ApronZ + 0.005f), new Vector3(0.305f, 0.045f, 0.145f), Black, outline: false);
                        k.Box(OnApron(0, 0.62f, 0.0f), new Vector3(0.10f, 0.08f, 0.03f), Gold, outline: false);
                    };
                    break;
                default: main = White; break;
            }
            k.Ellipsoid(new Vector3(0, ApronY, ApronZ), new Vector3(ApronRx, ApronRy, ApronRz), main);
            // dây đeo cổ
            for (int s = -1; s <= 1; s += 2)
                k.Ellipsoid(new Vector3(0.12f * s, 0.93f, 0.25f), new Vector3(0.035f, 0.10f, 0.04f), main * 0.85f, outline: false);
            deco?.Invoke();
        }

        private static void Pocket(MeshKit k, Color c) =>
            k.Ellipsoid(OnApron(0, 0.50f, -0.01f), new Vector3(0.12f, 0.07f, 0.03f), c);

        // ======================= DAO (tay phải) =======================

        private enum BladeShape { Chef, Cleaver, Willow }

        /// <summary>Dao cầm tay theo mã món; null = tay không.</summary>
        public static Mesh Knife(string id) => id == null ? null : Get("knife:" + id, k => BuildKnife(k, id));

        private static void BuildKnife(MeshKit k, string id)
        {
            var shape = BladeShape.Chef;
            Color blade = Steel, handle = C(139, 90, 52), bolster = C(150, 150, 160);
            System.Action deco = null;
            switch (id)
            {
                case "dao-thai-thit": break;
                case "dao-da-sy": blade = C(160, 168, 182); handle = C(98, 60, 36); bolster = C(196, 150, 70); break;
                case "dao-pho-gia-truyen": shape = BladeShape.Cleaver; handle = C(196, 62, 50); break;
                case "dao-vang-ngu-thien": blade = C(246, 200, 76); handle = C(170, 36, 40); bolster = C(250, 220, 120); break;
                case "dao-ngu-thien-phong": blade = C(222, 228, 238); handle = C(160, 40, 40); bolster = C(240, 200, 90); break;
                case "dao-than-bep": // lưỡi tối, viền lửa cam
                    blade = C(78, 80, 100); handle = C(40, 36, 44); bolster = C(230, 120, 40);
                    deco = () => k.Box(new Vector3(0, -0.86f, 0.115f), new Vector3(0.032f, 0.30f, 0.03f), C(255, 150, 40), outline: false);
                    break;
                case "dao-ngoc-bich": blade = C(96, 196, 146); handle = C(240, 236, 222); bolster = C(232, 190, 80); break;
                case "dao-lieu-diep": shape = BladeShape.Willow; blade = C(220, 226, 236); handle = C(40, 40, 46); bolster = C(200, 200, 210); break;
                case "ult-tet-0": // Long Phụng: lưỡi vàng, vảy đỏ
                    blade = C(250, 206, 80); handle = C(200, 30, 36); bolster = C(250, 226, 130);
                    deco = () => { for (int i = 0; i < 3; i++) k.Sphere(new Vector3(0, -0.60f - i * 0.08f, 0.05f), 0.028f, C(200, 40, 30), outline: false); };
                    break;
                case "ult-trungthu-0": // Cung Trăng: lưỡi bạc xanh, sao vàng
                    shape = BladeShape.Willow; blade = C(226, 232, 255); handle = C(150, 120, 200); bolster = C(250, 220, 120);
                    deco = () => StarOnBlade(k, -0.70f, 0.04f, C(255, 214, 90));
                    break;
                case "ult-quockhanh-0": // Sao Vàng: dao chặt đỏ, sao vàng
                    shape = BladeShape.Cleaver; blade = C(214, 42, 40); handle = C(110, 30, 26); bolster = C(250, 210, 80);
                    deco = () => StarOnBlade(k, -0.68f, 0.07f, C(255, 222, 60));
                    break;
                case "ult-noel-0": // Băng Tuyết
                    blade = C(170, 220, 250); handle = C(236, 244, 255); bolster = C(120, 170, 220);
                    deco = () => StarOnBlade(k, -0.68f, 0.05f, Color.white, 6, 0.3f);
                    break;
            }

            // Cán dao xuyên qua nắm tay, lưỡi chĩa theo hướng tay (-Y), lưỡi sắc quay ra trước (+Z)
            k.Cylinder(new Vector3(0, -0.50f, 0.0f), 0.042f, 0.22f, handle, seg: 10);
            float w = shape == BladeShape.Cleaver ? 0.20f : shape == BladeShape.Willow ? 0.075f : 0.13f;
            float len = shape == BladeShape.Cleaver ? 0.30f : 0.36f;
            k.Box(new Vector3(0, -0.54f, w * 0.5f - 0.03f), new Vector3(0.06f, 0.04f, w + 0.03f), bolster);
            k.Box(new Vector3(0, -0.54f - len, w * 0.5f - 0.02f), new Vector3(0.025f, len, w), blade);
            if (shape != BladeShape.Cleaver)
            {
                // mũi dao: hình nêm nhỏ cuối lưỡi, nằm trong mặt phẳng YZ
                k.M = Matrix4x4.TRS(new Vector3(0, -0.54f - len, w * 0.5f - 0.02f), Quaternion.Euler(0, 90, 0), Vector3.one);
                // toạ độ quanh trọng tâm tam giác để toả tam giác từ tâm cho đúng
                var g = new Vector2(w / 6f, -0.3f * w);
                k.Prism(new Vector3(g.x, g.y, 0), new[] { new Vector2(-w * 0.5f, 0) - g, new Vector2(w * 0.5f, 0) - g, new Vector2(w * 0.5f, -w * 0.9f) - g }, 0.025f, blade, outline: true);
                k.M = Matrix4x4.identity;
            }
            deco?.Invoke();
        }

        private static void StarOnBlade(MeshKit k, float y, float r, Color c, int points = 5, float inner = 0.45f)
        {
            for (int s = -1; s <= 1; s += 2)
            {
                k.M = Matrix4x4.TRS(new Vector3(0.016f * s, y, 0.05f), Quaternion.Euler(0, 90 * s, 0), Vector3.one);
                k.Prism(Vector3.zero, MeshKit.StarPoints(r, points, inner), 0.006f, c);
                k.M = Matrix4x4.identity;
            }
        }

        // ======================= NỒI / CHẢO (tay trái, khi nấu) =======================

        private enum WareShape { Pan, Pot, TallPot }

        /// <summary>Nồi/chảo cầm tay theo mã món; null = không cầm.</summary>
        public static Mesh Cookware(string id) => id == null ? null : Get("ware:" + id, k => BuildWare(k, id));

        private static void BuildWare(MeshKit k, string id)
        {
            var shape = WareShape.Pot;
            Color body = Steel, rim = C(226, 230, 236), lid = C(196, 202, 212), knob = Black;
            bool fire = false, star = false;
            switch (id)
            {
                case "noi-nhom": break;
                case "chao-gang": shape = WareShape.Pan; body = C(52, 52, 58); rim = C(70, 70, 78); break;
                case "noi-nuoc-dung": shape = WareShape.TallPot; break;
                case "noi-dong-cung-dinh": body = C(206, 112, 62); rim = C(232, 176, 90); lid = C(214, 124, 70); knob = Gold; break;
                case "bep-lo-hoang-cung": body = C(190, 92, 64); rim = Gold; lid = C(170, 80, 56); knob = Gold; fire = true; break;
                case "noi-ap-suat-than-toc": body = C(176, 196, 220); lid = C(150, 170, 196); knob = C(220, 40, 40); break;
                case "chao-lua-rong": shape = WareShape.Pan; body = C(46, 46, 54); rim = C(200, 50, 30); fire = true; break;
                case "ult-tet-1": body = C(238, 182, 70); rim = C(206, 34, 38); lid = C(244, 196, 84); knob = C(214, 40, 40); break;
                case "ult-trungthu-1": body = C(244, 240, 255); rim = C(214, 200, 246); lid = C(226, 220, 250); knob = C(255, 226, 120); break;
                case "ult-quockhanh-1": body = C(214, 40, 40); rim = Gold; lid = C(226, 56, 50); knob = Gold; star = true; break;
                case "ult-noel-1": body = C(150, 96, 60); rim = C(240, 236, 226); lid = C(170, 116, 74); knob = C(226, 40, 40); break;
            }

            // Cán cầm nối từ bàn tay ra
            k.Cylinder(new Vector3(0, -0.62f, 0), 0.035f, 0.28f, Black, seg: 8);
            // Thân nồi: trục nồi xoay 60° quanh X để khi tay đưa ra trước (lúc nấu) nồi nằm gần ngang
            float r = shape == WareShape.Pan ? 0.26f : 0.21f;
            k.M = Matrix4x4.TRS(new Vector3(0, -0.62f - r * 0.9f, -0.05f), Quaternion.Euler(60, 0, 0), Vector3.one);
            switch (shape)
            {
                case WareShape.Pan:
                    k.Cylinder(Vector3.zero, r, 0.07f, body, topScale: 1.15f);
                    k.Cylinder(new Vector3(0, 0.068f, 0), r * 1.15f, 0.015f, rim, outline: false);
                    k.Ellipsoid(new Vector3(0, 0.085f, 0), new Vector3(r * 0.7f, 0.03f, r * 0.7f), C(240, 190, 90), outline: false); // món trong chảo
                    break;
                default:
                {
                    float h = shape == WareShape.TallPot ? 0.30f : 0.20f;
                    k.Cylinder(Vector3.zero, r, h, body);
                    k.Cylinder(new Vector3(0, h - 0.02f, 0), r * 1.08f, 0.04f, rim);
                    k.Ellipsoid(new Vector3(0, h + 0.02f, 0), new Vector3(r * 0.98f, 0.08f, r * 0.98f), lid);
                    k.Sphere(new Vector3(0, h + 0.11f, 0), 0.04f, knob);
                    if (star) k.Ellipsoid(new Vector3(0, h * 0.5f, r * 0.98f), new Vector3(0.07f, 0.07f, 0.02f), Gold, outline: false);
                    break;
                }
            }
            if (fire) // lửa dưới đáy
            {
                k.Ellipsoid(new Vector3(0, -0.06f, 0), new Vector3(r * 0.7f, 0.06f, r * 0.7f), C(255, 120, 40), outline: false);
                k.Ellipsoid(new Vector3(0, -0.035f, 0), new Vector3(r * 0.45f, 0.05f, r * 0.45f), C(255, 214, 90), outline: false);
            }
            k.M = Matrix4x4.identity;
        }
    }
}
