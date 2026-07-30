using Nds32.Superopt.Isa.Architecture;
using Nds32.Superopt.Isa.Semantics;

namespace Nds32.Superopt.Tests;

[TestFixture]
public sealed class ConcreteExecutorTests
{
    private static readonly Register R1 = new(1);
    private static readonly Register R2 = new(2);
    private static readonly Register R3 = new(3);

    [Test]
    public void Execute_AddImmediate_WrapsAtThirtyTwoBits()
    {
        var initial = new ConcreteMachineState { [R1] = uint.MaxValue };
        var sequence = new InstructionSequence(
        [
            Instruction.WithImmediate(Opcode.AddImmediate, R2, R1, 1),
        ]);

        ConcreteMachineState result = new ConcreteExecutor().Execute(sequence, initial);

        Assert.That(result[R2], Is.Zero);
    }

    [TestCase(Opcode.Add, 7u, 3u, 10u)]
    [TestCase(Opcode.Subtract, 7u, 3u, 4u)]
    [TestCase(Opcode.And, 0b1100u, 0b1010u, 0b1000u)]
    [TestCase(Opcode.Or, 0b1100u, 0b1010u, 0b1110u)]
    [TestCase(Opcode.Xor, 0b1100u, 0b1010u, 0b0110u)]
    public void Execute_BinaryInstruction_ImplementsBitVectorSemantics(
        Opcode opcode,
        uint left,
        uint right,
        uint expected)
    {
        var initial = new ConcreteMachineState
        {
            [R1] = left,
            [R2] = right,
            [R3] = 0xdead_beef,
        };
        var sequence = new InstructionSequence(
        [
            Instruction.Binary(opcode, R3, R1, R2),
        ]);

        ConcreteMachineState result = new ConcreteExecutor().Execute(sequence, initial);

        Assert.That(result[R3], Is.EqualTo(expected));
    }

    [Test]
    public void Execute_DoesNotMutateInitialState()
    {
        var initial = new ConcreteMachineState { [R1] = 41 };
        var sequence = new InstructionSequence(
        [
            Instruction.WithImmediate(Opcode.AddImmediate, R1, R1, 1),
        ]);

        _ = new ConcreteExecutor().Execute(sequence, initial);

        Assert.That(initial[R1], Is.EqualTo(41));
    }
}
