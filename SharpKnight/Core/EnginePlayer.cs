using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using SharpKnight.Core;

namespace SharpKnight.Players
{
    /// <summary>
    /// Class representing a playable chess engine.
    /// </summary>
    public sealed class EnginePlayer : IPlayer, IChessEngine, IDisposable
    {
        // --- Private fields ---
        /// <summary>
        /// Current chess engine being used.
        /// </summary>
        private readonly ChessEngineBase _engine;
        /// <summary>
        /// Whether the player is accepting draws.
        /// </summary>
        private readonly bool _acceptDraws;

        // UCI-specific state
        /// <summary>
        /// True if the engine and UCI has been initialized.
        /// </summary>
        private volatile bool _initialized;
        /// <summary>
        /// True if the engine is thinking / searching a position.
        /// </summary>
        private volatile bool _thinking;
        /// <summary>
        /// True if the engine should quit / shut down.
        /// </summary>
        private volatile bool _shouldQuit;

        /// <summary>
        /// Threading and command queue for UCI loop
        /// </summary>
        private readonly Thread _uciThread;
        /// <summary>
        /// Queue of UCI commands.
        /// </summary>
        private readonly BlockingCollection<string> _commandQueue = new(new ConcurrentQueue<string>());
        /// <summary>
        /// 
        /// </summary>
        private readonly object _lock = new();

        // Position + time controls used by the UCI loop “go” path
        /// <summary>
        /// Current board the engine is using.
        /// </summary>
        private ChessBoard _currentBoard = new();
        /// <summary>
        /// Current clock the engine is using.
        /// </summary>
        private ChessClock _currentClock = new();
        /// <summary>
        /// Thread for handling searching a position for best move.
        /// </summary>
        private Thread? _searchThread;
        /// <summary>
        /// Mutex lock. Ensures board is only changed by one thread at a time.
        /// </summary>
        private readonly object _boardLock = new();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="engineImpl"></param>
        /// <param name="acceptDrawOffers"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public EnginePlayer(ChessEngineBase engineImpl, bool acceptDrawOffers = false)
        {
            _engine = engineImpl ?? throw new ArgumentNullException(nameof(engineImpl));
            _acceptDraws = acceptDrawOffers;

            // Start the background UCI loop thread immediately
            _uciThread = new Thread(UciLoop)
            {
                IsBackground = true,
                Name = "EnginePlayer.UCI"
            };
            _uciThread.Start();

            // Default “infinite” clock
            _currentClock = new ChessClock();
            _currentClock.SetInfinite(true);
        }

        /// <summary>
        /// Quit engine and dispose resources and threads.
        /// </summary>
        public void Dispose()
        {
            Quit();
            try { _uciThread.Join(); } catch { /* ignore */ }
            lock (_boardLock)
            {
                if (_searchThread != null && _searchThread.IsAlive)
                    _searchThread.Join();
            }
            _commandQueue.Dispose();
        }

        // ========== IPlayer ==========
        /// <summary>
        /// Engine finds the best move given current board and clock.
        /// </summary>
        /// <param name="board"></param>
        /// <param name="clock"></param>
        /// <returns>Best move found in search.</returns>
        public DenseMove GetMove(ChessBoard board, ChessClock clock)
        {
            _thinking = true;
            clock.Start();
            var best = _engine.FindBestMove(ref board, ref clock);
            clock.Stop();
            _thinking = false;
            return best;
        }

        /// <summary>
        /// Send out 'move' to UCI, notifying that a move was made.
        /// </summary>
        /// <param name="move"></param>
        public void NotifyOpponentMove(DenseMove move)
        {
            string moveStr = MoveToUci(move);
            Position("", moveStr);      // Empty FEN means use current position
        }

        /// <returns>PlayerType, which is Engine.</returns>
        PlayerType IPlayer.GetType()
        {
            return GetPlayerType();
        }

        /// <returns>The engine's name.</returns>
        public string GetName() => _engine.Name;
        /// <returns>PlayerType, which is Engine.</returns>
        public PlayerType GetPlayerType() => PlayerType.Engine;
        /// <returns>True if this engine player accepts draws.</returns>
        public bool AcceptsDraw() => _acceptDraws;
        /// <summary>
        /// Creates a new UCI game.
        /// </summary>
        public void OnGameEnd() => UciNewGame();

