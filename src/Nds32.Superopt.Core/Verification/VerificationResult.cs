using Nds32.Superopt.Isa.Architecture;

namespace Nds32.Superopt.Core.Verification;

public abstract record VerificationResult
{
    private VerificationResult()
    {
    }

    /// <summary>The verifier found no failing input, but did not prove equivalence.</summary>
    public sealed record NoCounterexample(int CasesChecked) : VerificationResult;

    /// <summary>A future formal verifier may return this only after a complete proof.</summary>
    public sealed record ProvenEquivalent(string ProofMethod) : VerificationResult;

    public sealed record Counterexample(
        int CaseIndex,
        ConcreteInput Input,
        Register DifferingRegister,
        uint SourceValue,
        uint CandidateValue) : VerificationResult;
}

public sealed record ConcreteInput(IReadOnlyDictionary<Register, uint> Registers);
