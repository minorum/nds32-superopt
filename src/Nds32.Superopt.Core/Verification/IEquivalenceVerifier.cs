using Nds32.Superopt.Isa.Architecture;

namespace Nds32.Superopt.Core.Verification;

public interface IEquivalenceVerifier
{
    VerificationResult Verify(
        InstructionSequence source,
        InstructionSequence candidate);
}
