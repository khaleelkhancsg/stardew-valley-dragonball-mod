using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace SaiyanTransformations
{
    /// <summary>Draws the aura, lightning and transform shockwave over the world.</summary>
    internal sealed class FxRenderer
    {
        public const int AuraW = 32;
        public const int AuraH = 56;
        public const int AuraFrames = 8;
        public const int LightningFrames = 4;

        private readonly ModEntry Owner;

        public FxRenderer(ModEntry owner)
        {
            this.Owner = owner;
        }

        /// <summary>Bottom-centre of the farmer's drawn sprite, in screen pixels.
        /// AuraOffsetX/Y in config nudge this if it does not line up.</summary>
        public Vector2 PlayerAnchor()
        {
            Vector2 p = Game1.GlobalToLocal(Game1.viewport, Game1.player.Position);
            return new Vector2(
                p.X + 32f + Owner.Config.AuraOffsetX,
                p.Y + 64f + Owner.Config.AuraOffsetY);
        }

        /// <summary>Switch the active sprite batch to additive so the effects glow.</summary>
        public void BeginGlow(SpriteBatch b)
        {
            if (!Owner.Config.AdditiveBlending)
                return;
            b.End();
            b.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp,
                    null, RasterizerState.CullCounterClockwise);
        }

        /// <summary>Restore the batch state the game expects.</summary>
        public void EndGlow(SpriteBatch b)
        {
            if (!Owner.Config.AdditiveBlending)
                return;
            b.End();
            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                    null, RasterizerState.CullCounterClockwise);
        }

        /// <summary>Bottom-centre of a monster's sprite, in screen pixels.</summary>
        public Vector2 MonsterAnchor(Character character)
        {
            Vector2 p = Game1.GlobalToLocal(Game1.viewport, character.Position);
            return new Vector2(p.X + 32f, p.Y + 64f);
        }

        /// <summary>Shared aura draw, used for both the farmer and bosses.
        /// <paramref name="phase"/> offsets the animation so several auras on screen
        /// do not pulse in lockstep.</summary>
        public void DrawAuraAt(SpriteBatch b, Vector2 anchor, Color colour, float scale,
                               float opacity, int phase = 0)
        {
            int frame = ((Owner.AnimTicks / 5) + phase) % AuraFrames;
            if (frame < 0)
                frame += AuraFrames;

            float pulse = 0.88f + 0.12f * (float)Math.Sin((Owner.AnimTicks + phase) * 0.09);
            float alpha = MathHelper.Clamp(opacity * pulse, 0f, 1f);

            Vector2 pos = new Vector2(anchor.X - (AuraW * scale / 2f), anchor.Y - (AuraH * scale));

            b.Draw(Owner.AuraTexture, pos, new Rectangle(frame * AuraW, 0, AuraW, AuraH),
                   colour * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            // white-hot inner core, drawn untinted so high forms read as blinding
            b.Draw(Owner.AuraTexture, pos, new Rectangle(frame * AuraW, AuraH, AuraW, AuraH),
                   Color.White * (alpha * 0.7f), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        public void DrawAura(SpriteBatch b, Transformation form)
        {
            this.DrawAuraAt(b, this.PlayerAnchor(), form.AuraColor,
                            4f * form.AuraScale, Owner.Config.AuraOpacity);
        }

        /// <summary>Silver flare and label shown when Ultra Instinct evades a hit.</summary>
        public void DrawDodge(SpriteBatch b, int elapsed, int total)
        {
            float t = MathHelper.Clamp((float)elapsed / total, 0f, 1f);
            float alpha = 1f - t;
            Vector2 anchor = this.PlayerAnchor();

            float scale = 3.5f + (t * 5f);
            Vector2 centre = new Vector2(anchor.X, anchor.Y - 64f);
            b.Draw(Owner.KameTexture,
                   new Vector2(centre.X - (32 * scale / 2f), centre.Y - (32 * scale / 2f)),
                   new Rectangle(Math.Min(3, (int)(t * 4f)) * 32, 64, 32, 32),
                   new Color(225, 240, 255) * alpha, 0f, Vector2.Zero, scale,
                   SpriteEffects.None, 0f);

            string label = "Dodged!";
            Vector2 size = Game1.smallFont.MeasureString(label);
            Utility.drawTextWithShadow(b, label, Game1.smallFont,
                new Vector2(centre.X - (size.X / 2f), anchor.Y - 150f - (t * 26f)),
                new Color(235, 245, 255) * alpha);
        }

        public void DrawLightning(SpriteBatch b, Transformation form)
        {
            // crackle in bursts rather than continuously
            int beat = Owner.AnimTicks / 6;
            if (beat % 3 == 2)
                return;

            Vector2 anchor = this.PlayerAnchor();
            float scale = 4f * form.AuraScale;
            int frame = beat % LightningFrames;
            Vector2 pos = new Vector2(anchor.X - (AuraW * scale / 2f), anchor.Y - (AuraH * scale));

            b.Draw(Owner.LightningTexture, pos, new Rectangle(frame * AuraW, 0, AuraW, AuraH),
                   Color.Lerp(form.AuraColor, Color.White, 0.6f) * 0.9f,
                   0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        /// <summary>Expanding shockwave ring played when a form is entered.</summary>
        public void DrawTransformBurst(SpriteBatch b, Transformation form, int elapsed, int total)
        {
            float t = MathHelper.Clamp((float)elapsed / total, 0f, 1f);
            int frame = Math.Min(3, (int)(t * 4f));
            float scale = 4f + t * 12f;
            float alpha = 1f - t;

            Vector2 anchor = this.PlayerAnchor();
            Vector2 centre = new Vector2(anchor.X, anchor.Y - 64f);
            Vector2 pos = new Vector2(centre.X - (32 * scale / 2f), centre.Y - (32 * scale / 2f));

            b.Draw(Owner.KameTexture, pos, new Rectangle(frame * 32, 64, 32, 32),
                   form.AuraColor * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
