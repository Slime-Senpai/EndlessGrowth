using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Reflection.Emit;
using System;
using UnityEngine;

namespace SlimeSenpai.EndlessGrowth
{
    public static class RTMadSkills_Patch_SkillRecordInterval_Patch
    {
        public static void Postfix(int __0, ref float __result)
        {
            if (__0 > 20 && __result == 0)
            {
                __result = -12f;
            }
        }
    }

    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.XpRequiredToLevelUpFrom))]
    public static class SkillRecord_XpRequiredToLevelUpFrom_Patch
    {
        // CURVE GOES BRRRRRRRRRRRRRRRRRRRRRRRRR until 100
        // (If you ever reach it you're a chad)
        // TODO Need a better way to do the curve so that it can scale to more than 100
        private static readonly SimpleCurve XpForInfiniteLevelUpCurve = new()
        {
            {
                new CurvePoint(0f, 1000f),
                true
            },
            {
                new CurvePoint(9f, 10000f),
                true
            },
            {
                new CurvePoint(19f, 30000f),
                true
            },
            {
                new CurvePoint(29f, 60000f),
                true
            },
            {
                new CurvePoint(39f, 100000f),
                true
            },
            {
                new CurvePoint(49f, 150000f),
                true
            },
            {
                new CurvePoint(59f, 220000f),
                true
            },
            {
                new CurvePoint(69f, 300000f),
                true
            },
            {
                new CurvePoint(79f, 400000f),
                true
            },
            {
                new CurvePoint(89f, 520000f),
                true
            },
            {
                new CurvePoint(99f, 650000f),
                true
            }
        };

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            var target = new CodeMatch[]
            {
                new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(SkillRecord), "XpForLevelUpCurve")),
            };

            // We find the moment where the curve is loaded into the stack
            matcher.MatchStartForward(target);

            // We replace the curve being loaded with our own, much wow
            matcher.SetInstruction(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(SkillRecord_XpRequiredToLevelUpFrom_Patch), nameof(XpForInfiniteLevelUpCurve))));

            return matcher.InstructionEnumeration();
        }
    }

    [HarmonyPatch(typeof(SkillRecord),  nameof(SkillRecord.Learn))]
    public static class SkillRecord_Learn_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            var targetFirstIF = new CodeMatch[]
            {
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(SkillRecord), nameof(SkillRecord.levelInt))),
                new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)20),
                new CodeMatch(OpCodes.Bne_Un),
                new CodeMatch(OpCodes.Ldarg_0)
            };

            var targetLevelIntPlusPlus = new CodeMatch[]
            {
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(SkillRecord), nameof(SkillRecord.levelInt))),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Add),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(SkillRecord), nameof(SkillRecord.levelInt)))
            };

            var targetSecondIf = new CodeMatch[]
            {
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(SkillRecord), nameof(SkillRecord.levelInt))),
                new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)20),
                new CodeMatch(OpCodes.Blt_S),
                new CodeMatch(OpCodes.Ldarg_0)
            };

            // We find the first if
            matcher.MatchEndForward(targetFirstIF);

            // This also will remove the second check of the if statement, but it's not a problem since we remove what's inside
            while (matcher.Opcode != OpCodes.Br)
            {
                matcher.RemoveInstruction();
            }

            // We find the part where the level is increased
            matcher.MatchEndForward(targetLevelIntPlusPlus);

            // We go after the Stfld
            matcher.Advance(1);

            // We add a call to CheckMaxLevel to make sure we always have the max level for the pawn
            matcher.Insert(new CodeInstruction[]
            {
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(SkillRecord), nameof(SkillRecord.levelInt))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CommonPatches), nameof(CommonPatches.CheckMaxLevel))),
            });

            // We find the second if
            matcher.MatchEndForward(targetSecondIf);

            // As long as we don't jump, we're in the if so we remove the instructions
            while (matcher.Opcode != OpCodes.Br_S)
            {
                matcher.RemoveInstruction();
            }

            // We don't want to jump too soon, so we also remove the Br_S
            matcher.RemoveInstruction();

            return matcher.InstructionEnumeration();
        }
    }

    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.LevelDescriptor), MethodType.Getter)]
    public static class SkillRecord_LevelDescriptor_Patch
    {
        // Finally something that's not a freaking Transpiler
        public static void Postfix(ref string __result, SkillRecord __instance)
        {
            // Get the level
            var level = __instance.GetLevelForUI(true);
            // If we can translate it then we do
            if (__result == "Unknown" && level > 20 && level < 100)
            {
                // Set the result
                __result = ("Skill" + level).Translate();
            }
        }
    }

    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.ExposeData))]
    public static class SkillRecord_ExposeData_Patch
    {
        
        public static void Postfix(SkillRecord __instance)
        {
            // Can't use Aptitude yet since it will break the cache, but the + 20 should make it work anyway
            CommonPatches.CheckMaxLevel(__instance.levelInt);
        }
    }

    public static class CommonPatches
    {
        public static int maxPawnLevel = 0;

        public static List<Bill> billLists = new();

        public static void CheckMaxLevel(int level)
        {
            if (level > maxPawnLevel)
            {
                // Store the old max
                var oldMax = GetMaxLevelForBill();

                // Set new max
                maxPawnLevel = level;

                foreach(Bill bill in billLists)
                {
                    // If the pawn was using the old max
                    if (bill.allowedSkillRange.max == oldMax)
                    {
                        // We make it use the new max
                        bill.allowedSkillRange.max = GetMaxLevelForBill();
                    }
                }
            }
        }

        public static int GetMaxLevelForBill()
        {
            // TODO Need a better range that (0, 100), maybe a setting
            // We'll make the max level in bills either 100 or the max of pawn skill + 20
            return Math.Max(100, maxPawnLevel + 20);
        }
        public static IEnumerable<CodeInstruction> ReplaceClampForMax(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            var targetMaxLevel = new CodeMatch[]
            {
                new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)20)
            };

            var targetClamp = new CodeMatch[]
            {
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Mathf), nameof(Mathf.Clamp), new Type[] { typeof(int), typeof(int), typeof(int) }))
            };

            // We find the moment where the number 20 is added to the stack
            matcher.MatchStartForward(targetMaxLevel);

            // We remove the instruction
            matcher.RemoveInstruction();

            matcher.MatchStartForward(targetClamp);

            matcher.SetInstruction(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), nameof(Mathf.Max), new Type[] { typeof(int), typeof(int) })));

            return matcher.InstructionEnumeration();
        }
    }

    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.Level), MethodType.Setter)]
    public static class SkillRecord_Level_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CommonPatches.ReplaceClampForMax(instructions);
        }
    }
    
    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.GetLevel))]
    public static class SkillRecord_GetLevel_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CommonPatches.ReplaceClampForMax(instructions);
        }
    }

    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.GetLevelForUI))]
    public static class SkillRecord_GetLevelForUI_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CommonPatches.ReplaceClampForMax(instructions);
        }
    }

    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.Interval))]
    public static class SkillRecord_Interval_Patch
    {
        public static void IntervalFix(SkillRecord instance, float mult)
        {
            if (instance.levelInt > 20)
            {
                instance.Learn(-12f * mult, false);
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            var targetSwitch = new CodeMatch[]
            {
                new CodeMatch(OpCodes.Switch)
            };

            // We find the switch case
            matcher.MatchEndForward(targetSwitch);

            // We advance by one to go after it (which is the default)
            matcher.Advance(1);

            // We insert a call to a new check function
            matcher.Insert(new CodeInstruction[]
            {
                // Add the current object
                new CodeInstruction(OpCodes.Ldarg_0),
                // Add the num local variable
                new CodeInstruction(OpCodes.Ldloc_0),
                // Call the function with both
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SkillRecord_Interval_Patch), nameof(IntervalFix)))
            });

            return matcher.InstructionEnumeration();
        }
    }

    [HarmonyPatch(typeof(QualityUtility), nameof(QualityUtility.GenerateQualityCreatedByPawn), new Type[] { typeof(int), typeof(bool) })]
    public static class QualityUtility_GenerateQualityCreatedByPawn_Patch
    {
        // TODO Need a better way to do the curve so that it can scale to more than 100
        private static readonly SimpleCurve QualityModifierCurve = new()
        {
            {
                new CurvePoint(0f, 0.7f),
                true
            },
            {
                new CurvePoint(2f, 1.5f),
                true
            },
            {
                new CurvePoint(3f, 1.8f),
                true
            },
            {
                new CurvePoint(4f, 2.0f),
                true
            },
            {
                new CurvePoint(8f, 2.8f),
                true
            },
            {
                new CurvePoint(12f, 3.4f),
                true
            },
            {
                new CurvePoint(20f, 4.2f),
                true
            },
            {
                new CurvePoint(30f, 5.0f),
                true
            },
            {
                new CurvePoint(40f, 5.7f),
                true
            },
            {
                new CurvePoint(50f, 6.0f),
                true
            },
            {
                new CurvePoint(100f, 10.0f),
                true
            }
        };

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            var targetSwitch = new CodeMatch[]
            {
                new CodeMatch(OpCodes.Ldarg_0), // We include the Ldarg_0 to prevent a problem with the stack
                new CodeMatch(OpCodes.Switch)
            };

            var targetNumber5 = new CodeMatch[]
            {
                new CodeMatch(OpCodes.Ldc_I4_5)
            };

            // We find the start of switch
            matcher.MatchStartForward(targetSwitch);

            // We then remove everything until the last case (4.2f)
            while (true)
            {
                var instruction = matcher.InstructionAt(0);

                if (instruction.opcode.Equals(OpCodes.Ldc_R4) && instruction.operand.Equals(4.2f))
                {
                    break;
                }
                
                matcher.RemoveInstruction();
            }

            // We continue to remove stuff until the end of the switch case
            while (!matcher.InstructionAt(0).opcode.Equals(OpCodes.Ldloc_0))
            {
                matcher.RemoveInstruction();
            }

            // We add a new way to get the modifier from the skill level using a custom curve
            matcher.Insert(new CodeInstruction[]
            {
                // We load the curve onto the stack
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(QualityUtility_GenerateQualityCreatedByPawn_Patch), nameof(QualityModifierCurve))),
                // We load the skill level onto the stack
                new CodeInstruction(OpCodes.Ldarg_0),
                // We convert it to a float
                new CodeInstruction(OpCodes.Conv_R4),
                // We execute the Evaluate
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(SimpleCurve), nameof(SimpleCurve.Evaluate))),
                // We store the value into the variable
                new CodeInstruction(OpCodes.Stloc_0)
            });

            // We find the number 5 of the Math.Clamp and we replace it with 6 to allow legendary items
            matcher.MatchStartForward(targetNumber5);
            matcher.SetInstruction(new CodeInstruction(OpCodes.Ldc_I4_6));

            // This number 5 is to check if the quality is masterclass, we'll also replace it with legendary, it will cause more masterclass to appear but less legendary
            matcher.MatchStartForward(targetNumber5);
            matcher.SetInstruction(new CodeInstruction(OpCodes.Ldc_I4_6));

            // Another Math.Clamp here to change to 6
            matcher.MatchStartForward(targetNumber5);
            matcher.SetInstruction(new CodeInstruction(OpCodes.Ldc_I4_6));

            return matcher.InstructionEnumeration();
        }
    }

    [HarmonyPatch(typeof(Bill), MethodType.Constructor, new Type[] {})]
    public static class Bill_EmptyConstructor_Patch
    {
        public static void Postfix(Bill __instance)
        {
            // Make the max skill range for the bill to 0, and a max level calculated from levels of pawns
            __instance.allowedSkillRange = new(0, CommonPatches.GetMaxLevelForBill());

            // We also store the bill for later usage
            CommonPatches.billLists.Add(__instance);
        }
    }

    [HarmonyPatch(typeof(Bill), MethodType.Constructor, new Type[] { typeof(RecipeDef), typeof(Precept_ThingStyle) })]
    public static class Bill_ConstructorWithArguments_Patch
    {
        // TODO Maybe merge the patch with the one above, never used a patch for two methods so meh
        public static void Postfix(Bill __instance)
        {
            // Make the max skill range for the bill to 0, and a max level calculated from levels of pawns
            __instance.allowedSkillRange = new(0, CommonPatches.GetMaxLevelForBill());

            // We also store the bill for later usage
            CommonPatches.billLists.Add(__instance);
        }
    }

    [HarmonyPatch(typeof(Dialog_BillConfig), "DoWindowContents")]
    public static class Dialog_BillConfig_DoWindowContents_Patch
    {

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            var targetMaxRange = new CodeMatch[]
            {
                new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)20)
            };

            // We find the number 20 that needs replacing (the only number 20 in the function)
            matcher.MatchStartForward(targetMaxRange);

            // We remove the instruction
            matcher.RemoveInstruction();

            // We replace it to a call to our custom field
            matcher.Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CommonPatches), nameof(CommonPatches.GetMaxLevelForBill))));

            return matcher.InstructionEnumeration();
        }
    }
}
