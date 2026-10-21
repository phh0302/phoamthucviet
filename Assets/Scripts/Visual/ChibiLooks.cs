using System.Collections.Generic;
using UnityEngine;

namespace PhoAmThucViet.Visual
{
    public enum HairStyle { Short, Bun, Long, Spiky }
    public enum HatStyle { None, Toque, TallToque, NonLa, Cap }

    /// <summary>Trang phục và màu sắc một nhân vật chibi.</summary>
    public class ChibiLook
    {
        public string key;
        public Color skin, shirt, pants, shoes, hair, hatColor = Color.white;
        public HairStyle hairStyle;
        public HatStyle hat;
        public bool apron, vest, bowTie, scarf, mustache, buttons, glasses;
        public Color apronColor = Color.white, vestColor, scarfColor;
    }

    /// <summary>
    /// Kiểu dáng các nhân vật: đầu bếp chính (mũ cao, khăn đỏ, ria mép), đầu bếp phụ, phục vụ (gi-lê, nơ),
    /// khách (đội nón lá, mũ lưỡi trai, tóc búi…). Mesh được tạo một lần rồi dùng chung.
    /// Đơn vị: nhân vật cao khoảng 2,3 (cả mũ), chân ở gốc, mặt quay theo +Z.
    /// </summary>
    public static class ChibiLooks
    {
        public const int CustomerVariants = 8;

        private static Material _material;
        private static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

        public static Material Material
        {
            get
            {
                if (_material != null) return _material;
                var shader = Resources.Load<Shader>("Shaders/ChibiToon");
                if (shader == null || !shader.isSupported) shader = Shader.Find("Sprites/Default"); // dự phòng: vẫn hiện màu đỉnh
                _material = new Material(shader) { name = "ChibiToon", renderQueue = 3000 };
                return _material;
            }
        }

        // ---------- Bảng màu ----------
        private static readonly Color Ink = new Color(0.12f, 0.08f, 0.07f);
        private static readonly Color Cheek = new Color(1f, 0.58f, 0.55f);
        private static readonly Color Mouth = new Color(0.62f, 0.22f, 0.20f);
        private static readonly Color White = new Color(0.97f, 0.97f, 0.95f);
        private static readonly Color[] Skins =
        {
            new Color(1.00f, 0.84f, 0.70f), new Color(0.96f, 0.78f, 0.62f), new Color(0.88f, 0.68f, 0.52f), new Color(1.00f, 0.88f, 0.76f),
        };
        private static readonly Color[] Hairs =
        {
            new Color(0.10f, 0.08f, 0.08f), new Color(0.22f, 0.14f, 0.10f), new Color(0.35f, 0.22f, 0.14f), new Color(0.55f, 0.55f, 0.58f),
        };
        private static readonly Color[] Shirts =
        {
            new Color(0.95f, 0.55f, 0.35f), new Color(0.40f, 0.75f, 0.45f), new Color(0.85f, 0.45f, 0.70f), new Color(0.95f, 0.80f, 0.30f),
            new Color(0.55f, 0.55f, 0.90f), new Color(0.30f, 0.70f, 0.75f), new Color(0.90f, 0.30f, 0.30f), new Color(0.98f, 0.95f, 0.88f),
        };
        private static readonly Color[] Pants =
        {
            new Color(0.25f, 0.32f, 0.55f), new Color(0.70f, 0.60f, 0.42f), new Color(0.22f, 0.22f, 0.25f), new Color(0.35f, 0.45f, 0.30f),
        };
        private static readonly Color[] StoolColors = { new Color(0.90f, 0.25f, 0.25f), new Color(0.25f, 0.50f, 0.90f) };
        private static readonly Color Straw = new Color(0.95f, 0.86f, 0.58f);

