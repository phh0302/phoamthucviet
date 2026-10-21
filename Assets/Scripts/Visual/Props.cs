using System.Collections.Generic;
using UnityEngine;

namespace PhoAmThucViet.Visual
{
    /// <summary>
    /// Đồ vật 3D dựng bằng code, cùng kiểu với nhân vật chibi (tô toon, viền đen) và cùng góc nghiêng / độ sâu,
    /// nên nhân vật đứng trước/sau bàn, quầy che nhau đúng. Mỗi mốc quán một kiểu:
    /// 0 xe đẩy inox + bàn nhựa thấp · 1 quầy gỗ + bàn gỗ vuông · 2 quầy ốp gạch + bàn tròn gỗ ·
    /// 3 bếp inox + bàn tròn mặt đá · 4 quầy gỗ đỏ viền vàng + bàn trải khăn đỏ.
    /// Gốc mỗi đồ vật đặt trên sàn; trục +Z của mô hình là hướng vào sâu trong quán.
    /// </summary>
    public static class Props
    {
        private static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

        private static Color C(float r, float g, float b) => new Color(r, g, b);

        private static Mesh Cached(string key, System.Func<Mesh> make)
        {
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = make();
            Cache[key] = m;
            return m;
        }

        /// <summary>Tạo một đồ vật đặt tại điểm sàn floorPos (toạ độ màn hình), trả về gốc.</summary>
        private static GameObject Spawn(string name, Transform parent, Vector2 floorPos, Mesh mesh, float scale = 1f)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(floorPos.x, floorPos.y, Chibi.DepthFor(floorPos.y));
            root.transform.localScale = Vector3.one * scale;
            var tilt = new GameObject("Model");
            tilt.transform.SetParent(root.transform, false);
            tilt.transform.localRotation = Quaternion.Euler(Chibi.TiltDegrees, 0, 0);
            tilt.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = tilt.AddComponent<MeshRenderer>();
            mr.sharedMaterial = ChibiLooks.Material;
            mr.sortingOrder = 6;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return root;
        }

        /// <summary>Đặt lại độ sâu khi dời đồ vật (giữ đúng thứ tự che với nhân vật).</summary>
        public static void MoveTo(Transform root, Vector2 floorPos) =>
            root.localPosition = new Vector3(floorPos.x, floorPos.y, Chibi.DepthFor(floorPos.y));

        // ================= Bàn =================

        /// <summary>Bàn ăn đặt sao cho tâm hình nằm quanh pos (giống vị trí sprite bàn cũ).</summary>
        public static GameObject Table(Transform parent, Vector2 pos, int tier, float width)
        {
            var mesh = Cached("table" + tier, () => TableMesh(tier));
            float scale = Mathf.Clamp(width / 1.0f, 0.6f, 1.1f);
            return Spawn("Table3D", parent, pos + new Vector2(0, -0.25f * scale), mesh, scale);
        }

