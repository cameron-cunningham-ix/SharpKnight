using System.Text;

namespace SharpKnight
{
    using U32 = UInt32;
    /// <summary>
    /// Global constants
    /// </summary>
    public static class Consts
    {
        /// <summary>
        /// Maximum number of plys in a game. 
        /// </summary>
        public const int MAX_PLY = 512;
        /// <summary>
        /// Maximum number of possible moves in a chess position.
        /// </summary>
        public const int MAX_MOVES = 218;
    }

    // ==== Enums ====

    /// <summary>
    /// 4-bit piece type enum where bit 3 is color (0xxx = white, 1xxx = black).
    /// Lowest 3 bits match with DenseType.
    /// </summary>
    public enum PieceType : int
    {
        EMPTY   = 0,   // 0000
        INVALID = 8,   // 1000 (sentinel)
        W_PAWN  = 1,   // 0001
        W_KNIGHT= 2,   // 0010
        W_BISHOP= 3,   // 0011
        W_ROOK  = 4,   // 0100
        W_QUEEN = 5,   // 0101
        W_KING  = 6,   // 0110
        B_PAWN  = 9,   // 1001
        B_KNIGHT= 10,  // 1010
        B_BISHOP= 11,  // 1011
        B_ROOK  = 12,  // 1100
        B_QUEEN = 13,  // 1101
        B_KING  = 14   // 1110
    }

    /// <summary>
    /// Piece / side color enum.
    /// </summary>
    public enum Color : int
    {
        WHITE = 0,
        BLACK = 1
    }

    /// <summary>
    /// 3-bit dense type enum (no color)
    /// </summary>
    public enum DenseType : int
    {
        D_EMPTY  = 0, // 000
        D_PAWN   = 1, // 001
        D_KNIGHT = 2, // 010
        D_BISHOP = 3, // 011
        D_ROOK   = 4, // 100
        D_QUEEN  = 5, // 101
        D_KING   = 6  // 110
    }

    /// <summary>
    /// Contains functions for converting types to strings.
    /// </summary>
    public static class TypeStrings
    {
        /// <summary>
        /// Convert color enum to corresponding string.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static string ColorToString(Color c) => c == Color.WHITE ? "w" : "b";

        /// <summary>
        /// Convert DenseType enum to corresponding string.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static string DenseTypeToString(DenseType t) => t switch
        {
            DenseType.D_EMPTY  => "EMPTY",
            DenseType.D_PAWN   => "PAWN",
            DenseType.D_KNIGHT => "KNIGHT",
            DenseType.D_BISHOP => "BISHOP",
            DenseType.D_ROOK   => "ROOK",
            DenseType.D_QUEEN  => "QUEEN",
            DenseType.D_KING   => "KING",
            _ => "?"
        };

        /// <summary>
        /// Convert PieceType enum to corresponding string.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static string PieceTypeToString(PieceType p) => p switch
        {
            PieceType.EMPTY     => "EMPTY",
            PieceType.INVALID   => "INVALID",
            PieceType.W_PAWN    => "W_PAWN",
            PieceType.W_KNIGHT  => "W_KNIGHT",
            PieceType.W_BISHOP  => "W_BISHOP",
            PieceType.W_ROOK    => "W_ROOK",
            PieceType.W_QUEEN   => "W_QUEEN",
            PieceType.W_KING    => "W_KING",
            PieceType.B_PAWN    => "B_PAWN",
            PieceType.B_KNIGHT  => "B_KNIGHT",
            PieceType.B_BISHOP  => "B_BISHOP",
            PieceType.B_ROOK    => "B_ROOK",
            PieceType.B_QUEEN   => "B_QUEEN",
            PieceType.B_KING    => "B_KING",
            _ => "?"
        };
    }

    // ==== DenseMove ====
    //
    // U32 layout (same as C++):
    // [31..29] CaptType (3)
    // [28]     Color (1)
    // [27..25] DType (3)
    // [24..22] PromoTo (3)
    // [21..16] From (6)
    // [15..10] To (6)
    // [9]      IsCastle (1)
    // [8]      IsEnPass (1)
    // [7..0]   Unused
    /// <summary>
    /// 32-bit piece-move representation. 
    /// </summary>
    public struct DenseMove : IEquatable<DenseMove>
    {
        /// <summary>
        /// Move data.
        /// </summary>
        public U32 Data;

