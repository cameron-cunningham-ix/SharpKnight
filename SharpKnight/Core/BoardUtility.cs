namespace SharpKnight.Core
{
    /// <summary>
    /// Board Utility class.
    /// Contains bitmasks for files, ranks, and integer constants for squares (A1..H8),
    /// plus small helpers to map an index to its rank/file and masks.
    /// </summary>
    public static class BUTIL
    {
        // ---- Small helpers ----
        /// <summary> Rank of index (0..7), horizontal rows from white’s side. </summary>
        public static int IndexToRank(int index) => index / 8;
        /// <summary> File of index (0..7), vertical columns from a..h. </summary>
        public static int IndexToFile(int index) => index % 8;
        /// <summary> Bitmask of the index’s rank. </summary>
        public static UInt64 IndexRankMask(int index) => RankMask << ((index / 8) * 8);
        /// <summary> Bitmask of the index’s file. </summary>
        public static U64 IndexFileMask(int index) => FileMask << (index % 8);

        // ---- Rank masks ----
        public const U64 RankMask = 0xFFUL;             
        public const U64 Rank1   = 0x00000000000000FFUL;
        public const U64 Rank2   = 0x000000000000FF00UL;
        public const U64 Rank3   = 0x0000000000FF0000UL;
        public const U64 Rank4   = 0x00000000FF000000UL;
        public const U64 Rank5   = 0x000000FF00000000UL;
        public const U64 Rank6   = 0x0000FF0000000000UL;
        public const U64 Rank7   = 0x00FF000000000000UL;
        public const U64 Rank8   = 0xFF00000000000000UL;

        // ---- File masks ----
        public const U64 FileMask = 0x0101010101010101UL;
        public const U64 FileA    = 0x0101010101010101UL;
        public const U64 FileB    = 0x0202020202020202UL;
        public const U64 FileC    = 0x0404040404040404UL;
        public const U64 FileD    = 0x0808080808080808UL;
        public const U64 FileE    = 0x1010101010101010UL;
        public const U64 FileF    = 0x2020202020202020UL;
        public const U64 FileG    = 0x4040404040404040UL;
        public const U64 FileH    = 0x8080808080808080UL;

        // Light/Dark square masks
        public const U64 LightSquareMask = 0xAA55AA55AA55AA55UL;
        public const U64 DarkSquareMask = 0x55AA55AA55AA55AAUL;

        // ---- Castling masks (squares between king and rook) ----
        public const U64 W_ShortCastleMask = 0x0000000000000060UL; // f1,g1  
        public const U64 W_LongCastleMask  = 0x000000000000000EUL; // b1,c1,d1
        public const U64 B_ShortCastleMask = 0x6000000000000000UL; // f8,g8
        public const U64 B_LongCastleMask  = 0x0E00000000000000UL; // b8,c8,d8

        // ---- Square indices (A1 = 0 .. H8 = 63) ----
        public const int A1 = 0,  B1 = 1,  C1 = 2,  D1 = 3,  E1 = 4,  F1 = 5,  G1 = 6,  H1 = 7;
        public const int A2 = 8,  B2 = 9,  C2 = 10, D2 = 11, E2 = 12, F2 = 13, G2 = 14, H2 = 15;
        public const int A3 = 16, B3 = 17, C3 = 18, D3 = 19, E3 = 20, F3 = 21, G3 = 22, H3 = 23;
        public const int A4 = 24, B4 = 25, C4 = 26, D4 = 27, E4 = 28, F4 = 29, G4 = 30, H4 = 31;
        public const int A5 = 32, B5 = 33, C5 = 34, D5 = 35, E5 = 36, F5 = 37, G5 = 38, H5 = 39;
        public const int A6 = 40, B6 = 41, C6 = 42, D6 = 43, E6 = 44, F6 = 45, G6 = 46, H6 = 47;
        public const int A7 = 48, B7 = 49, C7 = 50, D7 = 51, E7 = 52, F7 = 53, G7 = 54, H7 = 55;
        public const int A8 = 56, B8 = 57, C8 = 58, D8 = 59, E8 = 60, F8 = 61, G8 = 62, H8 = 63;
    }
}
