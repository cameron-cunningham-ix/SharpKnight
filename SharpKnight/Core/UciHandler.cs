using SharpKnight.Core;
using SharpKnight.Players;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SharpKnight.Core
{
    /// <summary>
    /// Static class for handling UCI inputs for engines.
    /// </summary>
    public static class UciHandler
    {
        /// <summary>
        /// Main UCI loop. Reads commands from stdin and dispatches to the engine.
        /// </summary>
        public static void UciLoop(EnginePlayer player)
        {
            string? line;
            while ((line = Console.ReadLine()) != null)
            {
                // Skip empty lines
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                using var reader = new StringReader(line);
                var cmd = NextToken(reader);
                if (string.IsNullOrEmpty(cmd))
                    continue;

                switch (cmd)
                {
                    case "quit":
                        return;

                    case "uci":
                        player.Uci();
                        break;

                    case "isready":
                        player.IsReady();
                        break;

                    case "ucinewgame":
                        player.UciNewGame();
                        break;

                    case "position":
                        {
                            var posType = NextToken(reader);
                            string fen = string.Empty;
                            string moves = string.Empty;

                            if (posType == "startpos")
                            {
                                // Expect optional "moves ..." remainder
                                var maybeMoves = NextToken(reader);
                                if (maybeMoves == "moves")
                                {
                                    // Read the rest of the line as moves
                                    moves = reader.ReadToEnd() ?? string.Empty;
                                    moves = moves.TrimStart();
                                }
                            }
                            else if (posType == "fen")
                            {
                                // Read FEN parts until we hit "moves" or end
                                // A valid FEN has 6 space-separated parts
                                var parts = new List<string>();
                                string? token;
                                while (!string.IsNullOrEmpty(token = NextToken(reader)) && token != "moves")
                                {
                                    parts.Add(token);
                                }
                                fen = string.Join(' ', parts);

                                if (token == "moves")
                                {
                                    moves = reader.ReadToEnd() ?? string.Empty;
                                    moves = moves.TrimStart();
                                }
                            }

                            player.Position(fen, moves);
                            break;
                        }

                    case "go":
                        {
                            // Parse "go" parameters into a dictionary<string,string>
                            // e.g., "wtime 300000 btime 300000 winc 200 binc 200 movestogo 40 ..." etc.
                            var @params = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            string? p;
                            while (!string.IsNullOrEmpty(p = NextToken(reader)))
                            {
                                var v = NextToken(reader);
                                if (string.IsNullOrEmpty(v))
                                {
                                    // Single switches without value are ignored (matches C++ behavior)
                                    break;
                                }
                                @params[p] = v;
                            }

                            player.Go(@params);
                            break;
                        }

                    case "stop":
                        player.Stop();
                        break;

                    case "setoption":
                        {
                            // Expect: setoption name <Name> [value <Value>]
                            var nameToken = NextToken(reader); // should be "name"
                            if (!string.Equals(nameToken, "name", StringComparison.Ordinal))
                                break;

                            var name = NextToken(reader);
                            if (string.IsNullOrEmpty(name))
                                break;

                            var valueToken = NextToken(reader);
                            if (string.Equals(valueToken, "value", StringComparison.Ordinal))
                            {
                                var value = NextToken(reader);
                                if (!string.IsNullOrEmpty(value))
                                {
                                    player.SetOption(name, value);
                                }
                            }
                            // else: options like "button" (no value) are ignored
                            break;
                        }

                    default:
                        // Silently ignore unknown commands
                        break;
                }
            }
        }

        /// <summary>
        /// Parses a space-separated UCI moves list ("e2e4 e7e5 g1f3"...) using the given FEN
        /// (or startpos if fen is empty). Returns the realized sequence of DenseMove objects.
        /// </summary>
        public static List<DenseMove> ParseUciMoves(string fen, string movesList)
        {
            var moves = new List<DenseMove>();
            var board = new ChessBoard();

            if (string.IsNullOrWhiteSpace(fen))
            {
                // Standard startpos
                board.SetupPositionFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
            }
            else
            {
                board.SetupPositionFromFEN(fen);
            }

            if (string.IsNullOrWhiteSpace(movesList))
                return moves;

            // Tokenize on whitespace
            var tokens = movesList.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            foreach (var moveStr in tokens)
            {
                if (moveStr.Length < 4)
                    continue;

                var fromStr = moveStr.Substring(0, 2);
                var toStr = moveStr.Substring(2, 2);

                int from = Utility.AlgebraicToIndex(fromStr);
                int to = Utility.AlgebraicToIndex(toStr);

                if (from == -1 || to == -1)
                    continue;

                var piece = board.GetPieceAt(from);
                var capturedDense = board.GetDenseTypeAt(to);

                var move = DenseMove.FromPieceType(piece, from, to, capturedDense);

                // Promotion (optional 5th char: q,r,b,n)
                if (moveStr.Length > 4)
                {
                    char promo = moveStr[4];
                    DenseType promoteTo;
                    switch (promo)
                    {
                        case 'q': promoteTo = DenseType.D_QUEEN; break;
                        case 'r': promoteTo = DenseType.D_ROOK; break;
                        case 'b': promoteTo = DenseType.D_BISHOP; break;
                        case 'n': promoteTo = DenseType.D_KNIGHT; break;
                        default: continue; // invalid promotion code
                    }
                    move.SetPromoteTo(promoteTo);
                }

                moves.Add(move);
                board.MakeMove(move, searching: true);
            }

            return moves;
        }

        // ----- small tokenizer helper -----

        private static string? NextToken(StringReader reader)
        {
            // Skip leading whitespace
            int c;
            do
            {
                c = reader.Peek();
                if (c == -1) return null;
                if (!char.IsWhiteSpace((char)c)) break;
                reader.Read();
            } while (true);

            // Accumulate non-whitespace
            var sb = new System.Text.StringBuilder();
            while ((c = reader.Peek()) != -1 && !char.IsWhiteSpace((char)c))
            {
                sb.Append((char)reader.Read());
            }
            return sb.ToString();
        }
    }
}
