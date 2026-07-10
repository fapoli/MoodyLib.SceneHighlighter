using UnityEngine;

namespace MoodyLib.SceneHighlighter {
    /// <summary>
    /// Four points describing a quad, in whatever coordinate space the consumer expects (e.g. normalized
    /// viewport space, 0,0 to 1,1). Points must be given in perimeter order (not crossed/bowtie).
    /// </summary>
    public readonly struct Quad {
        public readonly Vector2 A;
        public readonly Vector2 B;
        public readonly Vector2 C;
        public readonly Vector2 D;

        public Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d) {
            A = a;
            B = b;
            C = c;
            D = d;
        }

        /// <summary>
        /// Builds a Quad for a Scene-space highlight given a 3D world position, size, rotation, and the
        /// camera to project through. Rotation happens in world space (physically uniform) before
        /// projecting each corner through the camera, so it stays correct regardless of the camera's
        /// projection type (perspective or orthographic) or the screen's aspect ratio.
        /// </summary>
        public static Quad FromWorldSpace(Vector3 worldCenter, Vector2 size, Quaternion rotation, Camera camera) {
            var halfSize = size * 0.5f;

            Vector2 Corner(float signX, float signY) {
                var localOffset = new Vector3(signX * halfSize.x, signY * halfSize.y, 0f);
                return camera.WorldToViewportPoint(worldCenter + rotation * localOffset);
            }

            return new Quad(Corner(-1, -1), Corner(1, -1), Corner(1, 1), Corner(-1, 1));
        }

        /// <summary>
        /// Convenience for building a Quad directly from a Transform's position/rotation/scale, with a
        /// local offset and size (same shape as BoxCollider2D's API), projected through the given camera.
        /// </summary>
        public static Quad FromWorldSpace(Transform transform, Vector2 offset, Vector2 size, Camera camera) {
            var halfSize = size * 0.5f;

            Vector2 Corner(float signX, float signY) {
                var localPoint = new Vector3(offset.x + signX * halfSize.x, offset.y + signY * halfSize.y, 0f);
                return camera.WorldToViewportPoint(transform.TransformPoint(localPoint));
            }

            return new Quad(Corner(-1, -1), Corner(1, -1), Corner(1, 1), Corner(-1, 1));
        }

        /// <summary>
        /// Builds a Quad for a UI/screen-space highlight from a rect and rotation given in actual screen
        /// pixel coordinates (not normalized viewport space). Rotating in pixel space (physically uniform)
        /// avoids the shear that rotating an already-normalized rect would introduce on non-square screens.
        /// </summary>
        public static Quad FromScreenSpace(Rect screenRect, float angleDegrees = 0f) {
            var center = screenRect.center;
            var angleRad = angleDegrees * Mathf.Deg2Rad;
            var cos = Mathf.Cos(angleRad);
            var sin = Mathf.Sin(angleRad);

            Vector2 Corner(float x, float y) {
                var offset = new Vector2(x, y) - center;
                var rotated = center + new Vector2(offset.x * cos - offset.y * sin, offset.x * sin + offset.y * cos);
                return new Vector2(rotated.x / Screen.width, rotated.y / Screen.height);
            }

            return new Quad(
                Corner(screenRect.xMin, screenRect.yMin), Corner(screenRect.xMax, screenRect.yMin),
                Corner(screenRect.xMax, screenRect.yMax), Corner(screenRect.xMin, screenRect.yMax)
            );
        }

        /// <summary>
        /// Convenience for building a UI/screen-space Quad directly from a Transform's position/rotation,
        /// with a local offset and size, normalized against the actual screen dimensions.
        /// </summary>
        public static Quad FromScreenSpace(Transform transform, Vector2 offset, Vector2 size) {
            var halfSize = size * 0.5f;

            Vector2 Corner(float signX, float signY) {
                var localPoint = new Vector3(offset.x + signX * halfSize.x, offset.y + signY * halfSize.y, 0f);
                var worldPoint = transform.TransformPoint(localPoint);
                return new Vector2(worldPoint.x / Screen.width, worldPoint.y / Screen.height);
            }

            return new Quad(Corner(-1, -1), Corner(1, -1), Corner(1, 1), Corner(-1, 1));
        }
    }
}
