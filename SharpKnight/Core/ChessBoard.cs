using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Numerics;
using SharpKnight.Core;

namespace SharpKnight.Core
{
    /// <summary>
    /// Represents the state of a chessboard using bitboards.
    /// </summary>
    public class ChessBoard
    {
        // Public state
        /// <summary>
        /// Current game state including side to move, castling rights, en passant square, and move counters.
        /// </summary>
        public GameState CurrentGameState;

        /// <summary> Zobrist hash key for the current position. </summary>
        public U64 ZobristKey;

        /// <summary> Index of the current ply in the game history. </summary>
        public int PlyIndex;

        /// <summary> History of game states for undo functionality. </summary>
        public GameState[] StateHistory = new GameState[Consts.MAX_PLY];

        /// <summary>
        /// Set of Zobrist keys encountered in the current game (for repetition detection).
        /// </summary>
        public HashSet<U64> KeySet = new HashSet<U64>();

        // Bitboards
        /// <summary>
        /// Piece bitboards, contain both colors.
        /// </summary>
        private U64[] pieceBB = new U64[7]; // DenseType index
        /// <summary>
        /// Color bitboards, contain all pieces of one color.
        /// </summary>
        private U64[] colorBB = new U64[2]; // Color index

        /// <summary>
        /// Hold square indices of each king. [WHITE]=0,[BLACK]=1
        /// </summary>
        private int[] kingSquares = new int[2];

        /// <summary>
        /// Constructor
        /// </summary>
        public ChessBoard()
        {
            if (!PEXT.Initialized)
            {
                PEXT.Initialize();
            }
            InitializeWhiteBB();
            InitializeBlackBB();
            InitializeEmptyBB();
            InitializePawnsBB();
            InitializeKnightsBB();
            InitializeBishopsBB();
            InitializeRooksBB();
            InitializeQueensBB();
            InitializeKingsBB();
            InitializeGameState();
        }

        // =========================
        // FEN setup / printing
        // =========================

        /// <summary>
        /// Sets up board position according to FEN string.
        /// If FEN is empty or improperly formatted, sets up default starting position.
        /// </summary>
        /// <param name="fen">FEN string</param>
        public void SetupPositionFromFEN(string fen)
        {
            // Clear boards/state
            for (int i = 0; i < 7; i++) pieceBB[i] = 0UL;
            colorBB[(int)Color.WHITE] = 0UL;
            colorBB[(int)Color.BLACK] = 0UL;
            kingSquares[(int)Color.WHITE] = 0;
            kingSquares[(int)Color.BLACK] = 0;
            ZobristKey = 0UL;

            CurrentGameState.CanCastleBlackKingside = false;
            CurrentGameState.CanCastleBlackQueenside = false;
            CurrentGameState.CanCastleWhiteKingside = false;
            CurrentGameState.CanCastleWhiteQueenside = false;

            var parts = fen.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 6)
            {
                // Fallback to initial
                SetupPositionFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
                Console.Error.WriteLine("Invalid FEN; set to default position");
                return;
            }

            string position = parts[0];
            string playerToMove = parts[1];
            string castlingRights = parts[2];
            string enPassant = parts[3];
            string halfTurns = parts[4];
            string fullTurns = parts[5];

            int square = 56; // a8

            // Piece mapping
            foreach (char c in position)
            {
                if (square < 0)
                {
                    SetupPositionFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
                    Console.Error.WriteLine("Invalid FEN; set to default position");
                    return;
                }
                if (c == '/')
                {
                    square -= 16;
                }
                else if (char.IsDigit(c))
                {
                    square += (c - '0');
                }
                else if (UtilityMaps.FENToPiece.TryGetValue(c, out var piece))
                {
                    U64 squareBB = 1UL << square;

                    int col = piece <= PieceType.W_KING ? 0 : 1;
                    colorBB[col] |= squareBB;

                    pieceBB[PieceCode(piece)] |= squareBB;

                    if (piece == PieceType.W_KING) kingSquares[(int)Color.WHITE] = square;
                    if (piece == PieceType.B_KING) kingSquares[(int)Color.BLACK] = square;

                    square++;
                }
            }

            // Empty bitboard
            pieceBB[(int)DenseType.D_EMPTY] = (~colorBB[(int)Color.WHITE]) & (~colorBB[(int)Color.BLACK]);

            // Side
            CurrentGameState.SideToMove = (playerToMove == "w") ? Color.WHITE : Color.BLACK;

            // Castling
            foreach (char c in castlingRights)
            {
                if (c == 'K') CurrentGameState.CanCastleWhiteKingside = true;
                if (c == 'Q') CurrentGameState.CanCastleWhiteQueenside = true;
                if (c == 'k') CurrentGameState.CanCastleBlackKingside = true;
                if (c == 'q') CurrentGameState.CanCastleBlackQueenside = true;
            }

            // En passant
            CurrentGameState.EnPassantSquare = (enPassant != "-") ? AlgebraicToIndex(enPassant) : -1;