        public static ChibiLook For(ChibiKind kind, int variant)
        {
            switch (kind)
            {
                case ChibiKind.HeadChef:
                    return new ChibiLook
                    {
                        key = "head", skin = Skins[1], hair = Hairs[1], hairStyle = HairStyle.Short,
                        shirt = White, pants = new Color(0.25f, 0.25f, 0.28f), shoes = Ink,
                        hat = HatStyle.TallToque, scarf = true, scarfColor = new Color(0.85f, 0.15f, 0.15f),
                        mustache = true, buttons = true,
                    };
                case ChibiKind.Chef:
                {
                    int v = Mathf.Abs(variant);
                    return new ChibiLook
                    {
                        key = "chef" + v % 4, skin = Skins[v % Skins.Length], hair = Hairs[(v + 1) % 3],
                        hairStyle = v % 2 == 0 ? HairStyle.Short : HairStyle.Spiky,
                        shirt = White, pants = new Color(0.28f, 0.28f, 0.32f), shoes = Ink,
                        hat = HatStyle.Toque, apron = true, apronColor = new Color(0.95f, 0.62f, 0.25f),
                        scarf = true, scarfColor = new Color(0.98f, 0.75f, 0.25f), buttons = true,
                    };
                }
                case ChibiKind.Waiter:
                {
                    int v = Mathf.Abs(variant);
                    return new ChibiLook
                    {
                        key = "waiter" + v % 4, skin = Skins[(v + 2) % Skins.Length], hair = Hairs[v % 3],
                        hairStyle = v % 2 == 0 ? HairStyle.Short : HairStyle.Bun,
                        shirt = White, pants = new Color(0.16f, 0.17f, 0.22f), shoes = Ink,
                        vest = true, vestColor = new Color(0.18f, 0.30f, 0.62f), bowTie = true,
                        apron = true, apronColor = new Color(0.16f, 0.17f, 0.22f),
                    };
                }
                default:
                {
                    int v = Mathf.Abs(variant) % CustomerVariants;
                    var rng = new System.Random(v * 7919 + 13);
                    var look = new ChibiLook
                    {
                        key = "customer" + v,
                        skin = Skins[rng.Next(Skins.Length)],
                        hair = Hairs[rng.Next(3)],
                        shirt = Shirts[v % Shirts.Length],
                        pants = Pants[rng.Next(Pants.Length)],
                        shoes = new Color(0.35f, 0.25f, 0.20f),
                    };
                    // Mỗi khách một kiểu: nón lá, mũ lưỡi trai, tóc búi, tóc dài, kính…
                    switch (v)
                    {
                        case 0: look.hat = HatStyle.NonLa; look.hairStyle = HairStyle.Long; break;
                        case 1: look.hat = HatStyle.Cap; look.hatColor = new Color(0.90f, 0.30f, 0.25f); break;
                        case 2: look.hairStyle = HairStyle.Bun; break;
                        case 3: look.hairStyle = HairStyle.Long; look.glasses = true; break;
                        case 4: look.hairStyle = HairStyle.Spiky; break;
                        case 5: look.hat = HatStyle.NonLa; look.hairStyle = HairStyle.Short; look.hair = Hairs[3]; look.mustache = true; break;
                        case 6: look.hat = HatStyle.Cap; look.hatColor = new Color(0.25f, 0.45f, 0.85f); look.glasses = true; break;
                        default: look.hairStyle = HairStyle.Short; break;
                    }
                    return look;
                }
            }
        }

        // ================= Thân, đầu, mặt, mũ =================

        private const float HeadY = 1.42f, HeadR = 0.43f;

