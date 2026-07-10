using UnityEngine;
using UnityEngine.Serialization;

namespace MoodyLib.SceneHighlighter {
    /// <summary>
    /// Describes a rectangular area that can be highlighted, defined by offset/size in local space
    /// (same shape as BoxCollider2D's API) in whatever space its Mode says it lives in. Scene items are
    /// projected through a Camera; UI items are normalized directly against screen dimensions, since UI
    /// Canvas space isn't camera-projected the same way. Bounds are recomputed every frame so a moving/
    /// animating target stays tracked correctly.
    /// </summary>
    public class HighlightableArea : MonoBehaviour {
        public enum Mode {
            Scene,
            UI
        }

        [FormerlySerializedAs("mode")] [SerializeField] private Mode _mode = Mode.Scene;
        [FormerlySerializedAs("offset")] [SerializeField] private Vector2 _offset;
        [FormerlySerializedAs("size")] [SerializeField] private Vector2 _size = new(1f, 1f);

        public Quad currentQuad { get; private set; }

        private void Update() {
            currentQuad = _mode == Mode.UI ? Quad.FromScreenSpace(transform, _offset, _size) : Quad.FromWorldSpace(transform, _offset, _size, Camera.main);
        }

        private void OnDrawGizmosSelected() {
            Gizmos.color = Color.red;

            var halfSize = _size * 0.5f;
            var center = transform.TransformPoint(_offset);
            var xAxis = transform.TransformVector(new Vector3(halfSize.x, 0f, 0f));
            var yAxis = transform.TransformVector(new Vector3(0f, halfSize.y, 0f));

            DrawEllipseGizmo(center, xAxis, yAxis);
        }

        private static void DrawEllipseGizmo(Vector3 center, Vector3 xAxis, Vector3 yAxis, int segments = 48) {
            var prev = center + xAxis;

            for (var i = 1; i <= segments; i++) {
                var angle = i / (float) segments * Mathf.PI * 2f;
                var point = center + xAxis * Mathf.Cos(angle) + yAxis * Mathf.Sin(angle);
                Gizmos.DrawLine(prev, point);
                prev = point;
            }
        }
    }
}