            // Half/full move counts
            CurrentGameState.HalfMoveClock = ParseInt(halfTurns);
            CurrentGameState.FullMoveNumber = ParseInt(fullTurns);

            PlyIndex = 0;
            StateHistory[PlyIndex] = CurrentGameState;

            // Initialize and generate first Zobrist key
            if (!Zobrist.Initialized) Zobrist.Initialize();
            ZobristKey = GenerateZobristKey();
            KeySet.Clear();
            KeySet.Add(ZobristKey);
        }

        /// <returns>Properly formatted FEN string of current board.</returns>
        public string GetFEN()
        {
            var fen = new StringBuilder();
            int emptyCount = 0;

            for (int square = 56; square >= 0; square++)
            {
                PieceType current = GetPieceAt(square);
                if (current == PieceType.EMPTY)
                {
                    emptyCount++;
                    // End of rank
                    if (emptyCount >= 8 || square % 8 == 7)
                    {
                        fen.Append(emptyCount);
                        if (square != 7) fen.Append('/');
                        square -= 16;
                        emptyCount = 0;
                        continue;
                    }
                }
                else
                {
                    string fenPiece = UtilityMaps.PieceToFEN[current];
                    if (emptyCount != 0)
                    {
                        fen.Append(emptyCount);
                        emptyCount = 0;
                    }
                    fen.Append(fenPiece);
                    if (square % 8 == 7)
                    {
                        if (square != 7) fen.Append('/');
                        square -= 16;
                    }
                }
            }

            // Side
            fen.Append(' ');
            fen.Append(CurrentGameState.SideToMove == Color.WHITE ? "w" : "b");

            // Castling
            fen.Append(' ');
            bool anyCastle = false;
            if (CurrentGameState.CanCastleWhiteKingside) { fen.Append('K'); anyCastle = true; }
            if (CurrentGameState.CanCastleWhiteQueenside) { fen.Append('Q'); anyCastle = true; }
            if (CurrentGameState.CanCastleBlackKingside) { fen.Append('k'); anyCastle = true; }
            if (CurrentGameState.CanCastleBlackQueenside) { fen.Append('q'); anyCastle = true; }
            if (!anyCastle) fen.Append('-');

            // En passant
            fen.Append(' ');
            fen.Append(CurrentGameState.EnPassantSquare == -1 ? "-" : IndexToAlgebraic(CurrentGameState.EnPassantSquare));

            // Half-move
            fen.Append(' ');
            fen.Append(CurrentGameState.HalfMoveClock.ToString(CultureInfo.InvariantCulture));

            // Full-move
            fen.Append(' ');
            fen.Append(CurrentGameState.FullMoveNumber.ToString(CultureInfo.InvariantCulture));

            return fen.ToString();
        }

        /// <summary>
        /// Prints out current FEN to console.
        /// </summary>
        public void PrintFEN() => Console.WriteLine($"\n(ChessBoard printFEN) {GetFEN()}\n");

        // =========================
        // Bitboard getters
        // =========================

        /// <param name="pt">Given PieceType</param>
        /// <returns>Bitboard of the given PieceType (White Pawn, Black King etc).</returns>
        public U64 GetPieceSet(PieceType pt)
        {
            if (pt == PieceType.EMPTY) return pieceBB[(int)DenseType.D_EMPTY];
            if (pt == PieceType.INVALID) return 0UL;
            return pieceBB[PieceCode(pt)] & colorBB[ColorCode(pt)];
        }

        /// <param name="dt">Given DenseType</param>
        /// <returns>Bitboard of the given DenseType (Pawns, Rooks etc).</returns>
        public U64 GetDenseSet(DenseType dt) => pieceBB[(int)dt];

