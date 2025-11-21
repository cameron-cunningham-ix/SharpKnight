using NUnit.Framework;
using SharpKnight.Core;

namespace SharpKnight.Tests
{
    [TestFixture]
    public class UtilityTests
    {
        private ChessBoard board;

        [SetUp]
        public void SetUp()
        {
            PEXT.Initialize();
            board = new ChessBoard();
            Directory.CreateDirectory("TestOutput");
        }

        // --- FEN to Piece maps ---
        [Test]
        public void FENToPieceMap()
        {
            Assert.That(PieceType.W_PAWN, Is.EqualTo(Utility.FenToPiece['P']));
            Assert.That(PieceType.B_PAWN, Is.EqualTo(Utility.FenToPiece['p']));
            Assert.That(PieceType.W_KNIGHT, Is.EqualTo(Utility.FenToPiece['N']));
            Assert.That(PieceType.B_KNIGHT, Is.EqualTo(Utility.FenToPiece['n']));
            Assert.That(PieceType.W_BISHOP, Is.EqualTo(Utility.FenToPiece['B']));
            Assert.That(PieceType.B_BISHOP, Is.EqualTo(Utility.FenToPiece['b']));
            Assert.That(PieceType.W_ROOK, Is.EqualTo(Utility.FenToPiece['R']));
            Assert.That(PieceType.B_ROOK, Is.EqualTo(Utility.FenToPiece['r']));
            Assert.That(PieceType.W_QUEEN, Is.EqualTo(Utility.FenToPiece['Q']));
            Assert.That(PieceType.B_QUEEN, Is.EqualTo(Utility.FenToPiece['q']));
            Assert.That(PieceType.W_KING, Is.EqualTo(Utility.FenToPiece['K']));
            Assert.That(PieceType.B_KING, Is.EqualTo(Utility.FenToPiece['k']));
        }

        [Test]
        public void PieceToFENMap()
        {
            Assert.That("P", Is.EqualTo(Utility.PieceToFEN[PieceType.W_PAWN]));
            Assert.That("p", Is.EqualTo(Utility.PieceToFEN[PieceType.B_PAWN]));
            Assert.That("N", Is.EqualTo(Utility.PieceToFEN[PieceType.W_KNIGHT]));
            Assert.That("n", Is.EqualTo(Utility.PieceToFEN[PieceType.B_KNIGHT]));
            Assert.That("B", Is.EqualTo(Utility.PieceToFEN[PieceType.W_BISHOP]));
            Assert.That("b", Is.EqualTo(Utility.PieceToFEN[PieceType.B_BISHOP]));
            Assert.That("R", Is.EqualTo(Utility.PieceToFEN[PieceType.W_ROOK]));
            Assert.That("r", Is.EqualTo(Utility.PieceToFEN[PieceType.B_ROOK]));
            Assert.That("Q", Is.EqualTo(Utility.PieceToFEN[PieceType.W_QUEEN]));
            Assert.That("q", Is.EqualTo(Utility.PieceToFEN[PieceType.B_QUEEN]));
            Assert.That("K", Is.EqualTo(Utility.PieceToFEN[PieceType.W_KING]));
            Assert.That("k", Is.EqualTo(Utility.PieceToFEN[PieceType.B_KING]));
        }

