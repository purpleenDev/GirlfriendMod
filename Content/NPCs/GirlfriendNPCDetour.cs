using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Personalities;
using Terraria.GameContent.Bestiary;
using Terraria.Localization;
using Terraria.Audio;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria.GameContent;
using static GirlfriendMod.GirlfriendSystem;

namespace GirlfriendMod.Content.NPCs
{
    // This class handles the detour of vanilla Lost Girl and Nymph NPCs
    // to add our custom behavior without replacing them
    public class GirlfriendNPCDetour : GlobalNPC
    {
        // Dictionary to track which Lost Girl/Nymph NPCs have been befriended
        private static Dictionary<int, bool> befriendedNPCs = new Dictionary<int, bool>();
        private static Dictionary<int, bool> girlfriendNPCs = new Dictionary<int, bool>();
        private static Dictionary<int, bool> canCookNPCs = new Dictionary<int, bool>();
        private static Dictionary<int, int> affectionLevels = new Dictionary<int, int>();
        private static Dictionary<int, List<int>> receivedEmotes = new Dictionary<int, List<int>>();

        // Track the correct sequence of emotes needed to befriend
        private static readonly int[] correctEmoteSequence = new int[]
        { 
            // These represent heart, happy, wave, question
            // Use actual Terraria EmoteID values 
            EmoteID.EmoteHeart, EmoteID.EmoteHappy, EmoteID.EmoteWave, EmoteID.EmoteQuestion
        };

        // Add to Lost Girl/Nymph AI
        public override bool PreAI(NPC npc)
        {
            // Only affect Lost Girl and Nymph
            if (npc.type == NPCID.LostGirl || npc.type == NPCID.Nymph)
            {
                // If this NPC is befriended
                if (befriendedNPCs.ContainsKey(npc.whoAmI) && befriendedNPCs[npc.whoAmI])
                {
                    // If upgraded to girlfriend
                    if (girlfriendNPCs.ContainsKey(npc.whoAmI) && girlfriendNPCs[npc.whoAmI])
                    {
                        // Make her act like a town NPC
                        npc.friendly = true;
                        npc.damage = 0;
                        npc.dontTakeDamage = true;
                        npc.townNPC = true;

                        // Use Town NPC AI
                        npc.aiStyle = NPCAIStyleID.Passive;

                        // Periodically check for cooking pot
                        if (Main.GameUpdateCount % 600 == 0) // Every 10 seconds
                        {
                            CheckForCookingPot(npc);
                        }

                        return false; // Skip vanilla AI
                    }
                    else
                    {
                        // Just befriended but not girlfriend yet
                        npc.friendly = true;
                        npc.damage = 0;
                        npc.dontTakeDamage = true;

                        // Stay in place
                        npc.velocity = Vector2.Zero;

                        return false; // Skip vanilla AI
                    }
                }
            }

            return true; // Use vanilla AI for non-befriended NPCs
        }

        // Override how Lost Girl transforms into Nymph
        public override void PostAI(NPC npc)
        {
            if (npc.type == NPCID.LostGirl)
            {
                // If this is a befriended Lost Girl, prevent transformation
                if (befriendedNPCs.ContainsKey(npc.whoAmI) && befriendedNPCs[npc.whoAmI])
                {
                    // Ensure she doesn't transform
                    npc.ai[0] = 0;
                }
            }
        }

        // Change display name
        public override void ModifyNPCNameList(NPC npc, List<string> nameList)
        {
            if ((npc.type == NPCID.LostGirl || npc.type == NPCID.Nymph) &&
                befriendedNPCs.ContainsKey(npc.whoAmI) && befriendedNPCs[npc.whoAmI])
            {
                nameList.Clear();

                if (girlfriendNPCs.ContainsKey(npc.whoAmI) && girlfriendNPCs[npc.whoAmI])
                {
                    nameList.Add("Girlfriend");
                }
                else
                {
                    nameList.Add("Friendly Nymph");
                }
            }
        }

        // Override draw
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (npc.type == NPCID.Nymph &&
                befriendedNPCs.ContainsKey(npc.whoAmI) && befriendedNPCs[npc.whoAmI])
            {
                // For a full mod, you might modify the appearance here
                // This could add special visual effects or change the sprite

                // For now, just use default drawing
                return true;
            }

