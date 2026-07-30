using Nds32.Superopt.Core.Cost;
using Nds32.Superopt.Core.Rules;
using Nds32.Superopt.Core.Verification;
using Nds32.Superopt.Isa.Architecture;

namespace Nds32.Superopt.Core.Search;

public sealed class Superoptimizer
{
    private readonly CandidateEnumerator _candidates;
    private readonly ISequenceCostModel _costModel;
    private readonly IEquivalenceVerifier _verifier;

    public Superoptimizer(
        CandidateEnumerator candidates,
        ISequenceCostModel costModel,
        IEquivalenceVerifier verifier)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(costModel);
        ArgumentNullException.ThrowIfNull(verifier);

        _candidates = candidates;
        _costModel = costModel;
        _verifier = verifier;
    }

    public RewriteCandidate? FindCheapestReplacement(
        InstructionSequence source,
        Register destination,
        IReadOnlyCollection<Register>? inputRegisters = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        Register[] inputs = (inputRegisters ?? source.ReadRegisters.Enumerate().ToArray())
            .Distinct()
            .OrderBy(static register => register.Index)
            .ToArray();

        SequenceCost sourceCost = _costModel.GetCost(source);
        RewriteCandidate? best = null;

        foreach (InstructionSequence candidate in _candidates
                     .EnumerateSingleInstructionCandidates(destination, inputs)
                     .Distinct())
        {
            SequenceCost candidateCost = _costModel.GetCost(candidate);
            if (candidateCost.CompareTo(sourceCost) >= 0)
            {
                continue;
            }

            if (best is not null && candidateCost.CompareTo(best.ReplacementCost) >= 0)
            {
                continue;
            }

            VerificationResult result = _verifier.Verify(source, candidate);
            if (result is VerificationResult.Counterexample)
            {
                continue;
            }

            best = new RewriteCandidate(
                source,
                candidate,
                sourceCost,
                candidateCost,
                result);
        }

        return best;
    }
}