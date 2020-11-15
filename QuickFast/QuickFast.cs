﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace QuickFast
{
    public class Settings : ModSettings
    {
        private static HashSet<HairDef> _hairfilter;

        public static HashSet<HairDef> hairfilter
        {
            get
            {
                if (_hairfilter == null)
                {
                    _hairfilter = new HashSet<HairDef>();
                    if (DefToStrings == null)
                    {
                        DefToStrings = new List<string>();
                    }
                    foreach (var defToString in DefToStrings)
                    {
                        var foo = DefDatabase<HairDef>.GetNamed(defToString);
                        if (foo != null)
                        {
                            _hairfilter.Add(foo);
                        }
                    }
                }

                return _hairfilter;
            }
        }
        public static float hairScale = 1.7f;
        public static float hairScaleNarrow = 1.4f;
        public static float EquipModPC = 0.2f;
        public static int EquipModTicks = 10;
        public static bool FlatRate = true;
        public static bool HatsSleeping = true;
        public static bool HideHats = true;
        public static bool HideJackets = true;
        public static bool HatsOnlyWhileDrafted = false;
        public static bool HideHairUnderHats = true;
        public static bool ChangeEquipSpeed = true;
        private string buf;
        private Listing_Standard lis;

        public void DoWindowContents(Rect canvas)
        {
            Rect nifta = canvas;
            lis = new Listing_Standard();
            lis.ColumnWidth = (nifta.width - 40f) / 2f;

            lis.Begin(canvas);

            lis.Label("Apparel equip speed");
            lis.CheckboxLabeled("Change equip speeds", ref ChangeEquipSpeed);
            if (ChangeEquipSpeed)
            {
                lis.CheckboxLabeled("Same speed for all apparel", ref FlatRate);

                if (FlatRate)
                {
                    lis.LabelDouble("Equip speed Ticks", $"{EquipModTicks} ticks");
                    lis.IntEntry(ref EquipModTicks, ref buf);
                }
                else
                {
                    lis.LabelDouble("Equip duration %", $"{EquipModPC.ToStringPercent()}");
                    EquipModPC = lis.Slider(EquipModPC, 0, 1f);
                }
            }

            lis.GapLine();
            lis.Label("Apparel visibility");
            lis.CheckboxLabeled("Hide hats when sleeping", ref HatsSleeping);
            lis.CheckboxLabeled("Hide hats when indoors", ref HideHats);
            lis.CheckboxLabeled("Hide jackets when indoors", ref HideJackets);
            lis.CheckboxLabeled("Hats only while drafted", ref HatsOnlyWhileDrafted);
            lis.CheckboxLabeled("Hide hair under hats", ref HideHairUnderHats);


            lis.Label("Hat Scaling");
            lis.LabelDouble("Normal + Narrow Height", $"{hairScale}");
            var tamw = decimal.Round((decimal)lis.Slider(hairScale, 1f, 2f), 2);
            if (tamw != (decimal)hairScale)
            {
                hairScale = (float)tamw;
                bs.hairScale_Changed();
            }
            lis.LabelDouble("Narrow Width", $"{hairScaleNarrow}");
            tamw = decimal.Round((decimal)lis.Slider(hairScaleNarrow, 1f, 2f), 2);
            if (tamw != (decimal)hairScaleNarrow)
            {
                hairScaleNarrow = (float)tamw;
                bs.hairScale_Changed();
            }

            if (lis.ButtonText("Reset"))
            {
                hairScaleNarrow = 1.4f;
                hairScale = 1.7f;
                bs.hairScale_Changed();
            }


            GUI.color = Color.green;
            lis.Label("Press Ctrl + H while pawns are selected to show or hide their hairstyle under hats");
            GUI.color = Color.white;
            lis.GapLine();



            lis.End();
        }

        public static List<string> DefToStrings = new List<string>();
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref hairScaleNarrow, "hairScaleNarrow", 1.4f);
            Scribe_Values.Look(ref hairScale, "hairScale", 1.7f);
            Scribe_Values.Look(ref ChangeEquipSpeed, "ChangeEquipSpeed");
            Scribe_Values.Look(ref HatsOnlyWhileDrafted, "HatsOnlyWhileDrafted");
            Scribe_Values.Look(ref HideHairUnderHats, "HideHairUnderHats");
            Scribe_Values.Look(ref FlatRate, "FlatRate");
            Scribe_Values.Look(ref HideHats, "HatsIndoors");
            Scribe_Values.Look(ref HatsSleeping, "HatsSleeping");
            Scribe_Values.Look(ref EquipModPC, "EquipModPC", 0.2f);
            Scribe_Values.Look(ref EquipModTicks, "EquipModTicks", 10);
            Scribe_Values.Look(ref HideJackets, "HideJackets");
            Scribe_Collections.Look(ref DefToStrings, "hairFilter", LookMode.Value);
        }
    }


    [HarmonyPatch(typeof(UIRoot_Play))]
    [HarmonyPatch(nameof(UIRoot_Play.UIRootUpdate))]
    public static class h_UIRootOnGUI
    {
        public static void Postfix()
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.H))
            {
                foreach (var pawn in Find.Selector.SelectedObjects.OfType<Pawn>())
                {
                    if (Settings.hairfilter.Contains(pawn.story.hairDef))
                    {
                        Settings.hairfilter.Remove(pawn.story.hairDef);
                        Log.Warning($"Removed {pawn.story.hairDef.defName} from hair filter");
                    }
                    else
                    {
                        Settings.hairfilter.Add(pawn.story.hairDef);
                        Log.Warning($"Added {pawn.story.hairDef.defName} to hair filter");
                    }
                    bs.PatherCheck(pawn, pawn.Position, pawn.Position, true);
                }
                Settings.DefToStrings = new List<string>();
                foreach (var s in Settings.hairfilter)
                {
                    Settings.DefToStrings.Add(s.defName);
                }
                QuickFast.Settings.Write();
            }
        }
    }


    public class QuickFast : Mod
    {
        public QuickFast(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("QuickFast");
            harmony.PatchAll();

            Settings = base.GetSettings<Settings>();

            MethodInfo b__0 = null;
            MethodInfo b__2 = null;

            foreach (var type in GenTypes.AllTypes)
            {
                foreach (var methodInfo in type.GetMethods(AccessTools.all))
                {
                    if (methodInfo.Name.Contains("<LayDown>b__0"))
                    {
                        b__0 = methodInfo;
                    }

                    if (methodInfo.Name.Contains("<LayDown>b__2"))
                    {
                        b__2 = methodInfo;
                    }

                    if (b__0 != null && b__2 != null)
                    {
                        break;
                    }
                }
            }

            if (b__0 != null && b__2 != null)
            {
                var Prefix = new HarmonyMethod(typeof(QuickFast).GetMethod("Prefix_0"));
                harmony.Patch(b__0, Prefix);

                Prefix = new HarmonyMethod(typeof(QuickFast).GetMethod("Prefix_2"));
                harmony.Patch(b__2, Prefix);
            }
        }

        public static Settings Settings;

        public override void DoSettingsWindowContents(Rect canvas)
        {
            Settings.DoWindowContents(canvas);
        }

        public override string SettingsCategory()
        {
            return "Quick Fast";
        }

        public static void Prefix_0(object __instance)
        {
            if (!Settings.HatsSleeping)
            {
                return;
            }

            var toil = AccessTools.Field(__instance.GetType(), "layDown").GetValue(__instance) as Toil;
            var bed = toil.actor.CurrentBed();
            if (bed != null && toil.actor.RaceProps.Humanlike && !bed.def.building.bed_showSleeperBody)
            {
                toil.actor.Drawer.renderer.graphics.ClearCache();
                toil.actor.Drawer.renderer.graphics.apparelGraphics.Clear();
            }
        }

        public static void Prefix_2(object __instance)
        {
            if (!Settings.HatsSleeping)
            {
                return;
            }

            var toil = AccessTools.Field(__instance.GetType(), "layDown").GetValue(__instance) as Toil;

            if (toil.actor.RaceProps.Humanlike)
            {
                toil.actor.Drawer.renderer.graphics.ResolveApparelGraphics();
            }
        }
    }

    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnInternal), typeof(Vector3), typeof(float), typeof(bool), typeof(Rot4), typeof(Rot4), typeof(RotDrawMode), typeof(bool), typeof(bool), typeof(bool))]
    public static class Patch_RenderPawnInternal
    {

        public static Vector3 offset(Vector3 vec)
        {
            if (!Settings.HideHairUnderHats)
            {
                vec.y += -0.0036f;
            }
            return vec;
        }

        public static FieldInfo HideHairUnderHats = AccessTools.Field(typeof(Settings), "HideHairUnderHats");

        public static MethodInfo m_lorian = AccessTools.Method(typeof(bs), "lorian");

        public static MethodInfo m_offset = AccessTools.Method(typeof(Patch_RenderPawnInternal), "offset");

        public static MethodInfo m_get_HairMeshSet = AccessTools.Method(typeof(PawnGraphicSet), "get_HairMeshSet");

        public static MethodInfo m_MeshAt = AccessTools.Method(typeof(GraphicMeshSet), nameof(GraphicMeshSet.MeshAt));

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var found = false;
            var foudnb = false;
            var foundcal = false;
            var struc = instructions.ToList();
            for (var index = 0; index < struc.Count; index++)
            {
                var instruction = struc[index];
                if (foundcal is false && instruction.Calls(m_MeshAt) && struc[index - 2].Calls(m_get_HairMeshSet))
                {
                    foundcal = true;
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Stloc_S, (byte)15);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)5);
                    yield return new CodeInstruction(OpCodes.Call, m_lorian);
                }
                else
                if (foudnb is false && instruction.opcode == OpCodes.Ldloc_S && struc[index + 1].opcode == OpCodes.Brtrue_S && struc[index + 2].opcode == OpCodes.Ldarg_S)
                {
                    foudnb = true;
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Ldloc_S, (byte)13);
                    yield return new CodeInstruction(OpCodes.Call, m_offset);
                    yield return new CodeInstruction(OpCodes.Stloc_S, (byte)13);
                }
                else
                if (found is false && instruction.opcode == OpCodes.Ldc_I4_1 && struc[index + 1].opcode == OpCodes.Stloc_S && struc[index - 1].opcode == OpCodes.Brtrue_S)
                {
                    yield return new CodeInstruction(OpCodes.Ldsfld, HideHairUnderHats);
                    found = true;
                }
                else
                {
                    yield return instruction;
                }
            }
            if (foundcal is false)
            {
                Log.Error("Couldn't find get_HairMeshSet");
            }
            if (found is false)
            {
                Log.Error("Couldn't find Ldc_I4_1");
            }
            if (foudnb is false)
            {
                Log.Error("Couldn't find HairMatAt_NewTemp");
            }
        }
    }

    [StaticConstructorOnStartup]
    public static class bs
    {
        public static GraphicMeshSet biggerhair;
        public static GraphicMeshSet biggernarrowhair;
        public static Graphic bald = GraphicDatabase.Get<Graphic_Multi>("Things/Pawn/Humanlike/Hairs/Shaved", ShaderDatabase.Transparent, Vector2.one, Color.clear);


        public static void hairScale_Changed()
        {
            biggerhair = null;
            biggernarrowhair = null;
        }

        public static Mesh lorian(PawnRenderer pr, Rot4 rot)
        {
            if (pr.pawn.story.crownType == CrownType.Average)
            {
                if (biggerhair == null)
                {
                    biggerhair = new GraphicMeshSet(Settings.hairScale);
                }
                return biggerhair.MeshAt(rot);
            }
            if (pr.pawn.story.crownType == CrownType.Narrow)
            {
                if (biggernarrowhair == null)
                {
                    biggernarrowhair = new GraphicMeshSet(Settings.hairScaleNarrow, Settings.hairScale);
                }
                return biggernarrowhair.MeshAt(rot);
            }
            return biggerhair.MeshAt(rot);
        }

        public static bool ShouldShowHats(Pawn pawn) => Settings.HideHats is false && (Settings.HatsOnlyWhileDrafted is false || pawn.Drafted is false);

        public static void SwitchIndoors(Pawn pawn)
        {
            var graphics = pawn?.Drawer?.renderer?.graphics;
            if (graphics == null) return;
            if (UnityData.IsInMainThread is false) return;

            if (Settings.HideJackets)
            {
                if (graphics.apparelGraphics.Any(x => x.sourceApparel.def.apparel.LastLayer == ApparelLayerDefOf.OnSkin))
                {
                    graphics.apparelGraphics.RemoveAll(x => x.sourceApparel.def.apparel.LastLayer == ApparelLayerDefOf.Shell);
                }
            }

            if (graphics.hairGraphic == bald)
            {
                graphics.hairGraphic = GraphicDatabase.Get<Graphic_Multi>(pawn.story.hairDef.texPath, ShaderDatabase.Transparent, Vector2.one, pawn.story.hairColor);
            }

            if (Settings.HideHats is true || (Settings.HatsOnlyWhileDrafted is true && pawn.Drafted is false))
            {
                var hidden = graphics.apparelGraphics.RemoveAll(x => x.sourceApparel.def.apparel.LastLayer == ApparelLayerDefOf.Overhead);

                if (graphics.hairGraphic == bald)
                {
                    graphics.hairGraphic = GraphicDatabase.Get<Graphic_Multi>(pawn.story.hairDef.texPath, ShaderDatabase.Transparent, Vector2.one, pawn.story.hairColor);
                }
            }
            else
            {
                if (Settings.HideHairUnderHats)
                {
                    if (graphics.hairGraphic == bald)
                    {
                        graphics.hairGraphic = GraphicDatabase.Get<Graphic_Multi>(pawn.story.hairDef.texPath, ShaderDatabase.Transparent, Vector2.one, pawn.story.hairColor);
                    }
                }
                else
                {
                    if (graphics.apparelGraphics.Any(x => x.sourceApparel.def.apparel.LastLayer == ApparelLayerDefOf.Overhead))
                    {
                        if (Settings.hairfilter.Contains(pawn.story.hairDef))
                        {
                            graphics.hairGraphic = bald;
                        }
                    }
                }
            }
        }

        public static void SwitchOutdoors(Pawn pawn)
        {
            var graphics = pawn?.Drawer?.renderer?.graphics;
            if (graphics == null)
            {
                return;
            }

            graphics.ClearCache();
            graphics.apparelGraphics.Clear();
            using (var enumerator = graphics.pawn.apparel.WornApparel.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (ApparelGraphicRecordGetter.TryGetGraphicApparel(enumerator.Current, graphics.pawn.story.bodyType, out var item))
                    {
                        graphics.apparelGraphics.Add(item);
                    }
                }
            }

            if (Settings.HideHairUnderHats)
            {
                if (graphics.hairGraphic == bald)
                {
                    graphics.hairGraphic = GraphicDatabase.Get<Graphic_Multi>(pawn.story.hairDef.texPath, ShaderDatabase.Transparent, Vector2.one, pawn.story.hairColor);
                }
            }
            else
            {
                if (pawn.story?.hairDef != null)
                {
                    if (graphics.apparelGraphics.Any(x => x.sourceApparel.def.apparel.LastLayer == ApparelLayerDefOf.Overhead))
                    {
                        if (Settings.hairfilter.Contains(pawn.story.hairDef))
                        {
                            graphics.hairGraphic = bald;
                        }
                    }
                }
            }
        }

        public static void PatherCheck(Pawn pawn, IntVec3 nextCell, IntVec3 lastCell, bool startpath)
        {
            var map = pawn.MapHeld;

            if (ShouldRun(pawn, nextCell, lastCell, map) is false)
            {
                return;
            }

            if (startpath)
            {
                if (nextCell.UsesOutdoorTemperature(pawn.MapHeld))
                {
                    SwitchOutdoors(pawn);
                }
                else
                {
                    SwitchIndoors(pawn);
                }
                return;
            }

            StartPatchCheck(pawn, nextCell);

            cunt(pawn, nextCell, lastCell, map);
        }

        public static bool ShouldRun(Pawn pawn, IntVec3 nextCell, IntVec3 lastCell, Map map)
        {
            //   if (Settings.HideHats is false && Settings.HideJackets is false) return true;

            if (UnityData.IsInMainThread is false) return false;

            if (pawn.AnimalOrWildMan()) return false;

            if (map == null) return false;

            if (!nextCell.InBounds(map) || !lastCell.InBounds(map)) return false;

            return true;
        }

        public static void StartPatchCheck(Pawn pawn, IntVec3 nextCell)
        {
            if (nextCell.UsesOutdoorTemperature(pawn.MapHeld))
            {
                SwitchOutdoors(pawn);
            }
            else
            {
                SwitchIndoors(pawn);
            }
        }

        public static void cunt(Pawn pawn, IntVec3 nextCell, IntVec3 lastCell, Map map)
        {
            var last = lastCell.UsesOutdoorTemperature(map);
            var next = nextCell.UsesOutdoorTemperature(map);

            if (last && !next)
            {
                SwitchIndoors(pawn);
            }

            if (!last && next)
            {
                SwitchOutdoors(pawn);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_DraftController))]
    [HarmonyPatch(nameof(Pawn_DraftController.Drafted), MethodType.Setter)]
    public static class H_Drafted
    {
        public static void Postfix(Pawn_DraftController __instance)
        {
            if (__instance.draftedInt || __instance.pawn.Position.UsesOutdoorTemperature(__instance.pawn.MapHeld))
            {
                bs.SwitchOutdoors(__instance.pawn);
            }
            else
            {
                bs.SwitchIndoors(__instance.pawn);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.StartPath))]
    public static class H_StartPath
    {
        public static void Postfix(Pawn_PathFollower __instance) => bs.PatherCheck(__instance.pawn, __instance.nextCell, __instance.lastCell, true);
    }

    [HarmonyPatch(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.TryEnterNextPathCell))]
    [StaticConstructorOnStartup]
    public static class H_TryEnterNextPathCell
    {
        public static void Postfix(Pawn_PathFollower __instance) => bs.PatherCheck(__instance.pawn, __instance.nextCell, __instance.lastCell, false);
    }

    [HarmonyPatch(typeof(JobDriver_Wear), nameof(JobDriver_Wear.Notify_Starting))]
    public static class h_JobDriver_Wear
    {
        public static void Postfix(JobDriver_Wear __instance)
        {
            if (Settings.FlatRate)
            {
                __instance.duration = Settings.EquipModTicks;
            }
            else
            {
                __instance.duration = (int)(__instance.duration * Settings.EquipModPC);
            }
        }
    }


    [HarmonyPatch(typeof(JobDriver_RemoveApparel), nameof(JobDriver_RemoveApparel.Notify_Starting))]
    public static class h_JobDriver_RemoveApparel
    {
        public static void Postfix(JobDriver_Wear __instance)
        {
            if (Settings.FlatRate)
            {
                __instance.duration = Settings.EquipModTicks;
            }
            else
            {
                __instance.duration = (int)(__instance.duration * Settings.EquipModPC);
            }
        }
    }
}
