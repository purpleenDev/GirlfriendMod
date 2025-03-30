using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent;
using Terraria.GameContent.UI;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Linq;

namespace GirlfriendMod.Content.NPCs
{
    public class GirlfriendNPCDetour : GlobalNPC
    {
        // Static fields to track NPC states
        private static Dictionary<int, bool> befriendedNPCs = new();
        private static Dictionary<int, bool> girlfriendNPCs = new();
        private static Dictionary<int, bool> canCookNPCs = new();
        private static Dictionary<int, int> affectionLevels = new();
        private static Dictionary<int, List<int>> receivedEmotes = new();

        // Custom texture for Lost Girl and Friendly Girl when moving
        private static Asset<Texture2D> friendlyGirlTexture;

        // Sequence of emotes required to befriend the NPC
        private static readonly int[] correctEmoteSequence = { EmoteID.EmotionLove, EmoteID.EmoteHappiness };

        public override bool AppliesToEntity(NPC npc, bool lateInstantiation)
        {
            return npc.type == NPCID.LostGirl || npc.type == NPCID.Nymph;
        }

        public override bool PreAI(NPC npc)
        {
            // Prevent Lost Girl transformation when a player is nearby, unless attacked
            if (npc.type == NPCID.LostGirl && (!befriendedNPCs.TryGetValue(npc.whoAmI, out bool isBefriended) || !isBefriended))
            {
                Player nearestPlayer = Main.player[Player.FindClosest(npc.position, npc.width, npc.height)];
                if (Vector2.Distance(nearestPlayer.Center, npc.Center) <= 300f)
                {
                    npc.ai[0] = 0; // Reset transformation timer
                }
            }

            if (befriendedNPCs.TryGetValue(npc.whoAmI, out bool befriended) && befriended)
            {
                npc.friendly = true;
                npc.damage = 0;
                npc.dontTakeDamage = true;
                npc.townNPC = true;
                npc.aiStyle = NPCAIStyleID.Passive;

                if (girlfriendNPCs.TryGetValue(npc.whoAmI, out bool isGirlfriend) && isGirlfriend)
                {
                    if (Main.GameUpdateCount % 600 == 0)
                    {
                        CheckForCookingPot(npc);
                    }
                }
                else if (Main.rand.NextBool(1000)) // Occasional emotes for Friendly Girl
                {
                    int emoteID = Main.rand.NextBool() ? EmoteID.EmoteSadness : EmoteID.EmoteConfused;
                    EmoteBubble.NewBubble(emoteID, new WorldUIAnchor(npc), 180);
                }
                return false; // Override default AI
            }
            return true;
        }

        public override void PostAI(NPC npc)
        {
            if (npc.type == NPCID.LostGirl && befriendedNPCs.TryGetValue(npc.whoAmI, out bool befriended) && befriended)
            {
                npc.ai[0] = 0; // Prevent transformation for Friendly Girl
                npc.GivenName = girlfriendNPCs.TryGetValue(npc.whoAmI, out bool isGirlfriend) && isGirlfriend ? "Girlfriend" : "Friendly Girl";
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!Main.dedServ && friendlyGirlTexture != null)
            {
                Texture2D textureToUse;
                bool isMoving = Math.Abs(npc.velocity.X) > 0.1f; // Consider moving if horizontal velocity is significant

                if (npc.type == NPCID.LostGirl)
                {
                    if (befriendedNPCs.TryGetValue(npc.whoAmI, out bool befriended) && befriended)
                    {
                        // Friendly Girl uses FriendlyGirl sprite when moving
                        textureToUse = isMoving ? friendlyGirlTexture.Value : TextureAssets.Npc[npc.type].Value;
                    }
                    else
                    {
                        // Lost Girl uses FriendlyGirl sprite when moving, vanilla otherwise
                        textureToUse = isMoving ? friendlyGirlTexture.Value : TextureAssets.Npc[npc.type].Value;
                    }
                }
                else if (npc.type == NPCID.Nymph)
                {
                    // Nymph always uses NPC_195
                    textureToUse = TextureAssets.Npc[NPCID.Nymph].Value; // NPC_195
                }
                else
                {
                    return true; // Use default texture for other NPCs
                }

                // Draw the NPC with the selected texture
                SpriteEffects effects = npc.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                Vector2 drawPosition = npc.Center - screenPos;
                spriteBatch.Draw(textureToUse, drawPosition, npc.frame, drawColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, effects, 0f);
                return false; // Skip vanilla drawing
            }
            return true; // Use vanilla drawing if texture isn’t loaded
        }

