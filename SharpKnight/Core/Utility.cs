using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SharpKnight.Core
{

    public struct PerftMetrics
    {
        public U64 Nodes;
        public U64 Captures;
        public U64 EnPassants;
        public U64 Castles;
        public U64 Promotions;
        public U64 Checks;
        public U64 Checkmates;

        public PerftMetrics(
            U64 node = 0, U64 capture = 0, U64 enPassant = 0,
            U64 castle = 0, U64 promotion = 0, U64 check = 0, U64 checkmate = 0)
        {
            Nodes = node;
            Captures = capture;
            EnPassants = enPassant;
            Castles = castle;
            Promotions = promotion;
            Checks = check;
            Checkmates = checkmate;
        }

        public static PerftMetrics operator +(PerftMetrics a, PerftMetrics b) =>
            new PerftMetrics(
                a.Nodes + b.Nodes,
                a.Captures + b.Captures,
                a.EnPassants + b.EnPassants,
                a.Castles + b.Castles,
                a.Promotions + b.Promotions,
                a.Checks + b.Checks,
                a.Checkmates + b.Checkmates);

        public PerftMetrics Add(PerftMetrics other)
        {
            Nodes += other.Nodes;
            Captures += other.Captures;
            EnPassants += other.EnPassants;
            Castles += other.Castles;
            Promotions += other.Promotions;
            Checks += other.Checks;
            Checkmates += other.Checkmates;
            return this;
        }
    }

    /// <summary>
    /// Helper class to store a position plus how moves to get there.
    /// </summary>
    public sealed class PositionInfo
    {
        public U64 ZobristKey { get; }
        public string Fen { get; }
        public List<string> MoveSequence { get; }

        public PositionInfo(U64 key, string fen, IEnumerable<string> moves)
        {
            ZobristKey = key;
            Fen = fen;
            MoveSequence = moves.ToList();
        }
    }

    /// <summary>
    /// Static class for general utility functions and data structures.
    /// </summary>
    public static class Utility
    {
        // FEN and SAN maps
        /// <summary>
        /// Maps characters to PieceTypes.
        /// </summary>
        public static readonly Dictionary<char, PieceType> FenToPiece = new()
        {
            ['P'] = PieceType.W_PAWN, ['N'] = PieceType.W_KNIGHT, ['B'] = PieceType.W_BISHOP,
            ['R'] = PieceType.W_ROOK, ['Q'] = PieceType.W_QUEEN, ['K'] = PieceType.W_KING,
            ['p'] = PieceType.B_PAWN, ['n'] = PieceType.B_KNIGHT, ['b'] = PieceType.B_BISHOP,
            ['r'] = PieceType.B_ROOK, ['q'] = PieceType.B_QUEEN, ['k'] = PieceType.B_KING
        };

        /// <summary>
        /// Maps PieceType enum to characters.
        /// </summary>
        public static readonly Dictionary<PieceType, string> PieceToFEN = new()
        {
            [PieceType.W_PAWN] = "P", [PieceType.W_KNIGHT] = "N", [PieceType.W_BISHOP] = "B",
            [PieceType.W_ROOK] = "R", [PieceType.W_QUEEN] = "Q", [PieceType.W_KING] = "K",
            [PieceType.B_PAWN] = "p", [PieceType.B_KNIGHT] = "n", [PieceType.B_BISHOP] = "b",
            [PieceType.B_ROOK] = "r", [PieceType.B_QUEEN] = "q", [PieceType.B_KING] = "k",
        };

        /// <summary>
        /// Maps PieceType enum to SAN characters.
        /// </summary>
        public static readonly Dictionary<PieceType, string> PieceToSAN = new()
        {
            [PieceType.W_KNIGHT] = "N", [PieceType.W_BISHOP] = "B",
            [PieceType.W_ROOK] = "R", [PieceType.W_QUEEN] = "Q", [PieceType.W_KING] = "K",
            [PieceType.B_KNIGHT] = "N", [PieceType.B_BISHOP] = "B",
            [PieceType.B_ROOK] = "R", [PieceType.B_QUEEN] = "Q", [PieceType.B_KING] = "K",
        };

        public static int GetDenseType(PieceType type)
        {
            return ((int)type) & 7;
        }

        public static int GetColor(PieceType type)
        {
            return ((int)type) >> 3;
        }

        public static bool IsWithinBoard(int index) => index >= 0 && index < 64;

        public static void PrintBitboard(U64 bb)
        {
            Console.WriteLine();
            for (int r = 7; r >= 0; r--)
            {
                for (int f = 0; f < 8; f++)
                {
                    int sq = r * 8 + f;
                    Console.Write(((bb >> sq) & 1UL) != 0 ? '1' : '0');
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        public static void PrintBBLine(U64 bb)
        {
            var sb = new StringBuilder(64);
            for (int i = 63; i >= 0; i--) sb.Append(((bb >> i) & 1UL) != 0 ? '1' : '0');
            Console.WriteLine(sb.ToString());
        }

        // "e4" -> 28
        public static int AlgebraicToIndex(string square)
        {
            if (string.IsNullOrEmpty(square) || square.Length != 2) return -1;
            int file = char.ToLowerInvariant(square[0]) - 'a';
            int rank = square[1] - '1';
            if ((uint)file > 7 || (uint)rank > 7) return -1;
            return rank * 8 + file;
        }

        // 28 -> "e4"
        public static string IndexToAlgebraic(int index)
        {
            if (index < 0 || index > 63) return "??";
            char file = (char)('a' + (index % 8));
            char rank = (char)('1' + (index / 8));
            return $"{file}{rank}";
        }

        public static string ToAlgebraic(this DenseMove move)
        {
            var s = IndexToAlgebraic(move.GetFrom()) + IndexToAlgebraic(move.GetTo());
            var promo = move.GetPromoteDense();
            if (promo != DenseType.D_EMPTY)
            {
                s += promo switch
                {
                    DenseType.D_KNIGHT => "n",
                    DenseType.D_BISHOP => "b",
                    DenseType.D_ROOK => "r",
                    DenseType.D_QUEEN => "q",
                    _ => ""
                };
            }
            return s;
        }

        /// <param name="fen"></param>
        /// <returns>True if string is valid FEN, false if invalid.</returns>
        public static bool IsValidFEN(string fen)
        {
            if (string.IsNullOrWhiteSpace(fen)) return false;

            var parts = fen.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4) return false;

            var position = parts[0];
            var playerToMove = parts[1];
            var castlingRights = parts[2];
            var enPassant = parts[3];
            string halfTurns = parts.Length > 4 ? parts[4] : "0";
            string fullTurns = parts.Length > 5 ? parts[5] : "1";

            int square = 56;
            foreach (char c in position)
            {
                if (square < 0) return false;
                if (c == '/')
                {
                    square -= 16;
                    continue;
                }
                if (char.IsDigit(c))
                {
                    square += (c - '0');
                    continue;
                }
                if (!FenToPiece.ContainsKey(c)) return false;
                square++;
            }

            if (playerToMove != "w" && playerToMove != "b") return false;

            foreach (char c in castlingRights)
            {
                if (c == '-' && castlingRights.Length > 1) return false;
                if ("KQkq-".IndexOf(c) < 0) return false;
            }

            if (enPassant != "-")
            {
                int idx = AlgebraicToIndex(enPassant);
                if (idx < 16 || idx > 47) return false;
            }

            if (halfTurns.Any(ch => ch < '0' || ch > '9')) return false;
            if (fullTurns.Any(ch => ch < '0' || ch > '9')) return false;

            return true;
        }

        public static int CountLegalMoves(ChessBoard board)
        {
            int moveNum = 0;
            var _ = MoveGenerator.GenerateLegalMoves(board, ref moveNum);
            return moveNum;
        }

        public static bool IsCheckmate(ChessBoard board)
        {
            if (!board.IsInCheck()) return false;
            int moveNum = 0;
            var list = MoveGenerator.GenerateLegalMoves(board, ref moveNum);
            return moveNum == 0;
        }

        public static bool IsStalemate(ChessBoard board)
        {
            int moveNum = 0;
            var list = MoveGenerator.GenerateLegalMoves(board, ref moveNum);
            if (moveNum != 0) return false;
            if (board.IsInCheck()) return false;
            return true;
        }

        public static U64 Perft(ChessBoard board, int maxDepth, int depth, bool displaySubPerft = false)
        {
            if (depth == 0) return 1UL;

            U64 nodes = 0;
            int moveNum = 0;
            var moves = MoveGenerator.GeneratePsuedoMoves(board, ref moveNum);

            for (int i = 0; i < moveNum; i++)
            {
                board.MakeMove(moves[i], searching: true);

                if (!board.IsSideInCheck(board.GetOppSide()))
                {
                    var sub = Perft(board, maxDepth, depth - 1, displaySubPerft);
                    nodes += sub;
                    if (displaySubPerft && maxDepth == depth)
                        Console.WriteLine($"{moves[i].ToAlgebraic()}: {sub}");
                }

                board.UnmakeMove(moves[i], searching: true);
            }

            return nodes;
        }

        public static void SetupTestPosition(ChessBoard board, string positionName)
        {
            var tests = new Dictionary<string, string>
            {
                ["initial"] = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
                ["kiwipete"] = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1",
                ["position3"] = "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1",
                ["position4"] = "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1",
            };

            if (tests.TryGetValue(positionName, out var fen))
                board.SetupPositionFromFEN(fen);
        }

        public static bool VerifyAttackPattern(ChessBoard board, int square, IEnumerable<string> expectedAttacks)
        {
            U64 attacks;

            if ((board.GetPieceSet(PieceType.W_PAWN) & (1UL << square)) != 0)
                attacks = AttackMasks.WPawn[square];
            else if ((board.GetPieceSet(PieceType.W_KNIGHT) & (1UL << square)) != 0)
                attacks = AttackMasks.Knight[square];
            else if ((board.GetPieceSet(PieceType.W_BISHOP) & (1UL << square)) != 0)
                attacks = AttackMasks.BishopWithEdges[square];
            else if ((board.GetPieceSet(PieceType.W_ROOK) & (1UL << square)) != 0)
                attacks = AttackMasks.RookWithEdges[square];
            else if ((board.GetPieceSet(PieceType.W_QUEEN) & (1UL << square)) != 0)
                attacks = AttackMasks.QueenWithEdges[square];
            else if ((board.GetPieceSet(PieceType.W_KING) & (1UL << square)) != 0)
                attacks = AttackMasks.King[square];
            else
                return false;

            U64 expected = 0;
            foreach (var a in expectedAttacks)
            {
                int idx = AlgebraicToIndex(a);
                if (idx != -1) expected |= 1UL << idx;
            }
            return attacks == expected;
        }

        // SAN → DenseMove (returns default DenseMove() on failure)
        /// <param name="san"></param>
        /// <param name="board"></param>
        /// <returns>Correct DenseMove corresponding to SAN string if possible, default DenseMove if not.</returns>
        public static DenseMove SanToMove(string san, ChessBoard board)
        {
            if (string.IsNullOrWhiteSpace(san))
            {
                Console.WriteLine("Empty SAN");
                return default;
            }

            PieceType pieceType = PieceType.EMPTY;
            int fromFile = -1, fromRank = -1;
            int toFile = -1, toRank = -1;
            bool isCapture = false;
            bool isCastle = false;
            bool isPromotion = false;
            PieceType promoteTo = PieceType.EMPTY;

            var side = board.CurrentGameState.SideToMove;

            // Castling
            if (san == "O-O")
            {
                pieceType = (side == Color.WHITE) ? PieceType.W_KING : PieceType.B_KING;
                int from = (side == Color.WHITE) ? 4 : 60;
                int to = (side == Color.WHITE) ? 6 : 62;
                fromFile = from % 8; fromRank = from / 8;
                toFile = to % 8; toRank = to / 8;
                isCastle = true;
            }
            else if (san == "O-O-O")
            {
                pieceType = (side == Color.WHITE) ? PieceType.W_KING : PieceType.B_KING;
                int from = (side == Color.WHITE) ? 4 : 60;
                int to = (side == Color.WHITE) ? 2 : 58;
                fromFile = from % 8; fromRank = from / 8;
                toFile = to % 8; toRank = to / 8;
                isCastle = true;
            }
            else
            {
                string s = san;
                if (s.EndsWith("+") || s.EndsWith("#")) s = s[..^1];

                if (char.IsUpper(s[0]) && s[0] != 'O')
                {
                    char pc = s[0];
                    if (!FenToPiece.ContainsKey(pc)) return default;
                    pieceType = (side == Color.WHITE)
                        ? FenToPiece[pc]
                        : (PieceType)((int)FenToPiece[pc] + 8);
                }
                else if (s[0] != 'O')
                {
                    pieceType = (side == Color.WHITE) ? PieceType.W_PAWN : PieceType.B_PAWN;
                    int f = s[0] - 'a';
                    if ((uint)f > 7) return default;
                    toFile = f; // will be adjusted during parsing if needed
                }

                int idx = 1;
                while (idx < s.Length)
                {
                    char c = s[idx];

                    if (c >= 'a' && c <= 'h' && c != 'x')
                    {
                        if (toFile == -1) toFile = c - 'a';
                        else { fromFile = toFile; toFile = c - 'a'; }
                    }
                    else if (c == 'x') { isCapture = true; }
                    else if (c >= '1' && c <= '8')
                    {
                        if (toRank == -1) toRank = c - '1';
                        else { fromRank = toRank; toRank = c - '1'; }
                    }
                    else if (c == '=')
                    {
                        if (idx + 1 >= s.Length) return default;
                        isPromotion = true;
                        char pr = s[idx + 1];
                        promoteTo = pr switch
                        {
                            'Q' => (side == Color.WHITE) ? PieceType.W_QUEEN : PieceType.B_QUEEN,
                            'R' => (side == Color.WHITE) ? PieceType.W_ROOK : PieceType.B_ROOK,
                            'B' => (side == Color.WHITE) ? PieceType.W_BISHOP : PieceType.B_BISHOP,
                            'N' => (side == Color.WHITE) ? PieceType.W_KNIGHT : PieceType.B_KNIGHT,
                            _ => PieceType.EMPTY
                        };
                        if (promoteTo == PieceType.EMPTY) return default;
                        idx++; // skip piece letter
                    }

                    idx++;
                }
            }

            if (toFile == -1 || toRank == -1) return default;

            int moveNum = 0;
            var candidates = MoveGenerator.GenerateLegalMoves(board, ref moveNum);

            for (int i = 0; i < moveNum; i++)
            {
                var cand = candidates[i];

                if (cand.GetPieceType() != pieceType) continue;
                if (cand.GetTo() != (toFile + toRank * 8)) continue;
                if (cand.IsCapture() != isCapture) continue;

                int cFile = cand.GetFrom() % 8;
                int cRank = cand.GetFrom() / 8;
                if ((fromFile != -1 && cFile != fromFile) ||
                    (fromRank != -1 && cRank != fromRank))
                    continue;

                if (cand.IsPromotion() != isPromotion) continue;
                if (isPromotion && cand.GetPromotePiece() != promoteTo) continue;

                if (cand.IsCastle() != isCastle) continue;

                return cand;
            }

            return default;
        }

        public static void PrintBoard(ChessBoard board)
        {
            Console.WriteLine($"Side to move: {board.GetSideToMove()}");
            Console.WriteLine("\n   a b c d e f g h");
            Console.WriteLine("   ---------------");

            for (int rank = 7; rank >= 0; rank--)
            {
                Console.Write($"{rank + 1}| ");
                for (int file = 0; file < 8; file++)
                {
                    int sq = rank * 8 + file;
                    var p = board.GetPieceAt(sq);
                    char ch = p switch
                    {
                        PieceType.W_PAWN => 'P',
                        PieceType.W_KNIGHT => 'N',
                        PieceType.W_BISHOP => 'B',
                        PieceType.W_ROOK => 'R',
                        PieceType.W_QUEEN => 'Q',
                        PieceType.W_KING => 'K',
                        PieceType.B_PAWN => 'p',
                        PieceType.B_KNIGHT => 'n',
                        PieceType.B_BISHOP => 'b',
                        PieceType.B_ROOK => 'r',
                        PieceType.B_QUEEN => 'q',
                        PieceType.B_KING => 'k',
                        _ => '.'
                    };
                    Console.Write(ch);
                    Console.Write(' ');
                }
                Console.WriteLine($"| {rank + 1}");
            }
            Console.WriteLine("   ---------------");
            Console.WriteLine("   a b c d e f g h\n");
        }

        // Stubs
        public static U64 CheckZobristConsistency(
            ChessBoard board, int maxDepth, int depth,
            Dictionary<string, PositionInfo> seenPositions,
            List<string> currentMoveSequence,
            bool displaySubPositions = false)
        {
            throw new NotImplementedException("CheckZobristConsistency not completed yet.");
        }

        public static U64 DebugZobristKeys(ChessBoard board, int depth, bool displaySubPositions = false)
        {
            throw new NotImplementedException("DebugZobristKeys not completed yet.");
        }
    }

    /// <summary>
    /// Static class containing bitboard masks for each piece attack on each square.
    /// </summary>
    public static class AttackMasks
    {
        public static readonly U64[] WPawn = new U64[64] {
                0b0000000000000000000000000000000000000000000000000000001000000000,     // Index 0 (a1)
                0b0000000000000000000000000000000000000000000000000000010100000000,     // Index 1 (b1)
                0b0000000000000000000000000000000000000000000000000000101000000000,     // ...
                0b0000000000000000000000000000000000000000000000000001010000000000,     // All masks go in index order
                0b0000000000000000000000000000000000000000000000000010100000000000,
                0b0000000000000000000000000000000000000000000000000101000000000000,
                0b0000000000000000000000000000000000000000000000001010000000000000,
                0b0000000000000000000000000000000000000000000000000100000000000000,
                0b0000000000000000000000000000000000000000000000100000000000000000,
                0b0000000000000000000000000000000000000000000001010000000000000000,
                0b0000000000000000000000000000000000000000000010100000000000000000,
                0b0000000000000000000000000000000000000000000101000000000000000000,
                0b0000000000000000000000000000000000000000001010000000000000000000,
                0b0000000000000000000000000000000000000000010100000000000000000000,
                0b0000000000000000000000000000000000000000101000000000000000000000,
                0b0000000000000000000000000000000000000000010000000000000000000000,
                0b0000000000000000000000000000000000000010000000000000000000000000,
                0b0000000000000000000000000000000000000101000000000000000000000000,
                0b0000000000000000000000000000000000001010000000000000000000000000,
                0b0000000000000000000000000000000000010100000000000000000000000000,
                0b0000000000000000000000000000000000101000000000000000000000000000,
                0b0000000000000000000000000000000001010000000000000000000000000000,
                0b0000000000000000000000000000000010100000000000000000000000000000,
                0b0000000000000000000000000000000001000000000000000000000000000000,
                0b0000000000000000000000000000001000000000000000000000000000000000,
                0b0000000000000000000000000000010100000000000000000000000000000000,
                0b0000000000000000000000000000101000000000000000000000000000000000,
                0b0000000000000000000000000001010000000000000000000000000000000000,
                0b0000000000000000000000000010100000000000000000000000000000000000,
                0b0000000000000000000000000101000000000000000000000000000000000000,
                0b0000000000000000000000001010000000000000000000000000000000000000,
                0b0000000000000000000000000100000000000000000000000000000000000000,
                0b0000000000000000000000100000000000000000000000000000000000000000,
                0b0000000000000000000001010000000000000000000000000000000000000000,
                0b0000000000000000000010100000000000000000000000000000000000000000,
                0b0000000000000000000101000000000000000000000000000000000000000000,
                0b0000000000000000001010000000000000000000000000000000000000000000,
                0b0000000000000000010100000000000000000000000000000000000000000000,
                0b0000000000000000101000000000000000000000000000000000000000000000,
                0b0000000000000000010000000000000000000000000000000000000000000000,
                0b0000000000000010000000000000000000000000000000000000000000000000,
                0b0000000000000101000000000000000000000000000000000000000000000000,
                0b0000000000001010000000000000000000000000000000000000000000000000,
                0b0000000000010100000000000000000000000000000000000000000000000000,
                0b0000000000101000000000000000000000000000000000000000000000000000,
                0b0000000001010000000000000000000000000000000000000000000000000000,
                0b0000000010100000000000000000000000000000000000000000000000000000,
                0b0000000001000000000000000000000000000000000000000000000000000000,
                0b0000001000000000000000000000000000000000000000000000000000000000,
                0b0000010100000000000000000000000000000000000000000000000000000000,
                0b0000101000000000000000000000000000000000000000000000000000000000,
                0b0001010000000000000000000000000000000000000000000000000000000000,
                0b0010100000000000000000000000000000000000000000000000000000000000,
                0b0101000000000000000000000000000000000000000000000000000000000000,
                0b1010000000000000000000000000000000000000000000000000000000000000,
                0b0100000000000000000000000000000000000000000000000000000000000000,
                0b0000000000000000000000000000000000000000000000000000000000000010,
                0b0000000000000000000000000000000000000000000000000000000000000101,
                0b0000000000000000000000000000000000000000000000000000000000001010,
                0b0000000000000000000000000000000000000000000000000000000001000000,
                0b0000000000000000000000000000000000000000000000000000000000010100,
                0b0000000000000000000000000000000000000000000000000000000000101000,
                0b0000000000000000000000000000000000000000000000000000000001010000,
                0b0000000000000000000000000000000000000000000000000000000010100000,     // Index 63 (h8)
        };
        public static readonly U64[] BPawn = new U64[64] {
                0b0000001000000000000000000000000000000000000000000000000000000000,
                0b0000010100000000000000000000000000000000000000000000000000000000,
                0b0000101000000000000000000000000000000000000000000000000000000000,
                0b0001010000000000000000000000000000000000000000000000000000000000,
                0b0010100000000000000000000000000000000000000000000000000000000000,
                0b0101000000000000000000000000000000000000000000000000000000000000,
                0b1010000000000000000000000000000000000000000000000000000000000000,
                0b0100000000000000000000000000000000000000000000000000000000000000,
                0b0000000000000000000000000000000000000000000000000000000000000010,
                0b0000000000000000000000000000000000000000000000000000000000000101,
                0b0000000000000000000000000000000000000000000000000000000000001010,
                0b0000000000000000000000000000000000000000000000000000000000010100,
                0b0000000000000000000000000000000000000000000000000000000000101000,
                0b0000000000000000000000000000000000000000000000000000000001010000,
                0b0000000000000000000000000000000000000000000000000000000010100000,
                0b0000000000000000000000000000000000000000000000000000000001000000,
                0b0000000000000000000000000000000000000000000000000000001000000000,
                0b0000000000000000000000000000000000000000000000000000010100000000,
                0b0000000000000000000000000000000000000000000000000000101000000000,
                0b0000000000000000000000000000000000000000000000000001010000000000,
                0b0000000000000000000000000000000000000000000000000010100000000000,
                0b0000000000000000000000000000000000000000000000000101000000000000,
                0b0000000000000000000000000000000000000000000000001010000000000000,
                0b0000000000000000000000000000000000000000000000000100000000000000,
                0b0000000000000000000000000000000000000000000000100000000000000000,
                0b0000000000000000000000000000000000000000000001010000000000000000,
                0b0000000000000000000000000000000000000000000010100000000000000000,
                0b0000000000000000000000000000000000000000000101000000000000000000,
                0b0000000000000000000000000000000000000000001010000000000000000000,
                0b0000000000000000000000000000000000000000010100000000000000000000,
                0b0000000000000000000000000000000000000000101000000000000000000000,
                0b0000000000000000000000000000000000000000010000000000000000000000,
                0b0000000000000000000000000000000000000010000000000000000000000000,
                0b0000000000000000000000000000000000000101000000000000000000000000,
                0b0000000000000000000000000000000000001010000000000000000000000000,
                0b0000000000000000000000000000000000010100000000000000000000000000,
                0b0000000000000000000000000000000000101000000000000000000000000000,
                0b0000000000000000000000000000000001010000000000000000000000000000,
                0b0000000000000000000000000000000010100000000000000000000000000000,
                0b0000000000000000000000000000000001000000000000000000000000000000,
                0b0000000000000000000000000000001000000000000000000000000000000000,
                0b0000000000000000000000000000010100000000000000000000000000000000,
                0b0000000000000000000000000000101000000000000000000000000000000000,
                0b0000000000000000000000000001010000000000000000000000000000000000,
                0b0000000000000000000000000010100000000000000000000000000000000000,
                0b0000000000000000000000000101000000000000000000000000000000000000,
                0b0000000000000000000000001010000000000000000000000000000000000000,
                0b0000000000000000000000000100000000000000000000000000000000000000,
                0b0000000000000000000000100000000000000000000000000000000000000000,
                0b0000000000000000000001010000000000000000000000000000000000000000,
                0b0000000000000000000010100000000000000000000000000000000000000000,
                0b0000000000000000000101000000000000000000000000000000000000000000,
                0b0000000000000000001010000000000000000000000000000000000000000000,
                0b0000000000000000010100000000000000000000000000000000000000000000,
                0b0000000000000000101000000000000000000000000000000000000000000000,
                0b0000000000000000010000000000000000000000000000000000000000000000,
                0b0000000000000010000000000000000000000000000000000000000000000000,
                0b0000000000000101000000000000000000000000000000000000000000000000,
                0b0000000000001010000000000000000000000000000000000000000000000000,
                0b0000000000010100000000000000000000000000000000000000000000000000,
                0b0000000000101000000000000000000000000000000000000000000000000000,
                0b0000000001010000000000000000000000000000000000000000000000000000,
                0b0000000010100000000000000000000000000000000000000000000000000000,
                0b0000000001000000000000000000000000000000000000000000000000000000
        };
        public static readonly U64[] Knight = new U64[64] {
                0b0000000000000000000000000000000000000000000000100000010000000000,
                0b0000000000000000000000000000000000000000000001010000100000000000,
                0b0000000000000000000000000000000000000000000010100001000100000000,
                0b0000000000000000000000000000000000000000000101000010001000000000,
                0b0000000000000000000000000000000000000000001010000100010000000000,
                0b0000000000000000000000000000000000000000010100001000100000000000,
                0b0000000000000000000000000000000000000000101000000001000000000000,
                0b0000000000000000000000000000000000000000010000000010000000000000,
                0b0000000000000000000000000000000000000010000001000000000000000100,
                0b0000000000000000000000000000000000000101000010000000000000001000,
                0b0000000000000000000000000000000000001010000100010000000000010001,
                0b0000000000000000000000000000000000010100001000100000000000100010,
                0b0000000000000000000000000000000000101000010001000000000001000100,
                0b0000000000000000000000000000000001010000100010000000000010001000,
                0b0000000000000000000000000000000010100000000100000000000000010000,
                0b0000000000000000000000000000000001000000001000000000000000100000,
                0b0000000000000000000000000000001000000100000000000000010000000010,
                0b0000000000000000000000000000010100001000000000000000100000000101,
                0b0000000000000000000000000000101000010001000000000001000100001010,
                0b0000000000000000000000000001010000100010000000000010001000010100,
                0b0000000000000000000000000010100001000100000000000100010000101000,
                0b0000000000000000000000000101000010001000000000001000100001010000,
                0b0000000000000000000000001010000000010000000000000001000010100000,
                0b0000000000000000000000000100000000100000000000000010000001000000,
                0b0000000000000000000000100000010000000000000001000000001000000000,
                0b0000000000000000000001010000100000000000000010000000010100000000,
                0b0000000000000000000010100001000100000000000100010000101000000000,
                0b0000000000000000000101000010001000000000001000100001010000000000,
                0b0000000000000000001010000100010000000000010001000010100000000000,
                0b0000000000000000010100001000100000000000100010000101000000000000,
                0b0000000000000000101000000001000000000000000100001010000000000000,
                0b0000000000000000010000000010000000000000001000000100000000000000,
                0b0000000000000010000001000000000000000100000000100000000000000000,
                0b0000000000000101000010000000000000001000000001010000000000000000,
                0b0000000000001010000100010000000000010001000010100000000000000000,
                0b0000000000010100001000100000000000100010000101000000000000000000,
                0b0000000000101000010001000000000001000100001010000000000000000000,
                0b0000000001010000100010000000000010001000010100000000000000000000,
                0b0000000010100000000100000000000000010000101000000000000000000000,
                0b0000000001000000001000000000000000100000010000000000000000000000,
                0b0000001000000100000000000000010000000010000000000000000000000000,
                0b0000010100001000000000000000100000000101000000000000000000000000,
                0b0000101000010001000000000001000100001010000000000000000000000000,
                0b0001010000100010000000000010001000010100000000000000000000000000,
                0b0010100001000100000000000100010000101000000000000000000000000000,
                0b0101000010001000000000001000100001010000000000000000000000000000,
                0b1010000000010000000000000001000010100000000000000000000000000000,
                0b0100000000100000000000000010000001000000000000000000000000000000,
                0b0000010000000000000001000000001000000000000000000000000000000000,
                0b0000100000000000000010000000010100000000000000000000000000000000,
                0b0001000100000000000100010000101000000000000000000000000000000000,
                0b0010001000000000001000100001010000000000000000000000000000000000,
                0b0100010000000000010001000010100000000000000000000000000000000000,
                0b1000100000000000100010000101000000000000000000000000000000000000,
                0b0001000000000000000100001010000000000000000000000000000000000000,
                0b0010000000000000001000000100000000000000000000000000000000000000,
                0b0000000000000100000000100000000000000000000000000000000000000000,
                0b0000000000001000000001010000000000000000000000000000000000000000,
                0b0000000000010001000010100000000000000000000000000000000000000000,
                0b0000000000100010000101000000000000000000000000000000000000000000,
                0b0000000001000100001010000000000000000000000000000000000000000000,
                0b0000000010001000010100000000000000000000000000000000000000000000,
                0b0000000000010000101000000000000000000000000000000000000000000000,
                0b0000000000100000010000000000000000000000000000000000000000000000};
        public static readonly U64[] BishopWithEdges = new U64[64] {
                0b1000000001000000001000000001000000001000000001000000001000000000,
                0b0000000010000000010000000010000000010000000010000000010100000000,
                0b0000000000000000100000000100000000100000000100010000101000000000,
                0b0000000000000000000000001000000001000001001000100001010000000000,
                0b0000000000000000000000000000000110000010010001000010100000000000,
                0b0000000000000000000000010000001000000100100010000101000000000000,
                0b0000000000000001000000100000010000001000000100001010000000000000,
                0b0000000100000010000001000000100000010000001000000100000000000000,
                0b0100000000100000000100000000100000000100000000100000000000000010,
                0b1000000001000000001000000001000000001000000001010000000000000101,
                0b0000000010000000010000000010000000010001000010100000000000001010,
                0b0000000000000000100000000100000100100010000101000000000000010100,
                0b0000000000000000000000011000001001000100001010000000000000101000,
                0b0000000000000001000000100000010010001000010100000000000001010000,
                0b0000000100000010000001000000100000010000101000000000000010100000,
                0b0000001000000100000010000001000000100000010000000000000001000000,
                0b0010000000010000000010000000010000000010000000000000001000000100,
                0b0100000000100000000100000000100000000101000000000000010100001000,
                0b1000000001000000001000000001000100001010000000000000101000010001,
                0b0000000010000000010000010010001000010100000000000001010000100010,
                0b0000000000000001100000100100010000101000000000000010100001000100,
                0b0000000100000010000001001000100001010000000000000101000010001000,
                0b0000001000000100000010000001000010100000000000001010000000010000,
                0b0000010000001000000100000010000001000000000000000100000000100000,
                0b0001000000001000000001000000001000000000000000100000010000001000,
                0b0010000000010000000010000000010100000000000001010000100000010000,
                0b0100000000100000000100010000101000000000000010100001000100100000,
                0b1000000001000001001000100001010000000000000101000010001001000001,
                0b0000000110000010010001000010100000000000001010000100010010000010,
                0b0000001000000100100010000101000000000000010100001000100000000100,
                0b0000010000001000000100001010000000000000101000000001000000001000,
                0b0000100000010000001000000100000000000000010000000010000000010000,
                0b0000100000000100000000100000000000000010000001000000100000010000,
                0b0001000000001000000001010000000000000101000010000001000000100000,
                0b0010000000010001000010100000000000001010000100010010000001000000,
                0b0100000100100010000101000000000000010100001000100100000110000000,
                0b1000001001000100001010000000000000101000010001001000001000000001,
                0b0000010010001000010100000000000001010000100010000000010000000010,
                0b0000100000010000101000000000000010100000000100000000100000000100,
                0b0001000000100000010000000000000001000000001000000001000000001000,
                0b0000010000000010000000000000001000000100000010000001000000100000,
                0b0000100000000101000000000000010100001000000100000010000001000000,
                0b0001000100001010000000000000101000010001001000000100000010000000,
                0b0010001000010100000000000001010000100010010000011000000000000000,
                0b0100010000101000000000000010100001000100100000100000000100000000,
                0b1000100001010000000000000101000010001000000001000000001000000001,
                0b0001000010100000000000001010000000010000000010000000010000000010,
                0b0010000001000000000000000100000000100000000100000000100000000100,
                0b0000001000000000000000100000010000001000000100000010000001000000,
                0b0000010100000000000001010000100000010000001000000100000010000000,
                0b0000101000000000000010100001000100100000010000001000000000000000,
                0b0001010000000000000101000010001001000001100000000000000000000000,
                0b0010100000000000001010000100010010000010000000010000000000000000,
                0b0101000000000000010100001000100000000100000000100000000100000000,
                0b1010000000000000101000000001000000001000000001000000001000000001,
                0b0100000000000000010000000010000000010000000010000000010000000010,
                0b0000000000000010000001000000100000010000001000000100000010000000,
                0b0000000000000101000010000001000000100000010000001000000000000000,
                0b0000000000001010000100010010000001000000100000000000000000000000,
                0b0000000000010100001000100100000110000000000000000000000000000000,
                0b0000000000101000010001001000001000000001000000000000000000000000,
                0b0000000001010000100010000000010000000010000000010000000000000000,
                0b0000000010100000000100000000100000000100000000100000000100000000,
                0b0000000001000000001000000001000000001000000001000000001000000001,
        };
        public static readonly U64[] RookWithEdges = new U64[64] {
                0b0000000100000001000000010000000100000001000000010000000111111110,
                0b0000001000000010000000100000001000000010000000100000001011111101,
                0b0000010000000100000001000000010000000100000001000000010011111011,
                0b0000100000001000000010000000100000001000000010000000100011110111,
                0b0001000000010000000100000001000000010000000100000001000011101111,
                0b0010000000100000001000000010000000100000001000000010000011011111,
                0b0100000001000000010000000100000001000000010000000100000010111111,
                0b1000000010000000100000001000000010000000100000001000000001111111,
                0b0000000100000001000000010000000100000001000000011111111000000001,
                0b0000001000000010000000100000001000000010000000101111110100000010,
                0b0000010000000100000001000000010000000100000001001111101100000100,
                0b0000100000001000000010000000100000001000000010001111011100001000,
                0b0001000000010000000100000001000000010000000100001110111100010000,
                0b0010000000100000001000000010000000100000001000001101111100100000,
                0b0100000001000000010000000100000001000000010000001011111101000000,
                0b1000000010000000100000001000000010000000100000000111111110000000,
                0b0000000100000001000000010000000100000001111111100000000100000001,
                0b0000001000000010000000100000001000000010111111010000001000000010,
                0b0000010000000100000001000000010000000100111110110000010000000100, // 18
                0b0000100000001000000010000000100000001000111101110000100000001000,
                0b0001000000010000000100000001000000010000111011110001000000010000,
                0b0010000000100000001000000010000000100000110111110010000000100000,
                0b0100000001000000010000000100000001000000101111110100000001000000,
                0b1000000010000000100000001000000010000000011111111000000010000000,
                0b0000000100000001000000010000000111111110000000010000000100000001,
                0b0000001000000010000000100000001011111101000000100000001000000010,
                0b0000010000000100000001000000010011111011000001000000010000000100,
                0b0000100000001000000010000000100011110111000010000000100000001000,
                0b0001000000010000000100000001000011101111000100000001000000010000,
                0b0010000000100000001000000010000011011111001000000010000000100000,
                0b0100000001000000010000000100000010111111010000000100000001000000,
                0b1000000010000000100000001000000001111111100000001000000010000000,
                0b0000000100000001000000011111111000000001000000010000000100000001,
                0b0000001000000010000000101111110100000010000000100000001000000010,
                0b0000010000000100000001001111101100000100000001000000010000000100,
                0b0000100000001000000010001111011100001000000010000000100000001000,
                0b0001000000010000000100001110111100010000000100000001000000010000,
                0b0010000000100000001000001101111100100000001000000010000000100000,
                0b0100000001000000010000001011111101000000010000000100000001000000,
                0b1000000010000000100000000111111110000000100000001000000010000000,
                0b0000000100000001111111100000000100000001000000010000000100000001,
                0b0000001000000010111111010000001000000010000000100000001000000010,
                0b0000010000000100111110110000010000000100000001000000010000000100,
                0b0000100000001000111101110000100000001000000010000000100000001000,
                0b0001000000010000111011110001000000010000000100000001000000010000,
                0b0010000000100000110111110010000000100000001000000010000000100000,
                0b0100000001000000101111110100000001000000010000000100000001000000,
                0b1000000010000000011111111000000010000000100000001000000010000000,
                0b0000000111111110000000010000000100000001000000010000000100000001,
                0b0000001011111101000000100000001000000010000000100000001000000010,
                0b0000010011111011000001000000010000000100000001000000010000000100,
                0b0000100011110111000010000000100000001000000010000000100000001000,
                0b0001000011101111000100000001000000010000000100000001000000010000,
                0b0010000011011111001000000010000000100000001000000010000000100000,
                0b0100000010111111010000000100000001000000010000000100000001000000,
                0b1000000001111111100000001000000010000000100000001000000010000000,
                0b1111111000000001000000010000000100000001000000010000000100000001,
                0b1111110100000010000000100000001000000010000000100000001000000010,
                0b1111101100000100000001000000010000000100000001000000010000000100,
                0b1111011100001000000010000000100000001000000010000000100000001000,
                0b1110111100010000000100000001000000010000000100000001000000010000,
                0b1101111100100000001000000010000000100000001000000010000000100000,
                0b1011111101000000010000000100000001000000010000000100000001000000,
                0b0111111110000000100000001000000010000000100000001000000010000000
        };
        public static readonly U64[] QueenWithEdges = new U64[64] {
                0b1000000101000001001000010001000100001001000001010000001111111110,
                0b0000001010000010010000100010001000010010000010100000011111111101,
                0b0000010000000100100001000100010000100100000101010000111011111011,
                0b0000100000001000000010001000100001001001001010100001110011110111,
                0b0001000000010000000100000001000110010010010101000011100011101111,
                0b0010000000100000001000010010001000100100101010000111000011011111,
                0b0100000001000001010000100100010001001000010100001110000010111111,
                0b1000000110000010100001001000100010010000101000001100000001111111,
                0b0100000100100001000100010000100100000101000000111111111000000011,
                0b1000001001000010001000100001001000001010000001111111110100000111,
                0b0000010010000100010001000010010000010101000011101111101100001110,
                0b0000100000001000100010000100100100101010000111001111011100011100,
                0b0001000000010000000100011001001001010100001110001110111100111000,
                0b0010000000100001001000100010010010101000011100001101111101110000,
                0b0100000101000010010001000100100001010000111000001011111111100000,
                0b1000001010000100100010001001000010100000110000000111111111000000,
                0b0010000100010001000010010000010100000011111111100000001100000101,
                0b0100001000100010000100100000101000000111111111010000011100001010,
                0b1000010001000100001001000001010100001110111110110000111000010101,
                0b0000100010001000010010010010101000011100111101110001110000101010,
                0b0001000000010001100100100101010000111000111011110011100001010100,
                0b0010000100100010001001001010100001110000110111110111000010101000,
                0b0100001001000100010010000101000011100000101111111110000001010000,
                0b1000010010001000100100001010000011000000011111111100000010100000,
                0b0001000100001001000001010000001111111110000000110000010100001001,
                0b0010001000010010000010100000011111111101000001110000101000010010,
                0b0100010000100100000101010000111011111011000011100001010100100100,
                0b1000100001001001001010100001110011110111000111000010101001001001,
                0b0001000110010010010101000011100011101111001110000101010010010010,
                0b0010001000100100101010000111000011011111011100001010100000100100,
                0b0100010001001000010100001110000010111111111000000101000001001000,
                0b1000100010010000101000001100000001111111110000001010000010010000,
                0b0000100100000101000000111111111000000011000001010000100100010001,
                0b0001001000001010000001111111110100000111000010100001001000100010,
                0b0010010000010101000011101111101100001110000101010010010001000100,
                0b0100100100101010000111001111011100011100001010100100100110001000,
                0b1001001001010100001110001110111100111000010101001001001000010001,
                0b0010010010101000011100001101111101110000101010000010010000100010,
                0b0100100001010000111000001011111111100000010100000100100001000100,
                0b1001000010100000110000000111111111000000101000001001000010001000,
                0b0000010100000011111111100000001100000101000010010001000100100001,
                0b0000101000000111111111010000011100001010000100100010001001000010,
                0b0001010100001110111110110000111000010101001001000100010010000100,
                0b0010101000011100111101110001110000101010010010011000100000001000,
                0b0101010000111000111011110011100001010100100100100001000100010000,
                0b1010100001110000110111110111000010101000001001000010001000100001,
                0b0101000011100000101111111110000001010000010010000100010001000010,
                0b1010000011000000011111111100000010100000100100001000100010000100,
                0b0000001111111110000000110000010100001001000100010010000101000001,
                0b0000011111111101000001110000101000010010001000100100001010000010,
                0b0000111011111011000011100001010100100100010001001000010000000100,
                0b0001110011110111000111000010101001001001100010000000100000001000,
                0b0011100011101111001110000101010010010010000100010001000000010000,
                0b0111000011011111011100001010100000100100001000100010000100100000,
                0b1110000010111111111000000101000001001000010001000100001001000001,
                0b1100000001111111110000001010000010010000100010001000010010000010,
                0b1111111000000011000001010000100100010001001000010100000110000001,
                0b1111110100000111000010100001001000100010010000101000001000000010,
                0b1111101100001110000101010010010001000100100001000000010000000100,
                0b1111011100011100001010100100100110001000000010000000100000001000,
                0b1110111100111000010101001001001000010001000100000001000000010000,
                0b1101111101110000101010000010010000100010001000010010000000100000,
                0b1011111111100000010100000100100001000100010000100100000101000000,
                0b0111111111000000101000001001000010001000100001001000001010000001
        };
        public static readonly U64[] King = new U64[64] {
                0b0000000000000000000000000000000000000000000000000000001100000010,
                0b0000000000000000000000000000000000000000000000000000011100000101,
                0b0000000000000000000000000000000000000000000000000000111000001010,
                0b0000000000000000000000000000000000000000000000000001110000010100,
                0b0000000000000000000000000000000000000000000000000011100000101000,
                0b0000000000000000000000000000000000000000000000000111000001010000,
                0b0000000000000000000000000000000000000000000000001110000010100000,
                0b0000000000000000000000000000000000000000000000001100000001000000,
                0b0000000000000000000000000000000000000000000000110000001000000011,
                0b0000000000000000000000000000000000000000000001110000010100000111,
                0b0000000000000000000000000000000000000000000011100000101000001110,
                0b0000000000000000000000000000000000000000000111000001010000011100,
                0b0000000000000000000000000000000000000000001110000010100000111000,
                0b0000000000000000000000000000000000000000011100000101000001110000,
                0b0000000000000000000000000000000000000000111000001010000011100000,
                0b0000000000000000000000000000000000000000110000000100000011000000,
                0b0000000000000000000000000000000000000011000000100000001100000000,
                0b0000000000000000000000000000000000000111000001010000011100000000,
                0b0000000000000000000000000000000000001110000010100000111000000000,
                0b0000000000000000000000000000000000011100000101000001110000000000,
                0b0000000000000000000000000000000000111000001010000011100000000000,
                0b0000000000000000000000000000000001110000010100000111000000000000,
                0b0000000000000000000000000000000011100000101000001110000000000000,
                0b0000000000000000000000000000000011000000010000001100000000000000,
                0b0000000000000000000000000000001100000010000000110000000000000000,
                0b0000000000000000000000000000011100000101000001110000000000000000,
                0b0000000000000000000000000000111000001010000011100000000000000000,
                0b0000000000000000000000000001110000010100000111000000000000000000,
                0b0000000000000000000000000011100000101000001110000000000000000000,
                0b0000000000000000000000000111000001010000011100000000000000000000,
                0b0000000000000000000000001110000010100000111000000000000000000000,
                0b0000000000000000000000001100000001000000110000000000000000000000,
                0b0000000000000000000000110000001000000011000000000000000000000000,
                0b0000000000000000000001110000010100000111000000000000000000000000,
                0b0000000000000000000011100000101000001110000000000000000000000000,
                0b0000000000000000000111000001010000011100000000000000000000000000,
                0b0000000000000000001110000010100000111000000000000000000000000000,
                0b0000000000000000011100000101000001110000000000000000000000000000,
                0b0000000000000000111000001010000011100000000000000000000000000000,
                0b0000000000000000110000000100000011000000000000000000000000000000,
                0b0000000000000011000000100000001100000000000000000000000000000000,
                0b0000000000000111000001010000011100000000000000000000000000000000,
                0b0000000000001110000010100000111000000000000000000000000000000000,
                0b0000000000011100000101000001110000000000000000000000000000000000,
                0b0000000000111000001010000011100000000000000000000000000000000000,
                0b0000000001110000010100000111000000000000000000000000000000000000,
                0b0000000011100000101000001110000000000000000000000000000000000000,
                0b0000000011000000010000001100000000000000000000000000000000000000,
                0b0000001100000010000000110000000000000000000000000000000000000000,
                0b0000011100000101000001110000000000000000000000000000000000000000,
                0b0000111000001010000011100000000000000000000000000000000000000000,
                0b0001110000010100000111000000000000000000000000000000000000000000,
                0b0011100000101000001110000000000000000000000000000000000000000000,
                0b0111000001010000011100000000000000000000000000000000000000000000,
                0b1110000010100000111000000000000000000000000000000000000000000000,
                0b1100000001000000110000000000000000000000000000000000000000000000,
                0b0000001000000011000000000000000000000000000000000000000000000000,
                0b0000010100000111000000000000000000000000000000000000000000000000,
                0b0000101000001110000000000000000000000000000000000000000000000000,
                0b0001010000011100000000000000000000000000000000000000000000000000,
                0b0010100000111000000000000000000000000000000000000000000000000000,
                0b0101000001110000000000000000000000000000000000000000000000000000,
                0b1010000011100000000000000000000000000000000000000000000000000000,
                0b0100000011000000000000000000000000000000000000000000000000000000,
        };
    }
}
