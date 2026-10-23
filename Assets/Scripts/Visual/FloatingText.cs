using UnityEngine;

namespace PhoAmThucViet.Visual
{
    /// <summary>Chữ bay lên rồi mờ dần (ví dụ "+1.2K" khi quầy nấu xong).</summary>
    public class FloatingText : MonoBehaviour
    {
        private TextMesh _text;
        private Color _color;
        private float _age;
        private float _lifetime = 1.1f;
        private const float RiseSpeed = 1.2f;

        public static void Spawn(Transform parent, Vector2 pos, string text, Color color, float size = 0.36f, float lifetime = 1.1f, float rise = 1f)
        {
            var tm = SpriteFactory.Text("Floating", parent, pos, text, size, color, 30);
            var ft = tm.gameObject.AddComponent<FloatingText>();
            ft._text = tm;
            ft._color = color;
            ft._lifetime = lifetime;
            ft._rise = rise;
        }

        private float _rise = 1f;

        private void Update()
        {
            _age += Time.deltaTime;
            transform.localPosition += Vector3.up * (RiseSpeed * _rise * Time.deltaTime);
            float t = _age / _lifetime;
            _text.color = new Color(_color.r, _color.g, _color.b, 1 - t * t);
            if (_age >= _lifetime) Destroy(gameObject);
        }
    }
}
