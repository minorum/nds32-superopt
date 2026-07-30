using Nds32.Superopt.Core.Cost;
using Nds32.Superopt.Core.Verification;
using Nds32.Superopt.Isa.Architecture;

namespace Nds32.Superopt.Core.Rules;

/// <summary>
/// A cheaper candidate accompanied by verification evidence. It is not a trusted rewrite
/// unless <see cref="Verification"/> is <see cref="VerificationResult.ProvenEquivalent"/>.
/// </summary>
public sealed record RewriteCandidate
{
    internal RewriteCandidate(
        InstructionSequence source,
        InstructionSequence replacement,
        SequenceCost sourceCost,
        SequenceCost replacementCost,
        VerificationResult verification)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(replacement);
        ArgumentNullException.ThrowIfNull(verification);

        if (replacementCost.CompareTo(sourceCost) >= 0)
        {
            throw new ArgumentException(
                "A rewrite replacement must be strictly cheaper than its source.",
                nameof(replacementCost));
        }

        Source = source;
        Replacement = replacement;
        SourceCost = sourceCost;
        ReplacementCost = replacementCost;
        Verification = verification;
    }

    public InstructionSequence Source { get; }

    public InstructionSequence Replacement { get; }

    public SequenceCost SourceCost { get; }

    public SequenceCost ReplacementCost { get; }

    public VerificationResult Verification { get; }

    public int BytesSaved => SourceCost.Bytes - ReplacementCost.Bytes;

    public bool IsProven => Verification is VerificationResult.ProvenEquivalent;
}
