# GDD316 local battle prototype

Open BattleArena.unity and press Play. The scene contains a saved Main Camera, Battle Arena manager with project sprite references, and two islands visible in the editor. Players, pickups, and boundary walls are generated during Play. It is also included at the end of Build Settings; the campaign remains the default startup scene.

For temporary keyboard testing, press Enter in the Game view to create two players and start the countdown. P1: WASD moves, mouse aims, left click or Space fires, Left Shift boosts. P2: arrow keys move and aim (aim persists after release), Right Ctrl fires, Right Shift boosts. Enter after a result returns to the lobby. Escape resets the match. Controllers still support the original joining flow; keyboard mode starts when the lobby is empty.

Each controller presses Start to join (2–4). After everyone joins, press A for a three-second countdown. Left stick moves, right stick aims, right trigger fires, left trigger boosts. Last surviving boat wins. Start after the result returns to the lobby; B resets the lobby before a match. Disconnecting a joined controller pauses simulation until it reconnects.

Each boat has 100 hull. Basic cannon has unlimited ammo. Rapid fire has 40 rounds, shotgun has 8 volleys, laser has 5 shots. Fast boat has 6 seconds of boost fuel, consumed only while moving with LT held. Every pickup replaces the current power-up, including any unused ammo or fuel. Exhaustion restores the basic cannon. Pickup locations respawn after 10 seconds.

Boats, cannons, islands, treasure chest pickups, and projectiles use existing project sprites. Two central islands block boats and shots. Boundary walls and laser beams use simple geometry. Towers, campaign rewards, and fog are absent from this scene.

BattleBoat accepts BoatCommand input independently from controller polling. This is a starting boundary for future online input and authority work; there is no network implementation yet. Balance constants are prototype defaults in the source.

Validation still needs Unity Play Mode with two or four controllers, including disconnect/reconnect, all pickups, elimination, and rematches. Compilation alone does not verify rendering or controller behavior.

The battle scene reuses the Level 1 water SpriteRenderer and animated material, without its campaign fog script or trigger collider. Health bars are anchored to the corners: P1 top left, P2 top right, P3 bottom left, P4 bottom right.

Select Battle Arena in the scene and edit Random Pickup Pool. Each entry has a type, spawn weight, uses (ammo or seconds of boost fuel), color, and optional sprite icon. Entries are rolled independently on initial spawn, respawn, and round reset; duplicates can appear. Zero weight disables an entry. An empty or entirely disabled pool hides all pickups. Add entries with the Inspector list + button; introducing a new kind of weapon behavior also requires implementing its PowerUp enum value and combat behavior. Pickup Respawn Seconds controls the delay.