        /// <returns>Bitboard of white pawns.</returns>
        public U64 GetWhitePawns() => colorBB[(int)Color.WHITE] & pieceBB[(int)DenseType.D_PAWN];
        /// <returns>Bitboard of white knights.</returns>
        public U64 GetWhiteKnights() => colorBB[(int)Color.WHITE] & pieceBB[(int)DenseType.D_KNIGHT];
        /// <returns>Bitboard of white bishops.</returns>
        public U64 GetWhiteBishops() => colorBB[(int)Color.WHITE] & pieceBB[(int)DenseType.D_BISHOP];
        /// <returns>Bitboard of white rooks.</returns>
        public U64 GetWhiteRooks() => colorBB[(int)Color.WHITE] & pieceBB[(int)DenseType.D_ROOK];
        /// <returns>Bitboard of white queens.</returns>
        public U64 GetWhiteQueens() => colorBB[(int)Color.WHITE] & pieceBB[(int)DenseType.D_QUEEN];
        /// <returns>Bitboard of white kings.</returns>
        public U64 GetWhiteKings() => colorBB[(int)Color.WHITE] & pieceBB[(int)DenseType.D_KING];
        /// <returns>Bitboard of black pawns.</returns>
        public U64 GetBlackPawns() => colorBB[(int)Color.BLACK] & pieceBB[(int)DenseType.D_PAWN];
        /// <returns>Bitboard of black knights.</returns>
        public U64 GetBlackKnights() => colorBB[(int)Color.BLACK] & pieceBB[(int)DenseType.D_KNIGHT];
        /// <returns>Bitboard of black bishops.</returns>
        public U64 GetBlackBishops() => colorBB[(int)Color.BLACK] & pieceBB[(int)DenseType.D_BISHOP];
        /// <returns>Bitboard of black rooks.</returns>
        public U64 GetBlackRooks() => colorBB[(int)Color.BLACK] & pieceBB[(int)DenseType.D_ROOK];
        /// <returns>Bitboard of black queens.</returns>
        public U64 GetBlackQueens() => colorBB[(int)Color.BLACK] & pieceBB[(int)DenseType.D_QUEEN];
        /// <returns>Bitboard of black kings.</returns>
        public U64 GetBlackKings() => colorBB[(int)Color.BLACK] & pieceBB[(int)DenseType.D_KING];
        /// <returns>Bitboard of white pieces.</returns>
        public U64 GetWhitePieces() => colorBB[(int)Color.WHITE];
        /// <returns>Bitboard of black pieces.</returns>
        public U64 GetBlackPieces() => colorBB[(int)Color.BLACK];
        /// <returns>Bitboard of all pieces.</returns>
        public U64 GetAllPieces() => colorBB[(int)Color.WHITE] | colorBB[(int)Color.BLACK];
        /// <returns>Bitboard of all empty squares.</returns>
        public U64 GetEmptySquares() => pieceBB[(int)DenseType.D_EMPTY];

        // =========================
        // King square / side getters
        // =========================

        /// <returns>Index of white king square.</returns>
        public int GetWhiteKingSquare() => kingSquares[(int)Color.WHITE];
        /// <returns>Index of black king square.</returns>
        public int GetBlackKingSquare() => kingSquares[(int)Color.BLACK];
        /// <returns>Color of the side to make a move.</returns>
        public Color GetSideToMove() => CurrentGameState.SideToMove;
        /// <returns>Color of the opposite side to make a move.</returns>
        public Color GetOppSide() => (CurrentGameState.SideToMove == Color.WHITE) ? Color.BLACK : Color.WHITE;

        // =========================
        // Piece queries
        // =========================

        /// <param name="index">Square index</param>
        /// <returns>PieceType of the piece (or empty) at square index.</returns>
        public PieceType GetPieceAt(int index)
        {
            if (index < 0 || index > 63)
            {
                Console.Error.WriteLine($"ChessBoard GetPieceAt Error: Invalid index {index}");
                return PieceType.INVALID;
            }
            if ((GetAllPieces() & (1UL << index)) == 0UL) return PieceType.EMPTY;

            bool isBlack = (GetBlackPieces() & (1UL << index)) != 0;

            if ((pieceBB[(int)DenseType.D_PAWN] & (1UL << index)) != 0) return isBlack ? PieceType.B_PAWN : PieceType.W_PAWN;
            if ((pieceBB[(int)DenseType.D_KNIGHT] & (1UL << index)) != 0) return isBlack ? PieceType.B_KNIGHT : PieceType.W_KNIGHT;
            if ((pieceBB[(int)DenseType.D_BISHOP] & (1UL << index)) != 0) return isBlack ? PieceType.B_BISHOP : PieceType.W_BISHOP;
            if ((pieceBB[(int)DenseType.D_ROOK] & (1UL << index)) != 0) return isBlack ? PieceType.B_ROOK : PieceType.W_ROOK;
            if ((pieceBB[(int)DenseType.D_QUEEN] & (1UL << index)) != 0) return isBlack ? PieceType.B_QUEEN : PieceType.W_QUEEN;
            if ((pieceBB[(int)DenseType.D_KING] & (1UL << index)) != 0) return isBlack ? PieceType.B_KING : PieceType.W_KING;

            Console.WriteLine($"ChessBoard GetPieceAt Error: Invalid piece at square {index}");
            return PieceType.INVALID;
        }

        /// <param name="index">Square index</param>
        /// <returns>DenseType of the piece (or empty) at square index.</returns>
        public DenseType GetDenseTypeAt(int index)
        {
            if (index < 0 || index > 63)
            {
                Console.Error.WriteLine($"ChessBoard GetDenseTypeAt Error: Invalid index {index}");
                return DenseType.D_EMPTY;
            }
            if ((GetAllPieces() & (1UL << index)) == 0UL) return DenseType.D_EMPTY;

            if ((pieceBB[(int)DenseType.D_PAWN] & (1UL << index)) != 0) return DenseType.D_PAWN;
            if ((pieceBB[(int)DenseType.D_KNIGHT] & (1UL << index)) != 0) return DenseType.D_KNIGHT;
            if ((pieceBB[(int)DenseType.D_BISHOP] & (1UL << index)) != 0) return DenseType.D_BISHOP;
            if ((pieceBB[(int)DenseType.D_ROOK] & (1UL << index)) != 0) return DenseType.D_ROOK;
            if ((pieceBB[(int)DenseType.D_QUEEN] & (1UL << index)) != 0) return DenseType.D_QUEEN;
            if ((pieceBB[(int)DenseType.D_KING] & (1UL << index)) != 0) return DenseType.D_KING;

            Console.Error.WriteLine($"ChessBoard GetDenseTypeAt Error: Invalid piece at square {index}");
            return DenseType.D_EMPTY;
        }

