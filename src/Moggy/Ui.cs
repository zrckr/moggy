using System.Numerics;
using Foster.Framework;

namespace Moggy;

public static class Ui
{
    extension(Batcher batcher)
    {
        public void InVirtualScreen(Action action)
        {
            // Draw in virtual screen coordinates instead of the scrolling world.
            batcher.PushMatrix(Matrix3x2.Identity, relative: false);
            batcher.PushScissor(null);
            action();
            batcher.PopScissor();
            batcher.PopMatrix();
        }

        public void TextMonospaced(in SpriteFont font, ReadOnlySpan<char> text, Vector2 position, Color color)
        {
            if (font.Material is { } material)
            {
                batcher.PushMaterial(material);
            }

            if (font.Sampler is { } sampler)
            {
                batcher.PushSampler(sampler);
            }

            // HUD glyphs advance on a fixed grid instead of using font metrics.
            var glyphPosition = position;
            foreach (var character in text)
            {
                if (character == '\n')
                {
                    glyphPosition.X = position.X;
                    glyphPosition.Y += Mathz.TileSize;
                    continue;
                }

                if (font.TryGetCharacter(character, out var glyph) && glyph.Subtexture.Texture != null)
                {
                    batcher.Image(glyph.Subtexture, glyphPosition, color);
                }

                glyphPosition.X += Mathz.TileSize;
            }

            if (font.Sampler is not null)
            {
                batcher.PopSampler();
            }

            if (font.Material is not null)
            {
                batcher.PopMaterial();
            }
        }
    }
}