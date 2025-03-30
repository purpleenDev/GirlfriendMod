using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using GirlfriendMod.Content.NPCs;
using System.Reflection;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using System;

namespace GirlfriendMod.Content.UI
{
    // This class handles the integration with Terraria's native emote system
    public class EmoteHandler : ModSystem
    {
        public override void Load()
        {
            // Hook into the vanilla emote system when it's used
            On.Terraria.GameContent.Events.EmoteBubble.NewBubble += EmoteBubble_NewBubble;
        }

        private int EmoteBubble_NewBubble(On.Terraria.GameContent.Events.EmoteBubble.orig_NewBubble orig, int ID, Vector2 position, int time, int whoAmI, bool emotion)
        {
            // Call original method first
            int bubbleID = orig(ID, position, time, whoAmI, emotion);

            // If this is a player emote
            if (emotion && whoAmI >= 0 && whoAmI < Main.maxPlayers)
            {
                Player player = Main.player[whoAmI];
                if (player.active)
                {
                    // Check if the player is near a Lost Girl/Nymph NPC
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC npc = Main.npc[i];
                        if (npc.active && (npc.type == NPCID.LostGirl || npc.type == NPCID.Nymph))
                        {
                            // Calculate distance to this NPC
                            float distance = Vector2.Distance(player.Center, npc.Center);
                            
                            // If close enough to interact
                            if (distance <= 200f)
                            {
                                var globalNPC = npc.GetGlobalNPC<GirlfriendNPCDetour>();
                                bool success = globalNPC.ReceiveEmote(npc.whoAmI, ID, player);
                                
                                // If successfully befriended
                                if (success)
                                {
                                    // Send confirmation to the player
                                    if (Main.netMode != NetmodeID.Server)
                                    {
                                        String message = "You've befriended the Lost Girl!";
                                        Main.NewText(message, 255, 192, 203);
                                    }
                                }
                                else
                                {
                                    // Give a hint after a certain number of emotes
                                    Random random = new Random((int)DateTime.Now.Ticks);
                                    if (random.Next(5) == 0) // 20% chance
                                    {
                                        // Send hint about emote sequence
                                        if (Main.netMode != NetmodeID.Server)
                                        {
                                            String message = "The Lost Girl seems to be reacting to your emotes...";
                                            Main.NewText(message, 255, 192, 203);
                                        }
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }

            return bubbleID;
        }

        public override void Unload()
        {
            // Unhook from the emote system
            On.Terraria.GameContent.Events.EmoteBubble.NewBubble -= EmoteBubble_NewBubble;
        }
    }
}