        // =========================
        // Check/attacks
        // =========================

        /// <returns>True if the side to move is in check, false if not in check.</returns>
        public bool IsInCheck() => IsSideInCheck(GetSideToMove());
        /// <param name="side">Color of side to check</param>
        /// <returns>True if given color is in check, false if not in check.</returns>
        public bool IsSideInCheck(Color side) => (OppAttacksToSquare(kingSquares[(int)side], side) != 0UL);

        /// <summary>
        /// Calculates all psuedo-legal attacks of opposite color to square 'index'.
        /// </summary>
        /// <param name="index">Square indez</param>
        /// <param name="colorOfKing">Color of side to check</param>
        /// <returns>Bitboard of all pieces attacking 'index'.</returns>
        public U64 OppAttacksToSquare(int index, Color colorOfKing)
        {
            if (index < 0 || index > 63) return 0UL;

            U64 opPawns, opKnights, opBQ, opRQ, opK, occupancy, pawnAtkMask;

            if (colorOfKing == Color.WHITE)
            {
                opPawns = GetBlackPawns();
                opKnights = GetBlackKnights();
                opRQ = GetBlackRooks() | GetBlackQueens();
                opBQ = GetBlackBishops() | GetBlackQueens();
                opK = GetBlackKings();
                pawnAtkMask = AttackMasks.WPawn[index];
            }
            else
            {
                opPawns = GetWhitePawns();
                opKnights = GetWhiteKnights();
                opRQ = GetWhiteRooks() | GetWhiteQueens();
                opBQ = GetWhiteBishops() | GetWhiteQueens();
                opK = GetWhiteKings();
                pawnAtkMask = AttackMasks.BPawn[index];
            }

            occupancy = GetAllPieces();

            return
                (pawnAtkMask & opPawns) |
                (AttackMasks.Knight[index] & opKnights) |
                (PEXT.GetRookAttacks(index, occupancy) & opRQ) |
                (PEXT.GetBishopAttacks(index, occupancy) & opBQ) |
                (AttackMasks.King[index] & opK);
        }

        /// <summary>
        /// Calculate all psuedo-legal attacks for color 'side'.
        /// </summary>
        /// <param name="side">Color of side to calculate attacks for.</param>
        /// <returns>Bitboard of all psuedo-legal attacks of color 'side'.</returns>
        public U64 CalculateAttacksForSide(Color side)
        {
            U64 occupancy = GetAllPieces();
            U64 attacks = 0UL;
            U64 oppOrtho = side == Color.BLACK ? (GetBlackRooks() | GetBlackQueens())
                                               : (GetWhiteRooks() | GetWhiteQueens());
            U64 oppDiag = side == Color.BLACK ? (GetBlackBishops() | GetBlackQueens())
                                              : (GetWhiteBishops() | GetWhiteQueens());

            // Orthogonals
            U64 tmp = oppOrtho;
            while (tmp != 0UL)
            {
                int idx = LsbIndex(tmp);
                attacks |= PEXT.GetRookAttacks(idx, occupancy);
                tmp &= tmp - 1;
            }
            // Diagonals
            tmp = oppDiag;
            while (tmp != 0UL)
            {
                int idx = LsbIndex(tmp);
                attacks |= PEXT.GetBishopAttacks(idx, occupancy);
                tmp &= tmp - 1;
            }
            // Knights
            tmp = (side == Color.BLACK) ? GetBlackKnights() : GetWhiteKnights();
            while (tmp != 0UL)
            {
                int idx = LsbIndex(tmp);
                attacks |= AttackMasks.Knight[idx];
                tmp &= tmp - 1;
            }
            // Pawns
            tmp = (side == Color.BLACK) ? GetBlackPawns() : GetWhitePawns();
            while (tmp != 0UL)
            {
                int idx = LsbIndex(tmp);
                attacks |= (side == Color.BLACK) ? AttackMasks.WPawn[idx] : AttackMasks.BPawn[idx];
                tmp &= tmp - 1;
            }
            // Kings
            tmp = (side == Color.BLACK) ? GetBlackKings() : GetWhiteKings();
            while (tmp != 0UL)
            {
                int idx = LsbIndex(tmp);
                attacks |= AttackMasks.King[idx];
                tmp &= tmp - 1;
            }

            return attacks;
        }

        // =========================
        // Make / Unmake
        // =========================

