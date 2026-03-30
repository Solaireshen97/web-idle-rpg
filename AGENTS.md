# Agent instructions for web-idle-rpg

This is a solo-maintained web idle RPG project.

Your job is to help build a game that can run, be tested, and be expanded gradually.
Do not optimize for hypothetical future complexity.

## Main rule

Only implement what is needed now.
If future extension is possible, leave a simple door instead of building a full framework.

## Required engineering bias

Bias toward:

- fewer files
- fewer layers
- fewer concepts
- fewer indirections
- smaller APIs
- clearer naming
- lower maintenance cost
- faster validation of gameplay loops

## Avoid unless explicitly requested

- abstract frameworks
- unused interfaces
- generalized manager systems
- complicated plugin-style architectures
- broad event infrastructure
- universal combat pipelines
- highly generic save/load architecture
- full future-proof equipment/profession/combat frameworks
- restoring complex runtime state when a simpler settlement model would work

## Preferred approach

When solving a task:

1. First ask: what is the smallest implementation that makes the feature work?
2. Implement that version.
3. If future expansion seems likely, leave a simple seam:
   - a replaceable function
   - a small isolated module
   - a clear data boundary
   - a documented TODO point
4. Stop there unless explicitly told to generalize.

## Do not do this

Do not turn:

- one feature into a platform,
- one use case into a generic engine,
- one branch into a configurable framework,
- one gameplay need into a universal system.

## For game systems

For combat, monster logic, equipment, professions, loot, progression, persistence, and offline gains:

- favor the current playable loop over idealized architecture,
- prefer concrete implementations over generic systems,
- prefer simpler save structures over runtime object graph persistence,
- prefer understandable code over technically elegant but hard-to-maintain abstractions.

## Required explanation style

For non-trivial tasks, explain briefly:

- what you implemented,
- what complexity you intentionally did not introduce,
- where the future extension point is.

## Final mindset

This project should grow through validated slices of gameplay.
Build a small working game first.
Only after the current slice proves useful should the architecture expand.