        public static Mesh TorsoMesh(ChibiLook l)
        {
            if (Cache.TryGetValue("torso:" + l.key, out var m)) return m;
            var k = new MeshKit();

            // Thân và hông
            k.Ellipsoid(new Vector3(0, 0.74f, 0), new Vector3(0.37f, 0.36f, 0.31f), l.vest ? l.vestColor : l.shirt);
            k.Ellipsoid(new Vector3(0, 0.50f, 0), new Vector3(0.33f, 0.15f, 0.28f), l.pants);
            if (l.vest)
            {
                k.Ellipsoid(new Vector3(0, 0.86f, 0.25f), new Vector3(0.13f, 0.17f, 0.08f), l.shirt, outline: false); // cổ áo sơ mi
                k.Sphere(new Vector3(0, 0.66f, 0.30f), 0.03f, new Color(0.9f, 0.85f, 0.4f), outline: false);
                k.Sphere(new Vector3(0, 0.56f, 0.29f), 0.03f, new Color(0.9f, 0.85f, 0.4f), outline: false);
            }
            if (l.apron)
                k.Ellipsoid(new Vector3(0, l.vest ? 0.50f : 0.62f, 0.19f), new Vector3(0.30f, l.vest ? 0.20f : 0.30f, 0.14f), l.apronColor);
            if (l.buttons && !l.apron)
                for (int i = 0; i < 2; i++)
                for (int s = -1; s <= 1; s += 2)
                    k.Sphere(new Vector3(0.1f * s, 0.82f - i * 0.15f, 0.29f), 0.032f, new Color(0.35f, 0.35f, 0.4f), outline: false);
            if (l.buttons && l.apron)
                k.Sphere(new Vector3(0, 0.92f, 0.29f), 0.032f, new Color(0.35f, 0.35f, 0.4f), outline: false);
            if (l.scarf)
            {
                k.Ellipsoid(new Vector3(0, 1.02f, 0.02f), new Vector3(0.29f, 0.08f, 0.27f), l.scarfColor);
                k.Ellipsoid(new Vector3(0, 0.92f, 0.27f), new Vector3(0.09f, 0.11f, 0.05f), l.scarfColor);
            }
            if (l.bowTie)
            {
                var red = new Color(0.85f, 0.15f, 0.18f);
                k.Ellipsoid(new Vector3(-0.07f, 1.0f, 0.29f), new Vector3(0.07f, 0.045f, 0.04f), red);
                k.Ellipsoid(new Vector3(0.07f, 1.0f, 0.29f), new Vector3(0.07f, 0.045f, 0.04f), red);
                k.Sphere(new Vector3(0, 1.0f, 0.31f), 0.03f, red, outline: false);
            }

            // Đầu to và khuôn mặt
            k.Sphere(new Vector3(0, HeadY, 0), HeadR, l.skin);
            k.Sphere(new Vector3(-0.42f, HeadY - 0.02f, 0), 0.08f, l.skin); // tai
            k.Sphere(new Vector3(0.42f, HeadY - 0.02f, 0), 0.08f, l.skin);
            for (int s = -1; s <= 1; s += 2)
            {
                k.Ellipsoid(new Vector3(0.15f * s, HeadY + 0.02f, 0.39f), new Vector3(0.055f, 0.08f, 0.04f), Ink, outline: false);
                k.Sphere(new Vector3(0.15f * s - 0.015f, HeadY + 0.05f, 0.425f), 0.02f, Color.white, outline: false);
                k.Ellipsoid(new Vector3(0.25f * s, HeadY - 0.09f, 0.34f), new Vector3(0.07f, 0.04f, 0.04f), Cheek, outline: false);
            }
            k.Sphere(new Vector3(0, HeadY - 0.05f, 0.43f), 0.035f, l.skin * 0.95f, outline: false); // mũi
            k.Ellipsoid(new Vector3(0, HeadY - 0.13f, 0.40f), new Vector3(0.055f, 0.025f, 0.03f), Mouth, outline: false);
            if (l.mustache)
                for (int s = -1; s <= 1; s += 2)
                    k.Ellipsoid(new Vector3(0.07f * s, HeadY - 0.09f, 0.415f), new Vector3(0.08f, 0.032f, 0.035f), l.hair, outline: false);
            if (l.glasses)
            {
                var frame = new Color(0.15f, 0.12f, 0.12f);
                for (int s = -1; s <= 1; s += 2)
                {
                    k.Ellipsoid(new Vector3(0.15f * s, HeadY + 0.02f, 0.41f), new Vector3(0.095f, 0.085f, 0.02f), frame, outline: false);
                    k.Ellipsoid(new Vector3(0.15f * s, HeadY + 0.02f, 0.425f), new Vector3(0.072f, 0.064f, 0.014f), new Color(0.80f, 0.90f, 0.98f), outline: false);
                    k.Ellipsoid(new Vector3(0.15f * s, HeadY + 0.02f, 0.432f), new Vector3(0.03f, 0.045f, 0.01f), Ink, outline: false);
                }
                k.Ellipsoid(new Vector3(0, HeadY + 0.04f, 0.43f), new Vector3(0.06f, 0.014f, 0.014f), frame, outline: false);
            }

            Hair(k, l);
            Hat(k, l);

            m = k.Build("Torso " + l.key);
            Cache["torso:" + l.key] = m;
            return m;
        }