        /// <summary>
        /// Makes a move on the chessboard and updates the gamestate.
        /// </summary>
        /// <param name="move">The DenseMove data of move</param>
        /// <param name="searching">If true, does not add updated Zobrist key to KeySet. If false, updated Zobrist key
        /// gets added to KeySet.</param>
        public void MakeMove(DenseMove move, bool searching)
        {
            var movedPiece = move.GetPieceType();
            var movedColor = move.GetColor();
            int from = move.GetFrom();
            int to = move.GetTo();
            var capturedPiece = move.GetCaptPiece();
            bool isCastle = move.IsCastle();
            bool isEnPass = move.IsEnPassant();
            var promoPiece = move.GetPromotePiece();

            int prevCastleRights = CurrentGameState.GetCastleRights();

            // Captures / en passant
            if (capturedPiece != PieceType.EMPTY)
            {
                if (!isEnPass)
                {
                    RemovePiece(to, capturedPiece);
                }
                else
                {
                    int capturedPawnSquare = to + (movedColor == Color.WHITE ? -8 : 8);
                    RemovePiece(capturedPawnSquare, capturedPiece);
                }
            }
            else if (isCastle)
            {
                bool isKingside = (to > from);
                if (isKingside)
                {
                    int rookFrom = (movedPiece == PieceType.W_KING) ? 7 : 63;
                    int rookTo = (movedPiece == PieceType.W_KING) ? 5 : 61;
                    MovePiece(rookFrom, rookTo, (movedPiece == PieceType.W_KING) ? PieceType.W_ROOK : PieceType.B_ROOK);
                }
                else
                {
                    int rookFrom = (movedPiece == PieceType.W_KING) ? 0 : 56;
                    int rookTo = (movedPiece == PieceType.W_KING) ? 3 : 59;
                    MovePiece(rookFrom, rookTo, (movedPiece == PieceType.W_KING) ? PieceType.W_ROOK : PieceType.B_ROOK);
                }
            }

            // Promotion vs normal move
            if (promoPiece != PieceType.EMPTY)
            {
                RemovePiece(from, movedPiece);
                AddPiece(to, promoPiece);
            }
            else
            {
                MovePiece(from, to, movedPiece);
            }

            // Castling rights after move
            if (movedPiece == PieceType.W_KING)
            {
                CurrentGameState.CanCastleWhiteKingside = false;
                CurrentGameState.CanCastleWhiteQueenside = false;
            }
            else if (movedPiece == PieceType.B_KING)
            {
                CurrentGameState.CanCastleBlackKingside = false;
                CurrentGameState.CanCastleBlackQueenside = false;
            }
            if (from == 0 || to == 0) CurrentGameState.CanCastleWhiteQueenside = false;
            if (from == 7 || to == 7) CurrentGameState.CanCastleWhiteKingside = false;
            if (from == 56 || to == 56) CurrentGameState.CanCastleBlackQueenside = false;
            if (from == 63 || to == 63) CurrentGameState.CanCastleBlackKingside = false;

            // Update en passant (clear old EP Zobrist if needed)
            if (CurrentGameState.EnPassantSquare != -1)
            {
                ZobristKey ^= Zobrist.ZobristEnPass[CurrentGameState.GetEnPassantFileIndex()];
            }

            if (movedPiece != PieceType.W_PAWN && movedPiece != PieceType.B_PAWN)
            {
                CurrentGameState.EnPassantSquare = -1;
            }
            else
            {
                if (Math.Abs(to - from) == 16)
                {
                    int file = to % 8;
                    U64 adjacentFiles = 0;
                    if (file > 0) adjacentFiles |= BUTIL.FileMask << (file - 1);
                    if (file < 7) adjacentFiles |= BUTIL.FileMask << (file + 1);

                    U64 enemyPawns = (movedPiece == PieceType.W_PAWN) ? GetBlackPawns() : GetWhitePawns();
                    U64 enemyPawnsInPosition = enemyPawns & adjacentFiles &
                        ((movedPiece == PieceType.W_PAWN) ? BUTIL.Rank4 : BUTIL.Rank5);

                    if (enemyPawnsInPosition != 0UL)
                    {
                        CurrentGameState.EnPassantSquare = (from + to) / 2;
                        ZobristKey ^= Zobrist.ZobristEnPass[CurrentGameState.GetEnPassantFileIndex()];
                    }
                    else
                    {
                        CurrentGameState.EnPassantSquare = -1;
                    }
                }
                else
                {
                    CurrentGameState.EnPassantSquare = -1;
                }
            }

            // Switch side to move
            CurrentGameState.SideToMove = (CurrentGameState.SideToMove == Color.WHITE) ? Color.BLACK : Color.WHITE;

            // 50-move clock
            if (capturedPiece != PieceType.EMPTY || movedPiece == PieceType.W_PAWN || movedPiece == PieceType.B_PAWN)
                CurrentGameState.HalfMoveClock = 0;
            else
                CurrentGameState.HalfMoveClock++;

            // Fullmove number increments after black moves (i.e., when sideToMove flips back to white)
            if (CurrentGameState.SideToMove == Color.WHITE)
                CurrentGameState.FullMoveNumber++;

            // Zobrist updates
            ZobristKey ^= Zobrist.ZobristSideToMove;
            PlyIndex++;
            StateHistory[PlyIndex] = CurrentGameState;

            if (prevCastleRights != CurrentGameState.GetCastleRights())
            {
                ZobristKey ^= Zobrist.ZobristCastle[prevCastleRights];
                ZobristKey ^= Zobrist.ZobristCastle[CurrentGameState.GetCastleRights()];
            }

            if (!searching)
            {
                if (capturedPiece != PieceType.EMPTY) KeySet.Clear();
                KeySet.Add(ZobristKey);
            }
        }