        // ========== IChessEngine (UCI facade) ==========
        /// <summary>
        /// Sends UCI command 'uci' to engine.
        /// </summary>
        public void Uci() => Enqueue("uci");
        /// <summary>
        /// Sends UCI command 'isready' to engine.
        /// </summary>
        public void IsReady() => Enqueue("isready");

        /// <summary>
        /// Sends UCI command 'option' with parameters to engine.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void SetOption(string name, string value)
            => Enqueue($"setoption name {name} value {value}");

        /// <summary>
        /// Resets engine and creates a new board for a new game.
        /// </summary>
        public void UciNewGame()
        {
            _engine.ResetSearchDepth();
            _engine.ClearForNewGame();
            _currentClock = new ChessClock();
            _currentClock.SetInfinite(true);
            _currentBoard = new ChessBoard();
        }

        /// <summary>
        /// Sends UCI command 'position' with parameters to engine.
        /// </summary>
        /// <param name="fen">FEN string. Empty string is determined to be startpos.</param>
        /// <param name="moves"></param>
        public void Position(string fen, string moves)
        {
            var sb = new StringBuilder();
            sb.Append("position ");
            if (string.IsNullOrWhiteSpace(fen))
                sb.Append("startpos ");
            else
                sb.Append("fen ").Append(fen);

            if (!string.IsNullOrWhiteSpace(moves))
                sb.Append(" moves ").Append(moves);

            Enqueue(sb.ToString());
        }

        /// <summary>
        /// Implementation of IChessEngine Go function, calls EnginePlayer.Go with parameters.
        /// </summary>
        /// <param name="searchParams"></param>
        void IChessEngine.Go(IDictionary<string, string> searchParams)
        {
            // Convert IDictionary to Dictionary for internal method
            Go(searchParams != null ? new Dictionary<string, string>(searchParams) : null);
        }

        /// <summary>
        /// Send UCI command 'go' with search parameters to engine.
        /// </summary>
        /// <param name="searchParams"></param>
        public void Go(Dictionary<string, string> searchParams)
        {
            var sb = new StringBuilder("go");
            if (searchParams != null)
            {
                foreach (var kv in searchParams)
                    sb.Append(' ').Append(kv.Key).Append(' ').Append(kv.Value);
            }
            Enqueue(sb.ToString());
        }

        /// <summary>
        /// Stop engine searching.
        /// </summary>
        public void Stop()
        {
            _engine.StopSearch();
            _thinking = false;
        }

        /// <summary>
        /// Queue the engine for quitting.
        /// </summary>
        public void Quit()
        {
            _shouldQuit = true;
            _commandQueue.CompleteAdding();
        }

        /// <returns>True if the engine has been initialized.</returns>
        public bool IsInitialized() => _initialized;
        /// <returns>True if engine is thinking / searching a position.</returns>
        public bool IsThinking() => _thinking;
        /// <summary>
        /// Implementation of IChessEngine IsInitialized; done since _initialized is volatile.
        /// </summary>
        bool IChessEngine.IsInitialized => IsInitialized();
        /// <summary>
        /// Implementation of IChessEngine IsThinking; done since _thinking is volatile.
        /// </summary>
        bool IChessEngine.IsThinking => IsThinking();

        /// <summary>
        /// Wait until the engine is initialized or 5 seconds passes.
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns>True if engine initializes, false if not initialized in 5 seconds.</returns>
        public bool WaitForInitialization(TimeSpan? timeout = null)
        {
            var limit = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
            while (!_initialized)
            {
                if (DateTime.UtcNow >= limit) return false;
                Thread.Sleep(10);
            }
            return true;
        }

        /// <summary>
        /// Name of the current chess engine loaded.
        /// </summary>
        string IChessEngine.Name => GetName();

        /// <summary>
        /// Author of the current chess engine loaded.
        /// </summary>
        public string Author => _engine.Author;

        /// <summary>
        /// Best move found by the current chess engine.
        /// </summary>
        DenseMove IChessEngine.BestMove => _engine.BestMove;

        /// <summary>
        /// Move to ponder from the current chess engine.
        /// </summary>
        DenseMove IChessEngine.PonderMove => _engine.PonderMove;

        /// <returns>Current chess engine loaded.</returns>
        public ChessEngineBase GetEngineForTesting() => _engine;

        // ========== Private: UCI Loop ==========
        /// <summary>
        /// Enqueue 'cmd' to the UCI command queue.
        /// </summary>
        /// <param name="cmd"></param>
        private void Enqueue(string cmd)
        {
            if (!_commandQueue.IsAddingCompleted)
                _commandQueue.Add(cmd);
        }

