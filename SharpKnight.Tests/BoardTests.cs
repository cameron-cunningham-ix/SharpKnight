using SharpKnight.Core;
using NUnit.Framework;

namespace SharpKnight.Tests
{
    [TestFixture]
    public class BoardTests
    {
        protected ChessBoard board;

        [SetUp]
        public void Setup()
        {
            // Initialize PEXT and board
            PEXT.Initialize();

            board = new ChessBoard();
        }

        [Test]
        public void InitialBoardSetup()
        {
            // Test initial positions of all pieces
            Assert.That(board, Is.Not.Null);
            Assert.That(board.GetAllPieces(), Is.EqualTo(0xFFFF00000000FFFF));
            Assert.That(board.GetEmptySquares(), Is.EqualTo(0x0000FFFFFFFF0000));
            // White pieces
            Assert.That(board.GetWhitePieces(), Is.EqualTo(0x000000000000FFFF));
            Assert.That(board.GetWhitePawns(), Is.EqualTo(0x000000000000FF00));
            Assert.That(board.GetWhiteKnights(), Is.EqualTo(0x0000000000000042));
            Assert.That(board.GetWhiteBishops(), Is.EqualTo(0x0000000000000024));
            Assert.That(board.GetWhiteRooks(), Is.EqualTo(0x0000000000000081));
            Assert.That(board.GetWhiteQueens(), Is.EqualTo(0x0000000000000008));
            Assert.That(board.GetWhiteKings(), Is.EqualTo(0x0000000000000010));
            // Black pieces
            Assert.That(board.GetBlackPieces(), Is.EqualTo(0xFFFF000000000000));
            Assert.That(board.GetBlackPawns(), Is.EqualTo(0x00FF000000000000));
            Assert.That(board.GetBlackKnights(), Is.EqualTo(0x4200000000000000));
            Assert.That(board.GetBlackBishops(), Is.EqualTo(0x2400000000000000));
            Assert.That(board.GetBlackRooks(), Is.EqualTo(0x8100000000000000));
            Assert.That(board.GetBlackQueens(), Is.EqualTo(0x0800000000000000));
            Assert.That(board.GetBlackKings(), Is.EqualTo(0x1000000000000000));
        }

        [Test]
        public void GetPieceSet()
        {
            Assert.That(board.GetPieceSet(PieceType.EMPTY), Is.EqualTo(0x0000FFFFFFFF0000));
            Assert.That(board.GetPieceSet(PieceType.W_PAWN), Is.EqualTo(0x000000000000FF00));
            Assert.That(board.GetPieceSet(PieceType.W_KNIGHT), Is.EqualTo(0x0000000000000042));
            Assert.That(board.GetPieceSet(PieceType.W_BISHOP), Is.EqualTo(0x0000000000000024));
            Assert.That(board.GetPieceSet(PieceType.W_ROOK), Is.EqualTo(0x0000000000000081));
            Assert.That(board.GetPieceSet(PieceType.W_QUEEN), Is.EqualTo(0x0000000000000008));
            Assert.That(board.GetPieceSet(PieceType.W_KING), Is.EqualTo(0x0000000000000010));
            Assert.That(board.GetPieceSet(PieceType.B_PAWN), Is.EqualTo(0x00FF000000000000));
            Assert.That(board.GetPieceSet(PieceType.B_KNIGHT), Is.EqualTo(0x4200000000000000));
            Assert.That(board.GetPieceSet(PieceType.B_BISHOP), Is.EqualTo(0x2400000000000000));
            Assert.That(board.GetPieceSet(PieceType.B_ROOK), Is.EqualTo(0x8100000000000000));
            Assert.That(board.GetPieceSet(PieceType.B_QUEEN), Is.EqualTo(0x0800000000000000));
            Assert.That(board.GetPieceSet(PieceType.B_KING), Is.EqualTo(0x1000000000000000));
        }

