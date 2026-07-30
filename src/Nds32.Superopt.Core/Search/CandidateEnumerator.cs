using Nds32.Superopt.Isa.Architecture;

namespace Nds32.Superopt.Core.Search;

public sealed class CandidateEnumerator
{
    private static readonly int[] ArithmeticImmediates = [-16, -1, 0, 1, 2, 8, 15, 16, 31];
    private static readonly int[] ConstantImmediates   = [-16, -1, 0, 1, 15, 16, 31, 255];
    private static readonly int[] MaskImmediates       = [0, 1, 3, 7, 0xFF, 0xFF00, 0xFFFF];

    private static readonly Opcode[] RTypeOpcodes =
    [
        Opcode.Add, Opcode.Subtract, Opcode.And, Opcode.Or, Opcode.Xor,
        Opcode.Nor, Opcode.Multiply, Opcode.SetLessThan, Opcode.SetLessThanUnsigned,
        Opcode.Add45,
    ];

    /// <summary>
    /// Enumerates all single-instruction candidates that could be cheaper than the
    /// source sequence.  The empty sequence (complete removal) is included first to
    /// allow elimination of architectural no-ops.
    /// </summary>
    public IEnumerable<InstructionSequence> EnumerateSingleInstructionCandidates(
        Register destination,
        IReadOnlyCollection<Register> inputRegisters)
    {
        ArgumentNullException.ThrowIfNull(inputRegisters);

        // Allows removal of a sequence that has no observable architectural effect.
        yield return InstructionSequence.Empty;

        Register[] inputs = inputRegisters.Distinct().OrderBy(static register => register.Index).ToArray();

        foreach (Register source in inputs)
        {
            // ── Copy (16-bit) ─────────────────────────────────────────────────
            yield return new InstructionSequence(
            [
                Instruction.Unary(Opcode.Move55, destination, source),
            ]);

            // ── Immediate arithmetic ──────────────────────────────────────────
            foreach (int immediate in ArithmeticImmediates)
            {
                foreach (var inst in TryBuild(Instruction.WithImmediate(Opcode.AddImmediate, destination, source, immediate)))
                    yield return inst;

                foreach (var inst in TryBuild(Instruction.WithImmediate(Opcode.AddImmediate45, destination, source, immediate)))
                    yield return inst;
            }

            // ── Immediate bit ops ─────────────────────────────────────────────
            foreach (int immediate in MaskImmediates)
            {
                foreach (var inst in TryBuild(Instruction.WithImmediate(Opcode.AndImmediate, destination, source, immediate)))
                    yield return inst;

                foreach (var inst in TryBuild(Instruction.WithImmediate(Opcode.OrImmediate, destination, source, immediate)))
                    yield return inst;

                foreach (var inst in TryBuild(Instruction.WithImmediate(Opcode.XorImmediate, destination, source, immediate)))
                    yield return inst;
            }

            // ── Shifts ────────────────────────────────────────────────────────
            for (int shift = 0; shift <= 31; shift++)
            {
                foreach (var inst in TryBuild(Instruction.WithImmediate(Opcode.ShiftLeft, destination, source, shift)))
                    yield return inst;

                foreach (var inst in TryBuild(Instruction.WithImmediate(Opcode.ShiftRightLogical, destination, source, shift)))
                    yield return inst;

                foreach (var inst in TryBuild(Instruction.WithImmediate(Opcode.ShiftRightArithmetic, destination, source, shift)))
                    yield return inst;
            }

            // ── Register-register ─────────────────────────────────────────────
            foreach (Register secondSource in inputs)
            {
                foreach (var opcode in RTypeOpcodes)
                {
                    foreach (var inst in TryBuild(Instruction.Binary(opcode, destination, source, secondSource)))
                        yield return inst;
                }
            }
        }

        // ── Load constant ─────────────────────────────────────────────────────
        foreach (int immediate in ConstantImmediates)
        {
            foreach (var inst in TryBuild(Instruction.WithImmediate(Opcode.MoveImmediate, destination, default, immediate)))
                yield return inst;

            foreach (var inst in TryBuild(Instruction.WithImmediate(Opcode.MoveImmediate55, destination, default, immediate)))
                yield return inst;
        }
    }

    private static IEnumerable<InstructionSequence> TryBuild(Instruction inst)
    {
        if (inst.IsEncodingValid)
            yield return new InstructionSequence([inst]);
    }
}
