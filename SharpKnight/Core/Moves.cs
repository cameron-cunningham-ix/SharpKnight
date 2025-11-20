using System;
using System.Numerics; // BitOperations
using static SharpKnight.Consts;
using static SharpKnight.PieceType;
using static SharpKnight.DenseType;

namespace SharpKnight.Core
{
    /// <summary>
    /// Static class for generating all moves on a chess board.
    /// </summary>
    public static class MoveGenerator
    {
        // -------------------------
        // Public entry points
        // -------------------------
        /// <summary>
        /// Generates all psuedo-legal captures on the board for the current player to move.
        /// </summary>
        /// <param name="board"></param>
        /// <param name="moveNum"></param>
        /// <returns></returns>
        public static DenseMove[] GenerateCaptureMoves(ChessBoard board, ref int moveNum)
        {
            var moves = new DenseMove[MAX_MOVES];
            ulong pieceBB;
            var sideToMove = board.GetSideToMove();
            ulong occupancy = board.GetAllPieces();

            if (sideToMove == Color.WHITE)
            {
                ulong opposition = board.GetBlackPieces();
                ulong attackMask, attacks;

                // Pawn captures (with promotions on rank 8)
                ulong whitePawns = board.GetWhitePawns();
                while (whitePawns != 0UL)
                {
                    int index = BitOperations.TrailingZeroCount(whitePawns);
                    attackMask = AttackMasks.WPawn[index] & opposition;
                    while (attackMask != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(attackMask);
                        if (to >= 56 && to < 64)
                        {
                            var pm = DenseMove.FromPieceType(W_PAWN, index, to, board.GetDenseTypeAt(to), false, false, D_QUEEN);
                            moves[moveNum++] = pm;
                            pm.SetPromoteTo(D_ROOK);   moves[moveNum++] = pm;
                            pm.SetPromoteTo(D_BISHOP); moves[moveNum++] = pm;
                            pm.SetPromoteTo(D_KNIGHT); moves[moveNum++] = pm;
                        }
                        else
                        {
                            moves[moveNum++] = DenseMove.FromPieceType(W_PAWN, index, to, board.GetDenseTypeAt(to));
                        }
                        attackMask &= (attackMask - 1);
                    }
                    whitePawns &= (whitePawns - 1);
                }

                // Knights
                pieceBB = board.GetWhiteKnights();
                while (pieceBB != 0UL)
                {
                    int from = BitOperations.TrailingZeroCount(pieceBB);
                    attacks = AttackMasks.Knight[from] & opposition;
                    while (attacks != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(attacks);
                        moves[moveNum++] = DenseMove.FromPieceType(W_KNIGHT, from, to, board.GetDenseTypeAt(to));
                        attacks &= (attacks - 1);
                    }
                    pieceBB &= (pieceBB - 1);
                }

                // Bishops
                pieceBB = board.GetWhiteBishops();
                while (pieceBB != 0UL)
                {
                    int from = BitOperations.TrailingZeroCount(pieceBB);
                    attacks = AttackMasks.BishopWithEdges[from] & opposition;
                    ulong blocked = PEXT.GetBishopAttacks(from, occupancy);
                    attacks &= blocked;
                    while (attacks != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(attacks);
                        moves[moveNum++] = DenseMove.FromPieceType(W_BISHOP, from, to, board.GetDenseTypeAt(to));
                        attacks &= (attacks - 1);
                    }
                    pieceBB &= (pieceBB - 1);
                }

                // Rooks
                pieceBB = board.GetWhiteRooks();
                while (pieceBB != 0UL)
                {
                    int from = BitOperations.TrailingZeroCount(pieceBB);
                    attacks = AttackMasks.RookWithEdges[from] & opposition;
                    ulong blocked = PEXT.GetRookAttacks(from, occupancy);
                    attacks &= blocked;
                    while (attacks != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(attacks);
                        moves[moveNum++] = DenseMove.FromPieceType(W_ROOK, from, to, board.GetDenseTypeAt(to));
                        attacks &= (attacks - 1);
                    }
                    pieceBB &= (pieceBB - 1);
                }

                // Queens
                pieceBB = board.GetWhiteQueens();
                while (pieceBB != 0UL)
                {
                    int from = BitOperations.TrailingZeroCount(pieceBB);
                    attacks = AttackMasks.QueenWithEdges[from] & opposition;
                    ulong blocked = PEXT.GetBishopAttacks(from, occupancy) | PEXT.GetRookAttacks(from, occupancy);
                    attacks &= blocked;
                    while (attacks != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(attacks);
                        moves[moveNum++] = DenseMove.FromPieceType(W_QUEEN, from, to, board.GetDenseTypeAt(to));
                        attacks &= (attacks - 1);
                    }
                    pieceBB &= (pieceBB - 1);
                }

                // King
                pieceBB = board.GetWhiteKings();
                while (pieceBB != 0UL)
                {
                    int from = BitOperations.TrailingZeroCount(pieceBB);
                    attacks = AttackMasks.King[from] & opposition;
                    while (attacks != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(attacks);
                        moves[moveNum++] = DenseMove.FromPieceType(W_KING, from, to, board.GetDenseTypeAt(to));
                        attacks &= (attacks - 1);
                    }
                    pieceBB &= (pieceBB - 1);
                }

                // En passant captures are also captures
                GenerateEnPassantMoves(board, moves, ref moveNum);
            }
            else
            {
                ulong opposition = board.GetWhitePieces();
                ulong attackMask, attacks;

                // Pawn captures (with promotions on rank 1)
                ulong blackPawns = board.GetBlackPawns();
                while (blackPawns != 0UL)
                {
                    int index = BitOperations.TrailingZeroCount(blackPawns);
                    attackMask = AttackMasks.BPawn[index] & opposition;
                    while (attackMask != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(attackMask);
                        if (to >= 0 && to < 8)
                        {
                            var pm = DenseMove.FromPieceType(B_PAWN, index, to, board.GetDenseTypeAt(to), false, false, D_QUEEN);
                            moves[moveNum++] = pm;
                            pm.SetPromoteTo(D_ROOK);   moves[moveNum++] = pm;
                            pm.SetPromoteTo(D_BISHOP); moves[moveNum++] = pm;
                            pm.SetPromoteTo(D_KNIGHT); moves[moveNum++] = pm;
                        }
                        else
                        {
                            moves[moveNum++] = DenseMove.FromPieceType(B_PAWN, index, to, board.GetDenseTypeAt(to));
                        }
                        attackMask &= (attackMask - 1);
                    }
                    blackPawns &= (blackPawns - 1);
                }

                // Knights
                pieceBB = board.GetBlackKnights();
                while (pieceBB != 0UL)
                {
                    int from = BitOperations.TrailingZeroCount(pieceBB);
                    attacks = AttackMasks.Knight[from] & opposition;
                    while (attacks != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(attacks);
                        moves[moveNum++] = DenseMove.FromPieceType(B_KNIGHT, from, to, board.GetDenseTypeAt(to));
                        attacks &= (attacks - 1);
                    }
                    pieceBB &= (pieceBB - 1);
                }

                // Bishops
                pieceBB = board.GetBlackBishops();
                while (pieceBB != 0UL)
                {
                    int from = BitOperations.TrailingZeroCount(pieceBB);
                    attacks = AttackMasks.BishopWithEdges[from] & opposition;
                    ulong blocked = PEXT.GetBishopAttacks(from, occupancy);
                    attacks &= blocked;
                    while (attacks != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(attacks);
                        moves[moveNum++] = DenseMove.FromPieceType(B_BISHOP, from, to, board.GetDenseTypeAt(to));
                        attacks &= (attacks - 1);
                    }
                    pieceBB &= (pieceBB - 1);
                }

                // Rooks
                pieceBB = board.GetBlackRooks();
                while (pieceBB != 0UL)
                {
                    int from = BitOperations.TrailingZeroCount(pieceBB);
                    attacks = AttackMasks.RookWithEdges[from] & opposition;
                    ulong blocked = PEXT.GetRookAttacks(from, occupancy);
                    attacks &= blocked;
                    while (attacks != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(attacks);
                        moves[moveNum++] = DenseMove.FromPieceType(B_ROOK, from, to, board.GetDenseTypeAt(to));
                        attacks &= (attacks - 1);
                    }
                    pieceBB &= (pieceBB - 1);
                }

                // Queens
                pieceBB = board.GetBlackQueens();
                while (pieceBB != 0UL)
                {
                    int from = BitOperations.TrailingZeroCount(pieceBB);
                    attacks = AttackMasks.QueenWithEdges[from] & opposition;
                    ulong blocked = PEXT.GetBishopAttacks(from, occupancy) | PEXT.GetRookAttacks(from, occupancy);
                    attacks &= blocked;
                    while (attacks != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(attacks);
                        moves[moveNum++] = DenseMove.FromPieceType(B_QUEEN, from, to, board.GetDenseTypeAt(to));
                        attacks &= (attacks - 1);
                    }
                    pieceBB &= (pieceBB - 1);
                }

                // King
                pieceBB = board.GetBlackKings();
                while (pieceBB != 0UL)
                {
                    int from = BitOperations.TrailingZeroCount(pieceBB);
                    attacks = AttackMasks.King[from] & opposition;
                    while (attacks != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(attacks);
                        moves[moveNum++] = DenseMove.FromPieceType(B_KING, from, to, board.GetDenseTypeAt(to));
                        attacks &= (attacks - 1);
                    }
                    pieceBB &= (pieceBB - 1);
                }

                // En passant captures
                GenerateEnPassantMoves(board, moves, ref moveNum);
            }

            return moves;
        }

