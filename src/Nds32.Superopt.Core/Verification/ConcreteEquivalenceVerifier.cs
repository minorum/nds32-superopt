using Nds32.Superopt.Isa.Architecture;
using Nds32.Superopt.Isa.Semantics;

namespace Nds32.Superopt.Core.Verification;

/// <summary>
/// Fast deterministic counterexample search. This verifier never constitutes a proof.
/// </summary>
public sealed class ConcreteEquivalenceVerifier : IEquivalenceVerifier
{
    private static readonly uint[] EdgeValues =
    [
        0,
        1,
        2,
        0x0000_7fff,
        0x0000_8000,
        0x7fff_ffff,
        0x8000_0000,
        0xffff_0000,
        0xffff_fffe,
        0xffff_ffff,
        0x5555_5555,
        0xaaaa_aaaa,
    ];

    private readonly ConcreteExecutor _executor = new();
    private readonly int _randomCases;
    private readonly int _seed;

    public ConcreteEquivalenceVerifier(int randomCases = 512, int seed = 0x4e445333)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(randomCases);
        _randomCases = randomCases;
        _seed = seed;
    }

    public VerificationResult Verify(
        InstructionSequence source,
        InstructionSequence candidate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(candidate);

        int caseIndex = 0;

        foreach (ConcreteMachineState state in GenerateEdgeCases())
        {
            VerificationResult? failure = Compare(source, candidate, state, caseIndex++);
            if (failure is not null)
            {
                return failure;
            }
        }

        var random = new Random(_seed);
        for (int index = 0; index < _randomCases; index++)
        {
            var state = new ConcreteMachineState();
            for (byte registerIndex = 0; registerIndex < Register.Count; registerIndex++)
            {
                state[new Register(registerIndex)] = unchecked((uint)random.NextInt64(0, 1L << 32));
            }

            VerificationResult? failure = Compare(source, candidate, state, caseIndex++);
            if (failure is not null)
            {
                return failure;
            }
        }

        return new VerificationResult.NoCounterexample(caseIndex);
    }

    private VerificationResult? Compare(
        InstructionSequence source,
        InstructionSequence candidate,
        ConcreteMachineState state,
        int caseIndex)
    {
        ConcreteMachineState sourceState = _executor.Execute(source, state);
        ConcreteMachineState candidateState = _executor.Execute(candidate, state);
        Register? differingRegister = sourceState.FindFirstDifference(candidateState);

        if (differingRegister is not Register register)
        {
            return null;
        }

        var inputValues = new Dictionary<Register, uint>(Register.Count);
        for (byte registerIndex = 0; registerIndex < Register.Count; registerIndex++)
        {
            var input = new Register(registerIndex);
            inputValues.Add(input, state[input]);
        }

        return new VerificationResult.Counterexample(
            caseIndex,
            new ConcreteInput(inputValues),
            register,
            sourceState[register],
            candidateState[register]);
    }

    private static IEnumerable<ConcreteMachineState> GenerateEdgeCases()
    {
        yield return new ConcreteMachineState();

        foreach (uint value in EdgeValues)
        {
            var same = new ConcreteMachineState();
            for (byte registerIndex = 0; registerIndex < Register.Count; registerIndex++)
            {
                same[new Register(registerIndex)] = value;
            }

            yield return same;
        }

        for (byte registerIndex = 0; registerIndex < Register.Count; registerIndex++)
        {
            foreach (uint value in EdgeValues)
            {
                var state = new ConcreteMachineState();
                state[new Register(registerIndex)] = value;
                yield return state;
            }
        }
    }
}
