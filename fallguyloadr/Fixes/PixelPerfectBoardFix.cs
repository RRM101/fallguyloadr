using BepInEx.Unity.IL2CPP.Utils.Collections;
using Levels.PixelPerfect;
using System.Collections;
using UnityEngine;

namespace fallguyloadr.Fixes
{
    public class PixelPerfectBoardFix : MonoBehaviour // Too lazy to rewrite this
    {
        PixelPerfectManager pixelPerfectManager;
        PixelPerfectBoard pixelPerfectBoard;
        bool painted = false;

        public void Start()
        {
            pixelPerfectManager =  FindObjectOfType<PixelPerfectManager>();
            pixelPerfectBoard = GetComponent<PixelPerfectBoard>();
        }

        IEnumerator Painted()
        {
            painted = true;
            pixelPerfectBoard.PlayFanfare(5);
            int random_pattern = UnityEngine.Random.Range(0, pixelPerfectManager._config.PatternGroup.Patterns.Count);
            string pattern = pixelPerfectManager._config.PatternGroup.Patterns[random_pattern].PatternID.ToString();
            yield return new WaitForSeconds(3);
            pixelPerfectBoard.StopFanfare();
            pixelPerfectBoard.DrawDisplayPattern(pattern);
            painted = false;
        }

        public void Update()
        {
            if (pixelPerfectBoard._inputScreen._value.ToString() == pixelPerfectBoard._displayScreen._value.ToString() && !painted)
            {
                StartCoroutine(Painted().WrapToIl2Cpp());
            }
        }
    }
}