        /// <summary>
        /// Generates all psuedo-legal moves on the board for the player to move.
        /// </summary>
        /// <param name="board"></param>
        /// <param name="moveNum"></param>
        /// <returns></returns>
        public static DenseMove[] GeneratePsuedoMoves(ChessBoard board, ref int moveNum)
        {
            var moves = new DenseMove[MAX_MOVES];
            GeneratePieceMoves(board, moves, ref moveNum);
            GeneratePawnMoves(board, moves, ref moveNum);
            GenerateEnPassantMoves(board, moves, ref moveNum);
            GenerateCastlingMoves(board, moves, ref moveNum);
            return moves;
        }

        /// <summary>
        /// Generates all legal moves on the board for the player to move.
        /// </summary>
        /// <param name="board"></param>
        /// <param name="moveNum"></param>
        /// <returns></returns>
        public static DenseMove[] GenerateLegalMoves(ChessBoard board, ref int moveNum)
        {
            moveNum = 0;
            var legal = GeneratePsuedoMoves(board, ref moveNum);
            var sideToMove = board.GetSideToMove();

            for (int i = 0; i < moveNum;)
            {
                board.MakeMove(legal[i], true);
                if (board.IsSideInCheck(sideToMove))
                {
                    Console.WriteLine("check: " + legal[i].ToString());
                    board.UnmakeMove(legal[i], true);
                    legal[i] = legal[--moveNum]; // remove by swap-pop
                    continue;
                }
                board.UnmakeMove(legal[i], true);
                i++;
            }

            if (moveNum == 0)
            {
                for (int i = 0; i < legal.Length; i++) legal[i] = new DenseMove();
            }
            return legal;
        }