        /// <summary>
        /// Loop through and process the commands in the UCI command queue.
        /// </summary>
        private void UciLoop()
        {
            try
            {
                foreach (var cmd in _commandQueue.GetConsumingEnumerable())
                {
                    if (_shouldQuit) break;
                    ProcessCommand(cmd);
                }
            }
            catch (ObjectDisposedException) { /* shutting down */ }
        }

        /// <summary>
        /// Process one command from the UCI queue.
        /// </summary>
        /// <param name="cmd"></param>
        private void ProcessCommand(string cmd)
        {
            var iss = new StringReader(cmd);
            var token = NextToken(iss);

            if (token == "uci")
            {
                // Ensure PEXT tables are ready before move-gen usage
                if (!PEXT.Initialized) PEXT.Initialize();

                SendResponse($"id name {_engine.Name} {_engine.Version}");
                SendResponse($"id author {_engine.Author}");

                foreach (var opt in _engine.Options) // assume IEnumerable<EngineOption>
                {
                    SendResponse(opt.Value.ToUciString());
                }

                SendResponse("uciok");
                _initialized = true;
            }
            else if (token == "setoption")
            {
                // Syntax: setoption name <name> value <value>
                var nameTok = NextToken(iss); // "name"
                var name = NextToken(iss);
                var valueTok = NextToken(iss); // "value"
                if (valueTok == "value")
                {
                    var value = NextToken(iss);
                    if (!_engine.SetOption(name, value))
                    {
                        // Keep the log side-effect, but just Console.Error here
                        Console.Error.WriteLine($"Failed to set option {name} to value {value}");
                    }
                }
            }
            else if (token == "ucinewgame")
            {
                UciNewGame();
            }
            else if (token == "isready")
            {
                SendResponse("readyok");
            }
            else if (token == "go")
            {
                // Parse go parameters into a map
                var searchParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                string? p;
                while (!string.IsNullOrEmpty(p = NextToken(iss)))
                {
                    var v = NextToken(iss) ?? string.Empty;
                    searchParams[p!] = v;
                }

                lock (_boardLock)
                {
                    if (_searchThread != null && _searchThread.IsAlive)
                        _searchThread.Join();

                    _searchThread = new Thread(() =>
                    {
                        _currentClock.SetInfinite(true);

                        // Depth handling (supports "infinite" or integer)
                        if (searchParams.TryGetValue("depth", out var d))
                        {
                            if (string.Equals(d, "infinite", StringComparison.OrdinalIgnoreCase))
                                _engine.SetSearchDepth(64);
                            else if (d.All(char.IsDigit))
                                _engine.SetSearchDepth(int.Parse(d, CultureInfo.InvariantCulture));
                            else
                                _engine.ResetSearchDepth();
                        }
                        else
                        {
                            _engine.ResetSearchDepth();
                        }

                        // Time controls (wtime/btime in ms)
                        if (searchParams.TryGetValue("wtime", out var wms) && int.TryParse(wms, out var w))
                        {
                            _currentClock.SetTime(Color.WHITE, TimeSpan.FromMilliseconds(w));
                            _currentClock.SetInfinite(false);
                        }
                        if (searchParams.TryGetValue("btime", out var bms) && int.TryParse(bms, out var b))
                        {
                            _currentClock.SetTime(Color.BLACK, TimeSpan.FromMilliseconds(b));
                            _currentClock.SetInfinite(false);
                        }
                        if (_currentBoard.GetSideToMove() != _currentClock.GetActiveColor())
                        {
                            _currentClock.SwitchPlayer();
                        }

                        // Search now
                        DenseMove best;
                        lock (_boardLock)
                        {
                            best = GetMove(_currentBoard, _currentClock);
                        }
                        SendResponse("bestmove " + MoveToUci(best));
                    })
                    { IsBackground = true, Name = "EnginePlayer.Search" };
                    _searchThread.Start();
                }
            }
            else if (token == "position")
            {
                lock (_boardLock)
                {
                    var next = NextToken(iss); // "startpos" or "fen"
                    if (next == "startpos")
                    {
                        _currentBoard = new ChessBoard();
                        // optionally consume a following "moves"
                        next = NextToken(iss);
                    }
                    else if (next == "fen")
                    {
                        var fenParts = new List<string>(6);
                        for (var i = 0; i < 6; i++)
                        {
                            var part = NextToken(iss);
                            if (string.IsNullOrEmpty(part)) break;
                            fenParts.Add(part);
                        }
                        var fen = string.Join(' ', fenParts);
                        _currentBoard = new ChessBoard();
                        _currentBoard.SetupPositionFromFEN(fen);
                        next = NextToken(iss); // optionally "moves"
                    }

                    // Replay moves if present
                    if (next == "moves")
                    {
                        string? mv;
                        while (!string.IsNullOrEmpty(mv = NextToken(iss)))
                        {
                            var dm = UciToMove(mv!, _currentBoard);
                            _currentBoard.MakeMove(dm, searching: false);
                        }
                    }
                }
            }
            else if (token == "stop")
            {
                _engine.StopSearch();
                lock (_boardLock)
                {
                    if (_searchThread != null && _searchThread.IsAlive)
                        _searchThread.Join();
                }
            }
            // else: other UCI commands can be added here
        }

