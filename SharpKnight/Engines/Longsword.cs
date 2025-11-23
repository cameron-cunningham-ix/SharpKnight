using SharpKnight;
using SharpKnight.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace SharpKnight.Engines
{
    public sealed class LongswordEngine : ChessEngineBase
    {
        // ===== Constants =====
        private const int INF_POS = 999_999_999;
        private const int INF_NEG = -999_999_999;

        private const int TT_SIZE = 1 << 22; // ~4,194,304 entries (~96MB)

        // Killer-move storage per ply
        private const int KM_PLY = 64;

        // History heuristic bounds
        private const int HISTORY_MAX = 65_536;

        // Initial total count of non-pawn, non-king pieces for game-phase blending
        private const int INIT_MAJ_MIN_PIECES = 14;

        // ===== Search State =====
        private ulong nodeCount;
        private Stopwatch stopwatch = new();
        private TimeSpan prevDepthTime = TimeSpan.Zero;
        private DenseMove currentMove;
        private int currentMoveNumber;

        private readonly TTEntry[] transpositionTable = new TTEntry[TT_SIZE];
        private readonly DenseMove[] killerMoves = new DenseMove[KM_PLY * 2];
        private readonly int[,,] historyTable = new int[2, 64, 64]; // [Color][From][To]

        private int totalPiecesWithoutPawns;
        private float earlygameLerp;
        private float endgameLerp;

        // ===== Evaluation Parameters (UCI-exposed) =====
        private class Parameters
        {
            public int pawnValue = 100;
            public int knightValue = 320;
            public int bishopValue = 330;
            public int rookValue = 500;
            public int queenValue = 900;
            public int kingValue = 2000;

            public int mateScore = 100000;
            public int restrictKingBonus = 10;
            public int kingShieldBonus = 50;
            public int airyKingPenalty = -10;
            public int supportedPawnBonus = 90;
            public int supportingPawnBonus = 15;
            public int passedPawnBonus = 30;
            public int supportingPieceBonus = 15;
            public int doubledPawnPenalty = -50;
            public int isolatedPawnPenalty = -80;
            public int checkedPenalty = -300;
            public int checkingBonus = 300;
            public int bishopPairBonus = 150;
            public int rookOpenFileBonus = 250;
        }

        private readonly Parameters @params = new();

        public LongswordEngine()
            : base(
                name: "Longsword",
                version: "1.0",
                author: "Cameron Cunningham",
                defaultDepth: 8,
                minTime: TimeSpan.FromMilliseconds(200),
                maxTime: TimeSpan.FromMilliseconds(20_000))
        {
            // Register UCI options (Spin)
            RegisterOption(EngineOption.CreateSpin("PawnValue", 100, 10, 300));
            RegisterOption(EngineOption.CreateSpin("KnightValue", 320, 100, 500));
            RegisterOption(EngineOption.CreateSpin("BishopValue", 330, 110, 510));
            RegisterOption(EngineOption.CreateSpin("RookValue", 500, 500, 800));
            RegisterOption(EngineOption.CreateSpin("QueenValue", 900, 810, 1500));
            RegisterOption(EngineOption.CreateSpin("KingValue", 2000, 2000, 5000));

            RegisterOption(EngineOption.CreateSpin("MateScore", 100000, 50000, 200000));
            RegisterOption(EngineOption.CreateSpin("RestrictKingBonus", 10, 1, 100));
            RegisterOption(EngineOption.CreateSpin("KingShieldBonus", 50, 1, 300));
            RegisterOption(EngineOption.CreateSpin("AiryKingPenalty", -10, -100, -1));
            RegisterOption(EngineOption.CreateSpin("SupportedPawnBonus", 90, 1, 200));
            RegisterOption(EngineOption.CreateSpin("SupportingPawnBonus", 15, 1, 200));
            RegisterOption(EngineOption.CreateSpin("PassedPawnBonus", 30, 1, 200));
            RegisterOption(EngineOption.CreateSpin("SupportingPieceBonus", 15, 1, 200));
            RegisterOption(EngineOption.CreateSpin("DoubledPawnPenalty", -50, -200, -1));
            RegisterOption(EngineOption.CreateSpin("IsolatedPawnPenalty", -80, -200, -1));
            RegisterOption(EngineOption.CreateSpin("CheckedPenalty", -300, -5000, -1));
            RegisterOption(EngineOption.CreateSpin("CheckingBonus", 300, 1, 5000));
            RegisterOption(EngineOption.CreateSpin("BishopPairBonus", 150, 1, 300));
            RegisterOption(EngineOption.CreateSpin("RookOpenFileBonus", 250, 1, 500));
        }

        public override void ClearForNewGame()
        {
            Array.Clear(historyTable, 0, historyTable.Length);
            Array.Fill(killerMoves, default);
        }

        public override void OnOptionChanged(in EngineOption option)
        {
            if (option.CurrentValue is not int value) return;
            switch (option.Name)
            {
                case "PawnValue": @params.pawnValue = value; break;
                case "KnightValue": @params.knightValue = value; break;
                case "BishopValue": @params.bishopValue = value; break;
                case "RookValue": @params.rookValue = value; break;
                case "QueenValue": @params.queenValue = value; break;
                case "KingValue": @params.kingValue = value; break;

                case "MateScore": @params.mateScore = value; break;
                case "RestrictKingBonus": @params.restrictKingBonus = value; break;
                case "KingShieldBonus": @params.kingShieldBonus = value; break;
                case "AiryKingPenalty": @params.airyKingPenalty = value; break;
                case "SupportedPawnBonus": @params.supportedPawnBonus = value; break;
                case "SupportingPawnBonus": @params.supportingPawnBonus = value; break;
                case "PassedPawnBonus": @params.passedPawnBonus = value; break;
                case "SupportingPieceBonus": @params.supportingPieceBonus = value; break;
                case "DoubledPawnPenalty": @params.doubledPawnPenalty = value; break;
                case "IsolatedPawnPenalty": @params.isolatedPawnPenalty = value; break;
                case "CheckedPenalty": @params.checkedPenalty = value; break;
                case "CheckingBonus": @params.checkingBonus = value; break;
                case "BishopPairBonus": @params.bishopPairBonus = value; break;
                case "RookOpenFileBonus": @params.rookOpenFileBonus = value; break;
            }
        }

        // ===== Public Search Entry =====
        public override DenseMove FindBestMove(ref ChessBoard board, ref ChessClock clock, int maxDepth = -1)
        {
            StartSearch();
            stopwatch.Restart();
            prevDepthTime = TimeSpan.Zero;

            nodeCount = 0;
            currentMoveNumber = 0;

            int actualDepth = (maxDepth > 0) ? maxDepth : SearchDepth;
            DenseMove bestMoveOverall = default;
            int bestScoreOverall = 0;
            const int MIN_DEPTH = 2;

            // Generate legal root moves
            int moveNum = 0;
            var moves = MoveGenerator.GenerateLegalMoves(board, ref moveNum);

            for (int currDepth = MIN_DEPTH; currDepth <= actualDepth && isSearching; currDepth++)
            {
                nodeCount = 0;
                currentMoveNumber = 0;
                //clock.UpdateTime();

                if (currDepth > MIN_DEPTH && !KeepSearching(clock))
                    break;

                // PV move from TT (hash move)
                DenseMove hashMove = default;
                ref var tt = ref transpositionTable[board.ZobristKey % TT_SIZE];
                if (tt.Key == board.ZobristKey)
                    hashMove = tt.BestMove;

                int alpha = INF_NEG;
                int beta = INF_POS;
                DenseMove bestMove = default;
                int bestScore = INF_NEG;
                bool firstMove = true;

                OrderMoves(moves, moveNum, ply: 0, hashMove);

                for (int i = 0; i < moveNum; i++)
                {
                    currentMoveNumber = i + 1;
                    currentMove = moves[i];

                    SendInfo($"currmove {currentMove.ToAlgebraic()} currmovenumber {currentMoveNumber}");

                    board.MakeMove(moves[i], searching: true);
                    int score;

                    if (firstMove)
                    {
                        score = -AlphaBeta(board, currDepth - 1, -beta, -alpha, ply: 1, isPV: true);
                        firstMove = false;
                    }
                    else
                    {
                        // PVS narrow search
                        score = -AlphaBeta(board, currDepth - 1, -alpha - 1, -alpha, ply: 1, isPV: false);
                        if (score > alpha && score < beta)
                        {
                            score = -AlphaBeta(board, currDepth - 1, -beta, -alpha, ply: 1, isPV: true);
                        }
                    }

                    board.UnmakeMove(moves[i], searching: true);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = moves[i];
                        if (score > alpha) alpha = score;

                        var ms = stopwatch.ElapsedMilliseconds;
                        var info = $"depth {currDepth} score cp {bestScore} time {ms} nodes {nodeCount} {bestMove.ToAlgebraic()}";
                        if (ms > 0)
                        {
                            var nps = (nodeCount * 1000UL) / (ulong)ms;
                            info += $" nps {nps}";
                        }
                        SendInfo(info);
                    }
                }

                RecordTTEntry(board, bestMove, currDepth, bestScore, TTEntry.EXACT);
                bestMoveOverall = bestMove;
                bestScoreOverall = bestScore;

                var total = stopwatch.Elapsed;
                var iterInfo = $"Completed depth {currDepth} in {total.TotalMilliseconds:F0}ms, score: {bestScore}, nodes: {nodeCount}";
                if (total.TotalMilliseconds > 0)
                {
                    var nps = (ulong)(nodeCount * 1000.0 / total.TotalMilliseconds);
                    iterInfo += $", nps: {nps}";
                }
                SendInfo(iterInfo);

                prevDepthTime = total;
            }

            SetBestMove(bestMoveOverall);
            EndSearch();
            return bestMoveOverall;
        }

        // ===== Alpha-Beta & Quiescence =====
        private int AlphaBeta(ChessBoard board, int depth, int alpha, int beta, int ply, bool isPV)
        {
            if (depth <= 0 || !isSearching)
                return Quiescence(board, alpha, beta);

            // Draw by repetition or 50-move rule
            if (ply > 0 && (board.CurrentGameState.HalfMoveClock >= 100 || board.KeySet.Contains(board.ZobristKey)))
                return 0;

            // Transposition table probe
            if (CheckTT(board, depth, alpha, beta, out int ttScore, out var hashMove))
                return ttScore;

            int moveNum = 0;
            int quietNum = 0;
            var moves = MoveGenerator.GeneratePsuedoMoves(board, ref moveNum);
            var quietsSearched = new DenseMove[Math.Max(1, moveNum)];
            OrderMoves(moves, moveNum, ply, hashMove);

            bool noLegalMoves = true;
            var bestMove = default(DenseMove);
            int flag = TTEntry.ALPHA;
            var sideToMove = board.GetSideToMove();
            bool firstMove = true;

            for (int i = 0; i < moveNum; i++)
            {
                board.MakeMove(moves[i], searching: true);

                // Illegal (leaves king in check)
                if (board.IsSideInCheck(sideToMove))
                {
                    board.UnmakeMove(moves[i], searching: true);
                    continue;
                }
                noLegalMoves = false;

                int eval;
                if (firstMove)
                {
                    eval = -AlphaBeta(board, depth - 1, -beta, -alpha, ply + 1, isPV);
                    firstMove = false;
                }
                else
                {
                    // Late Move Reduction
                    int reduction = 0;
                    if (depth >= 3 && !moves[i].IsCapture() && !board.IsSideInCheck(sideToMove)
                        && !board.IsSideInCheck((Color)(1 - (int)sideToMove))
                        && !moves[i].Equals(killerMoves[ply]) && !moves[i].Equals(killerMoves[ply + 1]))
                    {
                        reduction = Math.Min(2, depth / 2);
                    }

                    eval = -AlphaBeta(board, depth - 1 - reduction, -alpha - 1, -alpha, ply + 1, false);
                    if (eval > alpha && eval < beta)
                        eval = -AlphaBeta(board, depth - 1, -beta, -alpha, ply + 1, true);
                }

                board.UnmakeMove(moves[i], searching: true);

                // Beta cutoff
                if (eval >= beta)
                {
                    if (!moves[i].IsCapture())
                    {
                        // Update killer moves
                        if (!moves[i].Equals(killerMoves[ply]))
                        {
                            killerMoves[ply + 1] = killerMoves[ply];
                            killerMoves[ply] = moves[i];
                        }
                        // History update for quiets that cause beta cutoffs
                        int bonus = CalculateHistBonus(depth);
                        var color = moves[i].GetColor();
                        int from = moves[i].GetFrom();
                        int to = moves[i].GetTo();
                        HistoryTableUpdate(color, from, to, bonus);

                        // Penalize other quiets searched this node
                        for (int j = 0; j < quietNum; j++)
                        {
                            if (quietsSearched[j].Equals(moves[i])) continue;
                            var q = quietsSearched[j];
                            HistoryTableUpdate(q.GetColor(), q.GetFrom(), q.GetTo(), -bonus);
                        }
                    }
                    flag = TTEntry.BETA;
                    RecordTTEntry(board, moves[i], depth, beta, flag);
                    return beta;
                }

                if (eval > alpha)
                {
                    alpha = eval;
                    bestMove = moves[i];
                    flag = TTEntry.EXACT;
                }

                if (!moves[i].IsCapture())
                {
                    quietsSearched[quietNum] = moves[i];
                    quietNum++;
                }
            }

            if (noLegalMoves)
                return board.IsSideInCheck(sideToMove) ? -@params.mateScore + ply : 0;

            RecordTTEntry(board, bestMove, depth, alpha, flag);
            return alpha;
        }

        private int Quiescence(ChessBoard board, int alpha, int beta)
        {
            nodeCount++;

            int standPat = EvaluatePosition(board);
            if (standPat >= beta) return beta;
            if (standPat > alpha) alpha = standPat;

            var sideToMove = board.GetSideToMove();
            int moveNum = 0;
            var captureMoves = MoveGenerator.GenerateCaptureMoves(board, ref moveNum);
            OrderCaptures(captureMoves, moveNum);

            for (int i = 0; i < moveNum; i++)
            {
                board.MakeMove(captureMoves[i], searching: true);

                // SEE pruning: skip bad captures
                if (StaticExchangeEvaluation(board, captureMoves[i].GetTo(), sideToMove) < 0)
                {
                    board.UnmakeMove(captureMoves[i], searching: true);
                    continue;
                }

                if (board.IsSideInCheck(sideToMove))
                {
                    board.UnmakeMove(captureMoves[i], searching: true);
                    continue;
                }

                int score = -Quiescence(board, -beta, -alpha);
                board.UnmakeMove(captureMoves[i], searching: true);

                if (score >= beta) return beta;
                if (score > alpha) alpha = score;
            }

            return alpha;
        }

        // Static Exchange Evaluation (SEE)
        private int StaticExchangeEvaluation(ChessBoard board, int square, Color sideToMove)
        {
            ulong attackers = board.OppAttacksToSquare(square, sideToMove);
            if (attackers == 0) return 0;

            var victim = board.GetPieceAt(square);
            int gain = GetPieceValue(victim);

            ulong mutableAttackers = attackers;
            int depth = 0;
            Span<int> exch = stackalloc int[32];
            exch[depth++] = gain;

            var currentTurn = sideToMove;
            while (mutableAttackers != 0)
            {
                int attackerSq = BitOperations.TrailingZeroCount(mutableAttackers);
                mutableAttackers &= mutableAttackers - 1; // remove LS1B

                var attacker = board.GetPieceAt(attackerSq);
                int attackerVal = GetPieceValue(attacker);
                gain = attackerVal - gain;
                if (gain < 0) break;
                exch[depth++] = gain;
                currentTurn = currentTurn == Color.WHITE ? Color.BLACK : Color.WHITE;
            }

            while (--depth > 0)
                exch[depth - 1] = Math.Min(-exch[depth], exch[depth - 1]);

            return exch[0];
        }

        private bool KeepSearching(ChessClock clock)
        {
            if (clock.IsInfinite()) return true;
            var remaining = clock.GetActiveColor() == Color.WHITE ? clock.GetWhiteTime() : clock.GetBlackTime();
            var estimatedNextDepth = prevDepthTime * 10; // conservative (can spike ~10x)
            var buffer = TimeSpan.FromMilliseconds(20);
            var maxForMove = TimeSpan.FromTicks(remaining.Ticks / 10); // <=10% of clock
            return estimatedNextDepth + buffer < maxForMove;
        }

        // ===== Evaluation =====
        public override int EvaluatePosition(in ChessBoard board)
        {
            nodeCount++;

            // Count major/minor (no pawns/kings) for game-phase lerp
            ulong majorMinor = board.GetAllPieces() & ~(board.GetDenseSet(DenseType.D_PAWN) | board.GetDenseSet(DenseType.D_KING));
            totalPiecesWithoutPawns = Popcount(majorMinor);

            earlygameLerp = (float)totalPiecesWithoutPawns / INIT_MAJ_MIN_PIECES;
            endgameLerp = Math.Clamp(8 - totalPiecesWithoutPawns, 0, 8) / (float)INIT_MAJ_MIN_PIECES;

            int score = 0;
            score += CountMaterial(board);
            score += EvaluatePositional(board, Color.WHITE);
            score -= EvaluatePositional(board, Color.BLACK);

            return board.GetSideToMove() == Color.WHITE ? score : -score;
        }

        private int CountMaterial(ChessBoard board)
        {
            int score = 0;
            ulong pieces;

            pieces = board.GetWhitePawns(); score += Popcount(pieces) * @params.pawnValue;
            pieces = board.GetWhiteKnights(); score += Popcount(pieces) * @params.knightValue;
            pieces = board.GetWhiteBishops(); score += Popcount(pieces) * @params.bishopValue;
            pieces = board.GetWhiteRooks(); score += Popcount(pieces) * @params.rookValue;
            pieces = board.GetWhiteQueens(); score += Popcount(pieces) * @params.queenValue;

            pieces = board.GetBlackPawns(); score -= Popcount(pieces) * @params.pawnValue;
            pieces = board.GetBlackKnights(); score -= Popcount(pieces) * @params.knightValue;
            pieces = board.GetBlackBishops(); score -= Popcount(pieces) * @params.bishopValue;
            pieces = board.GetBlackRooks(); score -= Popcount(pieces) * @params.rookValue;
            pieces = board.GetBlackQueens(); score -= Popcount(pieces) * @params.queenValue;

            return score;
        }

        private int EvaluatePositional(ChessBoard board, Color color)
        {
            int score = 0;
            ulong pawns, pawnRef, knights, bishops, rooks, queens;
            int kingSquare;
            int flip; // 0 for WHITE, 56 for BLACK (XOR flip)
            ulong occupancy = board.GetAllPieces();

            if (color == Color.WHITE)
            {
                flip = 0;
                pawns = board.GetWhitePawns(); pawnRef = pawns;
                knights = board.GetWhiteKnights();
                bishops = board.GetWhiteBishops();
                rooks = board.GetWhiteRooks();
                queens = board.GetWhiteQueens();
                kingSquare = board.GetWhiteKingSquare();
            }
            else
            {
                flip = 56;
                pawns = board.GetBlackPawns(); pawnRef = pawns;
                knights = board.GetBlackKnights();
                bishops = board.GetBlackBishops();
                rooks = board.GetBlackRooks();
                queens = board.GetBlackQueens();
                kingSquare = board.GetBlackKingSquare() ^ flip;
            }

            // === Pawns ===
            ulong tmp = pawns;
            while (tmp != 0)
            {
                int sq = Tzcnt(tmp) ^ flip;
                score += (int)(PST.PawnEarly[sq] * earlygameLerp + PST.PawnEnd[sq] * endgameLerp);
                int realSq = sq ^ flip;

                int file = BUTIL.IndexToFile(realSq);
                ulong fileMask = BUTIL.FileMask << file;
                if (Popcount(fileMask & pawnRef) > 1) score += @params.doubledPawnPenalty;

                ulong adjacent = 0UL;
                if (file > 0) adjacent |= BUTIL.FileMask << (file - 1);
                if (file < 7) adjacent |= BUTIL.FileMask << (file + 1);
                if ((adjacent & pawnRef) == 0) score += @params.isolatedPawnPenalty;

                ulong enemyPawns = (color == Color.WHITE) ? board.GetBlackPawns() : board.GetWhitePawns();
                adjacent |= BUTIL.FileMask << file; // include own file for passed pawn test
                if ((adjacent & enemyPawns) == 0) score += @params.passedPawnBonus;

                // Supporting pawns
                ulong supports = (color == Color.WHITE) ? (AttackMasks.WPawn[realSq] & pawnRef)
                                                        : (AttackMasks.BPawn[realSq] & pawnRef);
                if (supports != 0) score += @params.supportingPawnBonus * Popcount(supports);

                tmp &= tmp - 1;
            }

            // === Knights ===
            tmp = knights;
            while (tmp != 0)
            {
                int sq = Tzcnt(tmp) ^ flip;
                score += (int)(PST.KnightEarly[sq] * earlygameLerp + PST.KnightEnd[sq] * endgameLerp);
                tmp &= tmp - 1;
            }

            // === Bishops ===
            if (Popcount(bishops) >= 2)
            {
                if (((BUTIL.LightSquareMask & bishops) != 0) && ((BUTIL.DarkSquareMask & bishops) != 0))
                    score += @params.bishopPairBonus;
            }
            tmp = bishops;
            while (tmp != 0)
            {
                int sq = Tzcnt(tmp) ^ flip;
                score += (int)(PST.BishopEarly[sq] * earlygameLerp + PST.BishopEnd[sq] * endgameLerp);
                tmp &= tmp - 1;
            }

            // === Rooks ===
            tmp = rooks;
            while (tmp != 0)
            {
                int sq = Tzcnt(tmp) ^ flip;
                score += (int)(PST.RookEarly[sq] * earlygameLerp + PST.RookEnd[sq] * endgameLerp);
                int realSq = sq ^ flip;
                int file = BUTIL.IndexToFile(realSq);
                if (((BUTIL.FileMask << file) & occupancy) == 0UL)
                    score += @params.rookOpenFileBonus;
                tmp &= tmp - 1;
            }

            // === Queens ===
            tmp = queens;
            while (tmp != 0)
            {
                int sq = Tzcnt(tmp) ^ flip;
                score += (int)(PST.QueenEarly[sq] * earlygameLerp + PST.QueenEnd[sq] * endgameLerp);
                tmp &= tmp - 1;
            }

            // === King ===
            score += (int)(PST.KingEarly[kingSquare] * earlygameLerp + PST.KingEnd[kingSquare] * endgameLerp);
            int kingReal = kingSquare ^ flip;
            ulong kingShield = AttackMasks.King[kingReal] & pawnRef;
            if (Popcount(kingShield) >= 2 && (kingReal < 8 || kingReal >= 56))
                score += @params.kingShieldBonus;

            return score;
        }

        // ===== Move Ordering =====
        private readonly struct ScoredMove : IComparable<ScoredMove>
        {
            public readonly DenseMove Move;
            public readonly int Score;
            public ScoredMove(DenseMove m, int s) { Move = m; Score = s; }
            public int CompareTo(ScoredMove other) => other.Score.CompareTo(Score); // desc
        }

        private const int HASH_MOVE_SCORE = 1_000_000;
        private const int CAPTURE_BASE_SCORE = 100_000;
        private const int KILLER_MOVE_SCORE = 90_000;
        private const int PRIORITY_SCORE = 10_000;

        private void OrderMoves(DenseMove[] moves, int moveCount, int ply, in DenseMove hashMove)
        {
            var scored = new ScoredMove[moveCount];
            for (int i = 0; i < moveCount; i++)
            {
                int score = 0;
                if (moves[i].Equals(hashMove))
                {
                    score = HASH_MOVE_SCORE;
                }
                else if (moves[i].IsCapture())
                {
                    score = CAPTURE_BASE_SCORE +
                            GetPieceValue(moves[i].GetCaptPiece()) * 10 -
                            GetPieceValue(moves[i].GetPieceType());
                }
                else
                {
                    if (moves[i].Equals(killerMoves[ply])) score = KILLER_MOVE_SCORE;
                    else if (moves[i].Equals(killerMoves[ply + 1])) score = KILLER_MOVE_SCORE - PRIORITY_SCORE;

                    if (!moves[i].Equals(hashMove))
                    {
                        int color = (int)moves[i].GetColor();
                        int from = moves[i].GetFrom();
                        int to = moves[i].GetTo();
                        score = historyTable[color, from, to];
                    }
                }
                scored[i] = new ScoredMove(moves[i], score);
            }
            Array.Sort(scored);
            for (int i = 0; i < moveCount; i++) moves[i] = scored[i].Move;
        }

        private void OrderCaptures(DenseMove[] moves, int moveCount)
        {
            var scored = new ScoredMove[moveCount];
            for (int i = 0; i < moveCount; i++)
            {
                var m = moves[i];
                int score = m.IsCapture() ? GetPieceValue(m.GetCaptPiece()) * 10 - GetPieceValue(m.GetPieceType()) : 0;
                scored[i] = new ScoredMove(m, score);
            }
            Array.Sort(scored);
            for (int i = 0; i < moveCount; i++) moves[i] = scored[i].Move;
        }

        // ===== Transposition Table =====
        private void RecordTTEntry(ChessBoard board, DenseMove best, int depth, int score, int flag)
        {
            ref var entry = ref transpositionTable[board.ZobristKey % TT_SIZE];
            entry.Key = board.ZobristKey;
            entry.BestMove = best;
            entry.Depth = depth;
            entry.Score = score;
            entry.Flag = flag;
        }

        private bool CheckTT(ChessBoard board, int depth, int alpha, int beta, out int score, out DenseMove hashMove)
        {
            ref var entry = ref transpositionTable[board.ZobristKey % TT_SIZE];
            if (entry.Key == board.ZobristKey && entry.Depth >= depth)
            {
                hashMove = entry.BestMove;
                if (entry.Flag == TTEntry.EXACT) { score = entry.Score; return true; }
                if (entry.Flag == TTEntry.ALPHA && entry.Score <= alpha) { score = alpha; return true; }
                if (entry.Flag == TTEntry.BETA && entry.Score >= beta) { score = beta; return true; }
            }
            score = 0; hashMove = default; return false;
        }

        // ===== History Heuristic =====
        private void HistoryTableUpdate(Color sideToMove, int from, int to, int bonus)
        {
            int c = (int)sideToMove;
            int clamped = Math.Clamp(bonus, -HISTORY_MAX, HISTORY_MAX);
            int current = historyTable[c, from, to];
            historyTable[c, from, to] = current + clamped - (current * Math.Abs(clamped) / HISTORY_MAX);
        }

        private static int CalculateHistBonus(int depth) => depth * depth;

        // ===== Helpers =====
        private void SendInfo(string info) => Console.WriteLine($"info {info}");

        private int GetPieceValue(PieceType piece)
        {
            switch (Utility.GetDenseType(piece))
            {
                case (int)DenseType.D_PAWN: return @params.pawnValue;
                case (int)DenseType.D_KNIGHT: return @params.knightValue;
                case (int)DenseType.D_BISHOP: return @params.bishopValue;
                case (int)DenseType.D_ROOK: return @params.rookValue;
                case (int)DenseType.D_QUEEN: return @params.queenValue;
                case (int)DenseType.D_KING: return @params.kingValue;
                default: return 0;
            }
        }

        private static int Popcount(ulong x) => BitOperations.PopCount(x);
        private static int Tzcnt(ulong x) => BitOperations.TrailingZeroCount(x);

        // ===== TT Entry =====
        private struct TTEntry
        {
            public const int ALPHA = 0;
            public const int BETA = 1;
            public const int EXACT = 2;

            public ulong Key;
            public DenseMove BestMove;
            public int Depth;
            public int Score;
            public int Flag;
        }

        // ===== Piece-Square Tables (PST) =====
        private static class PST
        {
            public static readonly int[] PawnEarly = {
                0,  0,  0,  0,  0,  0,  0,  0,
               50, 50, 50, 50, 50, 50, 50, 50,
               20, 20, 25, 25, 25, 25, 20, 20,
                5,  5, 10, 20, 20, 10,  5,  5,
                0,  0,  0, 25, 25,  0,  0,  0,
                5, -5,-10,  0,  0,-10, -5,  5,
               15, 25, 15,-10,-10, 15, 25, 15,
                0,  0,  0,  0,  0,  0,  0,  0
            };
            public static readonly int[] PawnEnd = {
                0,  0,  0,  0,  0,  0,  0,  0,
              120,120,120,120,120,120,120,120,
               80, 80, 80, 80, 80, 80, 80, 80,
               50, 50, 50, 50, 50, 50, 50, 50,
               30, 30, 30, 30, 30, 30, 30, 30,
               10, 10, 10, 10, 10, 10, 10, 10,
                5,  5,  5,  5,  5,  5,  5,  5,
                0,  0,  0,  0,  0,  0,  0,  0
            };
            public static readonly int[] KnightEarly = {
                 -50,-40,-30,-30,-30,-30,-40,-50,
                 -40,-20,  0,  0,  0,  0,-20,-40,
                 -30,  0, 10, 15, 15, 10,  0,-30,
                 -30,  5, 15, 15, 15, 15,  5,-30,
                 -30,  0, 15, 15, 15, 15,  0,-30,
                 -30,  5, 10, 15, 15, 10,  5,-30,
                 -40,-20,  0,  5,  5,  0,-20,-40,
                 -50,-40,-30,-30,-30,-30,-40,-50
            };
            public static readonly int[] KnightEnd = {
                -40,-30,-20,-20,-20,-20,-30,-40,
                -30,-20,  0,  0,  0,  0,-20,-30,
                -20,  0, 10, 15, 15, 10,  0,-20,
                -20,  5, 15, 20, 20, 15,  5,-20,
                -20,  0, 15, 20, 20, 15,  0,-20,
                -20,  5, 10, 15, 15, 10,  5,-20,
                -30,-20,  0,  5,  5,  0,-20,-30,
                -40,-30,-20,-20,-20,-20,-30,-40
            };
            public static readonly int[] BishopEarly = {
                -20,-10,-10,-10,-10,-10,-10,-20,
                -10,  0,  0,  0,  0,  0,  0,-10,
                -10,  0,  5, 10, 10,  5,  0,-10,
                -10,  5,  5, 10, 10,  5,  5,-10,
                -10,  0, 10, 10, 10, 10,  0,-10,
                -10, 10, 10, 10, 10, 10, 10,-10,
                -10,  5,  0,  0,  0,  0,  5,-10,
                -20,-10,-10,-10,-10,-10,-10,-20
            };
            public static readonly int[] BishopEnd = {
                -20,-10,-10,-10,-10,-10,-10,-20,
                -10,  0,  0,  0,  0,  0,  0,-10,
                -10,  0,  5, 10, 10,  5,  0,-10,
                -10,  5,  5, 10, 10,  5,  5,-10,
                -10,  0, 10, 10, 10, 10,  0,-10,
                -10, 10, 10, 10, 10, 10, 10,-10,
                -10,  5,  0,  0,  0,  0,  5,-10,
                -20,-10,-10,-10,-10,-10,-10,-20
            };
            public static readonly int[] RookEarly = {
                 0,  0,  0,  5,  0, 10,  0,  0,
                 5, 10, 10, 10, 10, 10, 10,  5,
                -5,  0,  0,  0,  0,  0,  0, -5,
                -5,  0,  0,  0,  0,  0,  0, -5,
                -5,  0,  0,  0,  0,  0,  0, -5,
                -5,  0,  0,  0,  0,  0,  0, -5,
                -5,  0,  0,  0,  0,  0,  0, -5,
                 0,  0,  0, 20,  5, 20,  0,  0
            };
            public static readonly int[] RookEnd = {
                 0,  0,  0,  0,  0,  0,  0,  0,
                 5, 10, 10, 10, 10, 10, 10,  5,
                -5,  0,  0,  0,  0,  0,  0, -5,
                -5,  0,  0,  0,  0,  0,  0, -5,
                -5,  0,  0,  0,  0,  0,  0, -5,
                -5,  0,  0,  0,  0,  0,  0, -5,
                -5,  0,  0,  0,  0,  0,  0, -5,
                 0,  0,  0,  5,  5,  0,  0,  0
            };
            public static readonly int[] QueenEarly = {
                -20,-10,-10, -5, -5,-10,-10,-20,
                -10,  0,  0,  0,  0,  0,  0,-10,
                -10,  0,  5,  5,  5,  5,  0,-10,
                 -5,  0,  5,  5,  5,  5,  0, -5,
                 -5,  0,  5,  5,  5,  5,  0, -5,
                -10,  5,  5,  5,  5,  5,  0,-10,
                -10,  0,  0,  0,  0,  0,  0,-10,
                -20,-10,-10, -5, -5,-10,-10,-20
            };
            public static readonly int[] QueenEnd = {
                -20,-10,-10, -5, -5,-10,-10,-20,
                -10,  0,  0,  0,  0,  0,  0,-10,
                -10,  0,  5,  5,  5,  5,  0,-10,
                 -5,  0,  5,  5,  5,  5,  0, -5,
                 -5,  0,  5,  5,  5,  5,  0, -5,
                -10,  0,  5,  5,  5,  5,  0,-10,
                -10,  0,  0,  0,  0,  0,  0,-10,
                -20,-10,-10, -5, -5,-10,-10,-20
            };
            public static readonly int[] KingEarly = {
                -30,-40,-40,-50,-50,-40,-40,-30,
                -30,-40,-40,-50,-50,-40,-40,-30,
                -30,-40,-40,-50,-50,-40,-40,-30,
                -30,-40,-40,-50,-50,-40,-40,-30,
                -20,-30,-30,-40,-40,-30,-30,-20,
                -10,-20,-20,-20,-20,-20,-20,-10,
                -10,-25,-40,-40,-40,-40,-25,-10,
                 20, 30, 10,  0,  0, 10, 40, 20
            };
            public static readonly int[] KingEnd = {
                -50,-40,-30,-20,-20,-30,-40,-50,
                -30,-20,-10,  0,  0,-10,-20,-30,
                -30,-10, 20, 30, 30, 20,-10,-30,
                -30,-10, 30, 40, 40, 30,-10,-30,
                -30,-10, 30, 40, 40, 30,-10,-30,
                -30,-10, 20, 30, 30, 20,-10,-30,
                -30,-30,  0,  0,  0,  0,-30,-30,
                -50,-30,-30,-30,-30,-30,-30,-50
            };
        }

    }
}
