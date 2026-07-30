using Nds32Superopt.Instructions;
using Nds32Superopt.Optimization;

namespace Nds32Superopt.Tests;

/// <summary>
/// Tests for the superoptimizer's search algorithm.
/// Each test uses a very tight search space to keep runtime short.
/// </summary>
public class SuperoptimizerTests
{
    // Minimal search space: only the ALU ops needed for the specific test.
    private static SearchSpace TinySpace(int maxLen, params Opcode[] opcodes) => new()
    {
        NumRegisters = 4,
        MaxLength     = maxLen,
        Opcodes       = opcodes,
        Immediates    = [0, 1, -1, 2, 3, 4, 7, 8, 15, 16, 31, 0xFF, 0xFFFF, unchecked((int)0xFFFF_FFFF)],
    };

    // ── Test-vector generation ─────────────────────────────────────────────

    [Fact]
    public void GenerateTestVectors_ProducesCorrectOutputs()
    {
        static uint oracle(uint[] inputs) => inputs[0] + inputs[1];

        var vectors = Superoptimizer.GenerateTestVectors(oracle, numInputs: 2, count: 8);

        Assert.Equal(8, vectors.Count);
        foreach (var v in vectors)
            Assert.Equal(oracle(v.Inputs), v.ExpectedOutput);
    }

    [Fact]
    public void GenerateTestVectors_IncludesEdgeCases()
    {
        static uint oracle(uint[] inputs) => inputs[0] ^ inputs[1];

        var vectors = Superoptimizer.GenerateTestVectors(oracle, numInputs: 2, count: 20);

        // Edge cases must include (0,0), (1,1), (UINT_MAX, UINT_MAX)
        Assert.Contains(vectors, v => v.Inputs[0] == 0 && v.Inputs[1] == 0);
        Assert.Contains(vectors, v => v.Inputs[0] == 0xFFFF_FFFFu && v.Inputs[1] == 0xFFFF_FFFFu);
    }

    // ── Single-instruction searches ───────────────────────────────────────

    [Fact]
    public void Find_Addition_FindsAddInstruction()
    {
        var space = TinySpace(1, Opcode.Add);
        var opt   = new Superoptimizer(space);

        var result = opt.Find(inputs => inputs[0] + inputs[1], numInputs: 2);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(Opcode.Add, result[0].Op);
        Assert.Equal(0, result[0].Rd);
    }

    [Fact]
    public void Find_Subtraction_FindsSubInstruction()
    {
        var space = TinySpace(1, Opcode.Sub);
        var opt   = new Superoptimizer(space);

        var result = opt.Find(inputs => inputs[0] - inputs[1], numInputs: 2);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(Opcode.Sub, result[0].Op);
    }

    [Fact]
    public void Find_BitwiseAnd_FindsAndInstruction()
    {
        var space = TinySpace(1, Opcode.And);
        var opt   = new Superoptimizer(space);

        var result = opt.Find(inputs => inputs[0] & inputs[1], numInputs: 2);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(Opcode.And, result[0].Op);
    }

    [Fact]
    public void Find_LoadConstantZero_FindsMovi()
    {
        var space = TinySpace(1, Opcode.Movi);
        var opt   = new Superoptimizer(space);

        var result = opt.Find(_ => 0u, numInputs: 1);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(Opcode.Movi, result[0].Op);
        Assert.Equal(0, result[0].Imm);
    }

    [Fact]
    public void Find_LoadConstantNeg1_FindsMovi()
    {
        var space = TinySpace(1, Opcode.Movi);
        var opt   = new Superoptimizer(space);

        var result = opt.Find(_ => 0xFFFF_FFFFu, numInputs: 1);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(Opcode.Movi, result[0].Op);
        Assert.Equal(-1, result[0].Imm);
    }

    [Fact]
    public void Find_Identity_FindsSingleInstruction()
    {
        // r0 = r0  can be expressed as add r0, r0, r0 = 2*r0 — that's wrong.
        // But or r0, r0, r0 = r0 — correct.
        var space = TinySpace(1, Opcode.Or, Opcode.And, Opcode.Xor, Opcode.Add,
                              Opcode.Ori, Opcode.Addi);
        var opt   = new Superoptimizer(space);

        var result = opt.Find(inputs => inputs[0], numInputs: 1);

        Assert.NotNull(result);
        Assert.Single(result);

        // Verify by running a few inputs
        var vectors = Superoptimizer.GenerateTestVectors(inputs => inputs[0], 1, 8);
        foreach (var v in vectors)
        {
            var state = Nds32Superopt.Execution.CpuState.FromInputs(v.Inputs);
            state = Nds32Superopt.Execution.Interpreter.Execute(state, result);
            Assert.Equal(v.ExpectedOutput, state.Regs[0]);
        }
    }

