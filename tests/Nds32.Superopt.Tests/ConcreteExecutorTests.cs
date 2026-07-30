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

    [TestCase(Opcode.Add,      7u,           3u,           10u)]
    [TestCase(Opcode.Subtract, 7u,           3u,           4u)]
    [TestCase(Opcode.And,      0b1100u,      0b1010u,      0b1000u)]
    [TestCase(Opcode.Or,       0b1100u,      0b1010u,      0b1110u)]
    [TestCase(Opcode.Xor,      0b1100u,      0b1010u,      0b0110u)]
    [TestCase(Opcode.Nor,      0b1100u,      0b1010u,      unchecked((uint)~0b1110))]
    [TestCase(Opcode.Multiply, 6u,           7u,           42u)]
    [TestCase(Opcode.SetLessThan,         1u, 2u, 1u)]
    [TestCase(Opcode.SetLessThan,         2u, 1u, 0u)]
    [TestCase(Opcode.SetLessThan,         unchecked((uint)-1), 0u, 1u)]  // −1 < 0 (signed)
    [TestCase(Opcode.SetLessThanUnsigned, 1u, 2u, 1u)]
    [TestCase(Opcode.SetLessThanUnsigned, unchecked((uint)-1), 0u, 0u)] // UINT_MAX < 0 is false
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

    [TestCase(Opcode.AndImmediate, 0b1111u, 0b1010, 0b1010u)]
    [TestCase(Opcode.OrImmediate,  0b1100u, 0b0011, 0b1111u)]
    [TestCase(Opcode.XorImmediate, 0b1111u, 0b1010, 0b0101u)]
    public void Execute_ImmediateBitOp_ImplementsBitVectorSemantics(
        Opcode opcode,
        uint source,
        int immediate,
        uint expected)
    {
        var initial = new ConcreteMachineState { [R1] = source };
        var sequence = new InstructionSequence(
        [
            Instruction.WithImmediate(opcode, R2, R1, immediate),
        ]);

        ConcreteMachineState result = new ConcreteExecutor().Execute(sequence, initial);

        Assert.That(result[R2], Is.EqualTo(expected));
    }

    [Test]
    public void Execute_ShiftRightArithmetic_PreservesSignBit()
    {
        var initial = new ConcreteMachineState { [R1] = 0x8000_0000u };
        var sequence = new InstructionSequence(
        [
            Instruction.WithImmediate(Opcode.ShiftRightArithmetic, R2, R1, 1),
        ]);

        ConcreteMachineState result = new ConcreteExecutor().Execute(sequence, initial);

        Assert.That(result[R2], Is.EqualTo(0xC000_0000u));
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

    [Test]
    public void Execute_Nor_IsNotOfOr()
    {
        var initial = new ConcreteMachineState { [R1] = 0xF0u, [R2] = 0x0Fu };
        var sequence = new InstructionSequence(
        [
            Instruction.Binary(Opcode.Nor, R3, R1, R2),
        ]);

        ConcreteMachineState result = new ConcreteExecutor().Execute(sequence, initial);

        Assert.That(result[R3], Is.EqualTo(~0xFFu));
    }

    [Test]
    public void Execute_MultiplyOverflow_KeepsLower32Bits()
    {
        var initial = new ConcreteMachineState
        {
            [R1] = 0x8000_0001u,
            [R2] = 2u,
        };
        var sequence = new InstructionSequence(
        [
            Instruction.Binary(Opcode.Multiply, R3, R1, R2),
        ]);

        ConcreteMachineState result = new ConcreteExecutor().Execute(sequence, initial);

        // 0x8000_0001 * 2 = 0x1_0000_0002  →  lower 32 bits = 2
        Assert.That(result[R3], Is.EqualTo(2u));
    }
}

