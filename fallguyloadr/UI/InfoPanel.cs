using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Panels;

namespace fallguyloadr.UI
{
    public class InfoPanel : PanelBase
    {
        public InfoPanel(UIBase owner) : base(owner)
        {
            if (instance == null)
            {
                instance = this;
            }
        }

        public static InfoPanel instance;

        public override string Name => "Info";
        public override int MinWidth => 355;
        public override int MinHeight => 295;
        public override Vector2 DefaultAnchorMin => new(0.25f, 0.25f);
        public override Vector2 DefaultAnchorMax => new(0.75f, 0.75f);
        public override bool CanDragAndResize => true;

        string InfoText = "FGChaos Keyboard Shortcuts:\nCtrl+L: Stop Chaos\nF1: Open Effect Options\nF3: Open Debug Menu\n\nfallguyloadr Keyboard Shortcuts:\nF2: Toggle UI\nR: Respawn\nO: Stop Playing Replay\nP: Save Recorded Replay";

        protected override void ConstructPanelContent()
        {
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 800);
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100);
            Dragger.OnEndResize();

            GameObject verticleGroup = UIFactory.CreateVerticalGroup(ContentRoot, "Verticle Group", false, false, true, true, 2, bgColor: new Color(0.07f, 0.07f, 0.07f, 1));

            GameObject buttonRow = UIFactory.CreateHorizontalGroup(verticleGroup, "Button Row", true, false, true, true, 2, bgColor: new Color(0.07f, 0.07f, 0.07f, 1));

            ButtonRef discordButton = UIFactory.CreateButton(buttonRow, "Discord Button", "Discord");
            UIFactory.SetLayoutElement(discordButton.Component.gameObject, 100, 25, 0, 0);
            discordButton.OnClick += () =>
            {
                Application.OpenURL("https://discord.gg/SdMdBKrqGC");
            };

            ButtonRef githubButton = UIFactory.CreateButton(buttonRow, "Github Button", "Source Code");
            UIFactory.SetLayoutElement(githubButton.Component.gameObject, 100, 25, 0, 0);
            githubButton.OnClick += () =>
            {
                Application.OpenURL("https://github.com/RRM101/fallguyloadr");
            };

            GameObject buttonRow2 = UIFactory.CreateHorizontalGroup(verticleGroup, "Button Row 2", true, false, true, true, 2, bgColor: new Color(0.07f, 0.07f, 0.07f, 1));

            ButtonRef configButton = UIFactory.CreateButton(buttonRow2, "Config Button", "Open Config");
            UIFactory.SetLayoutElement(configButton.Component.gameObject, 100, 25, 0, 0);
            configButton.OnClick = OpenFGChaosConfig;

            GameObject textRow = UIFactory.CreateHorizontalGroup(ContentRoot, "Text Row", true, false, true, true, 2, new Vector4(4, 4, 4, 4), new Color(0.07f, 0.07f, 0.07f, 1));

            Text infoLabel = UIFactory.CreateLabel(textRow, "Info Label", InfoText);
            infoLabel.lineSpacing = 1.25f;
            UIFactory.SetLayoutElement(infoLabel.gameObject, 100, 25, 0, 0);
        }

        void OpenFGChaosConfig()
        {
            GameObject configManagerRoot = GameObject.Find("UniverseLibCanvas").transform.FindChild("com.sinai.BepInExConfigManager_Root").gameObject;
            configManagerRoot.SetActive(true);
            configManagerRoot.transform.GetChild(0).GetChild(0).FindChild("Content/Main/CategoryList/Viewport/Content/BUTTON_org.rrm1.fgchaos").GetComponent<Button>().onClick.Invoke();
        }
    }
}
