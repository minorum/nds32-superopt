using Nds32.Superopt.Isa.Architecture;

namespace Nds32.Superopt.Core.Cost;

public sealed class CodeSizeCostModel : ISequenceCostModel
{
    public SequenceCost GetCost(InstructionSequence sequence)
    {
        ArgumentNullException.ThrowIfNull(sequence);
        return new SequenceCost(sequence.EncodedSize, sequence.Instructions.Length);
    }
}
