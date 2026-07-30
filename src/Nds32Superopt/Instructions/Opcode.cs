namespace Nds32Superopt.Instructions;

/// <summary>
/// NDS32 instruction opcodes supported by the superoptimizer.
/// This is a representative subset of the NDS32 ISA covering ALU, shift, and
/// immediate operations that are most relevant to code optimization.
/// </summary>
public enum Opcode : byte
{
    // ── R-type: rd = f(rs1, rs2) ──────────────────────────────────────────────
    Add,   // rd = rs1 + rs2
    Sub,   // rd = rs1 − rs2
    Mul,   // rd = rs1 * rs2 (lower 32 bits)
    And,   // rd = rs1 & rs2
    Or,    // rd = rs1 | rs2
    Xor,   // rd = rs1 ^ rs2
    Nor,   // rd = ~(rs1 | rs2)
    Sll,   // rd = rs1 << (rs2 & 31)
    Srl,   // rd = rs1 >> (rs2 & 31)  (logical)
    Sra,   // rd = rs1 >>> (rs2 & 31) (arithmetic)
    Slt,   // rd = (signed rs1 < signed rs2) ? 1 : 0
    Sltu,  // rd = (rs1 < rs2) ? 1 : 0  (unsigned)

    // ── I-type: rd = f(rs1, imm) ──────────────────────────────────────────────
    Addi,  // rd = rs1 + sext(imm)
    Andi,  // rd = rs1 & zext(imm)
    Ori,   // rd = rs1 | zext(imm)
    Xori,  // rd = rs1 ^ zext(imm)
    Slli,  // rd = rs1 << (imm & 31)
    Srli,  // rd = rs1 >> (imm & 31)   (logical)
    Srai,  // rd = rs1 >>> (imm & 31)  (arithmetic)
    Slti,  // rd = (signed rs1 < sext(imm)) ? 1 : 0
    Sltui, // rd = (rs1 < zext(imm)) ? 1 : 0  (unsigned)

    // ── U-type: rd = f(imm) ───────────────────────────────────────────────────
    Movi,  // rd = sext(imm20)          move immediate (signed 20-bit)
    Sethi, // rd = imm20 << 12          set high bits
}

/// <summary>Extension helpers for <see cref="Opcode"/>.</summary>
public static class OpcodeExtensions
{
    /// <summary>Returns true when the opcode uses two register sources (R-type).</summary>
    public static bool IsRType(this Opcode op) => op <= Opcode.Sltu;

    /// <summary>Returns true when the opcode uses one register source and an immediate (I-type).</summary>
    public static bool IsIType(this Opcode op) => op >= Opcode.Addi && op <= Opcode.Sltui;

    /// <summary>Returns true when the opcode uses only an immediate and a destination (U-type).</summary>
    public static bool IsUType(this Opcode op) => op >= Opcode.Movi;

    /// <summary>Human-readable NDS32 assembly mnemonic for this opcode.</summary>
    public static string Mnemonic(this Opcode op) => op switch
    {
        Opcode.Add   => "add",
        Opcode.Sub   => "sub",
        Opcode.Mul   => "mul",
        Opcode.And   => "and",
        Opcode.Or    => "or",
        Opcode.Xor   => "xor",
        Opcode.Nor   => "nor",
        Opcode.Sll   => "sll",
        Opcode.Srl   => "srl",
        Opcode.Sra   => "sra",
        Opcode.Slt   => "slt",
        Opcode.Sltu  => "sltu",
        Opcode.Addi  => "addi",
        Opcode.Andi  => "andi",
        Opcode.Ori   => "ori",
        Opcode.Xori  => "xori",
        Opcode.Slli  => "slli",
        Opcode.Srli  => "srli",
        Opcode.Srai  => "srai",
        Opcode.Slti  => "slti",
        Opcode.Sltui => "sltui",
        Opcode.Movi  => "movi",
        Opcode.Sethi => "sethi",
        _            => op.ToString().ToLowerInvariant(),
    };
}
