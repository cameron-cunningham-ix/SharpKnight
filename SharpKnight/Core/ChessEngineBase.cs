using SharpKnight.Core;
using System;
using System.Collections.Generic;

namespace SharpKnight.Core
{
    /// <summary>
    /// Base class for all chess engine implementations.
    /// </summary>
    public abstract class ChessEngineBase
    {
        // Basic identity
        /// <summary>
        /// Name of the engine implementation.
        /// </summary>
        protected string engineName;
        /// <summary>
        /// Version of the engine.
        /// </summary>
        protected string engineVersion;
        /// <summary>
        /// Author of the engine.
        /// </summary>
        protected string engineAuthor;

        // Search state
        /// <summary>
        /// Whether engine is currently searching.
        /// </summary>
        protected volatile bool isSearching;
        /// <summary>
        /// Best move found in last search.
        /// </summary>
        protected DenseMove bestMove;
        /// <summary>
        /// Ponder move from last search.
        /// </summary>
        protected DenseMove ponderMove;

        // Depth/time configuration
        /// <summary>
        /// Default search depth for the engine.
        /// </summary>
        protected int defaultDepth;
        /// <summary>
        /// Current search depth for the engine.
        /// </summary>
        protected int searchDepth;
        /// <summary>
        /// Minimum time to spend searching a position.
        /// </summary>
        protected TimeSpan minTime;
        /// <summary>
        /// Maximum time to spend searching a position.
        /// </summary>
        protected TimeSpan maxTime;

        /// <summary>
        /// Registered options (UCI-style)
        /// </summary>
        protected readonly Dictionary<string, EngineOption> options =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Construct with basic engine information and defaults.
        /// </summary>
        public ChessEngineBase(
            string name,
            string version,
            string author,
            int defaultDepth = 4,
            TimeSpan? minTime = null,
            TimeSpan? maxTime = null)
        {
            engineName = name;
            engineVersion = version;
            engineAuthor = author;

            isSearching = false;

            this.defaultDepth = defaultDepth;
            searchDepth = defaultDepth;

            this.minTime = minTime ?? TimeSpan.FromMilliseconds(200);
            this.maxTime = maxTime ?? TimeSpan.FromMilliseconds(20000);
        }

        /// <summary>
        /// Find the best move for the given position and clock state.
        /// Must be implemented by derived engines.
        /// </summary>
        public abstract DenseMove FindBestMove(ref ChessBoard board,
                                               ref ChessClock clock,
                                               int maxDepth = -1);

        /// <summary>
        /// Optional: evaluation function. Must be implemented by derived engines.
        /// </summary>
        public abstract int EvaluatePosition(in ChessBoard board);

        /// <summary>
        /// Optional hook for new game; engines can clear internal state.
        /// </summary>
        public virtual void ClearForNewGame() { }

        /// <summary>Stop the current search (if any).</summary>
        public virtual void StopSearch() => isSearching = false;

        /// <summary>Set the target search depth.</summary>
        public virtual void SetSearchDepth(int depth) => searchDepth = depth;

        /// <summary>Reset the search depth to the engine’s default.</summary>
        public virtual void ResetSearchDepth() => searchDepth = defaultDepth;

        /// <summary>
        /// Calculate an appropriate search depth given the current clock.
        /// Override to implement time management; default returns current SearchDepth.
        /// </summary>
        public virtual int CalculateSearchDepth(in ChessClock clock) => searchDepth;

        /// <summary>
        /// Called when an option changes. Override in derived classes to react.
        /// </summary>
        public virtual void OnOptionChanged(in EngineOption option) { }

        /// <summary>
        /// Set a named option’s value. Returns false if not found or value invalid.
        /// </summary>
        public bool SetOption(string name, string value)
        {
            if (!options.TryGetValue(name, out var opt))
                return false;

            // Expect EngineOption to expose SetValue(string) and return bool
            if (opt.SetValue(value))
            {
                options[name] = opt;      // store back if EngineOption is a struct
                OnOptionChanged(opt);     // notify derived engine
                return true;
            }
            return false;
        }

        /// <summary>Engine display name.</summary>
        public string Name => engineName;

        /// <summary>Engine version string.</summary>
        public string Version => engineVersion;

        /// <summary>Engine author.</summary>
        public string Author => engineAuthor;

        /// <summary>True if the engine is currently thinking/searching.</summary>
        public bool IsThinking => isSearching;

        /// <summary>Best move from the last completed search.</summary>
        public DenseMove BestMove => bestMove;

        /// <summary>Ponder move associated with BestMove, if any.</summary>
        public DenseMove PonderMove => ponderMove;

        /// <summary>Current target search depth.</summary>
        public int SearchDepth => searchDepth;

        /// <summary>All registered engine options (read-only view).</summary>
        public IReadOnlyDictionary<string, EngineOption> Options => options;

        // ---------- Protected helpers for derived engines ----------

        /// <summary>Mark search as started.</summary>
        protected virtual void StartSearch() => isSearching = true;

        /// <summary>Mark search as ended.</summary>
        protected virtual void EndSearch() => isSearching = false;

        /// <summary>Set the current best move.</summary>
        protected virtual void SetBestMove(in DenseMove move) => bestMove = move;

        /// <summary>Set the current ponder move.</summary>
        protected virtual void SetPonderMove(in DenseMove move) => ponderMove = move;

        /// <summary>
        /// Register an option with this engine. If an option of the same name exists, it is replaced.
        /// Immediately calls OnOptionChanged so derived engines can sync internal state.
        /// </summary>
        protected void RegisterOption(in EngineOption option)
        {
            options[option.Name] = option;
            OnOptionChanged(option);
        }
    }
}
