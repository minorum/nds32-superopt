using System.Text;

namespace Nds32Superopt.Instructions;

/// <summary>
/// A single NDS32 instruction as used by the superoptimizer.
/// The fields are interpreted according to the opcode's format (R / I / U).
/// </summary>
/// <param name="Op">The opcode.</param>
/// <param name="Rd">Destination register index (0-based).</param>
/// <param name="Rs1">First source register index (R-type and I-type).</param>
/// <param name="Rs2">Second source register index (R-type only).</param>
/// <param name="Imm">Immediate value (I-type and U-type).</param>
public readonly record struct Instruction(
    Opcode Op,
    byte Rd,
    byte Rs1,
    byte Rs2,
    int Imm)
{
    // ── Factory helpers ───────────────────────────────────────────────────────

    /// <summary>Creates an R-type instruction (two register sources).</summary>
    public static Instruction R(Opcode op, byte rd, byte rs1, byte rs2) =>
        new(op, rd, rs1, rs2, 0);

    /// <summary>Creates an I-type instruction (one register source + immediate).</summary>
    public static Instruction I(Opcode op, byte rd, byte rs1, int imm) =>
        new(op, rd, rs1, 0, imm);

    /// <summary>Creates a U-type instruction (destination + immediate only).</summary>
    public static Instruction U(Opcode op, byte rd, int imm) =>
        new(op, rd, 0, 0, imm);

    // ── Display ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(Op.Mnemonic());
        sb.Append('\t');
        sb.Append('r');
        sb.Append(Rd);

        if (Op.IsRType())
        {
            sb.Append(", r"); sb.Append(Rs1);
            sb.Append(", r"); sb.Append(Rs2);
        }
        else if (Op.IsIType())
        {
            sb.Append(", r"); sb.Append(Rs1);
            sb.Append(", "); sb.Append(Imm);
        }
        else // U-type
        {
            sb.Append(", "); sb.Append(Imm);
        }

        return sb.ToString();
    }
}
