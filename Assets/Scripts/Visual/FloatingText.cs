using UnityEngine;

namespace PhoAmThucViet.Visual
{
    /// <summary>Chữ bay lên rồi mờ dần (ví dụ "+1.2K" khi quầy nấu xong).</summary>
    public class FloatingText : MonoBehaviour
    {
        private TextMesh _text;
        private Color _color;
        private float _age;
        private const float Lifetime = 1.1f;
        private const float RiseSpeed = 1.2f;

        public static void Spawn(Transform parent, Vector2 pos, string text, Color color, float size = 0.36f)
        {
            var tm = SpriteFactory.Text("Floating", parent, pos, text, size, color, 30);
            var ft = tm.gameObject.AddComponent<FloatingText>();
            ft._text = tm;
            ft._color = color;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            transform.localPosition += Vector3.up * (RiseSpeed * Time.deltaTime);
            float t = _age / Lifetime;
            _text.color = new Color(_color.r, _color.g, _color.b, 1 - t * t);
            if (_age >= Lifetime) Destroy(gameObject);
        }
    }
}
