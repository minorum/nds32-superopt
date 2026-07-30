namespace Nds32.Superopt.Isa.Architecture;

public enum Opcode : byte
{
    // ── R-type: rd = f(rs1, rs2) ─────────────────────────────────────────────
    Add,
    Subtract,
    And,
    Or,
    Xor,
    Nor,           // rd = ~(rs1 | rs2)
    Multiply,      // rd = rs1 * rs2  (lower 32 bits)
    SetLessThan,   // rd = ((int)rs1 < (int)rs2) ? 1 : 0
    SetLessThanUnsigned, // rd = (rs1 < rs2) ? 1 : 0

    // ── I-type: rd = f(rs1, imm) ─────────────────────────────────────────────
    AddImmediate,
    OrImmediate,   // rd = rs1 | zext(uimm15)
    AndImmediate,  // rd = rs1 & zext(uimm15)
    XorImmediate,  // rd = rs1 ^ zext(uimm15)
    ShiftLeft,              // rd = rs1 << imm5
    ShiftRightLogical,      // rd = rs1 >> imm5  (logical)
    ShiftRightArithmetic,   // rd = rs1 >>> imm5 (arithmetic)
    MoveImmediate,          // rd = sext(imm20)

    // ── 16-bit compact forms ──────────────────────────────────────────────────
    Move55,          // mov55  rd, rs1                 (16-bit)
    MoveImmediate55, // movi55 rd, sext(imm7)          (16-bit)
    Add45,           // add45  rd, rs2  →  rd += rs2   (16-bit, rd must be GPR4)
    AddImmediate45,  // addi45 rd, imm5 →  rd += imm5  (16-bit, rd must be GPR4)
}
