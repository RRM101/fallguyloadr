using DG.Tweening;
using FG.Common;
using FG.Common.Character;
using FG.Common.CMS;
using FG.Common.LevelEvents;
using FG.Common.LevelEvents.Handlers;
using FG.Common.LODs;
using HarmonyLib;
using Levels;
using Levels.ChickenChase;
using Levels.JumpShowdown;
using Levels.Obstacles;
using Levels.SeeSaw;
using Levels.TipToe;
using Levels.WallGuys;
using SRF;
using System;
using UnityEngine;
using static RootMotion.FinalIK.AimPoser;

namespace fallguyloadr
{
    public class IsGameServerPatches
    {
        [HarmonyPatch(typeof(COMMON_Button), "OnCollisionStay")]
        [HarmonyPrefix]
        static bool COMMON_ButtonOnCollisionStay(COMMON_Button __instance, Collision col)
        {
            switch (__instance._currentButtonState)
            {
                case COMMON_Button.ButtonState.Primed:
                    if (col.IsCollisionFromAbove(__instance.transform))
                    {
                        __instance.LastTriggeringFGCC = col.gameObject.GetComponentInParent<FallGuysCharacterController>();
                        __instance._localLastTriggeringFGCC = __instance.LastTriggeringFGCC;
                        __instance.PressButton(__instance.GameState.SimulationFixedTime);
                    }
                    break;

                case COMMON_Button.ButtonState.ReturningToPrimed:
                    __instance.TryApplyResetLaunchForce(col);
                    break;
            }

            return false;
        }

        [HarmonyPatch(typeof(WallGuysSegmentGenerator), "Awake")]
        [HarmonyPrefix]
        static bool WallGuysGeneratorAwake(WallGuysSegmentGenerator __instance)
        {
            __instance._collider = __instance.gameObject.GetComponent<BoxCollider>();
            FGRandom.Create(__instance._objectId, __instance.GameState.RoundRandomSeed);

            __instance.CreateSegmentObstacles();
            __instance._collider.enabled = false;

            return false;
        }

        [HarmonyPatch(typeof(COMMON_SeeSaw360), "Awake")]
        [HarmonyPrefix]
        static bool COMMON_SeeSaw360Awake(COMMON_SeeSaw360 __instance)
        {
            __instance._rb = __instance.gameObject.GetComponent<Rigidbody>();
            __instance.LimitAngularVelocity();

            return false;
        }

        [HarmonyPatch(typeof(COMMON_BlastBall), "StartBlastSequence")]
        [HarmonyPrefix]
        static bool COMMON_BlastBallStartBlastSequence(COMMON_BlastBall __instance)
        {
            __instance.StartSequenceEvent(__instance.GameState.GameplayTimeElapsed, false);
            return false;
        }

        [HarmonyPatch(typeof(COMMON_BlastBall), "AdvanceToExploded")]
        [HarmonyPrefix]
        static bool COMMON_BlastBallAdvanceToExploded(COMMON_BlastBall __instance)
        {
            __instance.SendExplosionLevelEvent();
            bool wasBeingCarried2 = __instance._carryObject.CarriedByCharacter != null;
            __instance.StartCoroutine(__instance.HandleEnvironmentalMovement(wasBeingCarried2));
            return true;
        }

        [HarmonyPatch(typeof(COMMON_BlastBall), "_explosionEventDefinitionIndex", MethodType.Getter)]
        [HarmonyPrefix]
        static bool COMMON_BlastBall_explosionEventDefinitionIndex(COMMON_BlastBall __instance, ref int __result)
        {
            __result = 1;
            return false;
        }

        [HarmonyPatch(typeof(COMMON_BlastBall), "IsLevelEditor", MethodType.Getter)]
        [HarmonyPrefix]
        static bool COMMON_BlastBallIsLevelEditor(COMMON_BlastBall __instance, ref bool __result)
        {
            __result = true;
            return false;
        }

        [HarmonyPatch(typeof(COMMON_BlastBall), "SendExplosionLevelEvent")]
        [HarmonyPrefix]
        static bool COMMON_BlastBallSendExplosionLevelEvent(COMMON_BlastBall __instance)
        {
            __instance._levelEventManager.GetHandlerAt(1).Cast<BlastBallExplosionEventHandler>().HandleLevelEvent(__instance.transform.position.x, __instance.transform.position.y, __instance.transform.position.z, __instance.ObjectIDString);
            return false;
        }

