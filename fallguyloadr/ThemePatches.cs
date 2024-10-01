using BepInEx;
using FGClient;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fallguyloadr
{
    public class ThemePatches
    {
        [HarmonyPatch(typeof(MainMenuManager), "Awake")]
        [HarmonyPrefix]
        static bool MainMenuManagerAwake(MainMenuManager __instance)
        {
            if (File.Exists($"{Paths.PluginPath}/fallguyloadr/Themes/{LoaderBehaviour.instance.currentTheme.Music}") && Plugin.Theme.Value != "Default")
            {
                __instance.gameObject.AddComponent<MainMenuCustomAudio>();
            }

            return true;
        }

        [HarmonyPatch(typeof(MainMenuManager), "PlayMenuMusic")]
        [HarmonyPrefix]
        static bool MainMenuManagerPlayMusic(MainMenuManager __instance)
        {
            MainMenuCustomAudio mainMenuCustomAudio = __instance.GetComponent<MainMenuCustomAudio>();
            if (mainMenuCustomAudio != null)
            {
                mainMenuCustomAudio.PlayMusic();
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(MainMenuManager), "ResumeMusic")]
        [HarmonyPrefix]
        static bool MainMenuManagerResume(MainMenuManager __instance)
        {
            MainMenuCustomAudio mainMenuCustomAudio = __instance.GetComponent<MainMenuCustomAudio>();
            if (mainMenuCustomAudio != null)
            {
                mainMenuCustomAudio.waveOut.Play();
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(MainMenuManager), "StopMusic")]
        [HarmonyPrefix]
        static bool MainMenuManagerStopMusic(MainMenuManager __instance)
        {
            MainMenuCustomAudio mainMenuCustomAudio = __instance.GetComponent<MainMenuCustomAudio>();

            if (mainMenuCustomAudio != null)
            {
                if (mainMenuCustomAudio.waveOut != null)
                {
                    mainMenuCustomAudio.stop = true;
                    mainMenuCustomAudio.waveOut.Dispose();
                }
                return false;
            }
            return true;
        }
    }
}