        public override void GetChat(NPC npc, ref string chat)
        {
            if (!befriendedNPCs.TryGetValue(npc.whoAmI, out bool befriended) || !befriended) return;

            List<string> dialogue = new();
            if (girlfriendNPCs.TryGetValue(npc.whoAmI, out bool isGirlfriend) && isGirlfriend)
            {
                dialogue.AddRange(new[] { "I'm so happy we're together!", "Living on the surface is much nicer than those cold caves.", "Do you want to go exploring sometime?" });
                if (canCookNPCs.TryGetValue(npc.whoAmI, out bool canCook) && canCook)
                {
                    dialogue.AddRange(new[] { "I've been learning to cook. Would you like me to make something for you?", "I found some new recipes I'd like to try!" });
                }
                chat = IsCooking(npc.whoAmI) ? $"Your meal will be ready in {GetRemainingCookTimeInSeconds(npc.whoAmI)} seconds!" : Main.rand.Next(dialogue);
            }
            else
            {
                dialogue.AddRange(new[] { "Thank you for not attacking me like others do.", "I've been alone in these caves for so long...", "Do you think we could be... friends?", "I wish I could leave these caves someday." });
                chat = Main.rand.Next(dialogue);
            }
        }

        public override void OnChatButtonClicked(NPC npc, bool firstButton)
        {
            if (!befriendedNPCs.TryGetValue(npc.whoAmI, out bool befriended) || !befriended) return;

            var modPlayer = Main.LocalPlayer.GetModPlayer<global::GirlfriendMod.Content.Players.GirlfriendModPlayer>();

            if (girlfriendNPCs.TryGetValue(npc.whoAmI, out bool isGirlfriend) && isGirlfriend)
            {
                if (firstButton)
                {
                    affectionLevels[npc.whoAmI] = Math.Min(100, affectionLevels.GetValueOrDefault(npc.whoAmI) + 1);
                }
                else if (canCookNPCs.TryGetValue(npc.whoAmI, out bool canCook) && canCook && !IsCooking(npc.whoAmI))
                {
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
                    Main.npcChatText = "What would you like to talk about?";
                    affectionLevels[npc.whoAmI] = Math.Min(100, affectionLevels.GetValueOrDefault(npc.whoAmI) + 5);
                }
                else if (affectionLevels.TryGetValue(npc.whoAmI, out int affection) && affection >= 80)
                {
                    Player player = Main.LocalPlayer;
                    int diamondRingType = ModContent.ItemType<global::GirlfriendMod.Content.Items.DiamondRing>();
                    bool hasRing = player.ConsumeItem(diamondRingType);

                    if (hasRing)
                    {
                        girlfriendNPCs[npc.whoAmI] = true;
                        npc.homeless = true;
                        npc.townNPC = true;
                        modPlayer.relationshipStatus = GirlfriendSystem.RelationshipStatus.Girlfriend;
                        Main.npcChatText = "Yes! I'd love to be your girlfriend and live with you on the surface!";
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

        public bool ReceiveEmote(int npcId, int emoteId, Player player)
        {
            NPC npc = Main.npc[npcId];
            var modPlayer = player.GetModPlayer<global::GirlfriendMod.Content.Players.GirlfriendModPlayer>();

            if ((npc.type != NPCID.LostGirl && npc.type != NPCID.Nymph) ||
                (befriendedNPCs.TryGetValue(npcId, out bool befriended) && befriended)) return false;

            receivedEmotes.TryAdd(npcId, new List<int>());
            var emotes = receivedEmotes[npcId];

            switch (emoteId)
            {
                case EmoteID.EmotionLove:
                case EmoteID.EmoteHappiness:
                    int[] happyEmotes = { EmoteID.EmoteHappiness, EmoteID.EmoteLaugh, EmoteID.EmotionLove };
                    EmoteBubble.NewBubble(happyEmotes[Main.rand.Next(happyEmotes.Length)], new WorldUIAnchor(npc), 180);
                    break;
                case EmoteID.EmoteAnger:
                    EmoteBubble.NewBubble(Main.rand.NextBool() ? EmoteID.EmoteSadness : EmoteID.EmoteAnger, new WorldUIAnchor(npc), 180);
                    break;
                case EmoteID.EmoteSadness:
                    if (Main.rand.NextBool())
                    {
                        npc.velocity = -npc.DirectionTo(player.Center) * 2f;
                    }
                    break;
                case EmoteID.EmoteScowl:
                    EmoteBubble.NewBubble(EmoteID.EmoteAnger, new WorldUIAnchor(npc), 180);
                    npc.velocity = -npc.DirectionTo(player.Center) * 3f;
                    break;
            }

            emotes.Add(emoteId);
            while (emotes.Count > correctEmoteSequence.Length)
            {
                emotes.RemoveAt(0);
            }

            if (emotes.Count == correctEmoteSequence.Length && emotes.SequenceEqual(correctEmoteSequence))
            {
                befriendedNPCs[npcId] = true;
                npc.friendly = true;
                npc.damage = 0;
                npc.dontTakeDamage = true;
                affectionLevels[npcId] = 0;
                modPlayer.relationshipStatus = GirlfriendSystem.RelationshipStatus.Friendly;
                npc.netUpdate = true;

                if (Main.netMode != NetmodeID.Server)
                {
                    Main.NewText("The Lost Girl smiles at you shyly...", 255, 150, 150);
                }
                return true;
            }
            return false;
        }

        private void CheckForCookingPot(NPC npc)
        {
            if (!girlfriendNPCs.TryGetValue(npc.whoAmI, out bool isGirlfriend) || !isGirlfriend ||
                npc.homeTileX <= 0 || npc.homeTileY <= 0) return;

            bool foundCookingPot = false;
            const int checkRadius = 35;

            for (int x = npc.homeTileX - checkRadius; x <= npc.homeTileX + checkRadius && !foundCookingPot; x++)
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
            }

            bool previousCookingStatus = canCookNPCs.TryGetValue(npc.whoAmI, out bool canCook) && canCook;
            canCookNPCs[npc.whoAmI] = foundCookingPot;

            if (foundCookingPot && !previousCookingStatus && Main.netMode != NetmodeID.Server)
            {
                Main.NewText("Your girlfriend has learned to cook!", 255, 192, 203);
            }
        }

        private void StartCooking(int npcId)
        {
            ModContent.GetInstance<GirlfriendSystem>()?.StartCooking(npcId);
        }

        private bool IsCooking(int npcId)
        {
            return ModContent.GetInstance<GirlfriendSystem>()?.IsCooking(npcId) ?? false;
        }

        private int GetRemainingCookTimeInSeconds(int npcId)
        {
            return ModContent.GetInstance<GirlfriendSystem>()?.GetRemainingCookTime(npcId) / 60 ?? 0;
        }

        public override void Load()
        {
            if (!Main.dedServ)
            {
                friendlyGirlTexture = ModContent.Request<Texture2D>("GirlfriendMod/Content/NPCs/FriendlyGirl", AssetRequestMode.ImmediateLoad);
            }
        }

        public override void Unload()
        {
            befriendedNPCs.Clear();
            girlfriendNPCs.Clear();
            canCookNPCs.Clear();
            affectionLevels.Clear();
            receivedEmotes.Clear();
            friendlyGirlTexture = null;
        }

        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            if (npc.type == NPCID.LostGirl && (!befriendedNPCs.TryGetValue(npc.whoAmI, out bool befriended) || !befriended))
            {
                npc.Transform(NPCID.Nymph); // Transform to Nymph using NPC_195 sprite
            }
            // Friendly Girl does not transform if hit
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (npc.type == NPCID.LostGirl && (!befriendedNPCs.TryGetValue(npc.whoAmI, out bool befriended) || !befriended))
            {
                npc.Transform(NPCID.Nymph); // Transform to Nymph using NPC_195 sprite
            }
            // Friendly Girl does not transform if hit
        }
    }
}