        [HarmonyPatch(typeof(OnTriggerLevelEventEmitter), "OnEnable")]
        [HarmonyPrefix]
        static bool OnTriggerLevelEventEmitterStart(OnTriggerLevelEventEmitter __instance)
        {
            __instance._levelEventManager = LevelEventManager.Instance;
            __instance._definitionIndex = 0;
            __instance._initialised = true;

            return false;
        }

        [HarmonyPatch(typeof(COMMON_PrefabSpawnerBase), "Start")]
        [HarmonyPrefix]
        static bool COMMON_PrefabSpawnerBaseStart(COMMON_PrefabSpawnerBase __instance)
        {
            foreach (COMMON_PrefabSpawnerBase.SpawnerEntry spawnerEntry in __instance._spawnObjects)
            {
                spawnerEntry.value.RemoveComponentIfExists<LodController>();
            }

            return false;
        }

        [HarmonyPatch(typeof(COMMON_PrefabSpawnerBase), "SpawnSelected")]
        [HarmonyPrefix]
        static bool PrefabSpawnerSpawnSelected(COMMON_PrefabSpawnerBase __instance, ref bool __result, bool isPreSpawning)
        {
            __instance.SelectedEntryIndex = UnityEngine.Random.Range(0, __instance._spawnObjects.Count);
            COMMON_PrefabSpawnerBase.SpawnerEntry spawnerEntry = __instance._spawnObjects[__instance.SelectedEntryIndex];
            __instance.Spawn(spawnerEntry);
            __result = true;

            return false;
        }

        [HarmonyPatch(typeof(COMMON_PrefabSpawnerBase), "CanPerformSpawn")]
        [HarmonyPrefix]
        static bool PrefabSpawnerCanPerformSpawn(COMMON_PrefabSpawnerBase __instance, ref bool __result)
        {
            __result = true;
            return false;
        }

        [HarmonyPatch(typeof(COMMON_PrefabSpawnerBase), "InstantiateObject")]
        [HarmonyPrefix]
        static bool PrefabSpawnerInstantiateObject(COMMON_PrefabSpawnerBase __instance, COMMON_PrefabSpawnerBase.SpawnerEntry entry, Vector3 spawnPosition)
        {
            Quaternion initialRotation = __instance.GetInitialRotation(entry);
            Vector3 spawnScale = entry.value.transform.localScale;
            GameObject netObject = UnityEngine.Object.Instantiate(entry.value);
            netObject.transform.position = spawnPosition;
            netObject.transform.localScale = spawnScale;
            netObject.transform.rotation = initialRotation;
            __instance.OnInstantiateObject(netObject, entry);

            return false;
        }

        [HarmonyPatch(typeof(COMMON_PrefabSpawnerBase), "SpawnSpecific")]
        [HarmonyPrefix]
        static bool PrefabSpawnerSpawnSpecific(COMMON_PrefabSpawnerBase __instance, COMMON_PrefabSpawnerBase.SpawnerEntry entryToSpawn)
        {
            __instance.Spawn(entryToSpawn);

            return false;
        }

        [HarmonyPatch(typeof(COMMON_GridPathRandomiser), "Awake")]
        [HarmonyPrefix]
        static bool COMMON_GridPathRandomiserAwake(COMMON_GridPathRandomiser __instance)
        {
            for (int i = 0; i < __instance._numPaths; i++)
            {
                __instance.GeneratePath(FGRandom.Create((uint)i, __instance.GameState.RoundRandomSeed));
            }

            __instance.CreatePathFromData();

            return true;
        }

        [HarmonyPatch(typeof(TipToe_Platform), "OnPlatformTriggerEnter")]
        [HarmonyPrefix]
        static bool TipToe_PlatformOnPlatformTriggerEnter(TipToe_Platform __instance, Collider other)
        {
            if (!__instance.IsFakePlatform)
            {
                __instance.OnRealPlatformLightOn();
            }
            else
            {
                __instance.HandleFakePlatformTriggeredOnClient(other);
            }
            return false;
        }

