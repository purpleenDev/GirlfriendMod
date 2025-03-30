using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using Microsoft.Xna.Framework;
using GirlfriendMod.Content.NPCs;
using System.Collections.Generic;

namespace GirlfriendMod.Content.Players
{
    // This class adds features and tracking for the player
    public class GirlfriendModPlayer : ModPlayer
    {
        public bool foundLostGirl = false;
        public bool shownEmoteHint = false;
        
        public override void OnEnterWorld(Player player)
        {
            // Reset tracking when player enters world
            foundLostGirl = false;
            shownEmoteHint = false;
        }
        
        public override void PostUpdate()
        {
            // Check if player is near a Lost Girl and hasn't received the hint yet
            if (!shownEmoteHint)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && (npc.type == NPCID.LostGirl || npc.type == NPCID.Nymph))
                    {
                        // Distance to show hint
                        float distance = Vector2.Distance(Player.Center, npc.Center);
                        
                        if (distance <= 300f)
                        {
                            // Player found a Lost Girl, show hint about emotes
                            if (Main.netMode != NetmodeID.Server)
                            {
                                string hintText = Language.GetTextValue("Mods.GirlfriendMod.Common.BefriendHint");
                                Main.NewText(hintText, 255, 192, 203);
                                
                                // Show a more specific hint about the emote sequence
                                string sequenceHint = Language.GetTextValue("Mods.GirlfriendMod.EmoteSequenceHint.Hint1");
                                Main.NewText(sequenceHint, 255, 192, 203);
                            }
                            
                            foundLostGirl = true;
                            shownEmoteHint = true;
                            break;
                        }
                    }
                }
            }
        }
        
        // Save player's progress with Lost Girl/Nymph relationship
        public override void SaveData(TagCompound tag)
        {
            tag["foundLostGirl"] = foundLostGirl;
            tag["shownEmoteHint"] = shownEmoteHint;
        }
        
        public override void LoadData(TagCompound tag)
        {
            foundLostGirl = tag.GetBool("foundLostGirl");
            shownEmoteHint = tag.GetBool("shownEmoteHint");
        }
    }
}