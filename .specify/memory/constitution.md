<!--
SYNC IMPACT REPORT
==================
Version change: TEMPLATE (unversioned) → 1.0.0
Rationale: Initial ratification. All template placeholders replaced with concrete,
project-specific principles derived from stakeholder input. MAJOR bump (0 → 1.0.0)
because this establishes the first governing version of the constitution.

Modified principles (placeholder → defined):
- [PRINCIPLE_1_NAME] → I. Code Quality & Clean Code
- [PRINCIPLE_2_NAME] → II. Comprehensive Documentation
- [PRINCIPLE_3_NAME] → III. Performance by Design
- [PRINCIPLE_4_NAME] → IV. Test Coverage & Discipline (NON-NEGOTIABLE)
- [PRINCIPLE_5_NAME] → V. Separation of Concerns & Dependency Injection
Added principles (beyond the original 5-slot template):
- VI. Cross-Platform by Default
- VII. Clarification Over Assumption

Added sections:
- Technology & Testing Standards (was [SECTION_2_NAME])
- Development Workflow & Quality Gates (was [SECTION_3_NAME])

Removed sections: None.

Templates requiring updates:
- ✅ .specify/templates/plan-template.md (Constitution Check gates populated)
- ✅ .specify/templates/tasks-template.md (testing made mandatory per Principle IV)
- ✅ .specify/templates/spec-template.md (reviewed — no mandatory changes required)
- ✅ .specify/templates/checklist-template.md (reviewed — no changes required)

Follow-up TODOs: None. Ratification date set to initial adoption date (today).
-->

# OpenSharp Constitution

## Core Principles

### I. Code Quality & Clean Code

Code MUST be readable, maintainable, and self-explanatory through naming and structure
rather than commentary. Inline comments MUST NOT be added unless they are properly
justifiable — that is, they explain *why* something non-obvious is done, not *what* the
code does. Dead code, commented-out blocks, and speculative abstractions (YAGNI) MUST NOT
be committed. Code MUST pass the project's linting/formatting gates before merge.

**Rationale**: Self-documenting code reduces maintenance burden and prevents comment rot.
Justified comments preserve genuine context; gratuitous comments obscure intent.

### II. Comprehensive Documentation

All public members — public classes, methods, properties, interfaces, and enums — MUST
carry XML documentation comments (`///`). API documentation MUST be generated from these
XML comments as part of the build/release process. Non-public members are exempt unless
their behavior is non-obvious.

**Rationale**: XML doc comments provide IDE IntelliSense, enforce intentional public-API
design, and serve as the single source of truth for generated reference documentation.

### III. Performance by Design

Performance MUST be a first-class consideration, not an afterthought. Hot paths MUST avoid
unnecessary allocations, redundant work, and blocking I/O. Asynchronous APIs MUST be used
for I/O-bound operations. Performance-sensitive features MUST define measurable performance
goals in their plan, and regressions against those goals MUST block release.

**Rationale**: Designing for performance up front is far cheaper than retrofitting it, and
explicit goals make performance verifiable rather than aspirational.

### IV. Test Coverage & Discipline (NON-NEGOTIABLE)

Automated tests MUST accompany all production code. Line/branch coverage MUST be at least
80% across the solution, measured by the CI coverage gate; merges that drop coverage below
80% MUST be blocked. Unit tests MUST isolate the unit under test, mocking collaborators
with Moq. System/acceptance tests SHOULD be written wherever feasible, using Reqnroll for
behavior-driven scenarios. Tests MUST be deterministic and independent of execution order.

**Rationale**: A measurable coverage floor and consistent tooling (Moq, Reqnroll) make
quality verifiable and prevent regressions from reaching users.

### V. Separation of Concerns & Dependency Injection

Core business logic MUST live in its own dedicated class library, independent of UI,
hosting, or infrastructure concerns. Each component MUST have a single, well-defined
responsibility. Dependencies MUST be inverted and supplied via dependency injection;
concrete dependencies MUST NOT be constructed directly within consumers (no `new` on
collaborators that should be injected). This keeps the core independently testable and
mockable.

**Rationale**: Clean separation and DI enable the testing discipline of Principle IV,
allow the core to be reused across hosts, and keep change blast-radius small.

### VI. Cross-Platform by Default

All code MUST be cross-platform unless a specification explicitly states otherwise.
Platform-specific APIs, path separators, line endings, and OS assumptions MUST be avoided
or abstracted. Any intentional platform restriction MUST be documented in the feature
specification and justified.

**Rationale**: Cross-platform support maximizes reach and portability; making it the
default prevents accidental platform lock-in.

### VII. Clarification Over Assumption

When any requirement, constraint, or design decision is unclear, the team (and any agent
acting on the project) MUST ask the user for clarification rather than guessing.
Assumptions MUST NOT be silently baked into specs, plans, or code. Where a reasonable
default is unavoidable, it MUST be recorded explicitly (e.g., in an Assumptions section)
and surfaced for confirmation.

**Rationale**: Unverified assumptions are a leading cause of rework; explicit clarification
keeps delivery aligned with actual intent.

## Technology & Testing Standards

- **Platform**: .NET / C#, targeting cross-platform runtimes (see Principle VI).
- **Unit testing**: Tests with Moq for all mocking of collaborators. Coverage gate at 80%.
- **System/acceptance testing**: Reqnroll for BDD-style system tests where feasible.
- **Documentation**: API reference generated from XML doc comments during the build.
- **Architecture**: Core logic isolated in a class library; dependency injection used
  throughout for composition and testability.
- **Performance**: Performance-sensitive work declares measurable goals and is verified
  against them.

## Development Workflow & Quality Gates

- Every change MUST pass, before merge: build, lint/format checks, the full test suite,
  the 80% coverage gate, and successful generation of XML-based documentation.
- Public-API additions or changes MUST include XML doc comments in the same change.
- New features MUST add or update unit tests; system tests SHOULD be added where feasible.
- Pull requests MUST verify compliance with every applicable constitutional principle.
- Any unclear requirement MUST be resolved via clarification before implementation
  proceeds (Principle VII).

## Governance

This constitution supersedes all other development practices for the OpenSharp project.

- **Amendments**: Proposed in writing, reviewed and approved by project maintainers, and
  accompanied by a migration/impact note when they change existing practice.
- **Versioning policy**: Semantic versioning of this document — MAJOR for
  backward-incompatible governance or principle removals/redefinitions; MINOR for new
  principles or materially expanded guidance; PATCH for clarifications and non-semantic
  refinements.
- **Compliance review**: All PRs and reviews MUST verify compliance with these principles.
  Deviations MUST be justified explicitly (e.g., in a plan's Complexity Tracking section)
  or rejected. Unjustified complexity MUST be removed.
- **Runtime guidance**: Agent and contributor runtime guidance lives in `CLAUDE.md` and the
  `.specify/` templates; those artifacts MUST stay consistent with this constitution.

**Version**: 1.0.0 | **Ratified**: 2026-06-18 | **Last Amended**: 2026-06-18
