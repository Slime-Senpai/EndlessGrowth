using Verse;
using HarmonyLib;

namespace SlimeSenpai.EndlessGrowth
{
    public class EndlessGrowthMod : Mod
    {
        public EndlessGrowthMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("fr.slimesenpai.rimworld.endlessgrowth");
            harmony.PatchAll();
        }
    }
}
