using NUnit.Framework;
using SharpKnight.Core;

namespace SharpKnight.Tests
{
    [TestFixture]
    public class ChessClockTests
    {
        // Helper: create a standard time control (5+3, no delay)
        private TimeControl CreateStandardTimeControl()
        {
            return new TimeControl(
                initial: TimeSpan.FromMinutes(5),
                increment: TimeSpan.FromSeconds(3),
                delay: TimeSpan.Zero,
                movesUntilTimeControl: -1,
                isInfinite: false
            );
        }

        // Helper: sleep
        private static void WaitFor(TimeSpan duration) => Thread.Sleep(duration);

        // --- Initialization ---
        [Test]
        public void Initialization()
        {
            var tc = CreateStandardTimeControl();
            var clock = new ChessClock(tc);

            Assert.That(clock.GetWhiteTime(), Is.EqualTo(TimeSpan.FromMinutes(5)));
            Assert.That(clock.GetBlackTime(), Is.EqualTo(TimeSpan.FromMinutes(5)));
            Assert.That(clock.GetActiveColor(), Is.EqualTo(Color.WHITE));
            Assert.That(clock.IsClockRunning(), Is.Not.True);
            Assert.That(clock.GetMoveCount(), Is.EqualTo(0));
        }

        // --- Basic clock Start/stop ---
        [Test]
        public void BasicClockOperation()
        {
            var tc = CreateStandardTimeControl();
            var clock = new ChessClock(tc);

            clock.Start();
            Assert.That(clock.IsClockRunning(), Is.True);

            WaitFor(TimeSpan.FromSeconds(1));
            Assert.That(clock.GetWhiteTime(), Is.LessThan(TimeSpan.FromMinutes(5)));

            clock.Stop();
            Assert.That(clock.IsClockRunning(), Is.Not.True);
        }

        // --- Player switching and increment application ---
        [Test]
        public void PlayerSwitching()
        {
            var tc = CreateStandardTimeControl();
            var clock = new ChessClock(tc);

            clock.Start();
            Assert.That(clock.GetActiveColor(), Is.EqualTo(Color.WHITE));

            var initialWhiteTime = clock.GetWhiteTime();
            WaitFor(TimeSpan.FromSeconds(1));

            // expected: initial - elapsed + increment
            var expectedWhiteTime = initialWhiteTime - TimeSpan.FromSeconds(1) + TimeSpan.FromSeconds(3);

            clock.MakeMove();

            Assert.That(clock.GetActiveColor(), Is.EqualTo(Color.BLACK));

            // Allow 100 ms tolerance for elapsed measurement
            var whiteDelta = (clock.GetWhiteTime() - expectedWhiteTime).Duration();
            Assert.That(whiteDelta, Is.LessThanOrEqualTo(TimeSpan.FromMilliseconds(100)));

            // Black time should still be ~5:00 (allow 10 ms tolerance)
            var blackDelta = (clock.GetBlackTime() - TimeSpan.FromMinutes(5)).Duration();
            Assert.That(blackDelta, Is.LessThan(TimeSpan.FromMilliseconds(10)));
        }

        // --- Increment added after move ---
        [Test]
        public void TimeIncrement()
        {
            var tc = CreateStandardTimeControl();
            var clock = new ChessClock(tc);

            clock.Start();
            WaitFor(TimeSpan.FromSeconds(1));
            var whiteBefore = clock.GetWhiteTime();

            clock.MakeMove();

            Assert.That(clock.GetWhiteTime(), Is.GreaterThanOrEqualTo(whiteBefore));
        }

        // --- Delay behavior (Bronstein/US delay semantics assumed by engine) ---
        [Test]
        public void MoveDelay()
        {
            var tc = new TimeControl(
                initial: TimeSpan.FromMinutes(5),
                increment: TimeSpan.Zero,
                delay: TimeSpan.FromSeconds(2)
            );
            var clock = new ChessClock(tc);

            clock.Start();
            var initial = clock.GetWhiteTime();

            WaitFor(TimeSpan.FromMilliseconds(1500));
            // Within delay, time shouldn't decrease
            Assert.That(clock.GetWhiteTime(), Is.EqualTo(initial));

            WaitFor(TimeSpan.FromMilliseconds(1000));
            // Past delay, time should have Started decreasing
            Assert.That(clock.GetWhiteTime(), Is.LessThan(initial));
        }

        // --- Infinite time control ---
        [Test]
        public void InfiniteTimeControl()
        {
            var tc = new TimeControl(
                initial: TimeSpan.FromMinutes(5),
                increment: TimeSpan.Zero,
                delay: TimeSpan.Zero,
                movesUntilTimeControl: -1,
                isInfinite: true
            );
            var clock = new ChessClock(tc);

            clock.Start();
            WaitFor(TimeSpan.FromSeconds(2));

            Assert.That(clock.GetWhiteTime(), Is.EqualTo(TimeSpan.FromMinutes(5)));
            Assert.That(clock.IsTimeUp(), Is.Not.True);
        }

