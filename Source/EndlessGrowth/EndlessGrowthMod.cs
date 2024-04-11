using Verse;
using HarmonyLib;
using System;
using UnityEngine;

namespace SlimeSenpai.EndlessGrowth
{
    public class EndlessGrowthMod : Mod
    {

        public static EndlessGrowthModSettings settings;

        public EndlessGrowthMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<EndlessGrowthModSettings>();

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

            // MAGIC Another funny patch for the Perfectionist of VanillaTraitsExpanded
            ((Action)(() =>
            {
                if (LoadedModManager.RunningModsListForReading.Any(x => x.Name == "Vanilla Traits Expanded"))
                {
                    harmony.Patch(AccessTools.Method(AccessTools.TypeByName("VanillaTraitsExpanded.GenerateQualityCreatedByPawn_Patch"), "Postfix"),
                        transpiler: new HarmonyMethod(typeof(VanillaTraitsExpanded_GenerateQualityCreatedByPawn_Patch_Patch),
                                                      nameof(VanillaTraitsExpanded_GenerateQualityCreatedByPawn_Patch_Patch.Transpiler)));
                }
            }))();
        }

        public override string SettingsCategory() => "EndlessGrowthModSettingsCategoryLabel".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new();
            listingStandard.Begin(inRect);

            listingStandard.Label("EndlessGrowth_pleaseRestart".Translate());

            listingStandard.Gap(24);

            listingStandard.TextFieldNumericLabeled("EndlessGrowth_maxLevelExplanation".Translate() + " ", ref settings.maxLevel, ref settings.maxLevelString, -1);
            
            listingStandard.Gap(24);

            listingStandard.CheckboxLabeled("EndlessGrowth_unlimitedPriceExplanation".Translate() + " ", ref settings.unlimitedPrice);
            
            listingStandard.Gap(24);

            // On a new line because LOOOOOOONG text
            listingStandard.Label("EndlessGrowth_gapForMaxBillExplanation".Translate());
            listingStandard.TextFieldNumericLabeled("", ref settings.gapForMaxBill, ref settings.gapForMaxBillString, 20);
            
            listingStandard.Gap(24);

            listingStandard.CheckboxLabeled("EndlessGrowth_craftNotificationsEnabledExplanation".Translate() + " ", ref settings.craftNotificationsEnabled);

            listingStandard.End();

            base.DoSettingsWindowContents(inRect);
        }
    }
}
