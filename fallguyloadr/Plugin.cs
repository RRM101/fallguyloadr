using BepInEx;
using BepInEx.Unity.IL2CPP;
using FG.Common;
using FG.Common.Character.MotorSystem;
using FG.Common.CMS;
using FGClient;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System.Collections;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using FMODUnity;
using Levels.PixelPerfect;
using Levels.Rollout;
using Levels.ScoreZone;
using System.Collections.Generic;
using Levels.DoorDash;
using System.Linq;
using FG.Common.LevelEvents;
using FGClient.UI;
using Levels.Progression;
using FG.Common.Character;
using UniverseLib.UI;
using FGClient.CatapultServices;

namespace fallguyloadr
{
    [BepInPlugin("org.rrm1.fallguyloadr", "fallguyloadr", version)]
    public class Plugin : BasePlugin
    {
        public const string version = "0.9.1";

        public override void Load()
        {
            ClassInjector.RegisterTypeInIl2Cpp<LoaderBehaviour>();
            ClassInjector.RegisterTypeInIl2Cpp<FallGuyBehaviour>();
            ClassInjector.RegisterTypeInIl2Cpp<Fixes.PixelPerfectBoardFix>();

            Harmony.CreateAndPatchAll(typeof(Patches));
            Harmony.CreateAndPatchAll(typeof(IsGameServerPatches));
            Harmony.CreateAndPatchAll(typeof(CosmeticsPatches));

            GameObject obj = new GameObject("Loader Behaviour");
            GameObject.DontDestroyOnLoad(obj);
            obj.hideFlags = HideFlags.HideAndDontSave;
            obj.AddComponent<LoaderBehaviour>();

            Log.LogInfo($"Plugin fallguy loadr is loaded!");            
        }
    }

    public class LoaderBehaviour : MonoBehaviour
    {
        public static LoaderBehaviour instance;
        public FallGuysCharacterController fallguy;
        StateGameLoading gameLoading;
        public MPGNetObject netObject;
        public ClientGameManager cgm;
        UIBase UI;
        public bool canLoadLevel = true;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(this);
            }

