# NDS32 Superoptimizer

Offline sequence synthesis and verification tooling for the Andes NDS32 LLVM backend.

The intended architecture is:

```text
C# offline tool
  ├─ independent NDS32 ISA semantics
  ├─ candidate enumeration
  ├─ counterexample filtering
  ├─ formal equivalence verification
  ├─ cost models
  └─ generated LLVM rewrite rules and tests

LLVM C++ integration
  └─ cheap generated matcher/rewriter used during normal compilation
```

## Current implementation

The first executable slice contains:

- a compact scalar NDS32 instruction model;
- encoding predicates and byte-size costs for selected 32-bit and 16-bit forms;
- explicit register read/write effects;
- a concrete machine-state executor;
- deterministic edge-case and randomized counterexample testing;
- complete register-file comparison, including unintended clobbers;
- bounded single-instruction candidate enumeration;
- a code-size-first candidate search;
- a .NET 10 / C# 14 NativeAOT command-line application;
- TUnit tests running on Microsoft.Testing.Platform;
- GitHub Actions build, test, NativeAOT publish, and executable smoke checks.

`ConcreteEquivalenceVerifier` is only a fast counterexample finder. A successful result is intentionally called `NoCounterexample`, **not a proof of equivalence**. Generated LLVM rewrites must eventually require `ProvenEquivalent` evidence from an SMT-backed verifier.

## Build and test

```bash
dotnet restore Nds32.Superopt.slnx
dotnet build Nds32.Superopt.slnx -c Release
dotnet run --project tests/Nds32.Superopt.Tests -c Release --no-build
```

## Run the demonstration

```bash
dotnet run --project src/Nds32.Superopt.Cli -- demo
```

It searches for smaller candidates for several built-in instruction sequences, including:

```text
addi r2, r1, 0
```

which should produce:

```text
mov55 r2, r1
```

Every result from the current concrete verifier is clearly reported as unproven.

## Publish NativeAOT

```bash
dotnet publish src/Nds32.Superopt.Cli/Nds32.Superopt.Cli.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained
```

## Next milestones

1. Add an SMT implementation of `IEquivalenceVerifier` for pure 32-bit register expressions.
2. Generate a machine-readable instruction catalogue from independently verified ISA data.
3. Parse normalized LLVM MIR windows from the firmware corpus.
4. Expand candidate search to sequences of length 2–4 with canonical temporary-register renaming.
5. Emit reproducible proof records, generated C++ matchers, and MIR regression tests.
6. Add memory, control flow, and CPU-feature modeling only after scalar integer rules are reliable.

See [the architecture document](docs/architecture.md) for the trust boundary and integration design.

## Safety boundary

Do not initially synthesize across MMIO, volatile accesses, inline assembly, system registers, exception entry/return, context switching, or `iret`.