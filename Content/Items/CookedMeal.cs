using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Creative;

namespace GirlfriendMod.Content.Items
{
    public class CookedMeal : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Home-Cooked Meal");
            Tooltip.SetDefault("'Made with love by your girlfriend'\nGreatly increases all stats for 10 minutes");
            
            // Journey mode research
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 5;
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.useTime = 17;
            Item.useAnimation = 17;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item2;
            Item.maxStack = 30;
            Item.consumable = true;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(silver: 50);
            Item.buffType = BuffID.WellFed3; // Exquisitely Stuffed buff
            Item.buffTime = 36000; // 10 minutes (60 seconds * 60 ticks * 10 minutes)
        }
    }
}