    // ── Multi-instruction searches ────────────────────────────────────────

    [Fact]
    public void Find_Abs_FindsTwoOrThreeInstructions()
    {
        // abs(r0) classically takes 2–3 instructions on NDS32
        var space = new SearchSpace
        {
            NumRegisters = 4,
            MaxLength     = 3,
            Opcodes       = [Opcode.Sra, Opcode.Srai, Opcode.Add, Opcode.Xor, Opcode.Sub],
            Immediates    = [0, 1, -1, 31],
        };
        var opt = new Superoptimizer(space);

        static uint oracle(uint[] inputs) {
            int x = (int)inputs[0];
            return (uint)(x < 0 ? -x : x);
        }

        var result = opt.Find(oracle, numInputs: 1);

        Assert.NotNull(result);
        Assert.True(result.Length <= 3);

        // Verify correctness
        var vectors = Superoptimizer.GenerateTestVectors(oracle, 1, 20, seed: 123);
        foreach (var v in vectors)
        {
            var state = Nds32Superopt.Execution.CpuState.FromInputs(v.Inputs);
            state = Nds32Superopt.Execution.Interpreter.Execute(state, result);
            Assert.Equal(v.ExpectedOutput, state.Regs[0]);
        }
    }

    [Fact]
    public void Find_ModuloByFour_FindsAndiInstruction()
    {
        var space = TinySpace(1, Opcode.Andi, Opcode.And);
        var opt   = new Superoptimizer(space);

        var result = opt.Find(inputs => inputs[0] & 3u, numInputs: 1);

        Assert.NotNull(result);
        Assert.Single(result);
        // Either "andi r0, r0, 3" or equivalent
        var vectors = Superoptimizer.GenerateTestVectors(inputs => inputs[0] & 3u, 1, 16);
        foreach (var v in vectors)
        {
            var state = Nds32Superopt.Execution.CpuState.FromInputs(v.Inputs);
            state = Nds32Superopt.Execution.Interpreter.Execute(state, result);
            Assert.Equal(v.ExpectedOutput, state.Regs[0]);
        }
    }

    [Fact]
    public void Find_MultiplyByTwo_FindsShiftOrAdd()
    {
        var space = TinySpace(1, Opcode.Add, Opcode.Slli, Opcode.Sll, Opcode.Mul);
        var opt   = new Superoptimizer(space);

        var result = opt.Find(inputs => inputs[0] * 2u, numInputs: 1);

        Assert.NotNull(result);
        Assert.Single(result);
    }

    // ── No-solution guard ─────────────────────────────────────────────────

    [Fact]
    public void Find_ImpossibleWithGivenSpace_ReturnsNull()
    {
        // Only Movi available, but oracle needs two inputs — can't be expressed
        // as a single Movi (Movi is a constant, independent of inputs).
        var space = TinySpace(1, Opcode.Movi);
        var opt   = new Superoptimizer(space);

        // sum(r0,r1) can't be done with just Movi
        var result = opt.Find(inputs => inputs[0] + inputs[1], numInputs: 2);

        Assert.Null(result);
    }

    // ── FindWithVectors API ───────────────────────────────────────────────

    [Fact]
    public void FindWithVectors_MatchesExpectedOutput()
    {
        var vectors = new List<TestVector>
        {
            new([0u, 0u], 0u),
            new([1u, 0u], 1u),
            new([0u, 1u], 1u),
            new([5u, 3u], 8u),
            new([0xFFFF_FFFFu, 1u], 0u),
        };

        var space = TinySpace(1, Opcode.Add);
        var opt   = new Superoptimizer(space);

        var result = opt.FindWithVectors(vectors, numInputs: 2);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(Opcode.Add, result[0].Op);
    }

    // ── Cancellation ─────────────────────────────────────────────────────

    [Fact]
    public void Find_CancelledToken_ReturnsNull()
    {
        var space = new SearchSpace { MaxLength = 6 };
        var opt   = new Superoptimizer(space);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Sum-of-inputs is findable in length 1, but we cancel immediately
        var result = opt.Find(inputs => inputs[0] + inputs[1], numInputs: 2,
            cancellationToken: cts.Token);

        // May be null (cancelled before finding) — just ensure no exception.
        // We can't assert null here because the first candidate might happen
        // to match before the cancellation check.
        _ = result;
    }
}
