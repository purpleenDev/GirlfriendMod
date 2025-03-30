using System.IO;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace GirlfriendMod
{
    // This class will handle the mod's game systems such as:
    // - Tracking which players have befriended the Lost Girl
    // - Managing relationship status
    // - Timer for cooking
    public class GirlfriendSystem : ModSystem
    {
        // Dictionary to store relationship status for each player
        // Key: Player.whoAmI, Value: RelationshipStatus enum
        private Dictionary<int, RelationshipStatus> playerRelationships;

        // Dictionary to track cooking timers
        // Key: NPC.whoAmI, Value: tick count remaining
        private Dictionary<int, int> cookingTimers;

        // Enum to track relationship progress
        public enum RelationshipStatus
        {
            None,           // Haven't met the Lost Girl yet
            Introduced,     // Found and didn't attack her
            Friendly,       // Successfully sent the right emote sequence
            Romantic,       // Had positive dialogue choices
            Girlfriend      // Given the diamond ring
        }

        public override void Load()
        {
            playerRelationships = new Dictionary<int, RelationshipStatus>();
            cookingTimers = new Dictionary<int, int>();
        }

        public override void SaveWorldData(TagCompound tag)
        {
            // Save cooking timers
            List<int> npcIds = new List<int>();
            List<int> timerValues = new List<int>();

            foreach (var pair in cookingTimers)
            {
                npcIds.Add(pair.Key);
                timerValues.Add(pair.Value);
            }

            tag["cookingNPCs"] = npcIds;
            tag["cookingTimers"] = timerValues;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            cookingTimers = new Dictionary<int, int>();

            if (tag.ContainsKey("cookingNPCs") && tag.ContainsKey("cookingTimers"))
            {
                List<int> npcIds = tag.Get<List<int>>("cookingNPCs");
                List<int> timerValues = tag.Get<List<int>>("cookingTimers");

                for (int i = 0; i < npcIds.Count; i++)
                {
                    if (i < timerValues.Count)
                    {
                        cookingTimers[npcIds[i]] = timerValues[i];
                    }
                }
            }
        }

        public override void SaveData(TagCompound tag)
        {
            // This saves per-player data
            // Called when a character saves
            if (Main.LocalPlayer != null && playerRelationships.ContainsKey(Main.LocalPlayer.whoAmI))
            {
                tag["relationshipStatus"] = (int)playerRelationships[Main.LocalPlayer.whoAmI];
            }
        }

        public override void LoadData(TagCompound tag)
        {
            // This loads per-player data
            // Called when a character loads
            if (tag.ContainsKey("relationshipStatus") && Main.LocalPlayer != null)
            {
                playerRelationships[Main.LocalPlayer.whoAmI] = (RelationshipStatus)tag.GetInt("relationshipStatus");
            }
        }

        public override void PostUpdateWorld()
        {
            // Update cooking timers
            List<int> finishedCooking = new List<int>();

            foreach (var pair in cookingTimers)
            {
                cookingTimers[pair.Key]--;

                if (cookingTimers[pair.Key] <= 0)
                {
                    finishedCooking.Add(pair.Key);
                    // Food is ready, notify the player
                    if (Main.LocalPlayer.active)
                    {
                        Main.NewText("Your girlfriend has finished cooking!", 255, 192, 203);
                    }
                }
            }

            // Remove finished timers
            foreach (int npcId in finishedCooking)
            {
                cookingTimers.Remove(npcId);
            }
        }

        // Methods for other parts of the mod to interact with this system

        public void SetRelationshipStatus(int playerId, RelationshipStatus status)
        {
            playerRelationships[playerId] = status;
        }

        public RelationshipStatus GetRelationshipStatus(int playerId)
        {
            if (playerRelationships.ContainsKey(playerId))
            {
                return playerRelationships[playerId];
            }
            return RelationshipStatus.None;
        }

        public void StartCooking(int npcId)
        {
            // 5 minutes = 300 seconds = 18000 ticks (60 ticks per second)
            cookingTimers[npcId] = 18000;
        }

        public bool IsCooking(int npcId)
        {
            return cookingTimers.ContainsKey(npcId);
        }

        public int GetRemainingCookTime(int npcId)
        {
            if (cookingTimers.ContainsKey(npcId))
            {
                return cookingTimers[npcId];
            }
            return 0;
        }
    }
}