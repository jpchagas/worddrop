using System;
using UnityEditor;
using UnityEngine;

namespace WordDrop.EditorTools
{
    /// <summary>Simple white icons drawn in code (tinted by the Image color): bomb, shuffle and check.</summary>
    public static partial class SceneBuilder
    {
        private const string BombIconPath = UiArtFolder + "/icon_bomb.png";
        private const string ShuffleIconPath = UiArtFolder + "/icon_shuffle.png";
        private const string CheckIconPath = UiArtFolder + "/icon_check.png";

        private const int IconSize = 128;

        private static void CreateIconSprites()
        {
            // Bomb: a ball with a short fuse and a spark.
            WriteIcon(BombIconPath, (x, y) =>
                Mathf.Max(Circle(x, y, 58, 54, 40),
                    Segment(x, y, 80, 86, 96, 104, 7),
                    Circle(x, y, 102, 110, 11)));

            // Shuffle: two crossing arrows.
            WriteIcon(ShuffleIconPath, (x, y) =>
                Mathf.Max(
                    Mathf.Max(Segment(x, y, 18, 36, 92, 92, 9), Triangle(x, y, 112, 104, 84, 112, 100, 80)),
                    Mathf.Max(Segment(x, y, 18, 92, 92, 36, 9), Triangle(x, y, 112, 24, 100, 48, 84, 16))));

            // Check mark.
            WriteIcon(CheckIconPath, (x, y) =>
                Mathf.Max(Segment(x, y, 22, 64, 52, 34, 11), Segment(x, y, 52, 34, 106, 96, 11)));
        }

        private static void WriteIcon(string path, Func<float, float, float> coverage)
        {
            var texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            for (int y = 0; y < IconSize; y++)
            {
                for (int x = 0; x < IconSize; x++)
                {
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(coverage(x + 0.5f, y + 0.5f))));
                }
            }

            WriteSprite(texture, path, Vector4.zero);
        }

        // Each shape returns coverage 0..1 with a 1-pixel soft edge.
        private static float Circle(float x, float y, float cx, float cy, float r) =>
            Mathf.Clamp01(r - Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) + 0.5f);

        private static float Segment(float x, float y, float ax, float ay, float bx, float by, float halfWidth)
        {
            var p = new Vector2(x, y);
            var a = new Vector2(ax, ay);
            var ab = new Vector2(bx, by) - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Mathf.Clamp01(halfWidth - Vector2.Distance(p, a + ab * t) + 0.5f);
        }

        private static float Triangle(float x, float y, float ax, float ay, float bx, float by, float cx, float cy)
        {
            float d1 = Edge(x, y, ax, ay, bx, by);
            float d2 = Edge(x, y, bx, by, cx, cy);
            float d3 = Edge(x, y, cx, cy, ax, ay);
            bool negative = d1 < 0 || d2 < 0 || d3 < 0;
            bool positive = d1 > 0 || d2 > 0 || d3 > 0;
            return negative && positive ? 0f : 1f;
        }

        private static float Edge(float x, float y, float ax, float ay, float bx, float by) =>
            (x - bx) * (ay - by) - (ax - bx) * (y - by);

        private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
