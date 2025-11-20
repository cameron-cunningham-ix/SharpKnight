namespace SharpKnight.Core
{
    /// <summary>
    /// Thread-safe chess clock that supports increments, delays, and staged time controls.
    /// </summary>
    public sealed class ChessClock
    {
        /// <summary>
        /// TimeControl of this ChessClock.
        /// </summary>
        private TimeControl _tc;

        /// <summary>
        /// Amount of time remaining for white.
        /// </summary>
        private TimeSpan _whiteTimeRemaining;
        /// <summary>
        /// Amount of time remaining for black.
        /// </summary>
        private TimeSpan _blackTimeRemaining;

        /// <summary>
        /// If the clock is currently running or is stopped.
        /// </summary>
        private bool _isRunning;
        /// <summary>
        /// The color of the player to move.
        /// </summary>
        private Color _activeColor;
        /// <summary>
        /// Number of moves played on this clock.
        /// </summary>
        private int _moveCount;

        /// <summary>
        /// 
        /// </summary>
        private DateTime _lastUpdateUtc;
        private readonly object _lock = new();

        /// <summary>Default: 5 minutes base.</summary>
        public ChessClock()
            : this(new TimeControl(initial: TimeSpan.FromMinutes(5)))
        { }

        /// <summary>
        /// Construct clock based on TimeControl tc.
        /// </summary>
        /// <param name="tc"></param>
        public ChessClock(in TimeControl tc)
        {
            _tc = tc;
            _whiteTimeRemaining = tc.InitialTime;
            _blackTimeRemaining = tc.InitialTime;
            _isRunning = false;
            _activeColor = Color.WHITE;
            _moveCount = 0;
            _lastUpdateUtc = DateTime.UtcNow;
        }

        /// <returns>Current set TimeControl of this clock.</returns>
        public TimeControl GetTimeControl() => _tc;

        /// <summary>
        /// Set the TimeControl of this clock and resets.
        /// </summary>
        /// <param name="tc"></param>
        public void SetTimeControl(in TimeControl tc)
        {
            lock (_lock)
            {
                _tc = tc;
                _whiteTimeRemaining = tc.InitialTime;
                _blackTimeRemaining = tc.InitialTime;
                _moveCount = 0;
            }
        }

        /// <summary>
        /// Set whether the TimeControl is infinite time.
        /// </summary>
        /// <param name="isInfinite"></param>
        public void SetInfinite(bool isInfinite)
        {
            _tc = _tc with { IsInfinite = isInfinite };
        }

        /// <summary>
        /// Add more time remaining for a color.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="amount"></param>
        public void AddTime(Color color, TimeSpan amount)
        {
            lock (_lock)
            {
                if (color == Color.WHITE) _whiteTimeRemaining += amount;
                else _blackTimeRemaining += amount;
            }
        }

        /// <summary>
        /// Set the time remaining for a color.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="amount"></param>
        public void SetTime(Color color, TimeSpan amount)
        {
            lock (_lock)
            {
                if (color == Color.WHITE) _whiteTimeRemaining = amount;
                else _blackTimeRemaining = amount;
            }
        }

        // ===== Clock control =====

        /// <summary>
        /// Start the clock running.
        /// </summary>
        public void Start()
        {
            lock (_lock)
            {
                if (!_isRunning)
                {
                    _isRunning = true;
                    _lastUpdateUtc = DateTime.UtcNow;
                }
            }
        }

        /// <summary>
        /// Stop the clock running.
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                if (_isRunning)
                {
                    UpdateTime_NoLock();
                    _isRunning = false;
                }
            }
        }

        // ===== Player actions =====

        /// <summary>
        /// Updates the clock time and switch the active player.
        /// </summary>
        public void SwitchPlayer()
        {
            lock (_lock)
            {
                UpdateTime_NoLock();
                _activeColor = _activeColor == Color.WHITE ? Color.BLACK : Color.WHITE;
                _lastUpdateUtc = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Call when the active player completes a move.
        /// Applies increment to the player who moved, handles staged time controls, then switches sides.
        /// </summary>
        public void MakeMove()
        {
            lock (_lock)
            {
                UpdateTime_NoLock();
                _moveCount++;

                // staged time controls (e.g., +base after 40/60/etc.)
                if (_tc.MovesUntilTimeControl > 0)
                {
                    if (_moveCount % _tc.MovesUntilTimeControl == _tc.MovesUntilTimeControl - 1
                        && _activeColor == Color.WHITE)
                    {
                        _whiteTimeRemaining += _tc.InitialTime;
                    }
                    else if (_moveCount % _tc.MovesUntilTimeControl == 0
                             && _activeColor == Color.BLACK)
                    {
                        _blackTimeRemaining += _tc.InitialTime;
                    }
                }

                ApplyIncrement_NoLock(_activeColor);
                _activeColor = _activeColor == Color.WHITE ? Color.BLACK : Color.WHITE;
                _lastUpdateUtc = DateTime.UtcNow;
            }
        }

        // ===== Queries =====

        /// <returns>True if clock is running, false if not.</returns>
        public bool IsClockRunning() => _isRunning;
        /// <returns>True if clock time is infinite, false if not.</returns>
        public bool IsInfinite() => _tc.IsInfinite;
        /// <returns>The color of the player to move.</returns>
        public Color GetActiveColor() => _activeColor;
        /// <returns>Number of moves played.</returns>
        public int GetMoveCount() => _moveCount;
        /// <returns>Time remaining for white.</returns>
        public TimeSpan GetWhiteTime()
        {
            lock (_lock)
            {
                if (!_isRunning || _tc.IsInfinite || _activeColor != Color.WHITE)
                    return _whiteTimeRemaining;

                var elapsed = DateTime.UtcNow - _lastUpdateUtc;
                if (_tc.Delay > TimeSpan.Zero && elapsed < _tc.Delay)
                    return _whiteTimeRemaining;

                var final = _whiteTimeRemaining - elapsed;
                return final > TimeSpan.Zero ? final : TimeSpan.Zero;
            }
        }
        /// <returns>Time remaining for black.</returns>
        public TimeSpan GetBlackTime()
        {
            lock (_lock)
            {
                if (!_isRunning || _tc.IsInfinite || _activeColor != Color.BLACK)
                    return _blackTimeRemaining;

                var elapsed = DateTime.UtcNow - _lastUpdateUtc;
                if (_tc.Delay > TimeSpan.Zero && elapsed < _tc.Delay)
                    return _blackTimeRemaining;

                var final = _blackTimeRemaining - elapsed;
                return final > TimeSpan.Zero ? final : TimeSpan.Zero;
            }
        }
        /// <returns>True if one color's time is up, false if both have time remaining.</returns>
        public bool IsTimeUp()
        {
            if (_tc.IsInfinite) return false;
            lock (_lock)
            {
                return GetWhiteTime_NoLock() <= TimeSpan.Zero
                    || GetBlackTime_NoLock() <= TimeSpan.Zero;
            }
        }
        /// <returns>True if white's time is up, false if not.</returns>
        public bool IsWhiteTimeUp() =>
            !_tc.IsInfinite && GetWhiteTime_NoLock() <= TimeSpan.Zero;
        /// <returns>True if black's time is up, false if not.</returns>
        public bool IsBlackTimeUp() =>
            !_tc.IsInfinite && GetBlackTime_NoLock() <= TimeSpan.Zero;

        // ===== Internals =====

        /// <summary>
        /// Updates the time remaining of color to move.
        /// </summary>
        private void UpdateTime_NoLock()
        {
            if (!_isRunning || _tc.IsInfinite) { _lastUpdateUtc = DateTime.UtcNow; return; }

            var now = DateTime.UtcNow;
            var elapsed = now - _lastUpdateUtc;

            // Only decrement outside of the delay window
            if (!IsInDelay_NoLock(now))
            {
                if (_activeColor == Color.WHITE)
                {
                    _whiteTimeRemaining -= elapsed;
                    if (_whiteTimeRemaining < TimeSpan.Zero)
                        _whiteTimeRemaining = TimeSpan.Zero;
                }
                else
                {
                    _blackTimeRemaining -= elapsed;
                    if (_blackTimeRemaining < TimeSpan.Zero)
                        _blackTimeRemaining = TimeSpan.Zero;
                }
            }

            _lastUpdateUtc = now;
        }
        /// <param name="nowUtc"></param>
        /// <returns>True if the current time is still within the delay window, false if not.</returns>
        private bool IsInDelay_NoLock(DateTime nowUtc)
        {
            if (_tc.Delay <= TimeSpan.Zero) return false;
            var elapsed = nowUtc - _lastUpdateUtc;
            return elapsed < _tc.Delay;
        }

        /// <summary>
        /// Adds time increment to given color.
        /// </summary>
        /// <param name="color"></param>
        private void ApplyIncrement_NoLock(Color color)
        {
            if (_tc.Increment <= TimeSpan.Zero) return;
            if (color == Color.WHITE) _whiteTimeRemaining += _tc.Increment;
            else _blackTimeRemaining += _tc.Increment;
        }
        /// <returns>Current time remaining for white.</returns>
        private TimeSpan GetWhiteTime_NoLock()
        {
            if (!_isRunning || _tc.IsInfinite || _activeColor != Color.WHITE)
                return _whiteTimeRemaining;
            var now = DateTime.UtcNow;
            var elapsed = now - _lastUpdateUtc;
            if (_tc.Delay > TimeSpan.Zero && elapsed < _tc.Delay)
                return _whiteTimeRemaining;
            var final = _whiteTimeRemaining - elapsed;
            return final > TimeSpan.Zero ? final : TimeSpan.Zero;
        }
        /// <returns>Current time remaining for black.</returns>
        private TimeSpan GetBlackTime_NoLock()
        {
            if (!_isRunning || _tc.IsInfinite || _activeColor != Color.BLACK)
                return _blackTimeRemaining;
            var now = DateTime.UtcNow;
            var elapsed = now - _lastUpdateUtc;
            if (_tc.Delay > TimeSpan.Zero && elapsed < _tc.Delay)
                return _blackTimeRemaining;
            var final = _blackTimeRemaining - elapsed;
            return final > TimeSpan.Zero ? final : TimeSpan.Zero;
        }
    }
}
