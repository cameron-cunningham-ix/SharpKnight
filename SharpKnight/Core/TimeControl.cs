using System;

namespace SharpKnight.Core
{
    /// <summary>
    /// Class represents a chess time control (initial time, increment, delay, etc.).
    /// </summary>
    public readonly struct TimeControl
    {
        /// <summary>
        /// Amount of initial time each side gets.
        /// </summary>
        public TimeSpan InitialTime { get; }
        /// <summary>
        /// Amount of increment time to a player after they move.
        /// </summary>
        public TimeSpan Increment { get; }
        /// <summary>
        /// Amount of time needed to elapse for the time remaining to decrement.
        /// </summary>
        public TimeSpan Delay { get; }
        /// <summary>
        /// Number of moves until the next time control period.
        /// </summary>
        public int MovesUntilTimeControl { get; }
        /// <summary>
        /// If true, time never expires and the clock does not count down.
        /// </summary>
        public bool IsInfinite { get; init; }

        /// <summary>
        /// Default: 1 hour base, no increment/delay, no staged time control, finite.
        /// </summary>
        public TimeControl(
            TimeSpan? initial = null,
            TimeSpan? increment = null,
            TimeSpan? delay = null,
            int movesUntilTimeControl = -1,
            bool isInfinite = false)
        {
            InitialTime = initial ?? TimeSpan.FromHours(1);
            Increment = increment ?? TimeSpan.Zero;
            Delay = delay ?? TimeSpan.Zero;
            MovesUntilTimeControl = movesUntilTimeControl;
            IsInfinite = isInfinite;
        }

        /// <summary>
        /// Example format: "300+2d1/40" (300s base, +2s increment, 1s delay, time control at 40 moves).
        /// "-" if infinite.
        /// </summary>
        public override string ToString()
        {
            if (IsInfinite) return "-";

            // Base in seconds
            var baseSecs = (int)InitialTime.TotalSeconds;
            var s = baseSecs.ToString();

            if (Increment > TimeSpan.Zero)
                s += "+" + (int)Increment.TotalSeconds;
            if (Delay > TimeSpan.Zero)
                s += "d" + (int)Delay.TotalSeconds;
            if (MovesUntilTimeControl > 0)
                s += "/" + MovesUntilTimeControl;

            return s;
        }

        /// <summary>
        /// Convenience factory for a 5-minute blitz (no increment/delay).
        /// </summary>
        public static TimeControl Blitz5() => new TimeControl(TimeSpan.FromMinutes(5));
    }
}
