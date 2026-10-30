# Bumpers

`BumperManager` is wired to `GameManager`, `RoomManager`, `DifficultyManager` and the Bumper prefab in Generation and Base. The reusable GameManager prefab also contains the manager. GameManager's Inspector includes a manual spawn button during Play Mode.

Spawning begins after 30 seconds in the player's current active room. The interval starts at 20 seconds and decreases by 10% per difficulty level, down to 5 seconds. Each observed automatic wave increases a run-wide wave counter. A room’s target population is floor(room area × elapsed waves / 150), capped at six managed bumpers. This preserves fractional area budgets across waves. A room contains at most 6 managed bumpers. Positions avoid walls, existing colliders and a 3-unit radius around the player. Large clock jumps still register a single automatic wave; room updates catch up to the waves observed during the run.

Spawned bumpers move at 2.5 units per second in a random horizontal direction. They reflect off the room limits, physical walls, the player and other bumpers. The original BrainBumper trigger continues to push the car. The moving chance can be reduced from 1 to leave some spawned bumpers stationary. Existing placed bumpers remain stationary until explicitly initialized by BumperManager.

Player contact is resolved through the collider's attached Rigidbody. The car's untagged front and rear collision shapes are accepted when that same body owns an enabled collider with a configured damageable tag. This matches BumperMotion's compound-body detection: a moving bumper can no longer turn away from those shapes without pushing the stationary player. The impulse points away from the bumper toward the body's centre of mass. The bumper's AttackHitbox uses hit-once-until-exit registration, so several car colliders produce one impulse and one OnBump event per contact; leaving and touching again rearms the action. Other rigidbodies, disabled action components and kinematic targets remain ineligible.

Before the 120-second overtime threshold, motion follows straight lines between rebounds. In overtime, guidance turns toward the player's current position at 5 degrees per second initially, increasing to a cap of 50 degrees per second over 12 additional difficulty steps. Bumper difficulty continues progressing once per DifficultySettings.SecondsPerLevel after overtime, even though normal trap generation has stopped. With the current 10-second period, guidance reaches its cap at 4:00. A 0.15-second delay after each rebound prevents guidance immediately cancelling the rebound.

Spawn delay, cadence, area budget, room cap, movement speed, moving chance, player clearance and guidance progression are configurable on BumperManager. BumperMotion exposes collision skin and the guidance delay after rebounds. It keeps a fixed height and uses kinematic Rigidbody movement with non-allocating casts. Room deactivation suspends motion naturally. Run end or manager disable stops managed motion; regeneration or clock rewind removes owned bumpers and resets spawning state.


## Prefab hierarchy and rotating sides

The prefab follows Corridor's Render / Physic / Action organization:

```text
Bumper
  Render
    Modèle
      BumperFrame
      Bumpers_sides
      BumperCap
    Feedback
      Rotation
      BounceEffect
  Physic
    Collision
  Action
    Bump
```

The model and collision volume are twice their original dimensions. The model's vertical offset preserves its original floor contact, so existing scene placements and BumperManager's ground placement continue to work. The root Rigidbody and BumperMotion keep their existing references and movement behavior. BrainBumper lives under Action/Bump; its hitbox remains under Physic/Collision.

BumperRotationFeedback is attached to Render/Feedback/Rotation and explicitly references Modèle/Bumpers_sides. It rotates only these sides around their local Y axis at 90 degrees per second by default. Degrees Per Second is editable, including zero to stop and negative values to reverse direction. Rotation uses gameplay time and stops while paused.


BumperBounceFeedback lives on Render/Feedback/BounceEffect and subscribes to BrainBumper.OnBump on Action/Bump. The collision hitbox activates the action, which applies its usual impulse to the player and emits OnBump. Feedback does not detect physics contacts itself. Only accepted player contacts with a Rigidbody emit this event.

The feedback briefly enlarges Bumpers_sides and BumperCap to 115% of their original local scales, expanding over 0.06 seconds and returning over 0.14 seconds. Scale Multiplier, Expand Duration and Return Duration are editable. Repeated bumps restart from the current scale without accumulating size. Disabling the effect unsubscribes it and restores both parts. Rotation and bounce use separate child GameObjects under Feedback, and both respect gameplay pause.

BumperCap now uses the dedicated BumperCapRed material, cloned from its previous Pattern Object material with the project's red palette color. It keeps the patterned shading and dark outline.

The free-position query excludes only the owning room's registered support floor through RoomTireTracks.IsFloor. The enlarged bumper's clearance margin can overlap that floor without blocking placement. All other colliders remain blocking, and a saturated query rejects the candidate. The manual Inspector command reports whether a bumper was spawned or no free position was found. Regression checks exercise the actual Inspector command in Generation and Base, as well as automatic spawning with a floor collider.


## Room update catch-up

RoomManager emits RoomUpdated for the current room and its adjacent rooms whenever
UpdateRooms refreshes their content, including when their trap difficulty is already
current. BumperManager.CatchUpRoom fills only the gap between the room’s target and
the managed bumpers already present. Explicit GameManager room-difficulty requests
use the same path. Repeating an update does not duplicate bumpers or reset their
movement, and rooms not belonging to the active neighborhood stay untouched.

The target uses the existing automatic wave cadence, initial 30-second delay,
150-cell density and six-bumper cap. Inactive rooms retain their catch-up target;
they are populated after activation, including in overtime when trap difficulty
has stopped increasing. Spawn positions still respect walls, colliders and player
clearance. A room without a free position retries its remaining deficit on a later
update. Clock rewind or dungeon regeneration clears both the wave budget and owned
bumpers. A large clock jump still follows the existing single-observed-wave policy;
catch-up concerns waves missed by a room, without adding a separate timer.
