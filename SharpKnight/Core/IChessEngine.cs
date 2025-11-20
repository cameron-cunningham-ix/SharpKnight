namespace SharpKnight.Core
{
    /// <summary>
    /// Interface that any chess engines should implement. Requires basic UCI implementation
    /// and engine information.
    /// </summary>
    public interface IChessEngine
    {
        // --- Core UCI commands ---
        /// <summary>
        /// Switch to UCI mode (should print "id", "uciok", etc.).
        /// </summary>
        void Uci();

        /// <summary>
        /// Check if the engine is ready (should respond with "readyok").
        /// </summary>
        void IsReady();

        /// <summary>
        /// Set an engine option (by UCI name/value).
        /// </summary>
        void SetOption(string name, string value);

        /// <summary>
        /// Notify the engine a new game is starting (clear state, tables, etc.).
        /// </summary>
        void UciNewGame();

        /// <summary>
        /// Set the current position, given a FEN and an optional move list (UCI format).
        /// Example: fen="startpos" or an actual FEN; moves="e2e4 e7e5 ..."
        /// </summary>
        void Position(string fen, string moves);
        
        /// <summary>
        /// Start calculating with UCI search parameters (wtime, btime, winc, binc, movestogo, depth, nodes, movetime, ponder, etc.).
        /// </summary>
        void Go(IDictionary<string, string> searchParams);

        /// <summary>
        /// Stop calculating (should produce a bestmove if one is being searched).
        /// </summary>
        void Stop();

        /// <summary>
        /// Quit the engine and release resources.
        /// </summary>
        void Quit();

        // --- Engine status ---
        /// <summary>
        /// True if UCI/engine is initialized.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// True if the engine is currently searching/thinking.
        /// </summary>
        bool IsThinking { get; }

        // --- Engine info ---
        /// <summary>
        /// Engine name to report via UCI.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Engine author to report via UCI.
        /// </summary>
        string Author { get; }

        /// <summary>
        /// Best move from the last completed search.
        /// </summary>
        DenseMove BestMove { get; }

        /// <summary>
        /// Ponder move associated with the best move (optional).
        /// </summary>
        DenseMove PonderMove { get; }
    }
}