        [Test]
        public void InitialGetPieceAt()
        {
            Assert.That(board.GetPieceAt(0), Is.EqualTo(PieceType.W_ROOK));
            Assert.That(board.GetPieceAt(1), Is.EqualTo(PieceType.W_KNIGHT));
            Assert.That(board.GetPieceAt(2), Is.EqualTo(PieceType.W_BISHOP));
            Assert.That(board.GetPieceAt(3), Is.EqualTo(PieceType.W_QUEEN));
            Assert.That(board.GetPieceAt(4), Is.EqualTo(PieceType.W_KING));
            Assert.That(board.GetPieceAt(5), Is.EqualTo(PieceType.W_BISHOP));
            Assert.That(board.GetPieceAt(6), Is.EqualTo(PieceType.W_KNIGHT));
            Assert.That(board.GetPieceAt(7), Is.EqualTo(PieceType.W_ROOK));
            Assert.That(board.GetPieceAt(8), Is.EqualTo(PieceType.W_PAWN));
            Assert.That(board.GetPieceAt(9), Is.EqualTo(PieceType.W_PAWN));
            Assert.That(board.GetPieceAt(10), Is.EqualTo(PieceType.W_PAWN));
            Assert.That(board.GetPieceAt(11), Is.EqualTo(PieceType.W_PAWN));
            Assert.That(board.GetPieceAt(12), Is.EqualTo(PieceType.W_PAWN));
            Assert.That(board.GetPieceAt(13), Is.EqualTo(PieceType.W_PAWN));
            Assert.That(board.GetPieceAt(14), Is.EqualTo(PieceType.W_PAWN));
            Assert.That(board.GetPieceAt(15), Is.EqualTo(PieceType.W_PAWN));

            Assert.That(board.GetPieceAt(32), Is.EqualTo(PieceType.EMPTY));

            Assert.That(board.GetPieceAt(56), Is.EqualTo(PieceType.B_ROOK));
            Assert.That(board.GetPieceAt(57), Is.EqualTo(PieceType.B_KNIGHT));
            Assert.That(board.GetPieceAt(58), Is.EqualTo(PieceType.B_BISHOP));
            Assert.That(board.GetPieceAt(59), Is.EqualTo(PieceType.B_QUEEN));
            Assert.That(board.GetPieceAt(60), Is.EqualTo(PieceType.B_KING));
            Assert.That(board.GetPieceAt(61), Is.EqualTo(PieceType.B_BISHOP));
            Assert.That(board.GetPieceAt(62), Is.EqualTo(PieceType.B_KNIGHT));
            Assert.That(board.GetPieceAt(63), Is.EqualTo(PieceType.B_ROOK));
            Assert.That(board.GetPieceAt(48), Is.EqualTo(PieceType.B_PAWN));
            Assert.That(board.GetPieceAt(49), Is.EqualTo(PieceType.B_PAWN));
            Assert.That(board.GetPieceAt(50), Is.EqualTo(PieceType.B_PAWN));
            Assert.That(board.GetPieceAt(51), Is.EqualTo(PieceType.B_PAWN));
            Assert.That(board.GetPieceAt(52), Is.EqualTo(PieceType.B_PAWN));
            Assert.That(board.GetPieceAt(53), Is.EqualTo(PieceType.B_PAWN));
            Assert.That(board.GetPieceAt(54), Is.EqualTo(PieceType.B_PAWN));
            Assert.That(board.GetPieceAt(55), Is.EqualTo(PieceType.B_PAWN));
        }

        [Test]
        public void GetPieceAt_1()
        {
            board.SetupPositionFromFEN("1nb2r2/rppkqp1p/p2p1npb/4p2Q/1PB1P3/P1N5/1BPPNPPP/2KR1R2 w - - 6 11");
            Assert.That(board.GetPieceAt(2), Is.EqualTo(PieceType.W_KING));
            Assert.That(board.GetPieceAt(3), Is.EqualTo(PieceType.W_ROOK));
            Assert.That(board.GetPieceAt(5), Is.EqualTo(PieceType.W_ROOK));
            Assert.That(board.GetPieceAt(9), Is.EqualTo(PieceType.W_BISHOP));
            Assert.That(board.GetPieceAt(10), Is.EqualTo(PieceType.W_PAWN));
            Assert.That(board.GetPieceAt(11), Is.EqualTo(PieceType.W_PAWN));
            Assert.That(board.GetPieceAt(12), Is.EqualTo(PieceType.W_KNIGHT));
            Assert.That(board.GetPieceAt(13), Is.EqualTo(PieceType.W_PAWN));
            Assert.That(board.GetPieceAt(14), Is.EqualTo(PieceType.W_PAWN));
            Assert.That(board.GetPieceAt(15), Is.EqualTo(PieceType.W_PAWN));
            Assert.That(board.GetPieceAt(16), Is.EqualTo(PieceType.W_PAWN));
            Assert.That(board.GetPieceAt(18), Is.EqualTo(PieceType.W_KNIGHT));
            Assert.That(board.GetPieceAt(25), Is.EqualTo(PieceType.W_PAWN));
            Assert.That(board.GetPieceAt(26), Is.EqualTo(PieceType.W_BISHOP));
            Assert.That(board.GetPieceAt(28), Is.EqualTo(PieceType.W_PAWN));
            Assert.That(board.GetPieceAt(39), Is.EqualTo(PieceType.W_QUEEN));

            Assert.That(board.GetPieceAt(36), Is.EqualTo(PieceType.B_PAWN));
            Assert.That(board.GetPieceAt(40), Is.EqualTo(PieceType.B_PAWN));
            Assert.That(board.GetPieceAt(43), Is.EqualTo(PieceType.B_PAWN));
            Assert.That(board.GetPieceAt(45), Is.EqualTo(PieceType.B_KNIGHT));
            Assert.That(board.GetPieceAt(46), Is.EqualTo(PieceType.B_PAWN));
            Assert.That(board.GetPieceAt(47), Is.EqualTo(PieceType.B_BISHOP));
            Assert.That(board.GetPieceAt(48), Is.EqualTo(PieceType.B_ROOK));
            Assert.That(board.GetPieceAt(49), Is.EqualTo(PieceType.B_PAWN));
            Assert.That(board.GetPieceAt(50), Is.EqualTo(PieceType.B_PAWN));
            Assert.That(board.GetPieceAt(51), Is.EqualTo(PieceType.B_KING));
            Assert.That(board.GetPieceAt(52), Is.EqualTo(PieceType.B_QUEEN));
            Assert.That(board.GetPieceAt(53), Is.EqualTo(PieceType.B_PAWN));
            Assert.That(board.GetPieceAt(55), Is.EqualTo(PieceType.B_PAWN));
            Assert.That(board.GetPieceAt(57), Is.EqualTo(PieceType.B_KNIGHT));
            Assert.That(board.GetPieceAt(58), Is.EqualTo(PieceType.B_BISHOP));
            Assert.That(board.GetPieceAt(61), Is.EqualTo(PieceType.B_ROOK));

        }

