using Nds32.Superopt.Isa.Architecture;

namespace Nds32.Superopt.Core.Cost;

public interface ISequenceCostModel
{
    SequenceCost GetCost(InstructionSequence sequence);
}
