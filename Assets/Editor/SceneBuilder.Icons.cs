using System;
using UnityEditor;
using UnityEngine;

namespace WordDrop.EditorTools
{
    /// <summary>
    /// Simple white icons drawn in code (tinted by the Image color): boosters,
    /// menu buttons (play, infinity, calendar, question, trophy, podium, gear),
    /// the level map (lock, pin) and panel buttons (home, restart, book, music,
    /// sound, vibration, gift).
    /// </summary>
    public static partial class SceneBuilder
    {
        private const string BombIconPath = UiArtFolder + "/icon_bomb.png";
        private const string ShuffleIconPath = UiArtFolder + "/icon_shuffle.png";
        private const string CheckIconPath = UiArtFolder + "/icon_check.png";
        private const string PlayIconPath = UiArtFolder + "/icon_play.png";
        private const string InfinityIconPath = UiArtFolder + "/icon_infinity.png";
        private const string CalendarIconPath = UiArtFolder + "/icon_calendar.png";
        private const string QuestionIconPath = UiArtFolder + "/icon_question.png";
        private const string TrophyIconPath = UiArtFolder + "/icon_trophy.png";
        private const string PodiumIconPath = UiArtFolder + "/icon_podium.png";
        private const string GearIconPath = UiArtFolder + "/icon_gear.png";
        private const string LockIconPath = UiArtFolder + "/icon_lock.png";
        private const string PinIconPath = UiArtFolder + "/icon_pin.png";
        private const string HomeIconPath = UiArtFolder + "/icon_home.png";
        private const string RestartIconPath = UiArtFolder + "/icon_restart.png";
        private const string BookIconPath = UiArtFolder + "/icon_book.png";
        private const string MusicIconPath = UiArtFolder + "/icon_music.png";
        private const string SoundIconPath = UiArtFolder + "/icon_sound.png";
        private const string VibrationIconPath = UiArtFolder + "/icon_vibration.png";
        private const string GiftIconPath = UiArtFolder + "/icon_gift.png";

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

            // Play: a triangle pointing right.
            WriteIcon(PlayIconPath, (x, y) => Triangle(x, y, 34, 16, 34, 112, 112, 64));

            // Infinity: a lemniscate drawn as a thick curve.
            var loop = new Vector2[65];
            for (int i = 0; i < loop.Length; i++)
            {
                float t = i * Mathf.PI * 2f / (loop.Length - 1);
                float d = 1f + Mathf.Sin(t) * Mathf.Sin(t);
                loop[i] = new Vector2(64 + 52 * Mathf.Cos(t) / d, 64 + 72 * Mathf.Sin(t) * Mathf.Cos(t) / d);
            }

            WriteIcon(InfinityIconPath, (x, y) => Polyline(x, y, loop, 10));

            // Calendar: a frame with a solid header, two binder rings and a grid of days.
            WriteIcon(CalendarIconPath, (x, y) =>
            {
                float frame = Mathf.Min(Box(x, y, 12, 10, 116, 106, 16), 1f - Box(x, y, 24, 22, 104, 78, 6));
                float rings = Mathf.Max(Segment(x, y, 40, 100, 40, 120, 8), Segment(x, y, 88, 100, 88, 120, 8));
                float days = 0f;
                for (int row = 0; row < 2; row++)
                {
                    for (int col = 0; col < 3; col++)
                    {
                        float left = 32 + col * 24;
                        float bottom = 30 + row * 24;
                        days = Mathf.Max(days, Box(x, y, left, bottom, left + 16, bottom + 16, 3));
                    }
                }

                return Mathf.Max(frame, rings, days);
            });

            // Question mark: an open arc, a short stem and a dot.
            WriteIcon(QuestionIconPath, (x, y) =>
            {
                float arc = x < 64 && y < 82 ? 0f : Ring(x, y, 64, 82, 26, 10);
                return Mathf.Max(arc, Segment(x, y, 64, 56, 64, 42, 10), Circle(x, y, 64, 18, 11));
            });

