namespace Nds32.Superopt.Isa.Architecture;

public readonly record struct Register
{
    public const byte Count = 32;

    public Register(byte index)
    {
        if (index >= Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "NDS32 has 32 general-purpose registers.");
        }

        Index = index;
    }

    public byte Index { get; }

    public uint Bit => 1u << Index;

    public bool IsGpr4Encodable => Index <= 11 || Index is >= 16 and <= 19;

    public override string ToString() => $"r{Index}";
}
