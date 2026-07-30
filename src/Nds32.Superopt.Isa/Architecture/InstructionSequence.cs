using System.Collections.Immutable;

namespace Nds32.Superopt.Isa.Architecture;

public sealed class InstructionSequence : IEquatable<InstructionSequence>
{
    public static InstructionSequence Empty { get; } = new(Array.Empty<Instruction>());

    public InstructionSequence(IEnumerable<Instruction> instructions)
    {
        ArgumentNullException.ThrowIfNull(instructions);
        Instructions = instructions.ToImmutableArray();

        if (Instructions.Any(static instruction => !instruction.IsEncodingValid))
        {
            throw new ArgumentException(
                "The sequence contains an instruction that cannot be encoded.",
                nameof(instructions));
        }
    }

    public ImmutableArray<Instruction> Instructions { get; }

    public int EncodedSize => Instructions.Sum(static instruction => instruction.EncodedSize);

    public RegisterMask ReadRegisters
    {
        get
        {
            RegisterMask reads = RegisterMask.Empty;
            RegisterMask written = RegisterMask.Empty;

            foreach (Instruction instruction in Instructions)
            {
                foreach (Register register in instruction.ReadRegisters.Enumerate())
                {
                    if (!written.Contains(register))
                    {
                        reads = reads.Add(register);
                    }
                }

                written = new RegisterMask(written.Bits | instruction.WrittenRegisters.Bits);
            }

            return reads;
        }
    }

    public RegisterMask WrittenRegisters
    {
        get
        {
            RegisterMask result = RegisterMask.Empty;
            foreach (Instruction instruction in Instructions)
            {
                result = new RegisterMask(result.Bits | instruction.WrittenRegisters.Bits);
            }

            return result;
        }
    }

    public bool Equals(InstructionSequence? other) =>
        other is not null && Instructions.AsSpan().SequenceEqual(other.Instructions.AsSpan());

    public override bool Equals(object? obj) => obj is InstructionSequence other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (Instruction instruction in Instructions)
        {
            hash.Add(instruction);
        }

        return hash.ToHashCode();
    }

    public override string ToString() => Instructions.Length == 0
        ? "<empty>"
        : string.Join(Environment.NewLine, Instructions);
}
