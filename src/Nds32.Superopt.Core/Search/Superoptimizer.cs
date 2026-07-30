using Nds32.Superopt.Core.Cost;
using Nds32.Superopt.Core.Rules;
using Nds32.Superopt.Core.Verification;
using Nds32.Superopt.Isa.Architecture;

namespace Nds32.Superopt.Core.Search;

public sealed class Superoptimizer(
    CandidateEnumerator candidates,
    ISequenceCostModel costModel,
    IEquivalenceVerifier verifier)
{
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

        SequenceCost sourceCost = costModel.GetCost(source);
        RewriteCandidate? best = null;

        foreach (InstructionSequence candidate in candidates
                     .EnumerateSingleInstructionCandidates(destination, inputs)
                     .Distinct())
        {
            SequenceCost candidateCost = costModel.GetCost(candidate);
            if (candidateCost.CompareTo(sourceCost) >= 0)
            {
                continue;
            }

            if (best is not null && candidateCost.CompareTo(best.ReplacementCost) >= 0)
            {
                continue;
            }

            VerificationResult result = verifier.Verify(source, candidate);
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
