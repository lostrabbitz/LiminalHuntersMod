using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using LiminalHuntersMod.Content.Buffs;
using LiminalHuntersMod.Common.Players;

namespace LiminalHuntersMod.Content.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class MirageHelmet : ModItem
    {
        public static LocalizedText SetBonusText { get; private set; }

        public override void SetStaticDefaults()
        {
            SetBonusText = this.GetLocalization("SetBonus");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.value = Item.sellPrice(silver: 30);
            Item.rare = ItemRarityID.Green;
            Item.defense = 4;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<MiragePlayer>().MaxClones += 2;   // +2 max clones
        }
 
        public override bool IsArmorSet(Item head, Item body, Item legs)
            => body.type == ModContent.ItemType<MirageBreastplate>() && legs.type == ModContent.ItemType<MirageLeggings>();
 
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = SetBonusText.Value;
            // Refreshed every tick: the buff lasts exactly as long as the full set is worn.
            player.AddBuff(ModContent.BuffType<MirageClassBuff>(), 2);
        }
    }
}