        private static void Hair(MeshKit k, ChibiLook l)
        {
            if (l.hat == HatStyle.Cap && l.hairStyle == HairStyle.Short) return; // mũ che kín tóc ngắn
            k.Ellipsoid(new Vector3(0, HeadY + 0.10f, -0.05f), new Vector3(0.46f, 0.36f, 0.44f), l.hair);
            switch (l.hairStyle)
            {
                case HairStyle.Bun:
                    k.Sphere(new Vector3(0, HeadY + 0.44f, -0.14f), 0.15f, l.hair);
                    break;
                case HairStyle.Long:
                    k.Ellipsoid(new Vector3(0, HeadY - 0.18f, -0.14f), new Vector3(0.44f, 0.38f, 0.32f), l.hair);
                    break;
                case HairStyle.Spiky:
                    for (int i = -1; i <= 1; i++)
                        k.Ellipsoid(new Vector3(0.15f * i, HeadY + 0.38f, 0.12f - Mathf.Abs(i) * 0.05f), new Vector3(0.12f, 0.12f, 0.14f), l.hair);
                    break;
            }
        }

        private static void Hat(MeshKit k, ChibiLook l)
        {
            switch (l.hat)
            {
                case HatStyle.Toque:
                    k.Cylinder(new Vector3(0, HeadY + 0.24f, -0.02f), 0.36f, 0.2f, White);
                    k.Ellipsoid(new Vector3(0, HeadY + 0.56f, -0.02f), new Vector3(0.42f, 0.24f, 0.42f), White);
                    break;
                case HatStyle.TallToque:
                    k.Cylinder(new Vector3(0, HeadY + 0.24f, -0.02f), 0.37f, 0.36f, White);
                    k.Ellipsoid(new Vector3(0, HeadY + 0.74f, -0.02f), new Vector3(0.47f, 0.30f, 0.47f), White);
                    k.Sphere(new Vector3(-0.2f, HeadY + 0.80f, 0.12f), 0.2f, White);
                    k.Sphere(new Vector3(0.2f, HeadY + 0.80f, 0.12f), 0.2f, White);
                    break;
                case HatStyle.NonLa:
                    k.Cone(new Vector3(0, HeadY + 0.26f, 0), 0.66f, 0.48f, Straw);
                    break;
                case HatStyle.Cap:
                    k.Ellipsoid(new Vector3(0, HeadY + 0.15f, -0.02f), new Vector3(0.455f, 0.32f, 0.455f), l.hatColor);
                    k.Ellipsoid(new Vector3(0, HeadY + 0.17f, 0.36f), new Vector3(0.30f, 0.04f, 0.24f), l.hatColor);
                    break;
            }
        }

        // ================= Tay, chân, ghế =================

        /// <summary>Tay: gốc ở vai, buông xuống theo -Y.</summary>
        public static Mesh ArmMesh(ChibiLook l)
        {
            if (Cache.TryGetValue("arm:" + l.key, out var m)) return m;
            var k = new MeshKit();
            var sleeve = l.vest ? l.shirt : l.shirt;
            k.Ellipsoid(new Vector3(0, -0.16f, 0), new Vector3(0.11f, 0.2f, 0.11f), sleeve);
            k.Sphere(new Vector3(0, -0.37f, 0), 0.1f, l.skin);
            m = k.Build("Arm " + l.key);
            Cache["arm:" + l.key] = m;
            return m;
        }

        /// <summary>Chân: gốc ở hông, giày hướng về +Z.</summary>
        public static Mesh LegMesh(ChibiLook l)
        {
            if (Cache.TryGetValue("leg:" + l.key, out var m)) return m;
            var k = new MeshKit();
            k.Cylinder(new Vector3(0, -0.34f, 0), 0.115f, 0.34f, l.pants, topScale: 1.1f);
            k.Ellipsoid(new Vector3(0, -0.35f, 0.05f), new Vector3(0.13f, 0.08f, 0.19f), l.shoes);
            m = k.Build("Leg " + l.key);
            Cache["leg:" + l.key] = m;
            return m;
        }

        /// <summary>Ghế nhựa thấp kiểu quán vỉa hè (đỏ hoặc xanh), hiện khi khách ngồi.</summary>
        public static Mesh StoolMesh(int variant)
        {
            int ci = Mathf.Abs(variant) % StoolColors.Length;
            if (Cache.TryGetValue("stool:" + ci, out var m)) return m;
            var k = new MeshKit();
            var c = StoolColors[ci];
            k.Cylinder(Vector3.zero, 0.26f, 0.1f, c * 0.85f, topScale: 0.92f);
            k.Cylinder(new Vector3(0, 0.1f, 0), 0.3f, 0.04f, c);
            m = k.Build("Stool " + ci);
            Cache["stool:" + ci] = m;
            return m;
        }
    }
}
