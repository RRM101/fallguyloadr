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
using BepInEx.Configuration;
using fallguyloadr.UI;
using NAudio.Wave;
using System.Text.Json;
using System.IO;
using fallguyloadr.JSON;
using UnityEngine.UI;
using Levels.Hoops;
using BepInEx.Logging;

namespace fallguyloadr
{
    [BepInDependency("org.rrm1.fgchaos", "1.2.0")]
    [BepInPlugin("org.rrm1.fallguyloadr", "fallguyloadr", version)]
    public class Plugin : BasePlugin
    {
        public const string version = "1.2.0";

        public static ManualLogSource Logs = new("fallguyloadr");

        public static ConfigEntry<string> Username { get; set; }
        public static ConfigEntry<float> LoadingGameScreenDelay { get; set; }
        public static ConfigEntry<bool> SkipRoundIntro { get; set; }
        public static ConfigEntry<bool> DisablePowerUpUI { get; set; }
        public static ConfigEntry<string> Theme { get; set; }
        public static ConfigEntry<int> CustomAudioVolume { get; set; }
        public static ConfigEntry<bool> UseV11CharacterPhysics { get; set; }

        public override void Load()
        {
            Username = Config.Bind("Config", "Username", Environment.UserName, "Your username which gets displayed in-game.");
            LoadingGameScreenDelay = Config.Bind("Config", "Waiting For Players Delay", 5f, "Amount of time in the Waiting For Players screen.");
            SkipRoundIntro = Config.Bind("Config", "Skip Round Intro", false, "Skips the round intro camera.");
            DisablePowerUpUI = Config.Bind("Config", "Disable Power-Up UI", false, "Disables Power-Up UI.");
            Theme = Config.Bind("Config", "Theme", "Default", "Custom theme for the Main Menu and the Round Loading Screen.");
            CustomAudioVolume = Config.Bind("Config", "Custom Audio Volume", 50, "Volume for custom audio. (Max 100)");
            UseV11CharacterPhysics = Config.Bind("Config", "Use 11.0 Physics", false, "Enables the physics changes made in Fall Guys versions 10.9 and 11.0");

            BepInEx.Logging.Logger.Sources.Add(Logs);

            ClassInjector.RegisterTypeInIl2Cpp<LoaderManager>();
            ClassInjector.RegisterTypeInIl2Cpp<FallGuyBehaviour>();
            ClassInjector.RegisterTypeInIl2Cpp<Fixes.PixelPerfectBoardFix>();
            ClassInjector.RegisterTypeInIl2Cpp<Fixes.HoopsManagerReimplementation>();
            ClassInjector.RegisterTypeInIl2Cpp<Fixes.HoopReimplementation>();
            ClassInjector.RegisterTypeInIl2Cpp<MainMenuCustomAudio>();

            Harmony.CreateAndPatchAll(typeof(Patches));
            Harmony.CreateAndPatchAll(typeof(IsGameServerPatches));
            Harmony.CreateAndPatchAll(typeof(CosmeticsPatches));
            Harmony.CreateAndPatchAll(typeof(ThemePatches), "ThemePatches");

            GameObject obj = new GameObject("Loader Behaviour");
            GameObject.DontDestroyOnLoad(obj);
            obj.hideFlags = HideFlags.HideAndDontSave;
            obj.AddComponent<LoaderManager>();

            Log.LogInfo($"Plugin fallguy loadr is loaded!");            
        }

        public static string GetModFolder()
        {
            // idiot protection
            return Directory.Exists($"{Paths.PluginPath}/fallguyloadr/fallguyloadr") ? $"{Paths.PluginPath}/fallguyloadr/fallguyloadr" : $"{Paths.PluginPath}/fallguyloadr";
        }
    }

    public class LoaderManager : MonoBehaviour
    {
        public static LoaderManager instance;
        public static int seed = DateTime.Now.Millisecond;
        public Theme currentTheme = Plugin.Theme.Value != "Default" ? JsonSerializer.Deserialize<Theme>(File.ReadAllText($"{Plugin.GetModFolder()}/Themes/{Plugin.Theme.Value}")) : JsonSerializer.Deserialize<Theme>(File.ReadAllText($"{Plugin.GetModFolder()}/Themes/Season10_Theme.json")); // spaghetti code
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

            new ReplayManager();
        }

