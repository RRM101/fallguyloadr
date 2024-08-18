using FG.Common.CMS;
using SRF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Panels;

namespace fallguyloadr.UI
{
    public class LoaderUI : PanelBase
    {
        public LoaderUI(UIBase owner) : base(owner)
        {
            if (instance == null)
            {
                instance = this;
            }
        }

        public static LoaderUI instance;

        public override string Name => $"fallguyloadr v{Plugin.version} | Press F2 to Hide/Unhide";
        public override int MinWidth => 800;
        public override int MinHeight => 145;
        public override Vector2 DefaultAnchorMin => new(0.25f, 0.25f);
        public override Vector2 DefaultAnchorMax => new(0.75f, 0.75f);
        public override bool CanDragAndResize => true;

        Dropdown roundsDropdown;
        Dropdown roundVariationDropdown;

        int selectedVariationIndex;

        Il2CppSystem.Collections.Generic.List<string> rounds = new Il2CppSystem.Collections.Generic.List<string>();
        Il2CppSystem.Collections.Generic.List<string> currentRounds;

        List<string> scenes = new List<string>();
        Dictionary<string, Il2CppSystem.Collections.Generic.List<string>> sceneNameAndRoundVariations = new Dictionary<string, Il2CppSystem.Collections.Generic.List<string>>();
        Il2CppSystem.Collections.Generic.List<string> variationsForSelectedRound;
        List<Round> usableRounds = new List<Round>();
        Dictionary<string, string> roundNameToSceneName = new Dictionary<string, string>();

        string[] deletedScenes = new string[]
        {
            "FallGuy_Arena_TestBed",
            "FallGuy_Audio_Test",
            "FallGuy_Gauntlet_TestBed",
            "FallGuy_Metrics_Testbed_01",
            "FallGuy_S9_Obstacle_Balance_Testbed",
            "FallGuy_ScoreZonesTeamMode",
            "FallGuy_Obstacles_Season_3",
            "FallGuy_Obstacles_Season_4",
            "FallGuy_Background_Goop_Season_5",
            "FallGuy_Background_Respawn_Season_5",
            "FallGuy_Obstacles_Season_5",
            "FallGuy_Background_Goop_Season_6",
            "FallGuy_Background_Respawn_Season_6",
            "FallGuy_Obstacles_Season_6",
            "FallGuy_Background_Goop_Season_7",
            "FallGuy_Background_Respawn_Season_7",
            "FallGuy_Obstacles_Symphony_1",
            "FallGuy_Obstacles_Symphony_2",
            "FallGuy_Season_9_SlideTestingArea",
            "FallGuy_Testbed",
            "FallGuy_Testbed_Wormhole",
            "FallGuy_ThinIce_FloorFall",
            "FallGuy_Toms_Gym",
            "FallGuy_Turntables",
            "FallGuy_Variation_QA",
            "FallGuy_Empty_TestBed",
            "FallGuy_ScoreZones"
        };

        protected override void ConstructPanelContent()
        {
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 800);
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100);
            Dragger.OnEndResize();

            GenerateRoundsList();

            GameObject searchRow = UIFactory.CreateHorizontalGroup(ContentRoot, "", true, false, true, true, 4, bgColor: new Color(0.07f, 0.07f, 0.07f, 1));
            InputFieldRef searchInputField = UIFactory.CreateInputField(searchRow, "search", "Search for a Round");
            UIFactory.SetLayoutElement(searchInputField.Component.gameObject, minHeight: 25, minWidth: 300, flexibleWidth: 0, flexibleHeight: 0);
            searchInputField.OnValueChanged += SearchRounds;

            GameObject dropdownRow = UIFactory.CreateHorizontalGroup(ContentRoot, "Dropdowns", true, false, true, true, 4, bgColor: new Color(0.07f, 0.07f, 0.07f, 1));

            UIFactory.CreateDropdown(dropdownRow, "Rounds", out roundsDropdown, "", 14, RoundSelected);
            SetDropdownRounds(rounds);
            UIFactory.SetLayoutElement(roundsDropdown.gameObject, minHeight: 25, minWidth: 390, flexibleWidth: 0, flexibleHeight: 0);

            UIFactory.CreateDropdown(dropdownRow, "Round Variation", out roundVariationDropdown, "", 14, VariationSelected);
            ClearVariationDropdown();
            UIFactory.SetLayoutElement(roundVariationDropdown.gameObject, minHeight: 25, minWidth: 390, flexibleWidth: 0, flexibleHeight: 0);

            GameObject buttonVerticleGroup = UIFactory.CreateVerticalGroup(ContentRoot, "Buttons group", true, false, true, true, 4, bgColor: new Color(0.07f, 0.07f, 0.07f, 1));

            GameObject loadRoundButtonRow = UIFactory.CreateHorizontalGroup(buttonVerticleGroup, "Buttons", true, false, true, true, 4, bgColor: new Color(0.07f, 0.07f, 0.07f, 1));
            GameObject miscButtons = UIFactory.CreateHorizontalGroup(buttonVerticleGroup, "Buttons2", true, false, true, true, 4, bgColor: new Color(0.07f, 0.07f, 0.07f, 1));

