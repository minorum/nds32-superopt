using System.Diagnostics;
using Nds32.Superopt.Isa.Architecture;

namespace Nds32.Superopt.Isa.Semantics;

public sealed class ConcreteExecutor
{
    public ConcreteMachineState Execute(
        InstructionSequence sequence,
        ConcreteMachineState initialState)
    {
        ConcreteMachineState state = initialState.Clone();

        foreach (Instruction instruction in sequence.Instructions)
        {
            Execute(instruction, state);
        }

        return state;
    }

    private static void Execute(Instruction instruction, ConcreteMachineState state)
    {
        uint source1 = state[instruction.Source1];
        uint source2 = state[instruction.Source2];

        state[instruction.Destination] = instruction.Opcode switch
        {
            Opcode.Add => unchecked(source1 + source2),
            Opcode.AddImmediate => unchecked(source1 + (uint)instruction.Immediate),
            Opcode.Subtract => unchecked(source1 - source2),
            Opcode.And => source1 & source2,
            Opcode.Or => source1 | source2,
            Opcode.Xor => source1 ^ source2,
            Opcode.ShiftLeft => source1 << instruction.Immediate,
            Opcode.ShiftRightLogical => source1 >> instruction.Immediate,
            Opcode.ShiftRightArithmetic => unchecked((uint)((int)source1 >> instruction.Immediate)),
            Opcode.Move55 => source1,
            Opcode.MoveImmediate => unchecked((uint)instruction.Immediate),
            Opcode.MoveImmediate55 => unchecked((uint)instruction.Immediate),
            Opcode.Add45 => unchecked(source1 + source2),
            Opcode.AddImmediate45 => unchecked(source1 + (uint)instruction.Immediate),
            _ => throw new UnreachableException($"Unsupported opcode: {instruction.Opcode}"),
        };
    }
}
