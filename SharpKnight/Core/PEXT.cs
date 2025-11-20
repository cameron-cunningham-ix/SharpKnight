using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace SharpKnight.Core
{
    /// <summary>
    /// Static class for handling PEXT/PDEP operations for distance attackers.
    /// </summary>
    public static class PEXT
    {
        /// <summary>
        /// True if PEXT has already been initialized.
        /// </summary>
        public static bool Initialized { get; private set; }

        // Attack masks for each square (relevant occupancy areas; edges excluded)
        /// <summary>
        /// Attack masks for rooks on each square.
        /// </summary>
        private static readonly ulong[] rookMasks = new ulong[64];
        /// <summary>
        /// Attack masks for bishops on each square.
        /// </summary>
        private static readonly ulong[] bishopMasks = new ulong[64];

        /// <summary>
        /// Precomputed rook move tables indexed by PEXT(occupancy, mask)
        /// For each square, the inner array length is 1 << popcount(mask)
        /// </summary>
        private static ulong[][] rookMoves = new ulong[64][];
        /// <summary>
        /// Precomputed bishop move tables indexed by PEXT(occupancy, mask)
        /// For each square, the inner array length is 1 << popcount(mask)
        /// </summary>
        private static ulong[][] bishopMoves = new ulong[64][];

        /// <summary>
        /// Lock for the possibility of two engine or board instances trying to instantiate.
        /// </summary>
        private static readonly object initLock = new object();

        /// <summary>
        /// Initializes PEXT by generating attack masks and move tables.
        /// </summary>
        public static void Initialize()
        {
            if (Initialized) return;
            lock (initLock)
            {
                if (Initialized) return;

                for (int sq = 0; sq < 64; sq++)
                {
                    // Build relevant masks (exclude board edges)
                    rookMasks[sq] = GenerateRookMask(sq);
                    bishopMasks[sq] = GenerateBishopMask(sq); // edges excluded like the C++ version

                    int rBits = BitOperations.PopCount(rookMasks[sq]);
                    int bBits = BitOperations.PopCount(bishopMasks[sq]);

                    rookMoves[sq] = new ulong[1 << rBits];
                    bishopMoves[sq] = new ulong[1 << bBits];

                    // For every subset of the relevant mask, expand occ with PDEP and precompute
                    // rook/bishop sliding attacks at that square under that occupancy.
                    for (ulong occ = 0; occ < (1UL << rBits); occ++)
                    {
                        ulong actualOcc = ParallelBitDeposit(occ, rookMasks[sq]);
                        rookMoves[sq][occ] = GenerateRookAttacks(sq, actualOcc);
                    }

                    for (ulong occ = 0; occ < (1UL << bBits); occ++)
                    {
                        ulong actualOcc = ParallelBitDeposit(occ, bishopMasks[sq]);
                        bishopMoves[sq][occ] = GenerateBishopAttacks(sq, actualOcc);
                    }
                }

                Initialized = true;
            }
        }

        /// <summary>
        /// Return rook sliding attacks for a square under a given full-board occupancy.
        /// Fast lookup via PEXT into the precomputed table.
        /// </summary>
        public static ulong GetRookAttacks(int square, ulong occupancy)
        {
            ulong index = ParallelBitExtract(occupancy, rookMasks[square]);
            return rookMoves[square][index];
            
        }

        /// <summary>
        /// Return bishop sliding attacks for a square under a given full-board occupancy.
        /// Fast lookup via PEXT into the precomputed table.
        /// </summary>
        public static ulong GetBishopAttacks(int square, ulong occupancy)
        {
            ulong index = ParallelBitExtract(occupancy, bishopMasks[square]);
            return bishopMoves[square][index];
        }

        // =========================
        // Mask builders (edges off)
        // =========================
        /// <summary>
        /// Generate the rook attack mask for 'square'.
        /// </summary>
        /// <param name="square"></param>
        /// <returns></returns>
        private static ulong GenerateRookMask(int square)
        {
            // Union of rank/file excluding edges and excluding the square itself.
            ulong attacks = 0UL;

            // Rank mask for this rank, with edge squares removed (bits 1..6 on that rank)
            // 0x7E = 0b01111110; shift it up to the correct rank.
            attacks |= 0x000000000000007EUL << (8 * (square / 8));

            // File mask for this file, with edges removed (bits 1..6 in that file)
            // 0x01010101010100 has 1s on file A rows 1..6; shift by file to position.
            attacks |= 0x0001010101010100UL << (square % 8);

            // Remove the square itself
            attacks &= ~(1UL << square);
            return attacks;
        }

        /// <summary>
        /// Generate the bishop attack mask for 'square'.
        /// </summary>
        /// <param name="square"></param>
        /// <returns></returns>
        private static ulong GenerateBishopMask(int square)
        {
            // Walk each diagonal and exclude edge squares
            ulong attacks = 0UL;
            int r = square / 8, f = square % 8;

            // NE (up + right), exclude the final edge square
            int rr = r + 1, ff = f + 1;
            while (rr < 8 && ff < 8) { attacks |= 1UL << (rr * 8 + ff); rr++; ff++; }
            if (rr - 1 >= 0 && ff - 1 >= 0) attacks &= ~(1UL << ((rr - 1) * 8 + (ff - 1)));

            // SE (down + right)
            rr = r - 1; ff = f + 1;
            while (rr >= 0 && ff < 8) { attacks |= 1UL << (rr * 8 + ff); rr--; ff++; }
            if (rr + 1 < 8 && ff - 1 >= 0) attacks &= ~(1UL << ((rr + 1) * 8 + (ff - 1)));

            // SW (down + left)
            rr = r - 1; ff = f - 1;
            while (rr >= 0 && ff >= 0) { attacks |= 1UL << (rr * 8 + ff); rr--; ff--; }
            if (rr + 1 < 8 && ff + 1 < 8) attacks &= ~(1UL << ((rr + 1) * 8 + (ff + 1)));

            // NW (up + left)
            rr = r + 1; ff = f - 1;
            while (rr < 8 && ff >= 0) { attacks |= 1UL << (rr * 8 + ff); rr++; ff--; }
            if (rr - 1 >= 0 && ff + 1 < 8) attacks &= ~(1UL << ((rr - 1) * 8 + (ff + 1)));

            return attacks;
        }

        // =========================
        // Sliding attack generators
        // =========================
        /// <summary>
        /// Generate all legal rook attacks from 'square' given occupancy.
        /// </summary>
        /// <param name="square"></param>
        /// <param name="occupancy"></param>
        /// <returns></returns>
        private static ulong GenerateRookAttacks(int square, ulong occupancy)
        {
            // March in 4 orthogonal directions until the first blocker (inclusive),
            // Collecting all intermediate squares
            ulong attacks = 0UL;
            int r = square / 8, f = square % 8;

            // North (increasing rank)
            for (int rr = r + 1; rr < 8; rr++)
            {
                int sq = rr * 8 + f;
                attacks |= 1UL << sq;
                if (((occupancy >> sq) & 1UL) != 0) break;
            }

            // South (decreasing rank)
            for (int rr = r - 1; rr >= 0; rr--)
            {
                int sq = rr * 8 + f;
                attacks |= 1UL << sq;
                if (((occupancy >> sq) & 1UL) != 0) break;
            }

            // East (increasing file)
            for (int ff = f + 1; ff < 8; ff++)
            {
                int sq = r * 8 + ff;
                attacks |= 1UL << sq;
                if (((occupancy >> sq) & 1UL) != 0) break;
            }

            // West (decreasing file)
            for (int ff = f - 1; ff >= 0; ff--)
            {
                int sq = r * 8 + ff;
                attacks |= 1UL << sq;
                if (((occupancy >> sq) & 1UL) != 0) break;
            }

            return attacks;
        }

        /// <summary>
        /// Generate all legal bishop attacks from 'square' given occupancy.
        /// </summary>
        /// <param name="square"></param>
        /// <param name="occupancy"></param>
        /// <returns></returns>
        private static ulong GenerateBishopAttacks(int square, ulong occupancy)
        {
            // March in 4 diagonals until first blocker (inclusive)
            ulong attacks = 0UL;
            int r = square / 8, f = square % 8;

            // NE
            for (int rr = r + 1, ff = f + 1; rr < 8 && ff < 8; rr++, ff++)
            {
                int sq = rr * 8 + ff;
                attacks |= 1UL << sq;
                if (((occupancy >> sq) & 1UL) != 0) break;
            }

            // SE
            for (int rr = r - 1, ff = f + 1; rr >= 0 && ff < 8; rr--, ff++)
            {
                int sq = rr * 8 + ff;
                attacks |= 1UL << sq;
                if (((occupancy >> sq) & 1UL) != 0) break;
            }

            // SW
            for (int rr = r - 1, ff = f - 1; rr >= 0 && ff >= 0; rr--, ff--)
            {
                int sq = rr * 8 + ff;
                attacks |= 1UL << sq;
                if (((occupancy >> sq) & 1UL) != 0) break;
            }

            // NW
            for (int rr = r + 1, ff = f - 1; rr < 8 && ff >= 0; rr++, ff--)
            {
                int sq = rr * 8 + ff;
                attacks |= 1UL << sq;
                if (((occupancy >> sq) & 1UL) != 0) break;
            }

            return attacks;
        }

        /// <summary>
        /// Wrapper for PEXT operation; chooses best available PEXT implementation on system.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ParallelBitExtract(ulong src, ulong mask)
        {
            if (Bmi2.X64.IsSupported)
            {
                return Bmi2.X64.ParallelBitExtract(src, mask);
            }
            else if (Bmi2.IsSupported)
            {
                return Pext64Via32(src, mask);
            }
            
            return Pext64Software(src, mask);
        }

        /// <summary>
        /// Wrapper for PDEP operation; chooses best available PDEP implementation on system.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ParallelBitDeposit(ulong src, ulong mask)
        {
            if (Bmi2.X64.IsSupported)
            {
                return Bmi2.X64.ParallelBitDeposit(src, mask);
            }
            else if (Bmi2.IsSupported)
            {
                return Pdep64Via32(src, mask);
            }

            return Pdep64Software(src, mask);
        }

        /// <summary>
        /// Performs 64-bit PEXT operation using 32-bit PEXT.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Pext64Via32(ulong src, ulong mask)
        {
            // Fallback using 32-bit
            uint srcLow = (uint)src;
            uint srcHigh = (uint)(src >> 32);
            uint maskLow = (uint)mask;
            uint maskHigh = (uint)(mask >> 32);

            uint outLow = Bmi2.ParallelBitExtract(srcLow, maskLow);
            uint outHigh = Bmi2.ParallelBitExtract(srcHigh, maskHigh);

            int countLow = (int)Popcnt.PopCount(maskLow);

            return (ulong)countLow | (ulong)(outHigh << countLow);
        }

        /// <summary>
        /// Performs 64-bit PDEP operation using 32-bit PDEP.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Pdep64Via32(ulong src, ulong mask)
        {
            // Fallback using 32-bit
            uint maskLow = (uint)mask;
            uint maskHigh = (ushort)(mask >> 32);

            // Low half: consume the first popcnt(maskLow) bits of src
            uint destLow = Bmi2.ParallelBitDeposit((uint)src, maskLow);

            // How many bits consumed by maskLow
            int cons = (int)Popcnt.PopCount(maskLow);

            // High hald: consume the next bits from src >> cons
            uint srcHigh = (uint)(src >> cons);
            uint destHigh = Bmi2.ParallelBitDeposit(srcHigh, maskHigh);

            // Combine
            return (ulong)destLow | (ulong)(destHigh << 32);
        }

        /// <summary>
        /// Performs PEXT operation using software implementation.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        private static ulong Pext64Software(ulong src, ulong mask)
        {
            // Software fallback
            ulong result = 0;
            ulong outBit = 1;

            while (mask != 0)
            {
                ulong lsb = mask & (ulong)-(long)mask; // lowest set bit
                if ((src & lsb) != 0)
                    result |= outBit;

                mask ^= lsb;
                outBit <<= 1;
            }

            return result;
        }

        /// <summary>
        /// Performs PDEP operation using software implementation.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        private static ulong Pdep64Software(ulong src, ulong mask)
        {
            // Software fallback
            ulong result = 0;
            ulong inBit = 1;

            while (mask != 0)
            {
                ulong lsb = mask & (ulong)-(long)mask;
                if ((src & inBit) != 0)
                    result |= lsb;

                mask ^= lsb;
                inBit <<= 1;
            }

            return result;
        }
    }
}
