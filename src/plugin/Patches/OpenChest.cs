using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Utility;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using UnityEngine;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(OpenChest))]
    internal static class OpenChestPatches
    {
        private static readonly Services.ISynchronizationService synchronizationService = Plugin.Services.GetService<Services.ISynchronizationService>();
        private static readonly Services.IChestManagerService chestManagerService = Plugin.Services.GetService<Services.IChestManagerService>();

        [HarmonyPrefix]
        [HarmonyPatch(nameof(OpenChest.OnTriggerStay))]
        public static bool OnTriggerStay_Prefix(Collider other, OpenChest __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            // Only intercept for the local player
            if (other != GameManager.Instance.player.GetComponent<Collider>())
            {
                return true;
            }

            if (__instance.pickedup || __instance.readyForPickupTime > MyTime.time)
            {
                return true;
            }

            var chestSpawned = chestManagerService.GetChestByReference(__instance);
            if (chestSpawned.Value == null)
            {
                return true;
            }

            // If the server granted us this chest, allow the original logic to run
            if (synchronizationService.IsChestGranted(chestSpawned.Key))
            {
                return true;
            }

            // Otherwise, if we haven't requested it yet, request it now
            if (!synchronizationService.IsChestPending(chestSpawned.Key))
            {
                synchronizationService.RequestChestOpen(chestSpawned.Key);
            }

            // Block original interaction while pending or not granted
            return false;
        }

        /// <summary>
        /// Send chest opened info to other players only when local player opens a chest.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(OpenChest.OnTriggerStay))]
        public static void OnTriggerStay_Postfix(Collider other, OpenChest __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            if (other != GameManager.Instance.player.GetComponent<Collider>())
            {
                return;
            }

            var chestSpawned = chestManagerService.GetChestByReference(__instance);
            if (chestSpawned.Value == null)
            {
                return;
            }

            // Only send if we actually have the grant and it's opening
            if (synchronizationService.IsChestGranted(chestSpawned.Key))
            {
                 // We need to be careful not to send this multiple times.
                 // But SynchronizationService.OnChestOpened might already handle it or the original logic might.
                 synchronizationService.OnChestOpened(__instance);
            }
        }
    }
}