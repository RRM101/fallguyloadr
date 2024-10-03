using BepInEx;
using fallguyloadr.JSON;
using FGClient;
using FGClient.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Panels;

namespace fallguyloadr.UI
{
    public class ThemeSelector : PanelBase
    {
        public ThemeSelector(UIBase owner) : base(owner)
        {
            if (instance == null)
            {
                instance = this;
            }
        }

        public static ThemeSelector instance;

        Sprite patternSprite = LoaderManager.PNGtoSprite($"{Plugin.GetModFolder()}/Themes/{LoaderManager.instance.currentTheme.Pattern}");
        Sprite linearGraidentImage;

        public override string Name => "Theme Selector";
        public override int MinWidth => (int)(LoaderManager.instance.currentTheme != null ? patternSprite.rect.width * 2 + 6 : 260);
        public override int MinHeight => (int)(LoaderManager.instance.currentTheme != null ? patternSprite.rect.height + 54 : 310);
        public override Vector2 DefaultAnchorMin => new(0.25f, 0.25f);
        public override Vector2 DefaultAnchorMax => new(0.75f, 0.75f);

        Dropdown themesDropdown;
        Il2CppSystem.Collections.Generic.List<string> themes = new();
        Theme pickedTheme;
        string pickedThemeName;

        GameObject bgRow;
        Image gradient;
        Image image;

        protected override void ConstructPanelContent()
        {
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, MinWidth);
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, MinHeight);
            Dragger.OnEndResize();

            Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
            foreach (Sprite sprite in sprites)
            {
                if (sprite.name == "UI_LinearGradient_Image")
                {
                    linearGraidentImage = sprite;
                }
            }

            Theme theme = LoaderManager.instance.currentTheme;

            GameObject selectionRow = UIFactory.CreateHorizontalGroup(ContentRoot, "Selection Row", true, false, true, true, 2, bgColor: new Color(0.07f, 0.07f, 0.07f, 1));

            UIFactory.CreateDropdown(selectionRow, "Themes", out themesDropdown, "", 14, PickTheme);
            UIFactory.SetLayoutElement(themesDropdown.gameObject, 150, 25, 0, 0);

            RefreshThemes();

            ButtonRef selectButton = UIFactory.CreateButton(selectionRow, "Select Button", "Select");
            UIFactory.SetLayoutElement(selectButton.Component.gameObject, 100, 25, 0, 0);
            selectButton.OnClick += SelectTheme;

            GameObject gameObject = UIFactory.CreateHorizontalGroup(ContentRoot, "", false, false, true, true, 0, bgColor: new Color(0.07f, 0.07f, 0.07f, 1));
            bgRow = UIFactory.CreateHorizontalGroup(gameObject, "Image", false, false, true, true, 0, new Vector4(0, 0, 0, 0), new Color(theme.UpperGradientRGBA[0], theme.UpperGradientRGBA[1], theme.UpperGradientRGBA[2], theme.UpperGradientRGBA[3]), childAlignment: TextAnchor.UpperLeft);

            gradient = UIFactory.CreateUIObject("gradient", bgRow).AddComponent<Image>();
            UIFactory.SetLayoutElement(gradient.gameObject, (MinWidth - 6)/2, MinHeight - 60);
            gradient.sprite = linearGraidentImage;
            gradient.color = new Color(theme.LowerGradientRGBA[0], theme.LowerGradientRGBA[1], theme.LowerGradientRGBA[2], theme.LowerGradientRGBA[3]);

            GameObject imageGameObject = UIFactory.CreateUIObject("image", gameObject);

