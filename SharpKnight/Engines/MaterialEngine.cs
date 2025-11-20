using SharpKnight.Core;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics.X86;

namespace SharpKnight.Engines
{
    /// <summary>
    /// A toy engine that only considers material (and check/mate) when deciding moves.
    /// </summary>
    public sealed class MaterialEngine : ChessEngineBase
    {
        const int INF_POS = 999999999;
        const int INF_NEG = -999999999;

        // Search statistics
        U64 NodeCount;          // Number of nodes searched
        Stopwatch Stopwatch;
        DenseMove CurrentMove;
        int CurrentMoveNumber;
        int TotalPiecesExcPawns;

        // Piece values (in centipawns)
        const int PAWN_VALUE = 100;
        const int KNIGHT_VALUE = 320;
        const int BISHOP_VALUE = 330;
        const int ROOK_VALUE = 500;
        const int QUEEN_VALUE = 900;
        const int KING_VALUE = 2000;

        //
        const int MATE_BONUS = 100000;
        const int CHECKING_BONUS = 1500;
        const int CHECKED_PENALTY = -1500;

        public MaterialEngine()
            : base(name: "MaterialEngine", version: "1.0", author: "Cameron Cunningham", defaultDepth: 5)
        {

        }

        public override DenseMove FindBestMove(ref ChessBoard board, ref ChessClock clock, int maxDepth = -1)
        {
            StartSearch();
            Stopwatch = Stopwatch.StartNew();
            NodeCount = 0;
            CurrentMoveNumber = 0;

            int actualDepth = (maxDepth > 0) ? maxDepth : SearchDepth;

            // Generate all legal moves
            int moveNum = 0;
            DenseMove[] moves = MoveGenerator.GenerateLegalMoves(board, ref moveNum);
            Color sideToMove = board.GetSideToMove();

            // Initialize alpha-beta search
            int alpha = INF_NEG;
            int beta = INF_POS;
            // bestScore will determine what the best move to play is.
            // A negative bestScore means the position for the opponent is better, positive means player position is better
            int bestScore = INF_NEG;
            DenseMove bestMove = moves[0];

            // Evaluate each move
            for (int i = 0; i < moveNum; i++)
            {
                CurrentMoveNumber = i;
                CurrentMove = moves[i];

                // Send current move info
                // TODO

                // Make move
                board.MakeMove(moves[i], true);

                int score = AlphaBeta(ref board, actualDepth - 1, alpha, beta);

                // Update best move if better score was found
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = moves[i];

                    if (score > alpha) alpha = score;
                }

                // Add nodes/second if meaningful time has elapsed
                // TODO
                board.UnmakeMove(moves[i], true);
            }

            EndSearch();
            SetBestMove(bestMove);
            return BestMove;
        }

        // Alpha-Beta Search
        int AlphaBeta(ref ChessBoard board, int depth, int alpha, int beta)
        {
            if (depth == 0 || !isSearching)
            {
                return EvaluatePosition(board);
            }

            int moveNum = 0;
            DenseMove[] moves = MoveGenerator.GeneratePsuedoMoves(board, ref moveNum);

            bool noLegalMoves = true;

            // Test every move in the position
            int bestScore = INF_NEG;
            for (int i = 0; i < moveNum; i++)
            {
                board.MakeMove(moves[i], true);

                // Check move legality
                if (board.IsSideInCheck(board.GetSideToMove()))
                {
                    board.UnmakeMove(moves[i], true);
                    continue;
                }

                noLegalMoves = false;

                int score = -AlphaBeta(ref board, depth - 1, -beta, -alpha);

                board.UnmakeMove(moves[i], true);

                if (score > bestScore)
                {
                    bestScore = score;
                    if (score > alpha)
                    {
                        alpha = score;
                    }
                }
                if (score >= beta)
                {
                    return bestScore;
                }
            }
            
            if (noLegalMoves)
            {
                return board.IsSideInCheck(board.GetSideToMove()) ? MATE_BONUS : 0;
            }
            return alpha;
        }

        public override int EvaluatePosition(in ChessBoard board)
        {
            NodeCount++;

            int score = 0;

            // Material count
            score += CountMaterial(board, Color.WHITE);
            score -= CountMaterial(board, Color.BLACK);

            return score;
        }

        private int CountMaterial(ChessBoard board, Color color)
        {
            int score = 0;
            U64 pieces;

            if (color == Color.WHITE)
            {
                score += (int)Popcnt.X64.PopCount(board.GetWhitePawns()) * PAWN_VALUE;
                score += (int)Popcnt.X64.PopCount(board.GetWhiteKnights()) * KNIGHT_VALUE;
                score += (int)Popcnt.X64.PopCount(board.GetWhiteBishops()) * BISHOP_VALUE;
                score += (int)Popcnt.X64.PopCount(board.GetWhiteRooks()) * ROOK_VALUE;
                score += (int)Popcnt.X64.PopCount(board.GetWhiteQueens()) * QUEEN_VALUE;
                score += (int)Popcnt.X64.PopCount(board.GetWhiteKings()) * KING_VALUE;
            }
            else
            {
                score += (int)Popcnt.X64.PopCount(board.GetBlackPawns()) * PAWN_VALUE;
                score += (int)Popcnt.X64.PopCount(board.GetBlackKnights()) * KNIGHT_VALUE;
                score += (int)Popcnt.X64.PopCount(board.GetBlackBishops()) * BISHOP_VALUE;
                score += (int)Popcnt.X64.PopCount(board.GetBlackRooks()) * ROOK_VALUE;
                score += (int)Popcnt.X64.PopCount(board.GetBlackQueens()) * QUEEN_VALUE;
                score += (int)Popcnt.X64.PopCount(board.GetBlackKings()) * KING_VALUE;
            }
            return score;
        }
    }
}