        // --- Piece/Color codes ---
        [Test]
        public void PieceCodes()
        {
            Assert.That(Utility.GetDenseType(PieceType.W_PAWN), Is.EqualTo((int)DenseType.D_PAWN));
            Assert.That(Utility.GetDenseType(PieceType.B_PAWN), Is.EqualTo((int)DenseType.D_PAWN));
            Assert.That(Utility.GetDenseType(PieceType.W_KNIGHT), Is.EqualTo((int)DenseType.D_KNIGHT));
            Assert.That(Utility.GetDenseType(PieceType.B_KNIGHT), Is.EqualTo((int)DenseType.D_KNIGHT));
            Assert.That(Utility.GetDenseType(PieceType.W_BISHOP), Is.EqualTo((int)DenseType.D_BISHOP));
            Assert.That(Utility.GetDenseType(PieceType.B_BISHOP), Is.EqualTo((int)DenseType.D_BISHOP));
            Assert.That(Utility.GetDenseType(PieceType.W_ROOK), Is.EqualTo((int)DenseType.D_ROOK));
            Assert.That(Utility.GetDenseType(PieceType.B_ROOK), Is.EqualTo((int)DenseType.D_ROOK));
            Assert.That(Utility.GetDenseType(PieceType.W_QUEEN), Is.EqualTo((int)DenseType.D_QUEEN));
            Assert.That(Utility.GetDenseType(PieceType.B_QUEEN), Is.EqualTo((int)DenseType.D_QUEEN));
            Assert.That(Utility.GetDenseType(PieceType.W_KING), Is.EqualTo((int)DenseType.D_KING));
            Assert.That(Utility.GetDenseType(PieceType.B_KING), Is.EqualTo((int)DenseType.D_KING));
        }

        [Test]
        public void ColorCodes()
        {
            Assert.That(Utility.GetColor(PieceType.W_PAWN), Is.EqualTo((int)Color.WHITE));
            Assert.That(Utility.GetColor(PieceType.B_PAWN), Is.EqualTo((int)Color.BLACK));
            Assert.That(Utility.GetColor(PieceType.W_KNIGHT), Is.EqualTo((int)Color.WHITE));
            Assert.That(Utility.GetColor(PieceType.B_KNIGHT), Is.EqualTo((int)Color.BLACK));
            Assert.That(Utility.GetColor(PieceType.W_BISHOP), Is.EqualTo((int)Color.WHITE));
            Assert.That(Utility.GetColor(PieceType.B_BISHOP), Is.EqualTo((int)Color.BLACK));
            Assert.That(Utility.GetColor(PieceType.W_ROOK), Is.EqualTo((int)Color.WHITE));
            Assert.That(Utility.GetColor(PieceType.B_ROOK), Is.EqualTo((int)Color.BLACK));
            Assert.That(Utility.GetColor(PieceType.W_QUEEN), Is.EqualTo((int)Color.WHITE));
            Assert.That(Utility.GetColor(PieceType.B_QUEEN), Is.EqualTo((int)Color.BLACK));
            Assert.That(Utility.GetColor(PieceType.W_KING), Is.EqualTo((int)Color.WHITE));
            Assert.That(Utility.GetColor(PieceType.B_KING), Is.EqualTo((int)Color.BLACK));
        }

        // --- Algebraic to Index ---
        [Test]
        public void AlgebraicNotationConversion()
        {
            // Valid
            Assert.That(Utility.AlgebraicToIndex("a1"), Is.EqualTo(0));
            Assert.That(Utility.AlgebraicToIndex("h1"), Is.EqualTo(7));
            Assert.That(Utility.AlgebraicToIndex("a8"), Is.EqualTo(56));
            Assert.That(Utility.AlgebraicToIndex("h8"), Is.EqualTo(63));
            Assert.That(Utility.AlgebraicToIndex("e4"), Is.EqualTo(28));

            // Invalid
            Assert.That(Utility.AlgebraicToIndex("i1"), Is.EqualTo(-1));
            Assert.That(Utility.AlgebraicToIndex("a9"), Is.EqualTo(-1));
            Assert.That(Utility.AlgebraicToIndex(""), Is.EqualTo(-1));
            Assert.That(Utility.AlgebraicToIndex("a"), Is.EqualTo(-1));

            // Reverse
            Assert.That(Utility.IndexToAlgebraic(0), Is.EqualTo("a1"));
            Assert.That(Utility.IndexToAlgebraic(7), Is.EqualTo("h1"));
            Assert.That(Utility.IndexToAlgebraic(56), Is.EqualTo("a8"));
            Assert.That(Utility.IndexToAlgebraic(63), Is.EqualTo("h8"));
            Assert.That(Utility.IndexToAlgebraic(28), Is.EqualTo("e4"));

            // Invalid indices
            Assert.That(Utility.IndexToAlgebraic(-1), Is.EqualTo("??"));
            Assert.That(Utility.IndexToAlgebraic(64), Is.EqualTo("??"));
        }