        [Test]
        public void GetDenseTypeAt()
        {
            // Initial
            Assert.That(board.GetDenseTypeAt(0), Is.EqualTo(DenseType.D_ROOK));
            Assert.That(board.GetDenseTypeAt(1), Is.EqualTo(DenseType.D_KNIGHT));
            Assert.That(board.GetDenseTypeAt(2), Is.EqualTo(DenseType.D_BISHOP));
            Assert.That(board.GetDenseTypeAt(3), Is.EqualTo(DenseType.D_QUEEN));
            Assert.That(board.GetDenseTypeAt(4), Is.EqualTo(DenseType.D_KING));
            Assert.That(board.GetDenseTypeAt(5), Is.EqualTo(DenseType.D_BISHOP));
            Assert.That(board.GetDenseTypeAt(6), Is.EqualTo(DenseType.D_KNIGHT));
            Assert.That(board.GetDenseTypeAt(7), Is.EqualTo(DenseType.D_ROOK));
            Assert.That(board.GetDenseTypeAt(8), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(9), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(10), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(11), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(12), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(13), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(14), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(15), Is.EqualTo(DenseType.D_PAWN));

            Assert.That(board.GetDenseTypeAt(56), Is.EqualTo(DenseType.D_ROOK));
            Assert.That(board.GetDenseTypeAt(57), Is.EqualTo(DenseType.D_KNIGHT));
            Assert.That(board.GetDenseTypeAt(58), Is.EqualTo(DenseType.D_BISHOP));
            Assert.That(board.GetDenseTypeAt(59), Is.EqualTo(DenseType.D_QUEEN));
            Assert.That(board.GetDenseTypeAt(60), Is.EqualTo(DenseType.D_KING));
            Assert.That(board.GetDenseTypeAt(61), Is.EqualTo(DenseType.D_BISHOP));
            Assert.That(board.GetDenseTypeAt(62), Is.EqualTo(DenseType.D_KNIGHT));
            Assert.That(board.GetDenseTypeAt(63), Is.EqualTo(DenseType.D_ROOK));
            Assert.That(board.GetDenseTypeAt(48), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(49), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(50), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(51), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(52), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(53), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(54), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(55), Is.EqualTo(DenseType.D_PAWN));

            // Random
            board.SetupPositionFromFEN("1nb2r2/rppkqp1p/p2p1npb/4p2Q/1PB1P3/P1N5/1BPPNPPP/2KR1R2 w - - 6 11");
            Assert.That(board.GetDenseTypeAt(2), Is.EqualTo(DenseType.D_KING));
            Assert.That(board.GetDenseTypeAt(3), Is.EqualTo(DenseType.D_ROOK));
            Assert.That(board.GetDenseTypeAt(5), Is.EqualTo(DenseType.D_ROOK));
            Assert.That(board.GetDenseTypeAt(9), Is.EqualTo(DenseType.D_BISHOP));
            Assert.That(board.GetDenseTypeAt(10), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(11), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(12), Is.EqualTo(DenseType.D_KNIGHT));
            Assert.That(board.GetDenseTypeAt(13), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(14), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(15), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(16), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(18), Is.EqualTo(DenseType.D_KNIGHT));
            Assert.That(board.GetDenseTypeAt(25), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(26), Is.EqualTo(DenseType.D_BISHOP));
            Assert.That(board.GetDenseTypeAt(28), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(39), Is.EqualTo(DenseType.D_QUEEN));

            Assert.That(board.GetDenseTypeAt(36), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(40), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(43), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(45), Is.EqualTo(DenseType.D_KNIGHT));
            Assert.That(board.GetDenseTypeAt(46), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(47), Is.EqualTo(DenseType.D_BISHOP));
            Assert.That(board.GetDenseTypeAt(48), Is.EqualTo(DenseType.D_ROOK));
            Assert.That(board.GetDenseTypeAt(49), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(50), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(53), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(55), Is.EqualTo(DenseType.D_PAWN));
            Assert.That(board.GetDenseTypeAt(51), Is.EqualTo(DenseType.D_KING));
            Assert.That(board.GetDenseTypeAt(52), Is.EqualTo(DenseType.D_QUEEN));
            Assert.That(board.GetDenseTypeAt(57), Is.EqualTo(DenseType.D_KNIGHT));
            Assert.That(board.GetDenseTypeAt(58), Is.EqualTo(DenseType.D_BISHOP));
            Assert.That(board.GetDenseTypeAt(61), Is.EqualTo(DenseType.D_ROOK));
        }

        [Test]
        public void GetKingSquare1()
        {
            Assert.That(board.GetWhiteKingSquare(), Is.EqualTo(4));
            Assert.That(board.GetBlackKingSquare(), Is.EqualTo(60));

            board.SetupPositionFromFEN("rnbq1bnr/pppp1ppp/5k2/4p3/4P3/3K4/PPPP1PPP/RNBQ1BNR w - - 4 4");
            Assert.That(board.GetWhiteKingSquare(), Is.EqualTo(BUTIL.D3));
            Assert.That(board.GetBlackKingSquare(), Is.EqualTo(BUTIL.F6));

            board.MakeMove(DenseMove.FromPieceType(PieceType.W_KING, BUTIL.D3, BUTIL.C4), false);
            Assert.That(board.GetWhiteKingSquare(), Is.EqualTo(BUTIL.C4));

            board.MakeMove(DenseMove.FromPieceType(PieceType.B_KING, BUTIL.F6, BUTIL.G6), false);
            board.UnmakeMove(DenseMove.FromPieceType(PieceType.B_KING, BUTIL.F6, BUTIL.G6), false);
            Assert.That(board.GetBlackKingSquare(), Is.EqualTo(BUTIL.F6));

            board.UnmakeMove(DenseMove.FromPieceType(PieceType.W_KING, BUTIL.D3, BUTIL.C4), false);
            Assert.That(board.GetWhiteKingSquare(), Is.EqualTo(BUTIL.D3));
        }