        private static Mesh TableMesh(int tier)
        {
            var k = new MeshKit();
            float topY;
            switch (tier)
            {
                case 0: // bàn nhựa xanh thấp kiểu vỉa hè
                {
                    var blue = C(0.25f, 0.52f, 0.88f);
                    topY = 0.34f;
                    for (int sx = -1; sx <= 1; sx += 2)
                    for (int sz = -1; sz <= 1; sz += 2)
                        k.Box(new Vector3(0.36f * sx, 0, 0.26f * sz), new Vector3(0.07f, topY, 0.07f), blue * 0.8f);
                    k.Box(new Vector3(0, topY - 0.06f, 0), new Vector3(0.88f, 0.06f, 0.66f), blue);
                    break;
                }
                case 1: // bàn gỗ vuông
                {
                    var wood = C(0.62f, 0.40f, 0.22f);
                    topY = 0.45f;
                    for (int sx = -1; sx <= 1; sx += 2)
                    for (int sz = -1; sz <= 1; sz += 2)
                        k.Box(new Vector3(0.38f * sx, 0, 0.28f * sz), new Vector3(0.07f, topY, 0.07f), wood * 0.75f);
                    k.Box(new Vector3(0, topY - 0.07f, 0), new Vector3(0.94f, 0.07f, 0.74f), wood);
                    k.Box(new Vector3(0, topY - 0.005f, -0.12f), new Vector3(0.94f, 0.006f, 0.02f), wood * 0.85f, outline: false); // khe ván
                    k.Box(new Vector3(0, topY - 0.005f, 0.12f), new Vector3(0.94f, 0.006f, 0.02f), wood * 0.85f, outline: false);
                    break;
                }
                case 2: // bàn tròn gỗ một chân
                {
                    var wood = C(0.72f, 0.48f, 0.27f);
                    topY = 0.46f;
                    k.Cylinder(Vector3.zero, 0.26f, 0.04f, C(0.30f, 0.22f, 0.16f));
                    k.Cylinder(Vector3.zero, 0.07f, topY, C(0.30f, 0.22f, 0.16f));
                    k.Cylinder(new Vector3(0, topY - 0.06f, 0), 0.5f, 0.06f, wood, seg: 24);
                    break;
                }
                case 3: // bàn tròn mặt đá chân sắt
                {
                    topY = 0.46f;
                    var metal = C(0.22f, 0.22f, 0.25f);
                    k.Cylinder(Vector3.zero, 0.28f, 0.04f, metal);
                    k.Cylinder(Vector3.zero, 0.06f, topY, metal);
                    k.Cylinder(new Vector3(0, topY - 0.06f, 0), 0.5f, 0.06f, C(0.93f, 0.93f, 0.91f), seg: 24);
                    k.Ellipsoid(new Vector3(0.12f, topY + 0.001f, 0.1f), new Vector3(0.18f, 0.003f, 0.08f), C(0.82f, 0.82f, 0.85f), outline: false); // vân đá
                    break;
                }
                default: // bàn tròn trải khăn đỏ, mâm xoay vàng
                {
                    topY = 0.49f;
                    var cloth = C(0.78f, 0.13f, 0.12f);
                    k.Cylinder(new Vector3(0, 0.02f, 0), 0.6f, topY - 0.02f, cloth, seg: 24, topScale: 0.9f);
                    k.Cylinder(new Vector3(0, topY - 0.02f, 0), 0.55f, 0.03f, C(0.96f, 0.93f, 0.85f), seg: 24);
                    k.Cylinder(new Vector3(0, topY + 0.01f, 0), 0.2f, 0.03f, C(0.92f, 0.72f, 0.30f), seg: 20);
                    k.Sphere(new Vector3(0, topY + 0.1f, 0), 0.07f, C(0.95f, 0.95f, 0.92f)); // ấm trà
                    k.Cylinder(new Vector3(0, topY + 0.15f, 0), 0.025f, 0.04f, C(0.95f, 0.95f, 0.92f));
                    return k.Build("Table " + tier);
                }
            }
            // Ống đũa tre và chai tương ớt trên bàn
            k.Cylinder(new Vector3(0.24f, topY, 0.14f), 0.05f, 0.13f, C(0.85f, 0.72f, 0.45f));
            for (int i = 0; i < 3; i++)
                k.Cylinder(new Vector3(0.22f + i * 0.02f, topY + 0.12f, 0.13f + (i % 2) * 0.02f), 0.008f, 0.08f, C(0.95f, 0.88f, 0.70f), outline: false, seg: 6);
            k.Cylinder(new Vector3(0.36f, topY, 0.18f), 0.035f, 0.12f, C(0.85f, 0.15f, 0.12f));
            k.Sphere(new Vector3(0.36f, topY + 0.13f, 0.18f), 0.03f, C(0.95f, 0.85f, 0.30f), outline: false);
            return k.Build("Table " + tier);
        }

        // ================= Quầy nấu =================

