using System;
using System.Threading;

namespace MegabonkTogether.Services
{
    public interface INetPlayerContext
    {
        uint? CurrentPlayerId { get; }
        IDisposable BeginScope(uint playerId);
        void Clear();
    }

    public class NetPlayerContext : INetPlayerContext
    {
        private static readonly AsyncLocal<uint?> _currentPlayerId = new();

        public uint? CurrentPlayerId => _currentPlayerId.Value;

        public IDisposable BeginScope(uint playerId)
        {
            var previousValue = _currentPlayerId.Value;
            _currentPlayerId.Value = playerId;
            
            return new ScopeDisposable(() => _currentPlayerId.Value = previousValue);
        }

        public void Clear()
        {
            _currentPlayerId.Value = null;
        }

        private class ScopeDisposable : IDisposable
        {
            private readonly Action _onDispose;
            private bool _disposed;

            public ScopeDisposable(Action onDispose)
            {
                _onDispose = onDispose;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _onDispose();
                }
            }
        }
    }
}
