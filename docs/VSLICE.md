# Moggy - Vertical Slice

## Purpose

Build one repeatable Score Attack level that proves the core pressure loop:

> Eat bugs, score quickly, raise chaos, attract enemies, roll an ability, and
> use that ability to regain control.

The slice validates whether this loop is readable and enjoyable before adding
more objectives, enemies, abilities, or progression.

## Level setup

- Objective: reach 1,200 points.
- Expected duration: 60 to 90 seconds.
- Layout: one generated layout using a fixed seed for repeatable testing.
- Bug population: 12 regular bugs, fixed and non-respawning.
- Enemy roster: one fixed type of straight chaser.
- Enemy population: four, fixed and respawning.
- Starting lives: three.
- Failure: losing all lives ends the run and restarts the level from the start.

These numbers are initial test values, not final balance targets.

## Scoring

- Eating a bug awards 100 points.
- Stomping an enemy awards 200 points.
- The score target is reachable by eating all 12 bugs.
- Enemy defeats allow a skilled player to finish without catching every bug.

This prevents an unlucky ability order from making the level impossible while
still rewarding offensive play.

## Player and life loss

The player's default state has no ability and uses a fixed normal speed. Contact
with an enemy in Normal costs one life. BigBoy stomps the enemy and remains
active. Microman absorbs the contact: the player returns to Normal without
losing a life, and the enemy remains active.

When Microman absorbs a contact, the player and enemy separate and the player
receives a brief collision-invincibility period. This prevents the same overlap
from immediately costing a life after the form changes.

After losing a life:

- The level state, score, bugs, enemies, chaos, and objective progress remain.
- Ability-counter progress remains.
- The player respawns at the starting position.
- The active ability ends.
- The player receives two seconds of invincibility.
- Enemies cannot be stomped during respawn invincibility.
- Active enemies clear pursuit and return to scattering before play resumes.

## Bugs

Bugs move around the level and flee when the player approaches. Their speed
increases as their population decreases. A fast bug must remain catchable by
flanking and interception even when it cannot be caught in a straight chase.

The slice must expose bug speed, flee distance, and maximum speed as tuning
values. The final bug must have a visible speed cue.

Each eaten bug:

- Awards points.
- Adds chaos.
- Advances the visible ability counter by one.

## Ability counter and rolls

The counter starts at zero. Reaching three bugs immediately rolls an ability.
Rolling consumes the full counter and resets it to zero. Twelve bugs provide up
to four rolls. Each roll independently has a 60% chance of BigBoy and a 40%
chance of Microman. Consecutive outcomes are unrestricted.

Bugs continue advancing the counter during an active ability. A new roll
immediately replaces the current effect and restarts the duration. The player
can chase a reroll during Microman or stop at two bugs to preserve BigBoy. If an
effect expires first, the player returns to normal without losing counter
progress.

At `2/3`, the counter displays a persistent visual warning and plays a short
audio cue. The warning communicates that the next bug will roll an ability and
replace the active effect.

## BigBoy

BigBoy makes the player approximately two cells wide and two cells tall for a
fixed duration. The player moves slower, rolls through the level, and stomps
enemies on contact instead of losing a life.

BigBoy must:

- Make enemy contact clearly safe.
- Award points and reduce chaos for each stomped enemy.
- Remain slow enough that positioning matters.
- End with an audiovisual warning before contact becomes dangerous again.

## Microman

Microman makes the player smaller and faster for the same fixed duration. Enemy
contact ends Microman instead of costing a life. Its smaller body has a reduced
eating radius, and eating a bug requires remaining in range for longer than
normal.

Microman must:

- Make escape and repositioning easier.
- Require more precise positioning to catch bugs.
- Show clear progress while the longer eating action is in progress.
- Remain controllable enough to avoid accidental deaths.
- Provide no direct way to defeat enemies.
- Absorb one enemy contact, then return the player safely to Normal.
- End with an audiovisual warning before normal speed returns.

The slice must verify that the reduced radius and longer eating time offset the
higher speed without making fast late-stage bugs impossible to catch.

## Chaos and attention

Chaos increases when the player eats bugs or rolls an ability. Consecutive
actions build it faster. After a period without scoring, chaos decreases over
time. Stomping an enemy immediately reduces it further, even though the stomp
also awards points.

The interface displays chaos as four enemy-attention levels. Each level provides
one chase slot, independently of the total enemy population. A change in
attention must be signalled before another enemy begins chasing.

With four enemies, attention level 4 allows the entire roster to chase.

The following values remain tunable without changing the slice design:

- Chaos gained per bug and ability roll.
- Consecutive-action multiplier and timeout.
- Inactivity delay and decay rate.
- Chaos removed by an enemy stomp.
- Thresholds for attention levels 1 through 4.
- Fixed enemy population and movement speed.
- Spawning warning duration and chase-handoff cooldown.

## Straight chaser

The slice enemy uses these states:

- Spawning: appears with a warning and cannot harm the player.
- Scattering: moves around the level without directly pursuing the player.
- Chasing: follows the player's recorded footsteps without predicting or
  flanking.
- Panicking: flees BigBoy and can be stomped on contact.
- Eaten: entered when stomped; awards points, reduces chaos, and leaves play
  until respawning.

Spawning finishes in scattering. A scattering enemy that sees the player pauses
with an alert cue. If the player remains in its field of view and the chase
budget has room, it begins chasing. If the budget is full, it can replace the
most distant chaser. The released chaser visibly shrugs and returns to
scattering. A short cooldown prevents repeated handoffs between nearby enemies.

Rising attention opens another chase slot. Decreasing attention closes one and
returns the most distant chaser to scattering. BigBoy suspends chase slots and
moves enemies to panicking. Losing a life clears all footstep targets and
returns every active enemy to scattering.

An eaten enemy returns through spawning after a delay. When its warning ends, it
scatters or begins its alert wind-up according to current attention and field of
view.

## Required feedback

The player must be able to read:

- Current score and target score.
- Remaining lives.
- Current attention level and an upcoming increase.
- Ability-counter progress from zero to three.
- Active ability and remaining duration.
- A warning that the next bug will replace the active ability.
- Respawn invincibility.
- A bug becoming faster as the population falls.
- Enemy transitions between scattering, chasing, and panicking.
- The alert wind-up before a scattering enemy begins chasing.
- Chase-slot handoffs between nearby and distant enemies.

## Out of scope

- Collect 'Em All and objective items.
- Boss levels.
- Additional enemy types or ability pairs.
- Multiple procedural combinations.
- Multi-level difficulty progression.
- Final scoring, high-score persistence, and extra-life rules.
- Final art, audio, and content volume.

## Exit criteria

The slice is complete when playtesting demonstrates that:

- A new player understands the objective and major state changes without an
  explanation.
- A run usually lasts between 60 and 90 seconds.
- The score target remains reachable for every possible ability sequence.
- Across repeated runs, both abilities produce different routing and
  enemy-management decisions.
- Rerolling Microman and preserving BigBoy both create meaningful risks.
- Players recognize the `2/3` warning and deliberately choose whether to reroll.
- Players recognize and can react to an enemy's alert wind-up.
- Raising chaos creates visible pressure instead of an unexplained difficulty
  spike.
- Stomping enemies provides a noticeable recovery from high attention.
- The last bug is caught by prediction or flanking, not prolonged pursuit.
- Losing a life does not corrupt or reset surviving level state.
- No valid combination of deaths, rolls, or depleted bugs can softlock the
  level.
- Reaching the target ends the active ability, restores normal form, shows
  victory feedback, and opens the score screen.