## Running the script:
#

import subprocess
import random
import json
import numpy as np
import argparse
from dataclasses import dataclass
from typing import List, Dict, Any

@dataclass
class Parameter:
    """Represents a tunable parameter with its constraints"""
    name: str
    min_value: float
    max_value: float
    default_value: float
    is_integer: bool = False

    def encode(self, value: float) -> List[bool]:
        """
        Encode a parameter value into a 16-bit binary representation.

        The value is normalized between 0 and 1 and then scaled to a 16-bit
        integer representation before being converted into binary.
        """
        normalized = (value - self.min_value) / (self.max_value - self.min_value)
        binary = format(int(normalized * 65535), '016b')
        return [bit == '1' for bit in binary]

    def decode(self, bits: List[bool]) -> float:
        """
        Decode a binary representation back into a floating-point parameter value.

        The binary is converted to an integer, then scaled back to the original range.
        """
        binary_str = ''.join('1' if bit else '0' for bit in bits)
        normalized = int(binary_str, 2) / 65535
        value = self.min_value + normalized * (self.max_value - self.min_value)
        return round(value) if self.is_integer else value

class PBIL:
    """Population-Based Incremental Learning (PBIL) algorithm for tuning engine parameters."""

    def __init__(self, parameters: List[Parameter], learn_rate: float = 0.1, neg_learn_rate: float = 0.075, mut_prob: float = 0.02, mut_shift: float = 0.05):
        """
        Initialize PBIL algorithm with tunable parameters and learning parameters.

        :param parameters: List of tunable parameters.
        :param learn_rate: Learning rate for probability vector updates.
        :param neg_learn_rate: Negative learning rate for adjusting bad solutions.
        :param mut_prob: Mutation probability per bit.
        :param mut_shift: Mutation shift amount.
        """
        self.parameters = parameters
        self.total_bits = len(parameters) * 16
        self.prob_vector = np.full(self.total_bits, 0.5)
        self.learn_rate = learn_rate
        self.neg_learn_rate = neg_learn_rate
        self.mut_prob = mut_prob
        self.mut_shift = mut_shift

    def generate_population(self, size: int) -> List[List[bool]]:
        """Generate a population of individuals based on the probability vector."""
        return [[random.random() < p for p in self.prob_vector] for _ in range(size)]

    def decode_individual(self, individual: List[bool]) -> Dict[str, float]:
        """Decode an individual's binary representation into a dictionary of parameter values."""
        return {param.name: param.decode(individual[i * 16:(i + 1) * 16]) for i, param in enumerate(self.parameters)}

    def update_probabilities(self, best: List[bool], worst: List[bool]):
        """
        Update probability vector based on best and worst performing individuals.

        If the best and worst individuals agree on a bit, a normal learning update is applied.
        If they disagree, a more aggressive update is performed to emphasize learning from the best.
        """
        for i in range(self.total_bits):
            if best[i] == worst[i]:
                self.prob_vector[i] = (self.prob_vector[i] * (1 - self.learn_rate) + best[i] * self.learn_rate)
            else:
                learn_rate2 = self.learn_rate + self.neg_learn_rate
                self.prob_vector[i] = (self.prob_vector[i] * (1 - learn_rate2) + best[i] * learn_rate2)
        
        # Apply mutations
        for i in range(self.total_bits):
            if random.random() < self.mut_prob:
                self.prob_vector[i] = (self.prob_vector[i] * (1 - self.mut_shift) + random.choice([0, 1]) * self.mut_shift)