        // Masks
        private const U32 moveMask_CaptType = 0b11100000000000000000000000000000u;
        private const U32 moveMask_Color    = 0b00010000000000000000000000000000u;
        private const U32 moveMask_DType    = 0b00001110000000000000000000000000u;
        private const U32 moveMask_Piece    = 0b00011110000000000000000000000000u; // color+dtype (bits 28..25)
        private const U32 moveMask_PromoTo  = 0b00000001110000000000000000000000u;
        private const U32 moveMask_From     = 0b00000000001111110000000000000000u;
        private const U32 moveMask_To       = 0b00000000000000001111110000000000u;
        private const U32 moveMask_IsCastle = 0b00000000000000000000001000000000u;
        private const U32 moveMask_IsEnPass = 0b00000000000000000000000100000000u;

        // Constructors
        public DenseMove(U32 raw) { Data = raw; }

        /// <param name="piece"></param>
        /// <param name="color"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="captureType"></param>
        /// <param name="isCastle"></param>
        /// <param name="isEnPassant"></param>
        /// <param name="promoteTo"></param>
        /// <returns>New DenseMove with set params.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Ensure 'from' and 'to' are each within 0 to 63.</exception>
        public static DenseMove FromFields(
            DenseType piece, Color color, int from, int to,
            DenseType captureType = DenseType.D_EMPTY,
            bool isCastle = false, bool isEnPassant = false,
            DenseType promoteTo = DenseType.D_EMPTY)
        {
            if ((uint)from > 63 || (uint)to > 63) throw new ArgumentOutOfRangeException("Square index out of range");
            U32 d = 0;
            if (isEnPassant) d |= 1u << 8;
            if (isCastle)    d |= 1u << 9;
            d |= ((U32)to   & 0x3Fu) << 10;
            d |= ((U32)from & 0x3Fu) << 16;
            d |= ((U32)promoteTo & 0x7u) << 22;
            d |= ((U32)piece & 0x7u) << 25;
            d |= ((U32)color & 0x1u) << 28;
            d |= ((U32)captureType & 0x7u) << 29;
            return new DenseMove(d);
        }

        /// <param name="piece"></param>
        /// <param name="color"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="captureType"></param>
        /// <param name="isCastle"></param>
        /// <param name="isEnPassant"></param>
        /// <param name="promoteTo"></param>
        /// <returns>New DenseMove with set params.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Ensure 'from' and 'to' are each within 0 to 63.</exception>
        public static DenseMove FromPieceType(
            PieceType piece, int from, int to,
            DenseType captureType = DenseType.D_EMPTY,
            bool isCastle = false, bool isEnPassant = false,
            DenseType promoteTo = DenseType.D_EMPTY)
        {
            if ((uint)from > 63 || (uint)to > 63) throw new ArgumentOutOfRangeException("Square index out of range");
            U32 d = 0;
            if (isEnPassant) d |= 1u << 8;
            if (isCastle)    d |= 1u << 9;
            d |= ((U32)to   & 0x3Fu) << 10;
            d |= ((U32)from & 0x3Fu) << 16;
            d |= ((U32)promoteTo & 0x7u) << 22;
            d |= ((U32)((int)piece & 0xFu)) << 25;     // 4-bit piece code (color+dtype)
            d |= ((U32)captureType & 0x7u) << 29;
            return new DenseMove(d);
        }

        // Equality
        public bool Equals(DenseMove other) => Data == other.Data;
        public override bool Equals(object obj) => obj is DenseMove m && Equals(m);
        //public override int GetHashCode() => (int)Data;

        // Getters
        public DenseType GetDenseType() => (DenseType)((Data & moveMask_DType) >> 25);
        public PieceType GetPieceType() => (PieceType)((Data & moveMask_Piece) >> 25);
        public Color GetColor() => (Color)((Data & moveMask_Color) >> 28);
        public int GetFrom() => (int)((Data & moveMask_From) >> 16);
        public int GetTo() => (int)((Data & moveMask_To) >> 10);

        public DenseType GetCaptDense() => (DenseType)((Data & moveMask_CaptType) >> 29);

