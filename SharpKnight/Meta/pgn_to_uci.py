import chess
import chess.pgn
import re

def pgn_to_uci(fen: str, pgn_str: str) -> str:
    """
    Convert a PGN string with engine evaluations into a UCI move sequence.
    
    Args:
        fen (str): The FEN string of the starting position.
        pgn_str (str): The PGN string containing moves and evaluations.
    
    Returns:
        str: A space-separated string of UCI moves.
    """
    # Refined regex to match SAN moves while excluding numerical evaluations
    san_moves = re.findall(r'\b(?:[NBRQK]?[a-h]?[1-8]?x?[a-h][1-8](?:=[NBRQ])?[+#]?)\b', pgn_str)

    # Initialize the board with the given FEN
    board = chess.Board(fen)

    uci_moves = []
    
    for move in san_moves:
        try:
            san_move = board.parse_san(move)
            uci_moves.append(san_move.uci())
            board.push(san_move)
        except ValueError:
            print(f"Skipping invalid move: {move}")
    
    return " ".join(uci_moves)

# Example usage
fen = "r1bqk2r/p1pnn1b1/1p1pp1pp/8/2PPp3/2N1BN2/PP1QBPPP/R4RK1 w kq - 0 1"
pgn_str = """1. Nh4 {+3.83/6 0.70s} e5 {-8.41/6 0.82s} 2. Nxg6 {+4.14/6 0.72s}
Nxg6 {-1.70/7 0.47s} 3. dxe5 {+1.70/6 0.38s} Bxe5 {-1.36/7 1.2s}
4. Bh5 {+1.36/6 0.91s} Qh4 {-1.28/7 0.77s} 5. Bxg6+ {+1.18/6 0.18s}
Ke7 {-2.64/7 0.99s} 6. Nd5+ {+2.54/6 0.21s} Kf8 {-3.45/7 0.83s}
7. f4 {+3.35/6 0.26s} exf3 {-1.85/7 0.49s} 8. Rxf3+ {+5.42/6 0.28s}
Kg7 {-5.96/7 0.59s} 9. Be4 {+5.96/6 0.41s} Nc5 {-13.55/6 0.43s}
10. Bxc5 {+5.82/6 0.23s} dxc5 {-12.83/6 0.35s} 11. Ne3 {+12.73/5 0.30s}
Rd8 {-4.83/7 0.60s} 12. Qe1 {+4.83/6 0.22s}
Qh5 {-5.18/7 0.51s, White makes an illegal move: f4e5} 0-1"""

uci_move_sequence = pgn_to_uci(fen, pgn_str)
print("UCI Moves:", uci_move_sequence)
