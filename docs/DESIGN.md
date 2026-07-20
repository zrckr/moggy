# Moggy - Game Design

## Game promise

Moggy is a short-session arcade game about a Pac-Man-like creature eating bugs
while managing escalating enemy attention. Fast, consecutive actions increase
chaos: they improve scoring and progress but cause more enemies to pursue the
player.

The player should constantly weigh immediate progress against the danger
created by acting aggressively.

## Core loop

1. Enter a randomly configured level with a clear objective.
2. Move through the level, eat bugs, and pursue the selected objective.
3. Build chaos by chaining actions such as eating bugs and rolling abilities.
4. Read the visible enemy-attention level, from 1 to 6, and adapt as pressure
   rises.
5. Fill the bug counter to roll a temporary positive or negative ability.
6. Use positive abilities to defeat enemies; use negative abilities to evade or
   manipulate them while continuing the objective.
7. Complete the objective, receive a level score, and continue to a harder
   level.

Enemies cannot normally be eaten or defeated. The player must avoid and
manipulate them unless a positive ability provides a direct counter.
Enemy contact in the normal form costs one life. An active ability defines its
own contact result: an offensive form can defeat the enemy, while a
non-offensive form ends instead of costing a life. Contact with the boss or a
boss attack costs one life unless an ability explicitly counters it.

## Level generation

Each level selects one compatible entry from all three categories:

- Objective: Score Attack, Collect 'Em All, or Defeat the Boss.
- Ability pair: one positive ability and its negative opposite. Only one effect
  is active at a time, for a fixed duration.
- Enemy roster: a curated mix of enemy types present in the level.

Combinations are selected from a manually curated compatibility list. Purely
independent random selection is out of scope because it can create trivial or
impossible levels.

Levels use randomly generated connected mazes. See [LEVELS.md](LEVELS.md) for
topology, placement, exits, and validation rules.

### Objectives

- Score Attack: reach a target score.
- Collect 'Em All: collect a fixed set of stationary objective items.
- Defeat the Boss: defeat a boss that is present from the start.

Objectives progress from simple to complex across a run. Target values, enemy
pressure, and ability timing provide the difficulty progression.

Objective items are visually distinct from bugs and visible from the start so
the player can plan a route. They remain collectible under either ability
effect. Regular bugs are optional in this mode and provide score, chaos, and
ability-counter progress.

Collecting an objective item generates less chaos than eating a regular bug and
participates in consecutive-action scaling. It does not advance the ability
counter. Required progress therefore attracts some attention, while optional
bugs remain the stronger risk taken for score and rerolls.

## Bugs

Bugs move around the level and try to escape when the player approaches. As
their population decreases, the remaining bugs move faster, increasing the
difficulty of finishing a sweep.

The speed increase needs a readable cue and an upper limit. Otherwise, the
final bug can turn level completion into an extended chase rather than a
routing decision.

Score Attack and Collect 'Em All use fixed, non-respawning populations of
regular bugs. Population size is divisible by three so every bug contributes to
a complete ability roll. Eating a bug awards score, generates chaos, and
increments a visible ability counter. Reaching three bugs immediately rolls an
ability. Rolling consumes the full counter and resets it to zero. There is no
combo timeout; chaos separately rewards consecutive actions. Bugs remain edible
and advance the counter in every player state.

Defeat the Boss replenishes eaten bugs until the boss is defeated. This prevents
an unrestricted sequence of negative rolls from exhausting every opportunity
to obtain a positive ability.

In Score Attack, bugs are the primary score source. In Collect 'Em All, they are
an optional risk taken for score or abilities. In Defeat the Boss, they provide
the ability rolls required to create attack opportunities.

## Chaos and attention

Chaos is an internal score produced by player actions. Enemy attention is its
visible six-level representation. Attention controls the number of simultaneous
chasers, independently of the total enemy population.

Chaos starts decreasing after the player spends a period of time without
scoring. Defeating an enemy with a positive ability immediately decreases chaos
further and awards points in Score Attack. Enemies are immediate threats that
the player should actively resolve, not permanent additions to the level.

