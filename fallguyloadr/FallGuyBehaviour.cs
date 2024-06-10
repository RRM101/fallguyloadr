using FG.Common;
using FG.Common.Character;
using FG.Common.Character.MotorSystem;
using System.Collections;
using Levels;
using Levels.Obstacles;
using Levels.PixelPerfect;
using Levels.Progression;
using UnityEngine;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using FGClient;
using FGClient.UI;
using System;
using Levels.ScoreZone;
using fallguyloadr.UI;

namespace fallguyloadr
{
    public class FallGuyBehaviour : MonoBehaviour
    {
        MotorAgent motorAgent;
        MPGNetObject netObject;
        CheckpointManager checkpointManager;
        bool qualified;

        void Start()
        {
            motorAgent = GetComponent<MotorAgent>();
            netObject = GetComponent<MPGNetObject>();
            checkpointManager = FindObjectOfType<CheckpointManager>();

            MotorFunctionPowerup motorFunctionPowerup = motorAgent.GetMotorFunction<MotorFunctionPowerup>();

            if (motorFunctionPowerup != null)
            {
                motorFunctionPowerup.EquippedPowerupData._duration = -1;
                motorFunctionPowerup.EquippedPowerupData._powerup = Resources.FindObjectsOfTypeAll<PowerupSO>()[0];
                motorFunctionPowerup.EquippedPowerupData._hasInfiniteStacks = true;
            }
        }

        void Update()
        {
            if (transform.position.y < -50 || Input.GetKeyDown(KeyCode.R))
            {
                LoaderBehaviour.instance.Respawn(checkpointManager);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            BinaryPixel binaryPixel = other.gameObject.GetComponentInParent<BinaryPixel>();
            EndZoneVFXTrigger endZoneVFXTrigger = other.gameObject.GetComponent<EndZoneVFXTrigger>();
            COMMON_ObjectiveReachEndZone objectiveReachEndZone = other.gameObject.GetComponent<COMMON_ObjectiveReachEndZone>();
            SpawnableCollectable collectable = other.gameObject.GetComponentInParent<SpawnableCollectable>();

            if (binaryPixel != null)
            {
                GameObject pixelPerfectBoard_object = binaryPixel.gameObject.transform.parent.parent.parent.parent.parent.gameObject;
                PixelPerfectBoard pixelPerfectBoard = pixelPerfectBoard_object.GetComponent<PixelPerfectBoard>();
                pixelPerfectBoard.ReceiveInput(binaryPixel.Index, netObject);
            }

            if ((endZoneVFXTrigger != null || objectiveReachEndZone != null) && !qualified)
            {
                qualified = true;
                Qualify();
            }

            if (collectable != null)
            {
                collectable.Collect();
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            try
            {
                collision.transform.parent.gameObject.TryGetComponent(out COMMON_RespawningTile respawningTile);

                if (respawningTile != null)
                {
                    respawningTile.OnPlatformSteppedOn();
                    StartCoroutine(RespawnTile(respawningTile).WrapToIl2Cpp());
                }
            }
            catch { }
        }

        IEnumerator RespawnTile(COMMON_RespawningTile respawningTile)
        {
            float multiplier = (respawningTile._brokenStateTimeScaler != null && respawningTile._brokenStateTimeScaler.enabled) ? respawningTile._brokenStateTimeScaler.FixedUpdateScalar : 1f;
            yield return new WaitForSeconds(respawningTile._brokenDuration * multiplier);
            respawningTile.OnTriggerRespawnRoutine();
        }

        void Qualify()
        {
            Action action = RoundOver;

            GlobalGameStateClient.Instance.GameStateView.GetLiveClientGameManager(out ClientGameManager cgm);
            cgm._musicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            QualifiedScreenViewModel.Show("Qualified", action);
            AudioManager.PlayGameplayEndAudio(true);
        }

        void RoundOver()
        {
            Action action = LoaderUI.instance.LoadRandomRound;
            RoundEndedScreenViewModel.Show(action);
            AudioManager.PlayOneShot(AudioManager.EventMasterData.RoundOver);
        }
    }
}
