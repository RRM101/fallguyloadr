using FGClient;
using HarmonyLib;
using System;
using System.Collections.Generic;
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
            __instance.gameObject.AddComponent<MainMenuCustomAudio>();

            return true;
        }

        [HarmonyPatch(typeof(MainMenuManager), "PlayMenuMusic")]
        [HarmonyPrefix]
        static bool MainMenuManagerPlayMusic(MainMenuManager __instance)
        {
            __instance.GetComponent<MainMenuCustomAudio>().PlayMusic();
            return false;
        }

        [HarmonyPatch(typeof(MainMenuManager), "ResumeMusic")]
        [HarmonyPrefix]
        static bool MainMenuManagerResume(MainMenuManager __instance)
        {
            __instance.GetComponent<MainMenuCustomAudio>().waveOut.Play();

            return false;
        }

        [HarmonyPatch(typeof(MainMenuManager), "StopMusic")]
        [HarmonyPrefix]
        static bool MainMenuManagerStopMusic(MainMenuManager __instance)
        {
            if (__instance.GetComponent<MainMenuCustomAudio>() != null)
            {
                __instance.GetComponent<MainMenuCustomAudio>().waveOut.Dispose();
            }

            return false;
        }
    }
}
