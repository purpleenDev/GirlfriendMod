using Terraria.ModLoader;

namespace GirlfriendMod
{
    public class GirlfriendMod : Mod
    {
        // This is the main mod class that TModLoader will load
        public GirlfriendMod()
        {
            // Any initialization code can go here
        }

        public override void Load()
        {
            // Called when the mod loads
            // Register custom emote logic here (handled in EmoteHandler.cs)

            base.Load();
        }

        public override void Unload()
        {
            // Called when the mod unloads
            // Cleanup any static references here

            base.Unload();
        }
    }
}