            return true;
        }

        // Handle chat for befriended NPCs
        public override string GetChat(NPC npc)
        {
            if ((npc.type == NPCID.LostGirl || npc.type == NPCID.Nymph) &&
                befriendedNPCs.ContainsKey(npc.whoAmI) && befriendedNPCs[npc.whoAmI])
            {
                if (girlfriendNPCs.ContainsKey(npc.whoAmI) && girlfriendNPCs[npc.whoAmI])
                {
                    List<string> dialogue = new List<string>();

                    // Basic girlfriend dialogue
                    dialogue.Add("I'm so happy we're together!");
                    dialogue.Add("Living on the surface is much nicer than those cold caves.");
                    dialogue.Add("Do you want to go exploring sometime?");

                    if (canCookNPCs.ContainsKey(npc.whoAmI) && canCookNPCs[npc.whoAmI])
                    {
                        dialogue.Add("I've been learning to cook. Would you like me to make something for you?");
                        dialogue.Add("I found some new recipes I'd like to try!");
                    }

                    if (IsCooking(npc.whoAmI))
                    {
                        return $"Your meal will be ready in {GetRemainingCookTimeInSeconds(npc.whoAmI)} seconds!";
                    }

                    return Main.rand.Next(dialogue);
                }
                else
                {
                    List<string> dialogue = new List<string>();

                    // Friendly but not girlfriend dialogue
                    dialogue.Add("Thank you for not attacking me like others do.");
                    dialogue.Add("I've been alone in these caves for so long...");
                    dialogue.Add("Do you think we could be... friends?");
                    dialogue.Add("I wish I could leave these caves someday.");

                    return Main.rand.Next(dialogue);
                }
            }

            return null; // Use vanilla chat for non-befriended NPCs
        }

        // Add custom chat buttons
        public override void SetChatButtons(NPC npc, ref string button, ref string button2)
        {
            if ((npc.type == NPCID.LostGirl || npc.type == NPCID.Nymph) &&
                befriendedNPCs.ContainsKey(npc.whoAmI) && befriendedNPCs[npc.whoAmI])
            {
                if (girlfriendNPCs.ContainsKey(npc.whoAmI) && girlfriendNPCs[npc.whoAmI])
                {
                    // Standard "Shop" button if she has items to sell
                    button = "Chat";

                    // Cooking button if she can cook
                    if (canCookNPCs.ContainsKey(npc.whoAmI) && canCookNPCs[npc.whoAmI] && !IsCooking(npc.whoAmI))
                    {
                        button2 = "Cook Meal";
                    }
                    else if (IsCooking(npc.whoAmI))
                    {
                        button2 = $"Cooking: {GetRemainingCookTimeInSeconds(npc.whoAmI)}s";
                    }
                }
                else
                {
                    button = "Chat";

                    // Show "Girlfriend" option if affection is high enough
                    if (affectionLevels.ContainsKey(npc.whoAmI) && affectionLevels[npc.whoAmI] >= 80)
                    {
                        button2 = "Girlfriend";
                    }
                }
            }
        }