            UniverseLib.Universe.Init(1, null, null, new()
            {
                Disable_EventSystem_Override = false,
                Force_Unlock_Mouse = true,
                Unhollowed_Modules_Folder = Paths.BepInExRootPath + "/interop"
            });
        }

        public void OnEnable()
        {
            SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>)OnSceneLoaded);
        }

        public void HandleCMSDataParsedEvent()
        {
            if (UI == null)
            {
                UI = UniversalUI.RegisterUI("org.rrm1.fallguyloadr", null);
                new SkinPresetsUI(UI);
                SkinPresetsUI.instance.SetActive(false);
                new LoaderUI(UI);
                UniverseLib.Config.ConfigManager.Force_Unlock_Mouse = false;
                AddCMSStringKeys();
            }
        }

        public void AddCMSStringKeys()
        {
            Dictionary<string, string> stringsToAdd = new Dictionary<string, string>()
            {
                {"fallguyloadr_create", "CREATE"},
                {"fallguyloadr_skin_presets_create_popup_additional_message", "Your current Customisations will be used."}
            };

            foreach (var toAdd in stringsToAdd) AddNewStringToCMS(toAdd.Key, toAdd.Value);
        }

        public void AddNewStringToCMS(string key, string value)
        {
            if (!CMSLoader.Instance._localisedStrings._localisedStrings.ContainsKey(key))
            {
                CMSLoader.Instance._localisedStrings._localisedStrings.Add(key, value);
            }
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.name.Contains("Fraggle") || !scene.name.Contains("Editor"))
            {
                if (scene.name.StartsWith("FallGuy_"))
                {
                    cgm = gameLoading._clientGameManager;
                    FallGuysCharacterController[] fallGuysCharacters = Resources.FindObjectsOfTypeAll<FallGuysCharacterController>();
                    foreach (FallGuysCharacterController fallGuysCharacter in fallGuysCharacters)
                    {
                        if (fallGuysCharacter.gameObject.name == "FallGuy")
                        {
                            FallGuysCharacterController fallguy_ = fallGuysCharacter;
                            fallguy_.GetComponent<MotorAgent>()._motorFunctionsConfig = MotorAgent.MotorAgentConfiguration.Offline;
                            fallguy = Instantiate(fallguy_).GetComponent<FallGuysCharacterController>();
                        }
                    }

                    MultiplayerStartingPosition startingPosition = FindObjectOfType<MultiplayerStartingPosition>();

                    fallguy.gameObject.AddComponent<FallGuyBehaviour>();

                    fallguy.IsControlledLocally = true;
                    fallguy.IsLocalPlayer = true;
                    fallguy.gameObject.transform.position = startingPosition.transform.position;
                    fallguy.gameObject.transform.rotation = startingPosition.transform.rotation;
                    netObject = fallguy.gameObject.AddComponent<MPGNetObject>();
                    netObject.NetID = new MPGNetID(1001);
                    netObject.FGCharacterController = fallguy;
                    netObject.pNetTX_ = new MPGNetTransform(netObject, null, null, null, false, 0);
                    fallguy._pNetObject = netObject;
                    LoadBank("BNK_SFX_PowerUp");
                    LoadBank("BNK_SFX_PowerUp_RollingBall");
                }
            }
        }

        public void FixObstacles()
        {
            ScoreZoneManager scoreZoneManager = FindObjectOfType<ScoreZoneManager>();
            PixelPerfectManager pixelPerfectManager = FindObjectOfType<PixelPerfectManager>();
            MPGNetObjectBase[] networkAwareGenericObjects = FindObjectsOfType<MPGNetObjectBase>();
            List<OfflineGrabTargetID> networkAwareGrabTargets = new List<OfflineGrabTargetID>();

            COMMON_FakeDoorRandomiser[] doorRandomisers = FindObjectsOfType<COMMON_FakeDoorRandomiser>();
            foreach (COMMON_FakeDoorRandomiser doorRandomiser in doorRandomisers)
            {
                doorRandomiser.InitializeServerSideData();
                doorRandomiser.CreateBreakableDoors();
            }


            if (scoreZoneManager != null)
            {
                SpawnableCollectable[] bubbles = FindObjectsOfType<SpawnableCollectable>();
                foreach (SpawnableCollectable bubble in bubbles)
                {
                    bubble.Spawn();
                }
            }

            if (pixelPerfectManager != null)
            {
                pixelPerfectManager._config = pixelPerfectManager._smallSquadConfig;
                pixelPerfectManager.Init();
                PixelPerfectBoard[] pixelPerfectBoards = pixelPerfectManager._boards;

                foreach (PixelPerfectBoard pixelPerfectBoard in pixelPerfectBoards)
                {
                    /*pixelPerfectBoard.enabled = true;
                    pixelPerfectBoard.RegisterRemoteMethods();*/
                    pixelPerfectBoard.gameObject.AddComponent<Fixes.PixelPerfectBoardFix>();
                }

                pixelPerfectManager.BeginGame();
            }

            foreach (MPGNetObjectBase networkAwareGenericObject in networkAwareGenericObjects)
            {
                OfflineGrabTargetID netObject = networkAwareGenericObject.gameObject.AddComponent<OfflineGrabTargetID>();
                networkAwareGrabTargets.Add(netObject);
            }

            if (networkAwareGrabTargets.Count > 0)
            {
                HashSet<int> uniqueIDs = new HashSet<int>();
                while (uniqueIDs.Count < networkAwareGrabTargets.Count)
                {
                    int randomID = UnityEngine.Random.RandomRange(42, 1000);
                    uniqueIDs.Add(randomID);
                }

                int[] uniqueIDArray = uniqueIDs.ToArray();

                int index = 0;
                foreach (OfflineGrabTargetID offlineGrabTargetID in networkAwareGrabTargets)
                {
                    offlineGrabTargetID._hashID = (uint)uniqueIDArray[index];
                    offlineGrabTargetID.Type = OfflineGrabTargetID.OfflineGrabTargetIDType.Mantle | OfflineGrabTargetID.OfflineGrabTargetIDType.Grab;
                    index++;
                }
            }
        }

        public void FixObstaclesOnLevelLoad()
        {            
            PlayerRatioedBulkItemSpawner bulkItemSpawner = FindObjectOfType<PlayerRatioedBulkItemSpawner>();

            if (LevelEventManager.Instance != null)
            {
                var BlastEventHandler = LevelEventManager.Instance.GetHandlerAt(1);
                BlastEventHandler.enabled = true;
                BlastEventHandler.RegisterRemoteMethods();
            }

            RolloutManager rolloutManager = FindObjectOfType<RolloutManager>();
            if (rolloutManager != null)
            {
                int index = 0;
                foreach (RolloutManager.RingSegmentSchema ringSegment in rolloutManager._ringSegmentSchemas)
                {
                    rolloutManager.InstantiateRing(index, 1);
                    index++;
                }
            }

            if (bulkItemSpawner != null)
            {
                Transform ItemParent;

                if (bulkItemSpawner.ItemParent != null)
                {
                    ItemParent = bulkItemSpawner.ItemParent;
                }
                else
                {
                    ItemParent = bulkItemSpawner.ItemParents[0];
                }

                int spawns = ItemParent.GetChildCount();

                for (int i = 0; i < spawns; i++)
                {
                    int randomnumber = UnityEngine.Random.Range(0, spawns - 1);
                    Transform spawn = ItemParent.GetChild(randomnumber);
                    Instantiate(bulkItemSpawner.ItemPrefab, spawn.position, spawn.rotation);
                }
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                UI.Enabled = !UI.Enabled;
                CursorManager.Instance.OnApplicationFocus(true);
            }
        }

        public void StartRound()
        {
            StartCoroutine(StartRoundCoroutine().WrapToIl2Cpp());
        }

        IEnumerator StartRoundCoroutine()
        {
            yield return new WaitForSeconds(5);
            gameLoading.OnServerRequestStartIntroCameras();
            yield return new WaitForSeconds(gameLoading._clientGameManager.CameraDirector.IntroCamerasDuration);

            GameMessageServerStartGame startGameMessage = new GameMessageServerStartGame();
            startGameMessage.StartRoundTime = 0;

            startGameMessage.EndRoundTime = 150;

            gameLoading.HandleGameServerStartGame(startGameMessage);
        }

        public void LoadRound(string round_id)
        {
            if (canLoadLevel)
            {
                Round round = CMSLoader.Instance.CMSData.Rounds[round_id];

                if (SceneManager.GetActiveScene().name == round.GetSceneName())
                {
                    SceneManager.LoadScene("Transition");
                    /*ModalMessageData modalMessageData = new ModalMessageData() // this is for v10.9
                    {
                        Title = "fallguyloadr - INFO",
                        Message = "Cannot load level with the same scene.",
                        LocaliseTitle = UIModalMessage.LocaliseOption.NotLocalised,
                        LocaliseMessage = UIModalMessage.LocaliseOption.NotLocalised,
                        ModalType = UIModalMessage.ModalType.MT_OK
                    };

                    PopupManager.Instance.Show(PopupInteractionType.Info, modalMessageData);

                    return;*/
                }

                NetworkGameData.SetGameOptionsFromRoundData(round, null);

                ShowsManager.Instance.SelectedGameMode = ShowsManager.GameMode.PrivateLobby;

                if (cgm != null)
                {
                    try
                    {
                        cgm.Shutdown();
                    }
                    catch (Exception e)
                    {
                        LevelUnloadError(e);
                    }
                }

                gameLoading = new StateGameLoading(GlobalGameStateClient.Instance._gameStateMachine, GlobalGameStateClient.Instance.CreateClientGameStateData(), GamePermission.Player, false, false);
                GlobalGameStateClient.Instance._gameStateMachine.ReplaceCurrentState(gameLoading.Cast<GameStateMachine.IGameState>());
                canLoadLevel = false;
            }
            else
            {
                ModalMessageData modalMessageData = new ModalMessageData()
                {
                    Title = "fallguyloadr - INFO",
                    Message = "Cannot load level. Please wait for the current round to finish loading.",
                    LocaliseTitle = UIModalMessage.LocaliseOption.NotLocalised,
                    LocaliseMessage = UIModalMessage.LocaliseOption.NotLocalised,
                    ModalType = UIModalMessage.ModalType.MT_OK
                };

                PopupManager.Instance.Show(PopupInteractionType.Info, modalMessageData);
            }
        }

        public void LoadRound(Round round)
        {
            LoadRound(round.Id);
        }

        void LevelUnloadError(Exception e)
        {
            ModalMessageData modalMessageData = new ModalMessageData()
            {
                Title = "fallguyloadr - ERROR",
                Message = $"There was a problem while unloading the level, you may still continue, but the game maybe bugged.\n\nThe Error:{e.Message}",
                LocaliseTitle = UIModalMessage.LocaliseOption.NotLocalised,
                LocaliseMessage = UIModalMessage.LocaliseOption.NotLocalised,
                ModalType = UIModalMessage.ModalType.MT_OK
            };

            PopupManager.Instance.Show(PopupInteractionType.Error, modalMessageData);
        }

        public void Respawn(CheckpointManager checkpointManager)
        {
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;

            void RespawnAtStart()
            {
                MultiplayerStartingPosition startingPosition = FindObjectOfType<MultiplayerStartingPosition>();
                position = startingPosition.transform.position;
                rotation = startingPosition.transform.rotation;
            }

            if (checkpointManager != null)
            {
                if (checkpointManager.NetIDToCheckpointMap.ContainsKey(netObject.NetID))
                {
                    CheckpointZone checkpoint = checkpointManager._checkpointZones[0];
                    uint checkpointID = checkpointManager._netIDToCheckpointMap[netObject.NetID];
                    foreach (CheckpointZone checkpointZone in checkpointManager._checkpointZones)
                    {
                        if (checkpointZone.UniqueId == checkpointID)
                        {
                            checkpoint = checkpointZone;
                            break;
                        }
                    }
                    checkpoint.GetNextSpawnPositionAndRotation(out position, out rotation);
                }
                else
                {
                    RespawnAtStart();
                }
            }
            else
            {
                RespawnAtStart();
            }

            fallguy.MotorAgent.GetMotorFunction<MotorFunctionTeleport>().RequestTeleport(position, rotation);
        }

        public static void LoadBank(string bank)
        {
            if (!RuntimeManager.HasBankLoaded(bank))
            {
                RuntimeManager.LoadBank(bank);
                RuntimeManager.LoadBank($"{bank}.assets");
            }
        }

        public static void UnloadBank(string bank)
        {
            if (RuntimeManager.HasBankLoaded(bank))
            {
                RuntimeManager.UnloadBank(bank);
                RuntimeManager.UnloadBank($"{bank}.assets");
            }
        }

        public void Login()
        {
            StartCoroutine(LoginRoutine().WrapToIl2Cpp());
        }

        IEnumerator LoginRoutine()
        {
            if (CMSLoader.Instance.CMSData == null)
            {
                PlayerTargetSettings.HardCurrencyEnabled = true;
                PlayerTargetSettings.QuitAnywhereEnabled = true;
                CatapultServicesManager.Instance.HandleConnected();
                while (CMSLoader.Instance.CMSData == null) { yield return null; }
                LoadCustomisations();
            }

            try // i just don't care
            {
                FindObjectOfType<TitleScreenViewModel>().OnLoginSucceeded();
            }
            catch { }
            FindObjectOfType<MainMenuManager>().ApplyOutfit();
        }

        void LoadCustomisations()
        {
            GlobalGameStateClient.Instance.PlayerProfile.CustomisationSelections = new CustomisationSelections();
            CustomisationSelections customisationSelections = GlobalGameStateClient.Instance.PlayerProfile.CustomisationSelections;
            EmotesOption[] allEmotes = Resources.FindObjectsOfTypeAll<EmotesOption>();
            SkinPatternOption[] patternOptions = Resources.FindObjectsOfTypeAll<SkinPatternOption>();
            ColourOption[] colourOptions = Resources.FindObjectsOfTypeAll<ColourOption>();
            FaceplateOption[] faceplateOptions = Resources.FindObjectsOfTypeAll<FaceplateOption>();
            NameplateOption[] nameplateOptions = Resources.FindObjectsOfTypeAll<NameplateOption>();
            VictoryOption[] victoryOptions = Resources.FindObjectsOfTypeAll<VictoryOption>();
            CostumeOption[] costumeOptions = Resources.FindObjectsOfTypeAll<CostumeOption>();
            EmotesOption[] emotearray;
            List<EmotesOption> emotelist = new List<EmotesOption>();
            HashSet<int> uniqueEmotes = new HashSet<int>();
            while (uniqueEmotes.Count < 4)
            {
                int randomnumber = UnityEngine.Random.Range(0, allEmotes.Length);
                uniqueEmotes.Add(randomnumber);
            }

            foreach (int emoteNumber in uniqueEmotes)
            {
                emotelist.Add(Resources.FindObjectsOfTypeAll<EmotesOption>()[emoteNumber]);
            }

            emotearray = emotelist.ToArray();

            foreach (CostumeOption costumeOption in costumeOptions)
            {
                switch (costumeOption.name)
                {
                    case "NoneBottomOption":
                        customisationSelections.CostumeBottomOption = costumeOption;
                        break;
                    case "NoneTopOption":
                        customisationSelections.CostumeTopOption = costumeOption;
                        break;
                }
            }

            customisationSelections.EmoteBottomOption = emotearray[2];
            customisationSelections.EmoteLeftOption = emotearray[3];
            customisationSelections.EmoteRightOption = emotearray[1];
            customisationSelections.EmoteTopOption = emotearray[0];
            customisationSelections.PatternOption = patternOptions[UnityEngine.Random.Range(0, patternOptions.Length)];
            customisationSelections.ColourOption = colourOptions[UnityEngine.Random.Range(0, colourOptions.Length)];
            customisationSelections.FaceplateOption = faceplateOptions[UnityEngine.Random.Range(0, faceplateOptions.Length)];
            customisationSelections.NameplateOption = nameplateOptions[UnityEngine.Random.Range(0, nameplateOptions.Length)];
            customisationSelections.VictoryPoseOption = victoryOptions[UnityEngine.Random.Range(0, victoryOptions.Length)];
        }
    }
}