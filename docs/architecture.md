# Architecture

The superoptimizer is an offline tool. Normal LLVM builds must consume generated, pre-verified rules rather than invoking an SMT solver.

```text
MIR or corpus window
        |
        v
independent NDS32 instruction model
        |
        +--> candidate enumeration
        |
        +--> concrete counterexample filtering
        |
        +--> formal bit-vector proof (planned)
        |
        v
proof record + generated LLVM matcher + MIR regression test
```

## Trust boundary

`ConcreteEquivalenceVerifier` is deliberately incomplete. It can reject bad candidates but cannot establish correctness. Its successful result is named `NoCounterexample` rather than `Equivalent`.

A rule may be emitted into the LLVM backend only when a complete verifier returns `ProvenEquivalent`.

## Architectural state

The initial model observes all 32 general-purpose registers. Candidate and source sequences must produce the same complete register file, preventing an apparently correct replacement from introducing an extra clobber.

The first milestone excludes memory, PSW flags, traps, control flow, system registers, inline assembly, MMIO, exception handling, and floating point. Those effects must be modeled before any sequence containing them is accepted.

## Independence

The semantic model must remain independent of LLVM TableGen patterns. Importing opcode names and encodings is acceptable; copying lowering semantics would risk proving a backend bug with the same bug.

## NativeAOT

The CLI is NativeAOT-compatible. Solver integrations should remain behind `IEquivalenceVerifier`; if a solver's managed bindings cannot be statically linked, a separate proof worker process may be used without affecting the NativeAOT corpus/search CLI.
