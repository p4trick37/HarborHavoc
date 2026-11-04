# GDD316 local battle

Open BattleArena.unity, Play, and focus the Game view. Controllers press Start to join, then A starts the countdown. With no controllers, two keyboard boats appear; Enter starts the match. Last boat afloat wins. Esc resets the lobby.

| Player | Move | Aim | Fire | Boost | Activate | Mine |
| --- | --- | --- | --- | --- | --- | --- |
| P1 keyboard | WASD | Mouse | Click / Space | Left Shift | E | F |
| P2 keyboard | Arrows | Last arrow direction | Right Ctrl | Right Shift | Enter | / (slash) |
| Controller | Left stick | Right stick | RT | LT | West face button (Xbox X) | R1 / RB |

The arena is 64 x 36, twice its previous width and height. The camera follows surviving boats and zooms to fit them. Existing boat art, Level 1 animated water, and corner health bars remain.

## Mystery pickups

Power-ups only come from sinking cargo AI boats. Each destroyed boat drops one mystery chest at its location. Anyone can collect it, regardless of who sank the boat; nothing is automatically awarded to the killer. Unclaimed loot disappears after 20 seconds of active play. Escaped boats drop nothing. There are no ambient pickups or pickup respawns. Collection fills primary, then reserve; when full, a new item replaces reserve. Activate starts the primary for five seconds by default. Reserve waits until expiry and requires a fresh activation press. No combined weapons or ammo/fuel consumption.

Select Battle Arena to edit Pickup Pool. Each entry exposes type, spawn weight, duration, color, and optional icon. Weight zero disables an entry; an empty pool hides pickups. New weapon behaviors require an enum entry and combat implementation. Mystery chests always hide their contents. Cargo spawn interval, alive cap, loot lifetime, fog damage, camera padding, and status settings are adjustable. A fully disabled pickup pool disables loot.

Items: rapid fire; shotgun; laser; fast boat (hold Boost); shield (blocks damage/status/push); shockwave (damage and knockback pulse each second); freeze shot (movement freeze); fire shot (burn damage); slow homing missile (limited turn rate); slow shot (movement reduction). All share the same two inventory slots and one active effect.

Freeze duration defaults to 1 second, slow to 3 seconds at half speed, burning to 3 seconds at 6 damage/second, shockwave radius to 6. These status durations are separate from item activation duration. Repeated status hits refresh duration; they do not multiply strength. Basic cannon remains usable between items and during defensive/movement items. Disconnecting a joined controller pauses simulation and timers.

TimedLoadout owns slot rules independently of Unity. The inventory checks in Tests/BattleMode exercise the actual loadout source. Runtime/editor compilation does not replace visual and controller Play Mode testing. Online networking is not implemented.

## Swapping VFX

Select Battle Arena > Vfx, or open Assets/BattleMode/BattleVfx.asset. Shots lists per-weapon projectile, muzzle, and impact prefabs from Master Stylized Projectiles. The None entry is the basic cannon and fallback for defensive items. Cartoon FX fields control attached burning/frozen/slow-poison/shield/boost effects, shockwave pulses, and boat death explosions. Slow Poison is the slow debuff's visual; it does not add poison damage.

Each visual exposes prefab, scale, rotation correction, and one-shot lifetime. Duplicate the profile to make another visual theme and assign it to the scene. Unassigned fields retain simple visual fallbacks. Unused package effects can remain unused. Looping projectile/status lifetimes follow their gameplay owner; one-shot lifetimes use the profile setting. Imported demo scripts, physics, and audio are disabled on spawned visual copies so damage and movement remain in the battle simulation. Imported assets are not modified. Particles pause during controller disconnection; transient effects are cleaned on reset.

VFX defaults are selected by asset purpose, not visually tuned in Play Mode. Verify particle scale, rotation, and material compatibility in the Game view and adjust the profile as needed.

## Merged player prefab and mappings

Battle Arena now instantiates the full Assets/Prefabs/Player.prefab, including its Rigidbody2D, collider, boat, turret, wake particles, and PlayerInput. The prefab references Assets/ControllerInput.inputactions. Existing Player/Move, Player/Look, Player/Shoot and both control schemes are preserved. Battle-only Join, StartMatch, Reset, Boost, and Activate actions live in an added Battle map in that same asset. Edit those bindings in Unity's Input Actions editor; battle input is scoped to each paired controller.

Campaign PlayerController/PlayerHealth are disabled on battle instances. PlayerController's Awake exits before campaign setup when a BattleBoat is present. BattleBoat retains battle-specific movement, health, status effects and timed inventory; campaign instances keep their existing behavior. Cursor UI is hidden only on battle clones. The source prefab is not rewritten.

Controller Start joins. Press Start again (or A) to start; with exactly one joined player this launches SOLO TEST, which does not auto-win. With multiple players, last boat alive wins. Enter in a one-controller lobby adds a keyboard opponent, using WASD/mouse/E; in a keyboard-only match the second keyboard player uses arrows/Right Ctrl/Enter. B/Esc returns to lobby. The menu Battle Mode > Validate Merged Input checks the prefab mapping. In Play Mode, Battle Mode > Test Controller Lobby In Play Mode uses a temporary virtual controller to verify full prefab spawning, pairing, join/start, movement, shooting, and solo mode. The smoke test resets only the running match, then removes the test controller.


## Fog and cargo traffic

The solid walls are removed. The safe play area stays fixed at 64 x 36; crossing any edge enters passable fog that deals 10 hull damage per second. The border uses the campaign fog material. Returning to safe water stops damage; shields retain their damage protection. Damage, cargo movement/spawning, and loot timers pause when gameplay pauses.

Select Battle Arena to edit Cargo Spawn Interval (5 seconds), Max Cargo Boats (4), Loot Lifetime (20 seconds), Fog Damage Per Second (10), and fog opacity. Select Assets/BattleMode/CargoBoat.prefab to edit Max Health (30), Speed (5), and Can Shoot (off by default). Optional shooting targets nearby players and exposes range, interval, and damage. Cargo is excluded from victory and spawns during solo testing too.

Boats enter beyond the camera and safe area, follow randomized cubic curves through the arena, and despawn at the far off-screen endpoint. Routes are sampled to avoid islands. Cargo accepts regular projectiles, lasers, homing missiles, shockwaves, freeze, fire and slow effects. Boat deaths reuse the configured explosion VFX. The dedicated prefab uses the existing boat/cannon artwork; swap its renderers to change the cargo skin.

## Sea mines

Each player starts every match with 3 sea mines (Battle Arena > Mines Per Player). The Mine button drops the Animal prefab behind the boat, where it lurks. After a one-second arming delay it behaves like the campaign Animal: when an opposing boat comes within its detection range it charges, and within attack range it pauses, lunges, then drags to a stop and cools down. If it hits a boat it deals its contact damage and is destroyed; shields block it. It never targets or collides with the player who dropped it, and other players can shoot it. Detection range, attack range, speed, lunge and damage come from the AnimalController on the Animal prefab, and health from its EnemyHealth, so edit the prefab to tune both modes. Those campaign scripts are stripped from battle copies. Mine bindings live in the Battle map of ControllerInput.inputactions.