        // --- FEN operations / printing ---
        [Test]
        public void FENOperations()
        {
            var initialFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            board.SetupPositionFromFEN(initialFEN);

            Assert.That(board.GetWhitePawns(), Is.EqualTo(0x000000000000FF00UL));
            Assert.That(board.GetBlackPawns(), Is.EqualTo(0x00FF000000000000UL));
            Assert.That(board.GetWhiteKings(), Is.EqualTo(0x0000000000000010UL));
            Assert.That(board.GetBlackKings(), Is.EqualTo(0x1000000000000000UL));

            Assert.That(board.CurrentGameState.SideToMove, Is.EqualTo(Color.WHITE));
            Assert.That(board.CurrentGameState.CanCastleWhiteKingside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleWhiteQueenside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleBlackKingside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleBlackQueenside, Is.True);
            Assert.That(board.CurrentGameState.EnPassantSquare, Is.EqualTo(-1));

            var complexFEN = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";
            board.SetupPositionFromFEN(complexFEN);

            //var sw = new StringWriter();
            //var oldOut = Console.Out;
            //try
            //{
            //    Console.SetOut(sw);
            //    board.PrintFEN();
            //}
            //finally
            //{
            //    Console.SetOut(oldOut);
            //}
            //var output = sw.ToString();
            //StringAssert.Contains(complexFEN, output);
        }

        // --- Legal move counting ---
        [Test]
        public void LegalMovesCount()
        {
            board.SetupPositionFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w - - 0 1");
            Assert.That(Utility.CountLegalMoves(board), Is.EqualTo(20));

            board.SetupPositionFromFEN("r1bqkbnr/pppp1Qpp/8/n3p3/2B1P3/8/PPPP1PPP/RNB1K1NR b KQkq - 0 1");
            Assert.That(Utility.CountLegalMoves(board), Is.EqualTo(0));
        }

        // --- Checkmate/Stalemate detection ---
        [Test]
        public void GameEndDetection()
        {
            board.SetupPositionFromFEN("rnb1kbnr/pppp1ppp/8/4p3/6Pq/5P2/PPPPP2P/RNBQKBNR w KQkq - 0 1");
            Assert.That(Utility.IsCheckmate(board), Is.True);
            Assert.That(Utility.IsStalemate(board), Is.Not.True);

            board.SetupPositionFromFEN("k7/8/1Q6/8/8/8/8/K7 b - - 0 1");
            Assert.That(Utility.IsCheckmate(board), Is.Not.True);
            Assert.That(Utility.IsStalemate(board), Is.True);

            board.SetupPositionFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w - - 0 1");
            Assert.That(Utility.IsCheckmate(board), Is.Not.True);
            Assert.That(Utility.IsStalemate(board), Is.Not.True);
        }

        // --- Named test positions ---
        [Test]
        public void TestPositionSetup()
        {
            Utility.SetupTestPosition(board, "initial");
            //board.PrintBitboards();
            Utility.PrintBitboard(board.GetWhitePawns());
            Assert.That(board.GetWhitePawns(), Is.EqualTo(0x000000000000FF00UL));

            Utility.SetupTestPosition(board, "kiwipete");
            Assert.That(board.GetWhitePawns(), Is.Not.EqualTo(0x000000000000FF00UL));

            // Should handle unknown name gracefully
            Assert.DoesNotThrow(() => Utility.SetupTestPosition(board, "nonexistent"));
        }

