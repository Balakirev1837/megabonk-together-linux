using MegabonkTogether.Common.Models;
using MegabonkTogether.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace MegabonkTogether.Services
{
    public interface IFinalBossOrbManagerService
    {
        public void QueueNextTarget(uint targetId);
        public Tuple<uint, uint> PeakNextTarget();
        public Tuple<uint, uint> GetNextTargetAndOrbId();
        public void SetOrbTarget(uint targetId, GameObject target, uint orbId);

        public uint? RemoveOrbTarget(GameObject go);

        public bool ContainsOrbTarget(GameObject go);

        public void Reset();
        public IEnumerable<BossOrbModel> GetAllOrbs();
        public IEnumerable<BossOrbModel> GetAllOrbsDeltaAndUpdate();
        public GameObject GetOrbById(uint id);
        public uint? GetTargetIdByReference(GameObject go);

        public void ClearQueueNextTarget();
    }

    internal class FinalBossOrbManagerService : IFinalBossOrbManagerService //TODO: Simplify this zzz
    {
        private class OrbInfo
        {
            public uint OrbId { get; set; }
            public uint TargetId { get; set; }
            public GameObject GameObject { get; set; }
        }

        private readonly ConcurrentDictionary<uint, OrbInfo> _orbsById = [];
        private readonly ConcurrentDictionary<uint, BossOrbModel> _previousOrbsDelta = [];
        private readonly ConcurrentQueue<uint> _queuedTargetIds = [];
        private readonly ConcurrentQueue<(uint targetId, uint orbId)> _pendingOrbCreation = [];
        private int _nextOrbId = 0;

        private const float POSITION_THRESHOLD = 0.1f;

        public void QueueNextTarget(uint targetId)
        {
            _queuedTargetIds.Enqueue(targetId);
        }

        public void ClearQueueNextTarget()
        {
            _queuedTargetIds.Clear();
        }

        /// <summary>
        /// Peak and reserve the next target for orb creation, can be called multiple times before GetNextTargetAndOrbId
        /// </summary>
        /// <returns></returns>
        public Tuple<uint, uint> PeakNextTarget()
        {
            if (!_queuedTargetIds.TryPeek(out var targetId))
                return null;

            var orbId = (uint)Interlocked.Increment(ref _nextOrbId);
            _pendingOrbCreation.Enqueue((targetId, orbId));
            return Tuple.Create(targetId, orbId);
        }

        /// <summary>
        /// Get and remove the next target for orb creation, should be called multiple times after PeakNextTarget
        /// </summary>
        /// <returns></returns>
        public Tuple<uint, uint> GetNextTargetAndOrbId()
        {
            if (_pendingOrbCreation.TryDequeue(out var pending))
            {
                return Tuple.Create(pending.targetId, pending.orbId);
            }

            return null;
        }

        public void SetOrbTarget(uint targetId, GameObject target, uint orbId)
        {
            _orbsById[orbId] = new OrbInfo
            {
                OrbId = orbId,
                TargetId = targetId,
                GameObject = target
            };
        }

        public void Reset()
        {
            _orbsById.Clear();
            _previousOrbsDelta.Clear();
            _queuedTargetIds.Clear();
            _pendingOrbCreation.Clear();
            _nextOrbId = 0;
        }

        public bool ContainsOrbTarget(GameObject go)
        {
            return _orbsById.Values.Any(orb => orb.GameObject == go);
        }

        public IEnumerable<BossOrbModel> GetAllOrbs()
        {
            return _orbsById.Values
                .Where(orb => orb.GameObject != null)
                .Select(orb => new BossOrbModel
                {
                    Id = orb.OrbId,
                    Position = Quantizer.Quantize(orb.GameObject.transform.position)
                });
        }

        public IEnumerable<BossOrbModel> GetAllOrbsDeltaAndUpdate()
        {
            var deltas = new List<BossOrbModel>();
            var currentIds = new HashSet<uint>();

            foreach (var kv in _orbsById)
            {
                var id = kv.Key;
                var orbInfo = kv.Value;

                if (orbInfo.GameObject == null) continue;

                currentIds.Add(id);

                var currentModel = new BossOrbModel
                {
                    Id = id,
                    Position = Quantizer.Quantize(orbInfo.GameObject.transform.position)
                };

                if (!_previousOrbsDelta.TryGetValue(id, out var previousModel))
                {
                    _previousOrbsDelta.TryAdd(id, currentModel);
                    deltas.Add(currentModel);
                }
                else if (HasDelta(previousModel, currentModel))
                {
                    previousModel.UpdateFrom(currentModel);
                    deltas.Add(currentModel);
                }
            }

            // Cleanup
            var keysToRemove = _previousOrbsDelta.Keys.Where(id => !currentIds.Contains(id)).ToList();
            foreach (var key in keysToRemove)
            {
                _previousOrbsDelta.TryRemove(key, out _);
            }

            return deltas;
        }

        private bool HasDelta(BossOrbModel previous, BossOrbModel current)
        {
            float positionDelta = Vector3.Distance(
                Quantizer.Dequantize(previous.Position),
                Quantizer.Dequantize(current.Position)
            );

            return positionDelta > POSITION_THRESHOLD;
        }

        public GameObject GetOrbById(uint id)
        {
            return _orbsById.TryGetValue(id, out var orb) ? orb.GameObject : null;
        }

        public uint? RemoveOrbTarget(GameObject go)
        {
            var orbToRemove = _orbsById.FirstOrDefault(kv => kv.Value.GameObject == go);

            if (orbToRemove.Value == null)
                return null;

            _orbsById.TryRemove(orbToRemove.Key, out _);
            _previousOrbsDelta.TryRemove(orbToRemove.Key, out _);
            return orbToRemove.Key;
        }

        public uint? GetTargetIdByReference(GameObject go)
        {
            var orb = _orbsById.Values.FirstOrDefault(o => o.GameObject == go);
            return orb?.TargetId;
        }
    }
}