Chaos must be predictable enough to support deliberate risk. The interface
should show the current attention level and clearly signal an upcoming
increase. Sustained action should be rewarding, but uncontrolled positive
feedback, where more actions cause more enemies and then more actions, must not
make recovery impossible. Chaos is the cost of rapid scoring and does not need
an additional reward merely for remaining high.

The exact action values, thresholds, decay rules, detection ranges, and handoff
timing remain tuning parameters.

## Enemies

Enemy types differ by their chasing behaviour. Their appearance must communicate
that difference before the player chooses which enemy to approach. All types
share these states:

- Spawning: enters with a warning and cannot harm the player yet.
- Scattering: moves around the level without directly pursuing the player.
- Chasing: uses its type-specific pursuit behaviour and harms on contact.
- Panicking: flees the player while a positive ability can defeat it.
- Eaten: awards any applicable score, reduces chaos, and leaves play until it
  respawns.

Attention is an approximate chase budget, capped by the available enemy count.
A scattering enemy that sees the player pauses with an alert cue. If the player
remains in its field of view and the chase budget has room, it begins chasing.
If the budget is full, it can replace the most distant chaser. The released
enemy visibly shrugs and returns to scattering. A short handoff cooldown
prevents rapid switching between nearby enemies.

Rising attention opens another chase slot. Decreasing attention closes one and
returns the most distant chaser to scattering. A positive offensive ability
suspends chase slots and makes enemies panic; when it expires, chase slots are
assigned again.

Losing a life clears enemy pursuit and returns every active enemy to scattering
before play resumes. Each enemy type defines only its chasing behaviour; the
other states and transitions are shared. Mixed rosters must avoid a universally
best enemy choice; each chasing behaviour should be preferable in different
positions or objectives.

An eaten enemy returns through the spawning state after a delay. When its
warning ends, it scatters or begins its alert wind-up according to current
attention and field of view.

## Abilities

Filling the bug counter rolls one temporary effect from the level's ability
pair. Positive abilities provide a direct way to defeat enemies. Negative
abilities hinder direct combat but must leave an indirect route to progress
through evasion, positioning, enemy manipulation, or objective play.

A negative ability should create a tactical complication, not suspend player
agency. It must not prevent collecting objective items, eating bugs, or pursuing
another roll. Negative effects may support progress indirectly by confusing
enemy AI or manipulating level hazards.

The player's default state has no ability. The visible counter lets the player
choose when to eat the third bug and accept a roll. Each roll is independent,
with a 60% chance of the positive effect and a 40% chance of the negative
effect. Consecutive outcomes are not restricted.

A roll immediately replaces the active effect and restarts the duration. The
player can pursue a reroll to escape an unwanted effect or stop at two bugs to
preserve a useful one. If an effect expires before another roll, the player
returns to the normal state without resetting counter progress.

At two bugs, the interface must provide a persistent visual warning and a short
audio cue that the next bug will trigger a roll and replace any active effect.

## Bosses

A boss has discrete health and is present from the start of its level. Contact
during a positive offensive ability removes one health point. After taking
damage, the boss becomes temporarily invulnerable and separates from the player.
The player can damage it again during the same ability after invulnerability
ends.

Boss levels use the same unrestricted 60/40 rolls as other objectives. Eaten
bugs continue to respawn until the boss is defeated, so another positive roll
always remains possible. Exact health, ability duration, invulnerability, and
bug respawn timing are balance values.

## Run structure and scoring

The game is arcade-first. Each completed level awards a score based on objective
completion and performance. Lives and total score persist between levels. When
the player has no lives remaining, the run ends. Long-term upgrades and
persistent character progression are outside the initial scope.

Losing a life does not reset the level. The player respawns with temporary
invincibility while bugs, score, chaos, enemies, and objective progress remain.
Ability-counter progress also remains. Losing the final life ends the run and
restarts the game from the first level.

Completing an objective immediately ends any active ability and restores the
normal form. The game then shows victory feedback and opens the level score
screen.

## Vertical slice

The first vertical slice validates the core loop using one Score Attack level.
See [VSLICE.md](VSLICE.md) for its scope, initial values, and exit criteria.