        // Handle button clicks
        public override void OnChatButtonClicked(NPC npc, bool firstButton)
        {
            if ((npc.type == NPCID.LostGirl || npc.type == NPCID.Nymph) &&
                befriendedNPCs.ContainsKey(npc.whoAmI) && befriendedNPCs[npc.whoAmI])
            {
                if (girlfriendNPCs.ContainsKey(npc.whoAmI) && girlfriendNPCs[npc.whoAmI])
                {
                    if (firstButton)
                    {
                        // Chat option - increases affection slightly
                        if (!affectionLevels.ContainsKey(npc.whoAmI))
                        {
                            affectionLevels[npc.whoAmI] = 0;
                        }
                        affectionLevels[npc.whoAmI] = Math.Min(100, affectionLevels[npc.whoAmI] + 1);
                    }
                    else if (canCookNPCs.ContainsKey(npc.whoAmI) && canCookNPCs[npc.whoAmI] && !IsCooking(npc.whoAmI))
                    {
                        // Start cooking
                        StartCooking(npc.whoAmI);

                        if (Main.netMode != NetmodeID.Server)
                        {
                            Main.NewText("Your girlfriend started cooking. It will be ready in 5 minutes.", 255, 192, 203);
                        }
                    }
                }
                else
                {
                    if (firstButton)
                    {
                        // Open romance dialogue options
                        Main.npcChatText = "What would you like to talk about?";

                        // Simple implementation: just increase affection
                        if (!affectionLevels.ContainsKey(npc.whoAmI))
                        {
                            affectionLevels[npc.whoAmI] = 0;
                        }
                        affectionLevels[npc.whoAmI] = Math.Min(100, affectionLevels[npc.whoAmI] + 5);
                    }
                    else if (affectionLevels.ContainsKey(npc.whoAmI) && affectionLevels[npc.whoAmI] >= 80)
                    {
                        // Check if player has diamond ring
                        Player player = Main.LocalPlayer;
                        int diamondRingType = ModContent.ItemType<GirlfriendMod.Content.Items.DiamondRing>();

                        // Check if player has the ring
                        bool hasRing = false;
                        for (int i = 0; i < 58; i++) // Check inventory and equipped items
                        {
                            if (player.inventory[i].type == diamondRingType && player.inventory[i].stack > 0)
                            {
                                hasRing = true;
                                // Remove ring
                                player.inventory[i].stack--;
                                if (player.inventory[i].stack <= 0)
                                {
                                    player.inventory[i].TurnToAir();
                                }
                                break;
                            }
                        }

                        if (hasRing)
                        {
                            // Make her a girlfriend
                            girlfriendNPCs[npc.whoAmI] = true;
                            npc.homeless = true; // Will need to assign a home
                            npc.townNPC = true;

                            // Update relationship status
                            var modSystem = ModContent.GetInstance<GirlfriendSystem>();
                            modSystem.SetRelationshipStatus(Main.myPlayer, RelationshipStatus.Girlfriend);

                            Main.npcChatText = "Yes! I'd love to be your girlfriend and live with you on the surface!";

                            // Send happy message
                            if (Main.netMode != NetmodeID.Server)
                            {
                                Main.NewText("She accepts your diamond ring with teary eyes...", 255, 150, 150);
                            }
                        }
                        else
                        {
                            Main.npcChatText = "I'd like something shiny to make our relationship official...maybe a diamond ring?";
                        }
                    }
                }
            }
        }

        // Handle emote interactions
        public bool ReceiveEmote(int npcId, int emoteId, Player player)
        {
            NPC npc = Main.npc[npcId];

            if ((npc.type == NPCID.LostGirl || npc.type == NPCID.Nymph) &&
                (!befriendedNPCs.ContainsKey(npcId) || !befriendedNPCs[npcId]))
            {
                // Initialize emote list if needed
                if (!receivedEmotes.ContainsKey(npcId))
                {
                    receivedEmotes[npcId] = new List<int>();
                }

                receivedEmotes[npcId].Add(emoteId);

                // Keep only the last N emotes where N is the length of the correct sequence
                while (receivedEmotes[npcId].Count > correctEmoteSequence.Length)
                {
                    receivedEmotes[npcId].RemoveAt(0);
                }

                // Check if the current sequence matches the correct one
                bool sequenceMatches = receivedEmotes[npcId].Count == correctEmoteSequence.Length;

                if (sequenceMatches)
                {
                    for (int i = 0; i < correctEmoteSequence.Length; i++)
                    {
                        if (receivedEmotes[npcId][i] != correctEmoteSequence[i])
                        {
                            sequenceMatches = false;
                            break;
                        }
                    }
                }

                if (sequenceMatches)
                {
                    // Befriend the Lost Girl/Nymph
                    befriendedNPCs[npcId] = true;
                    npc.friendly = true;
                    npc.damage = 0;
                    npc.dontTakeDamage = true;

                    // Initialize affection
                    affectionLevels[npcId] = 0;

                    // Update relationship status
                    var modSystem = ModContent.GetInstance<GirlfriendSystem>();
                    modSystem.SetRelationshipStatus(Main.myPlayer, RelationshipStatus.Friendly);

                    npc.netUpdate = true;

                    // Send happy message
                    if (Main.netMode != NetmodeID.Server)
                    {
                        Main.NewText("The Lost Girl smiles at you shyly...", 255, 150, 150);
                    }

                    return true;
                }
            }

            return false;
        }