        /// <summary>
        /// Extract the next token from a UCI command string.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        private static string? NextToken(StringReader r)
        {
            // Simple whitespace tokenizer
            var sb = new StringBuilder();
            int ch;
            // Skip leading whitespace
            while ((ch = r.Read()) != -1 && char.IsWhiteSpace((char)ch)) { }
            if (ch == -1) return null;
            sb.Append((char)ch);
            while ((ch = r.Read()) != -1 && !char.IsWhiteSpace((char)ch))
                sb.Append((char)ch);
            return sb.ToString();
        }

        /// <summary>
        /// Write to the console.
        /// </summary>
        /// <remarks>This was started when it might've needed / handled more functionality, could probably get rid.</remarks>
        /// <param name="s"></param>
        private void SendResponse(string s)
        {
            Console.WriteLine(s);
        }

        // --- UCI move encoding/decoding helpers ---
        /// <summary>
        /// Transform a DenseMove into UCI.
        /// </summary>
        /// <param name="move"></param>
        /// <returns></returns>
        private string MoveToUci(in DenseMove move)
        {
            var from = Utility.IndexToAlgebraic(move.GetFrom());
            var to = Utility.IndexToAlgebraic(move.GetTo());
            var promo = "";

            var pd = move.GetPromoteDense();
            if (pd != DenseType.D_EMPTY)
            {
                promo = pd switch
                {
                    DenseType.D_QUEEN => "q",
                    DenseType.D_ROOK => "r",
                    DenseType.D_BISHOP => "b",
                    DenseType.D_KNIGHT => "n",
                    _ => ""
                };
            }
            return from + to + promo;
        }

        // Assumes the UCI string is valid in the current position
        /// <summary>
        /// Transform a UCI move into DenseMove.
        /// </summary>
        /// <param name="uci"></param>
        /// <param name="board"></param>
        /// <returns></returns>
        private DenseMove UciToMove(string uci, ChessBoard board)
        {
            if (uci.Length < 4) return new DenseMove();

            var from = Utility.AlgebraicToIndex(uci.Substring(0, 2));
            var to = Utility.AlgebraicToIndex(uci.Substring(2, 2));
            if (from < 0 || to < 0) return new DenseMove();

            var piece = board.GetPieceAt(from);
            var capturedDense = board.GetDenseTypeAt(to);
            var isPromotion = uci.Length > 4;

            var move = DenseMove.FromPieceType(piece, from, to, capturedDense);

            if (isPromotion)
            {
                var prom = uci[4];
                var promoteTo = prom switch
                {
                    'q' => DenseType.D_QUEEN,
                    'r' => DenseType.D_ROOK,
                    'b' => DenseType.D_BISHOP,
                    'n' => DenseType.D_KNIGHT,
                    _ => DenseType.D_EMPTY
                };
                if (promoteTo == DenseType.D_EMPTY) return new DenseMove();
                move.SetPromoteTo(promoteTo);
            }

            // Castling
            if ((piece == PieceType.W_KING && from == 4 && (to == 2 || to == 6)) ||
                (piece == PieceType.B_KING && from == 60 && (to == 58 || to == 62)))
            {
                move.SetCastle(true);
            }

            // En passant
            if ((piece == PieceType.W_PAWN || piece == PieceType.B_PAWN) &&
                 to == board.CurrentGameState.EnPassantSquare)
            {
                move.SetEnPass(true);
                move.SetCapture(DenseType.D_PAWN);
            }

            return move;
        }
    }
}
