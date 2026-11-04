# GDD316 local battle

Open BattleArena.unity and focus the Game view. Start joins a controller; Start again or A starts the match (one player launches solo testing). Enter creates keyboard players or adds a keyboard opponent. Last player boat afloat wins; cargo never counts toward victory. Esc/B resets. Start-screen joins still carry into battle and rematch with the same players.

| Player | Move | Aim | Fire | Offensive activation | Defensive activation |
| --- | --- | --- | --- | --- | --- |
| Keyboard P1 | WASD | Mouse | Click / Space | E | Left Shift |
| Keyboard P2 | Arrows | Last arrow direction | Right Ctrl | Enter | Right Shift |
| Controller | Left stick | Right stick | RT | Square / X (west face) | LT |

## One item per category

Each player has exactly one offensive slot and one defensive slot. Each slot stores an item or runs that item; there are no reserves. A new pickup replaces the item in its category, cancels it if it was running, and waits for a fresh activation press. It never changes the other category. One offense and one defense can run together; two offensive effects cannot combine.

Durations default to five seconds and are editable per Pickup Pool entry on Battle Arena. They count combat time, not uses. Fast Boat now gives its speed boost for the full activated duration without holding LT. Old Battle/Boost bindings remain for campaign compatibility; battle defense uses Battle/ActivateDefense. The original Player/Move, Look, Shoot mappings and Player prefab are preserved.

## Cargo and fog

OffensiveCargoBoat.prefab: smaller (0.85 scale), faster (6.5), 20 health, orange tint. DefensiveCargoBoat.prefab: larger (1.2), slower (3.5), 50 health, blue tint. Each drops a random item only from its category. The original CargoBoat.prefab remains as a fallback. Each variant has its own Can Shoot toggle, health, speed, shooting interval/range/damage, and editable art. Scene defaults choose each category with equal probability.

Traffic enters off-screen, follows curved routes avoiding islands, and despawns without loot if it escapes. Only sinking cargo spawns mystery loot. Whoever physically collects a drop receives it, regardless of killer. Drops expire after 20 seconds. Battle Arena exposes spawn interval (5 seconds), maximum live cargo (4), category spawn ratio, and loot lifetime. A disabled category pool produces no loot for that category.

The fixed safe area is 64 x 36. No walls block the fog; crossing an edge deals 10 hull damage/second. Re-entry stops damage. Shield and Star block fog damage; Underwater does not. The camera frames every surviving boat, including underwater and smoke-hidden players, so neither player is lost from view. Boat rendering, labels and attached effects remain hidden during stealth. Gameplay timers pause on controller disconnect.

## Offensive items

Rapid Fire, Shotgun, Laser, Shockwave, Freeze Shot, Fire Shot, Homing Missile, Slow Shot, and 360 Shotgun.

Laser has a continuous raycast aiming line and hit marker during activation; both match the actual shot's first collision and ignore submerged boats. The guide stops at smoke to avoid revealing hidden targets. 360 Shotgun fires a radial volley with light knockback; pellet count, damage and firing interval are adjustable. It is an additional item; the original shotgun and shockwave remain.

## Defensive items

- Shield: damage/status/knockback protection.
- Fast Boat: automatic speed boost while active.
- Star: cycling rainbow boat colors, 20% faster by default, invincible; clears existing burn/freeze/slow on activation.
- Smoke Screen: deploys a fixed opaque area at activation. Any boat inside, including cargo, is hidden to everyone. Labels, attached VFX, aiming guides and item popups are hidden too. Leaving restores visibility. Replacing this active item removes its cloud.
- Underwater: hides the boat, suppresses all offensive firing, ignores weapon hits and passes through other boats while still colliding with islands. Fog still damages it. Brief bubbles appear roughly every 1.2 seconds for 0.3 seconds, with timing adjustable. It cannot be acquired as a homing/AI shooting target while hidden.

Smoke radius, Star/Fast Boat speed multipliers, bubble interval/visibility time, laser range, 360 shotgun tuning, status durations and damage are exposed on Battle Arena.

## Images and VFX

Collecting an item shows an actual world-space billboard box with an icon, category and item name above the boat for two seconds. Its screen size stays readable as the shared camera zooms. Every item has a distinct code-drawn fallback icon; set the Pickup Pool entry's Icon to replace it with custom sprite artwork. These are functional placeholder icons, not finished illustrations.

BattleVfx.asset controls swappable projectile/muzzle/impact mappings from Master Stylized Projectiles and boat effects from Cartoon FX. It includes smoke and intermittent bubble mappings. Smoke uses an opaque backing to guarantee concealment even with sparse particles. Imported effect scripts/physics/audio are disabled on visual copies; gameplay owns damage and movement. Original imported assets are not changed.

The full original Player prefab supplies visuals, collider, Rigidbody2D and PlayerInput. Campaign progression/health scripts are disabled on battle clones; BattleBoat handles local battle simulation. Online multiplayer is not implemented.

Compilation and standalone slot/fog checks do not replace Play Mode testing. Run the standalone checks with dotnet run --project Tests/BattleMode/Tests.csproj. Playtesting is left to the user.

## Heart HUD and audio

The old corner text panels are replaced by a screen-space canvas with a 1920 x 1080 reference scale. Each corner shows a player-colored heart rim, ocean-blue empty health, red liquid filling upward to the health level, a side-view sailboat silhouette, and a large percentage. Below are item icons and device-specific controls (Xbox X/LT, PlayStation Square/L2, or keyboard keys). Active items gain a grey overlay from top to bottom with large seconds remaining. Countdown numbers, GO, lobby prompts, world labels and match results are larger. Stealth still hides world labels.

Battle Arena > Audio Profile references BattleAudio.asset. It loops the existing campaign LevelMusic.mp3 and uses existing generic shot, hit and sink clips. Missing themed weapon/defense/UI clips get temporary synthesized sounds; those are placeholders, not finished sound design. Optional item loops and burning loops are supported. One sound is played per shotgun volley; repeated impacts/damage are throttled and SFX voices are capped. Music and SFX volume are independent.

See Assets/Sound/Battle/SOUND_LIST.md for every requested filename, including all fourteen items. Importing matching audio clips under Assets/Sound/Battle auto-assigns them; Battle Mode > Assign Named Battle Sounds can be run manually. All profile fields remain directly editable. Play Mode visual/audio testing is left to the user.


## Quick item testing

Open Battle Mode > Item Tester while in Play Mode. Join/start, select P1/P2/etc., choose an item and test duration, then Give item or Give and activate now. The window defaults to SmokeScreen and 15 seconds for convenient testing. Giving changes only that player copy, not the regular pickup pool.

Alternatively drag any prefab from Assets/BattleMode/TestPickups into BattleArena near a spawn point. SmokeScreen.prefab and Underwater.prefab are useful first choices. Start the match, sail over the icon, then press LT/L2 (keyboard Shift) for defense or Square/X (keyboard E/Enter) for offense. The prefab Inspector exposes item, duration, respawn and respawn delay. Default test pickups last five seconds and respawn after eight seconds. Remove test pickups from the shipping arena when finished.
