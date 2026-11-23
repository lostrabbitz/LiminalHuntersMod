using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Shaders;
using LiminalHuntersMod.Common.Players;

namespace LiminalHuntersMod.Common.Utilities
{
    /// <summary>Builds / refreshes a hidden, single-colored copy of a player (used for the dash afterimages).</summary>
    public static class DummyAppearance
    {
        public static Player Create()
        {
            Player d = new Player();
            d.whoAmI = 255;                 // not a real player slot
            d.active = true;
            d.dead = false;
            d.heldProj = -1;
            d.fullRotationOrigin = new Vector2(d.width / 2f, d.height / 2f);
            d.GetModPlayer<MiragePlayer>().IsCloneDummy = true;
            return d;
        }

        public static void Apply(Player d, Player o, Color tint, int dyeItem)
        {
            d.Male = o.Male;
            d.skinVariant = o.skinVariant;
            d.hair = o.hair;
            d.hairDye = 0;

            d.skinColor = d.hairColor = d.eyeColor = tint;
            d.shirtColor = d.underShirtColor = d.pantsColor = d.shoeColor = tint;

            d.head = o.head; d.body = o.body; d.legs = o.legs;
            d.handon = o.handon; d.handoff = o.handoff;
            d.back = o.back; d.front = o.front; d.shoe = o.shoe;
            d.waist = o.waist; d.shield = o.shield; d.neck = o.neck;
            d.face = o.face; d.balloon = o.balloon;
            d.wings = o.wings;

            int sh = GameShaders.Armor.GetShaderIdFromItemId(dyeItem);
            d.cHead = d.cBody = d.cLegs = sh;
            d.cHandOn = d.cHandOff = d.cBack = d.cFront = sh;
            d.cShoe = d.cWaist = d.cShield = d.cNeck = d.cFace = sh;
            d.cBalloon = d.cWings = sh;

            d.itemAnimation = 0;
            d.itemTime = 0;
        }
    }
}
