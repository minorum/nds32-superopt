using Nds32Superopt.Execution;
using Nds32Superopt.Instructions;

namespace Nds32Superopt.Tests;

/// <summary>Unit tests for the NDS32 interpreter.</summary>
public class InterpreterTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CpuState State(uint r0 = 0, uint r1 = 0, uint r2 = 0, uint r3 = 0)
    {
        var s = new CpuState();
        s.Regs[0] = r0;
        s.Regs[1] = r1;
        s.Regs[2] = r2;
        s.Regs[3] = r3;
        return s;
    }

    private static uint Exec(Instruction inst, uint r0 = 0, uint r1 = 0, uint r2 = 0, uint r3 = 0)
        => Interpreter.Execute(State(r0, r1, r2, r3), inst).Regs[0];

    // ── R-type ───────────────────────────────────────────────────────────────

    [Fact]
    public void Add_BasicAddition()
    {
        var inst = Instruction.R(Opcode.Add, 0, 0, 1);
        Assert.Equal(7u, Exec(inst, 3, 4));
    }

    [Fact]
    public void Add_Overflow_WrapsAround()
    {
        var inst = Instruction.R(Opcode.Add, 0, 0, 1);
        Assert.Equal(0u, Exec(inst, 0xFFFF_FFFFu, 1));
    }

    [Fact]
    public void Sub_BasicSubtraction()
    {
        var inst = Instruction.R(Opcode.Sub, 0, 0, 1);
        Assert.Equal(2u, Exec(inst, 5, 3));
    }

    [Fact]
    public void Sub_Underflow_Wraps()
    {
        var inst = Instruction.R(Opcode.Sub, 0, 0, 1);
        Assert.Equal(0xFFFF_FFFFu, Exec(inst, 0, 1));
    }

    [Fact]
    public void Mul_BasicMultiplication()
    {
        var inst = Instruction.R(Opcode.Mul, 0, 0, 1);
        Assert.Equal(12u, Exec(inst, 3, 4));
    }

    [Fact]
    public void Mul_Overflow_KeepsLower32Bits()
    {
        var inst = Instruction.R(Opcode.Mul, 0, 0, 1);
        // 0x8000_0001 * 2 = 0x1_0000_0002  → lower 32 bits = 2
        Assert.Equal(2u, Exec(inst, 0x8000_0001u, 2));
    }

    [Fact]
    public void And_BitwiseAnd()
    {
        var inst = Instruction.R(Opcode.And, 0, 0, 1);
        Assert.Equal(0x0Fu, Exec(inst, 0xFF, 0x0F));
    }

    [Fact]
    public void Or_BitwiseOr()
    {
        var inst = Instruction.R(Opcode.Or, 0, 0, 1);
        Assert.Equal(0xFFu, Exec(inst, 0xF0, 0x0F));
    }

    [Fact]
    public void Xor_BitwiseXor()
    {
        var inst = Instruction.R(Opcode.Xor, 0, 0, 1);
        Assert.Equal(0xFFu, Exec(inst, 0xF0, 0x0F));
        Assert.Equal(0u, Exec(inst, 0xFF, 0xFF));
    }

    [Fact]
    public void Nor_BitwiseNor()
    {
        var inst = Instruction.R(Opcode.Nor, 0, 0, 1);
        Assert.Equal(~0xFFu, Exec(inst, 0xF0, 0x0F));
    }

    [Fact]
    public void Sll_ShiftLeft()
    {
        var inst = Instruction.R(Opcode.Sll, 0, 0, 1);
        Assert.Equal(8u, Exec(inst, 1, 3));
    }

    [Fact]
    public void Sll_ShiftAmount_MaskedTo5Bits()
    {
        var inst = Instruction.R(Opcode.Sll, 0, 0, 1);
        // Shift by 32 (≡ 0 mod 32) → no shift
        Assert.Equal(1u, Exec(inst, 1, 32));
    }

    [Fact]
    public void Srl_LogicalShiftRight()
    {
        var inst = Instruction.R(Opcode.Srl, 0, 0, 1);
        Assert.Equal(0x4000_0000u, Exec(inst, 0x8000_0000u, 1));
    }

    [Fact]
    public void Sra_ArithmeticShiftRight_PreservesSign()
    {
        var inst = Instruction.R(Opcode.Sra, 0, 0, 1);
        Assert.Equal(0xFFFF_FFFFu, Exec(inst, 0xFFFF_FFFFu, 1));
        Assert.Equal(0xC000_0000u, Exec(inst, 0x8000_0000u, 1));
    }

    [Fact]
    public void Slt_SignedLessThan()
    {
        var inst = Instruction.R(Opcode.Slt, 0, 0, 1);
        Assert.Equal(1u, Exec(inst, 0xFFFF_FFFFu /*= -1*/, 0 /*= 0*/));  // -1 < 0
        Assert.Equal(0u, Exec(inst, 0u, 0xFFFF_FFFFu));                   // 0 < -1 = false
    }

    [Fact]
    public void Sltu_UnsignedLessThan()
    {
        var inst = Instruction.R(Opcode.Sltu, 0, 0, 1);
        Assert.Equal(1u, Exec(inst, 0u, 1u));
        Assert.Equal(1u, Exec(inst, 0u, 0xFFFF_FFFFu));  // 0 < UINT_MAX
        Assert.Equal(0u, Exec(inst, 0xFFFF_FFFFu, 0u));  // UINT_MAX < 0 = false
    }

    // ── I-type ────────────────────────────────────────────────────────────────

    [Fact]
    public void Addi_SignedImmediate()
    {
        var inst = Instruction.I(Opcode.Addi, 0, 0, -1);
        Assert.Equal(0xFFFF_FFFFu, Exec(inst, 0));
    }

    [Fact]
    public void Andi_ZeroExtendedImmediate()
    {
        var inst = Instruction.I(Opcode.Andi, 0, 0, 0x0F);
        Assert.Equal(0x0Au, Exec(inst, 0xFFFF_FFAAu));
    }

    [Fact]
    public void Ori_ZeroExtendedImmediate()
    {
        var inst = Instruction.I(Opcode.Ori, 0, 0, 0x0F);
        Assert.Equal(0x0Fu, Exec(inst, 0));
    }

    [Fact]
    public void Xori_ZeroExtendedImmediate()
    {
        var inst = Instruction.I(Opcode.Xori, 0, 0, 0xFF);
        Assert.Equal(0u, Exec(inst, 0xFF));
    }

    [Fact]
    public void Slli_ShiftLeftImmediate()
    {
        var inst = Instruction.I(Opcode.Slli, 0, 0, 2);
        Assert.Equal(12u, Exec(inst, 3));
    }

    [Fact]
    public void Srli_LogicalShiftRightImmediate()
    {
        var inst = Instruction.I(Opcode.Srli, 0, 0, 1);
        Assert.Equal(0x4000_0000u, Exec(inst, 0x8000_0000u));
    }

    [Fact]
    public void Srai_ArithmeticShiftRightImmediate()
    {
        var inst = Instruction.I(Opcode.Srai, 0, 0, 1);
        Assert.Equal(0xC000_0000u, Exec(inst, 0x8000_0000u));
    }

    [Fact]
    public void Slti_SignedImmediateLessThan()
    {
        var inst = Instruction.I(Opcode.Slti, 0, 0, 0);
        Assert.Equal(1u, Exec(inst, 0xFFFF_FFFFu));  // -1 < 0
        Assert.Equal(0u, Exec(inst, 0u));             // 0 < 0 = false
    }

    [Fact]
    public void Sltui_UnsignedImmediateLessThan()
    {
        var inst = Instruction.I(Opcode.Sltui, 0, 0, 5);
        Assert.Equal(1u, Exec(inst, 4));
        Assert.Equal(0u, Exec(inst, 5));
    }

    // ── U-type ────────────────────────────────────────────────────────────────

    [Fact]
    public void Movi_LoadSignedImmediate()
    {
        var inst = Instruction.U(Opcode.Movi, 0, -1);
        Assert.Equal(0xFFFF_FFFFu, Exec(inst));
    }

    [Fact]
    public void Movi_LoadPositiveImmediate()
    {
        var inst = Instruction.U(Opcode.Movi, 0, 42);
        Assert.Equal(42u, Exec(inst));
    }

    [Fact]
    public void Sethi_LoadHighBits()
    {
        var inst = Instruction.U(Opcode.Sethi, 0, 1);
        Assert.Equal(4096u, Exec(inst));  // 1 << 12
    }

    // ── Exception handling ───────────────────────────────────────────────────

    [Fact]
    public void Execute_ExceptionSet_InstructionSkipped()
    {
        var state = State(0xDEAD, 0, 0, 0);
        state.Exception = true;

        var inst = Instruction.U(Opcode.Movi, 0, 0);  // would set r0 = 0
        var after = Interpreter.Execute(state, inst);

        // r0 should remain unchanged because Exception was set
        Assert.Equal(0xDEADu, after.Regs[0]);
        Assert.True(after.Exception);
    }

    // ── Multi-instruction programs ───────────────────────────────────────────

    [Fact]
    public void ExecuteProgram_ComputesSumOfSquares()
    {
        // r0 = r0*r0 + r1*r1
        // mul r2, r0, r0
        // mul r3, r1, r1
        // add r0, r2, r3
        Instruction[] prog =
        [
            Instruction.R(Opcode.Mul, 2, 0, 0),
            Instruction.R(Opcode.Mul, 3, 1, 1),
            Instruction.R(Opcode.Add, 0, 2, 3),
        ];

        var state = CpuState.FromInputs([3u, 4u]);
        var result = Interpreter.Execute(state, prog);

        Assert.Equal(25u, result.Regs[0]);  // 3² + 4² = 9 + 16 = 25
    }

    [Fact]
    public void ExecuteProgram_Abs_WithShiftAndSub()
    {
        // Classic abs(r0) = ((r0 >> 31) + r0) ^ (r0 >> 31)
        // sra r1, r0, 31
        // add r0, r0, r1
        // xor r0, r0, r1
        Instruction[] prog =
        [
            Instruction.I(Opcode.Srai, 1, 0, 31),
            Instruction.R(Opcode.Add, 0, 0, 1),
            Instruction.R(Opcode.Xor, 0, 0, 1),
        ];

        var statePos = CpuState.FromInputs([5u]);
        var stateNeg = CpuState.FromInputs([unchecked((uint)-5)]);

        Assert.Equal(5u, Interpreter.Execute(statePos, prog).Regs[0]);
        Assert.Equal(5u, Interpreter.Execute(stateNeg, prog).Regs[0]);
    }

    // ── Instruction.ToString ─────────────────────────────────────────────────

    [Theory]
    [InlineData(Opcode.Add,  0, 1, 2, 0, "add\tr0, r1, r2")]
    [InlineData(Opcode.Addi, 0, 1, 0, 5, "addi\tr0, r1, 5")]
    [InlineData(Opcode.Movi, 0, 0, 0, 7, "movi\tr0, 7")]
    public void Instruction_ToString_FormatsCorrectly(
        Opcode op, byte rd, byte rs1, byte rs2, int imm, string expected)
    {
        var inst = new Instruction(op, rd, rs1, rs2, imm);
        Assert.Equal(expected, inst.ToString());
    }
}
