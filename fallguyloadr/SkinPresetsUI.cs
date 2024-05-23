using BepInEx;
using FG.Common;
using FGClient;
using FGClient.UI;
using MPG.Utility;
using SRF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Panels;

namespace fallguyloadr
{
    public class SkinPresetsUI : PanelBase
    {
        public SkinPresetsUI(UIBase owner) : base(owner)
        {
            if (instance == null)
            {
                instance = this;
            }
        }

        public static SkinPresetsUI instance;

        public override string Name => "Skin Preset Selector";
        public override int MinWidth => 555;
        public override int MinHeight => 310;
        public override Vector2 DefaultAnchorMin => new(0.25f, 0.25f);
        public override Vector2 DefaultAnchorMax => new(0.75f, 0.75f);

        Il2CppSystem.Collections.Generic.List<string> Presets = new();
        Dropdown presetsDropdown;

        SkinPreset currentPreset;
        string currentPresetName = string.Empty;

        string presetName 
        { 
            get
            {
                return currentPresetName == string.Empty ? "No preset currently chosen." : currentPresetName;
            }
        }

        Text currentPresetText;

        Text presetColourText;
        Text presetPatternText;
        Text presetFaceplateText;
        Text presetUpperText;
        Text presetLowerText;
        Text presetEmote1Text;
        Text presetEmote2Text;
        Text presetEmote3Text;
        Text presetEmote4Text;
        Text presetCelebrationText;

        Dictionary<string, ColourOption> ColourOptions = new();
        Dictionary<string, SkinPatternOption> PatternOptions = new();
        Dictionary<string, FaceplateOption> FaceplateOptions = new();
        Dictionary<string, CostumeOption> CostumeOptions = new();
        Dictionary<string, EmotesOption> EmoteOptions = new();
        Dictionary<string, VictoryOption> CelebrationOptions = new();

        protected override void ConstructPanelContent()
        {
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, MinWidth);
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, MinHeight);
            Dragger.OnEndResize();

            GameObject selectionRow = UIFactory.CreateHorizontalGroup(ContentRoot, "Selection Row", true, false, true, true, 2, bgColor: new Color(0.07f, 0.07f, 0.07f, 1));

            UIFactory.CreateDropdown(selectionRow, "Presets", out presetsDropdown, "", 14, SelectPresetFromDropdown);
            UIFactory.SetLayoutElement(presetsDropdown.gameObject, 350, 25, 0, 0);

            RefreshPresets();

            ButtonRef createButton = UIFactory.CreateButton(selectionRow, "Create Button", "Create");
            UIFactory.SetLayoutElement(createButton.Component.gameObject, 100, 25, 0, 0);
            createButton.OnClick += CreatePopup;

            ButtonRef selectButton = UIFactory.CreateButton(selectionRow, "Select Button", "Equip");
            UIFactory.SetLayoutElement(selectButton.Component.gameObject, 100, 25, 0, 0);
            selectButton.OnClick += EquipSelectedPreset;

            GameObject selectionNameCollumn = UIFactory.CreateVerticalGroup(ContentRoot, "", false, false, true, true, 5, new Vector4(5, 5, 5, 5), new Color(0.07f, 0.07f, 0.07f, 1));

            currentPresetText = UIFactory.CreateLabel(selectionNameCollumn, "Current Preset Name Text", presetName);
            UIFactory.SetLayoutElement(currentPresetText.gameObject);

            GameObject selectionsCollumn = UIFactory.CreateVerticalGroup(ContentRoot, "", false, false, true, true, 5, new Vector4(5, 5, 5, 5), new Color(0.07f, 0.07f, 0.07f, 1));

            presetColourText = UIFactory.CreateLabel(selectionsCollumn, "Preset Colour Text", "Colour: ");
            presetPatternText = UIFactory.CreateLabel(selectionsCollumn, "Preset Pattern Text", "Pattern: ");
            presetFaceplateText = UIFactory.CreateLabel(selectionsCollumn, "Preset Faceplate Text", "Faceplate: ");
            presetUpperText = UIFactory.CreateLabel(selectionsCollumn, "Preset Upper Text", "Upper Costume: ");
            presetLowerText = UIFactory.CreateLabel(selectionsCollumn, "Preset Lower Text", "Lower Costume: ");
            presetEmote1Text = UIFactory.CreateLabel(selectionsCollumn, "Preset Emote 1 Text", "Emote 1: ");
            presetEmote2Text = UIFactory.CreateLabel(selectionsCollumn, "Preset Emote 2 Text", "Emote 2: ");
            presetEmote3Text = UIFactory.CreateLabel(selectionsCollumn, "Preset Emote 3 Text", "Emote 3: ");
            presetEmote4Text = UIFactory.CreateLabel(selectionsCollumn, "Preset Emote 4 Text", "Emote 4: ");
            presetCelebrationText = UIFactory.CreateLabel(selectionsCollumn, "Preset Celebration Text", "Victory Celebration: ");

