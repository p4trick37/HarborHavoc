# GDD316 local battle

Open BattleArena.unity, Play, and focus the Game view. Controllers press Start to join, then A starts the countdown. With no controllers, two keyboard boats appear; Enter starts the match. Last boat afloat wins. Esc resets the lobby.

| Player | Move | Aim | Fire | Boost | Activate |
| --- | --- | --- | --- | --- | --- |
| P1 keyboard | WASD | Mouse | Click / Space | Left Shift | E |
| P2 keyboard | Arrows | Last arrow direction | Right Ctrl | Right Shift | Enter |
| Controller | Left stick | Right stick | RT | LT | West face button (Xbox X) |

The arena is 64 x 36, twice its previous width and height. The camera follows surviving boats and zooms to fit them. Existing boat art, Level 1 animated water, and corner health bars remain.

## Mystery pickups

Ten mystery chests appear at random clear locations and relocate on respawn. Items reveal when collected. Collection fills primary, then reserve. When full, new collection replaces reserve. Press Activate to start primary; reserve cannot activate until it expires. After expiry, reserve moves to primary and waits for a fresh button press. Items last five seconds by default, even when not firing or boosting. No ammo/fuel consumption and no combined weapons.

Select Battle Arena to edit Pickup Pool. Each entry exposes type, spawn weight, duration, color, and optional icon. Weight zero disables an entry; an empty pool hides pickups. New weapon behaviors require an enum entry and combat implementation. Mystery chests always hide their contents. Pickup Count, Respawn Seconds, camera padding, and status settings are adjustable.

Items: rapid fire; shotgun; laser; fast boat (hold Boost); shield (blocks damage/status/push); shockwave (damage and knockback pulse each second); freeze shot (movement freeze); fire shot (burn damage); slow homing missile (limited turn rate); slow shot (movement reduction). All share the same two inventory slots and one active effect.

Freeze duration defaults to 1 second, slow to 3 seconds at half speed, burning to 3 seconds at 6 damage/second, shockwave radius to 6. These status durations are separate from item activation duration. Repeated status hits refresh duration; they do not multiply strength. Basic cannon remains usable between items and during defensive/movement items. Disconnecting a joined controller pauses simulation and timers.

TimedLoadout owns slot rules independently of Unity. The inventory checks in Tests/BattleMode exercise the actual loadout source. Runtime/editor compilation does not replace visual and controller Play Mode testing. Online networking is not implemented.

## Swapping VFX

Select Battle Arena > Vfx, or open Assets/BattleMode/BattleVfx.asset. Shots lists per-weapon projectile, muzzle, and impact prefabs from Master Stylized Projectiles. The None entry is the basic cannon and fallback for defensive items. Cartoon FX fields control attached burning/frozen/slow-poison/shield/boost effects, shockwave pulses, and boat death explosions. Slow Poison is the slow debuff's visual; it does not add poison damage.

Each visual exposes prefab, scale, rotation correction, and one-shot lifetime. Duplicate the profile to make another visual theme and assign it to the scene. Unassigned fields retain simple visual fallbacks. Unused package effects can remain unused. Looping projectile/status lifetimes follow their gameplay owner; one-shot lifetimes use the profile setting. Imported demo scripts, physics, and audio are disabled on spawned visual copies so damage and movement remain in the battle simulation. Imported assets are not modified. Particles pause during controller disconnection; transient effects are cleaned on reset.

VFX defaults are selected by asset purpose, not visually tuned in Play Mode. Verify particle scale, rotation, and material compatibility in the Game view and adjust the profile as needed.