            image = imageGameObject.AddComponent<Image>();
            image.sprite = patternSprite;
            image.color = new Color(theme.CirclesRGBA[0], theme.CirclesRGBA[1], theme.CirclesRGBA[2], theme.CirclesRGBA[3]);
            UIFactory.SetLayoutElement(imageGameObject);
        }

        /*public override void Toggle()
        {
            RefreshThemes();
            base.Toggle();
        }*/

        void RefreshThemes()
        {
            themes.Clear();
            themes.Add("Default");
            string[] fileNames = Directory.GetFiles($"{Plugin.GetModFolder()}/Themes");
            foreach (string fileName in fileNames)
            {
                if (fileName.EndsWith(".json"))
                {
                    themes.Add(Path.GetFileNameWithoutExtension(fileName));
                }
            }

            themesDropdown.ClearOptions();
            themesDropdown.AddOptions(themes);
        }

        void SelectTheme()
        {
            if (LoaderManager.instance.canLoadLevel && pickedThemeName != null)
            {
                Plugin.Logs.LogInfo($"Switching to theme {pickedThemeName} from {Plugin.Theme.Value}");

                Action<bool> action = ReloadGame;

                if (pickedThemeName != "Default" && Plugin.Theme.Value != "Default")
                {
                    Plugin.Theme.Value = pickedThemeName + ".json";
                }
                else if (Plugin.Theme.Value == "Default")
                {
                    Plugin.Theme.Value = pickedThemeName + ".json";
                }
                else
                {
                    Plugin.Theme.Value = "Default";
                }

                ModalMessageData modalMessageData = new ModalMessageData()
                {
                    Title = "fallguyloadr - Theme Selector",
                    Message = "The game will now reload to apply the changes.",
                    LocaliseTitle = UIModalMessage.LocaliseOption.NotLocalised,
                    LocaliseMessage = UIModalMessage.LocaliseOption.NotLocalised,
                    ModalType = UIModalMessage.ModalType.MT_OK,
                    OnCloseButtonPressed = action
                };

                PopupManager.Instance.Show(PopupInteractionType.Info, modalMessageData);
            }
        }

        void Quit(bool wasOk)
        {
            Application.Quit();
        }

        void QuitPopup()
        {
            LoaderManager.instance.canLoadLevel = false;

            Action<bool> action = Quit;

            ModalMessageData modalMessageData = new ModalMessageData()
            {
                Title = "fallguyloadr - Theme Selector",
                Message = "You will need to relaunch to game to apply the changes",
                LocaliseTitle = UIModalMessage.LocaliseOption.NotLocalised,
                LocaliseMessage = UIModalMessage.LocaliseOption.NotLocalised,
                ModalType = UIModalMessage.ModalType.MT_BLOCKING,
                OnCloseButtonPressed = action
            };

            PopupManager.Instance.Show(PopupInteractionType.Info, modalMessageData);
        }

        void ReloadGame(bool wasOk)
        {
            GlobalGameStateClient.Instance.ForceMainMenuSceneReload = true;
            LoaderManager.instance.LoadTheme();

            if (SceneManager.GetActiveScene().name == "MainMenu")
            {
                MainMenuManager mainMenuManager = GameObject.FindObjectOfType<MainMenuManager>();
                if (Plugin.Theme.Value != "Default")
                {
                    GameObject background = GameObject.Find("Generic_UI_SeasonS10Background_Canvas_Variant");
                    LoaderManager.instance.SetTheme(LoaderManager.instance.currentTheme, background);
                }
            }

            GlobalGameStateClient.Instance.ReloadGame(false);
        }

        void PickTheme(int index)
        {
            pickedThemeName = themes[index];
            if (pickedThemeName != "Default")
            {
                string themeString = File.ReadAllText($"{Plugin.GetModFolder()}/Themes/{pickedThemeName}.json");

                pickedTheme = JsonSerializer.Deserialize<Theme>(themeString);
                SetTheme(pickedTheme);
            }
        } 

        void SetTheme(Theme theme)
        {
            bgRow.GetComponent<Image>().color = new Color(theme.UpperGradientRGBA[0], theme.UpperGradientRGBA[1], theme.UpperGradientRGBA[2], theme.UpperGradientRGBA[3]);
            gradient.color = new Color(theme.LowerGradientRGBA[0], theme.LowerGradientRGBA[1], theme.LowerGradientRGBA[2], theme.LowerGradientRGBA[3]);
            image.color = new Color(theme.CirclesRGBA[0], theme.CirclesRGBA[1], theme.CirclesRGBA[2], theme.CirclesRGBA[3]);
            patternSprite = LoaderManager.PNGtoSprite($"{Plugin.GetModFolder()}/Themes/{theme.Pattern}");
            image.sprite = patternSprite;
        }
    }
}
