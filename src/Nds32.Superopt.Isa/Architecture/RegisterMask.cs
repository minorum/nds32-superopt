using System.Numerics;

namespace Nds32.Superopt.Isa.Architecture;

/// <summary>A compact set of NDS32 general-purpose registers.</summary>
public readonly record struct RegisterMask(uint Bits)
{
    public static RegisterMask Empty => default;

    public int Count => BitOperations.PopCount(Bits);

    public bool Contains(Register register) => (Bits & register.Bit) != 0;

    public RegisterMask Add(Register register) => new(Bits | register.Bit);

    public RegisterMask Remove(Register register) => new(Bits & ~register.Bit);

    public IEnumerable<Register> Enumerate()
    {
        uint remaining = Bits;
        while (remaining != 0)
        {
            int index = BitOperations.TrailingZeroCount(remaining);
            yield return new Register(checked((byte)index));
            remaining &= remaining - 1;
        }
    }

    public static RegisterMask From(IEnumerable<Register> registers)
    {
        ArgumentNullException.ThrowIfNull(registers);

        RegisterMask result = Empty;
        foreach (Register register in registers)
        {
            result = result.Add(register);
        }

        return result;
    }
}