        // --- Attack pattern verification ---
        [Test]
        public void AttackPatternVerification()
        {
            board.SetupPositionFromFEN("8/8/8/8/4N3/8/8/8 w - - 0 1");
            var expectedKnight = new List<string> { "f6", "g5", "g3", "f2", "d2", "c3", "c5", "d6" };
            Assert.That(Utility.VerifyAttackPattern(board, 28, expectedKnight), Is.True);

            board.SetupPositionFromFEN("8/8/8/8/4B3/8/8/8 w - - 0 1");
            var expectedBishop = new List<string>
            {
                "h7","g6","f5","d3","c2","b1",
                "a8","b7","c6","d5","f3","g2","h1"
            };
            Assert.That(Utility.VerifyAttackPattern(board, 28, expectedBishop), Is.True);

            board.SetupPositionFromFEN("8/8/8/8/4Q3/8/8/8 w - - 0 1");
            var expectedQueen = new List<string>
            {
                "h7","g6","f5","d3","c2","b1",
                "a8","b7","c6","d5","f3","g2","h1",
                "e1","e2","e3","e5","e6","e7","e8",
                "a4","b4","c4","d4","f4","g4","h4"
            };
            Assert.That(Utility.VerifyAttackPattern(board, 28, expectedQueen), Is.True);
        }

        // --- Perft: Initial position ---
        [Test]
        public void PerftD1CalcInitial()
        {
            board.SetupPositionFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w - - 0 1");
            Assert.That(Utility.Perft(board, 1, 1), Is.EqualTo(20UL));
        }

        [Test]
        public void PerftD2CalcInitial()
        {
            board.SetupPositionFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w - - 0 1");
            Assert.That(Utility.Perft(board, 2, 2), Is.EqualTo(400UL));
        }

        [Test]
        public void PerftD3CalcInitial()
        {
            board.SetupPositionFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w - - 0 1");
            Assert.That(Utility.Perft(board, 3, 3), Is.EqualTo(8902UL));
        }

        [Test]
        public void PerftD4CalcInitial()
        {
            board.SetupPositionFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w - - 0 1");
            Assert.That(Utility.Perft(board, 4, 4), Is.EqualTo(197281UL));
        }

        [Test]
        public void PerftD5CalcInitial()
        {
            board.SetupPositionFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w - - 0 1");
            Assert.That(Utility.Perft(board, 5, 5), Is.EqualTo(4865609UL));
        }

        [Test]
        public void PerftD6CalcInitial()
        {
            board.SetupPositionFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w - - 0 1");
            Assert.That(Utility.Perft(board, 6, 6), Is.EqualTo(119060324UL));
        }

        // --- Perft: Kiwipete ---
        [Test]
        public void PerftD1CalcKiwiPete()
        {
            board.SetupPositionFromFEN("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1");
            Assert.That(Utility.Perft(board, 1, 1), Is.EqualTo(48UL));
        }

        [Test]
        public void PerftD1CalcKiwiPete_e5g6()
        {
            board.SetupPositionFromFEN("r3k2r/p1ppqpb1/bn2pnN1/3P4/1p2P3/2N2Q1p/PPPBBPPP/R3K2R b KQkq - 0 1");
            Assert.That(Utility.Perft(board, 1, 1, true), Is.EqualTo(42UL));
        }

        [Test]
        public void PerftD2CalcKiwiPete()
        {
            board.SetupPositionFromFEN("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1");
            Assert.That(Utility.Perft(board, 2, 2, true), Is.EqualTo(2039UL));
        }

        [Test]
        public void PerftD3CalcKiwiPete()
        {
            board.SetupPositionFromFEN("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1");
            Assert.That(Utility.Perft(board, 3, 3, true), Is.EqualTo(97862UL));
        }

        [Test]
        public void PerftD4CalcKiwiPete()
        {
            board.SetupPositionFromFEN("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1");
            Assert.That(Utility.Perft(board, 4, 4, true), Is.EqualTo(4085603UL));
        }

        // --- Perft: Position 3 (chessprogramming.org) ---
        [Test]
        public void PerftD1CalcPos3()
        {
            board.SetupPositionFromFEN("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1");
            Assert.That(Utility.Perft(board, 1, 1), Is.EqualTo(14UL));
        }

        [Test]
        public void PerftD3CalcPos3()
        {
            board.SetupPositionFromFEN("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1");
            Assert.That(Utility.Perft(board, 3, 3), Is.EqualTo(2812UL));
        }

        [Test]
        public void PerftD4CalcPos3()
        {
            board.SetupPositionFromFEN("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1");
            Assert.That(Utility.Perft(board, 4, 4), Is.EqualTo(43238UL));
        }