        // -------------------------
        // Private helpers
        // -------------------------
        /// <summary>
        /// Generates all psuedo-legal pawn moves and inserts them into 'moves' array.
        /// </summary>
        /// <param name="board"></param>
        /// <param name="moves"></param>
        /// <param name="moveNum"></param>
        private static void GeneratePawnMoves(ChessBoard board, DenseMove[] moves, ref int moveNum)
        {
            ulong empty = board.GetEmptySquares();
            var sideToMove = board.GetSideToMove();

            if (sideToMove == Color.WHITE)
            {
                // Captures
                ulong whitePawns = board.GetWhitePawns();
                while (whitePawns != 0UL)
                {
                    int from = BitOperations.TrailingZeroCount(whitePawns);
                    ulong mask = AttackMasks.WPawn[from] & board.GetBlackPieces();
                    while (mask != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(mask);
                        if (to >= 56 && to < 64)
                        {
                            var pm = DenseMove.FromPieceType(W_PAWN, from, to, board.GetDenseTypeAt(to), false, false, D_QUEEN);
                            moves[moveNum++] = pm;
                            pm.SetPromoteTo(D_ROOK);   moves[moveNum++] = pm;
                            pm.SetPromoteTo(D_BISHOP); moves[moveNum++] = pm;
                            pm.SetPromoteTo(D_KNIGHT); moves[moveNum++] = pm;
                        }
                        else
                        {
                            moves[moveNum++] = DenseMove.FromPieceType(W_PAWN, from, to, board.GetDenseTypeAt(to));
                        }
                        mask &= (mask - 1);
                    }
                    whitePawns &= (whitePawns - 1);
                }

                // Single pushes
                ulong single = (board.GetWhitePawns() << 8) & empty;
                // Double pushes from rank 2 -> rank 4 (mask 0x0000000000FF0000 after first shift)
                ulong dbl = ((single & 0x0000000000FF0000UL) << 8) & empty;

                while (single != 0UL)
                {
                    int to = BitOperations.TrailingZeroCount(single);
                    if (to >= 56 && to < 64)
                    {
                        var pm = DenseMove.FromPieceType(W_PAWN, to - 8, to, D_EMPTY, false, false, D_QUEEN);
                        moves[moveNum++] = pm;
                        pm.SetPromoteTo(D_ROOK);   moves[moveNum++] = pm;
                        pm.SetPromoteTo(D_BISHOP); moves[moveNum++] = pm;
                        pm.SetPromoteTo(D_KNIGHT); moves[moveNum++] = pm;
                    }
                    else
                    {
                        moves[moveNum++] = DenseMove.FromPieceType(W_PAWN, to - 8, to);
                    }
                    single &= (single - 1);
                }

                while (dbl != 0UL)
                {
                    int to = BitOperations.TrailingZeroCount(dbl);
                    moves[moveNum++] = DenseMove.FromPieceType(W_PAWN, to - 16, to);
                    dbl &= (dbl - 1);
                }
            }
            else
            {
                // Captures
                ulong blackPawns = board.GetBlackPawns();
                while (blackPawns != 0UL)
                {
                    int from = BitOperations.TrailingZeroCount(blackPawns);
                    ulong mask = AttackMasks.BPawn[from] & board.GetWhitePieces();
                    while (mask != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(mask);
                        if (to >= 0 && to < 8)
                        {
                            var pm = DenseMove.FromPieceType(B_PAWN, from, to, board.GetDenseTypeAt(to), false, false, D_QUEEN);
                            moves[moveNum++] = pm;
                            pm.SetPromoteTo(D_ROOK);   moves[moveNum++] = pm;
                            pm.SetPromoteTo(D_BISHOP); moves[moveNum++] = pm;
                            pm.SetPromoteTo(D_KNIGHT); moves[moveNum++] = pm;
                        }
                        else
                        {
                            moves[moveNum++] = DenseMove.FromPieceType(B_PAWN, from, to, board.GetDenseTypeAt(to));
                        }
                        mask &= (mask - 1);
                    }
                    blackPawns &= (blackPawns - 1);
                }

                // Single pushes (downwards)
                ulong single = (board.GetBlackPawns() >> 8) & empty;
                // Double pushes from rank 7 -> rank 5 (mask 0x0000FF0000000000 after first shift)
                ulong dbl = ((single & 0x0000FF0000000000UL) >> 8) & empty;

                while (single != 0UL)
                {
                    int to = BitOperations.TrailingZeroCount(single);
                    if (to >= 0 && to < 8)
                    {
                        var pm = DenseMove.FromPieceType(B_PAWN, to + 8, to, D_EMPTY, false, false, D_QUEEN);
                        moves[moveNum++] = pm;
                        pm.SetPromoteTo(D_ROOK);   moves[moveNum++] = pm;
                        pm.SetPromoteTo(D_BISHOP); moves[moveNum++] = pm;
                        pm.SetPromoteTo(D_KNIGHT); moves[moveNum++] = pm;
                    }
                    else
                    {
                        moves[moveNum++] = DenseMove.FromPieceType(B_PAWN, to + 8, to);
                    }
                    single &= (single - 1);
                }

                while (dbl != 0UL)
                {
                    int to = BitOperations.TrailingZeroCount(dbl);
                    moves[moveNum++] = DenseMove.FromPieceType(B_PAWN, to + 16, to);
                    dbl &= (dbl - 1);
                }
            }
        }

