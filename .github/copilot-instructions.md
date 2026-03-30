# Copilot instructions for this repository

This repository is a solo-developed web idle RPG project.
Optimize for simplicity, maintainability, and incremental delivery.

## Primary development philosophy

- Build the smallest working playable version first.
- Only implement what is explicitly needed right now.
- For future requirements, leave a simple extension point only.
- Do not build full frameworks for hypothetical future needs.
- Prefer "working game loop first" over "complete architecture first".

## Core principles

- Prioritize: runnable, understandable, debuggable, modifiable.
- Prefer direct implementations over speculative abstractions.
- If something may expand later, leave a clear seam/entry point instead of a full system.
- Refactor only when repeated real requirements prove the need.
- Avoid solving endgame architectural problems too early.

## By default, avoid

- premature abstraction
- generic frameworks without real current usage
- unnecessary interfaces or base classes
- deep inheritance hierarchies
- manager-of-managers patterns
- event buses unless explicitly required
- complex state machines unless explicitly required
- multi-layer indirection for a single current use case
- building systems for features that do not exist yet

## By default, prefer

- simple modules with clear responsibilities
- low cognitive load
- minimal moving parts
- local clarity over global theoretical elegance
- straightforward data flow
- simple persistence models
- easy replacement later over premature extensibility now

## Game-specific guidance

For gameplay systems such as combat, progression, equipment, professions, monsters, rewards, save/load, and offline progression:

- Implement only the current version needed for the playable loop.
- Do not design universal systems for all future gameplay possibilities.
- If future expansion seems likely, leave a door, not a framework.
- Separate "current playable need" from "future imagined possibility".
- Prefer simple result-based/offline settlement approaches over complex runtime restoration unless explicitly required.

## Required behavior when generating code

When completing a task:

1. Implement the minimum viable solution first.
2. If future expansion is likely, leave only a simple extension seam.
3. Do not proactively introduce architectural upgrades unless explicitly requested.
4. Explain briefly:
   - what was implemented now,
   - what was intentionally deferred,
   - where future expansion can happen.

## Decision rule

Before introducing abstraction, ask:

- Is this required by the current task?
- Is there more than one real use case right now?
- Does this reduce complexity today, not just in theory?
- Would a solo developer find this easier to maintain?

If the answer is "no" or unclear, do not introduce the abstraction.

## Important mindset

The goal is not to design the final system in advance.
The goal is to make the game work first, confirm the loop is fun and stable, and then evolve the code gradually based on real needs.
