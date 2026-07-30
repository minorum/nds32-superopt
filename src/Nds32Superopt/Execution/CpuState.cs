using System.Runtime.CompilerServices;

namespace Nds32Superopt.Execution;

/// <summary>
/// Fixed-size array of 8 uint registers, backed by an inline array for zero-allocation
/// indexing without bounds-check overhead in hot paths.
/// </summary>
[InlineArray(8)]
public struct RegisterFile
{
    // InlineArray requires exactly one named field.
    private uint _e0;
}

/// <summary>
/// Minimal NDS32 CPU state used by the superoptimizer:
/// 8 general-purpose 32-bit registers (r0–r7) and an exception flag.
/// <para>
/// r0–r1 are conventionally live-in (inputs); r0 is the live-out (output).
/// r2–r7 are scratch.  The same register conventions that the NDS32 ABI uses
/// for small leaf functions apply here.
/// </para>
/// </summary>
public struct CpuState
{
    /// <summary>General-purpose registers r0–r7.</summary>
    public RegisterFile Regs;

    /// <summary>
    /// Set to <see langword="true"/> when an instruction raises a fault
    /// (e.g. division by zero).  Once set, subsequent instructions are skipped.
    /// </summary>
    public bool Exception;

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Creates a fresh state with the given register values as live-in inputs.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CpuState FromInputs(ReadOnlySpan<uint> inputs)
    {
        CpuState s = default;
        for (int i = 0; i < inputs.Length && i < 8; i++)
            s.Regs[i] = inputs[i];
        return s;
    }

    /// <summary>
    /// Returns a fingerprint of the live-out registers specified by
    /// <paramref name="outputRegs"/>, encoded as a compact ulong array.
    /// Used for equivalence testing.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly uint OutputAt(int regIndex) => Regs[regIndex];
}
