using System.Collections.Generic;
using UnityEngine;

namespace PhoAmThucViet.Visual
{
    /// <summary>
    /// Ghép các khối 3D đơn giản (cầu dẹt, trụ, nón) thành một Mesh tô màu theo đỉnh,
    /// dùng cho nhân vật chibi dựng bằng code. Alpha của màu đỉnh = có vẽ viền (1) hay không (0).
    /// </summary>
    public class MeshKit
    {
        private readonly List<Vector3> _v = new List<Vector3>();
        private readonly List<Vector3> _n = new List<Vector3>();
        private readonly List<Color> _c = new List<Color>();
        private readonly List<Vector3> _o = new List<Vector3>(); // hướng nới viền (khối hộp: từ tâm ra góc, để viền liền mạch)
        private readonly List<int> _t = new List<int>();

        /// <summary>Biến đổi (dời / xoay) áp cho các khối thêm sau. Đặt lại Matrix4x4.identity khi xong.</summary>
        public Matrix4x4 M = Matrix4x4.identity;

        private static Color WithOutline(Color c, bool outline) => new Color(c.r, c.g, c.b, outline ? 1f : 0f);

        /// <summary>Thêm tam giác, tự đảo chiều để mặt trước quay ra ngoài (theo pháp tuyến các đỉnh).</summary>
        private void Tri(int a, int b, int c)
        {
            Vector3 face = Vector3.Cross(_v[b] - _v[a], _v[c] - _v[a]);
            Vector3 normal = _n[a] + _n[b] + _n[c];
            if (Vector3.Dot(face, normal) < 0) { _t.Add(a); _t.Add(c); _t.Add(b); }
            else { _t.Add(a); _t.Add(b); _t.Add(c); }
        }

        private int Vert(Vector3 p, Vector3 n, Color c) => Vert(p, n, c, n);

        private int Vert(Vector3 p, Vector3 n, Color c, Vector3 outlineDir)
        {
            p = M.MultiplyPoint3x4(p);
            n = M.MultiplyVector(n).normalized;
            outlineDir = M.MultiplyVector(outlineDir);
            _v.Add(p); _n.Add(n); _c.Add(c); _o.Add(outlineDir);
            return _v.Count - 1;
        }

        /// <summary>Hình hộp: tâm đáy tại baseCenter, kích thước size (rộng x, cao y, sâu z). Mặt phẳng, viền liền.</summary>
        public MeshKit Box(Vector3 baseCenter, Vector3 size, Color color, bool outline = true)
        {
            color = WithOutline(color, outline);
            Vector3 c = baseCenter + Vector3.up * size.y * 0.5f, h = size * 0.5f;
            Vector3[] normals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var n in normals)
            {
                // Hai trục nằm trên mặt
                Vector3 u = Mathf.Abs(n.y) > 0.5f ? Vector3.right : Vector3.up;
                Vector3 v = Vector3.Cross(n, u);
                int start = _v.Count;
                for (int k = 0; k < 4; k++)
                {
                    float su = (k == 0 || k == 3) ? -1 : 1, sv = k < 2 ? -1 : 1;
                    Vector3 local = n + u * su + v * sv; // góc hộp đơn vị
                    Vector3 p = c + Vector3.Scale(local, h);
                    Vert(p, n, color, local.normalized);
                }
                Tri(start, start + 1, start + 2);
                Tri(start, start + 2, start + 3);
            }
            return this;
        }

        /// <summary>Khối cầu dẹt tâm center, bán kính theo 3 trục.</summary>
        public MeshKit Ellipsoid(Vector3 center, Vector3 radii, Color color, bool outline = true, int seg = 16, int rings = 10)
        {
            color = WithOutline(color, outline);
            int start = _v.Count;
            for (int i = 0; i <= rings; i++)
            {
                float th = Mathf.PI * i / rings;
                float y = Mathf.Cos(th), s = Mathf.Sin(th);
                for (int j = 0; j <= seg; j++)
                {
                    float ph = 2 * Mathf.PI * j / seg;
                    var unit = new Vector3(s * Mathf.Cos(ph), y, s * Mathf.Sin(ph));
                    var n = new Vector3(unit.x / radii.x, unit.y / radii.y, unit.z / radii.z).normalized;
                    Vert(center + Vector3.Scale(unit, radii), n, color);
                }
            }
            for (int i = 0; i < rings; i++)
                for (int j = 0; j < seg; j++)
                {
                    int a = start + i * (seg + 1) + j, b = a + seg + 1;
                    if (i > 0) Tri(a, a + 1, b);
                    if (i < rings - 1) Tri(b, a + 1, b + 1);
                }
            return this;
        }

        public MeshKit Sphere(Vector3 center, float r, Color color, bool outline = true) =>
            Ellipsoid(center, new Vector3(r, r, r), color, outline, r < 0.08f ? 10 : 16, r < 0.08f ? 6 : 10);

