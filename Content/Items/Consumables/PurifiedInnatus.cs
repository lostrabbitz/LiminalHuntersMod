using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using LiminalHuntersMod.Common.Players;
using Microsoft.Xna.Framework;

namespace LiminalHuntersMod.Content.Items.Consumables
{
    public class PurifiedInnatus : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 19;
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = Item.CommonMaxStack;
            Item.consumable = true;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = Item.sellPrice(silver: 25);
        }

        public override bool CanUseItem(Player player)
        {
            var innatusPlayer = player.GetModPlayer<InnatusPlayer>();
            return innatusPlayer.maxInnatus < InnatusPlayer.AbsoluteMaxInnatus;
        }

        public override bool? UseItem(Player player)
        {
            var innatusPlayer = player.GetModPlayer<InnatusPlayer>();
            if (innatusPlayer.maxInnatus >= InnatusPlayer.AbsoluteMaxInnatus)
                return false;

            innatusPlayer.maxInnatus = MathHelper.Min(
                innatusPlayer.maxInnatus + 10f,
                InnatusPlayer.AbsoluteMaxInnatus
            );

            // Also raise current Innatus by 10 (up to the new maxInnatus)
            innatusPlayer.innatus = MathHelper.Clamp(
                innatusPlayer.innatus + 10f,
                InnatusPlayer.MinInnatus,
                innatusPlayer.maxInnatus
            );

            return true;
        }
    }
}