using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using LiminalHuntersMod.Content.DamageClasses;
using LiminalHuntersMod.Content.Projectiles.Weapons;

namespace LiminalHuntersMod.Content.Items.Weapons
{
    public class D20 : ModItem
    {
        public override void SetDefaults()
        {
            // Hitbox size in world coordinates
            Item.width = 24;
            Item.height = 24;

            // Damage stats: 40 Chance Damage
            Item.damage = 40;
            Item.DamageType = ModContent.GetInstance<ChanceDamageClass>();
            Item.knockBack = 4.5f;
            Item.crit = 6; // 6% base crit chance (+4% default = 10%)

            // Throwing mechanics (Grenade-style)
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.noMelee = true;       // Does not deal damage with swinging the item itself
            Item.noUseGraphic = true; // Sprite doesn't appear in player hand while swinging; projectile is thrown
            Item.autoReuse = true;

            // Projectile spawned
            Item.shoot = ModContent.ProjectileType<D20Projectile>();
            Item.shootSpeed = 14f; // Throw velocity

            // Value & Rarity
            Item.value = Item.buyPrice(silver: 75);
            Item.rare = ItemRarityID.Green;
            Item.UseSound = SoundID.Item1; // Throwing swoosh sound
            Item.consumable = false;      // Weapon with infinite uses
        }

        // Example crafting recipe so you can craft it right away in-game!
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.IronBar, 10)
                .AddIngredient(ItemID.FallenStar, 3)
                .AddIngredient(ItemID.Ruby, 1)
                .AddTile(TileID.Anvils)
                .Register();

            // Lead Bar alternative recipe
            CreateRecipe()
                .AddIngredient(ItemID.LeadBar, 10)
                .AddIngredient(ItemID.FallenStar, 3)
                .AddIngredient(ItemID.Ruby, 1)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
