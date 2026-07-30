using Nds32.Superopt.Isa.Architecture;

namespace Nds32.Superopt.Core.Search;

public sealed class CandidateEnumerator
{
    private static readonly int[] ArithmeticImmediates = [-16, -1, 0, 1, 2, 15, 16, 31];
    private static readonly int[] ConstantImmediates = [-16, -1, 0, 1, 15, 16, 31, 255];

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
            yield return new InstructionSequence(
            [
                Instruction.Unary(Opcode.Move55, destination, source),
            ]);

            foreach (int immediate in ArithmeticImmediates)
            {
                Instruction addImmediate = Instruction.WithImmediate(
                    Opcode.AddImmediate,
                    destination,
                    source,
                    immediate);

                if (addImmediate.IsEncodingValid)
                {
                    yield return new InstructionSequence([addImmediate]);
                }

                Instruction addImmediate45 = Instruction.WithImmediate(
                    Opcode.AddImmediate45,
                    destination,
                    source,
                    immediate);

                if (addImmediate45.IsEncodingValid)
                {
                    yield return new InstructionSequence([addImmediate45]);
                }
            }

            foreach (Register secondSource in inputs)
            {
                yield return Sequence(Opcode.Add, destination, source, secondSource);
                yield return Sequence(Opcode.Subtract, destination, source, secondSource);
                yield return Sequence(Opcode.And, destination, source, secondSource);
                yield return Sequence(Opcode.Or, destination, source, secondSource);
                yield return Sequence(Opcode.Xor, destination, source, secondSource);

                Instruction add45 = Instruction.Binary(Opcode.Add45, destination, source, secondSource);
                if (add45.IsEncodingValid)
                {
                    yield return new InstructionSequence([add45]);
                }
            }
        }

        foreach (int immediate in ConstantImmediates)
        {
            Instruction movi = Instruction.WithImmediate(
                Opcode.MoveImmediate,
                destination,
                default,
                immediate);

            if (movi.IsEncodingValid)
            {
                yield return new InstructionSequence([movi]);
            }

            Instruction movi55 = Instruction.WithImmediate(
                Opcode.MoveImmediate55,
                destination,
                default,
                immediate);

            if (movi55.IsEncodingValid)
            {
                yield return new InstructionSequence([movi55]);
            }
        }
    }

    private static InstructionSequence Sequence(
        Opcode opcode,
        Register destination,
        Register source1,
        Register source2) =>
        new([Instruction.Binary(opcode, destination, source1, source2)]);
}