        /// <summary>
        /// Unmakes a move on the board and updates the gamestate to previous state stored in StateHistory.
        /// </summary>
        /// <param name="move">The DenseMove data of the move</param>
        /// <param name="searching">If true, does not remove Zobrist key from KeySet. If false, Zobrist key
        /// gets removed from KeySet.</param>
        public void UnmakeMove(DenseMove move, bool searching)
        {
            // Remove current key if not searching
            if (!searching) KeySet.Remove(ZobristKey);

            int prevCastleRights = CurrentGameState.GetCastleRights();
            int prevEnPass = CurrentGameState.EnPassantSquare;

            // Restore previous state
            PlyIndex--;
            CurrentGameState = StateHistory[PlyIndex];

            var movedPiece = move.GetPieceType();
            var movedColor = move.GetColor();
            int movedFrom = move.GetFrom();
            int movedTo = move.GetTo();
            var undoCapturedPiece = move.GetCaptPiece();
            bool undoCastle = move.IsCastle();
            bool undoEnPass = move.IsEnPassant();
            var undoPromoPiece = move.GetPromotePiece();

            // Zobrist restore elements toggled in MakeMove tail
            if (prevCastleRights != CurrentGameState.GetCastleRights())
            {
                ZobristKey ^= Zobrist.ZobristCastle[prevCastleRights];
                ZobristKey ^= Zobrist.ZobristCastle[CurrentGameState.GetCastleRights()];
            }
            if (prevEnPass != -1)
                ZobristKey ^= Zobrist.ZobristEnPass[prevEnPass % 8];
            if (CurrentGameState.EnPassantSquare != -1)
                ZobristKey ^= Zobrist.ZobristEnPass[CurrentGameState.GetEnPassantFileIndex()];

            ZobristKey ^= Zobrist.ZobristSideToMove;

            // Move piece back, undo promotion if necessary
            if (undoPromoPiece != PieceType.EMPTY)
            {
                // Remove promoted piece at 'to', add pawn at 'from'
                RemovePiece(movedTo, undoPromoPiece);
                AddPiece(movedFrom, movedPiece);
            }
            else
            {
                MovePiece(movedTo, movedFrom, movedPiece); // reverse
            }

            // Undo castle rook move
            if (undoCastle)
            {
                bool kingside = (movedTo > movedFrom);
                if (kingside)
                {
                    int rookFrom = (movedPiece == PieceType.W_KING) ? 5 : 61;
                    int rookTo = (movedPiece == PieceType.W_KING) ? 7 : 63;
                    MovePiece(rookFrom, rookTo, (movedPiece == PieceType.W_KING) ? PieceType.W_ROOK : PieceType.B_ROOK);
                }
                else
                {
                    int rookFrom = (movedPiece == PieceType.W_KING) ? 3 : 59;
                    int rookTo = (movedPiece == PieceType.W_KING) ? 0 : 56;
                    MovePiece(rookFrom, rookTo, (movedPiece == PieceType.W_KING) ? PieceType.W_ROOK : PieceType.B_ROOK);
                }
            }

            // Restore captured piece
            if (undoCapturedPiece != PieceType.EMPTY)
            {
                if (!undoEnPass)
                {
                    AddPiece(movedTo, undoCapturedPiece);
                }
                else
                {
                    int capturedPawnSquare = movedTo + (movedColor == Color.WHITE ? -8 : 8);
                    AddPiece(capturedPawnSquare, undoCapturedPiece);
                }
            }
        }

        /// <summary>
        /// Generates the Zobrist key of current board by Zobrist hashing technique.
        /// </summary>
        /// <returns>Zobrist key of the current board and state</returns>
        public U64 GenerateZobristKey()
        {
            U64 key = 0UL;

            // pieces
            for (int sq = 0; sq < 64; sq++)
            {
                PieceType p = GetPieceAt(sq);
                if (p != PieceType.EMPTY && p != PieceType.INVALID)
                {
                    key ^= Zobrist.GetPieceSqKey(sq, p);
                }
            }
            // side
            if (CurrentGameState.SideToMove == Color.BLACK)
                key ^= Zobrist.ZobristSideToMove;

            // castling
            key ^= Zobrist.ZobristCastle[CurrentGameState.GetCastleRights()];

            // en passant
            if (CurrentGameState.EnPassantSquare != -1)
                key ^= Zobrist.ZobristEnPass[CurrentGameState.GetEnPassantFileIndex()];

            return key;
        }