        /// <summary>
        /// Generates all psuedo-legal en passant moves and inserts them into 'moves' array.
        /// </summary>
        /// <param name="board"></param>
        /// <param name="moves"></param>
        /// <param name="moveNum"></param>
        private static void GenerateEnPassantMoves(ChessBoard board, DenseMove[] moves, ref int moveNum)
        {
            int ep = board.CurrentGameState.EnPassantSquare;
            if (ep == -1) return;
            var sideToMove = board.CurrentGameState.SideToMove;

            if (sideToMove == Color.WHITE)
            {
                ulong epCaptors = AttackMasks.BPawn[ep] & board.GetWhitePawns();
                while (epCaptors != 0UL)
                {
                    int from = BitOperations.TrailingZeroCount(epCaptors);
                    moves[moveNum++] = DenseMove.FromPieceType(W_PAWN, from, ep, D_PAWN, false, true);
                    epCaptors &= (epCaptors - 1);
                }
            }
            else
            {
                ulong epCaptors = AttackMasks.WPawn[ep] & board.GetBlackPawns();
                while (epCaptors != 0UL)
                {
                    int from = BitOperations.TrailingZeroCount(epCaptors);
                    moves[moveNum++] = DenseMove.FromPieceType(B_PAWN, from, ep, D_PAWN, false, true);
                    epCaptors &= (epCaptors - 1);
                }
            }
        }

