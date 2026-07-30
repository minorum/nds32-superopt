using Nds32.Superopt.Isa.Architecture;

namespace Nds32.Superopt.Tests;

[TestFixture]
public sealed class RegisterTests
{
    [Test]
    public void Constructor_RejectsRegisterOutsideArchitectureRange()
    {
        Assert.That(
            () => new Register(32),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [TestCase(0, true)]
    [TestCase(11, true)]
    [TestCase(12, false)]
    [TestCase(16, true)]
    [TestCase(19, true)]
    [TestCase(20, false)]
    public void IsGpr4Encodable_MatchesNds32CompressedRegisterSet(byte index, bool expected)
    {
        Assert.That(new Register(index).IsGpr4Encodable, Is.EqualTo(expected));
    }

    [Test]
    public void RegisterMask_EnumeratesRegistersInEncodingOrder()
    {
        RegisterMask mask = RegisterMask.Empty
            .Add(new Register(19))
            .Add(new Register(1))
            .Add(new Register(7));

        Assert.That(
            mask.Enumerate().Select(static register => register.Index),
            Is.EqualTo(new byte[] { 1, 7, 19 }));
    }
}
