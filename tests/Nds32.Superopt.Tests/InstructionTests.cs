using Nds32.Superopt.Isa.Architecture;

namespace Nds32.Superopt.Tests;

public sealed class InstructionTests
{
    [Test]
    [Arguments(-16384, true)]
    [Arguments(16383, true)]
    [Arguments(-16385, false)]
    [Arguments(16384, false)]
    public async Task AddImmediate_UsesSignedFifteenBitRange(int immediate, bool expected)
    {
        Instruction instruction = Instruction.WithImmediate(
            Opcode.AddImmediate,
            new Register(2),
            new Register(1),
            immediate);

        await Assert.That(instruction.IsEncodingValid).IsEqualTo(expected);
    }

    [Test]
    public async Task AddImmediate45_RequiresTiedEncodableDestination()
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

        using (Assert.Multiple())
        {
            await Assert.That(valid.IsEncodingValid).IsTrue();
            await Assert.That(valid.EncodedSize).IsEqualTo(2);
            await Assert.That(untied.IsEncodingValid).IsFalse();
            await Assert.That(invalidRegister.IsEncodingValid).IsFalse();
        }
    }

    [Test]
    public async Task Sequence_DerivesExternalInputsAcrossTemporaryDefinitions()
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

        Register[] reads = sequence.ReadRegisters.Enumerate().ToArray();
        Register[] writes = sequence.WrittenRegisters.Enumerate().ToArray();

        using (Assert.Multiple())
        {
            await Assert.That(reads.SequenceEqual([r1, r2])).IsTrue();
            await Assert.That(writes.SequenceEqual([r3, r4])).IsTrue();
        }
    }

    [Test]
    [Arguments(Opcode.SetLessThan, "slts r3, r1, r2")]
    [Arguments(Opcode.SetLessThanUnsigned, "slt r3, r1, r2")]
    public async Task ComparisonMnemonics_MatchNds32Signedness(
        Opcode opcode,
        string expected)
    {
        Instruction instruction = Instruction.Binary(
            opcode,
            new Register(3),
            new Register(1),
            new Register(2));

        await Assert.That(instruction.ToString()).IsEqualTo(expected);
    }

    [Test]
    public async Task Factories_RejectOpcodeWithWrongOperandForm()
    {
        await Assert.That(() => Instruction.Binary(
                Opcode.AddImmediate,
                new Register(2),
                new Register(1),
                new Register(0)))
            .Throws<ArgumentOutOfRangeException>();
    }
}