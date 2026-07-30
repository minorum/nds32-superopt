using Nds32.Superopt.Isa.Architecture;

namespace Nds32.Superopt.Core.Search;

public sealed class CandidateEnumerator
{
    private static readonly int[] ArithmeticImmediates = [-16, -1, 0, 1, 2, 8, 15, 16, 31];
    private static readonly int[] ConstantImmediates = [-16, -1, 0, 1, 15, 16, 31, 255];
    private static readonly int[] MaskImmediates = [0, 1, 3, 7, 0xFF, 0xFF00, 0x7FFF];

    private static readonly Opcode[] RTypeOpcodes =
    [
        Opcode.Add,
        Opcode.Subtract,
        Opcode.And,
        Opcode.Or,
        Opcode.Xor,
        Opcode.Multiply,
        Opcode.SetLessThan,
        Opcode.SetLessThanUnsigned,
        Opcode.Add45,
    ];

    private static readonly Opcode[] ImmediateBitOpcodes =
    [
        Opcode.AndImmediate,
        Opcode.OrImmediate,
        Opcode.XorImmediate,
    ];

    private static readonly Opcode[] ShiftOpcodes =
    [
        Opcode.ShiftLeft,
        Opcode.ShiftRightLogical,
        Opcode.ShiftRightArithmetic,
    ];

    /// <summary>
    /// Enumerates single-instruction candidates plus the empty sequence. The caller
    /// remains responsible for equivalence verification and cost comparison.
    /// </summary>
    public IEnumerable<InstructionSequence> EnumerateSingleInstructionCandidates(
        Register destination,
        IReadOnlyCollection<Register> inputRegisters)
    {
        ArgumentNullException.ThrowIfNull(inputRegisters);

        yield return InstructionSequence.Empty;

        Register[] inputs = inputRegisters
            .Distinct()
            .OrderBy(static register => register.Index)
            .ToArray();

        foreach (Register source in inputs)
        {
            yield return new InstructionSequence(
            [
                Instruction.Unary(Opcode.Move55, destination, source),
            ]);

            foreach (int immediate in ArithmeticImmediates)
            {
                foreach (InstructionSequence sequence in TryBuild(
                             Instruction.WithImmediate(Opcode.AddImmediate, destination, source, immediate)))
                {
                    yield return sequence;
                }

                foreach (InstructionSequence sequence in TryBuild(
                             Instruction.WithImmediate(Opcode.AddImmediate45, destination, source, immediate)))
                {
                    yield return sequence;
                }
            }

            foreach (int immediate in MaskImmediates)
            {
                foreach (Opcode opcode in ImmediateBitOpcodes)
                {
                    foreach (InstructionSequence sequence in TryBuild(
                                 Instruction.WithImmediate(opcode, destination, source, immediate)))
                    {
                        yield return sequence;
                    }
                }
            }

            for (int shift = 0; shift <= 31; shift++)
            {
                foreach (Opcode opcode in ShiftOpcodes)
                {
                    yield return new InstructionSequence(
                    [
                        Instruction.WithImmediate(opcode, destination, source, shift),
                    ]);
                }
            }

            foreach (Register secondSource in inputs)
            {
                foreach (Opcode opcode in RTypeOpcodes)
                {
                    foreach (InstructionSequence sequence in TryBuild(
                                 Instruction.Binary(opcode, destination, source, secondSource)))
                    {
                        yield return sequence;
                    }
                }
            }
        }

        foreach (int immediate in ConstantImmediates)
        {
            foreach (InstructionSequence sequence in TryBuild(
                         Instruction.WithImmediate(Opcode.MoveImmediate, destination, default, immediate)))
            {
                yield return sequence;
            }

            foreach (InstructionSequence sequence in TryBuild(
                         Instruction.WithImmediate(Opcode.MoveImmediate55, destination, default, immediate)))
            {
                yield return sequence;
            }
        }
    }

    private static IEnumerable<InstructionSequence> TryBuild(Instruction instruction)
    {
        if (instruction.IsEncodingValid)
        {
            yield return new InstructionSequence([instruction]);
        }
    }
}