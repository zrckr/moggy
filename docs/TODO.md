# TODO

Implement the playable Score Attack slice defined in [VSLICE.md](VSLICE.md).
Keep Collect 'Em All, bosses, progression, and final content out of this work.

## Existing foundation

- [x] Generate a reproducible maze from the configured seed.
- [x] Move the player through the maze on a cell grid.
- [x] Spawn four enemies and reserve their occupied cells.
- [x] Record player footsteps and navigate enemies through the maze.
- [x] Render sprites, the level, the HUD layer, and debug tools.

## 1. Slice state and tuning

- [x] Add a single source of tuning values for the score target, starting lives,
  bug and enemy scores, ability odds and duration, respawn timing, chaos, and
  attention thresholds.
- [x] Add run and level state for score, lives, objective progress, game-over,
  victory, and the score screen.
- [x] Replace startup-only entity creation with level setup and restart paths.
- [x] Keep the current fixed seed while the slice is under test.

## 2. Level content

- [ ] Choose and store a safe player spawn instead of using the maze centre.
- [ ] Choose visible enemy spawners with path-distance separation from the
  player spawn.
- [x] Pair the generated boundary openings as wrap exits for all moving actors.
- [ ] Place 12 bugs across separate maze regions outside spawn safety areas.
- [ ] Reject a placement when required content is unreachable or too clustered.

## 3. Bugs and scoring

- [ ] Add bug entities with normal, fleeing, and eaten behaviour.
- [ ] Make nearby bugs flee while preserving routes where the player can flank
  or intercept them.
- [ ] Increase bug speed as the population falls, with a configured upper limit.
- [ ] Detect player contact continuously while either actor moves between cells.
- [ ] Award 100 points for a bug and remove it permanently from the level.
- [ ] Show a readable speed cue on the final fast bugs.
- [ ] End the level as soon as the score reaches 1,200.

## 4. Ability rolls and player forms

- [ ] Add a visible bug counter that rolls at three and resets to zero.
- [ ] Roll BigBoy at 60% and Microman at 40% without streak protection.
- [ ] Allow bugs to advance the counter while an ability is active.
- [ ] Replace the active ability and restart its duration on a new roll.
- [ ] Add the persistent visual and one-shot audio warning at `2/3`.
- [ ] Represent Normal, BigBoy, and Microman as explicit player forms.
- [ ] Restore Normal when an ability expires or the objective completes.

## 5. BigBoy and Microman

- [ ] Give BigBoy its larger presentation, slower movement, and rolling
  animation while keeping every corridor navigable.
- [ ] Make BigBoy stomp enemies on contact without ending the ability.
- [ ] Award 200 points and reduce chaos for every stomp.
- [ ] Give Microman its smaller presentation and faster movement.
- [ ] Reduce Microman's eating radius and require a longer, visible eating
  action before a bug is consumed.
- [ ] On enemy contact, consume Microman instead of a life, separate both
  actors, and grant brief collision invincibility.
- [ ] Warn before either ability expires and contact rules change.

## 6. Lives and collision

- [ ] Define shared overlap handling for the player, enemies, bugs, and exits.
- [ ] Make enemy contact in Normal consume one life.
- [ ] Respawn the player at the stored spawn without resetting score, bugs,
  chaos, enemy roster, or ability-counter progress.
- [ ] End the active ability and return all enemies to scattering on life loss.
- [ ] Grant two seconds of respawn invincibility and prevent stomps during it.
- [ ] End the run at zero lives and restart the slice from its initial state.

## 7. Chaos and attention

- [ ] Add chaos gains for bugs and ability rolls.
- [ ] Increase chaos gains for consecutive scoring actions within a configured
  time window.
- [ ] Start chaos decay after the configured period without scoring.
- [ ] Remove chaos immediately when BigBoy stomps an enemy.
- [ ] Convert chaos into four attention levels with advance warning near the
  next threshold.
- [ ] Expose every chaos value and threshold for playtest tuning.

## 8. Enemy state machine

- [ ] Replace unconditional pursuit with Spawning, Scattering, Chasing,
  Panicking, and Eaten states.
- [ ] Make scattering enemies wander without directly following the player.
- [ ] Add field-of-view detection, an alert pause, and a readable chase cue.
- [ ] Limit simultaneous chasers to the current attention level.
- [ ] When all slots are occupied, hand pursuit to the nearby candidate and
  return the most distant chaser to scattering.
- [ ] Add a handoff cooldown and a visible shrug cue to prevent noisy churn.
- [ ] Keep the straight chaser's existing footstep pursuit after activation.
- [ ] Send active enemies into Panicking while BigBoy is active.
- [ ] Move stomped enemies through Eaten and delayed Spawning before returning
  them to the fixed roster.
- [ ] Telegraph spawning and keep enemies harmless until spawning completes.

## 9. HUD and game flow

- [ ] Replace the static HUD score with current score and the 1,200 target.
- [ ] Display lives, attention, upcoming attention, ability counter, active
  ability, and remaining duration.
- [ ] Show respawn invincibility and all required enemy-state transitions.
- [ ] Add minimal audio cues for rolls, warnings, stomps, life loss, and
  victory.
- [ ] On victory, end the active ability, restore Normal, show feedback, and
  open a score screen.
- [ ] Add a game-over screen and a clear restart input.

## 10. Playtest and tuning

- [ ] Verify that every ability sequence can reach the target without a
  softlock.
- [ ] Verify that Microman rerolls and BigBoy preservation create distinct
  routing decisions.
- [ ] Verify that attention changes are understood before chase pressure rises.
- [ ] Verify that life loss preserves all surviving level state.
- [ ] Verify that the final bug is caught by interception rather than prolonged
  pursuit.
- [ ] Tune toward a typical run length of 60 to 90 seconds.
- [ ] Record unresolved balance findings in `VSLICE.md`; do not expand scope to
  solve them with additional modes or content.