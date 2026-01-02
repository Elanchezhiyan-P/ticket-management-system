using System.Collections.Concurrent;

namespace TicketMS.WebAPI.Middleware.Models
{
    /// <summary>
    /// Tracks errors for a specific client
    /// </summary>
    public class ErrorTracker
    {
        private readonly ConcurrentQueue<DateTime> _errorTimes = new();
        private DateTime? _cooldownStartTime;

        public DateTime LastErrorTime { get; private set; }

        public void AddError()
        {
            var now = DateTime.UtcNow;
            _errorTimes.Enqueue(now);
            LastErrorTime = now;

            // Start cooldown if this is the 3rd error
            if (GetErrorCount(TimeSpan.FromMinutes(5)) >= 3 && _cooldownStartTime == null)
            {
                _cooldownStartTime = now;
            }

            // Keep only recent errors (cleanup old entries)
            while (_errorTimes.TryPeek(out var oldest) && oldest < now.AddMinutes(-10))
            {
                _errorTimes.TryDequeue(out _);
            }
        }

        public int GetErrorCount(TimeSpan window)
        {
            var cutoff = DateTime.UtcNow - window;
            return _errorTimes.Count(t => t >= cutoff);
        }

        public bool IsInCooldown(TimeSpan cooldownDuration)
        {
            if (_cooldownStartTime == null) return false;

            if (DateTime.UtcNow - _cooldownStartTime.Value >= cooldownDuration)
            {
                // Cooldown expired, reset
                _cooldownStartTime = null;
                while (_errorTimes.TryDequeue(out _)) { } // Clear all errors
                return false;
            }

            return true;
        }

        public TimeSpan GetRemainingCooldown(TimeSpan cooldownDuration)
        {
            if (_cooldownStartTime == null) return TimeSpan.Zero;

            var elapsed = DateTime.UtcNow - _cooldownStartTime.Value;
            var remaining = cooldownDuration - elapsed;

            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }
}
