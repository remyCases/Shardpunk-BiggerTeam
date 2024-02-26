// Copyright (C) 2024 Rémy Cases
// See LICENSE file for extended copyright information.
// This file is part of the Speedshard repository from https://github.com/remyCases/Shardpunk-BiggerTeam.

using BepInEx;
using HarmonyLib;
using Assets.Scripts.GameUI.Tactical;
using Assets.Scripts.GameUI;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.GameUI.MainMenu;
using System.Reflection.Emit;
using HarmonyLib.Tools;
using UnityEngine.UI;

namespace BiggerTeam;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        // Plugin startup logic
        HarmonyFileLog.Enabled = true;
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        Harmony harmony = new(PluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
    }
}
   
[HarmonyPatch]
public static class BiggerTeamPatch
{
    // add 2 more slots for the main ui panel
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ActiveCharacterPickersListBehaviour), "Awake")]
    static bool ActiveCharacterPickersListBehaviourAwake(ActiveCharacterPickersListBehaviour __instance)
    {
        GameObject pickerPrefab = GameObject.Find("CharacterPicker (1)");
        Object.Instantiate(pickerPrefab, __instance.transform).transform.SetSiblingIndex(6);
        Object.Instantiate(pickerPrefab, __instance.transform).transform.SetSiblingIndex(7);

        return true;
    }

    // add 2 more slots for the character details panel
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CharacterDetailsCharacterPickersListBehaviour), "Awake")]
    static bool CharacterDetailsCharacterPickersListBehaviourAwake(CharacterDetailsCharacterPickersListBehaviour __instance)
    {
        GameObject pickerPrefab = GameObject.Find("CharacterPicker (1)");
        Object.Instantiate(pickerPrefab, __instance.transform).transform.SetSiblingIndex(6);
        Object.Instantiate(pickerPrefab, __instance.transform).transform.SetSiblingIndex(7);

        return true;
    }

    // add 2 more slots for the mission summary panel
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MissionSummaryModalBehaviour), "Awake")]
    static bool MissionSummaryModalBehaviourAwake(MissionSummaryModalBehaviour __instance)
    {
        GameObject pickerPrefab = GameObject.Find("MissionSummaryCharacterInfo");
        Object.Instantiate(pickerPrefab, __instance.transform.GetChild(0).GetChild(1).GetChild(3)).transform.SetSiblingIndex(6);
        Object.Instantiate(pickerPrefab, __instance.transform.GetChild(0).GetChild(1).GetChild(3)).transform.SetSiblingIndex(7);
        return true;
    }

    // For the mission summary panel
    // resizing UI for up to 8 units
    // since you can end a mission with 8 units and it will push the ui out of screen
    static readonly float resizingVal = 20f;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MissionSummaryCharacterInfo), "Awake")]
    static void MissionSummaryCharacterInfoAwake(MissionSummaryCharacterInfo __instance)
    {
        Vector2 resizingVec = new(resizingVal, 0.0f);

        // overall box
        __instance.GetComponent<RectTransform>().sizeDelta -= resizingVec;

        // bond display
        __instance.transform.GetChild(0).GetChild(0).GetComponent<RectTransform>().sizeDelta -= resizingVec;

        // button roll quirk
        __instance.transform.GetChild(0).GetChild(5).GetChild(0).GetComponent<RectTransform>().sizeDelta -= resizingVec;

        // dread progress bar
        __instance.transform.GetChild(0).GetChild(5).GetChild(3).GetComponent<RectTransform>().sizeDelta -= resizingVec;

        // stress gained text
        __instance.transform.GetChild(0).GetChild(5).GetChild(2).GetComponent<RectTransform>().sizeDelta -= resizingVec;

        // total xp text
        __instance.transform.GetChild(0).GetChild(4).GetChild(0).GetComponent<RectTransform>().sizeDelta -= resizingVec;

        // survived mission text
        __instance.transform.GetChild(0).GetChild(4).GetChild(1).GetChild(0).GetComponent<RectTransform>().sizeDelta -= resizingVec;
        
        // top kills text
        __instance.transform.GetChild(0).GetChild(4).GetChild(1).GetChild(1).GetComponent<RectTransform>().sizeDelta -= resizingVec;
        
        // top pickups text
        __instance.transform.GetChild(0).GetChild(4).GetChild(1).GetChild(2).GetComponent<RectTransform>().sizeDelta -= resizingVec;

        // xp progress bar
        __instance.transform.GetChild(0).GetChild(4).GetChild(2).GetComponent<RectTransform>().sizeDelta -= resizingVec;

        // button level up
        __instance.transform.GetChild(1).GetComponent<RectTransform>().sizeDelta -= resizingVec;
        
        // blinking level up
        __instance.transform.GetChild(2).GetComponent<RectTransform>().sizeDelta -= resizingVec;
        
        // blinking quirk roll
        __instance.transform.GetChild(3).GetComponent<RectTransform>().sizeDelta -= resizingVec;
    }
    
    // For the mission summary panel
    // resizing UI for up to 8 units
    // since you can end a mission with 8 units and it will push the ui out of screen
    // and the box is already resize during runtime
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(MissionSummaryCharacterInfo), "Initialize")]
    static IEnumerable<CodeInstruction> MissionSummaryCharacterInfoInitialize(IEnumerable<CodeInstruction> instructions)
    {
        foreach(CodeInstruction instruction in instructions)
        {
            if (instruction.Is(OpCodes.Ldc_R4, 280f))
            {
                yield return new CodeInstruction(OpCodes.Ldc_R4, (float)instruction.operand - resizingVal);
            }
            else
            {
                yield return instruction;
            }
        }
    }

    // max size from 4 to 6 for the selection of max size while starting a new game
    [HarmonyPostfix]
    [HarmonyPatch(typeof(MaxPartySizeDropdownDisplayBehaviour), "GetAvailableValues")]
    static int[] GetAvailableValues(int[] __result)
    {
        List<int> list = new(__result)
        {
            5,
            6
        };
        return list.ToArray();
    }

    // change the constuctor so we allow 4 human units to be selected at start
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PartySelectionUIState), MethodType.Constructor)]
    static IEnumerable<CodeInstruction> PartySelectionUIStateCtor(IEnumerable<CodeInstruction> instructions)
    {
        int status = 0;
        foreach(CodeInstruction instruction in instructions)
        {
            // find the 2 lines before
            if((instruction.Is(OpCodes.Stfld, AccessTools.Field(typeof(PartySelectionUIState), "_startingPartySize")) && status == 0) ||
            (instruction.opcode == OpCodes.Ldarg_0 && status == 1))
            {
                status++;
                yield return instruction;
            }
            // change 4 by a 5
            else if (instruction.opcode == OpCodes.Ldc_I4_4 && status == 2)
            {
                status++;
                yield return new CodeInstruction(OpCodes.Ldc_I4_5);
            }
            else
            {
                yield return instruction;
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PartySelectionCurrentPartyDisplayBehaviour), "Awake")]
    static bool PartySelectionCurrentPartyDisplayBehaviourAwake(PartySelectionCurrentPartyDisplayBehaviour __instance)
    {
        GameObject pickerPrefabDisplay = GameObject.Find("MainMenuCharacterPicker");
        GameObject startingDisplay4 = Object.Instantiate(pickerPrefabDisplay, __instance.transform);
        startingDisplay4.transform.SetSiblingIndex(5);
        startingDisplay4.name = "MainMenuCharacterPicker (3)";
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(DifficultySelectionModalDisplayBehaviour), "Awake")]
    static bool DifficultySelectionModalDisplayBehaviourAwake(DifficultySelectionModalDisplayBehaviour __instance)
    {
        FieldInfo availablePartySizes = AccessTools.Field(typeof(StartingPartySizeDropdownDisplayBehaviour), "AvailablePartySizes");
        availablePartySizes.SetValue(__instance.StartingPartySizeDropdown, new int[] { 
            1,
			2,
			3,
            4
        });

        // resize ui
        Transform startingRootTransform = __instance.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(0);
        startingRootTransform.GetComponent<RectTransform>().sizeDelta += new Vector2(121f, 0f);

        // add new box
        GameObject pickerPrefabImage = GameObject.Find("StartingCharacterImage");
        GameObject startingImage4 = Object.Instantiate(pickerPrefabImage, startingRootTransform);
        startingImage4.transform.SetSiblingIndex(3); // before the bot
        startingImage4.name = "StartingCharacterImage.4";

        // fill the character display list
        MethodInfo setElementDisplays = AccessTools.Method(typeof(GenericListDisplayBehavior<CharacterImageDisplay, string>), "set_ElementDisplays");
        setElementDisplays.Invoke(__instance.StartingCharactersImagesList, new object[] { __instance.transform.GetComponentsInChildren<CharacterImageDisplay>() });
        return true;
    }
    
    // add 2 more slots for the shelter panel
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShelterCharactersListDisplayBehaviour), "Awake")]
    static bool ShelterCharactersListDisplayBehaviourAwakePrefix(ShelterCharactersListDisplayBehaviour __instance)
    {
        GameObject pickerPrefab1 = GameObject.Find("IdleCharacterSprite.3");
        GameObject gameObject1 = Object.Instantiate(pickerPrefab1, __instance.transform.GetChild(0));
        gameObject1.transform.SetSiblingIndex(4);
        gameObject1.transform.localPosition = pickerPrefab1.transform.localPosition + new Vector3(150f, -20f, 0f);

        GameObject pickerPrefab2 = GameObject.Find("IdleCharacterSprite.4");
        GameObject gameObject2 = Object.Instantiate(pickerPrefab2, __instance.transform.GetChild(0));
        gameObject2.transform.SetSiblingIndex(5);
        gameObject2.transform.localPosition = pickerPrefab2.transform.localPosition + new Vector3(-100f, 40f, 0f);
        return true;
    }

    // change the list of slots for the shelter panel
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShelterCharactersListDisplayBehaviour), "Awake")]
    static void ShelterCharactersListDisplayBehaviourAwakePostfix(ShelterCharactersListDisplayBehaviour __instance)
    {
        __instance.IdleCharacterDisplays = __instance.GetComponentsInChildren<ShelterCharacterDisplayBehaviour>();
    }

    // add 2 more slots for the food distribution panel
    [HarmonyPrefix]
    [HarmonyPatch(typeof(FoodDistributionCharacterPickersListDisplay), "Awake")]
    static bool FoodDistributionCharacterPickersListDisplayAwake(FoodDistributionCharacterPickersListDisplay __instance)
    {
        GameObject pickerPrefab = GameObject.Find("FoodDistributionCharacterPicker");
        Object.Instantiate<GameObject>(pickerPrefab, __instance.transform).transform.SetSiblingIndex(4);
        Object.Instantiate<GameObject>(pickerPrefab, __instance.transform).transform.SetSiblingIndex(5);
        
        return true;
    }

    // add 2 more slots for the inventory distribution panel
    [HarmonyPrefix]
    [HarmonyPatch(typeof(InventoryDistributionCharacterPickersListDisplay), "Awake")]
    static bool InventoryDistributionCharacterPickersListDisplayAwake(InventoryDistributionCharacterPickersListDisplay __instance)
    {
        GameObject pickerPrefab = GameObject.Find("CharacterInventoryDistributionDetails");
        Object.Instantiate(pickerPrefab, __instance.transform.GetChild(0)).transform.SetSiblingIndex(2);
        Object.Instantiate(pickerPrefab, __instance.transform.GetChild(1)).transform.SetSiblingIndex(2);
        
        __instance.transform.parent.GetChild(0).transform.localPosition += new Vector3(0f, -150f, 0f); // start button
        __instance.transform.parent.GetChild(5).transform.localPosition += new Vector3(0f, -150f, 0f); // back button
        __instance.transform.parent.GetChild(4).transform.localPosition += new Vector3(0f, -150f, 0f); // text
        return true;
    }

    // add 2 more slots for the party recomposition panel
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PartyRecompositionModalDisplay), "Awake")]
    static bool PartyRecompositionModalDisplayAwake(PartyRecompositionModalDisplay __instance)
    {
        GameObject pickerPrefab = GameObject.Find("PartyRecompositionCharacterPicker");
        Object.Instantiate(pickerPrefab, __instance.transform.GetChild(0).GetChild(1).GetChild(6)).transform.SetSiblingIndex(5);
        Object.Instantiate(pickerPrefab, __instance.transform.GetChild(0).GetChild(1).GetChild(6)).transform.SetSiblingIndex(6);

        MethodInfo awake = AccessTools.Method(typeof(PartyRecompositionCharacterPickersListDisplay), "Awake");
        awake.Invoke(__instance.CharacterPickersListDisplay, null);
        return true;
    }

    // add 2 more slots for the victory panel
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(VictoryScreenCharactersDisplayBehaviour), "Show")]
    static IEnumerable<CodeInstruction> VictoryScreenCharactersDisplayBehaviourShow(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        LocalBuilder varCharE = il.DeclareLocal(typeof(GameObject));
        LocalBuilder varCharF = il.DeclareLocal(typeof(GameObject));
        int patch = 0;

        foreach(CodeInstruction instruction in instructions)
        {
            if(instruction.opcode == OpCodes.Stloc_2 && patch == 0)
            {
                patch++;
                yield return instruction;

                // find CharacterD
                yield return new CodeInstruction(OpCodes.Ldstr, "CharacterD");
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GameObject), "Find", new System.Type[] { typeof(string) }));
                yield return new CodeInstruction(OpCodes.Dup);

                // instantiate CharacterE
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Component), "get_transform"));
                yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Transform), "GetChild", new System.Type[] { typeof(int) }));
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Object), "Instantiate", new System.Type[] { typeof(GameObject), typeof(Transform) }));
                yield return new CodeInstruction(OpCodes.Stloc_S, varCharE);

                // rename CharacterE
                yield return new CodeInstruction(OpCodes.Ldloc_S, varCharE);
                yield return new CodeInstruction(OpCodes.Ldstr, "CharacterE");
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Object), "set_name", new System.Type[] { typeof(string) }));
                
                // sibling index CharacterE
                yield return new CodeInstruction(OpCodes.Ldloc_S, varCharE);
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(GameObject), "get_transform"));
                yield return new CodeInstruction(OpCodes.Ldc_I4_4);
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Transform), "SetSiblingIndex", new System.Type[] { typeof(int) }));

                // reposition CharacterE
                yield return new CodeInstruction(OpCodes.Ldloc_S, varCharE);
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(GameObject), "get_transform"));
                yield return new CodeInstruction(OpCodes.Dup);
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Transform), "get_localPosition"));
                yield return new CodeInstruction(OpCodes.Ldc_R4, 90.0f);
                yield return new CodeInstruction(OpCodes.Ldc_R4, 0.0f);
                yield return new CodeInstruction(OpCodes.Ldc_R4, 0.0f);
                yield return new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(Vector3), new System.Type[] { typeof(float), typeof(float), typeof(float) }));
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Vector3), "op_Addition", new System.Type[] { typeof(Vector3), typeof(Vector3) }));
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Transform), "set_localPosition", new System.Type[] { typeof(Vector3) }));
                
                // instantiate CharacterF
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Component), "get_transform"));
                yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Transform), "GetChild", new System.Type[] { typeof(int) }));
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Object), "Instantiate", new System.Type[] { typeof(GameObject), typeof(Transform) }));
                yield return new CodeInstruction(OpCodes.Stloc_S, varCharF);

                // rename CharacterF
                yield return new CodeInstruction(OpCodes.Ldloc_S, varCharF);
                yield return new CodeInstruction(OpCodes.Ldstr, "CharacterF");
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Object), "set_name", new System.Type[] { typeof(string) }));
                
                // sibling index CharacterF
                yield return new CodeInstruction(OpCodes.Ldloc_S, varCharF);
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(GameObject), "get_transform"));
                yield return new CodeInstruction(OpCodes.Ldc_I4_5);
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Transform), "SetSiblingIndex", new System.Type[] { typeof(int) }));

                // reposition CharacterF
                yield return new CodeInstruction(OpCodes.Ldloc_S, varCharF);
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(GameObject), "get_transform"));
                yield return new CodeInstruction(OpCodes.Dup);
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Transform), "get_localPosition"));
                yield return new CodeInstruction(OpCodes.Ldc_R4, 180.0f);
                yield return new CodeInstruction(OpCodes.Ldc_R4, 0.0f);
                yield return new CodeInstruction(OpCodes.Ldc_R4, 0.0f);
                yield return new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(Vector3), new System.Type[] { typeof(float), typeof(float), typeof(float) }));
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Vector3), "op_Addition", new System.Type[] { typeof(Vector3), typeof(Vector3) }));
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Transform), "set_localPosition", new System.Type[] { typeof(Vector3) }));

                // add CharacterE
                yield return new CodeInstruction(OpCodes.Ldloc_2);
                yield return new CodeInstruction(OpCodes.Ldloc_S, varCharE);
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(GameObject), "GetComponent").MakeGenericMethod(typeof(Image)));
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<>).MakeGenericType(typeof(Image)), "Add"));
                
                // add CharacterF
                yield return new CodeInstruction(OpCodes.Ldloc_2);
                yield return new CodeInstruction(OpCodes.Ldloc_S, varCharF);
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(GameObject), "GetComponent").MakeGenericMethod(typeof(Image)));
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<>).MakeGenericType(typeof(Image)), "Add"));

            }
            else
            {
                yield return instruction;  
            }
        }
    }

    // add 2 more slots for the main title screen
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SavedProgressDisplayBehaviour), "OnGameEvent")]
    static bool SavedProgressDisplayBehaviourOnGameEvent(SavedProgressDisplayBehaviour __instance)
    {
        // adding 2 more character display (from 6 to 8)
        // but once
        if (__instance.transform.GetChild(0).GetChild(2).childCount == 6) // vanilla is 6
        {
            GameObject pickerPrefab = GameObject.Find("CharacterDisplay1.5");
            GameObject characterDisplay6 = Object.Instantiate(pickerPrefab, __instance.transform.GetChild(0).GetChild(2));
            characterDisplay6.transform.SetSiblingIndex(6);
            characterDisplay6.name = "CharacterDisplay1.6";
            GameObject characterDisplay7 = Object.Instantiate(pickerPrefab, __instance.transform.GetChild(0).GetChild(2));
            characterDisplay7.transform.SetSiblingIndex(7);
            characterDisplay7.name = "CharacterDisplay1.7";

            RectTransform rectRoot = __instance.transform.GetChild(0).transform.GetComponent<RectTransform>();
            rectRoot.sizeDelta += new Vector2(rectRoot.sizeDelta.x/4.0f, 0.0f); // increase the window of 25% since we add 2 more portraits
        }

        // fill the character display list
        MethodInfo setElementDisplays = AccessTools.Method(typeof(GenericListDisplayBehavior<CharacterImageDisplay, string>), "set_ElementDisplays");
        setElementDisplays.Invoke(__instance.CharactersImagesList, new object[] { __instance.transform.GetComponentsInChildren<CharacterImageDisplay>() });
        return true;
    }
}
