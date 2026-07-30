using Nds32Superopt.Instructions;

namespace Nds32Superopt.Optimization;

/// <summary>
/// Lazily enumerates every possible program of a fixed <see cref="_length"/> over a
/// given <see cref="SearchSpace"/>, with two key pruning rules:
/// <list type="number">
///   <item>
///     Source registers must be <em>defined</em>: either a live-in register
///     (r0 … r<paramref name="numInputs"/>-1) or written by an earlier instruction.
///     This prevents programs from reading uninitialised scratch registers.
///   </item>
///   <item>
///     The last instruction must write to r0 (the live-out result register).
///     Any program whose final instruction doesn't produce the output can be
///     safely skipped.
///   </item>
/// </list>
/// Together these rules reduce the search space from O(N^L) to a much smaller set
/// without missing any valid optimised program.
/// </summary>
internal sealed class ProgramEnumerator(SearchSpace space, int length, int numInputs)
{
    private readonly SearchSpace _space = space;
    private readonly int _length = length;
    private readonly int _numInputs = numInputs;

    /// <summary>
    /// Yields all candidate programs of exactly <see cref="_length"/> instructions
    /// that pass the pruning rules.
    /// </summary>
    public IEnumerable<Instruction[]> Enumerate()
    {
        var program = new Instruction[_length];

        // Compute initial defined-register bitmask from live-in inputs.
        int initialDefined = 0;
        for (int i = 0; i < _numInputs && i < 32; i++)
            initialDefined |= 1 << i;

        return EnumerateAt(program, 0, initialDefined);
    }

    private IEnumerable<Instruction[]> EnumerateAt(Instruction[] program, int pos, int defined)
    {
        if (pos == _length)
        {
            yield return program;
            yield break;
        }

        bool isLast = pos == _length - 1;
        int n = _space.NumRegisters;

        // ── R-type instructions ───────────────────────────────────────────────
        foreach (var op in _space.RTypeOpcodes)
        {
            for (byte rd = 0; rd < n; rd++)
            {
                if (isLast && rd != 0) continue; // last inst must produce r0

                for (byte rs1 = 0; rs1 < n; rs1++)
                {
                    if ((defined & (1 << rs1)) == 0) continue; // rs1 must be defined

                    for (byte rs2 = 0; rs2 < n; rs2++)
                    {
                        if ((defined & (1 << rs2)) == 0) continue; // rs2 must be defined

                        program[pos] = Instruction.R(op, rd, rs1, rs2);
                        foreach (var p in EnumerateAt(program, pos + 1, defined | (1 << rd)))
                            yield return p;
                    }
                }
            }
        }

        // ── I-type instructions ───────────────────────────────────────────────
        foreach (var op in _space.ITypeOpcodes)
        {
            for (byte rd = 0; rd < n; rd++)
            {
                if (isLast && rd != 0) continue;

                for (byte rs1 = 0; rs1 < n; rs1++)
                {
                    if ((defined & (1 << rs1)) == 0) continue;

                    foreach (var imm in _space.Immediates)
                    {
                        program[pos] = Instruction.I(op, rd, rs1, imm);
                        foreach (var p in EnumerateAt(program, pos + 1, defined | (1 << rd)))
                            yield return p;
                    }
                }
            }
        }

        // ── U-type instructions ───────────────────────────────────────────────
        foreach (var op in _space.UTypeOpcodes)
        {
            for (byte rd = 0; rd < n; rd++)
            {
                if (isLast && rd != 0) continue;

                foreach (var imm in _space.Immediates)
                {
                    program[pos] = Instruction.U(op, rd, imm);
                    foreach (var p in EnumerateAt(program, pos + 1, defined | (1 << rd)))
                        yield return p;
                }
            }
        }
    }
}

