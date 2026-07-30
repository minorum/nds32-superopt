namespace Nds32.Superopt.Isa.Architecture;

public readonly record struct Instruction(
    Opcode Opcode,
    Register Destination,
    Register Source1,
    Register Source2,
    int Immediate = 0)
{
    private static readonly Register Placeholder = new(0);

    public static Instruction Unary(Opcode opcode, Register destination, Register source) =>
        new(opcode, destination, source, Placeholder);

    public static Instruction Binary(
        Opcode opcode,
        Register destination,
        Register source1,
        Register source2) =>
        new(opcode, destination, source1, source2);

    public static Instruction WithImmediate(
        Opcode opcode,
        Register destination,
        Register source,
        int immediate) =>
        new(opcode, destination, source, Placeholder, immediate);

    public int EncodedSize => Opcode switch
    {
        Opcode.Move55 or Opcode.MoveImmediate55 or Opcode.Add45 or Opcode.AddImmediate45 => 2,
        _ => 4,
    };

    public RegisterMask ReadRegisters => Opcode switch
    {
        Opcode.MoveImmediate or Opcode.MoveImmediate55 =>
            RegisterMask.Empty,
        Opcode.Add or Opcode.Subtract or Opcode.And or Opcode.Or or Opcode.Xor or
        Opcode.Nor or Opcode.Multiply or Opcode.SetLessThan or Opcode.SetLessThanUnsigned or
        Opcode.Add45 =>
            RegisterMask.Empty.Add(Source1).Add(Source2),
        _ => RegisterMask.Empty.Add(Source1),
    };

    public RegisterMask WrittenRegisters => RegisterMask.Empty.Add(Destination);

    public bool IsEncodingValid => Opcode switch
    {
        Opcode.Move55 => true,
        Opcode.MoveImmediate55   => Immediate is >= -16 and <= 15,
        Opcode.Add45             => Destination == Source1 && Destination.IsGpr4Encodable,
        Opcode.AddImmediate45    =>
            Destination == Source1 && Destination.IsGpr4Encodable && Immediate is >= 0 and <= 31,
        Opcode.AddImmediate      => Immediate is >= -16384 and <= 16383,
        Opcode.MoveImmediate     => Immediate is >= -524288 and <= 524287,
        Opcode.OrImmediate or Opcode.AndImmediate or Opcode.XorImmediate
                                 => Immediate is >= 0 and <= 32767,
        Opcode.ShiftLeft or Opcode.ShiftRightLogical or Opcode.ShiftRightArithmetic
                                 => Immediate is >= 0 and <= 31,
        _ => true,
    };

    public override string ToString() => Opcode switch
    {
        Opcode.Add              => $"add {Destination}, {Source1}, {Source2}",
        Opcode.AddImmediate     => $"addi {Destination}, {Source1}, {Immediate}",
        Opcode.Subtract         => $"sub {Destination}, {Source1}, {Source2}",
        Opcode.And              => $"and {Destination}, {Source1}, {Source2}",
        Opcode.Or               => $"or {Destination}, {Source1}, {Source2}",
        Opcode.Xor              => $"xor {Destination}, {Source1}, {Source2}",
        Opcode.Nor              => $"nor {Destination}, {Source1}, {Source2}",
        Opcode.Multiply         => $"mul {Destination}, {Source1}, {Source2}",
        Opcode.SetLessThan      => $"slt {Destination}, {Source1}, {Source2}",
        Opcode.SetLessThanUnsigned => $"sltu {Destination}, {Source1}, {Source2}",
        Opcode.OrImmediate      => $"ori {Destination}, {Source1}, {Immediate}",
        Opcode.AndImmediate     => $"andi {Destination}, {Source1}, {Immediate}",
        Opcode.XorImmediate     => $"xori {Destination}, {Source1}, {Immediate}",
        Opcode.ShiftLeft        => $"slli {Destination}, {Source1}, {Immediate}",
        Opcode.ShiftRightLogical  => $"srli {Destination}, {Source1}, {Immediate}",
        Opcode.ShiftRightArithmetic => $"srai {Destination}, {Source1}, {Immediate}",
        Opcode.Move55           => $"mov55 {Destination}, {Source1}",
        Opcode.MoveImmediate    => $"movi {Destination}, {Immediate}",
        Opcode.MoveImmediate55  => $"movi55 {Destination}, {Immediate}",
        Opcode.Add45            => $"add45 {Destination}, {Source2}",
        Opcode.AddImmediate45   => $"addi45 {Destination}, {Immediate}",
        _ => throw new ArgumentOutOfRangeException(nameof(Opcode), Opcode, "Unknown NDS32 opcode."),
    };
}
