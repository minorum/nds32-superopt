using Nds32.Superopt.Isa.Architecture;

namespace Nds32.Superopt.Tests;

[TestFixture]
public sealed class InstructionTests
{
    [TestCase(-16384, true)]
    [TestCase(16383, true)]
    [TestCase(-16385, false)]
    [TestCase(16384, false)]
    public void AddImmediate_UsesSignedFifteenBitRange(int immediate, bool expected)
    {
        Instruction instruction = Instruction.WithImmediate(
            Opcode.AddImmediate,
            new Register(2),
            new Register(1),
            immediate);

        Assert.That(instruction.IsEncodingValid, Is.EqualTo(expected));
    }

    [Test]
    public void AddImmediate45_RequiresTiedEncodableDestination()
    {
        Instruction valid = Instruction.WithImmediate(
            Opcode.AddImmediate45,
            new Register(2),
            new Register(2),
            31);
        Instruction untied = Instruction.WithImmediate(
            Opcode.AddImmediate45,
            new Register(2),
            new Register(1),
            31);
        Instruction invalidRegister = Instruction.WithImmediate(
            Opcode.AddImmediate45,
            new Register(12),
            new Register(12),
            31);

        Assert.Multiple(() =>
        {
            Assert.That(valid.IsEncodingValid, Is.True);
            Assert.That(valid.EncodedSize, Is.EqualTo(2));
            Assert.That(untied.IsEncodingValid, Is.False);
            Assert.That(invalidRegister.IsEncodingValid, Is.False);
        });
    }

    [Test]
    public void Sequence_DerivesExternalInputsAcrossTemporaryDefinitions()
    {
        var r1 = new Register(1);
        var r2 = new Register(2);
        var r3 = new Register(3);
        var r4 = new Register(4);
        var sequence = new InstructionSequence(
        [
            Instruction.Binary(Opcode.Add, r3, r1, r2),
            Instruction.Binary(Opcode.Xor, r4, r3, r1),
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(sequence.ReadRegisters.Enumerate(), Is.EqualTo(new[] { r1, r2 }));
            Assert.That(sequence.WrittenRegisters.Enumerate(), Is.EqualTo(new[] { r3, r4 }));
        });
    }
}
