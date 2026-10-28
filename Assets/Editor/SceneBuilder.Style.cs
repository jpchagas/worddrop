using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace WordDrop.EditorTools
{
    /// <summary>
    /// The visual style kit: Fredoka fonts (OFL, Assets/Art/Fonts) and the
    /// generated sprites that give tiles, buttons and cards their depth.
    /// Sprites are white with shading baked into brightness, so an Image color
    /// tints them: the tint is the brightest part (the gloss), the face is a
    /// little darker and the bottom lip much darker.
    /// </summary>
    public static partial class SceneBuilder
    {
        private const string FontFolder = "Assets/Art/Fonts";
        private const string BodyFontSourcePath = FontFolder + "/Fredoka-Medium.ttf";
        private const string BoldFontSourcePath = FontFolder + "/Fredoka-Bold.ttf";
        private const string BodyFontPath = FontFolder + "/Fredoka-Medium SDF.asset";
        private const string BoldFontPath = FontFolder + "/Fredoka-Bold SDF.asset";
        private const string BoldShadowMaterialPath = FontFolder + "/Fredoka-Bold SDF - Shadow.mat";

        private const string TileSpritePath = UiArtFolder + "/tile.png";
        private const string ButtonSpritePath = UiArtFolder + "/button.png";
        private const string CardSpritePath = UiArtFolder + "/card.png";
        private const string SoftShadowSpritePath = UiArtFolder + "/soft_shadow.png";
        private const string GlowSpritePath = UiArtFolder + "/glow.png";
        private const string WellSpritePath = UiArtFolder + "/well.png";
        private const string NodeSpritePath = UiArtFolder + "/node.png";
        private const string FoliageSpritePath = UiArtFolder + "/foliage.png";
        private const string RaysSpritePath = UiArtFolder + "/rays.png";

        // Baked into the font atlas so common text never waits for a glyph at runtime.
        private const string FontCharacters =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "ÁÀÂÃÉÊÍÓÔÕÚÜÇáàâãéêíóôõúüç×·…–—“”‘’ªº°";

        // Button sprite geometry, in sprite pixels: the lip is the darker band under the face.
        private const int ButtonSpriteSize = 128;
        private const float ButtonLip = 14f;
        /// <summary>Button height that shows the sprite at its own pixel size (see <see cref="ButtonSlice"/>).</summary>
        private const float ButtonReferenceHeight = 120f;

        private static TMP_FontAsset boldFont;
        private static Material boldShadowMaterial;
        private static Sprite tileSprite;
        private static Sprite buttonSprite;
        private static Sprite cardSprite;
        private static Sprite softShadow;
        private static Sprite glow;
        private static Sprite well;
        private static Sprite node;
        private static Sprite foliage;
        private static Sprite rays;

        private static void LoadStyle()
        {
            CreateStyleSprites();
            font = FontAsset(BodyFontSourcePath, BodyFontPath);
            boldFont = FontAsset(BoldFontSourcePath, BoldFontPath);
            boldShadowMaterial = ShadowMaterial(boldFont);

            tileSprite = LoadSprite(TileSpritePath);
            buttonSprite = LoadSprite(ButtonSpritePath);
            cardSprite = LoadSprite(CardSpritePath);
            softShadow = LoadSprite(SoftShadowSpritePath);
            glow = LoadSprite(GlowSpritePath);
            well = LoadSprite(WellSpritePath);
            node = LoadSprite(NodeSpritePath);
            foliage = LoadSprite(FoliageSpritePath);
            rays = LoadSprite(RaysSpritePath);
        }

        /// <summary>
        /// Pixels-per-unit multiplier that scales the button sprite's corners and
        /// lip with the button's height, so every button has the same shape.
        /// </summary>
        private static float ButtonSlice(float height) => ButtonReferenceHeight / height;

        /// <summary>Height of a button's lip in canvas units, to lift its label onto the face.</summary>
        private static float ButtonLipHeight(float height) => ButtonLip * height / ButtonReferenceHeight;

        // ---------------------------------------------------------------- Fonts

        /// <summary>Loads the SDF font asset, creating it (dynamic, with common pt-BR glyphs baked in) if missing.</summary>
        private static TMP_FontAsset FontAsset(string sourcePath, string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            var source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: false);
            asset.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(asset, assetPath);

            asset.atlasTextures[0].name = asset.name + " Atlas";
            asset.material.name = asset.name + " Material";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            AssetDatabase.AddObjectToAsset(asset.material, asset);

            asset.TryAddCharacters(FontCharacters, out _);
            // Symbols Fredoka lacks come from TextMesh Pro's default font.
            asset.fallbackFontAssetTable = new List<TMP_FontAsset> { TMP_Settings.defaultFontAsset };

            // Keep the baked glyphs in builds instead of clearing the atlas.
            var serialized = new SerializedObject(asset);
            SerializedProperty clear = serialized.FindProperty("m_ClearDynamicDataOnBuild");
            if (clear != null)
            {
                clear.boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        /// <summary>A material preset with a soft drop shadow under the letters, for bold text.</summary>
        private static Material ShadowMaterial(TMP_FontAsset fontAsset)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(BoldShadowMaterialPath);
            if (material == null)
            {
                material = new Material(fontAsset.material);
                AssetDatabase.CreateAsset(material, BoldShadowMaterialPath);
            }

            material.CopyPropertiesFromMaterial(fontAsset.material);
            material.name = Path.GetFileNameWithoutExtension(BoldShadowMaterialPath);
            material.EnableKeyword("UNDERLAY_ON");
            material.SetColor("_UnderlayColor", new Color(0.02f, 0.04f, 0.12f, 0.45f));
            material.SetFloat("_UnderlayOffsetX", 0f);
            material.SetFloat("_UnderlayOffsetY", -0.6f);
            material.SetFloat("_UnderlayDilate", 0.15f);
            material.SetFloat("_UnderlaySoftness", 0.25f);
            EditorUtility.SetDirty(material);
            return material;
        }

        // ---------------------------------------------------------------- Sprites

        private static void CreateStyleSprites()
        {
            Directory.CreateDirectory(UiArtFolder);

            // Letter tile: glossy face, darker lip at the bottom, thin darker edge.
            const int tile = 160;
            const float tileRadius = 36f;
            const float tileLip = 13f;
            WriteShadedSprite(tile, tile, TileSpritePath, Vector4.zero, (x, y) =>
            {
                float outer = Coverage(RoundedRectDistance(x, y, 0, 0, tile, tile, tileRadius));
                float faceDistance = RoundedRectDistance(x, y, 0, tileLip, tile, tile, tileRadius);
                float face = Coverage(faceDistance);
                float shade = Mathf.Lerp(0.78f, 0.95f, Mathf.InverseLerp(tileLip, tile, y));
                float gloss = Coverage(RoundedRectDistance(x, y, 14, tile * 0.56f, tile - 14, tile - 9, 24f));
                shade = Mathf.Lerp(shade, 1f, gloss * 0.85f);
                if (faceDistance > -3f)
                {
                    shade *= 0.9f; // Thin edge so neighbouring tiles read apart.
                }

                return (Mathf.Lerp(0.56f, shade, face), outer);
            });

            // Button: like a tile, but nine-sliced, so the lip sits in the bottom border.
            const int b = ButtonSpriteSize;
            const float buttonRadius = 44f;
            WriteShadedSprite(b, b, ButtonSpritePath, new Vector4(48, 48 + ButtonLip, 48, 48), (x, y) =>
            {
                float outer = Coverage(RoundedRectDistance(x, y, 0, 0, b, b, buttonRadius));
                float faceDistance = RoundedRectDistance(x, y, 0, ButtonLip, b, b, buttonRadius);
                float face = Coverage(faceDistance);
                float shade = Mathf.Lerp(0.86f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(b * 0.55f, b - 8, y)));
                if (faceDistance > -2.5f)
                {
                    shade *= 0.92f;
                }

                return (Mathf.Lerp(0.58f, shade, face), outer);
            });

            // Card: flat face with a lighter rim, brightest along the top edge.
            const int c = 128;
            const float cardRadius = 44f;
            WriteShadedSprite(c, c, CardSpritePath, new Vector4(48, 48, 48, 48), (x, y) =>
            {
                float distance = RoundedRectDistance(x, y, 0, 0, c, c, cardRadius);
                float rim = distance > -3.5f ? (y > c / 2f ? 1f : 0.93f) : 0.84f;
                return (rim, Coverage(distance));
            });

            // Soft shadow: a blurred rounded rectangle, tinted black and placed behind cards.
            const int s = 128;
            WriteShadedSprite(s, s, SoftShadowSpritePath, new Vector4(60, 60, 60, 60), (x, y) =>
            {
                float distance = RoundedRectDistance(x, y, 28, 28, s - 28, s - 28, 20f);
                return (1f, 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-12f, 26f, distance)));
            });

            // Glow: a soft round light, for behind stars, titles and bursts.
            const int g = 128;
            WriteShadedSprite(g, g, GlowSpritePath, Vector4.zero, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(g / 2f, g / 2f)) / (g / 2f);
                float a = Mathf.Clamp01(1f - d);
                return (1f, a * a);
            });

            // Well: a recessed area (the board, the next-piece box), shaded darker
            // under its top edge as if the rim casts a shadow into it.
            const int w = 128;
            const float wellRadius = 40f;
            WriteShadedSprite(w, w, WellSpritePath, new Vector4(48, 48, 48, 48), (x, y) =>
            {
                float distance = RoundedRectDistance(x, y, 0, 0, w, w, wellRadius);
                float shadow = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(w, w - 22f, y));
                float shade = Mathf.Lerp(0.5f, 0.82f, shadow);
                if (distance > -3f)
                {
                    shade = 1.0f; // Thin light edge where the well meets the frame.
                }

                return (shade, Coverage(distance));
            });

            // Node: a round glossy button (level map), lip at the bottom like the tiles.
            const int n = 192;
            const float nodeLip = 16f;
            WriteShadedSprite(n, n, NodeSpritePath, Vector4.zero, (x, y) =>
            {
                float r = n / 2f;
                float outer = Coverage(Vector2.Distance(new Vector2(x, y), new Vector2(r, r)) - r);
                float faceDistance = Vector2.Distance(new Vector2(x, y), new Vector2(r, r + nodeLip / 2f)) - (r - nodeLip / 2f);
                float face = Coverage(faceDistance);
                float shade = Mathf.Lerp(0.8f, 0.96f, Mathf.InverseLerp(nodeLip, n, y));
                float gloss = Coverage(Vector2.Distance(new Vector2(x, (y - n * 0.7f) * 1.9f + n * 0.7f), new Vector2(r, n * 0.7f)) - r * 0.62f);
                shade = Mathf.Lerp(shade, 1f, gloss * 0.7f);
                if (faceDistance > -4f)
                {
                    shade *= 0.9f;
                }

                return (Mathf.Lerp(0.56f, shade, face), outer);
            });

            // Rays: a sunburst of 12 soft beams fading outward, for behind earned stars.
            const int rs = 256;
            WriteShadedSprite(rs, rs, RaysSpritePath, Vector4.zero, (x, y) =>
            {
                Vector2 d = new Vector2(x - rs / 2f, y - rs / 2f);
                float distance = d.magnitude / (rs / 2f);
                float beam = Mathf.Cos(Mathf.Atan2(d.y, d.x) * 12f) * 0.5f + 0.5f;
                beam = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 0.75f, beam));
                return (1f, beam * Mathf.Clamp01(1f - distance) * Mathf.Clamp01(distance * 4f));
            });

            CreateFoliageSprite();
            CreateBackgroundSprite();
        }

        /// <summary>
        /// Jungle foliage for the bottom of the menus (from screens_draft.png):
        /// leaves fanning up from both corners, lower in the middle, with a
        /// purple swoosh behind them. In full color, so the Image stays white.
        /// </summary>
        private static void CreateFoliageSprite()
        {
            const int w = 720;
            const int h = 280;
            var random = new System.Random(11);
            float Range(float a, float b) => a + (float)random.NextDouble() * (b - a);

            Color[] layers = { Hex(0x123F45), Hex(0x1C6A43), Hex(0x0E3326) };
            var leaves = new List<(Vector2 basePoint, Vector2 dir, float length, float width, float bend, Color color)>();
            for (int layer = 0; layer < layers.Length; layer++)
            {
                for (int i = 0; i < 16; i++)
                {
                    // Mostly near the corners; the middle stays low so buttons and text stay clear.
                    bool left = i % 2 == 0;
                    float edge = Mathf.Pow(Range(0f, 1f), 1.6f) * w * 0.55f;
                    float x = left ? edge : w - edge;
                    float nearCorner = 1f - edge / (w * 0.55f);
                    float angle = (left ? Range(35f, 100f) : Range(80f, 145f)) * Mathf.Deg2Rad;
                    float length = Range(70f, 120f) + nearCorner * Range(60f, 150f) - layer * 15f;
                    Color color = Color.Lerp(layers[layer], Color.black, Range(0f, 0.25f));
                    leaves.Add((new Vector2(x, Range(-20f, 10f)), new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)),
                        length, length * Range(0.16f, 0.24f), Range(-0.25f, 0.25f), color));
                }
            }

            Color purple = Hex(0x4A2577);
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    var color = new Color(0f, 0f, 0f, 0f);

                    // Purple swoosh: a wide arc rising from the bottom left.
                    float arc = Vector2.Distance(p, new Vector2(w * 0.05f, -h * 1.1f)) - h * 1.45f;
                    float swoosh = Mathf.Clamp01(Mathf.Min(0.5f - arc, 0.5f + arc + 46f)) * Mathf.Clamp01(1.2f - x / (w * 0.6f));
                    color = Color.Lerp(color, purple, swoosh);

                    foreach (var leaf in leaves)
                    {
                        Vector2 d = p - leaf.basePoint;
                        float u = Vector2.Dot(d, leaf.dir);
                        if (u <= 0f || u >= leaf.length)
                        {
                            continue;
                        }

                        float t = u / leaf.length;
                        float v = Vector2.Dot(d, new Vector2(-leaf.dir.y, leaf.dir.x)) - leaf.bend * t * t * leaf.length;
                        float half = leaf.width * Mathf.Pow(Mathf.Sin(Mathf.PI * t), 0.8f);
                        float inside = Mathf.Clamp01(half - Mathf.Abs(v) + 0.5f);
                        if (inside <= 0f)
                        {
                            continue;
                        }

                        // Lighter toward the tip, with a pale midrib.
                        Color shade = Color.Lerp(leaf.color, leaf.color * 1.35f, t);
                        if (Mathf.Abs(v) < 1.4f && t < 0.9f)
                        {
                            shade = Color.Lerp(shade, Color.white, 0.18f);
                        }

                        shade.a = 1f;
                        color = Color.Lerp(color, shade, inside);
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            WriteSprite(texture, FoliageSpritePath, Vector4.zero);
        }

        /// <summary>
        /// The screen background: deep navy, lighter towards the top, a soft
        /// light behind the top center and darker corners. Slight noise keeps
        /// the gradient from banding.
        /// </summary>
        private static void CreateBackgroundSprite()
        {
            const int w = 270;
            const int h = 480;
            Color top = Hex(0x24427C);
            Color middle = Hex(0x132759);
            Color bottom = Hex(0x080F26);
            Color light = Hex(0x4C7FD8);
            var noise = new System.Random(7);

            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                float t = y / (h - 1f);
                Color baseColor = t > 0.5f
                    ? Color.Lerp(middle, top, (t - 0.5f) * 2f)
                    : Color.Lerp(bottom, middle, t * 2f);
                for (int x = 0; x < w; x++)
                {
                    var p = new Vector2(x / (float)w, y / (float)h);
                    float glowAmount = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(p.x, p.y * 1.8f), new Vector2(0.5f, 1.55f)) / 0.75f);
                    Color color = Color.Lerp(baseColor, light, glowAmount * glowAmount * 0.4f);

                    float edge = Vector2.Distance(p, new Vector2(0.5f, 0.5f)) / 0.72f;
                    color *= 1f - 0.35f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, edge));

                    float dither = ((float)noise.NextDouble() - 0.5f) / 255f;
                    texture.SetPixel(x, y, new Color(color.r + dither, color.g + dither, color.b + dither, 1f));
                }
            }

            WriteSprite(texture, GradientSpritePath, Vector4.zero);
        }

        /// <summary>Renders a white sprite from a function returning (brightness, alpha) per pixel center.</summary>
        private static void WriteShadedSprite(int width, int height, string path, Vector4 border,
            System.Func<float, float, (float shade, float alpha)> pixel)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var (shade, alpha) = pixel(x + 0.5f, y + 0.5f);
                    texture.SetPixel(x, y, new Color(shade, shade, shade, alpha));
                }
            }

            WriteSprite(texture, path, border);
        }

        /// <summary>Signed distance from a point to a rounded rectangle: negative inside, positive outside.</summary>
        private static float RoundedRectDistance(float px, float py, float x0, float y0, float x1, float y1, float radius)
        {
            float cx = Mathf.Clamp(px, x0 + radius, x1 - radius);
            float cy = Mathf.Clamp(py, y0 + radius, y1 - radius);
            return Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy)) - radius;
        }

        /// <summary>Anti-aliased coverage of a pixel from its signed distance to a shape's edge.</summary>
        private static float Coverage(float distance) => Mathf.Clamp01(0.5f - distance);
    }
}
