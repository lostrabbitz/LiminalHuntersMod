using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using LiminalHuntersMod.Content.DamageClasses;
using LiminalHuntersMod.Common.Players;

namespace LiminalHuntersMod.Content.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class MirageBreastplate : ModItem
    {
        public override void UpdateEquip(Player player)
        {
            player.GetDamage<MirageDamageClass>().Flat += 3f;          // +3 mirage damage
            player.GetModPlayer<MiragePlayer>().CloneTimeAlive += 1f;  // +1 second clone lifespan
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.value = Item.sellPrice(silver: 40);
            Item.rare = ItemRarityID.Green;
            Item.defense = 6;
        }
    }
}
