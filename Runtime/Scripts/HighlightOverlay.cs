using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MoodyLib.SceneHighlighter {
    [RequireComponent(typeof(Image))]
    public class HighlightOverlay : MonoBehaviour, ICanvasRaycastFilter {
        private const int MaxHighlights = 8;

        private static readonly int HighlightCountProperty = Shader.PropertyToID("_HighlightCount");
        private static readonly int HighlightCornersProperty = Shader.PropertyToID("_HighlightCorners");

        private static HighlightOverlay _instance;

        private Material _material;
        private readonly Vector4[] _cornerBuffer = new Vector4[MaxHighlights * 2];
        private readonly List<Quad> _activeQuads = new();
        private bool _blocksInteraction;

        private void Awake() {
            var image = GetComponent<Image>();
            image.enabled = true;
            _material = new Material(image.material);
            image.material = _material;

            _instance = this;
            ClearInstance();
        }

        /// <summary>
        /// Shows the overlay with quad-shaped holes cut out at the given quads, in normalized viewport
        /// space (0,0 to 1,1). Purely geometric - has no idea what a quad represents or where it came from.
        /// If blocksInteraction is true, clicks are blocked everywhere, holes included - useful for
        /// highlighting something to look at without letting it be interacted with yet.
        /// </summary>
        public static void SetHighlights(IReadOnlyList<Quad> quads, bool blocksInteraction = false) {
            if (_instance) {
                _instance.SetHighlightsInstance(quads, blocksInteraction);
            } else {
                Debug.LogWarning("No component with HighlightOverlay has been detected in the scene. Highlights will not be shown.");
            }
        }

        public static void SetHighlight(Quad quad, bool blocksInteraction = false) {
            SetHighlights(new[] { quad }, blocksInteraction);
        }

        /// <summary>
        /// Convenience for highlighting a HighlightableArea directly, using its current bounds.
        /// </summary>
        public static void SetHighlight(HighlightableArea area, bool blocksInteraction = false) {
            SetHighlight(area.currentQuad, blocksInteraction);
        }

        public static void SetHighlights(IReadOnlyList<HighlightableArea> areas, bool blocksInteraction = false) {
            var quads = new Quad[areas.Count];
            for (var i = 0; i < areas.Count; i++) {
                quads[i] = areas[i].currentQuad;
            }

            SetHighlights(quads, blocksInteraction);
        }

        public static void Clear() {
            if (_instance) {
                _instance.ClearInstance();
            } else {
                Debug.LogWarning("No component with HighlightOverlay has been detected in the scene.");
            }
        }

        private void SetHighlightsInstance(IReadOnlyList<Quad> quads, bool blocksInteraction) {
            var count = Mathf.Min(quads.Count, MaxHighlights);

            _activeQuads.Clear();

            for (var i = 0; i < count; i++) {
                var q = quads[i];
                _activeQuads.Add(q);
                _cornerBuffer[i * 2] = new Vector4(q.A.x, q.A.y, q.B.x, q.B.y);
                _cornerBuffer[i * 2 + 1] = new Vector4(q.C.x, q.C.y, q.D.x, q.D.y);
            }

            _material.SetInt(HighlightCountProperty, count);
            _material.SetVectorArray(HighlightCornersProperty, _cornerBuffer);
            gameObject.SetActive(true);

            _blocksInteraction = blocksInteraction;
        }

        private void ClearInstance() {
            _activeQuads.Clear();
            _blocksInteraction = false;
            _material.SetInt(HighlightCountProperty, 0);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Lets clicks pass through wherever a hole is currently punched, instead of the overlay's full-screen
        /// rect blocking everything regardless of what the shader visually draws - unless blocksInteraction
        /// was requested, in which case every point stays blocked regardless of hole geometry.
        /// </summary>
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera) {
            if (_blocksInteraction) return true;

            var viewportPoint = new Vector2(screenPoint.x / Screen.width, screenPoint.y / Screen.height);

            foreach (var quad in _activeQuads) {
                if (PointInEllipse(viewportPoint, quad)) return false;
            }

            return true;
        }

        private static bool PointInEllipse(Vector2 p, Quad q) {
            return EllipseCoords(p, q.A, q.B, q.D).sqrMagnitude <= 1f;
        }

        private static Vector2 EllipseCoords(Vector2 p, Vector2 a, Vector2 b, Vector2 d) {
            var u = b - a;
            var v = d - a;
            var rel = p - a;

            var det = u.x * v.y - u.y * v.x;
            var uCoord = (rel.x * v.y - rel.y * v.x) / det;
            var vCoord = (u.x * rel.y - u.y * rel.x) / det;

            return new Vector2(uCoord, vCoord) * 2f - Vector2.one;
        }
    }
}
