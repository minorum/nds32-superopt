using Nds32.Superopt.Isa.Architecture;

namespace Nds32.Superopt.Tests;

public sealed class RegisterTests
{
    [Test]
    public async Task Constructor_RejectsRegisterOutsideArchitectureRange()
    {
        await Assert.That(() => new Register(32))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    [Arguments((byte)0, true)]
    [Arguments((byte)11, true)]
    [Arguments((byte)12, false)]
    [Arguments((byte)16, true)]
    [Arguments((byte)19, true)]
    [Arguments((byte)20, false)]
    public async Task IsGpr4Encodable_MatchesNds32CompressedRegisterSet(
        byte index,
        bool expected)
    {
        await Assert.That(new Register(index).IsGpr4Encodable).IsEqualTo(expected);
    }

    [Test]
    public async Task RegisterMask_EnumeratesRegistersInEncodingOrder()
    {
        RegisterMask mask = RegisterMask.Empty
            .Add(new Register(19))
            .Add(new Register(1))
            .Add(new Register(7));

        byte[] actual = mask.Enumerate()
            .Select(static register => register.Index)
            .ToArray();

        await Assert.That(actual.SequenceEqual([1, 7, 19])).IsTrue();
    }
}