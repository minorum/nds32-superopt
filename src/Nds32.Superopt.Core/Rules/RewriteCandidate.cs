using Nds32.Superopt.Core.Cost;
using Nds32.Superopt.Core.Verification;
using Nds32.Superopt.Isa.Architecture;

namespace Nds32.Superopt.Core.Rules;

/// <summary>
/// A cheaper candidate accompanied by verification evidence. It is not a trusted rewrite
/// unless <see cref="Verification"/> is <see cref="VerificationResult.ProvenEquivalent"/>.
/// </summary>
public sealed record RewriteCandidate(
    InstructionSequence Source,
    InstructionSequence Replacement,
    SequenceCost SourceCost,
    SequenceCost ReplacementCost,
    VerificationResult Verification)
{
    public int BytesSaved => SourceCost.Bytes - ReplacementCost.Bytes;

    public bool IsProven => Verification is VerificationResult.ProvenEquivalent;
}
