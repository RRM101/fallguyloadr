using BepInEx;
using FG.Common;
using FG.Common.Character;
using FG.Common.CMS;
using FGClient;
using FGClient.CatapultServices;
using FGClient.Challenges;
using FGClient.UI;
using HarmonyLib;
using Levels.SnowballSurvival;
using System;
using UnityEngine;

namespace fallguyloadr
{
    public class Patches
    {
        [HarmonyPatch(typeof(TitleScreenViewModel), "TryShowTermsOfServicePopup")]
        [HarmonyPatch(typeof(ChallengesManager), "TryUpdatePlayerChallengeGroups")]
        [HarmonyPatch(typeof(CatapultServicesManager), "ShowReloadGamePopup")]
        [HarmonyPatch(typeof(EOSManagerFG), "OnCheckToTriggerMissingFilesPopup")]
        [HarmonyPatch(typeof(COMMON_SnowballSurvivalBall), "Awake")]
        [HarmonyPatch(typeof(InGameUiManager), "Cleanup")]
        [HarmonyPatch(typeof(ClientPlayerManager), "OnPlayerSpawned")]
        [HarmonyPrefix]
        static bool donothing()
        {
            return false;
        }

        [HarmonyPatch(typeof(ClientGameStateView), "RoundRandomSeed", MethodType.Getter)]
        [HarmonyPrefix]
        static bool RoundRandomSeed(ClientGameStateView __instance, ref int __result)
        {
            __result = DateTime.Now.Millisecond;
            return false;
        }

        [HarmonyPatch(typeof(ClientGameManager), "GameLevelLoaded")]
        [HarmonyPostfix]
        static void ClientGameManagerGameLevelLoaded(ClientGameManager __instance)
        {
            __instance.SetupPlayerUpdateManagerAndRegister(LoaderBehaviour.instance.fallguy, true);
            LoaderBehaviour.instance.fallguy.SetTeamID(1);
            LoaderBehaviour.instance.fallguy.SpeedBoostManager.SetAuthority(true);
            LoaderBehaviour.instance.fallguy.SpeedBoostManager.SetCharacterController(LoaderBehaviour.instance.fallguy);
            CustomisationManager.Instance.ApplyCustomisationsToFallGuy(LoaderBehaviour.instance.fallguy.gameObject, GlobalGameStateClient.Instance.PlayerProfile.CustomisationSelections, -1);

            __instance._clientPlayerManager.ClearAllPlayers();

            NetworkPlayerDataClient networkPlayerDataClient = new NetworkPlayerDataClient();
            networkPlayerDataClient.fgcc = LoaderBehaviour.instance.fallguy;
            networkPlayerDataClient.isLocalPlayer = true;
            networkPlayerDataClient.isParticipant = true;
            networkPlayerDataClient.TeamID = 1;
            networkPlayerDataClient.playerKey = "win_" + GlobalGameStateClient.Instance.PlayerProfile.PlatformAccountName;
            networkPlayerDataClient.platformID = "win";
            networkPlayerDataClient.partyId = "";
            networkPlayerDataClient.SquadID = 0;
            networkPlayerDataClient.objectNetID = LoaderBehaviour.instance.netObject.NetID;

            PlayerMetadata playerMetadata = new PlayerMetadata(GlobalGameStateClient.Instance.PlayerProfile.CustomisationSelections, -1, "win_" + GlobalGameStateClient.Instance.PlayerProfile.PlatformAccountName, "win", true);

            __instance._clientPlayerManager._playerMetadata.Add(0, playerMetadata);
            __instance._clientPlayerManager._players.Add(networkPlayerDataClient);
            __instance._clientPlayerManager._playerIdIndex.Add(0, networkPlayerDataClient);
            __instance._clientPlayerManager._playerNetIdIndex.Add(LoaderBehaviour.instance.netObject.NetID, networkPlayerDataClient);
            __instance._clientPlayerManager.LocalPlayer_ReadOnly = networkPlayerDataClient;

            __instance.AddPlayerIdentification("win_"+GlobalGameStateClient.Instance.PlayerProfile.PlatformAccountName, "win", LoaderBehaviour.instance.fallguy, true, "", 0);

            __instance.AddPlayerCollidersToMap(LoaderBehaviour.instance.fallguy);
            LoaderBehaviour.instance.StartRound();

            __instance._trackPlayerSpawn(LoaderBehaviour.instance.netObject);
            LoaderBehaviour.instance.FixObstaclesOnLevelLoad();
        }

