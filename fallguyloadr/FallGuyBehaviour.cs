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

namespace fallguyloadr
{
    public class FallGuyBehaviour : MonoBehaviour
    {
        public static bool usingV11Physics = false;

        List<Vector3> positions = new();
        List<Quaternion> rotations = new();
        int playingIndex;

        FallGuysCharacterController fallGuysCharacter;
        Rigidbody rb;
        MotorAgent motorAgent;
        MPGNetObject netObject;
        CheckpointManager checkpointManager;
        MotorFunctionMovement movement;
        bool qualified;

        void Start()
        {
            fallGuysCharacter = GetComponent<FallGuysCharacterController>();
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

            if (Plugin.UseV11CharacterPhysics.Value)
            {
                SetV11Physics();
            }

            if (ReplayManager.Instance.currentReplay != null)
            {
                Replay replay = ReplayManager.Instance.currentReplay;
                foreach (float[] position in replay.Positions)
                {
                    positions.Add(new Vector3(position[0], position[1], position[2]));
                }

                foreach (float[] rotation in replay.Rotations)
                {
                    rotations.Add(new Quaternion(rotation[0], rotation[1], rotation[2], rotation[3]));
                }
                transform.position = positions[0];
            }
        }

        void OnGUI()
        {
            if (ReplayManager.Instance.hashCheckFailed)
            {
                GUI.Label(new Rect(5, 5, Screen.width, Screen.height), "<size=25><color=red>Hash check failed!</color></size>");
            }
        }

        void Update()
        {
            if (transform.position.y < -50 || Input.GetKeyDown(KeyCode.R))
            {
                LoaderBehaviour.instance.Respawn(checkpointManager);
            }

            if (LoaderBehaviour.instance.canLoadLevel)
            {
                if (Input.GetKeyDown(KeyCode.O))
                {
                    StopPlayingReplay();
                }

                if (Input.GetKeyDown(KeyCode.P))
                {
                    StopRecording(true);
                }
            }
        }

        void FixedUpdate()
        {
            if (ReplayManager.Instance.startPlaying)
            {
                if (ReplayManager.Instance.currentReplay != null)
                {
                    if (positions.Count-1 < playingIndex | rotations.Count-1 < playingIndex)
                    {
                        StopPlayingReplay();
                        return;
                    }

                    rb.velocity = Vector3.zero;
                    transform.position = positions[playingIndex];
                    transform.rotation = rotations[playingIndex];
                    fallGuysCharacter.SetDesiredRotation(rotations[playingIndex]);
                    motorAgent.Animator.SetBool(new HashedAnimatorString("Moving"), true);
                    movement.SetDesiredLean(1);
                    playingIndex++;
                }
                else
                {
                    positions.Add(transform.position);
                    rotations.Add(transform.rotation);
                }
            }
        }

        void StopPlayingReplay()
        {
            ReplayManager.Instance.StopPlayingReplay();
            positions.Clear();
            rotations.Clear();
            movement.SetDesiredLean(0);
            motorAgent.Animator.SetBool(new HashedAnimatorString("Moving"), false);
        }

        void SetV11Physics()
        {
            usingV11Physics = true;
            CharacterControllerData data = fallGuysCharacter._data;
            data.aerialTurnSpeed = 8;
            data.getUpJumpInterruptTime = 0.1f;
            data.getUpRollOverAngleThreshold = 40;
            data.getUpRollOverMaxDuration = 0.4f;
            data.getUpRollOverRotationSpeed = 6;
            data.getUpStandUprightAngleThreshold = 30;
            data.getUpStandUprightRotationSpeed = 4;
            data.impactAlongFloorMultiplier = 0.85f;
            data.impactOwnVelocityContribution = 0.5f;
            data.impactVerticalMultiplier = 0.2f;
            data.ragdollRepinMaxDelay = 0.3f;
            data.ragdollRepinSpeed = 0.9f;
            data.rollingInAirMaxSpeed = 0.2f;
            data.stunnedMovementDelay = 0;
            data.jumpForce = new Vector3(0, 17.5f, 0);
        }

        public void StopRecording(bool save)
        {
            if (ReplayManager.Instance.currentReplay == null)
            {
                ReplayManager.Instance.startPlaying = false;
                if (save)
                {
                    ReplayManager.SaveReplay(positions.ToArray(), rotations.ToArray());
                }
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

                StopRecording(true);

                Qualify(false);
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

        public void Qualify(bool win)
        {
            void SwitchToVictoryScreen()
            {
                GlobalGameStateClient.Instance.SwitchToVictoryScreen(0);
            }

            GlobalGameStateClient.Instance.GameStateView.GetLiveClientGameManager(out ClientGameManager cgm);
            cgm._musicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);

            if (win)
            {
                Action action = SwitchToVictoryScreen;
                WinnerScreenViewModel.Show("winner", true, action);
            }
            else
            {
                Action action = RoundOver;

                QualifiedScreenViewModel.Show("qualified", action);
            }

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
