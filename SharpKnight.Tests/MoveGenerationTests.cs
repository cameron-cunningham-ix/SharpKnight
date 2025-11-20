using SharpKnight.Core;
using NUnit.Framework;

namespace SharpKnight.Tests
{
    [TestFixture]
    public class MoveGenerationTests
    {
        private ChessBoard board = null!;
        private DenseMove[] moves = null!;

        [SetUp]
        public void SetUp()
        {
            // Initialize PEXT
            PEXT.Initialize();

            board = new ChessBoard();
            moves = new DenseMove[Consts.MAX_MOVES];
        }

        [TearDown]
        public void TearDown()
        {
            // Clear the move buffer
            for (int i = 0; i < moves.Length; i++) moves[i] = default;
        }

        // Helper: does the generated list contain from->to?
        private static bool ContainsMove(DenseMove[] m, int moveNum, int from, int to)
            => m.Take(moveNum).Any(x => x.GetFrom() == from && x.GetTo() == to);

        private void SetBoard(ChessBoard newBoard) => board = newBoard;

        // ---- Tests ----

        [Test]
        public void InitialWhitePawnMoves()
        {
            int moveNum = 0;
            moves = MoveGenerator.GenerateLegalMoves(board, ref moveNum);

            int pawnMoves = 0;
            for (int i = 0; i < moveNum; i++)
            {
                if (moves[i].GetPieceType() == PieceType.W_PAWN) pawnMoves++;
            }

            Assert.That(pawnMoves, Is.EqualTo(16)); // All initial white pawn pushes (8 single + 8 double)
        }

        [Test]
        public void InitialBlackPawnMoves()
        {
            board.SetupPositionFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR b KQkq - 0 1");

            int moveNum = 0;
            moves = MoveGenerator.GenerateLegalMoves(board, ref moveNum);

            int pawnMoves = 0;
            for (int i = 0; i < moveNum; i++)
            {
                if (moves[i].GetPieceType() == PieceType.B_PAWN) pawnMoves++;
            }

            Assert.That(pawnMoves, Is.EqualTo(16)); // All initial black pawn pushes
        }

        [Test]
        public void InitialKnightMoves()
        {
            int moveNum = 0;
            moves = MoveGenerator.GenerateLegalMoves(board, ref moveNum);

            int knightMoves = 0;
            for (int i = 0; i < moveNum; i++)
            {
                if (moves[i].GetPieceType() == PieceType.W_KNIGHT)
                {
                    knightMoves++;
                    Console.WriteLine(moves[i].ToString());
                }
            }
            

            // Nb1-c3 and Nb1-a3
            Assert.That(ContainsMove(moves, moveNum, 1, 16), Is.True);  // b1 -> c3
            Assert.That(ContainsMove(moves, moveNum, 1, 18), Is.True);  // b1 -> a3
            Assert.That(knightMoves, Is.EqualTo(4)); // two knights, two moves each
        }

        [Test]
        public void AllInitialMoves()
        {
            board.SetupPositionFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w - - 0 1");

            int moveNum = 0;
            moves = MoveGenerator.GenerateLegalMoves(board, ref moveNum);

            for (int i = 0; i < moveNum; i++)
            {
                Console.WriteLine(moves[i].ToString());
            }

            Assert.That(moveNum, Is.EqualTo(20));
        }

        [Test]
        public void NotStalemate()
        {
            board.SetupPositionFromFEN("7Q/8/8/2k5/P1P1B1P1/N3PP2/P6P/R1B1K1NR b KQ - 0 24");
            board.PrintBitboards();

            int moveNum = 0;
            moves = MoveGenerator.GenerateLegalMoves(board, ref moveNum);

            for (int i = 0; i < moveNum; i++)
            {
                Console.WriteLine(moves[i].ToString());
            }

            Assert.That(moveNum, Is.EqualTo(3));
        }

        [Test]
        public void Illegal1()
        {
            board.SetupPositionFromFEN("r1br4/p1p3k1/1p5p/2p1b2q/2P1B3/4NR2/PP4PP/R3Q1K1 w - - 4 13");

            int moveNum = 0;
            moves = MoveGenerator.GenerateLegalMoves(board, ref moveNum);

            Assert.That(!ContainsMove(moves, moveNum, BUTIL.F4, BUTIL.E5));
        }

        [Test]
        public void Illegal2()
        {
            board.SetupPositionFromFEN("3r4/1ppk1rb1/3p2pp/n2Pp3/4P2P/2P2pP1/PP1N1P1R/4R1K1 b - - 0 23");

            // Sequence of moves
            board.MakeMove(DenseMove.FromPieceType(PieceType.B_PAWN, BUTIL.C7, BUTIL.C5), searching: false);
            board.MakeMove(DenseMove.FromPieceType(PieceType.W_PAWN, BUTIL.D5, BUTIL.C6, DenseType.D_PAWN, false, true), searching: false);
            board.MakeMove(DenseMove.FromPieceType(PieceType.B_KNIGHT, BUTIL.A5, BUTIL.C6, DenseType.D_PAWN), searching: false);
            board.MakeMove(DenseMove.FromPieceType(PieceType.W_PAWN, BUTIL.B2, BUTIL.B4), searching: false);
            board.MakeMove(DenseMove.FromPieceType(PieceType.B_ROOK, BUTIL.F7, BUTIL.E7), searching: false);

            int moveNum = 0;
            moves = MoveGenerator.GeneratePsuedoMoves(board, ref moveNum);

            Assert.That(!ContainsMove(moves, moveNum, BUTIL.B4, BUTIL.C5));
        }

        [Test]
        public void EnPassantMoves()
        {
            // Case 1: EP available and legal
            board.SetupPositionFromFEN("r1bqkbnr/ppp1pppp/2n5/3pP3/8/8/PPPP1PPP/RNBQKBNR w KQkq d6 0 3");
            int moveNum = 0;
            moves = MoveGenerator.GenerateLegalMoves(board, ref moveNum);
            Assert.That(ContainsMove(moves, moveNum, 36, 43)); // e5xd6 ep (indices as in original)

            // Case 2: reset; EP should not be allowed now
            Array.Clear(moves, 0, moves.Length);
            board.SetupPositionFromFEN("r1bqkbnr/ppp1pppp/2n5/3pP3/8/P7/1PPP1PPP/RNBQKBNR b KQkq - 0 3");
            moves = MoveGenerator.GenerateLegalMoves(board, ref moveNum);
            Assert.That(!ContainsMove(moves, moveNum, 36, 43));
            Assert.That(!ContainsMove(moves, moveNum, 36, 45));

            // Case 3: EP square f6; white can capture e5xf6 ep
            Array.Clear(moves, 0, moves.Length);
            board.SetupPositionFromFEN("r1bqkbnr/ppp1p1pp/2n5/3pPp2/8/P7/1PPP1PPP/RNBQKBNR w KQkq f6 0 4");
            moves = MoveGenerator.GenerateLegalMoves(board, ref moveNum);
            Assert.That(ContainsMove(moves, moveNum, 36, 45));

            // Case 4: black captures g4xg3 ep
            Array.Clear(moves, 0, moves.Length);
            board.SetupPositionFromFEN("r1bqkbnr/ppp1p1pp/2n5/3pP3/5pP1/P1N5/1PPP1P1P/R1BQKBNR b KQkq g3 0 5");
            moves = MoveGenerator.GenerateLegalMoves(board, ref moveNum);
            Assert.That(ContainsMove(moves, moveNum, 29, 22));
        }
    }
}
