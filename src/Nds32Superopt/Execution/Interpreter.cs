using System.Runtime.CompilerServices;
using Nds32Superopt.Instructions;

namespace Nds32Superopt.Execution;

/// <summary>
/// Pure functional interpreter for the NDS32 instruction subset used by the superoptimizer.
/// All methods are static and allocation-free; they return a new (mutated copy of) <see cref="CpuState"/>.
/// </summary>
public static class Interpreter
{
    /// <summary>
    /// Executes a single <paramref name="inst"/> against <paramref name="state"/> and
    /// returns the updated state.  If <see cref="CpuState.Exception"/> is already set,
    /// the state is returned unchanged.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CpuState Execute(CpuState state, Instruction inst)
    {
        if (state.Exception) return state;

        uint rs1 = state.Regs[inst.Rs1];
        uint rs2 = state.Regs[inst.Rs2];

        uint result = inst.Op switch
        {
            // ── R-type ────────────────────────────────────────────────────────
            Opcode.Add  => rs1 + rs2,
            Opcode.Sub  => rs1 - rs2,
            Opcode.Mul  => rs1 * rs2,
            Opcode.And  => rs1 & rs2,
            Opcode.Or   => rs1 | rs2,
            Opcode.Xor  => rs1 ^ rs2,
            Opcode.Nor  => ~(rs1 | rs2),
            Opcode.Sll  => rs1 << (int)(rs2 & 31),
            Opcode.Srl  => rs1 >> (int)(rs2 & 31),
            Opcode.Sra  => (uint)((int)rs1 >> (int)(rs2 & 31)),
            Opcode.Slt  => (int)rs1 < (int)rs2 ? 1u : 0u,
            Opcode.Sltu => rs1 < rs2 ? 1u : 0u,

            // ── I-type ────────────────────────────────────────────────────────
            Opcode.Addi  => rs1 + (uint)inst.Imm,
            Opcode.Andi  => rs1 & (uint)inst.Imm,
            Opcode.Ori   => rs1 | (uint)inst.Imm,
            Opcode.Xori  => rs1 ^ (uint)inst.Imm,
            Opcode.Slli  => rs1 << (inst.Imm & 31),
            Opcode.Srli  => rs1 >> (inst.Imm & 31),
            Opcode.Srai  => (uint)((int)rs1 >> (inst.Imm & 31)),
            Opcode.Slti  => (int)rs1 < inst.Imm ? 1u : 0u,
            Opcode.Sltui => rs1 < (uint)inst.Imm ? 1u : 0u,

            // ── U-type ────────────────────────────────────────────────────────
            Opcode.Movi  => (uint)inst.Imm,
            Opcode.Sethi => (uint)inst.Imm << 12,

            _ => 0u,
        };

        state.Regs[inst.Rd] = result;
        return state;
    }

    /// <summary>
    /// Executes an entire <paramref name="program"/> (sequence of instructions)
    /// against <paramref name="state"/>, returning the final state.
    /// Execution stops early if <see cref="CpuState.Exception"/> is raised.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CpuState Execute(CpuState state, ReadOnlySpan<Instruction> program)
    {
        foreach (ref readonly var inst in program)
        {
            state = Execute(state, inst);
            if (state.Exception) break;
        }
        return state;
    }
}
