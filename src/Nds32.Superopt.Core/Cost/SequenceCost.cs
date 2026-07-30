namespace Nds32.Superopt.Core.Cost;

public readonly record struct SequenceCost(int Bytes, int InstructionCount) : IComparable<SequenceCost>
{
    public int CompareTo(SequenceCost other)
    {
        int bytes = Bytes.CompareTo(other.Bytes);
        return bytes != 0 ? bytes : InstructionCount.CompareTo(other.InstructionCount);
    }

    public override string ToString() => $"{Bytes} byte(s), {InstructionCount} instruction(s)";
}