        // =========================
        // Debug prints (optional)
        // =========================

        /// <summary>
        /// Prints bitboard param to console.
        /// </summary>
        /// <param name="bitb"></param>
        public static void PrintBB(U64 bitb)
        {
            var line = Convert.ToString((long)bitb, 2).PadLeft(64, '0');
            Console.WriteLine();
            for (int row = 0; row < 8; row++)
            {
                Console.WriteLine(line.Substring(row * 8, 8));
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Prints all bitboards to console.
        /// </summary>
        /// <param name="fullInfo"></param>
        public void PrintBitboards(bool fullInfo = true)
        {
            Console.WriteLine("White:");
            PrintBB(GetWhitePieces());
            Console.WriteLine("Black:");
            PrintBB(GetBlackPieces());
            if (fullInfo)
            {
                Console.WriteLine("Pawns:"); PrintBB(pieceBB[(int)DenseType.D_PAWN]);
                Console.WriteLine("Knights:"); PrintBB(pieceBB[(int)DenseType.D_KNIGHT]);
                Console.WriteLine("Bishops:"); PrintBB(pieceBB[(int)DenseType.D_BISHOP]);
                Console.WriteLine("Rooks:"); PrintBB(pieceBB[(int)DenseType.D_ROOK]);
                Console.WriteLine("Queens:"); PrintBB(pieceBB[(int)DenseType.D_QUEEN]);
                Console.WriteLine("Kings:"); PrintBB(pieceBB[(int)DenseType.D_KING]);
            }
        }

        /// <summary>
        /// Prints StateHistory to console.
        /// </summary>
        public void PrintStateHistory()
        {
            for (int i = 0; i <= PlyIndex; i++)
            {
                Console.WriteLine($"{i}: {StateHistory[i]}");
            }
        }

        // =========================
        // Private helpers
        // =========================

        /// <summary>Initialize white pieces to standard starting position.</summary>
        private void InitializeWhiteBB() => colorBB[(int)Color.WHITE] = 0x000000000000FFFFUL;
        /// <summary>Initialize black pieces to standard starting position.</summary>
        private void InitializeBlackBB() => colorBB[(int)Color.BLACK] = 0xFFFF000000000000UL;
        /// <summary>Initialize empty squares to standard starting position.</summary>
        private void InitializeEmptyBB() => pieceBB[(int)DenseType.D_EMPTY] = 0x0000FFFFFFFF0000UL;
        /// <summary>Initialize pawn pieces to standard starting position.</summary>
        private void InitializePawnsBB() => pieceBB[(int)DenseType.D_PAWN] = 0x00FF00000000FF00UL;
        /// <summary>Initialize knight pieces to standard starting position.</summary>
        private void InitializeKnightsBB() => pieceBB[(int)DenseType.D_KNIGHT] = 0x4200000000000042UL;
        /// <summary>Initialize bishop pieces to standard starting position.</summary>
        private void InitializeBishopsBB() => pieceBB[(int)DenseType.D_BISHOP] = 0x2400000000000024UL;
        /// <summary>Initialize rook pieces to standard starting position.</summary>
        private void InitializeRooksBB() => pieceBB[(int)DenseType.D_ROOK] = 0x8100000000000081UL;
        /// <summary>Initialize queen pieces to standard starting position.</summary>
        private void InitializeQueensBB() => pieceBB[(int)DenseType.D_QUEEN] = 0x0800000000000008UL;
        /// <summary>Initialize king pieces to standard starting position.</summary>
        private void InitializeKingsBB()
        {
            pieceBB[(int)DenseType.D_KING] = 0x1000000000000010UL;
            kingSquares[(int)Color.WHITE] = 4;
            kingSquares[(int)Color.BLACK] = 60;
        }

        /// <summary>Initialize game state to standard starting position, clear KeySet and add initial Zobrist key.</summary>
        private void InitializeGameState()
        {
            CurrentGameState = new GameState(true);
            PlyIndex = 0;
            StateHistory[0] = CurrentGameState;
            if (!Zobrist.Initialized) Zobrist.Initialize();
            ZobristKey = GenerateZobristKey();
            KeySet.Clear();
            KeySet.TrimExcess();
            KeySet.Add(ZobristKey);
        }

        /// <summary>
        /// Helper to move a piece from one square to another. Updates bitboards and Zobrist key.
        /// </summary>
        /// <param name="from">Starting square</param>
        /// <param name="to">Ending square</param>
        /// <param name="piece">Piece's type</param>
        private void MovePiece(int from, int to, PieceType piece)
        {
            U64 fromToBB = (1UL << from) | (1UL << to);
            int col = ColorCode(piece);
            int type = PieceCode(piece);

            colorBB[col] ^= fromToBB;
            pieceBB[type] ^= fromToBB;

            pieceBB[(int)DenseType.D_EMPTY] = ~(colorBB[(int)Color.WHITE] | colorBB[(int)Color.BLACK]);

            ZobristKey ^= Zobrist.GetPieceSqKey(from, piece);
            ZobristKey ^= Zobrist.GetPieceSqKey(to, piece);

            if (piece == PieceType.W_KING) kingSquares[(int)Color.WHITE] = to;
            else if (piece == PieceType.B_KING) kingSquares[(int)Color.BLACK] = to;
        }

        /// <summary>
        /// Helper to remove a piece from a square. Updates bitboards and Zobrist key.
        /// </summary>
        /// <param name="square">Starting square</param>
        /// <param name="piece">Piece's type</param>
        private void RemovePiece(int square, PieceType piece)
        {
            U64 clear = ~(1UL << square);
            int col = ColorCode(piece);
            int type = PieceCode(piece);

            colorBB[col] &= clear;
            pieceBB[type] &= clear;
            pieceBB[(int)DenseType.D_EMPTY] |= (1UL << square);

            ZobristKey ^= Zobrist.GetPieceSqKey(square, piece);
        }

        /// <summary>
        /// Helper to add a piece to a square. Updates bitboards and Zobrist key.
        /// </summary>
        /// <param name="square">Starting square</param>
        /// <param name="piece">Piece's type</param>
        private void AddPiece(int square, PieceType piece)
        {
            U64 bb = 1UL << square;
            int col = ColorCode(piece);
            int type = PieceCode(piece);

            colorBB[col] |= bb;
            pieceBB[type] |= bb;
            pieceBB[(int)DenseType.D_EMPTY] &= ~bb;

            ZobristKey ^= Zobrist.GetPieceSqKey(square, piece);
        }

        // =========================
        // Small utilities
        // =========================

        /// <summary>
        /// Get the DenseType piece of given PieceType.
        /// </summary>
        /// <param name="ps"></param>
        /// <returns>DenseType integer</returns>
        private static int PieceCode(PieceType ps) => ((int)ps & 7);
        /// <summary>
        /// Get the color of given PieceType
        /// </summary>
        /// <param name="ps"></param>
        /// <returns>Color integer</returns>
        private static int ColorCode(PieceType ps) => (((int)ps >> 3) & 1);

        /// <summary>
        /// Helper for parsing ints in FEN string.
        /// </summary>
        /// <param name="s"></param>
        /// <returns>Integer if possible, 0 if not</returns>
        private static int ParseInt(string s)
        {
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)) return v;
            return 0;
        }