        /// <summary>Kiểu bảng màu quầy: thân, mặt quầy, nẹp.</summary>
        private static void CounterPalette(int tier, bool locked, out Color body, out Color top, out Color trim)
        {
            switch (tier)
            {
                case 0: body = C(0.78f, 0.80f, 0.83f); top = C(0.90f, 0.91f, 0.93f); trim = C(0.25f, 0.52f, 0.88f); break;
                case 1: body = C(0.55f, 0.35f, 0.20f); top = C(0.70f, 0.48f, 0.28f); trim = C(0.40f, 0.25f, 0.14f); break;
                case 2: body = C(0.90f, 0.91f, 0.92f); top = C(0.62f, 0.43f, 0.26f); trim = C(0.25f, 0.45f, 0.72f); break;
                case 3: body = C(0.70f, 0.72f, 0.76f); top = C(0.84f, 0.86f, 0.88f); trim = C(0.45f, 0.47f, 0.50f); break;
                default: body = C(0.40f, 0.10f, 0.07f); top = C(0.94f, 0.93f, 0.89f); trim = C(0.92f, 0.72f, 0.30f); break;
            }
            if (locked)
            {
                body = Gray(body) * 0.7f; top = Gray(top) * 0.75f; trim = Gray(trim) * 0.7f;
            }
        }

        private static Color Gray(Color c)
        {
            float g = c.grayscale;
            return new Color(g, g, g);
        }

        public const float CounterHeight = 0.55f, CounterDepth = 0.75f;

        /// <summary>
        /// Quầy nấu rộng width, tâm hình quanh pos. cookware = kiểu dụng cụ trên quầy (nồi, chảo, xửng hấp, vỉ nướng).
        /// Trả về gốc; locked = quầy chưa mở (màu xám, không có dụng cụ).
        /// </summary>
        public static GameObject Counter(Transform parent, Vector2 pos, int tier, float width, int cookware, bool locked)
        {
            float w = Mathf.Round(width * 20f) / 20f;
            int kind = locked ? -1 : ((cookware % 4) + 4) % 4;
            var mesh = Cached($"counter{tier}_{w:0.00}_{kind}", () => CounterMesh(tier, w, kind));
            return Spawn(locked ? "Counter3D (khóa)" : "Counter3D", parent, pos + new Vector2(0, -0.42f), mesh);
        }

