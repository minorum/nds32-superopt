namespace Nds32.Superopt.Isa.Architecture;

public enum Opcode : byte
{
    Add,
    AddImmediate,
    Subtract,
    And,
    Or,
    Xor,
    ShiftLeft,
    ShiftRightLogical,
    ShiftRightArithmetic,
    Move55,
    MoveImmediate,
    MoveImmediate55,
    Add45,
    AddImmediate45,
}
