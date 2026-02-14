<!--
Sync Impact Report
- Version change: N/A -> 0.1.0
- Modified principles: N/A
- Added sections: Core Principles, Additional Constraints, Development Workflow, Governance
- Removed sections: N/A
- Templates requiring updates:
  - .specify/templates/plan-template.md ✅ updated
  - .specify/templates/spec-template.md ✅ updated
  - .specify/templates/tasks-template.md ✅ updated
  - .codex/prompts/speckit.constitution.md ✅ updated
- Follow-up TODOs: None
-->
# Meetloop Constitution

## Core Principles

### I. Code Quality Is Non-Negotiable

All production code MUST be readable, well-structured, and self-explanatory.
Complexity MUST be justified in the plan and minimized in the implementation.
No dead code, commented-out code, or unresolved TODOs are allowed in mainline.
Rationale: Clean code reduces defects and makes change safer and faster.

### II. Tests Are a Required Deliverable

Every new behavior MUST include automated tests at the appropriate level
(unit, integration, or end-to-end), and bug fixes MUST include a regression
test. Tests MUST be deterministic and run in CI. Merging is blocked on failing
tests.
Rationale: Tests are the primary safety net for change and refactoring.

### III. User Experience Comes First

User journeys and acceptance scenarios MUST be defined and validated for
user-facing changes. Error messages MUST be actionable and humane. Performance
regressions that impact the primary flow are unacceptable without explicit
approval and mitigation.
Rationale: The product succeeds only if users can accomplish their goals.
Every feature is specified as independently deliverable user stories with
acceptance scenarios. Each story must be testable and usable as a standalone
MVP slice.

### IV. Simplicity Over Cleverness

Prefer the simplest solution that meets requirements. Abstractions MUST be
introduced only when they reduce total complexity or prevent duplication across
at least two real use cases. Premature optimization is disallowed unless the
performance goal is explicit and measured.
Rationale: Simple systems are easier to evolve and less error-prone.

### V. Modularity Over Monolithic Design

Features MUST be built as cohesive modules with clear boundaries, minimal
dependencies, and explicit interfaces. Shared modules MUST have a single,
focused responsibility and be independently testable. Circular dependencies are
prohibited.
Rationale: Modularity supports parallel work and reduces blast radius of change.

### VI. Contract-First Interfaces

Public APIs, data contracts, and cross-module interfaces are defined before
implementation. Contract changes require versioning and migration notes.

### VII. Observability & Reliability

Key flows must include structured logging, error handling, and operational
signals. Reliability requirements are documented for user-facing workflows.

## Additional Constraints

- Social features must include privacy defaults and safety controls.
- Data retention rules must be documented for any stored user data.
- No feature may proceed to implementation without testable acceptance
  scenarios.

## Quality Gates

- Every PR MUST include passing automated tests and static analysis/linting.
- User-facing changes MUST include acceptance scenarios and a UX review in the
  spec or plan.
- Any complexity exception MUST be documented in the plan with alternatives
  considered and rejected.
- Modular boundaries MUST be documented when adding new modules or services.

## Development Workflow

- Specifications must include user stories, acceptance scenarios, and success
  criteria before planning.
- Plans must record constitution gates and justify any violations.
- Tasks must be organized by user story and support independent delivery.
- Work MUST start from a spec and plan that reference this constitution.
- PRs MUST be small, reviewable, and tied to a single user story or change.
- Code review MUST explicitly verify compliance with the Core Principles.
- Refactors are encouraged when they simplify or modularize existing code and
  do not regress tests or UX.

## Governance

- This constitution supersedes all other guidance.
- All PRs and reviews must verify compliance with the Core Principles.
- Amendments require a documented rationale, version bump, and migration plan
  where applicable.

**Version**: 0.1.0 | **Ratified**: 2026-02-13 | **Last Amended**: 2026-02-13