        // --- Time forfeit detection ---
        [Test]
        public void TimeForfeit()
        {
            var tc = new TimeControl(
                initial: TimeSpan.FromSeconds(1),
                increment: TimeSpan.Zero,
                delay: TimeSpan.Zero
            );
            var clock = new ChessClock(tc);

            clock.Start();
            WaitFor(TimeSpan.FromMilliseconds(1100));

            Assert.That(clock.IsTimeUp(), Is.True);
            Assert.That(clock.IsWhiteTimeUp(), Is.True);
            Assert.That(clock.IsBlackTimeUp(), Is.Not.True);
        }

        // --- Multiple time periods (e.g., add 5 minutes every 2 movesUntilTimeControl) ---
        [Test]
        public void MultipleTimePeriods()
        {
            var tc = new TimeControl(
                initial: TimeSpan.FromMinutes(5),
                increment: TimeSpan.Zero,
                delay: TimeSpan.Zero,
                movesUntilTimeControl: 2 // new time control every 2 movesUntilTimeControl
            );
            var clock = new ChessClock(tc);

            clock.Start();
            WaitFor(TimeSpan.FromSeconds(1));
            clock.MakeMove(); // move 1 - white finishes, increment period
            clock.MakeMove(); // move 2 - black finishes, increment period

            // After 2 total movesUntilTimeControl, white should have received another 5 minutes (net > 6 due to the earlier 1s spent)
            Assert.That(clock.GetWhiteTime(), Is.GreaterThan(TimeSpan.FromMinutes(6)));
        }

        // --- Pause and resume ---
        [Test]
        public void PauseResume()
        {
            var tc = CreateStandardTimeControl();
            var clock = new ChessClock(tc);

            clock.Start();
            WaitFor(TimeSpan.FromSeconds(1));
            var beforePause = clock.GetWhiteTime();

            clock.Stop();
            WaitFor(TimeSpan.FromSeconds(1));
            Assert.That(clock.GetWhiteTime(), Is.InRange(beforePause - TimeSpan.FromMilliseconds(1), beforePause + TimeSpan.FromMilliseconds(1)));

            clock.Start();
            WaitFor(TimeSpan.FromSeconds(1));
            Assert.That(clock.GetWhiteTime(), Is.LessThan(beforePause));
        }

        // --- Manual time adjustments ---
        [Test]
        public void TimeAdjustment()
        {
            var tc = CreateStandardTimeControl();
            var clock = new ChessClock(tc);

            clock.AddTime(Color.WHITE, TimeSpan.FromMinutes(1));
            Assert.That(clock.GetWhiteTime(), Is.EqualTo(TimeSpan.FromMinutes(6)));

            clock.SetTime(Color.BLACK, TimeSpan.FromMinutes(3));
            Assert.That(clock.GetBlackTime(), Is.EqualTo(TimeSpan.FromMinutes(3)));
        }

        // --- Move counter increments on each move ---
        [Test]
        public void MoveCounter()
        {
            var tc = CreateStandardTimeControl();
            var clock = new ChessClock(tc);

            Assert.That(clock.GetMoveCount(), Is.EqualTo(0));

            clock.Start();
            clock.MakeMove(); // White movesUntilTimeControl
            Assert.That(clock.GetMoveCount(), Is.EqualTo(1));

            clock.MakeMove(); // Black movesUntilTimeControl
            Assert.That(clock.GetMoveCount(), Is.EqualTo(2));
        }

        // --- Thread safety smoke test ---
        [Test]
        public void ThreadSafety()
        {
            var tc = CreateStandardTimeControl();
            var clock = new ChessClock(tc);
            clock.Start();

            var threads = new List<Thread>();
            for (int i = 0; i < 10; i++)
            {
                threads.Add(new Thread(() =>
                {
                    for (int j = 0; j < 100; j++)
                    {
                        _ = clock.GetWhiteTime();
                        _ = clock.GetBlackTime();
                        Thread.Sleep(1);
                    }
                }));
            }

            threads.ForEach(t => t.Start());
            threads.ForEach(t => t.Join());

            Assert.Pass(); // no crashes / races observed
        }

        // --- Edge cases: idempotent Start/stop; increment on move while stopped ---
        [Test]
        public void EdgeCases()
        {
            var tc = CreateStandardTimeControl();
            var clock = new ChessClock(tc);

            // Stopping when already stopped
            clock.Stop();
            Assert.That(clock.IsClockRunning(), Is.False);
            clock.Stop();
            Assert.That(clock.IsClockRunning(), Is.False);

            // Starting when already Started
            clock.Start();
            Assert.That(clock.IsClockRunning(), Is.True);
            clock.Start();
            Assert.That(clock.IsClockRunning(), Is.True);

            // Switching players when clock is stopped still applies increment per rules
            clock.Stop();
            var whiteBefore = clock.GetWhiteTime();
            clock.MakeMove(); // should still grant increment even if not running
            Assert.That(clock.GetWhiteTime(), Is.EqualTo(whiteBefore + TimeSpan.FromSeconds(3)));
        }
    }
}
