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
    // This NPC has multiple states:
    // 1. Initially appears as Lost Girl (hostile NPC)
    // 2. Can be befriended using Terraria's built-in emotes
    // 3. Once befriended becomes a friendly NPC
    // 4. Can be romanced through dialogue
    // 5. Can become girlfriend with diamond ring
    public class NymphGirlfriend : ModNPC
    {
        // Track internal states for this NPC
        private bool befriended = false;
        private bool isGirlfriend = false;
        private bool canCook = false;

        // Public accessors for other classes
        public bool IsBefriended() => befriended;
        public bool IsGirlfriend() => isGirlfriend;
        
        // Track emote sequence for befriending
        // These are actual emote IDs from Terraria's EmoteID class
        private readonly int[] correctEmoteSequence = new int[] 
        { 
            // These represent the correct sequence of emotes to befriend the Lost Girl
            // Example: Heart (3), Happy (1), Wave (10), Question (5)
            EmoteID.EmoteHeart, EmoteID.EmoteHappy, EmoteID.EmoteWave, EmoteID.EmoteQuestion
        };
        private List<int> receivedEmotes = new List<int>();
        
        // Affection level (0-100)
        private int affectionLevel = 0;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Lost Girl");
            Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.Nymph]; // Use Nymph's frame count
            
            // Set NPC shop and happiness conditions when she becomes a town NPC
            NPCID.Sets.ExtraFramesCount[Type] = 0;
            NPCID.Sets.AttackFrameCount[Type] = 4;
            NPCID.Sets.DangerDetectRange[Type] = 700;
            NPCID.Sets.AttackType[Type] = 0;
            NPCID.Sets.AttackTime[Type] = 90;
            NPCID.Sets.AttackAverageChance[Type] = 30;
            NPCID.Sets.HatOffsetY[Type] = 0;
            
            // Town NPC stuff
            NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers(0)
            {
                Velocity = 1f,
                Direction = 1
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);

            // Happiness settings when she becomes a town NPC
            NPC.Happiness
                .SetBiomeAffection<ForestBiome>(AffectionLevel.Like)
                .SetBiomeAffection<SnowBiome>(AffectionLevel.Dislike)
                .SetNPCAffection(NPCID.Guide, AffectionLevel.Dislike) // She gets jealous of the Guide
                .SetNPCAffection(NPCID.Dryad, AffectionLevel.Like); // She likes nature
        }

        public override void SetDefaults()
        {
            // Start with Lost Girl defaults
            NPC.CloneDefaults(NPCID.LostGirl);
            NPC.width = 18;
            NPC.height = 46;
            NPC.damage = 0; // Non-aggressive initially
            NPC.defense = 15;
            NPC.lifeMax = 250;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 0f;
            NPC.friendly = false; // Will become true when befriended
            NPC.knockBackResist = 0.5f;
            
            // Initially a hostile NPC
            NPC.aiStyle = NPCAIStyleID.Nymph;
            AIType = NPCID.LostGirl;
            AnimationType = NPCID.LostGirl;
            
            // Not a town NPC initially
            NPC.townNPC = false;
            NPC.homeless = true;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            // Add bestiary info
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Caverns,
                new FlavorTextBestiaryInfoElement("A mysterious girl lost in the caves. Approach her gently and show some emotes to befriend her.")
            });
        }

        public override void AI()
        {
            if (befriended)
            {
                // If befriended, use town NPC behavior
                if (!isGirlfriend)
                {
                    // Stay in place - simple AI for cave encounter
                    NPC.velocity = Vector2.Zero;
                    NPC.friendly = true;
                    NPC.damage = 0;
                }
                else
                {
                    // Town NPC AI when she's a girlfriend
                    NPC.townNPC = true;
                    NPC.aiStyle = NPCAIStyleID.Passive;
                    AIType = NPCID.Guide;
                    AnimationType = NPCID.Guide;
                }
            }
            else
            {
                // Acting as a Lost Girl - check if we should transform
                // This is vanilla-like behavior when player gets close and attacks
                Player closestPlayer = Main.player[Player.FindClosest(NPC.position, NPC.width, NPC.height)];
                
                // Check if player is in range and hits the NPC
                if (!NPC.HasPlayerTarget && Main.player[Player.FindClosest(NPC.Center, 200, 200)].active)
                {
                    // If player gets too close without sending emotes, transform to Nymph
                    if (Vector2.Distance(closestPlayer.Center, NPC.Center) < 200f && NPC.life < NPC.lifeMax)
                    {
                        TransformToNymph();
                    }
                }
            }
        }
        
        private void TransformToNymph()
        {
            // Change to hostile Nymph
            NPC.Transform(NPCID.Nymph);
            NPC.netUpdate = true;
            
            // She becomes angry
            if (Main.netMode != NetmodeID.Server)
            {
                Main.NewText("The Lost Girl transforms into a Nymph!", 255, 50, 50);
            }
        }
        
        // Called when player successfully befriends the Lost Girl
        public void BefriendLostGirl()
        {
            befriended = true;
            NPC.friendly = true;
            NPC.damage = 0;
            DisplayName.SetDefault("Friendly Nymph");
            NPC.dontTakeDamage = true;
            
            // Update the relationship status
            var modSystem = ModContent.GetInstance<GirlfriendSystem>();
            modSystem.SetRelationshipStatus(Main.myPlayer, RelationshipStatus.Friendly);
            
            NPC.netUpdate = true;
            
            // Send happy message
            if (Main.netMode != NetmodeID.Server)
            {
                Main.NewText("The Lost Girl smiles at you shyly...", 255, 150, 150);
            }
        }
        
        // Handles player giving a diamond ring and making her a girlfriend
        public void MakeGirlfriend()
        {
            isGirlfriend = true;
            NPC.friendly = true;
            NPC.damage = 0;
            DisplayName.SetDefault("Girlfriend");
            NPC.townNPC = true;
            NPC.homeless = true; // Will need to assign a home
            
            // Update relationship status
            var modSystem = ModContent.GetInstance<GirlfriendSystem>();
            modSystem.SetRelationshipStatus(Main.myPlayer, RelationshipStatus.Girlfriend);
            
            NPC.netUpdate = true;
            
            // Send happy message
            if (Main.netMode != NetmodeID.Server)
            {
                Main.NewText("She accepts your diamond ring with teary eyes...", 255, 150, 150);
            }
        }
        
        // Enable cooking if there's a cooking pot in her house
        public void CheckForCookingPot()
        {
            if (isGirlfriend && NPC.homeTileX > 0 && NPC.homeTileY > 0)
            {
                // Check for cooking pot in home area
                bool foundCookingPot = false;
                int checkRadius = 35; // Search area
                
                for (int x = NPC.homeTileX - checkRadius; x <= NPC.homeTileX + checkRadius; x++)
                {
                    for (int y = NPC.homeTileY - checkRadius; y <= NPC.homeTileY + checkRadius; y++)
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
                
                canCook = foundCookingPot;
                
                if (canCook && !previousCookingStatus)
                {
                    // First time finding a pot
                    if (Main.netMode != NetmodeID.Server)
                    {
                        Main.NewText("Your girlfriend has learned to cook!", 255, 192, 203);
                    }
                }
                
                previousCookingStatus = canCook;
            }
        }
        
        private bool previousCookingStatus = false;
        
        public override void PostAI()
        {
            // Check for cooking pot periodically
            if (Main.GameUpdateCount % 600 == 0) // Check every 10 seconds
            {
                CheckForCookingPot();
            }
        }
        
        // Handle emote interactions from players
        public bool ReceiveEmote(int emoteId, Player player)
        {
            if (!befriended)
            {
                receivedEmotes.Add(emoteId);
                
                // Keep only the last N emotes where N is the length of the correct sequence
                while (receivedEmotes.Count > correctEmoteSequence.Length)
                {
                    receivedEmotes.RemoveAt(0);
                }
                
                // Check if the current sequence matches the correct one
                bool sequenceMatches = receivedEmotes.Count == correctEmoteSequence.Length;
                
                if (sequenceMatches)
                {
                    for (int i = 0; i < correctEmoteSequence.Length; i++)
                    {
                        if (receivedEmotes[i] != correctEmoteSequence[i])
                        {
                            sequenceMatches = false;
                            break;
                        }
                    }
                }
                
                if (sequenceMatches)
                {
                    BefriendLostGirl();
                    return true;
                }
            }
            
            return false;
        }
        
        // Start cooking process
        public void StartCooking()
        {
            if (isGirlfriend && canCook)
            {
                var modSystem = ModContent.GetInstance<GirlfriendSystem>();
                modSystem.StartCooking(NPC.whoAmI);
                
                if (Main.netMode != NetmodeID.Server)
                {
                    Main.NewText("Your girlfriend started cooking. It will be ready in 5 minutes.", 255, 192, 203);
                }
            }
        }
        
        // Check if currently cooking
        public bool IsCooking()
        {
            if (isGirlfriend && canCook)
            {
                var modSystem = ModContent.GetInstance<GirlfriendSystem>();
                return modSystem.IsCooking(NPC.whoAmI);
            }
            return false;
        }
        
        // Get remaining cook time in seconds
        public int GetRemainingCookTimeInSeconds()
        {
            var modSystem = ModContent.GetInstance<GirlfriendSystem>();
            return modSystem.GetRemainingCookTime(NPC.whoAmI) / 60; // Convert ticks to seconds
        }
        
        // Handle NPC chat options
        public override string GetChat()
        {
            if (isGirlfriend)
            {
                List<string> dialogue = new List<string>();
                
                // Basic girlfriend dialogue
                dialogue.Add("I'm so happy we're together!");
                dialogue.Add("Living on the surface is much nicer than those cold caves.");
                dialogue.Add("Do you want to go exploring sometime?");
                
                if (canCook)
                {
                    dialogue.Add("I've been learning to cook. Would you like me to make something for you?");
                    dialogue.Add("I found some new recipes I'd like to try!");
                }
                
                if (IsCooking())
                {
                    return $"Your meal will be ready in {GetRemainingCookTimeInSeconds()} seconds!";
                }
                
                return Main.rand.Next(dialogue);
            }
            else if (befriended)
            {
                List<string> dialogue = new List<string>();
                
                // Friendly but not girlfriend dialogue
                dialogue.Add("Thank you for not attacking me like others do.");
                dialogue.Add("I've been alone in these caves for so long...");
                dialogue.Add("Do you think we could be... friends?");
                dialogue.Add("I wish I could leave these caves someday.");
                
                return Main.rand.Next(dialogue);
            }
            
            // Default Lost Girl dialogue
            return "...";
        }
        
        // Add special shop
        public override void SetChatButtons(ref string button, ref string button2)
        {
            if (isGirlfriend)
            {
                // Standard "Shop" button if she has items to sell
                button = "Chat";
                
                // Cooking button if she can cook
                if (canCook && !IsCooking())
                {
                    button2 = "Cook Meal";
                }
                else if (IsCooking())
                {
                    button2 = $"Cooking: {GetRemainingCookTimeInSeconds()}s";
                }
            }
            else if (befriended)
            {
                button = "Chat";
                
                // Show "Girlfriend" option if affection is high enough
                var modSystem = ModContent.GetInstance<GirlfriendSystem>();
                if (affectionLevel >= 80)
                {
                    button2 = "Girlfriend";
                }
            }
        }
        
        // Handle button clicks
        public override void OnChatButtonClicked(bool firstButton, ref bool shop)
        {
            if (isGirlfriend)
            {
                if (firstButton)
                {
                    // Chat option - increases affection slightly
                    affectionLevel = Math.Min(100, affectionLevel + 1);
                    shop = false;
                }
                else if (canCook && !IsCooking())
                {
                    // Start cooking
                    StartCooking();
                }
            }
            else if (befriended)
            {
                if (firstButton)
                {
                    // Open romance dialogue options
                    // This would be implemented with a custom UI in a larger mod
                    Main.npcChatText = "What would you like to talk about?";
                    
                    // Simple implementation: just increase affection
                    affectionLevel = Math.Min(100, affectionLevel + 5);
                }
                else if (affectionLevel >= 80)
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
                        MakeGirlfriend();
                        Main.npcChatText = "Yes! I'd love to be your girlfriend and live with you on the surface!";
                    }
                    else
                    {
                        Main.npcChatText = "I'd like something shiny to make our relationship official...maybe a diamond ring?";
                    }
                }
            }
        }
        
        // Save/load NPC state
        public override void SaveData(TagCompound tag)
        {
            tag["befriended"] = befriended;
            tag["isGirlfriend"] = isGirlfriend;
            tag["canCook"] = canCook;
            tag["affectionLevel"] = affectionLevel;
        }
        
        public override void LoadData(TagCompound tag)
        {
            befriended = tag.GetBool("befriended");
            isGirlfriend = tag.GetBool("isGirlfriend");
            canCook = tag.GetBool("canCook");
            affectionLevel = tag.GetInt("affectionLevel");
            
            // Update display name based on status
            if (isGirlfriend)
            {
                DisplayName.SetDefault("Girlfriend");
                NPC.townNPC = true;
            }
            else if (befriended)
            {
                DisplayName.SetDefault("Friendly Nymph");
                NPC.friendly = true;
                NPC.damage = 0;
                NPC.dontTakeDamage = true;
            }
        }
        
        // Network syncing
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(befriended);
            writer.Write(isGirlfriend);
            writer.Write(canCook);
            writer.Write(affectionLevel);
        }
        
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            befriended = reader.ReadBoolean();
            isGirlfriend = reader.ReadBoolean();
            canCook = reader.ReadBoolean();
            affectionLevel = reader.ReadInt32();
        }
    }
}