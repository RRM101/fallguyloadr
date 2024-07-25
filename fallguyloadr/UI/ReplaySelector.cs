﻿using BepInEx;
using fallguyloadr.JSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Panels;

namespace fallguyloadr.UI
{
    public class ReplaySelector : PanelBase
    {
        public ReplaySelector(UIBase owner) : base(owner)
        {
            if (instance == null)
            {
                instance = this;
            }
        }

        public static ReplaySelector instance;

        public override string Name => "Replays";
        public override int MinWidth => 505;
        public override int MinHeight => 160;
        public override Vector2 DefaultAnchorMin => new(0.25f, 0.25f);
        public override Vector2 DefaultAnchorMax => new(0.75f, 0.75f);

        Il2CppSystem.Collections.Generic.List<string> Replays = new();
        Dropdown replaysDropdown;

        Replay selectedReplay;

        Text currentReplayText;

        Text replayDurationText;
        Text replayRoundIDText;
        Text replaySeedText;

        protected override void ConstructPanelContent()
        {
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, MinWidth);
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, MinHeight);
            Dragger.OnEndResize();

            GameObject selectionRow = UIFactory.CreateHorizontalGroup(ContentRoot, "Selection Row", true, false, true, true, 2, bgColor: new Color(0.07f, 0.07f, 0.07f, 1));

            UIFactory.CreateDropdown(selectionRow, "Replays", out replaysDropdown, "", 14, SelectReplay);
            UIFactory.SetLayoutElement(replaysDropdown.gameObject, 350, 25, 0, 0);

            RefreshReplays();

            ButtonRef playButton = UIFactory.CreateButton(selectionRow, "Play Replay Button", "Play");
            UIFactory.SetLayoutElement(playButton.Component.gameObject, 100, 25, 0, 0);
            playButton.OnClick += PlayReplay;

            GameObject selectionNameCollumn = UIFactory.CreateVerticalGroup(ContentRoot, "", false, false, true, true, 5, new Vector4(5, 5, 5, 5), new Color(0.07f, 0.07f, 0.07f, 1));

            currentReplayText = UIFactory.CreateLabel(selectionNameCollumn, "Replay Name Text", "");
            UIFactory.SetLayoutElement(currentReplayText.gameObject);

            GameObject selectionsCollumn = UIFactory.CreateVerticalGroup(ContentRoot, "", false, false, true, true, 5, new Vector4(5, 5, 5, 5), new Color(0.07f, 0.07f, 0.07f, 1));

            replayDurationText = UIFactory.CreateLabel(selectionsCollumn, "Replay Duration Text", "");
            replayRoundIDText = UIFactory.CreateLabel(selectionsCollumn, "Replay RoundID Text", "");
            replaySeedText = UIFactory.CreateLabel(selectionsCollumn, "Replay Seed Text", "");

            ChangeReplayInfo(null, 0);
        }

        public override void Toggle()
        {
            RefreshReplays();
            base.Toggle();
        }

        void RefreshReplays()
        {
            Replays.Clear();
            Replays.Add("Select a Replay");

            string[] fileNames = Directory.GetFiles($"{Paths.PluginPath}/fallguyloadr/Replays");
            foreach (string fileName in fileNames)
            {
                if (fileName.EndsWith(".json"))
                {
                    Replays.Add(Path.GetFileNameWithoutExtension(fileName));
                }
            }

            replaysDropdown.ClearOptions();
            replaysDropdown.AddOptions(Replays);
        }

        void SelectReplay(int index)
        {
            if (index > 0)
            {
                selectedReplay = JsonSerializer.Deserialize<Replay>(File.ReadAllText($"{Paths.PluginPath}/fallguyloadr/Replays/{Replays[index]}.json"));
            }
            else
            {
                selectedReplay = null;
            }
            ChangeReplayInfo(selectedReplay, index);
        }

        void ChangeReplayInfo(Replay replay, int index)
        {
            currentReplayText.text = index > 0 ? Replays[index] : "None";

            if (replay != null)
            {
                TimeSpan timespan = TimeSpan.FromSeconds(replay.Positions.Length / 50);
                string durationText;

                if (timespan.Minutes > 0)
                {
                    durationText = $"{timespan.Minutes}m {timespan.Seconds}s";
                }
                else
                {
                    durationText = $"{timespan.Seconds}s";
                }

                replayDurationText.text = $"Duration: {durationText}";
                replayRoundIDText.text = $"Round ID: {replay.RoundID}";
                replaySeedText.text = $"Seed: {replay.Seed}";
            }
            else
            {
                replayDurationText.text = $"Duration:";
                replayRoundIDText.text = $"Round ID:";
                replaySeedText.text = $"Seed:";
            }
        }

        void PlayReplay()
        {
            if (selectedReplay != null && LoaderBehaviour.instance.canLoadLevel)
            {
                LoaderBehaviour.instance.currentReplay = selectedReplay;
                LoaderBehaviour.instance.LoadRound(selectedReplay.RoundID, selectedReplay.Seed);
            }
        }
    }
}
