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
using System.Collections.Generic;
using fallguyloadr.JSON;
using System.Text.Json;
using System.IO;
using BepInEx;

namespace fallguyloadr
{
    public class FallGuyBehaviour : MonoBehaviour
    {
        public bool startPlaying;
        List<Vector3> positons = new();
        List<Quaternion> rotations = new();
        int playingIndex;

        Rigidbody rb;
        MotorAgent motorAgent;
        MPGNetObject netObject;
        CheckpointManager checkpointManager;
        MotorFunctionMovement movement;
        bool qualified;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            motorAgent = GetComponent<MotorAgent>();
            netObject = GetComponent<MPGNetObject>();
            checkpointManager = FindObjectOfType<CheckpointManager>();
            movement = motorAgent.GetMotorFunction<MotorFunctionMovement>();

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

        void FixedUpdate()
        {
            if (startPlaying)
            {
                if (LoaderBehaviour.instance.currentReplay != null)
                {
                    if (positons.Count-1 < playingIndex | rotations.Count-1 < playingIndex)
                    {
                        StopPlayingReplay();
                        return;
                    }

                    rb.velocity = Vector3.zero;
                    transform.position = positons[playingIndex];
                    transform.rotation = rotations[playingIndex];
                    motorAgent.Animator.SetBool(new HashedAnimatorString("Moving"), true);
                    movement.SetDesiredLean(1);
                    playingIndex++;
                }
                else
                {
                    positons.Add(transform.position);
                    rotations.Add(transform.rotation);
                }
            }
        }

        public void RoundStarted()
        {
            if (LoaderBehaviour.instance.currentReplay != null)
            {
                Replay replay = LoaderBehaviour.instance.currentReplay;
                foreach (float[] position in replay.Positions)
                {
                    positons.Add(new Vector3(position[0], position[1], position[2]));
                }

                foreach (float[] rotation in replay.Rotations)
                {
                    rotations.Add(new Quaternion(rotation[0], rotation[1], rotation[2], rotation[3]));
                }
            }
            else
            {
                positons.Clear();
                rotations.Clear();
            }
            startPlaying = true;
        }

        public void StopRecording(bool save)
        {
            startPlaying = false;
            if (save)
            {
                List<float[]> positionsList = new();
                List<float[]> rotationsList = new();

                foreach (Vector3 position in positons)
                {
                    positionsList.Add(new float[] {position.x, position.y, position.z});
                }

                foreach (Quaternion rotation in rotations)
                {
                    rotationsList.Add(new float[] { rotation.x, rotation.y, rotation.z, rotation.w });
                }

                Replay replay = new Replay();
                replay.Version = Plugin.version;
                replay.Seed = LoaderBehaviour.seed;
                replay.RoundID = NetworkGameData.currentGameOptions_._roundID;
                replay.Positions = positionsList.ToArray();
                replay.Rotations = rotationsList.ToArray();

                string replayJson = JsonSerializer.Serialize<Replay>(replay);

                string datetime = $"{DateTime.Now.Day}-{DateTime.Now.Month}-{DateTime.Now.Year} {DateTime.Now.Hour}-{DateTime.Now.Minute}-{DateTime.Now.Second}";

                File.WriteAllText($"{Paths.PluginPath}/fallguyloadr/Replays/Replay - {NetworkGameData.currentGameOptions_._roundID} - {datetime}.json", replayJson);
            }
        }

        void StopPlayingReplay()
        {
            startPlaying = false;
            positons.Clear();
            rotations.Clear();
            GetComponent<FallGuysCharacterControllerInput>().AcceptInput = true;
            LoaderBehaviour.instance.currentReplay = null;
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
                if (LoaderBehaviour.instance.currentReplay != null)
                {

                }
                else
                {
                    StopRecording(true);
                }

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
