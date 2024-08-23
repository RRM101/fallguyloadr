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
            if (replay != null && LoaderBehaviour.instance.canLoadLevel)
            {
                StopPlayingReplay();
                currentReplay = replay;
                currentReplayCalculatedChecksum = CalculateReplayChecksum(replay);
                LoaderBehaviour.instance.LoadRound(currentReplay.RoundID, currentReplay.Seed);
                FGChaos.Plugin.tempDisable = true;
            }
        }

        public void StopPlayingReplay()
        {
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

        public static string CalculateReplayChecksum(Replay replay)
        {
            List<object> data = new()
            {
                replay.Version,
                replay.Seed,
                replay.RoundID,
                replay.UsingV11Physics,
                replay.UsingFGChaos
            };

            foreach (float[] position in replay.Positions)
            {
                data.Add(position);
            }

            foreach (float[] rotation in replay.Rotations)
            {
                data.Add(rotation);
            }

            SHA256 sha256 = SHA256.Create();

            byte[] sha256bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data.ToArray())));

            StringBuilder sb = new StringBuilder();

            foreach (byte b in sha256bytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