        private static Mesh CounterMesh(int tier, float w, int kind)
        {
            CounterPalette(tier, kind < 0, out var body, out var top, out var trim);
            const float H = CounterHeight, D = CounterDepth;
            var k = new MeshKit();
            k.Box(new Vector3(0, 0, D / 2), new Vector3(w, H - 0.06f, D), body);
            k.Box(new Vector3(0, H - 0.06f, D / 2), new Vector3(w + 0.06f, 0.06f, D + 0.06f), top);
            // Nẹp ngang mặt trước + vách chia ô tủ
            k.Box(new Vector3(0, H * 0.62f, -0.012f), new Vector3(w - 0.02f, 0.05f, 0.02f), trim, outline: false);
            int panels = Mathf.Max(1, Mathf.RoundToInt(w / 0.6f));
            for (int i = 1; i < panels; i++)
                k.Box(new Vector3(-w / 2 + w * i / panels, 0.04f, -0.008f), new Vector3(0.03f, H * 0.55f, 0.015f), body * 0.75f, outline: false);
            if (tier == 0) // xe đẩy: bánh xe hai bên
                for (int s = -1; s <= 1; s += 2)
                {
                    k.Ellipsoid(new Vector3(s * (w / 2 + 0.03f), 0.13f, 0.18f), new Vector3(0.04f, 0.13f, 0.13f), C(0.18f, 0.18f, 0.2f));
                    k.Ellipsoid(new Vector3(s * (w / 2 + 0.06f), 0.13f, 0.18f), new Vector3(0.02f, 0.05f, 0.05f), C(0.7f, 0.7f, 0.72f), outline: false);
                }
            if (tier == 4) // viền vàng chân quầy
                k.Box(new Vector3(0, 0, -0.01f), new Vector3(w + 0.02f, 0.05f, 0.03f), trim, outline: false);

            if (kind >= 0)
            {
                float y = H, x = -w * 0.28f, z = D * 0.58f;
                switch (kind)
                {
                    case 0: // nồi nước dùng to
                        k.Cylinder(new Vector3(x, y, z), 0.17f, 0.22f, C(0.40f, 0.42f, 0.46f));
                        k.Ellipsoid(new Vector3(x, y + 0.22f, z), new Vector3(0.17f, 0.05f, 0.17f), C(0.55f, 0.57f, 0.60f));
                        k.Sphere(new Vector3(x, y + 0.28f, z), 0.035f, C(0.2f, 0.2f, 0.22f));
                        break;
                    case 1: // bếp + chảo
                        k.Cylinder(new Vector3(x, y, z), 0.15f, 0.05f, C(0.15f, 0.15f, 0.17f));
                        k.Ellipsoid(new Vector3(x, y + 0.1f, z), new Vector3(0.21f, 0.06f, 0.21f), C(0.20f, 0.20f, 0.22f));
                        k.Ellipsoid(new Vector3(x, y + 0.13f, z), new Vector3(0.15f, 0.02f, 0.15f), C(0.85f, 0.55f, 0.25f), outline: false); // món trong chảo
                        k.Box(new Vector3(x + 0.3f, y + 0.1f, z), new Vector3(0.18f, 0.03f, 0.04f), C(0.35f, 0.22f, 0.12f));
                        break;
                    case 2: // xửng hấp tre
                        k.Cylinder(new Vector3(x, y, z), 0.16f, 0.09f, C(0.85f, 0.70f, 0.42f));
                        k.Cylinder(new Vector3(x, y + 0.09f, z), 0.16f, 0.09f, C(0.80f, 0.65f, 0.38f));
                        k.Ellipsoid(new Vector3(x, y + 0.18f, z), new Vector3(0.16f, 0.06f, 0.16f), C(0.88f, 0.74f, 0.46f));
                        break;
                    default: // vỉ nướng than
                        k.Box(new Vector3(x, y, z), new Vector3(0.38f, 0.07f, 0.26f), C(0.15f, 0.15f, 0.16f));
                        for (int i = -2; i <= 2; i++)
                            k.Box(new Vector3(x + i * 0.07f, y + 0.07f, z), new Vector3(0.025f, 0.012f, 0.24f), C(0.95f, 0.45f, 0.15f), outline: false);
                        break;
                }
                // Thớt + bó rau thơm bên phải
                k.Box(new Vector3(w * 0.28f, y, D * 0.55f), new Vector3(0.32f, 0.03f, 0.22f), C(0.85f, 0.68f, 0.45f));
                for (int i = 0; i < 3; i++)
                    k.Sphere(new Vector3(w * 0.28f - 0.06f + i * 0.06f, y + 0.06f, D * 0.55f + (i % 2) * 0.03f), 0.05f, C(0.30f, 0.65f, 0.25f), outline: false);
            }
            return k.Build($"Counter {tier}");
        }

        // ================= Ghế =================

        /// <summary>Chiều cao mặt ghế (đơn vị thế giới) theo mốc, để khách ngồi khớp.</summary>
        public static float ChairSeatHeight(int tier) => tier == 0 ? 0.13f : tier == 1 ? 0.17f : 0.2f;

        private static readonly Color[] StoolColors =
        {
            new Color(0.88f, 0.28f, 0.25f), new Color(0.25f, 0.52f, 0.88f), new Color(0.30f, 0.70f, 0.40f),
        };

        /// <summary>
        /// Ghế đặt bên trái bàn, khách ngồi quay mặt sang phải (vào bàn); tựa lưng nằm phía trái.
        /// Vỉa hè: ghế nhựa thấp đủ màu · quán cóc: đẩu gỗ · ngõ nhỏ: ghế gỗ tựa lưng ·
        /// mặt phố: ghế gỗ uốn chân sắt · nhà hàng lớn: ghế sơn mài đỏ tựa cao viền vàng.
        /// </summary>
        public static GameObject Chair(Transform parent, Vector2 seatPos, int tier, int variant)
        {
            int t = Mathf.Clamp(tier, 0, 4);
            int v = t == 0 ? ((variant % StoolColors.Length) + StoolColors.Length) % StoolColors.Length : 0;
            var mesh = Cached($"chair{t}_{v}", () => ChairMesh(t, v));
            return Spawn("Chair", parent, seatPos, mesh);
        }

