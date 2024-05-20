using Catapult.Modules.Items.Protocol.Dtos;
using FallGuys.Player.Protocol.Client.Cosmetics;
using FG.Common.CMS;
using FG.Common.Definition;
using FGClient.Customiser;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace fallguyloadr
{
    public class CosmeticsPatches
    {
        [HarmonyPatch(typeof(CustomiserScreenViewModel), "HandleConfigureRequestFailed")]
        [HarmonyPrefix]
        static bool CustomiserScreenViewModelShowSpinner(CustomiserScreenViewModel __instance)
        {
            __instance.HideSpinner();
            return false;
        }

        [HarmonyPatch(typeof(CustomiserColourSection), "GetOwnedList")]
        [HarmonyPrefix]
        static bool ColourGetList(CustomiserColourSection __instance, ref Il2CppSystem.Collections.Generic.List<ColourSchemeDto> __result)
        {
            ColourOption[] colourOptions = Resources.FindObjectsOfTypeAll<ColourOption>();
            Il2CppSystem.Collections.Generic.List<ColourSchemeDto> colourSchemes = new Il2CppSystem.Collections.Generic.List<ColourSchemeDto>();

            foreach (ColourOption colourOption in colourOptions)
            {
                if (colourOption.CMSData != null)
                {
                    colourSchemes.Add(ItemDtoToColourSchemeDto(CMSDefinitionToItemDto(colourOption.CMSData)));
                }
            }
            __result = colourSchemes;
            return false;
        }

        [HarmonyPatch(typeof(CustomiserPatternsSection), "GetOwnedList")]
        [HarmonyPrefix]
        static bool PatternGetList(CustomiserPatternsSection __instance, ref Il2CppSystem.Collections.Generic.List<PatternDto> __result)
        {
            SkinPatternOption[] patternOptions = Resources.FindObjectsOfTypeAll<SkinPatternOption>();
            Il2CppSystem.Collections.Generic.List<PatternDto> patterns = new Il2CppSystem.Collections.Generic.List<PatternDto>();

            foreach (SkinPatternOption patternOption in patternOptions)
            {
                if (patternOption.CMSData != null)
                {
                    patterns.Add(ItemDtoToPatternDto(CMSDefinitionToItemDto(patternOption.CMSData)));
                }
            }
            __result = patterns;
            return false;
        }

        [HarmonyPatch(typeof(CustomiserFaceplateSection), "GetOwnedList")]
        [HarmonyPrefix]
        static bool FaceplateGetList(CustomiserFaceplateSection __instance, ref Il2CppSystem.Collections.Generic.List<FaceplateDto> __result)
        {
            FaceplateOption[] faceplateOptions = Resources.FindObjectsOfTypeAll<FaceplateOption>();
            Il2CppSystem.Collections.Generic.List<FaceplateDto> faceplates = new Il2CppSystem.Collections.Generic.List<FaceplateDto>();

            foreach (FaceplateOption faceplateOption in faceplateOptions)
            {
                if (faceplateOption.CMSData != null)
                {
                    faceplates.Add(ItemDtoToFaceplateDto(CMSDefinitionToItemDto(faceplateOption.CMSData)));
                }
            }
            __result = faceplates;
            return false;
        }

        [HarmonyPatch(typeof(CustomiserNameplateSection), "GetOwnedList")]
        [HarmonyPrefix]
        static bool NameplateGetList(CustomiserNameplateSection __instance, ref Il2CppSystem.Collections.Generic.List<NameplateDto> __result)
        {
            NameplateOption[] nameplateOptions = Resources.FindObjectsOfTypeAll<NameplateOption>();
            Il2CppSystem.Collections.Generic.List<NameplateDto> nameplates = new Il2CppSystem.Collections.Generic.List<NameplateDto>();

            foreach (NameplateOption nameplateOption in nameplateOptions)
            {
                if (nameplateOption.CMSData != null)
                {
                    nameplates.Add(ItemDtoToNameplateDto(CMSDefinitionToItemDto(nameplateOption.CMSData)));
                }
            }
            __result = nameplates;
            return false;
        }

        [HarmonyPatch(typeof(CustomiserNicknameSection), "GetOwnedList")]
        [HarmonyPrefix]
        static bool NicknameGetList(CustomiserNicknameSection __instance, ref Il2CppSystem.Collections.Generic.List<NicknameDto> __result)
        {
            NicknamesSO nicknamesSO = Resources.FindObjectsOfTypeAll<NicknamesSO>().FirstOrDefault();
            Il2CppSystem.Collections.Generic.List<NicknameDto> nicknames = new Il2CppSystem.Collections.Generic.List<NicknameDto>();
            foreach (Nickname nickname in nicknamesSO.Nicknames.Values)
            {
                nicknames.Add(ItemDtoToNicknameDto(CMSDefinitionToItemDto(nickname)));
            }

            __result = nicknames;
            return false;
        }

        [HarmonyPatch(typeof(CustomiserEmotesSection), "GetOwnedList")]
        [HarmonyPrefix]
        static bool NameplateGetList(CustomiserEmotesSection __instance, ref Il2CppSystem.Collections.Generic.List<EmoteDto> __result)
        {
            EmotesOption[] emotesOptions = Resources.FindObjectsOfTypeAll<EmotesOption>();
            Il2CppSystem.Collections.Generic.List<EmoteDto> emotes = new Il2CppSystem.Collections.Generic.List<EmoteDto>();

            foreach (EmotesOption emotesOption in emotesOptions)
            {
                if (emotesOption.CMSData != null)
                {
                    emotes.Add(ItemDtoToEmoteDto(CMSDefinitionToItemDto(emotesOption.CMSData)));
                }
            }
            __result = emotes;
            return false;
        }

        [HarmonyPatch(typeof(CustomiserVictorySection), "GetOwnedList")]
        [HarmonyPrefix]
        static bool VictoryGetList(CustomiserVictorySection __instance, ref Il2CppSystem.Collections.Generic.List<PunchlineDto> __result)
        {
            VictoryOption[] victoryOptions = Resources.FindObjectsOfTypeAll<VictoryOption>();
            Il2CppSystem.Collections.Generic.List<PunchlineDto> punchlines = new Il2CppSystem.Collections.Generic.List<PunchlineDto>();

            foreach (VictoryOption victoryOption in victoryOptions)
            {
                if (victoryOption.CMSData != null)
                {
                    punchlines.Add(ItemDtoToPunchlineDto(CMSDefinitionToItemDto(victoryOption.CMSData)));
                }
            }
            __result = punchlines;
            return false;
        }

        [HarmonyPatch(typeof(CustomiserLowerCostumeSection), "GetOwnedList")]
        [HarmonyPrefix]
        static bool LowerGetList(CustomiserLowerCostumeSection __instance, ref Il2CppSystem.Collections.Generic.List<LowerCostumePieceDto> __result)
        {
            CostumeOption[] costumeOptions = Resources.FindObjectsOfTypeAll<CostumeOption>();
            Il2CppSystem.Collections.Generic.List<LowerCostumePieceDto> lowerCostumePieces = new Il2CppSystem.Collections.Generic.List<LowerCostumePieceDto>();

            foreach (CostumeOption costumeOption in costumeOptions)
            {
                if (costumeOption.CMSData != null && costumeOption.CostumeType == CostumeType.Bottom)
                {
                    lowerCostumePieces.Add(ItemDtoToLowerCostumePieceDto(CMSDefinitionToItemDto(costumeOption.CMSData)));
                }
            }
            __result = lowerCostumePieces;
            return false;
        }

        [HarmonyPatch(typeof(CustomiserUpperCostumeSection), "GetOwnedList")]
        [HarmonyPrefix]
        static bool UpperGetList(CustomiserUpperCostumeSection __instance, ref Il2CppSystem.Collections.Generic.List<UpperCostumePieceDto> __result)
        {
            CostumeOption[] costumeOptions = Resources.FindObjectsOfTypeAll<CostumeOption>();
            Il2CppSystem.Collections.Generic.List<UpperCostumePieceDto> upperCostumePieces = new Il2CppSystem.Collections.Generic.List<UpperCostumePieceDto>();

            foreach (CostumeOption costumeOption in costumeOptions)
            {
                if (costumeOption.CMSData != null && costumeOption.CostumeType == CostumeType.Top)
                {
                    upperCostumePieces.Add(ItemDtoToUpperCostumePieceDto(CMSDefinitionToItemDto(costumeOption.CMSData)));
                }
            }
            __result = upperCostumePieces;
            return false;
        }

        public static ItemDto CMSDefinitionToItemDto(CMSItemDefinition itemDefinition)
        {
            ItemDto itemDto = new ItemDto();

            itemDto.ContentId = itemDefinition.Id;
            itemDto.Id = itemDefinition.FullItemId;
            itemDto.ContentType = itemDefinition.GroupId;
            itemDto.Quantity = 1;
            return itemDto;
        }

        public static ColourSchemeDto ItemDtoToColourSchemeDto(ItemDto itemDto)
        {
            ColourSchemeDto cosmeticDto = new ColourSchemeDto();
            cosmeticDto.EarnedAt = Il2CppSystem.DateTime.Now;
            cosmeticDto.Item = itemDto;
            cosmeticDto.IsFavourite = false;
            return cosmeticDto;
        }

        public static PatternDto ItemDtoToPatternDto(ItemDto itemDto)
        {
            PatternDto cosmeticDto = new PatternDto();
            cosmeticDto.EarnedAt = Il2CppSystem.DateTime.Now;
            cosmeticDto.Item = itemDto;
            cosmeticDto.IsFavourite = false;
            return cosmeticDto;
        }

        public static FaceplateDto ItemDtoToFaceplateDto(ItemDto itemDto)
        {
            FaceplateDto cosmeticDto = new FaceplateDto();
            cosmeticDto.EarnedAt = Il2CppSystem.DateTime.Now;
            cosmeticDto.Item = itemDto;
            cosmeticDto.IsFavourite = false;
            return cosmeticDto;
        }

        public static NameplateDto ItemDtoToNameplateDto(ItemDto itemDto)
        {
            NameplateDto cosmeticDto = new NameplateDto();
            cosmeticDto.EarnedAt = Il2CppSystem.DateTime.Now;
            cosmeticDto.Item = itemDto;
            cosmeticDto.IsFavourite = false;
            return cosmeticDto;
        }

        public static NicknameDto ItemDtoToNicknameDto(ItemDto itemDto)
        {
            NicknameDto cosmeticDto = new NicknameDto();
            cosmeticDto.EarnedAt = Il2CppSystem.DateTime.Now;
            cosmeticDto.Item = itemDto;
            cosmeticDto.IsFavourite = false;
            return cosmeticDto;
        }

        public static EmoteDto ItemDtoToEmoteDto(ItemDto itemDto)
        {
            EmoteDto cosmeticDto = new EmoteDto();
            cosmeticDto.EarnedAt = Il2CppSystem.DateTime.Now;
            cosmeticDto.Item = itemDto;
            cosmeticDto.IsFavourite = false;
            return cosmeticDto;
        }

        public static PunchlineDto ItemDtoToPunchlineDto(ItemDto itemDto)
        {
            PunchlineDto cosmeticDto = new PunchlineDto();
            cosmeticDto.EarnedAt = Il2CppSystem.DateTime.Now;
            cosmeticDto.Item = itemDto;
            cosmeticDto.IsFavourite = false;
            return cosmeticDto;
        }

        public static LowerCostumePieceDto ItemDtoToLowerCostumePieceDto(ItemDto itemDto)
        {
            LowerCostumePieceDto cosmeticDto = new LowerCostumePieceDto();
            cosmeticDto.EarnedAt = Il2CppSystem.DateTime.Now;
            cosmeticDto.Item = itemDto;
            cosmeticDto.IsFavourite = false;
            return cosmeticDto;
        }

        public static UpperCostumePieceDto ItemDtoToUpperCostumePieceDto(ItemDto itemDto)
        {
            UpperCostumePieceDto cosmeticDto = new UpperCostumePieceDto();
            cosmeticDto.EarnedAt = Il2CppSystem.DateTime.Now;
            cosmeticDto.Item = itemDto;
            cosmeticDto.IsFavourite = false;
            return cosmeticDto;
        }
    }
}