        [Test]
        public void PerftD5CalcPos3()
        {
            board.SetupPositionFromFEN("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1");
            Assert.That(Utility.Perft(board, 5, 5), Is.EqualTo(674624UL));
        }

        [Test]
        public void PerftD6CalcPos3()
        {
            board.SetupPositionFromFEN("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1");
            Assert.That(Utility.Perft(board, 6, 6), Is.EqualTo(11030083UL));
        }

        // --- Perft: Position 4 (+ mirror) ---
        [Test]
        public void PerftD1CalcPos4()
        {
            board.SetupPositionFromFEN("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1");
            Assert.That(Utility.Perft(board, 1, 1), Is.EqualTo(6UL));
        }

        [Test]
        public void PerftD2CalcPos4()
        {
            board.SetupPositionFromFEN("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1");
            Assert.That(Utility.Perft(board, 2, 2), Is.EqualTo(264UL));
        }

        [Test]
        public void PerftD3CalcPos4()
        {
            board.SetupPositionFromFEN("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1");
            Assert.That(Utility.Perft(board, 3, 3), Is.EqualTo(9467UL));
        }

        [Test]
        public void PerftD4CalcPos4()
        {
            board.SetupPositionFromFEN("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1");
            Assert.That(Utility.Perft(board, 4, 4), Is.EqualTo(422333UL));
        }

        [Test]
        public void PerftD1CalcPos4Mirror()
        {
            board.SetupPositionFromFEN("r2q1rk1/pP1p2pp/Q4n2/bbp1p3/Np6/1B3NBn/pPPP1PPP/R3K2R b KQ - 0 1 ");
            Assert.That(Utility.Perft(board, 1, 1), Is.EqualTo(6UL));
        }

        [Test]
        public void PerftD2CalcPos4Mirror()
        {
            board.SetupPositionFromFEN("r2q1rk1/pP1p2pp/Q4n2/bbp1p3/Np6/1B3NBn/pPPP1PPP/R3K2R b KQ - 0 1 ");
            Assert.That(Utility.Perft(board, 2, 2), Is.EqualTo(264UL));
        }

        [Test]
        public void PerftD3CalcPos4Mirror()
        {
            board.SetupPositionFromFEN("r2q1rk1/pP1p2pp/Q4n2/bbp1p3/Np6/1B3NBn/pPPP1PPP/R3K2R b KQ - 0 1 ");
            Assert.That(Utility.Perft(board, 3, 3), Is.EqualTo(9467UL));
        }

        [Test]
        public void PerftD4CalcPos4Mirror()
        {
            board.SetupPositionFromFEN("r2q1rk1/pP1p2pp/Q4n2/bbp1p3/Np6/1B3NBn/pPPP1PPP/R3K2R b KQ - 0 1 ");
            Assert.That(Utility.Perft(board, 4, 4), Is.EqualTo(422333UL));
        }

        [Test]
        public void PerftD5CalcPos4Mirror()
        {
            board.SetupPositionFromFEN("r2q1rk1/pP1p2pp/Q4n2/bbp1p3/Np6/1B3NBn/pPPP1PPP/R3K2R b KQ - 0 1 ");
            Assert.That(Utility.Perft(board, 5, 5), Is.EqualTo(15833292UL));
        }

        // --- Perft: Position 5 ---
        [Test]
        public void PerftD1CalcPos5()
        {
            board.SetupPositionFromFEN("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8");
            Assert.That(Utility.Perft(board, 1, 1), Is.EqualTo(44UL));
        }

        [Test]
        public void PerftD2CalcPos5()
        {
            board.SetupPositionFromFEN("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8");
            Assert.That(Utility.Perft(board, 2, 2), Is.EqualTo(1486UL));
        }

        [Test]
        public void PerftD3CalcPos5()
        {
            board.SetupPositionFromFEN("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8");
            Assert.That(Utility.Perft(board, 3, 3, true), Is.EqualTo(62379UL));
        }