        /// <summary>
        /// Generates all psuedo-legal castling moves and inserts them into 'moves' array.
        /// </summary>
        /// <param name="board"></param>
        /// <param name="moves"></param>
        /// <param name="moveNum"></param>
        private static void GenerateCastlingMoves(ChessBoard board, DenseMove[] moves, ref int moveNum)
        {
            var sideToMove = board.GetSideToMove();
            // Kings cannot castle out of check
            ulong enemyAttacks = board.OppAttacksToSquare(sideToMove == Color.WHITE ? 4 : 60, sideToMove);
            if (enemyAttacks != 0UL) return;

            ulong occ = board.GetAllPieces();
            if (sideToMove == Color.WHITE)
            {
                if (board.CurrentGameState.CanCastleWhiteKingside &&
                    (occ & BUTIL.W_ShortCastleMask) == 0UL &&
                    board.OppAttacksToSquare(5, Color.WHITE) == 0UL)
                {
                    moves[moveNum++] = DenseMove.FromPieceType(W_KING, 4, 6, D_EMPTY, true);
                }
                if (board.CurrentGameState.CanCastleWhiteQueenside &&
                    (occ & BUTIL.W_LongCastleMask) == 0UL &&
                    board.OppAttacksToSquare(3, Color.WHITE) == 0UL)
                {
                    moves[moveNum++] = DenseMove.FromPieceType(W_KING, 4, 2, D_EMPTY, true);
                }
            }
            else
            {
                if (board.CurrentGameState.CanCastleBlackKingside &&
                    (occ & BUTIL.B_ShortCastleMask) == 0UL &&
                    board.OppAttacksToSquare(61, Color.BLACK) == 0UL)
                {
                    moves[moveNum++] = DenseMove.FromPieceType(B_KING, 60, 62, D_EMPTY, true);
                }
                if (board.CurrentGameState.CanCastleBlackQueenside &&
                    (occ & BUTIL.B_LongCastleMask) == 0UL &&
                    board.OppAttacksToSquare(59, Color.BLACK) == 0UL)
                {
                    moves[moveNum++] = DenseMove.FromPieceType(B_KING, 60, 58, D_EMPTY, true);
                }
            }
        }

