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

namespace fallguyloadr
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
        public override int MinHeight => 100;
        public override Vector2 DefaultAnchorMin => new(0.25f, 0.25f);
        public override Vector2 DefaultAnchorMax => new(0.75f, 0.75f);
        public override bool CanDragAndResize => true;

        Dropdown roundVariationDropdown;
        int selectedRoundIndex;
        int selectedVariationIndex;
        Il2CppSystem.Collections.Generic.List<string> rounds = new Il2CppSystem.Collections.Generic.List<string>();
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

            GameObject dropdownRow = UIFactory.CreateHorizontalGroup(ContentRoot, "Dropdowns", true, false, true, true, 4, bgColor: new Color(0.07f, 0.07f, 0.07f, 1));

            UIFactory.CreateDropdown(dropdownRow, "Rounds", out Dropdown dropdown, "", 14, RoundSelected);
            dropdown.AddOptions(rounds);
            UIFactory.SetLayoutElement(dropdown.gameObject, minHeight: 25, minWidth: 400, flexibleWidth: 0, flexibleHeight: 0);

            UIFactory.CreateDropdown(dropdownRow, "Round Variation", out roundVariationDropdown, "", 14, VariationSelected);
            roundVariationDropdown.options.Add(new Dropdown.OptionData("Choose a Variation"));
            UIFactory.SetLayoutElement(roundVariationDropdown.gameObject, minHeight: 25, minWidth: 400, flexibleWidth: 0, flexibleHeight: 0);

            GameObject buttonRow = UIFactory.CreateHorizontalGroup(ContentRoot, "Buttons", true, false, true, true, 4, bgColor: new Color(0.07f, 0.07f, 0.07f, 1));

            ButtonRef loadRoundButton = UIFactory.CreateButton(buttonRow, "Load Round Button", "Load Round");
            UIFactory.SetLayoutElement(loadRoundButton.Component.gameObject, minHeight: 25, minWidth: 266, flexibleWidth: 0, flexibleHeight: 0);
            loadRoundButton.OnClick += () =>
            {
                if (variationsForSelectedRound != null)
                {
                    LoaderBehaviour.instance.LoadRound(variationsForSelectedRound[selectedVariationIndex]);
                }
            };

            ButtonRef skinPresetsButton = UIFactory.CreateButton(buttonRow, "Skin Presets Button", "Skin Presets");
            UIFactory.SetLayoutElement(skinPresetsButton.Component.gameObject, minHeight: 25, minWidth: 266, flexibleWidth: 0, flexibleHeight: 0);
            skinPresetsButton.OnClick += SkinPresetsUI.instance.Toggle;

            ButtonRef loadRandomRoundButton = UIFactory.CreateButton(buttonRow, "Load Random Round Button", "Load Random Round");
            UIFactory.SetLayoutElement(loadRandomRoundButton.Component.gameObject, minHeight: 25, minWidth: 266, flexibleWidth: 0, flexibleHeight: 0);
            loadRandomRoundButton.OnClick += LoadRandomRound;
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
            rounds.Insert(0, "Choose a Round");
        }

        string RemoveIndentation(string inputString)
        {
            string noTagsString = Regex.Replace(inputString, "<.*?>", string.Empty);
            return Regex.Replace(noTagsString, " {2,}", " ");
        }

        void RoundSelected(int roundIndex)
        {
            selectedRoundIndex = roundIndex;
            roundVariationDropdown.options.Clear();
            if (roundIndex > 0)
            {
                variationsForSelectedRound = sceneNameAndRoundVariations[roundNameToSceneName[rounds[roundIndex]]];
                roundVariationDropdown.AddOptions(variationsForSelectedRound);
            }
        }

        void VariationSelected(int variationIndex)
        {
            selectedVariationIndex = variationIndex;
        }

        void LoadRandomRound()
        {
            int randomRoundIndex = UnityEngine.Random.Range(0, usableRounds.Count);
            LoaderBehaviour.instance.LoadRound(usableRounds[randomRoundIndex]);
        }
    }
}
