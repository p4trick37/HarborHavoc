using System.Collections.Generic;
using UnityEngine;

namespace HarborBattle
{
    public enum BattleCue {PlayerHit,PlayerSink,CargoHit,CargoSink,Pickup,Activate,Expire,Countdown,Go,Victory,ShieldBlock,Bubbles,FogDamage}

    public class BattleAudio : MonoBehaviour
    {
        public static BattleAudio Instance {get;private set;}
        private BattleAudioProfile profile;
        private AudioSource music;
        private AudioSource[] voices;
        private readonly Dictionary<string,float> last=new Dictionary<string,float>();
        private readonly List<AudioSource> loops=new List<AudioSource>();
        private readonly Dictionary<string,AudioClip> generated=new Dictionary<string,AudioClip>();
        private int voice;
        private bool paused;
        public void Initialize(BattleAudioProfile settings)
        {
            Instance=this;profile=settings;
            music=gameObject.AddComponent<AudioSource>();music.playOnAwake=false;music.loop=true;music.spatialBlend=0;
            music.volume=profile!=null ? profile.musicVolume : .3f;
            music.clip=profile!=null ? profile.music : null;
            if(music.clip!=null)
            {
                if(AudioManager.Instance!=null) AudioManager.Instance.StopBackgroundMusic();
                music.Play();
            }
            voices=new AudioSource[profile!=null ? Mathf.Clamp(profile.maxVoices,4,32) : 16];
            for(int i=0;i<voices.Length;i++) {voices[i]=gameObject.AddComponent<AudioSource>();voices[i].playOnAwake=false;voices[i].spatialBlend=0;}
        }
        public void Shot(PowerUp kind)
        {
            var item=profile?.For(kind);
            AudioClip clip=item?.shot;
            if(clip==null && kind==PowerUp.None) clip=profile?.cannonShot;
            Play("shot"+kind,clip!=null ? clip : Synth(kind,0),item?.volume ?? .7f,.065f);
        }
        public bool HasSpecificImpact(PowerUp kind)
        {
            var clip=profile?.For(kind)?.impact;
            return clip!=null && clip!=profile.cannonImpact && clip!=profile.playerHit && clip!=profile.cargoHit;
        }
        public void Impact(PowerUp kind,bool hitBoat=false)
        {
            // Damage itself supplies the campaign hit cue unless this named override exists.
            if(hitBoat && !HasSpecificImpact(kind))return;
            var item=profile?.For(kind);
            AudioClip clip=item?.impact;
            if(clip==null) clip=profile?.cannonImpact;
            Play("impact"+kind,clip!=null ? clip : Synth(PowerUp.None,1),item?.volume ?? .6f,.09f);
        }
        public AudioSource ActivateItem(PowerUp kind,Transform owner)
        {
            var item=profile?.For(kind);
            Play("activate"+kind,item?.activate!=null ? item.activate : Synth(kind,2),item?.volume ?? .6f,.05f);
            return StartLoop(item?.loop,item?.volume ?? .6f,owner);
        }
        private AudioSource StartLoop(AudioClip clip,float volume,Transform owner)
        {
            if(clip==null)return null;
            var source=owner.gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;
            source.clip=clip;source.loop=true;source.volume=(profile!=null?profile.sfxVolume:.7f)*volume*.5f;
            source.Play();loops.Add(source);return source;
        }
        public void Burn(Transform owner,bool active,ref AudioSource loop)
        {
            if(active && loop==null)loop=StartLoop(profile?.burnLoop,.4f,owner);
            else if(!active && loop!=null){loop.Stop();loops.Remove(loop);Destroy(loop);loop=null;}
        }
        public void EndItem(PowerUp kind,ref AudioSource loop)
        {
            if(loop!=null) {loop.Stop();loops.Remove(loop);Destroy(loop);loop=null;}
            if(kind==PowerUp.None)return;
            var item=profile?.For(kind);
            var clip=item?.end!=null ? item.end : profile?.expire;
            Play("end"+kind,clip!=null?clip:Synth(kind,4),.45f,.08f);
        }
        public void Cue(BattleCue cue)
        {
            AudioClip clip=null;
            if(profile!=null)
                switch(cue)
                {
                    case BattleCue.PlayerHit:clip=profile.playerHit;break;case BattleCue.PlayerSink:clip=profile.playerSink;break;
                    case BattleCue.CargoHit:clip=profile.cargoHit;break;case BattleCue.CargoSink:clip=profile.cargoSink;break;
                    case BattleCue.Pickup:clip=profile.pickup;break;case BattleCue.Activate:clip=profile.activate;break;
                    case BattleCue.Expire:clip=profile.expire;break;case BattleCue.Countdown:clip=profile.countdown;break;
                    case BattleCue.Go:clip=profile.go;break;case BattleCue.Victory:clip=profile.victory;break;
                    case BattleCue.ShieldBlock:clip=profile.shieldBlock;break;case BattleCue.Bubbles:clip=profile.bubbles;break;
                    case BattleCue.FogDamage:clip=profile.fogDamage!=null ? profile.fogDamage : profile.playerHit;break;
                }
            if(clip==null)
            {
                PowerUp kind=cue==BattleCue.ShieldBlock ? PowerUp.Shield : cue==BattleCue.Bubbles ? PowerUp.Underwater : cue==BattleCue.PlayerSink || cue==BattleCue.CargoSink ? PowerUp.HomingMissile : PowerUp.Star;
                clip=Synth(kind,3+(int)cue);
            }
            float cooldown=cue==BattleCue.FogDamage ? .5f : cue==BattleCue.PlayerHit || cue==BattleCue.CargoHit ? .25f : .06f;
            Play("cue"+cue,clip,.75f,cooldown);
        }
        private void Play(string key,AudioClip clip,float volume,float interval)
        {
            if(clip==null || voices==null || (last.TryGetValue(key,out float time) && Time.unscaledTime-time<interval))return;
            last[key]=Time.unscaledTime;
            var source=voices[voice++%voices.Length];source.Stop();source.clip=clip;source.pitch=1;
            source.volume=volume*(profile!=null?profile.sfxVolume:.7f);source.Play();
        }
        private AudioClip Synth(PowerUp kind,int phase)
        {
            string key=kind+":"+phase;
            if(generated.TryGetValue(key,out var clip))return clip;
            const int rate=22050;float duration=phase==0?.22f:phase==1?.28f:.4f;
            var samples=new float[(int)(rate*duration)];var random=new System.Random((int)kind*127+phase);float noise=0;
            for(int i=0;i<samples.Length;i++)
            {
                float t=i/(float)rate,u=t/duration;noise=noise*.8f+((float)random.NextDouble()*2-1)*.2f;
                float signal;
                switch(kind)
                {
                    case PowerUp.FreezeShot: signal=Mathf.Sin(t*2*Mathf.PI*(2200-800*u))*.4f+Mathf.Sin(t*2*Mathf.PI*3470)*.2f+noise*.3f;break;
                    case PowerUp.FireShot:signal=noise*2+Mathf.Sin(t*2*Mathf.PI*(130-60*u))*.3f;break;
                    case PowerUp.Laser:signal=Mathf.Sin(t*2*Mathf.PI*(1800-1500*u))*.7f;break;
                    case PowerUp.HomingMissile:signal=noise*2+Mathf.Sin(t*2*Mathf.PI*(90-50*u))*.5f;break;
                    case PowerUp.SlowShot:case PowerUp.Underwater:signal=Mathf.Sin(t*2*Mathf.PI*(430-300*u))*(.5f+.5f*Mathf.Sin(t*70));break;
                    case PowerUp.Shield:signal=Mathf.Sin(t*2*Mathf.PI*540)*.5f+Mathf.Sin(t*2*Mathf.PI*811)*.25f;break;
                    case PowerUp.Star:signal=Mathf.Sin(t*2*Mathf.PI*(phase==3+(int)BattleCue.Countdown?660:660+220*Mathf.Floor(u*3)))*.5f;break;
                    case PowerUp.FastBoat:case PowerUp.SmokeScreen:signal=noise*1.5f;break;
                    default:signal=noise*1.4f+Mathf.Sin(t*2*Mathf.PI*(180-130*u))*.4f;break;
                }
                samples[i]=Mathf.Clamp(signal,-1,1)*Mathf.Min(1,t/.008f)*Mathf.Pow(1-u,2)*.38f;
            }
            clip=AudioClip.Create("Temporary "+key,samples.Length,1,rate,false);clip.SetData(samples,0);generated.Add(key,clip);return clip;
        }
        private void Update()
        {
            bool pause=BattleArena.Instance!=null && BattleArena.Instance.Running && !BattleArena.Instance.CanSimulate;
            loops.RemoveAll(source=>source==null);
            if(pause==paused)return;paused=pause;
            foreach(var source in voices) {if(pause)source.Pause();else source.UnPause();}
            foreach(var source in loops) {if(pause)source.Pause();else source.UnPause();}
        }
        private void OnDestroy()
        {
            if(Instance==this)Instance=null;
            foreach(var clip in generated.Values) if(clip!=null)Destroy(clip);
        }
    }
}