        /// <summary>
        /// Decodes algebraic square (a1, c8, etc) into square index.
        /// </summary>
        /// <param name="sq">Algebraic square; must be between a1 and h8.</param>
        /// <returns>Square's index if valid, -1 if invalid.</returns>
        private static int AlgebraicToIndex(string sq)
        {
            if (sq.Length != 2) return -1;
            int file = char.ToLowerInvariant(sq[0]) - 'a';
            int rank = sq[1] - '1';
            if (file < 0 || file > 7 || rank < 0 || rank > 7) return -1;
            return rank * 8 + file;
        }

        /// <summary>
        /// Turns square index (0 to 63) into algebraic square.
        /// </summary>
        /// <param name="index">Index, must be between 0 and 63.</param>
        /// <returns>String of algebraic square.</returns>
        private static string IndexToAlgebraic(int index)
        {
            if (index < 0 || index > 63) return "??";
            char file = (char)('a' + (index % 8));
            char rank = (char)('1' + (index / 8));
            return new string(new[] { file, rank });
        }

        /// <summary>
        /// Alias for BitOperations.TrailingZeroCount
        /// </summary>
        /// <param name="bb"></param>
        /// <returns></returns>
        private static int LsbIndex(U64 bb) => BitOperations.TrailingZeroCount(bb);
    }

    // ===== Helper maps (FEN <-> Piece) =====
    internal static class UtilityMaps
    {
        public static readonly Dictionary<char, PieceType> FENToPiece = new()
        {
            { 'P', PieceType.W_PAWN }, { 'N', PieceType.W_KNIGHT }, { 'B', PieceType.W_BISHOP },
            { 'R', PieceType.W_ROOK }, { 'Q', PieceType.W_QUEEN }, { 'K', PieceType.W_KING },
            { 'p', PieceType.B_PAWN }, { 'n', PieceType.B_KNIGHT }, { 'b', PieceType.B_BISHOP },
            { 'r', PieceType.B_ROOK }, { 'q', PieceType.B_QUEEN }, { 'k', PieceType.B_KING }
        };

        public static readonly Dictionary<PieceType, string> PieceToFEN = new()
        {
            { PieceType.W_PAWN, "P" }, { PieceType.W_KNIGHT, "N" }, { PieceType.W_BISHOP, "B" },
            { PieceType.W_ROOK, "R" }, { PieceType.W_QUEEN, "Q" }, { PieceType.W_KING, "K" },
            { PieceType.B_PAWN, "p" }, { PieceType.B_KNIGHT, "n" }, { PieceType.B_BISHOP, "b" },
            { PieceType.B_ROOK, "r" }, { PieceType.B_QUEEN, "q" }, { PieceType.B_KING, "k" },
        };
    }

}
