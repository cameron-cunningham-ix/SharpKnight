# Running the script:
# py sprt_test.py /path/to/engine1 /path/to/engine2 --cutechess /path/to/cutechess-cli (# of games)

import subprocess
import argparse

def run_sprt_test(engine1_path, engine2_path, fastchess_cli_path, elo0=5, elo1=15, alpha=0.05, beta=0.05, games=1000):
    """
    Runs an SPRT match between two chess engines using fastchess.
    
    :param engine1_path: Path to the first chess engine.
    :param engine2_path: Path to the second chess engine.
    :param cutechess_cli_path: Path to fastchess executable.
    :param elo0: Lower bound of the ELO difference hypothesis.
    :param elo1: Upper bound of the ELO difference hypothesis.
    :param alpha: Type I error probability.
    :param beta: Type II error probability.
    :param games: Maximum number of games to play.
    """
    
    cmd = [
        fastchess_cli_path,
        "-engine", f"cmd={engine1_path}", "name=Engine1",
        "-engine", f"cmd={engine2_path}", "name=Engine2",
        "-each", "tc=40/10+0.1", "proto=uci",  # 40 moves in 10s with 0.1s increment, UCI
        "-tournament", "roundrobin",
        "-games", str(games),
        "-concurrency", "2",  # Adjust based on your CPU
        "-sprt", f"elo0={elo0}", f"elo1={elo1}", f"alpha={alpha}", f"beta={beta}",
        "-ratinginterval", "10",
        # "-outcomeinterval", "10",
        "-openings", "file=C:/Users/bluej/SoftDevProjects/GitHub/KnightEngine/meta/2500_positions.epd", "format=epd", "order=random",
        "-pgnout", "sprt_results.pgn"
    ]

    # Run the SPRT match
    process = subprocess.run(cmd, capture_output=True, text=True)

    # Print results
    print("SPRT Match Results:")
    print(process.stdout)
    print("SPRT Match Errors (if any):")
    print(process.stderr)

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Run SPRT test between two chess engines.")
    parser.add_argument("engine1", help="Path to the first chess engine.")
    parser.add_argument("engine2", help="Path to the second chess engine.")
    parser.add_argument("games", help="Maximum number of games to play")
    parser.add_argument("--fastchess", default="fastchess", help="Path to fastchess executable.")
    
    args = parser.parse_args()
    
    run_sprt_test(args.engine1, args.engine2, args.fastchess, games=args.games)