        [Test]
        public void AttacksToSquare1()
        {
            Assert.That(board.OppAttacksToSquare(4, Color.WHITE), Is.EqualTo(0x0UL));

            board.SetupPositionFromFEN("8/2p5/3p4/KP5r/1R3p1k/4P3/6P1/8 w - - 0 1");
            Assert.That(
                board.OppAttacksToSquare(33, Color.WHITE),
                Is.EqualTo(0b0000000000000000000000001000000000000000000000000000000000000000UL)
            );
        }

        [Test]
        public void AttacksToSquare2()
        {
            board.SetupPositionFromFEN("1r6/2p5/3n4/KP5r/1R3p1k/4P3/6P1/8 w - - 0 1");
            Utility.PrintBitboard(board.OppAttacksToSquare(33, Color.WHITE));

            Assert.That(
                board.OppAttacksToSquare(33, Color.WHITE),
                Is.EqualTo(0b0000001000000000000010001000000000000000000000000000000000000000UL)
            );

        }

        [Test]
        public void GameState1()
        {
            Assert.That(board.CurrentGameState.SideToMove, Is.EqualTo(Color.WHITE));
            Assert.That(board.GetSideToMove(), Is.EqualTo(Color.WHITE));
            Assert.That(board.GetOppSide(), Is.EqualTo(Color.BLACK));
            Assert.That(board.CurrentGameState.EnPassantSquare, Is.EqualTo(-1));
            Assert.That(board.CurrentGameState.FullMoveNumber, Is.EqualTo(1));
            Assert.That(board.CurrentGameState.HalfMoveClock, Is.EqualTo(0));
            Assert.That(board.CurrentGameState.CanCastleWhiteKingside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleWhiteQueenside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleBlackKingside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleBlackQueenside, Is.True);
        }

        [Test]
        public void GameState2()
        {
            board.SetupPositionFromFEN("rnbqkbnr/ppp2ppp/8/3pp3/2B1P3/8/PPPP1PPP/RNBQK1NR w KQkq - 0 3");
            Assert.That(board.CurrentGameState.SideToMove, Is.EqualTo(Color.WHITE));
            Assert.That(board.GetSideToMove(), Is.EqualTo(Color.WHITE));
            Assert.That(board.GetOppSide(), Is.EqualTo(Color.BLACK));
            Assert.That(board.CurrentGameState.EnPassantSquare, Is.EqualTo(-1));
            Assert.That(board.CurrentGameState.FullMoveNumber, Is.EqualTo(3));
            Assert.That(board.CurrentGameState.HalfMoveClock, Is.EqualTo(0));
            Assert.That(board.CurrentGameState.CanCastleWhiteKingside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleWhiteQueenside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleBlackKingside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleBlackQueenside, Is.True);
        }

        [Test]
        public void GameState3()
        {
            board.SetupPositionFromFEN("rnbqkbnr/ppp2ppp/8/3pp3/2B1P3/8/PPPP1PPP/RNBQK1NR b KQkq - 0 3");
            Assert.That(board.CurrentGameState.SideToMove, Is.EqualTo(Color.BLACK));
            Assert.That(board.GetSideToMove(), Is.EqualTo(Color.BLACK));
            Assert.That(board.GetOppSide(), Is.EqualTo(Color.WHITE));
            Assert.That(board.CurrentGameState.EnPassantSquare, Is.EqualTo(-1));
            Assert.That(board.CurrentGameState.FullMoveNumber, Is.EqualTo(3));
            Assert.That(board.CurrentGameState.HalfMoveClock, Is.EqualTo(0));
            Assert.That(board.CurrentGameState.CanCastleWhiteKingside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleWhiteQueenside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleBlackKingside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleBlackQueenside, Is.True);
        }

        [Test]
        public void GameState4()
        {
            board.SetupPositionFromFEN("rnbqkbnr/pp3ppp/8/2pPp3/2B5/8/PPPP1PPP/RNBQK1NR w KQkq c6 0 4");
            Assert.That(board.CurrentGameState.SideToMove, Is.EqualTo(Color.WHITE));
            Assert.That(board.GetSideToMove(), Is.EqualTo(Color.WHITE));
            Assert.That(board.GetOppSide(), Is.EqualTo(Color.BLACK));
            Assert.That(board.CurrentGameState.EnPassantSquare, Is.EqualTo(BUTIL.C6));
            Assert.That(board.CurrentGameState.FullMoveNumber, Is.EqualTo(4));
            Assert.That(board.CurrentGameState.HalfMoveClock, Is.EqualTo(0));
            Assert.That(board.CurrentGameState.CanCastleWhiteKingside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleWhiteQueenside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleBlackKingside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleBlackQueenside, Is.True);
        }