        void OnEnable()
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
                new ThemeSelector(UI);
                ThemeSelector.instance.SetActive(false);
                new ReplaySelector(UI);
                ReplaySelector.instance.SetActive(false);
                new LoaderUI(UI);
                UniverseLib.Config.ConfigManager.Force_Unlock_Mouse = false;
                AddCMSStringKeys();
            }
        }

        void AddCMSStringKeys()
        {
            Dictionary<string, string> stringsToAdd = new Dictionary<string, string>()
            {
                {"fallguyloadr_create", "CREATE"},
                {"fallguyloadr_skin_presets_create_popup_additional_message", "Your current Customisations will be used."},
                {"fallguyloadr_replay", "REPLAY"}
            };

            foreach (var toAdd in stringsToAdd) AddNewStringToCMS(toAdd.Key, toAdd.Value);
        }

        void AddNewStringToCMS(string key, string value)
        {
            if (!CMSLoader.Instance._localisedStrings._localisedStrings.ContainsKey(key))
            {
                CMSLoader.Instance._localisedStrings._localisedStrings.Add(key, value);
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!(scene.name.Contains("Fraggle") || scene.name.Contains("Editor")))
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

                    MultiplayerStartingPosition[] startingPositions = FindObjectsOfType<MultiplayerStartingPosition>();
                    MultiplayerStartingPosition startingPosition = startingPositions[UnityEngine.Random.Range(0, startingPositions.Length)];

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

            HoopsManager hoopsManager = FindObjectOfType<HoopsManager>();
            if (hoopsManager != null)
            {
                hoopsManager.gameObject.AddComponent<Fixes.HoopsManagerReimplementation>();
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
            yield return new WaitForSeconds(Plugin.LoadingGameScreenDelay.Value);
            gameLoading.OnServerRequestStartIntroCameras();
            ClientGameManager cgm = gameLoading._clientGameManager;
            yield return new WaitForSeconds(Plugin.SkipRoundIntro.Value ? 0 : cgm.CameraDirector.IntroCamerasDuration);

            Round round = CMSLoader.Instance.CMSData.Rounds[NetworkGameData.currentGameOptions_._roundID];            

            if (!cgm.IsShutdown)
            {
                GameMessageServerStartGame startGameMessage = new GameMessageServerStartGame();
                startGameMessage.StartRoundTime = 0;

                startGameMessage.EndRoundTime = round.GameRules.Duration;

                gameLoading.HandleGameServerStartGame(startGameMessage);
            }
        }

        public void LoadRound(string round_id, int seed_)
        {
            if (canLoadLevel)
            {
                seed = seed_;

                Round round = CMSLoader.Instance.CMSData.Rounds[round_id];

                if (SceneManager.GetActiveScene().name == round.GetSceneName())
                {
                    SceneManager.LoadScene("Transition");
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

        public void LoadRound(string round_id)
        {
            LoadRound(round_id, (int)DateTime.Now.Ticks);
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
                MultiplayerStartingPosition[] startingPositions = FindObjectsOfType<MultiplayerStartingPosition>();
                MultiplayerStartingPosition startingPosition = startingPositions[UnityEngine.Random.Range(0, startingPositions.Length)];
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
            canLoadLevel = true;

            if (CMSLoader.Instance.CMSData == null)
            {
                if (Plugin.Theme.Value != "Default")
                {
                    LoadTheme();
                }

                PlayerTargetSettings.HardCurrencyEnabled = true;
                PlayerTargetSettings.QuitAnywhereEnabled = true;
                CatapultServicesManager.Instance.HandleConnected();
                while (CMSLoader.Instance.CMSData == null) { yield return null; }
                LoadCustomisations();
            }

            GameObject.Find("UICanvas_Client_V2(Clone)/Default/Topbar_Prime(Clone)/SafeArea/TabsHorizontalLayout/SeasonPassButton").SetActive(false);
            GameObject.Find("UICanvas_Client_V2(Clone)/Default/Topbar_Prime(Clone)/SafeArea/TabsHorizontalLayout/ShopButton").SetActive(false);
            GameObject.Find("UICanvas_Client_V2(Clone)/Default/MainMenuBuilder(Clone)/MainScreensParent/Menu_Screen_Level_Editor/Menu_UI_LevelEditor(Clone)/Container_NotBanned/Container_Buttons/UI_Menu_LevelEditor_MainCarrousel/Load Button").SetActive(false);

            if (Plugin.Theme.Value != "Default")
            {
                GameObject background = GameObject.Find("Generic_UI_SeasonS10Background_Canvas_Variant");
                SetTheme(currentTheme, background);
            }

            try // i just don't care
            {
                FindObjectOfType<TitleScreenViewModel>().OnLoginSucceeded();    
            }
            catch { }
            FindObjectOfType<MainMenuManager>().ApplyOutfit();

            PlayerDetailsService playerDetailsService = PlatformServices.Current.PlayerDetailsService.Cast<PlayerDetailsService>();
            PlayerDetailsService.PlayerDetails playerDetails = new PlayerDetailsService.PlayerDetails(PlayerDetailsService.NameSource.InGame, false, "win", GlobalGameStateClient.Instance.GetLocalPlayerName(), GlobalGameStateClient.Instance.GetLocalPlayerName(), PlayerNameType.PlatformAccountName, false);
            playerDetailsService._playerDict.Add(GlobalGameStateClient.Instance.GetLocalPlayerKey(), playerDetails);
            GameObject.Find("UICanvas_Client_V2(Clone)/Default/MainMenuBuilder(Clone)/MainScreensParent/Menu_Screen_Main/Prime_UI_MainMenu_Canvas(Clone)/SafeArea/BottomLeft_Group/AnimContainer/PB_UI_NameTag").GetComponent<NameTagViewModel>().UpdateDisplayWithLocalPlayer();
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
            List<Nickname> nicknames = new();

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

            foreach (Nickname nickname in CMSLoader.Instance.CMSData.Nicknames.Values)
            {
                nicknames.Add(nickname);
            }

            NicknameOption nicknameOption = new NicknameOption();
            nicknameOption.SetCMSData(nicknames[UnityEngine.Random.Range(0, nicknames.Count)]);

            customisationSelections.EmoteBottomOption = emotearray[2];
            customisationSelections.EmoteLeftOption = emotearray[3];
            customisationSelections.EmoteRightOption = emotearray[1];
            customisationSelections.EmoteTopOption = emotearray[0];
            customisationSelections.PatternOption = patternOptions[UnityEngine.Random.Range(0, patternOptions.Length)];
            customisationSelections.ColourOption = colourOptions[UnityEngine.Random.Range(0, colourOptions.Length)];
            customisationSelections.FaceplateOption = faceplateOptions[UnityEngine.Random.Range(0, faceplateOptions.Length)];
            customisationSelections.NameplateOption = nameplateOptions[UnityEngine.Random.Range(0, nameplateOptions.Length)];
            customisationSelections.VictoryPoseOption = victoryOptions[UnityEngine.Random.Range(0, victoryOptions.Length)];
            customisationSelections.NicknameOption = nicknameOption;
        }

        public static Sprite PNGtoSprite(string path)
        {
            if (File.Exists(path))
            {
                byte[] imagedata = File.ReadAllBytes(path);
                Texture2D texture = new Texture2D(0, 0, TextureFormat.ARGB32, false);
                ImageConversion.LoadImage(texture, imagedata);
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
                return sprite;
            }
            return null;
        }

        void Quit(bool wasOk)
        {
            Application.Quit();
        }

        void QuitPopup()
        {
            canLoadLevel = false;
            Action<bool> action = Quit;

            ModalMessageData modalMessageData = new ModalMessageData()
            {
                Title = "fallguyloadr - Themes",
                Message = $"The Theme you have selected ({Plugin.Theme.Value}) does not exist",
                LocaliseTitle = UIModalMessage.LocaliseOption.NotLocalised,
                LocaliseMessage = UIModalMessage.LocaliseOption.NotLocalised,
                ModalType = UIModalMessage.ModalType.MT_BLOCKING,
                OnCloseButtonPressed = action
            };

            PopupManager.Instance.Show(PopupInteractionType.Error, modalMessageData);
            Plugin.Theme.Value = "Default";
        }

        public void LoadTheme()
        {
            if (File.Exists($"{Plugin.GetModFolder()}/Themes/{Plugin.Theme.Value}"))
            {
                string themeString = File.ReadAllText($"{Plugin.GetModFolder()}/Themes/{Plugin.Theme.Value}");
                currentTheme = JsonSerializer.Deserialize<Theme>(themeString);

                LoadingGameScreenViewModel[] loadingGameScreens = Resources.FindObjectsOfTypeAll<LoadingGameScreenViewModel>();
                foreach (LoadingGameScreenViewModel loadingGameScreen in loadingGameScreens)
                {
                    if (loadingGameScreen.name == "Prime_UI_RoundSelected_Prefab_Canvas")
                    {
                        SetTheme(currentTheme, loadingGameScreen.transform.GetChild(1).GetChild(0).gameObject);
                    }
                }

                if (SceneManager.GetActiveScene().name == "MainMenu")
                {
                    MainMenuManager mainMenuManager = GameObject.FindObjectOfType<MainMenuManager>();
                    mainMenuManager.StopMusic();
                }
            }
            else
            {
                QuitPopup();
            }
        }

        public void SetTheme(Theme theme, GameObject gameObject)
        {
            Sprite pattern = PNGtoSprite($"{Plugin.GetModFolder()}/Themes/{theme.Pattern}");
            Transform mask = gameObject.transform.GetChild(1);

            if (theme.UpperGradientRGBA != null)
            {
                Image backdrop = mask.GetChild(0).GetComponent<Image>();
                backdrop.sprite = null;
                backdrop.color = new Color(theme.UpperGradientRGBA[0], theme.UpperGradientRGBA[1], theme.UpperGradientRGBA[2], theme.UpperGradientRGBA[3]);
            }

            if (theme.CirclesRGBA != null)
            {
                mask.GetChild(2).GetComponent<Image>().color = Color.white;
                Material circlesMaterial = mask.GetChild(2).GetComponent<Image>().material;
                circlesMaterial.color = new Color(theme.CirclesRGBA[0], theme.CirclesRGBA[1], theme.CirclesRGBA[2], theme.CirclesRGBA[3]);
                circlesMaterial.SetTexture("_Pattern", pattern.texture);
            }

            if (theme.LowerGradientRGBA != null)
            {
                Image gradient = mask.GetChild(3).GetComponent<Image>();
                gradient.gameObject.SetActive(true);
                gradient.color = new Color(theme.LowerGradientRGBA[0], theme.LowerGradientRGBA[1], theme.LowerGradientRGBA[2], theme.LowerGradientRGBA[3]);
            }
        }
    }
}