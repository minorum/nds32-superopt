using Nds32.Superopt.Isa.Architecture;

namespace Nds32.Superopt.Isa.Semantics;

public sealed class ConcreteMachineState
{
    private readonly uint[] _registers;

    public ConcreteMachineState()
    {
        _registers = new uint[Register.Count];
    }

    private ConcreteMachineState(uint[] registers)
    {
        _registers = registers;
    }

    public uint this[Register register]
    {
        get => _registers[register.Index];
        set => _registers[register.Index] = value;
    }

    public ConcreteMachineState Clone() => new((uint[])_registers.Clone());

    public bool ArchitecturalEquals(ConcreteMachineState other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Registers.SequenceEqual(other.Registers);
    }

    public Register? FindFirstDifference(ConcreteMachineState other)
    {
        ArgumentNullException.ThrowIfNull(other);

        for (byte index = 0; index < Register.Count; index++)
        {
            var register = new Register(index);
            if (this[register] != other[register])
            {
                return register;
            }
        }

        return null;
    }

    public ReadOnlySpan<uint> Registers => _registers;
}
