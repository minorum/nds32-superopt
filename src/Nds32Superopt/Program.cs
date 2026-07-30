using Nds32Superopt.Instructions;
using Nds32Superopt.Optimization;

// ── CLI entry point ───────────────────────────────────────────────────────────

var banner = """
    ╔══════════════════════════════════════════════════════╗
    ║  nds32-superopt  –  NDS32 offline superoptimizer     ║
    ║  C#14 / .NET 10 / NativeAOT                          ║
    ╚══════════════════════════════════════════════════════╝
    """;

Console.WriteLine(banner);

if (args.Length > 0 && args[0] is "--help" or "-h")
{
    PrintHelp();
    return 0;
}

// ── Demo mode (default when no arguments are given) ───────────────────────────

Console.WriteLine("Running built-in demonstration optimizations...");
Console.WriteLine();

RunDemo();

return 0;

// ─────────────────────────────────────────────────────────────────────────────

static void RunDemo()
{
    // ── abs(r0)  — absolute value ─────────────────────────────────────────────
    // Classic NDS32 sequence:
    //   srai  r1, r0, 31   ; sign mask: 0xFFFFFFFF if negative, 0 if positive
    //   add   r0, r0, r1   ; r0 += sign mask
    //   xor   r0, r0, r1   ; flip bits if negative  →  abs(r0)
    DemoCase("abs(r0)     — absolute value of a signed integer",
        new SearchSpace
        {
            NumRegisters = 3,
            MaxLength     = 3,
            Opcodes       = [Opcode.Srai, Opcode.Add, Opcode.Xor],
            Immediates    = [31],
        },
        numInputs: 1,
        oracle: inputs => { int x = (int)inputs[0]; return (uint)(x < 0 ? -x : x); });

    // ── r0 & ~r1  — bit-clear (AND NOT) ─────────────────────────────────────
    // NDS32 has no ANDN; emulate with NOR + AND:
    //   nor   r2, r1, r1   ; r2 = ~r1
    //   and   r0, r0, r2
    DemoCase("r0 & ~r1    — bit-clear (AND NOT)",
        new SearchSpace
        {
            NumRegisters = 4,
            MaxLength     = 2,
            Opcodes       = [Opcode.Nor, Opcode.And],
            Immediates    = [],
        },
        numInputs: 2,
        oracle: inputs => inputs[0] & ~inputs[1]);

    // ── r0 % 4  — unsigned modulo by power-of-two ────────────────────────────
    DemoCase("r0 % 4      — unsigned modulo by 4",
        new SearchSpace
        {
            NumRegisters = 2,
            MaxLength     = 1,
            Opcodes       = [Opcode.Andi],
            Immediates    = [3],
        },
        numInputs: 1,
        oracle: inputs => inputs[0] & 3u);

    // ── r0 * 3  — multiply by 3 ──────────────────────────────────────────────
    DemoCase("r0 * 3      — multiply by 3 via add",
        new SearchSpace
        {
            NumRegisters = 3,
            MaxLength     = 2,
            Opcodes       = [Opcode.Add],
            Immediates    = [],
        },
        numInputs: 1,
        oracle: inputs => inputs[0] * 3u);

    // ── min(r0, r1)  — signed minimum ────────────────────────────────────────
    // Branchless: min(a,b) = b + ((a−b) & (a−b)>>31)
    //   sub   r2, r0, r1
    //   srai  r3, r2, 31
    //   and   r2, r2, r3
    //   add   r0, r1, r2
    DemoCase("min(r0, r1) — signed minimum of two values",
        new SearchSpace
        {
            NumRegisters = 4,
            MaxLength     = 4,
            Opcodes       = [Opcode.Sub, Opcode.Srai, Opcode.And, Opcode.Add],
            Immediates    = [31],
        },
        numInputs: 2,
        oracle: inputs => { int a = (int)inputs[0], b = (int)inputs[1]; return (uint)(a < b ? a : b); });

    // ── max(r0, r1)  — signed maximum ────────────────────────────────────────
    // Branchless: max(a,b) = a − ((a−b) & (a−b)>>31)
    //   sub   r2, r0, r1
    //   srai  r3, r2, 31
    //   and   r2, r2, r3
    //   sub   r0, r0, r2
    DemoCase("max(r0, r1) — signed maximum of two values",
        new SearchSpace
        {
            NumRegisters = 4,
            MaxLength     = 4,
            Opcodes       = [Opcode.Sub, Opcode.Srai, Opcode.And],
            Immediates    = [31],
        },
        numInputs: 2,
        oracle: inputs => { int a = (int)inputs[0], b = (int)inputs[1]; return (uint)(a > b ? a : b); });

    // ── sign(r0)  — signum: −1 / 0 / +1 ─────────────────────────────────────
    // sign(x) = (x > 0) | (x >> 31)
    //   sub   r2, r0, r0   ; r2 = 0  (constant zero without Movi)
    //   slt   r3, r2, r0   ; r3 = (0 < r0) = 1 if positive, 0 otherwise
    //   srai  r2, r0, 31   ; r2 = -1 if negative, 0 if non-negative
    //   or    r0, r3, r2
    DemoCase("sign(r0)    — signum: −1 / 0 / +1",
        new SearchSpace
        {
            NumRegisters = 4,
            MaxLength     = 4,
            Opcodes       = [Opcode.Sub, Opcode.Slt, Opcode.Srai, Opcode.Or],
            Immediates    = [31],
        },
        numInputs: 1,
        oracle: inputs =>
        {
            int x = (int)inputs[0];
            return x == 0 ? 0u : x > 0 ? 1u : unchecked((uint)-1);
        });
}

static void DemoCase(
    string description,
    SearchSpace space,
    int numInputs,
    Func<uint[], uint> oracle)
{
    Console.Write($"  {description} … ");

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    var opt = new Superoptimizer(space);
    var sw  = System.Diagnostics.Stopwatch.StartNew();
    var result = opt.Find(oracle, numInputs: numInputs, cancellationToken: cts.Token);
    sw.Stop();

    if (result is null)
    {
        Console.WriteLine($"(no solution found within search bounds, {sw.ElapsedMilliseconds} ms)");
    }
    else
    {
        Console.WriteLine($"({sw.ElapsedMilliseconds} ms, {result.Length} instruction{(result.Length == 1 ? "" : "s")})");
        foreach (var inst in result)
            Console.WriteLine($"      {inst}");
    }
    Console.WriteLine();
}

static void PrintHelp()
{
    Console.WriteLine("""
        USAGE
          nds32-superopt [--help]

        DESCRIPTION
          Runs the built-in NDS32 superoptimizer demonstrations.
          For each canonical function (abs, min, max, …) the tool finds the
          shortest equivalent NDS32 program through exhaustive enumeration.

        OPTIONS
          --help, -h   Show this help message and exit.

        SUPPORTED OPCODES (full search space)
          R-type : add sub mul and or xor nor sll srl sra slt sltu
          I-type : addi andi ori xori slli srli srai slti sltui
          U-type : movi sethi

        REGISTER CONVENTION
          r0 … r(numInputs-1)   live-in inputs
          r0                    live-out result
          remaining registers   scratch (zero-initialised)

        DEMO CASES
          abs(r0)        — absolute value (3 instructions)
          r0 & ~r1       — bit-clear, AND NOT (2 instructions)
          r0 % 4         — unsigned modulo by 4 (1 instruction)
          r0 * 3         — multiply by constant (2 instructions)
          min(r0, r1)    — signed minimum (4 instructions)
          max(r0, r1)    — signed maximum (4 instructions)
          sign(r0)       — signum: −1/0/+1 (4 instructions)
        """);
}

