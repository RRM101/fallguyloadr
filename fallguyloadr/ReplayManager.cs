using FGClient.UI;
using FGClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fallguyloadr.JSON;
using System.Text.Json;
using System.Security.Cryptography;
using UnityEngine;
using BepInEx;
using FG.Common.CMS;
using FG.Common;
using System.IO;
using System.Text.RegularExpressions;
using Il2CppSystem.Xml.Schema;

namespace fallguyloadr
{
    public class ReplayManager
    {
        private static ReplayManager instance;

        public static ReplayManager Instance => instance;

        public ReplayManager()
        {
            if (instance == null)
            {
                instance = this;
            }
        }
        
        private string currentReplayCalculatedChecksum;

        public bool startPlaying = false;
        public Replay currentReplay;
        public bool hashCheckFailed 
        {
            get
            {
                if (currentReplay != null)
                    return currentReplay.Checksum != currentReplayCalculatedChecksum;
                return false;
            }
        }

        ClientGameManager cgm;

        public void PlayReplay(Replay replay)
        {
            if (replay != null && LoaderManager.instance.canLoadLevel)
            {
                StopPlayingReplay();
                currentReplay = replay;
                currentReplayCalculatedChecksum = CalculateReplayChecksum(replay);
                LoaderManager.instance.LoadRound(currentReplay.RoundID, currentReplay.Seed);
                FGChaos.Plugin.tempDisable = true;
                Plugin.Logs.LogInfo("Playing replay");
            }
        }

        public void StopPlayingReplay()
        {
            if (currentReplay != null)
            {
                Plugin.Logs.LogInfo("Stopping replay");
            }
            startPlaying = false;
            SetUIState(false);
            currentReplay = null;
            currentReplayCalculatedChecksum = "";
            cgm = null;
            FGChaos.Plugin.tempDisable = false;
        }

        public void RoundStarted()
        {
            GlobalGameStateClient.Instance.GameStateView.GetLiveClientGameManager(out cgm);
            if (currentReplay != null)
            {
                SetUIState(true);
            }
            startPlaying = true;
        }

        void SetUIState(bool spectator)
        {
            if (cgm != null)
            {
                GameplayQualificationStatusPromptViewModel gameplayQualificationStatus = cgm._inGameUiManager._switchableView._views[4].GetComponentInChildren<GameplayQualificationStatusPromptViewModel>();
                gameplayQualificationStatus.UpdateDisplay(true, false);
                gameplayQualificationStatus.transform.FindChild("GameObject/LowerLayoutRoot/QualifiedLayout/Text").GetComponent<LocalisedStaticLabel>().SetLocalisationKey("fallguyloadr_replay");
                cgm._inGameUiManager._switchableView._views[4].GetComponentInChildren<NameTagViewModel>().UpdateDisplay(GlobalGameStateClient.Instance.GetLocalPlayerKey(), "", GlobalGameStateClient.Instance._playerProfile.CustomisationSelections);

                if (spectator)
                {
                    cgm._inGameUiManager._switchableView.SetViewVisibility(2, false);
                    cgm._inGameUiManager._switchableView.SetViewVisibility(4, true);
                }
                else
                {
                    cgm._inGameUiManager._switchableView.SetViewVisibility(2, true);
                    cgm._inGameUiManager._switchableView.SetViewVisibility(4, false);
                }
            }
        }

        static string RemoveIndentation(string inputString)
        {
            string noTagsString = Regex.Replace(inputString, "<.*?>", string.Empty);
            return Regex.Replace(noTagsString, " {2,}", " ");
        }

        public static void SaveReplay(Vector3[] positions_, Quaternion[] rotations_)
        {
            string datetime = $"{DateTime.Now.Day}-{DateTime.Now.Month}-{DateTime.Now.Year} {DateTime.Now.Hour}-{DateTime.Now.Minute}-{DateTime.Now.Second}";
            string filepath = $"{Paths.PluginPath}/fallguyloadr/Replays/{RemoveIndentation(CMSLoader.Instance.CMSData.Rounds[NetworkGameData.currentGameOptions_._roundID].DisplayName.Text)} - {datetime}.json";

            if (!File.Exists(filepath))
            {
                float[][] positions = new float[positions_.Length][];
                float[][] rotations = new float[rotations_.Length][];
                //List<float[]> rotationsList = new();

                for (int i = 0; i < positions_.Length; i++)
                {
                    Vector3 position = positions_[i];
                    positions[i] = new float[] { position.x, position.y, position.z };
                }

                for (int i = 0; i < rotations_.Length; i++)
                {
                    Quaternion rotation = rotations_[i];
                    rotations[i] = new float[] { rotation.x, rotation.y, rotation.z, rotation.w };
                }

                Replay replay = new Replay();
                replay.Version = Plugin.version;
                replay.Seed = LoaderManager.seed;
                replay.RoundID = NetworkGameData.currentGameOptions_._roundID;
                replay.UsingV11Physics = FallGuyBehaviour.usingV11Physics;
                replay.UsingFGChaos = FGChaos.ChaosManager.chaosInstance != null;
                replay.Positions = positions;
                replay.Rotations = rotations;
                replay.Checksum = CalculateReplayChecksum(replay);

                string replayJson = JsonSerializer.Serialize<Replay>(replay);

                File.WriteAllText(filepath, replayJson);
            }
            else
            {
                Plugin.Logs.LogError($"Could not save replay, \"{filepath}\" exists.");
                ModalMessageData modalMessageData = new ModalMessageData()
                {
                    Title = "fallguyloadr - Replay",
                    Message = $"Could not save Replay because a file with the same name exists.",
                    LocaliseTitle = UIModalMessage.LocaliseOption.NotLocalised,
                    LocaliseMessage = UIModalMessage.LocaliseOption.NotLocalised,
                    ModalType = UIModalMessage.ModalType.MT_BLOCKING,
                };

                PopupManager.Instance.Show(PopupInteractionType.Error, modalMessageData);
            }
        }

        public static string CalculateReplayChecksum(Replay replay)
        {
            Replay replayNoChecksum = new();
            replayNoChecksum.Positions = replay.Positions;
            replayNoChecksum.Rotations = replay.Rotations;
            replayNoChecksum.Version = replay.Version;
            replayNoChecksum.Seed = replay.Seed;
            replayNoChecksum.RoundID = replay.RoundID;
            replayNoChecksum.UsingFGChaos = replay.UsingFGChaos;

            SHA256 sha256 = SHA256.Create();

            byte[] sha256bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(replayNoChecksum)));

            StringBuilder sb = new StringBuilder();

            foreach (byte b in sha256bytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
