using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Creative;

namespace GirlfriendMod.Content.Items
{
    public class DiamondRing : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Diamond Ring");
            Tooltip.SetDefault("'A beautiful ring to propose with'\nGive this to the Friendly Nymph when her affection is high enough");
            
            // Journey mode research
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.maxStack = 1;
            Item.value = Item.sellPrice(gold: 5);
            Item.rare = ItemRarityID.Pink;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Diamond, 5);
            recipe.AddIngredient(ItemID.GoldBar, 1);
            recipe.AddTile(TileID.Anvils);
            recipe.Register();
            
            // Alternative with platinum
            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.Diamond, 5);
            recipe2.AddIngredient(ItemID.PlatinumBar, 1);
            recipe2.AddTile(TileID.Anvils);
            recipe2.Register();
        }
    }
}