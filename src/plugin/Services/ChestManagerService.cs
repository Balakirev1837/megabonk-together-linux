
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace MegabonkTogether.Services
{
    public interface IChestManagerService
    {
        public uint AddChest(Object chestObject);

        public void PushNextChestId(uint chestId);
        public uint? SetNextChest(Object chestObject);
        public Object? GetChest(uint chestId);
        public void RemoveChest(uint chestId);
        public KeyValuePair<uint, Object> GetChestByReference(OpenChest instance);
        public void ResetForNextLevel();

        public bool TryClaimChest(uint chestId, uint playerId);
        public uint? GetClaimant(uint chestId);
    }
    public class ChestManagerService : IChestManagerService
    {
        private readonly ConcurrentDictionary<uint, Object> chests = [];
        private readonly ConcurrentDictionary<uint, uint> claimedChests = [];
        private readonly ConcurrentQueue<uint> nextIds = [];
        private int nextChestId = -1;

        public bool TryClaimChest(uint chestId, uint playerId)
        {
            return claimedChests.TryAdd(chestId, playerId);
        }

        public uint? GetClaimant(uint chestId)
        {
            if (claimedChests.TryGetValue(chestId, out var playerId))
            {
                return playerId;
            }
            return null;
        }

        public uint AddChest(Object chestObject)
        {
            var chestId = (uint)Interlocked.Increment(ref nextChestId);

            chests.TryAdd(chestId, chestObject);

            return chestId;
        }

        public void PushNextChestId(uint chestId)
        {
            nextIds.Enqueue(chestId);
        }

        public uint? SetNextChest(Object chestObject)
        {
            if (nextIds.TryDequeue(out var chestId))
            {
                chests.TryAdd(chestId, chestObject);
                return chestId;
            }
            else
            {
                Plugin.Log.LogWarning($"No chest id available for spawned chest.");
                return null;
            }
        }

        public Object? GetChest(uint chestId)
        {
            chests.TryGetValue(chestId, out var chestObject);
            return chestObject;
        }
        public void RemoveChest(uint chestId)
        {
            chests.TryRemove(chestId, out _);
        }

        public KeyValuePair<uint, Object> GetChestByReference(OpenChest instance)
        {
            foreach (var kvp in chests)
            {
                if (kvp.Value == instance.gameObject)
                {
                    return kvp;
                }
            }

            return new KeyValuePair<uint, Object>(0, null);
        }

        public void ResetForNextLevel()
        {
            nextChestId = -1;
            nextIds.Clear();
            //chests.Select(kvp => kvp.Value).ToList().ForEach(GameObject.Destroy);
            chests.Clear();
            claimedChests.Clear();
        }
    }
}