        [Test]
        public void GameState5()
        {
            board.SetupPositionFromFEN("rnbqkbnr/pp3ppp/8/2pPp3/2B5/8/PPPPKPPP/RNBQ2NR b kq - 1 4");
            Assert.That(board.CurrentGameState.SideToMove, Is.EqualTo(Color.BLACK));
            Assert.That(board.GetSideToMove(), Is.EqualTo(Color.BLACK));
            Assert.That(board.GetOppSide(), Is.EqualTo(Color.WHITE));
            Assert.That(board.CurrentGameState.EnPassantSquare, Is.EqualTo(-1));
            Assert.That(board.CurrentGameState.FullMoveNumber, Is.EqualTo(4));
            Assert.That(board.CurrentGameState.HalfMoveClock, Is.EqualTo(1));
            Assert.That(board.CurrentGameState.CanCastleWhiteKingside, Is.False);
            Assert.That(board.CurrentGameState.CanCastleWhiteQueenside, Is.False);
            Assert.That(board.CurrentGameState.CanCastleBlackKingside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleBlackQueenside, Is.True);
        }

        [Test]
        public void GameState6()
        {
            board.MakeMove(DenseMove.FromPieceType(PieceType.W_PAWN, BUTIL.D2, BUTIL.D4), true);
            Assert.That(board.CurrentGameState.SideToMove, Is.EqualTo(Color.BLACK));
            Assert.That(board.GetSideToMove(), Is.EqualTo(Color.BLACK));
            Assert.That(board.GetOppSide(), Is.EqualTo(Color.WHITE));
            Assert.That(board.CurrentGameState.EnPassantSquare, Is.EqualTo(-1));
            Assert.That(board.CurrentGameState.FullMoveNumber, Is.EqualTo(1));
            Assert.That(board.CurrentGameState.HalfMoveClock, Is.EqualTo(0));
            Assert.That(board.CurrentGameState.CanCastleWhiteKingside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleWhiteQueenside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleBlackKingside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleBlackQueenside, Is.True);

            board.MakeMove(DenseMove.FromPieceType(PieceType.B_PAWN, BUTIL.A7, BUTIL.A5), true);
            Assert.That(board.CurrentGameState.SideToMove, Is.EqualTo(Color.WHITE));
            Assert.That(board.GetSideToMove(), Is.EqualTo(Color.WHITE));
            Assert.That(board.GetOppSide(), Is.EqualTo(Color.BLACK));
            Assert.That(board.CurrentGameState.EnPassantSquare, Is.EqualTo(-1));
            Assert.That(board.CurrentGameState.FullMoveNumber, Is.EqualTo(2));
            Assert.That(board.CurrentGameState.HalfMoveClock, Is.EqualTo(0));
            Assert.That(board.CurrentGameState.CanCastleWhiteKingside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleWhiteQueenside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleBlackKingside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleBlackQueenside, Is.True);

            board.MakeMove(DenseMove.FromPieceType(PieceType.W_PAWN, BUTIL.D4, BUTIL.D5), true);
            Assert.That(board.CurrentGameState.SideToMove, Is.EqualTo(Color.BLACK));
            Assert.That(board.GetSideToMove(), Is.EqualTo(Color.BLACK));
            Assert.That(board.GetOppSide(), Is.EqualTo(Color.WHITE));
            Assert.That(board.CurrentGameState.EnPassantSquare, Is.EqualTo(-1));
            Assert.That(board.CurrentGameState.FullMoveNumber, Is.EqualTo(2));
            Assert.That(board.CurrentGameState.HalfMoveClock, Is.EqualTo(0));
            Assert.That(board.CurrentGameState.CanCastleWhiteKingside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleWhiteQueenside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleBlackKingside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleBlackQueenside, Is.True);

            board.MakeMove(DenseMove.FromPieceType(PieceType.B_PAWN, BUTIL.E7, BUTIL.E5), true);
            Assert.That(board.CurrentGameState.SideToMove, Is.EqualTo(Color.WHITE));
            Assert.That(board.GetSideToMove(), Is.EqualTo(Color.WHITE));
            Assert.That(board.GetOppSide(), Is.EqualTo(Color.BLACK));
            Assert.That(board.CurrentGameState.EnPassantSquare, Is.EqualTo(BUTIL.E6));
            Assert.That(board.CurrentGameState.FullMoveNumber, Is.EqualTo(3));
            Assert.That(board.CurrentGameState.HalfMoveClock, Is.EqualTo(0));
            Assert.That(board.CurrentGameState.CanCastleWhiteKingside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleWhiteQueenside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleBlackKingside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleBlackQueenside, Is.True);

            board.UnmakeMove(DenseMove.FromPieceType(PieceType.B_PAWN, BUTIL.E7, BUTIL.E5), true);
            Assert.That(board.CurrentGameState.SideToMove, Is.EqualTo(Color.BLACK));
            Assert.That(board.GetSideToMove(), Is.EqualTo(Color.BLACK));
            Assert.That(board.GetOppSide(), Is.EqualTo(Color.WHITE));
            Assert.That(board.CurrentGameState.EnPassantSquare, Is.EqualTo(-1));
            Assert.That(board.CurrentGameState.FullMoveNumber, Is.EqualTo(2));
            Assert.That(board.CurrentGameState.HalfMoveClock, Is.EqualTo(0));
            Assert.That(board.CurrentGameState.CanCastleWhiteKingside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleWhiteQueenside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleBlackKingside, Is.True);
            Assert.That(board.CurrentGameState.CanCastleBlackQueenside, Is.True);
        }

        [Test]
        public void GameState7()
        {
            board.SetupPositionFromFEN("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1");
            board.MakeMove(DenseMove.FromPieceType(PieceType.W_ROOK, 7, 15), false);

            Assert.That(board.CurrentGameState.CanCastleWhiteKingside, Is.Not.True);
            Assert.That(board.CurrentGameState.CanCastleWhiteQueenside, Is.True);
        }