        private static Mesh ChairMesh(int tier, int v)
        {
            var k = new MeshKit();
            float h = ChairSeatHeight(tier);
            switch (tier)
            {
                case 0: // ghế nhựa thấp vỉa hè
                {
                    var c = StoolColors[v];
                    k.Cylinder(Vector3.zero, 0.13f, h - 0.02f, c * 0.85f, topScale: 0.85f);
                    k.Cylinder(new Vector3(0, h - 0.02f, 0), 0.15f, 0.025f, c);
                    break;
                }
                case 1: // đẩu gỗ vuông
                {
                    var wood = C(0.58f, 0.38f, 0.22f);
                    for (int sx = -1; sx <= 1; sx += 2)
                    for (int sz = -1; sz <= 1; sz += 2)
                        k.Box(new Vector3(0.1f * sx, 0, 0.1f * sz), new Vector3(0.035f, h - 0.03f, 0.035f), wood * 0.75f);
                    k.Box(new Vector3(0, h - 0.035f, 0), new Vector3(0.28f, 0.035f, 0.28f), wood);
                    break;
                }
                case 3: // ghế gỗ uốn, chân sắt mảnh, mặt tròn
                {
                    var metal = C(0.22f, 0.22f, 0.25f);
                    var wood = C(0.66f, 0.44f, 0.26f);
                    for (int sx = -1; sx <= 1; sx += 2)
                    for (int sz = -1; sz <= 1; sz += 2)
                        k.Box(new Vector3(0.09f * sx, 0, 0.09f * sz), new Vector3(0.025f, h - 0.03f, 0.025f), metal);
                    k.Cylinder(new Vector3(0, h - 0.035f, 0), 0.15f, 0.035f, wood);
                    for (int sz = -1; sz <= 1; sz += 2)
                        k.Box(new Vector3(-0.16f, h, 0.09f * sz), new Vector3(0.025f, 0.24f, 0.025f), metal);
                    k.Box(new Vector3(-0.16f, h + 0.17f, 0), new Vector3(0.035f, 0.08f, 0.24f), wood);
                    break;
                }
                default: // ghế gỗ tựa lưng (ngõ nhỏ) / ghế sơn mài đỏ tựa cao (nhà hàng lớn)
                {
                    bool lux = tier >= 4;
                    var body = lux ? C(0.62f, 0.12f, 0.10f) : C(0.60f, 0.40f, 0.23f);
                    var trim = lux ? C(0.92f, 0.72f, 0.30f) : C(0.48f, 0.30f, 0.17f);
                    for (int sx = -1; sx <= 1; sx += 2)
                    for (int sz = -1; sz <= 1; sz += 2)
                        k.Box(new Vector3(0.11f * sx, 0, 0.11f * sz), new Vector3(0.035f, h - 0.035f, 0.035f), body * 0.8f);
                    k.Box(new Vector3(-0.02f, h - 0.04f, 0), new Vector3(0.34f, 0.04f, 0.3f), body);
                    float backH = lux ? 0.36f : 0.26f;
                    k.Box(new Vector3(-0.17f, h - 0.04f, 0), new Vector3(0.04f, backH + 0.04f, 0.3f), body);
                    k.Box(new Vector3(-0.193f, h + backH * 0.35f, 0), new Vector3(0.01f, backH * 0.35f, 0.16f), trim, outline: false);
                    if (lux) k.Box(new Vector3(-0.17f, h + backH, 0), new Vector3(0.05f, 0.03f, 0.34f), trim);
                    break;
                }
            }
            return k.Build("Chair " + tier);
        }

        // ================= Cây cảnh, két hàng =================

        public static GameObject Plant(Transform parent, Vector2 pos, int tier, int variant, float scale = 1f)
        {
            int v = ((variant % 3) + 3) % 3;
            var mesh = Cached($"plant{(tier >= 4 ? 4 : 0)}_{v}", () => PlantMesh(tier >= 4, v));
            return Spawn("Plant", parent, pos, mesh, scale);
        }

