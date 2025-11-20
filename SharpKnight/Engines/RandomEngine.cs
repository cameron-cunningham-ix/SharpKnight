using SharpKnight.Core;
using System;

namespace SharpKnight.Engines
{
    /// <summary>
    /// A toy engine that picks a random legal move.
    /// </summary>
    public sealed class RandomEngine : ChessEngineBase
    {
        private readonly Random _rng;

        public RandomEngine()
            // Only needs depth 1
            : base(name: "RandomEngine", version: "1.0", author: "Cameron Cunningham", defaultDepth: 1)
        {
            _rng = new Random(); // Time-seeded
        }

        public override DenseMove FindBestMove(ref ChessBoard board, ref ChessClock clock, int maxDepth = -1)
        {
            StartSearch();

            int moveNum = 0; 
            var moves = MoveGenerator.GenerateLegalMoves(board, ref moveNum);

            if (moveNum > 0)
            {
                int idx = _rng.Next(moveNum);
                SetBestMove(moves[idx]);
            }
            else
            {
                // No legal moves (checkmate/stalemate), BestMove remains default
                SetBestMove(default);
            }

            EndSearch();
            return BestMove;
        }

        public override int EvaluatePosition(in ChessBoard board)
        {
            // Random engine doesn't evaluate positions
            return 0;
        }
    }
}
