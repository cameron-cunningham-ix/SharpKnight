using NUnit.Framework;
using SharpKnight.Core;
using System.Diagnostics;

namespace SharpKnight.Tests
{
    [TestFixture]
    public class ExecutionTimeTests
    {
        private ChessBoard board;

        public ExecutionTimeTests()
        {
            board = new ChessBoard();
        }

        [SetUp]
        public void SetUp()
        {
            // Initialize PEXT tables once per test
            PEXT.Initialize();
            board = new ChessBoard();

            // "kiwipete" position
            board.SetupPositionFromFEN(
                "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1");
        }

        [TearDown]
        public void TearDown()
        {
            // No-op
        }

        // --- Helpers ---

        private static long ElapsedNanos(long ticks)
        {
            // Convert Stopwatch ticks to nanoseconds
            return (long)(ticks * (1_000_000_000.0 / Stopwatch.Frequency));
        }

        private static long ElapsedMicros(long ticks)
        {
            // Convert Stopwatch ticks to microseconds
            return (long)(ticks * (1_000_000.0 / Stopwatch.Frequency));
        }

        // --- Tests ---

        [Test]
        public void GetRookAttacksSingle()
        {
            var sw = Stopwatch.StartNew();

            ulong attacks = PEXT.GetRookAttacks(28, board.GetAllPieces());

            sw.Stop();
            Console.WriteLine($"\nGetRookAttacksSingle Elapsed time: {ElapsedNanos(sw.ElapsedTicks)} nanos\n");
            Assert.Pass(); // timing-only
        }

        [Test]
        public void GetRookAttacksMulti()
        {
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < 100; i++)
            {
                ulong attacks = PEXT.GetRookAttacks(i % 64, board.GetAllPieces());
            }

            sw.Stop();
            Console.WriteLine($"\nGetRookAttacksMulti Elapsed time: {ElapsedMicros(sw.ElapsedTicks)} micros\n");
            Assert.Pass();
        }

        [Test]
        public void GetBishopAttacksSingle()
        {
            var sw = Stopwatch.StartNew();

            ulong attacks = PEXT.GetBishopAttacks(28, board.GetAllPieces());

            sw.Stop();
            Console.WriteLine($"\nGetBishopAttacksSingle Elapsed time: {ElapsedNanos(sw.ElapsedTicks)} nanos\n");
            Assert.Pass();
        }

        [Test]
        public void GetBishopAttacksMulti()
        {
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < 100; i++)
            {
                ulong attacks = PEXT.GetBishopAttacks(28, board.GetAllPieces());
            }

            sw.Stop();
            Console.WriteLine($"\nGetBishopAttacksMulti Elapsed time: {ElapsedMicros(sw.ElapsedTicks)} micros\n");
            Assert.Pass();
        }

        [Test]
        public void GetQueenAttacksSingle()
        {
            var sw = Stopwatch.StartNew();

            ulong attacks =
                PEXT.GetRookAttacks(28, board.GetAllPieces()) |
                PEXT.GetBishopAttacks(28, board.GetAllPieces());

            sw.Stop();
            Console.WriteLine($"\nGetQueenAttacksSingle Elapsed time: {ElapsedNanos(sw.ElapsedTicks)} nanos\n");
            Assert.Pass();
        }

        [Test]
        public void GetQueenAttacksMulti()
        {
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < 100; i++)
            {
                ulong attacks =
                    PEXT.GetRookAttacks(28, board.GetAllPieces()) |
                    PEXT.GetBishopAttacks(28, board.GetAllPieces());
            }

            sw.Stop();
            Console.WriteLine($"\nGetQueenAttacksMulti Elapsed time: {ElapsedMicros(sw.ElapsedTicks)} micros\n");
            Assert.Pass();
        }

        [Test]
        public void ChessBoardAttacksToSquareSingle()
        {
            var sw = Stopwatch.StartNew();

            ulong attacks = board.OppAttacksToSquare(28, Color.WHITE);

            sw.Stop();
            Console.WriteLine($"\nChessBoardAttacksToSquareSingle Elapsed time: {ElapsedNanos(sw.ElapsedTicks)} nanos\n");
            Assert.Pass();
        }

        [Test]
        public void ChessBoardAttacksToSquareMulti()
        {
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < 100; i++)
            {
                ulong attacks = board.OppAttacksToSquare(i % 64, Color.WHITE);
            }

            sw.Stop();
            Console.WriteLine($"\nChessBoardAttacksToSquareMulti Elapsed time: {ElapsedMicros(sw.ElapsedTicks)} micros\n");
            Assert.Pass();
        }

        [Test]
        public void MakeMoveD1()
        {
            int moveNum = 0;
            var moves = MoveGenerator.GenerateLegalMoves(board, ref moveNum);

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < moveNum; i++)
            {
                board.MakeMove(moves[i], searching: true);
                board.UnmakeMove(moves[i], searching: true);
            }

            sw.Stop();
            Console.WriteLine($"\nMakeMoveD1 Elapsed time: {ElapsedMicros(sw.ElapsedTicks)} micros\n");
            Assert.Pass();
        }
    }
}