            ButtonRef loadRoundButton = UIFactory.CreateButton(loadRoundButtonRow, "Load Round Button", "Load Round");
            UIFactory.SetLayoutElement(loadRoundButton.Component.gameObject, minHeight: 25, minWidth: 0, flexibleWidth: 0, flexibleHeight: 0);
            loadRoundButton.OnClick += () =>
            {
                if (variationsForSelectedRound != null)
                {
                    ReplayManager.Instance.StopPlayingReplay();
                    LoaderBehaviour.instance.LoadRound(variationsForSelectedRound[selectedVariationIndex]);
                }
            };

            ButtonRef loadRandomRoundButton = UIFactory.CreateButton(loadRoundButtonRow, "Load Random Round Button", "Load Random Round");
            UIFactory.SetLayoutElement(loadRandomRoundButton.Component.gameObject, minHeight: 25, minWidth: 0, flexibleWidth: 0, flexibleHeight: 0);
            loadRandomRoundButton.OnClick += LoadRandomRound;

            ButtonRef skinPresetsButton = UIFactory.CreateButton(miscButtons, "Skin Presets Button", "Skin Presets");
            UIFactory.SetLayoutElement(skinPresetsButton.Component.gameObject, minHeight: 25, minWidth: 0, flexibleWidth: 0, flexibleHeight: 0);
            skinPresetsButton.OnClick += SkinPresetsUI.instance.Toggle;

            ButtonRef themeSelectorButton = UIFactory.CreateButton(miscButtons, "Theme Selector Button", "Themes");
            UIFactory.SetLayoutElement(themeSelectorButton.Component.gameObject, minHeight: 25, minWidth: 0, flexibleWidth: 0, flexibleHeight: 0);
            themeSelectorButton.OnClick += ThemeSelector.instance.Toggle;

            ButtonRef replaysButton = UIFactory.CreateButton(miscButtons, "Replays Button", "Replays");
            UIFactory.SetLayoutElement(replaysButton.Component.gameObject, minHeight: 25, minWidth: 0, flexibleWidth: 0, flexibleHeight: 0);
            replaysButton.OnClick += ReplaySelector.instance.Toggle;
        }

        protected override void OnClosePanelClicked()
        {
            Owner.Enabled = false;
            CursorManager.Instance.OnApplicationFocus(true);
        }

        void GenerateRoundsList()
        {
            foreach (Round round in CMSLoader.Instance.CMSData.Rounds.Values)
            {
                string roundID = round.Id;
                if (!round.IsUGC() && !deletedScenes.Contains(round.GetSceneName()) && round.DisplayName != null)
                {
                    if (!scenes.Contains(round.GetSceneName()) && !rounds.Contains(RemoveIndentation(round.DisplayName.Text)))
                    {
                        scenes.Add(round.GetSceneName());
                        sceneNameAndRoundVariations.Add(round.GetSceneName(), new Il2CppSystem.Collections.Generic.List<string>());
                        sceneNameAndRoundVariations[round.GetSceneName()].Add(roundID);
                        rounds.Add(RemoveIndentation(round.DisplayName.Text));
                        roundNameToSceneName.Add(RemoveIndentation(round.DisplayName.Text), round.GetSceneName());
                    }
                    else if (sceneNameAndRoundVariations.ContainsKey(round.GetSceneName()))
                    {
                        sceneNameAndRoundVariations[round.GetSceneName()].Add(roundID);
                    }

                    usableRounds.Add(round);
                }
            }

            rounds.Sort();
        }

        string RemoveIndentation(string inputString)
        {
            string noTagsString = Regex.Replace(inputString, "<.*?>", string.Empty);
            return Regex.Replace(noTagsString, " {2,}", " ");
        }

        void ClearVariationDropdown()
        {
            roundVariationDropdown.ClearOptions();
            roundVariationDropdown.options.Add(new Dropdown.OptionData("Round IDs/Variations"));
            roundVariationDropdown.RefreshShownValue();
            variationsForSelectedRound = null;
        }

        void RoundSelected(int roundIndex)
        {
            if (roundIndex > 0)
            {
                roundVariationDropdown.ClearOptions();
                variationsForSelectedRound = sceneNameAndRoundVariations[roundNameToSceneName[currentRounds[roundIndex - 1]]];
                roundVariationDropdown.AddOptions(variationsForSelectedRound);
            }
            else
            {
                ClearVariationDropdown();
            }
        }

        void VariationSelected(int variationIndex)
        {
            selectedVariationIndex = variationIndex;
        }

        public void LoadRandomRound()
        {
            ReplayManager.Instance.StopPlayingReplay();
            int randomRoundIndex = UnityEngine.Random.Range(0, usableRounds.Count);
            LoaderBehaviour.instance.LoadRound(usableRounds[randomRoundIndex]);
        }

        void SetDropdownRounds(Il2CppSystem.Collections.Generic.List<string> roundList)
        {
            roundsDropdown.ClearOptions();
            roundsDropdown.options.Add(new Dropdown.OptionData("Rounds"));
            roundsDropdown.AddOptions(roundList);
            currentRounds = roundList;
        }

        void SearchRounds(string searchText)
        {
            ClearVariationDropdown();
            searchText = searchText.ToLower();
            Il2CppSystem.Collections.Generic.List<string> roundsSearch = new();
            foreach (string round in rounds)
            {
                if (searchText == string.Empty)
                {
                    SetDropdownRounds(rounds);
                    return;
                }

                if (round.ToLower().Contains(searchText))
                {
                    roundsSearch.Add(round);
                }
            }

            SetDropdownRounds(roundsSearch);
        }
    }
}