        [Test]
        public void PerftD2CalcPos5_d7c8n()
        {
            board.SetupPositionFromFEN("rnNq1k1r/pp2bppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R b KQ - 0 8");
            Assert.That(Utility.Perft(board, 2, 2, true), Is.EqualTo(1607UL));
        }

        [Test]
        public void PerftD4CalcPos5()
        {
            board.SetupPositionFromFEN("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8");
            Assert.That(Utility.Perft(board, 4, 4), Is.EqualTo(2103487UL));
        }

        [Test]
        public void PerftD5CalcPos5()
        {
            board.SetupPositionFromFEN("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8");
            Assert.That(Utility.Perft(board, 5, 5), Is.EqualTo(89941194UL));
        }

        // --- Perft: Position 6 ---
        [Test]
        public void PerftD1CalcPos6()
        {
            board.SetupPositionFromFEN("r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10");
            Assert.That(Utility.Perft(board, 1, 1), Is.EqualTo(46UL));
        }

        [Test]
        public void PerftD2CalcPos6()
        {
            board.SetupPositionFromFEN("r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10");
            Assert.That(Utility.Perft(board, 2, 2), Is.EqualTo(2079UL));
        }

        [Test]
        public void PerftD3CalcPos6()
        {
            board.SetupPositionFromFEN("r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10");
            Assert.That(Utility.Perft(board, 3, 3), Is.EqualTo(89890UL));
        }

        [Test]
        public void PerftD4CalcPos6()
        {
            board.SetupPositionFromFEN("r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10");
            Assert.That(Utility.Perft(board, 4, 4), Is.EqualTo(3894594UL));
        }

        [Test]
        public void PerftD5CalcPos6()
        {
            board.SetupPositionFromFEN("r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10");
            Assert.That(Utility.Perft(board, 5, 5), Is.EqualTo(164075551UL));
        }

        // --- Perft: Standard (Ethereal epd) Pos 3-6 ---
        [Test]
        public void PerftD1CalcStandardPos3()
        {
            board.SetupPositionFromFEN("4k3/8/8/8/8/8/8/4K2R w K - 0 1");
            Assert.That(Utility.Perft(board, 1, 1), Is.EqualTo(15UL));
        }

        [Test]
        public void PerftD2CalcStandardPos3()
        {
            board.SetupPositionFromFEN("4k3/8/8/8/8/8/8/4K2R w K - 0 1");
            Assert.That(Utility.Perft(board, 2, 2), Is.EqualTo(66UL));
        }

        [Test]
        public void PerftD3CalcStandardPos3()
        {
            board.SetupPositionFromFEN("4k3/8/8/8/8/8/8/4K2R w K - 0 1");
            Assert.That(Utility.Perft(board, 3, 3), Is.EqualTo(1197UL));
        }

        [Test]
        public void PerftD4CalcStandardPos3()
        {
            board.SetupPositionFromFEN("4k3/8/8/8/8/8/8/4K2R w K - 0 1");
            Assert.That(Utility.Perft(board, 4, 4), Is.EqualTo(7059UL));
        }

        [Test]
        public void PerftD5CalcStandardPos3()
        {
            board.SetupPositionFromFEN("4k3/8/8/8/8/8/8/4K2R w K - 0 1");
            Assert.That(Utility.Perft(board, 5, 5), Is.EqualTo(133987UL));
        }

        [Test]
        public void PerftD1CalcStandardPos4()
        {
            board.SetupPositionFromFEN("4k3/8/8/8/8/8/8/R3K3 w Q - 0 1");
            Assert.That(Utility.Perft(board, 1, 1), Is.EqualTo(16UL));
        }

        [Test]
        public void PerftD2CalcStandardPos4()
        {
            board.SetupPositionFromFEN("4k3/8/8/8/8/8/8/R3K3 w Q - 0 1");
            Assert.That(Utility.Perft(board, 2, 2), Is.EqualTo(71UL));
        }

        [Test]
        public void PerftD3CalcStandardPos4()
        {
            board.SetupPositionFromFEN("4k3/8/8/8/8/8/8/R3K3 w Q - 0 1");
            Assert.That(Utility.Perft(board, 3, 3), Is.EqualTo(1287UL));
        }

