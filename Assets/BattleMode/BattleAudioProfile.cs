using System;
using System.Collections.Generic;
using UnityEngine;

namespace HarborBattle
{
    [Serializable]
    public class ItemAudio
    {
        public PowerUp kind;
        public AudioClip shot,impact,activate,loop,end;
        [Range(0,1)] public float volume=.7f;
    }

    [CreateAssetMenu(menuName="Battle Mode/Audio Profile")]
    public class BattleAudioProfile : ScriptableObject
    {
        public AudioClip music,cannonShot,cannonImpact,playerHit,playerSink,cargoHit,cargoSink;
        public AudioClip pickup,activate,expire,countdown,go,victory,shieldBlock,bubbles,fogDamage;
        public AudioClip burnLoop;
        [Range(0,1)] public float musicVolume=.3f,sfxVolume=.7f;
        [Range(4,32)] public int maxVoices=16;
        public List<ItemAudio> items=new List<ItemAudio>();
        public ItemAudio For(PowerUp kind) => items.Find(item=>item!=null && item.kind==kind);
    }
}
