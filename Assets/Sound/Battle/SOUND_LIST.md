# Battle sound filenames

Import clips anywhere under Assets/Sound/Battle. WAV is preferred; OGG and MP3 work too, using the same filenames without changing the stem. Named clips automatically update Assets/BattleMode/BattleAudio.asset on import. You can also run Battle Mode > Assign Named Battle Sounds, or assign clips manually in that profile.

Campaign LevelMusic.mp3 is already linked. Player damage uses the campaign PlayerHit sound unless that weapon has a named impact override. The override replaces the generic hit cue. Missing launch/UI sounds retain temporary fallbacks; optional loops remain silent until supplied.

The imported clips are assigned as follows (spaces and underscores are optional when matching names):

| Imported filename | Use |
| --- | --- |
| FrostShot | Freeze launch |
| FreezeShotImpact | Freeze impact |
| FireShotHit_Burning | Fire impact |
| LaserShot | Laser launch |
| ShotgunShot | Shotgun and 360 shotgun launch |
| HomingRocketShot_EnRoute | Missile launch |
| ShockwavePulse_HomingRocketImpact | Shockwave pulse and missile impact |
| SlowShot | Slow launch |
| SlowShotImpact_TargetSlowed | Slow impact |
| FastBoatActivated | Speed activation |
| ShieldActivated / ShieldBlockedShot | Shield activation / block |
| StarActivate / SmokeScreenActivate | Respective activation |
| UnderwaterDive / UnderwaterBubbles / UnderwaterEnd | Dive / bubbles / surface |

Clip assignments and volumes can be changed in Assets/BattleMode/BattleAudio.asset. The FireShotHit_Burning and HomingRocketShot_EnRoute clips are one-shots, not repeating loops.

| Power-up | Filenames |
| --- | --- |
| Rapid fire | RapidFire_Shot.wav; RapidFire_Impact.wav |
| Shotgun | Shotgun_Shot.wav; Shotgun_Impact.wav |
| Laser | Laser_Shot.wav; Laser_Impact.wav |
| Shockwave | Shockwave_Pulse.wav; Shockwave_Impact.wav |
| Freeze shot | FreezeShot_Shot.wav; FreezeShot_Impact.wav |
| Fire shot | FireShot_Shot.wav; FireShot_Impact.wav |
| Homing missile | HomingMissile_Launch.wav; HomingMissile_Explosion.wav |
| Slow shot | SlowShot_Shot.wav; SlowShot_Impact.wav |
| 360-degree shotgun | Shotgun360_Shot.wav; Shotgun360_Impact.wav (may reuse shotgun clips) |
| Fast boat | FastBoat_Activate.wav; FastBoat_Loop.wav; FastBoat_End.wav |
| Shield | Shield_Activate.wav; Shield_Block.wav; Shield_End.wav |
| Star | Star_Activate.wav; Star_Loop.wav; Star_End.wav |
| Smoke screen | SmokeScreen_Activate.wav; SmokeScreen_End.wav |
| Underwater | Underwater_Dive.wav; Underwater_Bubbles.wav; Underwater_Surface.wav |

Shared events:

- Cannon_Shot.wav; Cannon_Impact.wav
- Player_Hit.wav; Player_Sink.wav
- Cargo_Hit.wav; Cargo_Sink.wav
- Item_Pickup.wav; Item_Activate.wav; Item_Expire.wav
- Countdown_Tick.wav; Match_Go.wav; Match_Victory.wav

Optional: FireShot_BurnLoop.wav; Fog_Damage.wav. Any item may also provide ItemName_Activate.wav, ItemName_Loop.wav, or ItemName_End.wav using its enum name; the listed Underwater Dive/Surface names are recognized aliases. Loops should loop cleanly; shot/impact clips should be short with little leading silence. All clips can be swapped manually, with independent music, SFX and per-item volume controls.

Priority: themed weapon shot/impact sounds, then defensive activation sounds, then UI cues. Existing generic clips cover the rest while gathering assets. Do not rename the existing campaign music.