            //SelectPreset(new SkinPreset(), "");
            SetCostumeOptions();
        }

        void SetCostumeOptions()
        {
            ColourOption[] colourOptions = Resources.FindObjectsOfTypeAll<ColourOption>();
            SkinPatternOption[] patternOptions = Resources.FindObjectsOfTypeAll<SkinPatternOption>();
            FaceplateOption[] faceplateOptions = Resources.FindObjectsOfTypeAll<FaceplateOption>();
            CostumeOption[] costumeOptions = Resources.FindObjectsOfTypeAll<CostumeOption>();
            EmotesOption[] emoteOptions = Resources.FindObjectsOfTypeAll<EmotesOption>();
            VictoryOption[] celebrationOptions = Resources.FindObjectsOfTypeAll<VictoryOption>();

            foreach (ColourOption colourOption in colourOptions)
            {
                if (!ColourOptions.ContainsKey(colourOption.name))
                {
                    ColourOptions.Add(colourOption.name, colourOption);
                }
            }

            foreach (SkinPatternOption patternOption in patternOptions)
            {
                if (!PatternOptions.ContainsKey(patternOption.name))
                {
                    PatternOptions.Add(patternOption.name, patternOption);
                }
            }

            foreach (FaceplateOption faceplateOption in faceplateOptions)
            {
                if (!FaceplateOptions.ContainsKey(faceplateOption.name))
                {
                    FaceplateOptions.Add(faceplateOption.name, faceplateOption);
                }
            }

            foreach (CostumeOption costumeOption in costumeOptions)
            {
                if (!CostumeOptions.ContainsKey(costumeOption.name))
                {
                    CostumeOptions.Add(costumeOption.name, costumeOption);
                }
            }

            foreach (EmotesOption emoteOption in emoteOptions)
            {
                if (!EmoteOptions.ContainsKey(emoteOption.name))
                {
                    EmoteOptions.Add(emoteOption.name, emoteOption);
                }
            }

            foreach (VictoryOption celebrationOption in celebrationOptions)
            {
                if (!CelebrationOptions.ContainsKey(celebrationOption.name))
                {
                    CelebrationOptions.Add(celebrationOption.name, celebrationOption);
                }
            }
        }

        void DisableAllUI()
        {
            int ChildCount = Owner.RootObject.transform.parent.GetChildCount();
            for (int i = 0; i < ChildCount; i++)
            {
                Owner.RootObject.transform.parent.GetChild(i).gameObject.SetActive(false);
            }
        }

        void RefreshPresets()
        {
            Presets.Clear();
            Presets.Add("Choose a Preset");
            string[] fileNames = Directory.GetFiles($"{Paths.PluginPath}/fallguyloadr/Presets");
            foreach (string fileName in fileNames)
            {
                if (fileName.EndsWith(".json"))
                {
                    Presets.Add(Path.GetFileNameWithoutExtension(fileName));
                }
            }

            presetsDropdown.ClearOptions();
            presetsDropdown.AddOptions(Presets);
        }

        void CreatePopup()
        {
            DisableAllUI(); // i have to do this because you cant type when any universe lib ui is open for some reason

            Action<bool, string> action = CreatePreset;

            ModalMessageWithInputFieldData modalMessageData = new ModalMessageWithInputFieldData()
            {
                Title = "fallguyloadr - SKIN PRESETS",
                Message = "Enter the name you want to give to the preset.",
                LocaliseTitle = UIModalMessage.LocaliseOption.NotLocalised,
                LocaliseMessage = UIModalMessage.LocaliseOption.NotLocalised,
                ModalType = UIModalMessage.ModalType.MT_OK_CANCEL,
                InputFieldType = TMP_InputField.ContentType.Standard,
                MessageAdditional = "fallguyloadr_skin_presets_create_popup_additional_message",
                OkTextOverrideId = "fallguyloadr_create",
                OnInputFieldModalClosed = action
            };

            PopupManager.Instance.Show(PopupInteractionType.Query, modalMessageData);
        }

        void CreatePreset(bool wasOk, string inputFieldText)
        {
            if (wasOk)
            {
                CustomisationSelections selections = GlobalGameStateClient.Instance.PlayerProfile.CustomisationSelections;

                SkinPreset skinPreset = new SkinPreset();
                skinPreset.Colour = selections.ColourOption.name;
                skinPreset.Pattern = selections.PatternOption.name;
                skinPreset.Faceplate = selections.FaceplateOption.name;
                skinPreset.Upper = selections.CostumeTopOption.name;
                skinPreset.Lower = selections.CostumeBottomOption.name;
                skinPreset.Emote1 = selections.EmoteTopOption.name;
                skinPreset.Emote2 = selections.EmoteRightOption.name;
                skinPreset.Emote3 = selections.EmoteBottomOption.name;
                skinPreset.Emote4 = selections.EmoteLeftOption.name;
                skinPreset.Celebration = selections.VictoryPoseOption.name;

                string skinPresetJson = JsonSerializer.Serialize(skinPreset);

                inputFieldText = inputFieldText.Length > 0 ? inputFieldText : "NoName";

                File.WriteAllText($"{Paths.PluginPath}/fallguyloadr/Presets/{inputFieldText}.json", skinPresetJson);
                RefreshPresets();
                SelectPreset(skinPreset, inputFieldText);
            }
            Owner.Enabled = true;
        }

        void SelectPreset(SkinPreset skinPreset, string name)
        {
            currentPreset = skinPreset;

            presetColourText.text = $"Colour: {ColourOptions[currentPreset.Colour].DisplayName} <color=#666666><i>{currentPreset.Colour}</i></color>";
            presetPatternText.text = $"Pattern: {PatternOptions[currentPreset.Pattern].DisplayName} <color=#666666><i>{currentPreset.Pattern}</i></color>";
            presetFaceplateText.text = $"Faceplate: {FaceplateOptions[currentPreset.Faceplate].DisplayName} <color=#666666><i>{currentPreset.Faceplate}</i></color>";
            presetUpperText.text = $"Upper Costume: {CostumeOptions[currentPreset.Upper].DisplayName} <color=#666666><i>{currentPreset.Upper}</i></color>";
            presetLowerText.text = $"Lower Costume: {CostumeOptions[currentPreset.Lower].DisplayName} <color=#666666><i>{currentPreset.Lower}</i></color>";
            presetEmote1Text.text = $"Emote 1: {EmoteOptions[currentPreset.Emote1].DisplayName} <color=#666666><i>{currentPreset.Emote1}</i></color>";
            presetEmote2Text.text = $"Emote 2: {EmoteOptions[currentPreset.Emote2].DisplayName} <color=#666666><i>{currentPreset.Emote2}</i></color>";
            presetEmote3Text.text = $"Emote 3: {EmoteOptions[currentPreset.Emote3].DisplayName} <color=#666666><i>{currentPreset.Emote3}</i></color>";
            presetEmote4Text.text = $"Emote 4: {EmoteOptions[currentPreset.Emote4].DisplayName} <color=#666666><i>{currentPreset.Emote4}</i></color>";
            presetCelebrationText.text = $"Victory Celebrarion: {CelebrationOptions[currentPreset.Celebration].DisplayName} <color=#666666><i>{currentPreset.Celebration}</i></color>";

            currentPresetName = name;

            currentPresetText.text = presetName;
        }

        void SelectPresetFromDropdown(int index)
        {
            if (index > 0)
            {
                ReadPresetJson($"{Paths.PluginPath}/fallguyloadr/Presets/{Presets[index]}.json");
            }
        }

        void ReadPresetJson(string path)
        {
            SkinPreset skinPreset = JsonSerializer.Deserialize<SkinPreset>(File.ReadAllText(path));
            SelectPreset(skinPreset, Path.GetFileNameWithoutExtension(path));
        }

        void EquipSelectedPreset()
        {
            if (currentPreset != null)
            {
                CustomisationSelections selections = GlobalGameStateClient.Instance.PlayerProfile.CustomisationSelections;

                selections.ColourOption = ColourOptions[currentPreset.Colour];
                selections.PatternOption = PatternOptions[currentPreset.Pattern];
                selections.FaceplateOption = FaceplateOptions[currentPreset.Faceplate];
                selections.CostumeTopOption = CostumeOptions[currentPreset.Upper];
                selections.CostumeBottomOption = CostumeOptions[currentPreset.Lower];
                selections.EmoteTopOption = EmoteOptions[currentPreset.Emote1];
                selections.EmoteRightOption = EmoteOptions[currentPreset.Emote2];
                selections.EmoteBottomOption = EmoteOptions[currentPreset.Emote3];
                selections.EmoteLeftOption = EmoteOptions[currentPreset.Emote4];
                selections.VictoryPoseOption = CelebrationOptions[currentPreset.Celebration];

                MainMenuManager mainMenuManager = GameObject.FindObjectOfType<MainMenuManager>();

                if (mainMenuManager != null)
                {
                    mainMenuManager.ApplyOutfit();
                }
            }
        }
    }
}
