# ADR-002: C++20

- **Status:** Accepted
- **Date:** 2026-09-21

## Context

Atlas requires control over memory, threading, rendering resources and performance.

## Decision

C++20 is the primary implementation language.

## Rationale

C++ provides native performance, explicit resource management, mature graphics libraries, concurrency primitives and direct low-level systems access.

C++20 provides modern language facilities while retaining broad compiler support.

## Consequences

Positive:
- Performance control
- Strong fit for renderer/engine work
- Modern language features

Negative:
- More implementation complexity
- Manual lifetime/ownership concerns
- Greater risk of undefined behaviour

Sanitizers, tests and explicit ownership rules are therefore required parts of development.