        [Test]
        public void EnPassantUpdate()
        {
            board.SetupPositionFromFEN("rnbqkbnr/ppp1pppp/8/8/3p4/8/PPPPPPPP/RNBQKBNR w KQkq - 0 3");
            board.MakeMove(DenseMove.FromPieceType(PieceType.W_PAWN, 12, 28), false);

            Assert.That(board.CurrentGameState.EnPassantSquare, Is.EqualTo(20));

        }

        [Test]
        public void IsInCheck1()
        {
            Assert.That(board.IsInCheck(), Is.False);
            Assert.That(board.IsSideInCheck(Color.WHITE), Is.False);
            Assert.That(board.IsSideInCheck(Color.BLACK), Is.False);

            board.SetupPositionFromFEN("r1bq1rk1/p1pp2pp/2n2n2/1p2pp2/3PPB2/b1N2N2/PPP1QPPP/R2K1B1R w - - 3 8");
            Assert.That(board.IsInCheck(), Is.False);
            Assert.That(board.IsSideInCheck(Color.WHITE), Is.False);
            Assert.That(board.IsSideInCheck(Color.BLACK), Is.False);

            board.SetupPositionFromFEN("r1bq1rk1/p1pp2pp/2n2n2/1p2pp2/2QPPB2/b1N2N2/PPP2PPP/R2K1B1R b - - 4 8");
            Assert.That(board.IsInCheck(), Is.True);
            Assert.That(board.IsSideInCheck(Color.WHITE), Is.False);
            Assert.That(board.IsSideInCheck(Color.BLACK), Is.True);
        }

        [Test]
        public void IsInCheck2()
        {
            board.MakeMove(DenseMove.FromPieceType(PieceType.W_PAWN, BUTIL.E2, BUTIL.E4), false);
            Assert.That(board.IsInCheck(), Is.False);
            Assert.That(board.IsSideInCheck(Color.WHITE), Is.False);
            Assert.That(board.IsSideInCheck(Color.BLACK), Is.False);

            board.MakeMove(DenseMove.FromPieceType(PieceType.B_PAWN, BUTIL.E7, BUTIL.E5), false);
            Assert.That(board.IsInCheck(), Is.False);
            Assert.That(board.IsSideInCheck(Color.WHITE), Is.False);
            Assert.That(board.IsSideInCheck(Color.BLACK), Is.False);

            board.MakeMove(DenseMove.FromPieceType(PieceType.W_BISHOP, BUTIL.F1, BUTIL.B5), false);
            Assert.That(board.IsInCheck(), Is.False);

            board.MakeMove(DenseMove.FromPieceType(PieceType.B_BISHOP, BUTIL.F8, BUTIL.B4), false);
            Assert.That(board.IsInCheck(), Is.False);

            board.MakeMove(DenseMove.FromPieceType(PieceType.W_BISHOP, BUTIL.B5, BUTIL.D7, DenseType.D_PAWN), false);
            Assert.That(board.IsInCheck(), Is.True);
            Assert.That(board.IsSideInCheck(Color.WHITE), Is.False);
            Assert.That(board.IsSideInCheck(Color.BLACK), Is.True);

            board.UnmakeMove(DenseMove.FromPieceType(PieceType.W_BISHOP, BUTIL.B5, BUTIL.D7, DenseType.D_PAWN), false);
            Assert.That(board.IsInCheck(), Is.False);
            Assert.That(board.IsSideInCheck(Color.WHITE), Is.False);
            Assert.That(board.IsSideInCheck(Color.BLACK), Is.False);
        }

        [Test]
        public void GetFEN1()
        {
            Console.WriteLine(board.GetFEN());
            Assert.That(board.GetFEN(), Is.EqualTo("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"));
        }

        [Test]
        public void GetFEN2()
        {
            board.SetupPositionFromFEN("r1bq1rk1/p1pp2pp/2n2n2/1p2pp2/3PPB2/b1N2N2/PPP1QPPP/R2K1B1R w - - 3 8");
            Assert.That(board.GetFEN(), Is.EqualTo("r1bq1rk1/p1pp2pp/2n2n2/1p2pp2/3PPB2/b1N2N2/PPP1QPPP/R2K1B1R w - - 3 8"));
        }

        [Test]
        public void Zobrist1()
        {
            var board1 = new ChessBoard();
            var board2 = new ChessBoard();
            Assert.That(board1.ZobristKey, Is.EqualTo(board2.ZobristKey));
        }

        [Test]
        public void Zobrist2()
        {
            var board1 = new ChessBoard();
            var board2 = new ChessBoard();

            var pawn = DenseMove.FromPieceType(PieceType.W_PAWN, BUTIL.E2, BUTIL.E4);
            Console.WriteLine("Board2 key before MakeMove:");
            Utility.PrintBBLine(board2.ZobristKey);
            board2.MakeMove(pawn, false);
            Console.WriteLine("Board2 key after MakeMove:");
            Utility.PrintBBLine(board2.ZobristKey);
            board2.UnmakeMove(pawn, false);
            Console.WriteLine("Board2 key after UnmakeMove:");
            Utility.PrintBBLine(board2.ZobristKey);

            Assert.That(board1.ZobristKey, Is.EqualTo(board2.ZobristKey));

            Console.WriteLine("Board1 key:");
            Utility.PrintBBLine(board1.ZobristKey);
            Console.WriteLine("Board2 key:");
            Utility.PrintBBLine(board2.ZobristKey);
        }