            // Trophy: a cup with handles on a stem and a base.
            WriteIcon(TrophyIconPath, (x, y) =>
            {
                float cup = Mathf.Max(Box(x, y, 30, 74, 98, 114, 4), y < 78 ? Circle(x, y, 64, 78, 34) : 0f);
                float handles = Mathf.Max(x < 34 ? Ring(x, y, 32, 88, 16, 6) : 0f, x > 94 ? Ring(x, y, 96, 88, 16, 6) : 0f);
                return Mathf.Max(cup, handles, Box(x, y, 56, 26, 72, 50, 2), Box(x, y, 36, 10, 92, 28, 6));
            });

            // Records: a podium, the middle step highest, with a medal over it.
            WriteIcon(PodiumIconPath, (x, y) =>
                Mathf.Max(Box(x, y, 10, 10, 44, 58, 6), Box(x, y, 47, 10, 81, 82, 6), Box(x, y, 84, 10, 118, 44, 6),
                    Circle(x, y, 64, 106, 13)));

            // Gear: a ring with eight teeth and a hole.
            WriteIcon(GearIconPath, (x, y) =>
            {
                float gear = Circle(x, y, 64, 64, 40);
                for (int k = 0; k < 8; k++)
                {
                    float angle = k * Mathf.PI / 4f;
                    float u = (x - 64) * Mathf.Cos(angle) + (y - 64) * Mathf.Sin(angle);
                    float v = -(x - 64) * Mathf.Sin(angle) + (y - 64) * Mathf.Cos(angle);
                    gear = Mathf.Max(gear, Box(u, v, 30, -12, 60, 12, 4));
                }

                return Mathf.Min(gear, 1f - Circle(x, y, 64, 64, 17));
            });

            // Lock: a shackle over a body with a keyhole.
            WriteIcon(LockIconPath, (x, y) =>
            {
                float shackle = Mathf.Max(y >= 76 ? Ring(x, y, 64, 76, 26, 9) : 0f,
                    Mathf.Max(Segment(x, y, 38, 60, 38, 76, 9), Segment(x, y, 90, 60, 90, 76, 9)));
                float keyhole = Mathf.Max(Circle(x, y, 64, 46, 10), Segment(x, y, 64, 46, 64, 26, 5));
                return Mathf.Min(Mathf.Max(shackle, Box(x, y, 22, 8, 106, 72, 14)), 1f - keyhole);
            });

            // Map pin: a round head tapering to a point, with a hole.
            WriteIcon(PinIconPath, (x, y) =>
                Mathf.Min(Mathf.Max(Circle(x, y, 64, 82, 40), Triangle(x, y, 30, 64, 98, 64, 64, 4)), 1f - Circle(x, y, 64, 82, 16)));

            // Home: a roof over a house with a door.
            WriteIcon(HomeIconPath, (x, y) =>
                Mathf.Min(Mathf.Max(Triangle(x, y, 8, 62, 120, 62, 64, 116), Box(x, y, 24, 10, 104, 70, 8)),
                    1f - Box(x, y, 52, 4, 76, 46, 6)));

            // Restart: an almost-closed circle ending in an arrowhead at the top.
            WriteIcon(RestartIconPath, (x, y) =>
            {
                float angle = Mathf.Atan2(y - 62, x - 64) * Mathf.Rad2Deg;
                float arc = angle > 62f && angle < 112f ? 0f : Ring(x, y, 64, 62, 38, 10);
                return Mathf.Max(arc, Triangle(x, y, 96, 116, 70, 74, 60, 106));
            });

            // Book: two open pages with lines.
            WriteIcon(BookIconPath, (x, y) =>
            {
                float pages = Mathf.Max(Box(x, y, 8, 18, 60, 108, 8), Box(x, y, 68, 18, 120, 108, 8));
                float lines = 0f;
                foreach (float lineY in new[] { 44f, 64f, 84f })
                {
                    lines = Mathf.Max(lines, Segment(x, y, 20, lineY, 48, lineY, 3), Segment(x, y, 80, lineY, 108, lineY, 3));
                }

                return Mathf.Min(pages, 1f - lines);
            });

