using NUnit.Framework;
using SharpKnight.Core;

namespace SharpKnight.Tests
{
    [TestFixture]
    public class PextTests
    {
        private readonly Random _rng = new Random(1337); // deterministic

        [SetUp]
        public void SetUp()
        {
            PEXT.Initialize();
        }

        /// <summary>Generate a random 64-bit value.</summary>
        private ulong RandomU64()
        {
            // Compose 4 x 16-bit chunks
            ulong u1 = (ulong)(_rng.Next() & 0xFFFF);
            ulong u2 = (ulong)(_rng.Next() & 0xFFFF);
            ulong u3 = (ulong)(_rng.Next() & 0xFFFF);
            ulong u4 = (ulong)(_rng.Next() & 0xFFFF);
            return u1 | (u2 << 16) | (u3 << 32) | (u4 << 48);
        }

        /// <summary>Fewer bits set.</summary>
        private ulong RandomU64FewBits()
        {
            return RandomU64() & RandomU64() & RandomU64();
        }

        [Test]
        public void RookAttacks()
        {
            for (int square = 0; square < 64; square++)
            {
                ulong occupancy = RandomU64FewBits();
                Console.WriteLine("occupancy:\n");
                Utility.PrintBitboard(occupancy);

                ulong attacks = PEXT.GetRookAttacks(square, occupancy);
                Console.WriteLine("attacks:\n");
                Utility.PrintBitboard(attacks);
            }
        }

        [Test]
        public void BishopAttacks()
        {
            for (int square = 0; square < 64; square++)
            {
                ulong occupancy = RandomU64FewBits();
                Console.WriteLine("occupancy:\n");
                Utility.PrintBitboard(occupancy);

                ulong attacks = PEXT.GetBishopAttacks(square, occupancy);
                Console.WriteLine("attacks:\n");
                Utility.PrintBitboard(attacks);
            }
        }
    }

}
