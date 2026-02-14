using MegabonkTogether.Extensions;
using System.Collections.Generic;
using UnityEngine;

namespace MegabonkTogether.Scripts.Snapshot
{
    public class PlayerInterpolator : MonoBehaviour
    {
        protected float interpolationDelayMs = 0.10f;
        protected int maxBufferSize = 30;
        private const float MIN_DELAY = 0.05f;
        private const float MAX_DELAY = 0.25f;
        private const float JITTER_SAMPLE_COUNT = 20;

        private Transform modelTransform;
        private Animator animator;
        private HoverAnimations hoverAnimations;

        private readonly List<PlayerSnapshot> snapshotsBuffer = new List<PlayerSnapshot>();
        private readonly Queue<double> arrivalTimes = new();
        private double lastArrivalTime;
        private float measuredJitter;

        protected void Update()
        {
            if (!HasEnoughSnapshots())
            {
                return;
            }

            double renderTime = Time.timeAsDouble - interpolationDelayMs;
            PerformInterpolation(renderTime);
            CleanupOldSnapshots(renderTime);
        }

        public void Initialize(Transform modelTransform, Animator animator, HoverAnimations hoverAnimations = null)
        {
            this.modelTransform = modelTransform;
            this.animator = animator;
            this.hoverAnimations = hoverAnimations;
        }

        public void AddSnapshot(PlayerSnapshot snapshot)
        {
            var now = Time.timeAsDouble;
            
            if (lastArrivalTime > 0)
            {
                var interval = now - lastArrivalTime;
                arrivalTimes.Enqueue(interval);
                
                if (arrivalTimes.Count > JITTER_SAMPLE_COUNT)
                {
                    arrivalTimes.Dequeue();
                }
                
                UpdateJitterAndDelay();
            }
            lastArrivalTime = now;
            
            snapshotsBuffer.Add(snapshot);

            if (snapshotsBuffer.Count > maxBufferSize)
            {
                snapshotsBuffer.RemoveAt(0);
            }
        }
        
        private void UpdateJitterAndDelay()
        {
            if (arrivalTimes.Count < 3) return;
            
            var intervals = arrivalTimes.ToArray();
            double sum = 0, sumSq = 0;
            foreach (var i in intervals)
            {
                sum += i;
                sumSq += i * i;
            }
            var mean = sum / intervals.Length;
            var variance = (sumSq / intervals.Length) - (mean * mean);
            measuredJitter = Mathf.Sqrt((float)variance);
            
            var targetDelay = Mathf.Clamp(measuredJitter * 3f, MIN_DELAY, MAX_DELAY);
            interpolationDelayMs = Mathf.Lerp(interpolationDelayMs, targetDelay, 0.1f);
        }

        protected bool HasEnoughSnapshots()
        {
            return snapshotsBuffer.Count >= 2;
        }

        protected void PerformInterpolation(double renderTime)
        {
            if (!FindSnapshotPair(renderTime, out PlayerSnapshot older, out PlayerSnapshot newer))
            {
                return;
            }

            float t = CalculateInterpolationFactor(renderTime, older.Timestamp, newer.Timestamp);
            t = Mathf.Clamp01(t);
            InterpolateSnapshot(older, newer, t);
        }

        private bool FindSnapshotPair(double renderTime, out PlayerSnapshot older, out PlayerSnapshot newer)
        {
            older = null;
            newer = null;

            for (int i = 0; i < snapshotsBuffer.Count - 1; i++)
            {
                if (snapshotsBuffer[i].Timestamp <= renderTime &&
                    snapshotsBuffer[i + 1].Timestamp >= renderTime)
                {
                    older = snapshotsBuffer[i];
                    newer = snapshotsBuffer[i + 1];
                    return true;
                }
            }

            return false;
        }

        private float CalculateInterpolationFactor(double renderTime, double olderTime, double newerTime)
        {
            return (float)((renderTime - olderTime) / (newerTime - olderTime));
        }

        private void InterpolateSnapshot(PlayerSnapshot older, PlayerSnapshot newer, float t)
        {
            if (modelTransform == null || animator == null)
            {
                return;
            }

            if (hoverAnimations != null)
            {
                hoverAnimations.defaultPos = Vector3.Lerp(older.Position, newer.Position, t);
            }
            else
            {
                modelTransform.position = Vector3.Lerp(older.Position, newer.Position, t);
            }

            if (newer.Rotation != Quaternion.identity)
            {
                if (hoverAnimations != null)
                {
                    hoverAnimations.defaultRotation = Quaternion.Slerp(older.Rotation, newer.Rotation, t).eulerAngles;
                }
                else
                {
                    modelTransform.rotation = Quaternion.Slerp(older.Rotation, newer.Rotation, t);
                }
            }

            animator.UpdateAnimator(newer.AnimatorState);
        }

        protected void CleanupOldSnapshots(double renderTime)
        {
            while (snapshotsBuffer.Count > 2 &&
                   snapshotsBuffer[0].Timestamp < renderTime - interpolationDelayMs)
            {
                snapshotsBuffer.RemoveAt(0);
            }
        }
    }
}
