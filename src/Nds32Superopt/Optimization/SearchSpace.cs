using Nds32Superopt.Instructions;

namespace Nds32Superopt.Optimization;

/// <summary>
/// Defines the universe of instructions the superoptimizer may use when
/// constructing candidate programs.
/// </summary>
public sealed class SearchSpace
{
    // ── Defaults ──────────────────────────────────────────────────────────────

    /// <summary>Default set of opcodes considered during search.</summary>
    public static readonly Opcode[] DefaultOpcodes =
    [
        Opcode.Add, Opcode.Sub, Opcode.And, Opcode.Or, Opcode.Xor,
        Opcode.Nor, Opcode.Sll, Opcode.Srl, Opcode.Sra,
        Opcode.Mul, Opcode.Slt, Opcode.Sltu,
        Opcode.Addi, Opcode.Andi, Opcode.Ori, Opcode.Xori,
        Opcode.Slli, Opcode.Srli, Opcode.Srai,
        Opcode.Slti, Opcode.Sltui,
        Opcode.Movi, Opcode.Sethi,
    ];

    /// <summary>
    /// Default set of small immediates tried for I-type and U-type instructions.
    /// Chosen to cover common bit-manipulation patterns.
    /// </summary>
    public static readonly int[] DefaultImmediates =
    [
        0, 1, -1, 2, -2, 3, 4, 7, 8, 15, 16, 31, 32,
        0xFF, 0xFFFF, unchecked((int)0xFFFF_0000),
        unchecked((int)0x8000_0000),
        unchecked((int)0xFFFF_FFFF),
    ];

    // ── Configuration ─────────────────────────────────────────────────────────

    /// <summary>Opcodes the superoptimizer is allowed to emit.</summary>
    public Opcode[] Opcodes { get; init; } = DefaultOpcodes;

    /// <summary>
    /// Number of registers available (0 … <see cref="NumRegisters"/>-1).
    /// Defaults to 4 (r0–r3).
    /// </summary>
    public int NumRegisters { get; init; } = 4;

    /// <summary>Immediate values to try for I-type and U-type instructions.</summary>
    public int[] Immediates { get; init; } = DefaultImmediates;

    /// <summary>Maximum program length (in instructions) to search.</summary>
    public int MaxLength { get; init; } = 6;

    // ── Lazily-computed splits ────────────────────────────────────────────────
    // Computed on first access so that object-initializer syntax works correctly:
    // the init properties (Opcodes, etc.) are set *after* the constructor body
    // runs, so any constructor-time precomputation would see the default values.

    private Opcode[]? _rTypeOpcodes;
    private Opcode[]? _iTypeOpcodes;
    private Opcode[]? _uTypeOpcodes;

    /// <summary>R-type opcodes extracted from <see cref="Opcodes"/>.</summary>
    public Opcode[] RTypeOpcodes => _rTypeOpcodes ??= FilterOpcodes(OpcodeKind.R);

    /// <summary>I-type opcodes extracted from <see cref="Opcodes"/>.</summary>
    public Opcode[] ITypeOpcodes => _iTypeOpcodes ??= FilterOpcodes(OpcodeKind.I);

    /// <summary>U-type opcodes extracted from <see cref="Opcodes"/>.</summary>
    public Opcode[] UTypeOpcodes => _uTypeOpcodes ??= FilterOpcodes(OpcodeKind.U);

    private enum OpcodeKind { R, I, U }

    private Opcode[] FilterOpcodes(OpcodeKind kind)
    {
        var list = new List<Opcode>();
        foreach (var op in Opcodes)
        {
            bool match = kind switch
            {
                OpcodeKind.R => op.IsRType(),
                OpcodeKind.I => op.IsIType(),
                _            => op.IsUType(),
            };
            if (match) list.Add(op);
        }
        return [.. list];
    }
}