        private static Mesh PlantMesh(bool lux, int v)
        {
            var k = new MeshKit();
            var leaf = C(0.28f, 0.62f, 0.28f);
            var leafDark = C(0.20f, 0.48f, 0.22f);
            if (lux)
            {
                // Bình gốm men lam
                k.Ellipsoid(new Vector3(0, 0.28f, 0), new Vector3(0.24f, 0.28f, 0.24f), C(0.92f, 0.94f, 0.97f));
                k.Ellipsoid(new Vector3(0, 0.30f, -0.2f), new Vector3(0.12f, 0.08f, 0.05f), C(0.20f, 0.35f, 0.70f), outline: false);
                k.Cylinder(new Vector3(0, 0.5f, 0), 0.1f, 0.14f, C(0.92f, 0.94f, 0.97f), topScale: 1.3f);
            }
            else
            {
                k.Cylinder(Vector3.zero, 0.17f, 0.3f, C(0.78f, 0.42f, 0.26f), topScale: 1.2f); // chậu đất nung
                k.Cylinder(new Vector3(0, 0.27f, 0), 0.21f, 0.05f, C(0.70f, 0.37f, 0.22f));
            }
            float baseY = lux ? 0.62f : 0.32f;
            switch (v)
            {
                case 0: // bụi lá tròn
                    k.Sphere(new Vector3(0, baseY + 0.2f, 0), 0.24f, leaf);
                    k.Sphere(new Vector3(-0.16f, baseY + 0.12f, -0.04f), 0.16f, leafDark);
                    k.Sphere(new Vector3(0.16f, baseY + 0.14f, -0.02f), 0.17f, leaf);
                    k.Sphere(new Vector3(0, baseY + 0.4f, 0.02f), 0.14f, leafDark);
                    break;
                case 1: // cây lá dài (kiểu cau cảnh)
                    for (int i = -2; i <= 2; i++)
                        k.Ellipsoid(new Vector3(i * 0.1f, baseY + 0.3f + (2 - Mathf.Abs(i)) * 0.06f, (i % 2) * 0.05f),
                            new Vector3(0.06f, 0.3f, 0.06f), i % 2 == 0 ? leaf : leafDark);
                    break;
                default: // cây quất / hoa: tán tròn có chấm quả
                    k.Sphere(new Vector3(0, baseY + 0.22f, 0), 0.26f, leafDark);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * Mathf.PI / 3;
                        k.Sphere(new Vector3(Mathf.Cos(a) * 0.16f, baseY + 0.24f + Mathf.Sin(a * 2) * 0.08f, -0.2f + Mathf.Sin(a) * 0.05f),
                            0.045f, lux ? C(0.95f, 0.30f, 0.45f) : C(0.98f, 0.62f, 0.10f), outline: false);
                    }
                    break;
            }
            return k.Build("Plant");
        }

        // ================= Phố: cây, xe máy =================

        /// <summary>Cây xanh vỉa hè có ô đất quanh gốc.</summary>
        public static GameObject Tree(Transform parent, Vector2 pos, int variant)
        {
            int v = ((variant % 2) + 2) % 2;
            var mesh = Cached("tree" + v, () =>
            {
                var k = new MeshKit();
                k.Box(Vector3.zero, new Vector3(0.42f, 0.03f, 0.42f), C(0.35f, 0.28f, 0.22f));
                k.Cylinder(Vector3.zero, 0.07f, 0.75f, C(0.45f, 0.32f, 0.22f), topScale: 0.8f);
                var leaf = v == 0 ? C(0.32f, 0.62f, 0.32f) : C(0.40f, 0.66f, 0.30f);
                var dark = leaf * 0.82f;
                k.Sphere(new Vector3(0, 0.95f, 0), 0.36f, leaf);
                k.Sphere(new Vector3(-0.24f, 0.8f, 0.02f), 0.24f, dark);
                k.Sphere(new Vector3(0.25f, 0.84f, -0.02f), 0.25f, leaf);
                k.Sphere(new Vector3(0.05f, 1.2f, 0.05f), 0.22f, dark);
                return k.Build("Tree");
            });
            return Spawn("Tree", parent, pos, mesh);
        }

        private static readonly Color[] BikeColors =
        {
            new Color(0.85f, 0.22f, 0.20f), new Color(0.22f, 0.42f, 0.80f), new Color(0.20f, 0.20f, 0.22f), new Color(0.92f, 0.92f, 0.90f),
        };

        /// <summary>Chiều cao yên xe (đơn vị thế giới) để người lái ngồi khớp.</summary>
        public const float BikeSeatHeight = 0.4f;

        /// <summary>Xe máy (kiểu xe số), quay đầu sang phải hoặc trái.</summary>
        public static GameObject Motorbike(Transform parent, Vector2 pos, int variant, bool facingRight)
        {
            int c = ((variant % BikeColors.Length) + BikeColors.Length) % BikeColors.Length;
            float s = facingRight ? 1f : -1f;
            var mesh = Cached($"bike{c}_{(facingRight ? 1 : 0)}", () =>
            {
                var k = new MeshKit();
                var tire = C(0.15f, 0.15f, 0.16f);
                var body = BikeColors[c];
                foreach (float wx in new[] { -0.27f, 0.27f })
                {
                    k.Ellipsoid(new Vector3(wx * s, 0.13f, 0), new Vector3(0.13f, 0.13f, 0.04f), tire);
                    k.Ellipsoid(new Vector3(wx * s, 0.13f, 0), new Vector3(0.05f, 0.05f, 0.045f), C(0.7f, 0.7f, 0.72f), outline: false);
                }
                k.Ellipsoid(new Vector3(0, 0.28f, 0), new Vector3(0.26f, 0.1f, 0.09f), body);
                k.Ellipsoid(new Vector3(0.16f * s, 0.36f, 0), new Vector3(0.1f, 0.16f, 0.1f), body);   // ốp đầu xe
                k.Ellipsoid(new Vector3(-0.08f * s, BikeSeatHeight - 0.02f, 0), new Vector3(0.17f, 0.045f, 0.09f), C(0.18f, 0.15f, 0.14f)); // yên
                k.Box(new Vector3(0.24f * s, 0.13f, 0), new Vector3(0.025f, 0.42f, 0.025f), C(0.6f, 0.6f, 0.62f), outline: false); // phuộc
                k.Box(new Vector3(0.22f * s, 0.55f, 0), new Vector3(0.03f, 0.03f, 0.34f), C(0.25f, 0.25f, 0.27f)); // tay lái
                k.Sphere(new Vector3(0.29f * s, 0.46f, 0), 0.04f, C(1f, 0.92f, 0.6f), outline: false); // đèn pha
                k.Box(new Vector3(-0.3f * s, 0.3f, 0), new Vector3(0.08f, 0.04f, 0.1f), C(0.85f, 0.2f, 0.15f), outline: false); // đèn hậu
                return k.Build("Motorbike");
            });
            return Spawn("Motorbike", parent, pos, mesh);
        }

        /// <summary>Chồng két nhựa (vỉa hè, quán cóc).</summary>
        public static GameObject Crates(Transform parent, Vector2 pos)
        {
            var mesh = Cached("crates", () =>
            {
                var k = new MeshKit();
                var red = C(0.85f, 0.22f, 0.18f);
                var blue = C(0.22f, 0.45f, 0.82f);
                k.Box(new Vector3(0, 0, 0), new Vector3(0.5f, 0.24f, 0.36f), red);
                k.Box(new Vector3(0, 0.24f, 0), new Vector3(0.5f, 0.24f, 0.36f), blue);
                k.Box(new Vector3(0.05f, 0.48f, 0), new Vector3(0.5f, 0.24f, 0.36f), red);
                for (int i = 0; i < 3; i++)
                    k.Box(new Vector3(i == 2 ? 0.05f : 0, 0.08f + i * 0.24f, -0.185f), new Vector3(0.16f, 0.05f, 0.01f), C(0.15f, 0.12f, 0.1f), outline: false); // lỗ tay cầm
                return k.Build("Crates");
            });
            return Spawn("Crates", parent, pos, mesh);
        }
    }
}
