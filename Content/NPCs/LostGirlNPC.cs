using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace girlfriend.Content.NPCs
{
    public class LostGirlNPC : ModNPC
    {
        private int friendshipLevel = 0; // 0 = neutrale, 1 = amica, 2 = innamorata, 3 = fidanzata
        private int cookTimer = 0;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Lost Girl");
        }

        public override void SetDefaults()
        {
            NPC.width = 18;
            NPC.height = 40;
            NPC.aiStyle = -1; // AI personalizzata
            NPC.lifeMax = 250;
            NPC.defense = 15;
            NPC.knockBackResist = 0f;
        }

        public override void AI()
        {
            Player player = Main.LocalPlayer;
            if (friendshipLevel >= 3 && NPC.housingCategory == 0) // Fidanzata, con casa
            {
                if (cookTimer > 0)
                {
                    cookTimer--;
                    if (cookTimer == 0)
                    {
                        Item.NewItem(NPC.GetSource_Loot(), NPC.position, ItemID.CookedFish);
                        Main.NewText("Your girlfriend prepared some food!");
                    }
                }
            }
        }

        public override bool CanChat()
        {
            return friendshipLevel > 0;
        }

        public override string GetChat()
        {
            switch (friendshipLevel)
            {
                case 1: return "Grazie per non avermi attaccata...";
                case 2: return "Mi sento così vicina a te!";
                case 3: return "Sono così felice con te.";
                default: return "Chi sei?";
            }
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            if (friendshipLevel == 2) button = "Proponi";
            if (friendshipLevel == 3 && cookTimer == 0) button = "Prepara";
        }

        public override void OnChatButtonClicked(bool firstButton, ref bool shop)
        {
            Player player = Main.LocalPlayer;
            if (firstButton && friendshipLevel == 2 && player.ConsumeItem(ItemID.DiamondRing))
            {
                friendshipLevel = 3;
                NPC.townNPC = true;
                Main.NewText("La Lost Girl è ora la tua fidanzata!");
            }
            else if (firstButton && friendshipLevel == 3)
            {
                cookTimer = 300; // 5 minuti (60 tick/sec * 5 min)
                Main.NewText("Ha iniziato a cucinare...");
            }
        }
    }
}