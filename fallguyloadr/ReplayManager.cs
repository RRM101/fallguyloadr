using FGClient.UI;
using FGClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fallguyloadr.JSON;

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

        public bool startPlaying = false;
        public Replay currentReplay;

        ClientGameManager cgm;

        public void PlayReplay(Replay replay)
        {
            if (replay != null && LoaderBehaviour.instance.canLoadLevel)
            {
                currentReplay = replay;
                LoaderBehaviour.instance.LoadRound(currentReplay.RoundID, currentReplay.Seed);
            }
        }

        public void StopPlayingReplay()
        {
            startPlaying = false;
            SetUIState(false);
            currentReplay = null;
            cgm = null;
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
                cgm._inGameUiManager._switchableView._views[4].GetComponentInChildren<GameplayQualificationStatusPromptViewModel>().UpdateDisplay(true, false);
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
    }
}