        /// <summary>Hình trụ đứng: đáy tại baseCenter, cao h, có nắp hai đầu.</summary>
        public MeshKit Cylinder(Vector3 baseCenter, float r, float h, Color color, bool outline = true, int seg = 16, float topScale = 1f)
        {
            color = WithOutline(color, outline);
            int start = _v.Count;
            float rt = r * topScale;
            for (int j = 0; j <= seg; j++)
            {
                float ph = 2 * Mathf.PI * j / seg;
                var dir = new Vector3(Mathf.Cos(ph), 0, Mathf.Sin(ph));
                var n = new Vector3(dir.x * h, r - rt, dir.z * h).normalized;
                Vert(baseCenter + dir * r, n, color);
                Vert(baseCenter + dir * rt + Vector3.up * h, n, color);
            }
            for (int j = 0; j < seg; j++)
            {
                int a = start + j * 2;
                Tri(a, a + 1, a + 2);
                Tri(a + 2, a + 1, a + 3);
            }
            Cap(baseCenter, r, Vector3.down, color, seg);
            if (rt > 0.001f) Cap(baseCenter + Vector3.up * h, rt, Vector3.up, color, seg);
            return this;
        }

        /// <summary>Nón (như nón lá): đáy tròn tại baseCenter, đỉnh cao h.</summary>
        public MeshKit Cone(Vector3 baseCenter, float r, float h, Color color, bool outline = true, int seg = 20)
        {
            color = WithOutline(color, outline);
            int start = _v.Count;
            for (int j = 0; j <= seg; j++)
            {
                float ph = 2 * Mathf.PI * j / seg;
                var dir = new Vector3(Mathf.Cos(ph), 0, Mathf.Sin(ph));
                var n = new Vector3(dir.x * h, r, dir.z * h).normalized;
                Vert(baseCenter + dir * r, n, color);
                Vert(baseCenter + Vector3.up * h, n, color);
            }
            for (int j = 0; j < seg; j++)
            {
                int a = start + j * 2;
                Tri(a, a + 1, a + 2);
            }
            Cap(baseCenter, r, Vector3.down, color, seg);
            return this;
        }

        private void Cap(Vector3 center, float r, Vector3 normal, Color color, int seg)
        {
            int c = Vert(center, normal, color);
            int start = _v.Count;
            for (int j = 0; j <= seg; j++)
            {
                float ph = 2 * Mathf.PI * j / seg;
                Vert(center + new Vector3(Mathf.Cos(ph), 0, Mathf.Sin(ph)) * r, normal, color);
            }
            for (int j = 0; j < seg; j++) Tri(c, start + j, start + j + 1);
        }

        /// <summary>
        /// Hình phẳng có bề dày (ngôi sao, hình thoi…): đa giác pts quanh tâm trên mặt XY, mặt trước quay +Z.
        /// Đa giác lõm hình sao vẫn được vì tam giác toả ra từ tâm.
        /// </summary>
        public MeshKit Prism(Vector3 center, IList<Vector2> pts, float depth, Color color, bool outline = false)
        {
            color = WithOutline(color, outline);
            for (int side = -1; side <= 1; side += 2)
            {
                var n = new Vector3(0, 0, side);
                var off = new Vector3(0, 0, side * depth * 0.5f);
                int c = Vert(center + off, n, color);
                int start = _v.Count;
                foreach (var p in pts) Vert(center + off + new Vector3(p.x, p.y, 0), n, color);
                for (int i = 0; i < pts.Count; i++) Tri(c, start + i, start + (i + 1) % pts.Count);
            }
            for (int i = 0; i < pts.Count; i++)
            {
                Vector2 a = pts[i], b = pts[(i + 1) % pts.Count];
                Vector2 e = b - a;
                var n = new Vector3(e.y, -e.x, 0).normalized;
                if (Vector2.Dot(new Vector2(n.x, n.y), (a + b) * 0.5f) < 0) n = -n;
                int s0 = Vert(center + new Vector3(a.x, a.y, -depth * 0.5f), n, color);
                Vert(center + new Vector3(b.x, b.y, -depth * 0.5f), n, color);
                Vert(center + new Vector3(b.x, b.y, depth * 0.5f), n, color);
                Vert(center + new Vector3(a.x, a.y, depth * 0.5f), n, color);
                Tri(s0, s0 + 1, s0 + 2);
                Tri(s0, s0 + 2, s0 + 3);
            }
            return this;
        }

        /// <summary>Các đỉnh ngôi sao n cánh bán kính r, cánh đầu tiên chĩa lên.</summary>
        public static Vector2[] StarPoints(float r, int n = 5, float inner = 0.45f)
        {
            var pts = new Vector2[n * 2];
            for (int i = 0; i < n * 2; i++)
            {
                float rr = i % 2 == 0 ? r : r * inner;
                float a = Mathf.PI / 2 + i * Mathf.PI / n;
                pts[i] = new Vector2(Mathf.Cos(a) * rr, Mathf.Sin(a) * rr);
            }
            return pts;
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(_v);
            mesh.SetNormals(_n);
            mesh.SetColors(_c);
            mesh.SetUVs(1, _o);
            mesh.SetTriangles(_t, 0);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }
    }
}