        [Test]
        public void PerftD4CalcStandardPos4()
        {
            board.SetupPositionFromFEN("4k3/8/8/8/8/8/8/R3K3 w Q - 0 1");
            Assert.That(Utility.Perft(board, 4, 4), Is.EqualTo(7626UL));
        }

        [Test]
        public void PerftD5CalcStandardPos4()
        {
            board.SetupPositionFromFEN("4k3/8/8/8/8/8/8/R3K3 w Q - 0 1");
            Assert.That(Utility.Perft(board, 5, 5), Is.EqualTo(145232UL));
        }

        [Test]
        public void PerftD1CalcStandardPos5()
        {
            board.SetupPositionFromFEN("4k2r/8/8/8/8/8/8/4K3 w k - 0 1");
            Assert.That(Utility.Perft(board, 1, 1), Is.EqualTo(5UL));
        }

        [Test]
        public void PerftD2CalcStandardPos5()
        {
            board.SetupPositionFromFEN("4k2r/8/8/8/8/8/8/4K3 w k - 0 1");
            Assert.That(Utility.Perft(board, 2, 2), Is.EqualTo(75UL));
        }

        [Test]
        public void PerftD3CalcStandardPos5()
        {
            board.SetupPositionFromFEN("4k2r/8/8/8/8/8/8/4K3 w k - 0 1");
            Assert.That(Utility.Perft(board, 3, 3), Is.EqualTo(459UL));
        }

        [Test]
        public void PerftD4CalcStandardPos5()
        {
            board.SetupPositionFromFEN("4k2r/8/8/8/8/8/8/4K3 w k - 0 1");
            Assert.That(Utility.Perft(board, 4, 4, true), Is.EqualTo(8290UL));
        }

        [Test]
        public void PerftD3CalcStandardPos5_e1d1()
        {
            board.SetupPositionFromFEN("4k2r/8/8/8/8/8/8/3K4 b k - 1 1");
            Assert.That(Utility.Perft(board, 3, 3, true), Is.EqualTo(1255UL));
        }

        [Test]
        public void PerftD2CalcStandardPos5_h8h1()
        {
            board.SetupPositionFromFEN("4k3/8/8/8/8/8/8/3K3r w - - 2 2");
            Assert.That(Utility.Perft(board, 2, 2, true), Is.EqualTo(57UL));
        }

        [Test]
        public void PerftD5CalcStandardPos5()
        {
            board.SetupPositionFromFEN("4k2r/8/8/8/8/8/8/4K3 w k - 0 1");
            Assert.That(Utility.Perft(board, 5, 5), Is.EqualTo(47635UL));
        }

        [Test]
        public void PerftD1CalcStandardPos6()
        {
            board.SetupPositionFromFEN("r3k3/8/8/8/8/8/8/4K3 w q - 0 1");
            Assert.That(Utility.Perft(board, 1, 1), Is.EqualTo(5UL));
        }

        [Test]
        public void PerftD2CalcStandardPos6()
        {
            board.SetupPositionFromFEN("r3k3/8/8/8/8/8/8/4K3 w q - 0 1");
            Assert.That(Utility.Perft(board, 2, 2), Is.EqualTo(80UL));
        }

        [Test]
        public void PerftD3CalcStandardPos6()
        {
            board.SetupPositionFromFEN("r3k3/8/8/8/8/8/8/4K3 w q - 0 1");
            Assert.That(Utility.Perft(board, 3, 3), Is.EqualTo(493UL));
        }

        [Test]
        public void PerftD4CalcStandardPos6()
        {
            board.SetupPositionFromFEN("r3k3/8/8/8/8/8/8/4K3 w q - 0 1");
            Assert.That(Utility.Perft(board, 4, 4), Is.EqualTo(8897UL));
        }

        [Test]
        public void PerftD5CalcStandardPos6()
        {
            board.SetupPositionFromFEN("r3k3/8/8/8/8/8/8/4K3 w q - 0 1");
            Assert.That(Utility.Perft(board, 5, 5), Is.EqualTo(52710UL));
        }

    }
}
