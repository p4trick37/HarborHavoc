using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HarborBattle.Editor
{
    public static class BattleAudioSetup
    {
        [MenuItem("Battle Mode/Assign Named Battle Sounds")]
        public static void Assign()
        {
            var profile=AssetDatabase.LoadAssetAtPath<BattleAudioProfile>("Assets/BattleMode/BattleAudio.asset");
            if(profile==null)return;
            var clips=new Dictionary<string,AudioClip>(StringComparer.OrdinalIgnoreCase);
            string Normalize(string name) => System.Text.RegularExpressions.Regex.Replace(name,"[^a-zA-Z0-9]","").ToLowerInvariant();
            foreach(var guid in AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/Sound"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid),name=Path.GetFileNameWithoutExtension(path);
                var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                string normalized=Normalize(name);
                if(!clips.ContainsKey(normalized) || path.StartsWith("Assets/Sound/Battle",StringComparison.OrdinalIgnoreCase))clips[normalized]=clip;
                // Composite names can intentionally identify several uses.
                foreach(var part in name.Split('_'))
                    if(part.Length>5 && !clips.ContainsKey(Normalize(part)))clips[Normalize(part)]=clip;
            }
            void Set(ref AudioClip field,params string[] names)
            {
                for(int i=0;i<names.Length;i++)
                {
                    bool generic=field==null || field.name=="PlayerShoot" || field.name=="PlayerHit" || field.name=="PlayerDie" || field.name=="EnemyHit" || field.name=="EnemyDied";
                    if(i>0 && !generic)continue;
                    if(clips.TryGetValue(Normalize(names[i]),out var clip)){field=clip;return;}
                }
            }
            Set(ref profile.music,"LevelMusic");Set(ref profile.cannonShot,"Cannon_Shot","PlayerShoot");Set(ref profile.cannonImpact,"Cannon_Impact","PlayerHit");
            Set(ref profile.playerHit,"Player_Hit","PlayerHit");Set(ref profile.playerSink,"Player_Sink","PlayerDie");
            Set(ref profile.cargoHit,"Cargo_Hit","EnemyHit");Set(ref profile.cargoSink,"Cargo_Sink","EnemyDied");
            Set(ref profile.pickup,"Item_Pickup");Set(ref profile.activate,"Item_Activate");Set(ref profile.expire,"Item_Expire");
            Set(ref profile.countdown,"Countdown_Tick");Set(ref profile.go,"Match_Go");Set(ref profile.victory,"Match_Victory");
            Set(ref profile.shieldBlock,"Shield_Block","ShieldBlockedShot");Set(ref profile.bubbles,"Underwater_Bubbles");Set(ref profile.fogDamage,"Fog_Damage");
            Set(ref profile.burnLoop,"FireShot_BurnLoop");
            foreach(PowerUp kind in Enum.GetValues(typeof(PowerUp)))
            {
                if(kind==PowerUp.None)continue;
                var item=profile.For(kind);if(item==null){item=new ItemAudio {kind=kind};profile.items.Add(item);}
                string stem=kind.ToString();
                if(item.impact==profile.playerHit || item.impact==profile.cargoHit || item.impact==profile.cannonImpact)item.impact=null;
                Set(ref item.shot,stem+"_Shot");Set(ref item.impact,stem+"_Impact");
                Set(ref item.activate,stem+"_Activate",stem+"Activated");Set(ref item.loop,stem+"_Loop");Set(ref item.end,stem+"_End");
                if(kind==PowerUp.FreezeShot)Set(ref item.shot,"FreezeShot_Shot","FrostShot");
                if(kind==PowerUp.FireShot)Set(ref item.impact,"FireShot_Impact","FireShotHit");
                if(kind==PowerUp.SlowShot)Set(ref item.shot,"SlowShot_Shot","SlowShot");
                if(kind==PowerUp.HomingMissile){Set(ref item.shot,"HomingMissile_Launch","HomingRocketShot");Set(ref item.impact,"HomingMissile_Explosion","HomingRocketImpact");}
                if(kind==PowerUp.Shockwave)Set(ref item.shot,"Shockwave_Pulse");
                if(kind==PowerUp.Underwater){Set(ref item.activate,"Underwater_Dive");Set(ref item.end,"Underwater_Surface","UnderwaterEnd");}
                if(kind==PowerUp.Shotgun360){Set(ref item.shot,"Shotgun360_Shot","Shotgun_Shot");Set(ref item.impact,"Shotgun360_Impact","Shotgun_Impact");}
            }
            EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();Debug.Log("Battle audio profile updated from named sounds. Missing themed clips retain temporary fallbacks.");
        }
    }
    public class BattleSoundImporter : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported,string[] deleted,string[] moved,string[] movedFrom)
        {
            foreach(var path in imported)
                if(path.StartsWith("Assets/Sound/Battle/",StringComparison.OrdinalIgnoreCase) && (path.EndsWith(".wav",StringComparison.OrdinalIgnoreCase)||path.EndsWith(".ogg",StringComparison.OrdinalIgnoreCase)||path.EndsWith(".mp3",StringComparison.OrdinalIgnoreCase)))
                {EditorApplication.delayCall+=BattleAudioSetup.Assign;break;}
        }
    }
}
