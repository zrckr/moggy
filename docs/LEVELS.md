# Moggy - Level Generation

## Purpose

Levels are randomly generated mazes that support routing, flanking, enemy
handoffs, and short arcade objectives. Generation must produce playable layouts,
not merely connected ones.

This document defines the level contract. Specific content and tuning values for
the first playable level remain in [VSLICE.md](VSLICE.md).

## Generation pipeline

1. Generate maze walls and outer openings from a reproducible seed.
2. Pair outer openings as wrap exits.
3. Select the player spawn and enemy spawners.
4. Place the enemy roster, bugs, and mode-specific content.
5. Validate reachability, separation, escape routes, and content distribution.
6. Reject and regenerate any level that fails validation.

Topology generation and content placement are separate stages. A connected maze
can still be unsuitable because of unsafe spawns or poor content distribution.

## Walls and topology

The maze consists of walkable corridors separated by walls. Every walkable area
must be connected. The topology should contain enough intersections and loops
to support flanking and alternate escape routes.

Long hallways and dead ends are permitted in moderation. They must not dominate
the maze or make fast bugs impossible to intercept. All gameplay-critical
locations need more than one practical approach unless isolation is intentional.

All forms use the same navigable maze. BigBoy appears approximately two cells
wide and two cells tall but must remain able to traverse generated corridors.
The collision representation used to satisfy this is an implementation detail.

## Wrap exits

Outer openings are paired as wrap exits. Entering one opening moves an actor to
its paired opening while preserving movement direction where possible.

Players, bugs, and enemies use the same exit pairs. Exits are routing tools, not
player-only escape points. Every generated level must have an even number of
usable outer openings.

Exit placement and pairing must avoid immediate collisions at the destination.
An actor cannot emerge inside a wall, spawn warning, or another blocking actor.

## Player spawn

Each level has one player spawn, reused after losing a life. It must:

- Be separated from enemy spawners by sufficient path distance.
- Have at least two practical escape routes.
- Contain no bugs or objective items in its safety area.
- Keep exit destinations and enemy spawn paths outside that safety area.

After life loss, the player returns here with temporary invincibility. Active
enemies clear pursuit and return to scattering before play resumes.

## Enemy spawners and roster

Enemy spawners are fixed, visible level locations. They must:

- Be separated from the player spawn by path distance.
- Avoid dead ends and narrow escape routes.
- Be distributed across different maze regions.
- Telegraph an enemy before it becomes harmful.

The level owns a fixed enemy roster independent of attention. Attention controls
chase slots, not total enemy count. Mixed rosters may contain multiple enemy
types, each identified visually before the player chooses which one to approach.

Eaten enemies return through a spawner after a delay, preserving the fixed
roster. They use the normal spawning warning before becoming harmful. The number
of spawners remains an open design decision.

## Bugs

Score Attack and Collect 'Em All have finite bug populations divisible by three.
Bugs are placed after spawns and exits are known. Placement must:

- Distribute bugs across multiple maze regions.
- Avoid player and enemy spawn safety areas.
- Provide enough local space for fleeing and interception.
- Avoid large clusters that grant an effortless immediate reroll.
- Use intersections to create flanking opportunities.

All bugs must remain reachable through normal movement and every wrap exit they
can enter must have a valid destination.

## Objective items

Collect 'Em All adds stationary objective items. They are visible from the start
and distributed across distinct maze regions. They cannot appear in spawn safety
areas or become unreachable under an ability.

Objective items generate less chaos than bugs and participate in consecutive-
action scaling. They do not advance the ability counter.

## Mode-specific content

- Score Attack places regular bugs as the primary score source.
- Collect 'Em All places objective items and optional regular bugs.
- Defeat the Boss places the boss and replenishes eaten bugs until the boss is
  defeated, preserving access to future ability rolls.

The generator must validate each mode after placing its content. A topologically
valid maze is rejected if its objective cannot be completed.

## Keys and doors

Keys and doors are outside the initial scope. They alter reachability and add
mandatory ordering to an otherwise open arcade maze.

If introduced later, they form a separate level modifier. Doors open
permanently, keys cannot appear behind their own doors, and every lock state
must preserve a route to all required objectives.

## Validation contract

A generated level is accepted only when:

- Every required location is reachable from the player spawn.
- Player and enemy spawn safety areas do not overlap.
- The player spawn has at least two escape routes.
- Exit openings form complete, safe pairs.
- Bugs and objective items satisfy their distribution rules.
- Fast bugs remain interceptable in the generated topology.
- The selected objective remains completable with the placed content.
- The seed reproduces the same topology, pairing, and placements.

## Open design decisions

- How many enemy spawners does a level contain?
- How does enemy population progress between levels?