        public PieceType GetCaptPiece()
        {
            int dense = (int)((Data & moveMask_CaptType) >> 29);
            if (dense == 0) return PieceType.EMPTY;
            // moving white means captured piece is black => set color bit; moving black => captured is white (no color bit)
            bool movingWhite = GetColor() == Color.WHITE;
            int piece4 = dense | (movingWhite ? 0b1000 : 0b0000);
            return (PieceType)piece4;
        }

        public bool IsCapture() => GetCaptDense() != DenseType.D_EMPTY;
        public bool IsCastle() => ((Data & moveMask_IsCastle) >> 9) != 0;
        public bool IsEnPassant() => ((Data & moveMask_IsEnPass) >> 8) != 0;

        public PieceType GetPromotePiece()
        {
            int dense = (int)((Data & moveMask_PromoTo) >> 22);
            if (dense == 0) return PieceType.EMPTY;
            int piece4 = dense | (((int)GetColor()) << 3); // use move color as promoted piece color
            return (PieceType)piece4;
        }

        public DenseType GetPromoteDense() => (DenseType)((Data & moveMask_PromoTo) >> 22);

        public bool IsPromotion() => (Data & moveMask_PromoTo) != 0;

        // Setters / mutators
        public void SetDenseType(DenseType t)
        {
            Data &= ~moveMask_DType;
            Data |= ((U32)t & 0x7u) << 25;
        }
        public void SetPieceType(PieceType p)
        {
            Data &= ~moveMask_Piece;
            Data |= ((U32)((int)p & 0xFu)) << 25;
        }
        public void SetColor(Color c)
        {
            Data &= ~moveMask_Color;
            Data |= ((U32)c & 0x1u) << 28;
        }
        public void SetFrom(int from)
        {
            Data &= ~moveMask_From;
            Data |= ((U32)from & 0x3Fu) << 16;
        }
        public void SetTo(int to)
        {
            Data &= ~moveMask_To;
            Data |= ((U32)to & 0x3Fu) << 10;
        }
        public void SetPromoteTo(DenseType promoteTo)
        {
            Data &= ~moveMask_PromoTo;
            Data |= ((U32)promoteTo & 0x7u) << 22;
        }
        public void SetCapture(DenseType captured)
        {
            Data &= ~moveMask_CaptType;
            Data |= ((U32)captured & 0x7u) << 29;
        }
        public void SetCastle(bool castle)
        {
            Data &= ~moveMask_IsCastle;
            if (castle) Data |= 1u << 9;
        }
        public void SetEnPass(bool enpass)
        {
            Data &= ~moveMask_IsEnPass;
            if (enpass) Data |= 1u << 8;
        }