        [HarmonyPatch(typeof(AFKManager), "Start")]
        [HarmonyPrefix]
        static bool AFKManagerStart(AFKManager __instance)
        {
            GameObject.Destroy(__instance.gameObject);
            return false;
        }

        [HarmonyPatch(typeof(GameplayScoringViewModel), "Initialise")]
        [HarmonyPrefix]
        static bool GameplayScoringViewModelInitialise(GameplayScoringViewModel __instance)
        {
            GameObject.Destroy(__instance.gameObject);
            return false;
        }

        [HarmonyPatch(typeof(ClientGameManager), "OnIntroCountdownEnded")]
        [HarmonyPrefix]
        static bool ClientGameManagerOnIntroCountdownEnded(ClientGameManager __instance)
        {
            LoaderBehaviour.instance.FixObstacles();
            LoaderBehaviour.instance.canLoadLevel = true;
            return true;
        }

        [HarmonyPatch(typeof(InGameUiManager), "Shutdown")]
        [HarmonyPrefix]
        static bool InGameUiManagerShutdown(InGameUiManager __instance)
        {
            __instance.HidePopups();
            __instance.HideScreen();
            return false;
        }

        [HarmonyPatch(typeof(InGameIntroCameraState), "HandleIntroCamsStarted")]
        [HarmonyPrefix]
        static bool InGameIntroCameraStateHandleIntroCamsStarted(InGameIntroCameraState __instance)
        {            
            GlobalGameStateClient.Instance.GameStateView.GetLiveClientGameManager(out ClientGameManager cgm);
            __instance._clientGameManager = cgm;

            return true;
        }

        [HarmonyPatch(typeof(LeaveMatchPopupManager), "OnClose")]
        [HarmonyPrefix]
        static bool LeaveMatchPopupManager(LeaveMatchPopupManager __instance, bool wasOk)
        {
            AudioManager.PlayOneShot(AudioManager.EventMasterData.GenericCancel);
            if (wasOk)
            {
                __instance.CloseScreen();
                AudioManager.SetGlobalParam(AudioManager.EventMasterData.InGameMenuParam, 0);
                GlobalGameStateClient.Instance._gameStateMachine.ReplaceCurrentState(new StateMainMenu(GlobalGameStateClient.Instance._gameStateMachine, GlobalGameStateClient.Instance.CreateClientGameStateData(), false).Cast<GameStateMachine.IGameState>());
                LoaderBehaviour.instance.canLoadLevel = false;
            }
            else
            {
                __instance.CloseScreen();
            }
            return false;
        }

        [HarmonyPatch(typeof(CMSLoader), "InitItemsFromContent")]
        [HarmonyPostfix]
        static void CMSLoaderInitItemsFromContent(CMSLoader __instance)
        {
            LoaderBehaviour.instance.HandleCMSDataParsedEvent();
        }

        [HarmonyPatch(typeof(CatapultServicesManager), "OnLoginFailed")]
        [HarmonyPrefix]
        static bool CatapultServicesManagerOnLoginFailed()
        {
            LoaderBehaviour.instance.Login();
            return false;
        }

        [HarmonyPatch(typeof(PlayerProfile), "PlatformAccountName", MethodType.Getter)]
        [HarmonyPatch(typeof(IdentityProviderUtils), "GetEosOrGeneratedName")]
        [HarmonyPrefix]
        static bool IdentityProviderUtilsGetEosOrGeneratedName(ref string __result)
        {
            __result = Plugin.Username.Value;
            return false;
        }

        [HarmonyPatch(typeof(LevelEditorMenuViewModel), "RequestCreateScreen")]
        [HarmonyPrefix]
        static bool LevelEditorMenuViewModelOnCreateScreenResponse(LevelEditorMenuViewModel __instance)
        {
            __instance.ShowCreateScreen();
            return false;
        }

        [HarmonyPatch(typeof(CabinModeStatusHelper), "IsUserInCabinMode")]
        [HarmonyPrefix]
        static bool CabinModeStatusHelper(ref bool __result)
        {
            __result = false;
            return false;
        }

        [HarmonyPatch(typeof(GamefuelContentService), "GetCachePath")]
        [HarmonyPrefix]
        static bool GamefuelContentServiceGetCachePath(string filename, ref string __result)
        {
            __result = $"{Paths.PluginPath}/fallguyloadr/Assets/{filename}.gdata";
            return false;
        }
    }
}