        [Test]
        public void Zobrist3()
        {
            var board1 = new ChessBoard();
            board1.SetupPositionFromFEN("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq - 0 1");
            var board2 = new ChessBoard();

            var pawn = DenseMove.FromPieceType(PieceType.W_PAWN, BUTIL.E2, BUTIL.E4);

            Console.WriteLine("Board2 key before MakeMove:");
            Utility.PrintBBLine(board2.ZobristKey);
            board2.MakeMove(pawn, false);
            Console.WriteLine("Board2 key after MakeMove:");
            Utility.PrintBBLine(board2.ZobristKey);

            Assert.That(board1.ZobristKey, Is.EqualTo(board2.ZobristKey));

            Console.WriteLine("Board1 key:");
            Utility.PrintBBLine(board1.ZobristKey);
            Console.WriteLine("Board2 key:");
            Utility.PrintBBLine(board2.ZobristKey);

            Console.WriteLine($"\n\nWhite Pawn at e2 key: {Zobrist.GetPieceSqKey(BUTIL.E2, PieceType.W_PAWN)}\nWhite Pawn at e4 key: {Zobrist.GetPieceSqKey(BUTIL.E4, PieceType.W_PAWN)}\nInitial ZKey: {board.ZobristKey}");
            board.ZobristKey ^= Zobrist.GetPieceSqKey(BUTIL.E2, PieceType.W_PAWN);
            Console.WriteLine($"ZKey after XOR wpe2: {board.ZobristKey}");
            board.ZobristKey ^= Zobrist.GetPieceSqKey(BUTIL.E4, PieceType.W_PAWN);
            Console.WriteLine($"ZKey after XOR wpe4: {board.ZobristKey}");
            Console.WriteLine($"Black to move key: {Zobrist.ZobristSideToMove}");
            board.ZobristKey ^= Zobrist.ZobristSideToMove;
            Console.WriteLine($"ZKey after XOR black: {board.ZobristKey}");
        }

        [Test]
        public void Zobrist4()
        {
            var board1 = new ChessBoard();

            board.SetupPositionFromFEN("r1bqkb1r/pppppppp/2n2n2/8/8/2N2N2/PPPPPPPP/R1BQKB1R w KQkq - 4 3");

            board1.MakeMove(DenseMove.FromPieceType(PieceType.W_KNIGHT, BUTIL.B1, BUTIL.C3), false);
            board1.MakeMove(DenseMove.FromPieceType(PieceType.B_KNIGHT, BUTIL.G8, BUTIL.F6), false);
            board1.MakeMove(DenseMove.FromPieceType(PieceType.W_KNIGHT, BUTIL.G1, BUTIL.F3), false);
            board1.MakeMove(DenseMove.FromPieceType(PieceType.B_KNIGHT, BUTIL.B8, BUTIL.C6), false);
            Assert.That(board.ZobristKey, Is.EqualTo(board1.ZobristKey));

            board.SetupPositionFromFEN("r1bqkb1r/pppppppp/2n2n2/8/8/2N2N2/PPPPPPPP/1RBQKB1R b Kkq - 5 3");

            board1.MakeMove(DenseMove.FromPieceType(PieceType.W_ROOK, BUTIL.A1, BUTIL.B1), false);
            Assert.That(board.ZobristKey, Is.EqualTo(board1.ZobristKey));
        }

        [Test]
        public void Zobrist5()
        {
            var board1 = new ChessBoard();
            board1.MakeMove(DenseMove.FromPieceType(PieceType.W_PAWN, BUTIL.A2, BUTIL.A3), false);
            board1.MakeMove(DenseMove.FromPieceType(PieceType.B_PAWN, BUTIL.A7, BUTIL.A6), false);
            board1.MakeMove(DenseMove.FromPieceType(PieceType.W_ROOK, BUTIL.A1, BUTIL.A2), false);
            board1.MakeMove(DenseMove.FromPieceType(PieceType.B_ROOK, BUTIL.A8, BUTIL.A7), false);
            board1.MakeMove(DenseMove.FromPieceType(PieceType.W_PAWN, BUTIL.H2, BUTIL.H3), false);
            board1.MakeMove(DenseMove.FromPieceType(PieceType.B_PAWN, BUTIL.H7, BUTIL.H6), false);
            board1.MakeMove(DenseMove.FromPieceType(PieceType.W_ROOK, BUTIL.H1, BUTIL.H2), false);
            board1.MakeMove(DenseMove.FromPieceType(PieceType.B_ROOK, BUTIL.H8, BUTIL.H7), false);

            board.SetupPositionFromFEN("1nbqkbn1/rppppppr/p6p/8/8/P6P/RPPPPPPR/1NBQKBN1 w - - 2 5");
            Assert.That(board.ZobristKey, Is.EqualTo(board1.ZobristKey));

            board.SetupPositionFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

            board1.UnmakeMove(DenseMove.FromPieceType(PieceType.B_ROOK, BUTIL.H8, BUTIL.H7), false);
            board1.UnmakeMove(DenseMove.FromPieceType(PieceType.W_ROOK, BUTIL.H1, BUTIL.H2), false);
            board1.UnmakeMove(DenseMove.FromPieceType(PieceType.B_PAWN, BUTIL.H7, BUTIL.H6), false);
            board1.UnmakeMove(DenseMove.FromPieceType(PieceType.W_PAWN, BUTIL.H2, BUTIL.H3), false);
            board1.UnmakeMove(DenseMove.FromPieceType(PieceType.B_ROOK, BUTIL.A8, BUTIL.A7), false);
            board1.UnmakeMove(DenseMove.FromPieceType(PieceType.W_ROOK, BUTIL.A1, BUTIL.A2), false);
            board1.UnmakeMove(DenseMove.FromPieceType(PieceType.B_PAWN, BUTIL.A7, BUTIL.A6), false);
            board1.UnmakeMove(DenseMove.FromPieceType(PieceType.W_PAWN, BUTIL.A2, BUTIL.A3), false);

            Assert.That(board.ZobristKey, Is.EqualTo(board1.ZobristKey));
        }