        public string ToString(bool brief)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0} from {1} to {2}\n",
                TypeStrings.PieceTypeToString(GetPieceType()),
                GetFrom(), GetTo());

            if (brief) return sb.ToString();

            sb.AppendFormat(" - isCapture: {0}", IsCapture());
            if (IsCapture())
            {
                sb.AppendFormat(" [{0}]", TypeStrings.PieceTypeToString(GetCaptPiece()));
            }
            sb.AppendFormat("\n - isCastle: {0}\n - isEnPass: {1}\n", IsCastle(), IsEnPassant());
            if (IsPromotion())
            {
                sb.AppendFormat(" - promote: {0}\n", TypeStrings.PieceTypeToString(GetPromotePiece()));
            }
            return sb.ToString();
        }

        public override string ToString() => ToString(brief: true);
    }

    /// <summary>
    /// Class representing a game's stateful information such as side to move,
    /// castling rights, number of moves played etc.
    /// </summary>
    public struct GameState
    {
        public Color SideToMove;
        public bool CanCastleWhiteKingside;
        public bool CanCastleWhiteQueenside;
        public bool CanCastleBlackKingside;
        public bool CanCastleBlackQueenside;
        public int EnPassantSquare;   // -1 if none
        public int HalfMoveClock;
        public int FullMoveNumber;

        public GameState(bool init = true)
        {
            CanCastleWhiteKingside = true;
            CanCastleWhiteQueenside = true;
            CanCastleBlackKingside = true;
            CanCastleBlackQueenside = true;
            EnPassantSquare = -1;
            SideToMove = Color.WHITE;
            HalfMoveClock = 0;
            FullMoveNumber = 1;
        }

        public int GetCastleRights()
        {
            int rights = 0;
            if (CanCastleWhiteKingside)  rights += 8;
            if (CanCastleWhiteQueenside) rights += 4;
            if (CanCastleBlackKingside)  rights += 2;
            if (CanCastleBlackQueenside) rights += 1;
            return rights;
        }

        public int GetEnPassantFileIndex() => EnPassantSquare % 8;

        public override string ToString()
        {
            var rights = new StringBuilder();
            if (CanCastleWhiteKingside)  rights.Append('K');
            if (CanCastleWhiteQueenside) rights.Append('Q');
            if (CanCastleBlackKingside)  rights.Append('k');
            if (CanCastleBlackQueenside) rights.Append('q');
            if (rights.Length == 0) rights.Append('-');

            return $"side: {TypeStrings.ColorToString(SideToMove)} {rights} {EnPassantSquare} {HalfMoveClock} {FullMoveNumber}";
        }
    }

    /// <summary>
    /// Class representing modifiable engine options through UCI.
    /// </summary>
    public class EngineOption
    {
        public enum OptionType { Check, Spin, String }

        public string Name { get; set; }
        public OptionType Type { get; set; }

        // Current & default values boxed; use helpers to read/write strongly.
        public object CurrentValue { get; set; }
        public object DefaultValue { get; set; }

        // For Spin
        public int? MinValue { get; set; }
        public int? MaxValue { get; set; }

        private EngineOption(string name, OptionType type, object current, object def, int? min, int? max)
        {
            Name = name;
            Type = type;
            CurrentValue = current;
            DefaultValue = def;
            MinValue = min;
            MaxValue = max;
        }

        public static EngineOption CreateCheck(string name, bool defaultVal)
            => new EngineOption(name, OptionType.Check, defaultVal, defaultVal, null, null);

        public static EngineOption CreateSpin(string name, int defaultVal, int min, int max)
            => new EngineOption(name, OptionType.Spin, defaultVal, defaultVal, min, max);

        public static EngineOption CreateString(string name, string defaultVal)
            => new EngineOption(name, OptionType.String, defaultVal, defaultVal, null, null);

        public bool SetValue(string value)
        {
            try
            {
                switch (Type)
                {
                    case OptionType.Check:
                        if (value.Equals("true", StringComparison.OrdinalIgnoreCase))
                        {
                            CurrentValue = true;
                            return true;
                        }
                        if (value.Equals("false", StringComparison.OrdinalIgnoreCase))
                        {
                            CurrentValue = false;
                            return true;
                        }
                        return false;

                    case OptionType.Spin:
                        if (int.TryParse(value, out int spinVal))
                        {
                            if ((MinValue.HasValue && spinVal < MinValue.Value) ||
                                (MaxValue.HasValue && spinVal > MaxValue.Value))
                            {
                                return false; // out of bounds
                            }
                            CurrentValue = spinVal;
                            return true;
                        }
                        return false;

                    case OptionType.String:
                        CurrentValue = value;
                        return true;
                }
            }
            catch
            {
                // Parsing or casting failed
                return false;
            }

            return false; // unknown type
        }

        public void ResetValue()
        {
            CurrentValue = DefaultValue;
        }

        public string ToUciString()
        {
            var sb = new StringBuilder();
            sb.Append("option name ").Append(Name).Append(" type ");
            switch (Type)
            {
                case OptionType.Check:
                    sb.Append("check default ").Append(((bool)DefaultValue) ? "true" : "false");
                    break;
                case OptionType.Spin:
                    sb.Append("spin default ").Append((int)DefaultValue)
                      .Append(" min ").Append(MinValue!.Value)
                      .Append(" max ").Append(MaxValue!.Value);
                    break;
                case OptionType.String:
                    sb.Append("string default ").Append((string)DefaultValue);
                    break;
            }
            return sb.ToString();
        }

        public string GetCurrentValueString() => Type switch
        {
            OptionType.Check  => ((bool)CurrentValue) ? "true" : "false",
            OptionType.Spin   => ((int)CurrentValue).ToString(),
            OptionType.String => (string)CurrentValue,
            _ => ""
        };

    }
}
