using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;

namespace SaiyanTransformations
{
    /// <summary>Nudges the Super Saiyan 3 mane toward the back of the head on the side-facing
    /// frames, at draw time. The hair sprite itself is untouched (so nothing is clipped) - we
    /// only shift where the game paints it. Facing right, "back" is screen-left; facing left
    /// (which the game draws by mirroring the right frame) "back" is screen-right, so the two
    /// sides are shifted in opposite screen directions to stay symmetric on the head.</summary>
    // NB: the game's method name is misspelled ("Accesories"); match it exactly.
    [HarmonyPatch(typeof(FarmerRenderer), nameof(FarmerRenderer.drawHairAndAccesories))]
    internal static class HairOffsetPatch
    {
        // Transformation.All[2].HairId (Super Saiyan 3)
        private const int Ssj3HairId = 77213003;

        private static void Prefix(int facingDirection, Farmer who, ref Vector2 position, float scale)
        {
            ModEntry mod = ModEntry.Instance;
            if (mod?.Config == null || !mod.Config.FixSaiyanSideHair)
                return;
            if (who == null || who.hair.Value != Ssj3HairId)
                return;
            if (facingDirection != Game1.right && facingDirection != Game1.left)
                return;

            // one source pixel is drawn at 4 * scale screen pixels (the game's own hair-offset
            // convention), so this shifts by exactly the requested number of source pixels
            float off = mod.Config.SaiyanSideHairOffsetPx * 4f * scale;
            if (facingDirection == Game1.right)
                position.X -= off;   // seat the mane back
            else
                position.X += off;   // mirrored frame: back is the other way
        }
    }
}