        [Test]
        public void Zobrist6()
        {
            var board1 = new ChessBoard();
            board.SetupPositionFromFEN("rnbqkbnr/ppp1p1pp/8/3pPp2/8/8/PPPP1PPP/RNBQKBNR w KQkq f6 0 3");

            board1.MakeMove(DenseMove.FromPieceType(PieceType.W_PAWN, BUTIL.E2, BUTIL.E4), false);
            board1.MakeMove(DenseMove.FromPieceType(PieceType.B_PAWN, BUTIL.D7, BUTIL.D5), false);
            board1.MakeMove(DenseMove.FromPieceType(PieceType.W_PAWN, BUTIL.E4, BUTIL.E5), false);
            board1.MakeMove(DenseMove.FromPieceType(PieceType.B_PAWN, BUTIL.F7, BUTIL.F5), false);

            Assert.That(board.ZobristKey, Is.EqualTo(board1.ZobristKey));
        }

        [Test]
        public void Zobrist7()
        {
            board.SetupPositionFromFEN("rnbqkbnr/pppppppp/8/8/3P4/8/PPP1PPPP/RNBQKBNR b KQkq - 0 1");
            var board1 = new ChessBoard();
            board1.MakeMove(DenseMove.FromPieceType(PieceType.W_PAWN, BUTIL.D2, BUTIL.D4), false);
            Assert.That(board.ZobristKey, Is.EqualTo(board1.ZobristKey));

            board.SetupPositionFromFEN("rnbqkbnr/pppp1ppp/8/4p3/3P4/8/PPP1PPPP/RNBQKBNR w KQkq - 0 2");
            board1.MakeMove(DenseMove.FromPieceType(PieceType.B_PAWN, BUTIL.E7, BUTIL.E5), false);
            Assert.That(board.ZobristKey, Is.EqualTo(board1.ZobristKey));

            board.SetupPositionFromFEN("rnbqkbnr/pppp1ppp/8/4P3/8/8/PPP1PPPP/RNBQKBNR b KQkq - 0 2");
            board1.MakeMove(DenseMove.FromPieceType(PieceType.W_PAWN, BUTIL.D4, BUTIL.E5, DenseType.D_PAWN), false);
            Assert.That(board.ZobristKey, Is.EqualTo(board1.ZobristKey));
        }

        [Test]
        public void Zobrist8()
        {
            var board1 = new ChessBoard();
            board.SetupPositionFromFEN("5k2/8/8/8/8/8/8/4K2q w - - 0 2");

            board1.SetupPositionFromFEN("5k2/8/8/8/8/8/6p1/4K2Q b - - 0 1");
            var promo = DenseMove.FromPieceType(PieceType.B_PAWN, BUTIL.G2, BUTIL.H1, DenseType.D_QUEEN);
            promo.SetPromoteTo(DenseType.D_QUEEN);
            board1.MakeMove(promo, false);

            Assert.That(board.ZobristKey, Is.EqualTo(board1.ZobristKey));
        }

        [Test]
        public void Zobrist9()
        {
            var board1 = new ChessBoard();

            var nc3 = DenseMove.FromPieceType(PieceType.W_KNIGHT, BUTIL.B1, BUTIL.C3);
            var nf3 = DenseMove.FromPieceType(PieceType.W_KNIGHT, BUTIL.G1, BUTIL.F3);
            var nf6 = DenseMove.FromPieceType(PieceType.B_KNIGHT, BUTIL.G8, BUTIL.F6);
            var nc6 = DenseMove.FromPieceType(PieceType.B_KNIGHT, BUTIL.B8, BUTIL.C6);

            board.MakeMove(nc3, false);
            board.MakeMove(nc6, false);
            board.MakeMove(nf3, false);
            board.MakeMove(nf6, false);

            board1.MakeMove(nf3, false);
            board1.MakeMove(nc6, false);
            board1.MakeMove(nc3, false);
            board1.MakeMove(nf6, false);

            Assert.That(board.ZobristKey, Is.EqualTo(board1.ZobristKey));
        }

        [Test]
        public void Zobrist10()
        {
            var board1 = new ChessBoard();

            var g4 = DenseMove.FromPieceType(PieceType.W_PAWN, BUTIL.G2, BUTIL.G4);
            var h4 = DenseMove.FromPieceType(PieceType.W_PAWN, BUTIL.H2, BUTIL.H4);
            var h5 = DenseMove.FromPieceType(PieceType.B_PAWN, BUTIL.H7, BUTIL.H5);

            board.MakeMove(g4, false);
            board.MakeMove(h5, false);
            board.MakeMove(h4, false);

            board1.SetupPositionFromFEN("rnbqkbnr/ppppppp1/8/7p/6PP/8/PPPPPP2/RNBQKBNR b KQkq - 0 2");
            Assert.That(board.ZobristKey, Is.EqualTo(board1.ZobristKey));
        }

    }

}

