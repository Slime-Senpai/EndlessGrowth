using Verse;
using HarmonyLib;
using System;

namespace SlimeSenpai.EndlessGrowth
{
    public class EndlessGrowthMod : Mod
    {
        public EndlessGrowthMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("fr.slimesenpai.rimworld.endlessgrowth");
            harmony.PatchAll();

            // MAGIC Who doesn't like some good old magic to patch. Also Mad Skills creator I hate you and your Prefix with return false <3
            ((Action)(() =>
            {
                if (LoadedModManager.RunningModsListForReading.Any(x => x.Name == "Mad Skills"))
                {
                    harmony.Patch(AccessTools.Method(AccessTools.TypeByName("RTMadSkills.Patch_SkillRecordInterval"), "VanillaMultiplier"),
                        postfix: new HarmonyMethod(typeof(RTMadSkills_Patch_SkillRecordInterval_Patch), nameof(RTMadSkills_Patch_SkillRecordInterval_Patch.Postfix)));
                }
            }))();
        }
    }
}
