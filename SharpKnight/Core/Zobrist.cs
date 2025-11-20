// Thanks to Bruce Moreland via http://www.brucemo.com/compchess/programming/zobrist.htm

using System;
using System.Runtime.CompilerServices;

namespace SharpKnight.Core
{
    /// <summary>
    /// Helper class for generating and using Zobrist keys.
    /// Zobrist keys are 64-bit numbers that represent a position on the board.
    /// </summary>
    public static class Zobrist
    {

        private const ulong RNG_SEED = 917346853UL; // DO NOT CHANGE

        // Public fields
        /// <summary>
        /// True if PEXT has already been initialized.
        /// </summary>
        public static bool Initialized { get; private set; } = false;

        // 64 squares * 12 piece types (6 per color) = 768
        public static readonly ulong[] ZobristPieces = new ulong[768];

        // Side to move (XOR this when the side-to-move flips)
        public static ulong ZobristSideToMove;

        // 16 possibilities (bitmask over KQkq); index with your GameState.getCastleRights()
        public static readonly ulong[] ZobristCastle = new ulong[16];

        // 8 files (a..h) for en passant; index with file of ep square
        public static readonly ulong[] ZobristEnPass = new ulong[8];

        /// <summary>
        /// Generate random 64-bit numbers for all Zobrist tables.
        /// Call once at program start, before creating boards.
        /// </summary>
        public static void Initialize()
        {
            if (Initialized) return;

            // SplitMix64 — small, fast 64-bit deterministc RNG
            var rng = new SplitMix64(RNG_SEED);

            // Per-square / per-color / per-type keys
            for (int square = 0; square < 64; square++)
            {
                for (int color = 0; color < 2; color++)
                {
                    for (int type = 0; type < 6; type++)
                    {
                        ZobristPieces[(square * 12) + (color * 6) + type] = rng.NextULong();
                    }
                }
            }

            // Castling rights
            for (int i = 0; i < 16; i++)
                ZobristCastle[i] = rng.NextULong();

            // En-passant files
            for (int i = 0; i < 8; i++)
                ZobristEnPass[i] = rng.NextULong();

            // Side-to-move
            ZobristSideToMove = rng.NextULong();

            Initialized = true;
        }

        /// <summary>
        /// Return the piece-square key used for XORing into the position hash.
        /// </summary>
        /// <param name="sq">0..63</param>
        /// <param name="piece">Concrete colored piece (e.g., W_ROOK, B_KNIGHT)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetPieceSqKey(int sq, PieceType piece)
        {
            int color = ZTypes.ColorCode(piece);
            int dtype = ZTypes.PieceCode(piece); // returns 1..6

            return ZobristPieces[(sq * 12) + (color * 6) + (dtype - 1)];
        }

        // ====== Minimal, deterministic 64-bit RNG (SplitMix64) ======
        private struct SplitMix64
        {
            private ulong _state;

            public SplitMix64(ulong seed)
            {
                _state = seed;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ulong NextULong()
            {
                ulong z = (_state += 0x9E3779B97F4A7C15UL);
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }
    }

    internal static class ZTypes
    {
        // Return 0 for white, 1 for black
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ColorCode(PieceType p)
            => (p >= PieceType.B_PAWN && p <= PieceType.B_KING) ? 1 : 0;

        // Dense code: pawn..king -> 1..6
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PieceCode(PieceType p)
        {
            switch (p)
            {
                case PieceType.W_PAWN:
                case PieceType.B_PAWN:   return 1;
                case PieceType.W_KNIGHT:
                case PieceType.B_KNIGHT: return 2;
                case PieceType.W_BISHOP:
                case PieceType.B_BISHOP: return 3;
                case PieceType.W_ROOK:
                case PieceType.B_ROOK:   return 4;
                case PieceType.W_QUEEN:
                case PieceType.B_QUEEN:  return 5;
                case PieceType.W_KING:
                case PieceType.B_KING:   return 6;
                default:                 return 0; // EMPTY/invalid not used here
            }
        }
    }
}