            // Music: two beamed notes.
            WriteIcon(MusicIconPath, (x, y) =>
                Mathf.Max(Circle(x, y, 42, 30, 18), Circle(x, y, 94, 42, 18),
                    Mathf.Max(Segment(x, y, 56, 30, 56, 106, 6), Segment(x, y, 108, 42, 108, 118, 6)),
                    Segment(x, y, 56, 104, 108, 116, 10)));

            // Sound: a speaker with two waves.
            WriteIcon(SoundIconPath, (x, y) =>
            {
                float speaker = Mathf.Max(Box(x, y, 10, 44, 42, 84, 5),
                    Mathf.Max(Triangle(x, y, 34, 44, 70, 112, 70, 16), Triangle(x, y, 34, 44, 34, 84, 70, 112)));
                float angle = Mathf.Abs(Mathf.Atan2(y - 64, x - 72) * Mathf.Rad2Deg);
                float waves = angle < 48f ? Mathf.Max(Ring(x, y, 72, 64, 24, 6), Ring(x, y, 72, 64, 46, 6)) : 0f;
                return Mathf.Max(speaker, waves);
            });

            // Vibration: a phone outline with buzz lines either side.
            WriteIcon(VibrationIconPath, (x, y) =>
            {
                float phone = Mathf.Min(Box(x, y, 40, 10, 88, 118, 12), 1f - Box(x, y, 49, 24, 79, 102, 4));
                float buzz = Mathf.Max(
                    Mathf.Max(Segment(x, y, 26, 40, 16, 52, 4), Segment(x, y, 16, 52, 26, 64, 4), Segment(x, y, 26, 64, 16, 76, 4), Segment(x, y, 16, 76, 26, 88, 4)),
                    Mathf.Max(Segment(x, y, 102, 40, 112, 52, 4), Segment(x, y, 112, 52, 102, 64, 4), Segment(x, y, 102, 64, 112, 76, 4), Segment(x, y, 112, 76, 102, 88, 4)));
                return Mathf.Max(phone, buzz);
            });

            // Gift: a box and lid split by a ribbon, with a bow on top.
            WriteIcon(GiftIconPath, (x, y) =>
            {
                float box = Mathf.Max(Box(x, y, 18, 8, 110, 66, 6), Box(x, y, 10, 70, 118, 92, 6));
                box = Mathf.Min(box, 1f - Box(x, y, 59, 0, 69, 92, 0));
                float bow = Mathf.Min(Mathf.Max(Circle(x, y, 46, 106, 15), Circle(x, y, 82, 106, 15)),
                    1f - Mathf.Max(Circle(x, y, 46, 106, 6), Circle(x, y, 82, 106, 6)));
                return Mathf.Max(box, bow);
            });
        }

        private static void WriteIcon(string path, Func<float, float, float> coverage)
        {
            var texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            for (int y = 0; y < IconSize; y++)
            {
                for (int x = 0; x < IconSize; x++)
                {
                    // 2×2 samples per pixel, so straight-edged shapes (triangles) are smooth too.
                    float sum = 0f;
                    for (int s = 0; s < 4; s++)
                    {
                        sum += Mathf.Clamp01(coverage(x + 0.25f + 0.5f * (s % 2), y + 0.25f + 0.5f * (s / 2)));
                    }

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, sum / 4f));
                }
            }

            WriteSprite(texture, path, Vector4.zero);
        }

        // Each shape returns coverage 0..1 with a 1-pixel soft edge.
        private static float Circle(float x, float y, float cx, float cy, float r) =>
            Mathf.Clamp01(r - Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) + 0.5f);

        private static float Ring(float x, float y, float cx, float cy, float r, float halfWidth) =>
            Mathf.Clamp01(halfWidth - Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r) + 0.5f);

        private static float Box(float x, float y, float x0, float y0, float x1, float y1, float radius) =>
            Coverage(RoundedRectDistance(x, y, x0, y0, x1, y1, radius));

        private static float Polyline(float x, float y, Vector2[] points, float halfWidth)
        {
            float best = 0f;
            for (int i = 1; i < points.Length; i++)
            {
                best = Mathf.Max(best, Segment(x, y, points[i - 1].x, points[i - 1].y, points[i].x, points[i].y, halfWidth));
            }

            return best;
        }

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