class EngineEvaluator:
    """Handles engine evaluation using CuteChess for automated parameter tuning."""

    def __init__(self, engine_path: str, cutechess_cli_path: str):
        """
        Initialize the engine evaluator with paths to the chess engine and CuteChess CLI.

        Extracts UCI parameters to determine what can be tuned.
        """
        self.engine_path = engine_path
        self.cutechess_cli = cutechess_cli_path
        self.parameters = self.extract_uci_parameters()
        self.default_params = {param.name: param.default_value for param in self.parameters}

    def extract_uci_parameters(self) -> List[Parameter]:
        """Extract UCI parameter constraints and default values"""
        try:
            process = subprocess.Popen([self.engine_path], stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
            process.stdin.write("uci\n")
            process.stdin.flush()
            parameters = []
            while True:
                line = process.stdout.readline().strip()
                if line.startswith("option name"):
                    parts = line.split()
                    name = parts[2]
                    default_val = None
                    min_val = None
                    max_val = None
                    if "spin" in line:
                        min_val = int(parts[parts.index("min") + 1])
                        max_val = int(parts[parts.index("max") + 1])
                        default_val = int(parts[parts.index("default") + 1])
                        parameters.append(Parameter(name, min_val, max_val, default_val, True))
                elif line == "uciok":
                    break
            process.stdin.write("quit\n")
            process.stdin.flush()
            process.terminate()
            process.wait(timeout=1)
            return parameters
        except Exception as e:
            print(f"Error extracting parameters: {e}")
            return []

    def parse_tournament_results(self, output: str) -> List[float]:
        """Extract engine win percentages from tournament output"""
        lines = output.strip().split('\n')
        rankings_start = next((i for i, line in enumerate(lines) if line.startswith("Rank Name")), -1)
        if rankings_start == -1:
            print("Error: Could not find rankings in tournament output")
            return []
        scores = []
        i = rankings_start + 1
        while i < len(lines) and lines[i].strip():
            parts = lines[i].split()
            try:
                score = float(parts[-2].rstrip('%'))
                scores.append(score / 100.0)
            except (IndexError, ValueError):
                print(f"Error parsing score from line: {lines[i]}")
            i += 1
        return scores

    def run_tournament(self, configs: List[Dict[str, float]], games_per_encounter: int = 4) -> List[float]:
        """
        Run a round-robin tournament with different parameter configurations.

        :param configs: List of engine configurations (parameter settings).
        :param games_per_encounter: Number of games played per encounter.
        :return: List of win percentages for each configuration.
        """
        cmd = [
            self.cutechess_cli, "-tournament", "round-robin", "-games", str(games_per_encounter), "-repeat",
            "-concurrency", "2", "-each", "tc=40/10+0.01", "proto=uci", "-pgnout", "engine_tuning_results.pgn",
            "-openings", "file=D:/dev/SoftDevProjects/GitHub/KnightEngine/meta/2500_positions.epd", "format=epd", "order=random", "plies=1"
        ]
        for i, params in enumerate(configs):
            cmd.extend(["-engine", f"name=Engine{i}", f"cmd=\"{self.engine_path}\""] + [f"option.{name}={value}" for name, value in params.items()])
        try:
            result = subprocess.run(cmd, capture_output=True, text=True, check=True)
            return self.parse_tournament_results(result.stdout)
        except subprocess.CalledProcessError as e:
            print(f"Tournament failed: {e}")
            return [0.0] * len(configs)

def main():
    """Runs the parameter tuning process"""
    parser = argparse.ArgumentParser()
    parser.add_argument("engine_path", type=str, help="Path to the engine executable")
    parser.add_argument("cutechess_cli_path", type=str, help="Path to CuteChess CLI executable")
    args = parser.parse_args()
    
    evaluator = EngineEvaluator(args.engine_path, args.cutechess_cli_path)
    parameters = evaluator.parameters
    pbil = PBIL(parameters)
    
    results_file = "parameter_tuning_log.json"
    with open(results_file, "w") as f:
        json.dump([], f)

    for generation in range(10):
        population = pbil.generate_population(3)  # 3 evolved individuals + 1 default
        configurations = [pbil.decode_individual(ind) for ind in population]
        configurations.insert(0, evaluator.default_params)  # Insert default parameters as Engine0

        fitness_scores = evaluator.run_tournament(configurations)
        sorted_indices = np.argsort(fitness_scores)[::-1]
        best_index = sorted_indices[0]

        best_params = configurations[best_index]
        best_score = fitness_scores[best_index]
        default_score = fitness_scores[0]  # First engine is the default one

        with open(results_file, "r+") as f:
            data = json.load(f)
            data.append({"generation": generation, "parameters": best_params, "score": best_score, "default_score": default_score})
            f.seek(0)
            json.dump(data, f, indent=2)

        print(f"Generation {generation}: Best Score = {best_score}, Default Score = {default_score}")
        print(f"Best Parameters: {best_params}")

if __name__ == "__main__":
    main()
