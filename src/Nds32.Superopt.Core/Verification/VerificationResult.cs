using Nds32.Superopt.Isa.Architecture;

namespace Nds32.Superopt.Core.Verification;

public abstract record VerificationResult
{
    private VerificationResult()
    {
    }

    /// <summary>The verifier found no failing input, but did not prove equivalence.</summary>
    public sealed record NoCounterexample(int CasesChecked) : VerificationResult;

    /// <summary>
    /// A complete verifier may return this only after producing a formal proof. The
    /// constructor is internal so external verifier implementations cannot forge proof
    /// status merely by returning the expected runtime type.
    /// </summary>
    public sealed record ProvenEquivalent : VerificationResult
    {
        internal ProvenEquivalent(string proofMethod)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(proofMethod);
            ProofMethod = proofMethod;
        }

        public string ProofMethod { get; }
    }

    public sealed record Counterexample(
        int CaseIndex,
        ConcreteInput Input,
        Register DifferingRegister,
        uint SourceValue,
        uint CandidateValue) : VerificationResult;
}

public sealed record ConcreteInput(IReadOnlyDictionary<Register, uint> Registers);