        /// <summary>
        /// Generates all psuedo-legal piece moves and inserts them into 'moves' array.
        /// </summary>
        /// <param name="board"></param>
        /// <param name="moves"></param>
        /// <param name="moveNum"></param>
        private static void GeneratePieceMoves(ChessBoard board, DenseMove[] moves, ref int moveNum)
        {
            ulong pieceBB;
            var sideToMove = board.GetSideToMove();
            ulong occ = board.GetAllPieces();
            ulong empty = board.GetEmptySquares();

            if (sideToMove == Color.WHITE)
            {
                ulong opposition = board.GetBlackPieces();
                ulong attackMask, attacks, freeSpace;

                // Queens
                pieceBB = board.GetWhiteQueens();
                while (pieceBB != 0UL)
                {
                    int from = BitOperations.TrailingZeroCount(pieceBB);
                    attackMask = AttackMasks.QueenWithEdges[from];
                    attacks = attackMask & opposition;
                    freeSpace = attackMask & empty;
                    ulong blocked = PEXT.GetBishopAttacks(from, occ) | PEXT.GetRookAttacks(from, occ);
                    attacks &= blocked; freeSpace &= blocked;
                    while (attacks != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(attacks);
                        moves[moveNum++] = DenseMove.FromPieceType(W_QUEEN, from, to, board.GetDenseTypeAt(to));
                        attacks &= (attacks - 1);
                    }
                    while (freeSpace != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(freeSpace);
                        moves[moveNum++] = DenseMove.FromPieceType(W_QUEEN, from, to);
                        freeSpace &= (freeSpace - 1);
                    }
                    pieceBB &= (pieceBB - 1);
                }

                // Rooks
                pieceBB = board.GetWhiteRooks();
                while (pieceBB != 0UL)
                {
                    int from = BitOperations.TrailingZeroCount(pieceBB);
                    attackMask = AttackMasks.RookWithEdges[from];
                    attacks = attackMask & opposition;
                    freeSpace = attackMask & empty;
                    ulong blocked = PEXT.GetRookAttacks(from, occ);
                    attacks &= blocked; freeSpace &= blocked;
                    while (attacks != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(attacks);
                        moves[moveNum++] = DenseMove.FromPieceType(W_ROOK, from, to, board.GetDenseTypeAt(to));
                        attacks &= (attacks - 1);
                    }
                    while (freeSpace != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(freeSpace);
                        moves[moveNum++] = DenseMove.FromPieceType(W_ROOK, from, to);
                        freeSpace &= (freeSpace - 1);
                    }
                    pieceBB &= (pieceBB - 1);
                }

                // Knights
                pieceBB = board.GetWhiteKnights();
                while (pieceBB != 0UL)
                {
                    int from = BitOperations.TrailingZeroCount(pieceBB);
                    attackMask = AttackMasks.Knight[from];
                    attacks = attackMask & opposition;
                    freeSpace = attackMask & empty;
                    while (attacks != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(attacks);
                        moves[moveNum++] = DenseMove.FromPieceType(W_KNIGHT, from, to, board.GetDenseTypeAt(to));
                        attacks &= (attacks - 1);
                    }
                    while (freeSpace != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(freeSpace);
                        moves[moveNum++] = DenseMove.FromPieceType(W_KNIGHT, from, to);
                        freeSpace &= (freeSpace - 1);
                    }
                    pieceBB &= (pieceBB - 1);
                }

                // Bishops
                pieceBB = board.GetWhiteBishops();
                while (pieceBB != 0UL)
                {
                    int from = BitOperations.TrailingZeroCount(pieceBB);
                    attackMask = AttackMasks.BishopWithEdges[from];
                    attacks = attackMask & opposition;
                    freeSpace = attackMask & empty;
                    ulong blocked = PEXT.GetBishopAttacks(from, occ);
                    attacks &= blocked; freeSpace &= blocked;
                    while (attacks != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(attacks);
                        moves[moveNum++] = DenseMove.FromPieceType(W_BISHOP, from, to, board.GetDenseTypeAt(to));
                        attacks &= (attacks - 1);
                    }
                    while (freeSpace != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(freeSpace);
                        moves[moveNum++] = DenseMove.FromPieceType(W_BISHOP, from, to);
                        freeSpace &= (freeSpace - 1);
                    }
                    pieceBB &= (pieceBB - 1);
                }

                // King
                pieceBB = board.GetWhiteKings();
                while (pieceBB != 0UL)
                {
                    int from = BitOperations.TrailingZeroCount(pieceBB);
                    attackMask = AttackMasks.King[from];
                    attacks = attackMask & opposition;
                    ulong free = attackMask & empty;
                    while (attacks != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(attacks);
                        moves[moveNum++] = DenseMove.FromPieceType(W_KING, from, to, board.GetDenseTypeAt(to));
                        attacks &= (attacks - 1);
                    }
                    while (free != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(free);
                        moves[moveNum++] = DenseMove.FromPieceType(W_KING, from, to);
                        free &= (free - 1);
                    }
                    pieceBB &= (pieceBB - 1);
                }
            }
            else
            {
                ulong opposition = board.GetWhitePieces();
                ulong attackMask, attacks, freeSpace;

                // Queens
                pieceBB = board.GetBlackQueens();
                while (pieceBB != 0UL)
                {
                    int from = BitOperations.TrailingZeroCount(pieceBB);
                    attackMask = AttackMasks.QueenWithEdges[from];
                    attacks = attackMask & opposition;
                    freeSpace = attackMask & empty;
                    ulong blocked = PEXT.GetBishopAttacks(from, occ) | PEXT.GetRookAttacks(from, occ);
                    attacks &= blocked; freeSpace &= blocked;
                    while (attacks != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(attacks);
                        moves[moveNum++] = DenseMove.FromPieceType(B_QUEEN, from, to, board.GetDenseTypeAt(to));
                        attacks &= (attacks - 1);
                    }
                    while (freeSpace != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(freeSpace);
                        moves[moveNum++] = DenseMove.FromPieceType(B_QUEEN, from, to);
                        freeSpace &= (freeSpace - 1);
                    }
                    pieceBB &= (pieceBB - 1);
                }

                // Rooks
                pieceBB = board.GetBlackRooks();
                while (pieceBB != 0UL)
                {
                    int from = BitOperations.TrailingZeroCount(pieceBB);
                    attackMask = AttackMasks.RookWithEdges[from];
                    attacks = attackMask & opposition;
                    freeSpace = attackMask & empty;
                    ulong blocked = PEXT.GetRookAttacks(from, occ);
                    attacks &= blocked; freeSpace &= blocked;
                    while (attacks != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(attacks);
                        moves[moveNum++] = DenseMove.FromPieceType(B_ROOK, from, to, board.GetDenseTypeAt(to));
                        attacks &= (attacks - 1);
                    }
                    while (freeSpace != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(freeSpace);
                        moves[moveNum++] = DenseMove.FromPieceType(B_ROOK, from, to);
                        freeSpace &= (freeSpace - 1);
                    }
                    pieceBB &= (pieceBB - 1);
                }

                // Knights
                pieceBB = board.GetBlackKnights();
                while (pieceBB != 0UL)
                {
                    int from = BitOperations.TrailingZeroCount(pieceBB);
                    attackMask = AttackMasks.Knight[from];
                    attacks = attackMask & opposition;
                    freeSpace = attackMask & empty;
                    while (attacks != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(attacks);
                        moves[moveNum++] = DenseMove.FromPieceType(B_KNIGHT, from, to, board.GetDenseTypeAt(to));
                        attacks &= (attacks - 1);
                    }
                    while (freeSpace != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(freeSpace);
                        moves[moveNum++] = DenseMove.FromPieceType(B_KNIGHT, from, to);
                        freeSpace &= (freeSpace - 1);
                    }
                    pieceBB &= (pieceBB - 1);
                }

                // Bishops
                pieceBB = board.GetBlackBishops();
                while (pieceBB != 0UL)
                {
                    int from = BitOperations.TrailingZeroCount(pieceBB);
                    attackMask = AttackMasks.BishopWithEdges[from];
                    attacks = attackMask & opposition;
                    freeSpace = attackMask & empty;
                    ulong blocked = PEXT.GetBishopAttacks(from, occ);
                    attacks &= blocked; freeSpace &= blocked;
                    while (attacks != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(attacks);
                        moves[moveNum++] = DenseMove.FromPieceType(B_BISHOP, from, to, board.GetDenseTypeAt(to));
                        attacks &= (attacks - 1);
                    }
                    while (freeSpace != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(freeSpace);
                        moves[moveNum++] = DenseMove.FromPieceType(B_BISHOP, from, to);
                        freeSpace &= (freeSpace - 1);
                    }
                    pieceBB &= (pieceBB - 1);
                }

                // King
                pieceBB = board.GetBlackKings();
                while (pieceBB != 0UL)
                {
                    int from = BitOperations.TrailingZeroCount(pieceBB);
                    attackMask = AttackMasks.King[from];
                    attacks = attackMask & opposition;
                    freeSpace = attackMask & empty;
                    while (attacks != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(attacks);
                        moves[moveNum++] = DenseMove.FromPieceType(B_KING, from, to, board.GetDenseTypeAt(to));
                        attacks &= (attacks - 1);
                    }
                    while (freeSpace != 0UL)
                    {
                        int to = BitOperations.TrailingZeroCount(freeSpace);
                        moves[moveNum++] = DenseMove.FromPieceType(B_KING, from, to);
                        freeSpace &= (freeSpace - 1);
                    }
                    pieceBB &= (pieceBB - 1);
                }
            }
        }
    }
}
