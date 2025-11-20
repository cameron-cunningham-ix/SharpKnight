using SharpKnight.Core;

namespace SharpKnight.Players
{
    /// <summary>
    /// Interface that all player types must implement.
    /// </summary>
    public interface IPlayer
    {
        /// <summary>
        /// Get a move from this player given the current game state.
        /// Should return the chosen move; throw if no valid move can be made.
        /// </summary>
        DenseMove GetMove(ChessBoard board, ChessClock clock);

        /// <summary>
        /// Notify the player of the opponent's move (useful for engines to update internal state).
        /// </summary>
        void NotifyOpponentMove(DenseMove move);

        /// <summary>Get the player's displayed name.</summary>
        string GetName();

        /// <summary>Get the type of player (human, engine, network).</summary>
        PlayerType GetType();

        /// <summary>Return true if this player accepts draw offers.</summary>
        bool AcceptsDraw();

        /// <summary>Called when the game ends to allow cleanup of resources.</summary>
        void OnGameEnd();

        /// <summary>
        /// Optional UCI support flag. Default false.
        /// </summary>
        bool SupportsUCI => false;
    }

    /// <summary>
    /// Enum to identify the type of player.
    /// </summary>
    public enum PlayerType
    {
        Human,
        Engine,
        Network
    }
}
