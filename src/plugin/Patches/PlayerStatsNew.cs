using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(PlayerStatsNew))]
    internal static class PlayerStatsNewPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();
        private static readonly INetPlayerContext netPlayerContext = Plugin.Services.GetService<INetPlayerContext>();

        [HarmonyPrefix]
        [HarmonyPatch(nameof(PlayerStatsNew.GetStat))]
        public static bool GetStat_Prefix(EStat stat, ref float __result)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            var currentPlayerId = netPlayerContext.CurrentPlayerId;
            if (currentPlayerId.HasValue && playerManagerService.IsRemoteConnectionId(currentPlayerId.Value))
            {
                var netPlayer = playerManagerService.GetNetPlayerByNetplayId(currentPlayerId.Value);
                if (netPlayer == null)
                {
                    return true;
                }
                __result = netPlayer.Inventory.playerStats.GetStat(stat);
                return false;
            }

            return true;
        }
    }
}
