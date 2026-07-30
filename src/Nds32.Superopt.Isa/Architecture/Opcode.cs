namespace Nds32.Superopt.Isa.Architecture;

public enum Opcode : byte
{
    // R-type: rd = f(rs1, rs2)
    Add,
    Subtract,
    And,
    Or,
    Xor,
    Multiply,
    SetLessThan,         // Signed comparison; encoded as slts.
    SetLessThanUnsigned, // Unsigned comparison; encoded as slt.

    // I-type: rd = f(rs1, immediate)
    AddImmediate,
    OrImmediate,
    AndImmediate,
    XorImmediate,
    ShiftLeft,
    ShiftRightLogical,
    ShiftRightArithmetic,
    MoveImmediate,

    // 16-bit compact forms
    Move55,
    MoveImmediate55, // Signed 5-bit immediate.
    Add45,
    AddImmediate45,
}