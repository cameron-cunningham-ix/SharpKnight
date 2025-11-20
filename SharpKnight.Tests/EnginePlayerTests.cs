using NUnit.Framework;
using SharpKnight.Core;
using SharpKnight.Players;
using SharpKnight.Engines;

namespace SharpKnight.Tests
{
    // Mock chess engine for testing
    internal sealed class MockEngine : ChessEngineBase
    {
        private DenseMove _moveToReturn;

        public MockEngine()
            : base("MockEngine", "1.0", "Test Author", defaultDepth: 2)
        { }

        public override DenseMove FindBestMove(ref ChessBoard board, ref ChessClock clock, int maxDepth = -1)
        {
            StartSearch();
            // Simulate "thinking" proportional to depth
            var depth = (maxDepth > 0 ? maxDepth : SearchDepth);
            Thread.Sleep(50 * depth);
            EndSearch();
            return _moveToReturn;
        }

        public override int EvaluatePosition(in ChessBoard board) => 0;

        public void setMoveToReturn(in DenseMove move) => _moveToReturn = move;
    }

    [TestFixture]
    public class EnginePlayerTests
    {
        private MockEngine mockEngine;
        private EnginePlayer player;
        private ChessBoard board;
        private GameState state;
        private TimeControl tc;
        private ChessClock clock;

        public EnginePlayerTests()
        {
            // 5+3, no delay
            tc = new TimeControl(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(3), TimeSpan.Zero);
            clock = new ChessClock(tc);
            board = new ChessBoard();
            state = new GameState();
        }

        [SetUp]
        public void SetUp()
        {
            // Initialize PEXT tables just like in the C++ tests
            PEXT.Initialize();

            mockEngine = new MockEngine();
            player = new EnginePlayer(mockEngine, acceptDrawOffers: false);
        }

        [TearDown]
        public void TearDown()
        {
            player?.OnGameEnd();
            player?.Dispose();
            player = null;
            mockEngine = null;
        }

        // Test basic initialization
        [Test]
        public void Initialization()
        {
            Assert.That(player.IsThinking(), Is.False);
            Assert.That(player.GetPlayerType(), Is.EqualTo(PlayerType.Engine));
            Assert.That(player.AcceptsDraw(), Is.False);
        }

        // Test getting a move
        [Test]
        public void GetMove()
        {
            // e2e4 (from E2 to E4)
            var expectedMove = DenseMove.FromPieceType(PieceType.W_PAWN, 12, 28);
            var newMock = new MockEngine();
            newMock.setMoveToReturn(expectedMove);

            var testPlayer = new EnginePlayer(newMock);

            DenseMove actual = testPlayer.GetMove(board, clock);

            Assert.That(actual.GetFrom(), Is.EqualTo(expectedMove.GetFrom()));
            Assert.That(actual.GetTo(), Is.EqualTo(expectedMove.GetTo()));
            Assert.That(actual.GetPieceType(), Is.EqualTo(expectedMove.GetPieceType()));
        }

        // Test time management
        [Test]
        public void TimeManagement()
        {
            Directory.CreateDirectory("TestOutput");
            using var outfile = new StreamWriter(Path.Combine("TestOutput", "EnginePlayerTest_TimeManagement.txt"));
            outfile.WriteLine("EnginePlayerTest_TimeManagement.txt:");

            // Capture Console.Out similar to GoogleTest CaptureStdout
            var prev = Console.Out;
            using var writer = new StringWriter();
            Console.SetOut(writer);

            var newMock = new MockEngine();
            var timeControlledPlayer = new EnginePlayer(newMock, acceptDrawOffers: false);

            var start = DateTime.UtcNow;
            timeControlledPlayer.GetMove(board, clock);
            var elapsed = DateTime.UtcNow - start;

            // Expect >= 100ms due to Thread.Sleep(50 * depth) with depth~2 by default
            Assert.That(elapsed.TotalMilliseconds, Is.GreaterThanOrEqualTo(100));

            Console.Out.Flush();
            outfile.Write(writer.ToString());

            // restore
            Console.SetOut(prev);
        }

        // Test UCI protocol commands
        [Test]
        public void UCICommands()
        {
            player.Uci();
            Assert.That(player.WaitForInitialization(), Is.True);

            player.SetOption("Hash", "32");

            // Empty FEN => startpos; one move e2e4
            string moves = "e2e4";
            player.Position("", moves);

            player.IsReady();

            player.Stop();
            Assert.That(player.IsThinking(), Is.False);
        }

        // Test engine with random engine
        [Test]
        public void RandomEngineIntegration()
        {
            var randomEngine = new RandomEngine();
            var randomPlayer = new EnginePlayer(randomEngine);

            var moves = new List<DenseMove>();
            for (int i = 0; i < 5; i++)
            {
                moves.Add(randomPlayer.GetMove(board, clock));
            }

            // Verify there is some variability
            bool hasDifferent = moves.Any(m => m.GetFrom() != moves[0].GetFrom() || m.GetTo() != moves[0].GetTo());
            Assert.That(hasDifferent, Is.True);
        }

        // Test game end notification
        [Test]
        public void GameEnd()
        {
            var t = new Thread(() =>
            {
                player.GetMove(board, clock);
            });
            t.Start();

            // Give engine a moment to enter searching
            Thread.Sleep(10);

            player.OnGameEnd();

            if (t.IsAlive) t.Join();
            Assert.That(player.IsThinking(), Is.False);
        }

        // Test opponent move notification
        [Test]
        public void OpponentMoveNotification()
        {
            DenseMove opp = DenseMove.FromPieceType(PieceType.B_PAWN, 52, 36); // e7e5
            player.NotifyOpponentMove(opp);

            // Mostly behavioral; ensure no crash and engine isn't stuck in thinking state
            Assert.That(player.IsThinking(), Is.False);
        }

        // Test concurrent UCI command handling
        [Test]
        public void ConcurrentCommands()
        {
            var t = new Thread(() =>
            {
                player.GetMove(board, clock);
            });
            t.Start();

            player.IsReady();  // Should be responsive
            player.Stop();     // Should stop the search

            if (t.IsAlive) t.Join();

            Assert.That(player.IsThinking(), Is.False);
        }

        // Test error handling
        [Test]
        public void ErrorHandling1()
        {
            // Invalid option should not crash
            player.SetOption("InvalidOption", "value");

            // Invalid position FEN handled gracefully
            player.Position("invalid fen", "");

            // Invalid move string should be handled gracefully
            string invalidMoves = "a1a1";
            player.Position("", invalidMoves);

            Assert.Pass("Handled invalid inputs without crashing.");
        }

    }
}