        [HarmonyPatch(typeof(TipToe_Platform), "OnPlatformTriggerExit")]
        [HarmonyPrefix]
        static bool TipToe_PlatformOnPlatformTriggerExit(TipToe_Platform __instance)
        {
            if (!__instance.IsFakePlatform)
            {
                __instance.OnRealPlatformLightOff();
            }
            return false;
        }

        [HarmonyPatch(typeof(JumpShowdown_PlatformsController), "Start")]
        [HarmonyPrefix]
        static bool JumpShowdown_PlatformsControllerStart(JumpShowdown_PlatformsController __instance)
        {
            __instance._state = JumpShowdown_PlatformsController.State.Picking;
            return false;
        }

        [HarmonyPatch(typeof(JumpShowdown_PlatformsController), "TickBeforeShake")]
        [HarmonyPrefix]
        static bool JumpShowdown_PlatformsControllerTickBeforeShake(JumpShowdown_PlatformsController __instance) // It does not shake for some reason
        {
            __instance._fallTimer -= Time.deltaTime;
            if (__instance._fallTimer <= 0f)
            {
                __instance._shakeTimer = __instance._shakeTimeBeforeFall.RandomValueInRange;
                __instance._platforms[__instance._nextPlatformIndex].OnPlatformShake();
                __instance._state = JumpShowdown_PlatformsController.State.Shaking;
            }
            return false;
        }

        [HarmonyPatch(typeof(JumpShowdown_PlatformsController), "ShakeBeforeFall")]
        [HarmonyPrefix]
        static bool JumpShowdown_PlatformsControllerShakeBeforeFall(JumpShowdown_PlatformsController __instance)
        {
            __instance._shakeTimer -= Time.deltaTime;
            if (__instance._shakeTimer <= 0f)
            {
                __instance._platforms[__instance._nextPlatformIndex].OnPlatformFall();
                __instance._platforms[__instance._nextPlatformIndex] = null;
                __instance._platformsFallen++;
                __instance._state = JumpShowdown_PlatformsController.State.Picking;
            }
            return false;
        }

        [HarmonyPatch(typeof(MotorFunctionSwingStateGrab), "Begin")]
        [HarmonyPostfix]
        static void MotorFunctionSwingStateGrabBegin(MotorFunctionSwingStateGrab __instance)
        {
            __instance.HasServerConfirmedGrab = true;
        }

        [HarmonyPatch(typeof(NPCController), "IsLevelEditor", MethodType.Getter)]
        [HarmonyPatch(typeof(NPCAI), "IsLevelEditor", MethodType.Getter)]
        [HarmonyPrefix]
        static bool NPCAIIsLevelEditor(ref bool __result)
        {            
            __result = true;
            return false;
        }

        [HarmonyPatch(typeof(COMMON_BullRMIController), "Start")]
        [HarmonyPrefix]
        static bool COMMON_BullRMIControllerStart(COMMON_BullRMIController __instance)
        {
            __instance.SetNormal(__instance._bullAI.stateMachine.chaseState.SpeedOverride);

            void SetNormal()
            {
                __instance.SetNormal(__instance._bullAI.stateMachine.chaseState.SpeedOverride);
            }

            __instance._bumper.OnPlayerCollision = (Action)__instance.SetPlayerCollision;

            BullStateCharge chargeState = __instance._bullAI.stateMachine.chargeState;

            chargeState.OnChargeCancel = (Action)__instance.SetCancelCharge;

            chargeState.OnChargeBegin = (Action)__instance.SetCharging;

            chargeState.OnChargeBrake = (Action)__instance.SetBraking;

            chargeState.OnChargeEnd = (Action)SetNormal;
            return false;
        }

        [HarmonyPatch(typeof(MotorFunctionPortalStateActive), "End")]
        [HarmonyPostfix]
        static void MotorFunctionPortalStateActiveEnd(MotorFunctionPortalStateActive __instance)
        {
            __instance._motorFunctionPortal.ClearFailSafe();
        }

        [HarmonyPatch(typeof(COMMON_Hoop), "PlayEnter")]
        [HarmonyPostfix]
        static void COMMON_HoopPlayEnter(COMMON_Hoop __instance, bool isGold, bool firstRun, Vector3 pos)
        {
            //__instance.transform.parent.DOMoveY(pos.y, 2).SetEase(Ease.InOutSine);
            __instance.UpdateVisuals(isGold);
        }
    }
}
