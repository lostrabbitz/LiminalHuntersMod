using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using LiminalHuntersMod.Common.Players;

namespace LiminalHuntersMod.Content.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class MirageLeggings : ModItem
    {
        public override void UpdateEquip(Player player)
        {
            MiragePlayer mp = player.GetModPlayer<MiragePlayer>();
            mp.DashMaxBlocks += 10f;      // +10 blocks dash distance
            mp.DashBonusDamage += 15;     // +15 dash damage on top of the normal damage
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.value = Item.sellPrice(silver: 30);
            Item.rare = ItemRarityID.Green;
            Item.defense = 4;
        }
    }
}
