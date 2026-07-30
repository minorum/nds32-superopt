using Nds32.Superopt.Isa.Architecture;
using Nds32.Superopt.Isa.Semantics;

namespace Nds32.Superopt.Tests;

public sealed class ConcreteExecutorTests
{
    private static readonly Register R1 = new(1);
    private static readonly Register R2 = new(2);
    private static readonly Register R3 = new(3);

    [Test]
    public async Task Execute_AddImmediate_WrapsAtThirtyTwoBits()
    {
        var initial = new ConcreteMachineState { [R1] = uint.MaxValue };
        var sequence = new InstructionSequence(
        [
            Instruction.WithImmediate(Opcode.AddImmediate, R2, R1, 1),
        ]);

        ConcreteMachineState result = new ConcreteExecutor().Execute(sequence, initial);

        await Assert.That(result[R2]).IsEqualTo(0u);
    }

    [Test]
    [Arguments(Opcode.Add, 7u, 3u, 10u)]
    [Arguments(Opcode.Subtract, 7u, 3u, 4u)]
    [Arguments(Opcode.And, 0b1100u, 0b1010u, 0b1000u)]
    [Arguments(Opcode.Or, 0b1100u, 0b1010u, 0b1110u)]
    [Arguments(Opcode.Xor, 0b1100u, 0b1010u, 0b0110u)]
    [Arguments(Opcode.Multiply, 6u, 7u, 42u)]
    [Arguments(Opcode.SetLessThan, 1u, 2u, 1u)]
    [Arguments(Opcode.SetLessThan, 2u, 1u, 0u)]
    [Arguments(Opcode.SetLessThan, uint.MaxValue, 0u, 1u)]
    [Arguments(Opcode.SetLessThanUnsigned, 1u, 2u, 1u)]
    [Arguments(Opcode.SetLessThanUnsigned, uint.MaxValue, 0u, 0u)]
    public async Task Execute_BinaryInstruction_ImplementsBitVectorSemantics(
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

        await Assert.That(result[R3]).IsEqualTo(expected);
    }

    [Test]
    [Arguments(Opcode.AndImmediate, 0b1111u, 0b1010, 0b1010u)]
    [Arguments(Opcode.OrImmediate, 0b1100u, 0b0011, 0b1111u)]
    [Arguments(Opcode.XorImmediate, 0b1111u, 0b1010, 0b0101u)]
    public async Task Execute_ImmediateBitOp_ImplementsBitVectorSemantics(
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

        await Assert.That(result[R2]).IsEqualTo(expected);
    }

    [Test]
    public async Task Execute_ShiftRightArithmetic_PreservesSignBit()
    {
        var initial = new ConcreteMachineState { [R1] = 0x8000_0000u };
        var sequence = new InstructionSequence(
        [
            Instruction.WithImmediate(Opcode.ShiftRightArithmetic, R2, R1, 1),
        ]);

        ConcreteMachineState result = new ConcreteExecutor().Execute(sequence, initial);

        await Assert.That(result[R2]).IsEqualTo(0xC000_0000u);
    }

    [Test]
    public async Task Execute_DoesNotMutateInitialState()
    {
        var initial = new ConcreteMachineState { [R1] = 41 };
        var sequence = new InstructionSequence(
        [
            Instruction.WithImmediate(Opcode.AddImmediate, R1, R1, 1),
        ]);

        _ = new ConcreteExecutor().Execute(sequence, initial);

        await Assert.That(initial[R1]).IsEqualTo(41u);
    }

    [Test]
    public async Task Execute_MultiplyOverflow_KeepsLower32Bits()
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

        await Assert.That(result[R3]).IsEqualTo(2u);
    }
}