        // Check for cooking pot
        private void CheckForCookingPot(NPC npc)
        {
            if (girlfriendNPCs.ContainsKey(npc.whoAmI) && girlfriendNPCs[npc.whoAmI] &&
                npc.homeTileX > 0 && npc.homeTileY > 0)
            {
                // Check for cooking pot in home area
                bool foundCookingPot = false;
                int checkRadius = 35; // Search area

                for (int x = npc.homeTileX - checkRadius; x <= npc.homeTileX + checkRadius; x++)
                {
                    for (int y = npc.homeTileY - checkRadius; y <= npc.homeTileY + checkRadius; y++)
                    {
                        Tile tile = Framing.GetTileSafely(x, y);
                        if (tile.HasTile && tile.TileType == TileID.CookingPots)
                        {
                            foundCookingPot = true;
                            break;
                        }
                    }

                    if (foundCookingPot)
                        break;
                }

                bool previousCookingStatus = canCookNPCs.ContainsKey(npc.whoAmI) && canCookNPCs[npc.whoAmI];
                canCookNPCs[npc.whoAmI] = foundCookingPot;

                if (foundCookingPot && !previousCookingStatus)
                {
                    // First time finding a pot
                    if (Main.netMode != NetmodeID.Server)
                    {
                        Main.NewText("Your girlfriend has learned to cook!", 255, 192, 203);
                    }
                }
            }
        }

        // Cooking methods
        private void StartCooking(int npcId)
        {
            var modSystem = ModContent.GetInstance<GirlfriendSystem>();
            modSystem.StartCooking(npcId);
        }

        private bool IsCooking(int npcId)
        {
            var modSystem = ModContent.GetInstance<GirlfriendSystem>();
            return modSystem.IsCooking(npcId);
        }

        private int GetRemainingCookTimeInSeconds(int npcId)
        {
            var modSystem = ModContent.GetInstance<GirlfriendSystem>();
            return modSystem.GetRemainingCookTime(npcId) / 60; // Convert ticks to seconds
        }

        // Reset tracked data on world unload to prevent issues
        public override void OnWorldUnload()
        {
            befriendedNPCs.Clear();
            girlfriendNPCs.Clear();
            canCookNPCs.Clear();
            affectionLevels.Clear();
            receivedEmotes.Clear();
        }

        // For multiplayer sync
        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            if (npc.type == NPCID.LostGirl || npc.type == NPCID.Nymph)
            {
                bool isBefriended = befriendedNPCs.ContainsKey(npc.whoAmI) && befriendedNPCs[npc.whoAmI];
                bool isGirlfriend = girlfriendNPCs.ContainsKey(npc.whoAmI) && girlfriendNPCs[npc.whoAmI];
                bool canCook = canCookNPCs.ContainsKey(npc.whoAmI) && canCookNPCs[npc.whoAmI];
                int affection = affectionLevels.ContainsKey(npc.whoAmI) ? affectionLevels[npc.whoAmI] : 0;

                binaryWriter.Write(isBefriended);
                binaryWriter.Write(isGirlfriend);
                binaryWriter.Write(canCook);
                binaryWriter.Write(affection);
            }
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            if (npc.type == NPCID.LostGirl || npc.type == NPCID.Nymph)
            {
                bool isBefriended = binaryReader.ReadBoolean();
                bool isGirlfriend = binaryReader.ReadBoolean();
                bool canCook = binaryReader.ReadBoolean();
                int affection = binaryReader.ReadInt32();

                befriendedNPCs[npc.whoAmI] = isBefriended;
                girlfriendNPCs[npc.whoAmI] = isGirlfriend;
                canCookNPCs[npc.whoAmI] = canCook;
                affectionLevels[npc.whoAmI] = affection;
            }
        }
    }
}