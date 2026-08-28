# Review guidelines

Review this repository as a public Unity package targeting Unity 6000.5.7f1.

Prioritize findings that can cause incorrect runtime behavior, broken ownership or cleanup, stale pooled state,
serialization failures, package-boundary violations, or missing regression coverage. Avoid low-value comments about
subjective style when the code follows the repository conventions.

## Required checks

- Verify every spawned VFX lease is released exactly once and its lifetime session is disposed exactly once.
- Verify stale `VFXHandle` values cannot affect a reused instance.
- Check Emitter, Provider, pool Scope, Scene unload, cancellation, and object-destruction ordering.
- Check ParticleSystem, Sub Emitter, TrailRenderer, and Animator reuse state.
- Treat prefab-root `VFXInstance` and GameObjectPooling root-only lifecycle-handler discovery as explicit contracts.
- Check runtime code does not depend on Editor assemblies.
- Check public API changes have a migration path and matching Korean and English documentation.
- Require relevant Unity Test Framework coverage for behavior changes. Do not treat `dotnet build` alone as proof
  that Unity runtime behavior is correct.
- Treat `.unity`, `.prefab`, `.asset`, `.meta`, and asmdef GUID consistency as correctness concerns.

## Finding quality

Report only actionable findings with a concrete failure scenario. Include the affected path and line, severity,
rationale, and a suggested direction. Do not ask for compatibility aliases for removed 0.x APIs; this package is
stabilizing before 1.x and intentionally allows breaking cleanup with documented migration.

## Review depth

Use no sub-agents for documentation-only or trivial changes. Use one focused sub-agent for a small change involving
lifetime, pooling, serialization, or cancellation. Use multiple focused sub-agents only for large changes spanning
independent runtime, asset, and test concerns. Sub-agents must remain read-only; the main reviewer verifies findings
and posts the final review.
