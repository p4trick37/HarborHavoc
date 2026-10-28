using System;
using System.Collections.Generic;
using UnityEngine;

namespace HarborBattle
{
    [Serializable]
    public class VfxVisual
    {
        public GameObject prefab;
        [Min(.01f)] public float scale=1;
        [Tooltip("Local rotation correction. Projectile forward is mapped into the XY playfield.")]
        public Vector3 rotation;
        [Min(.1f)] public float lifetime=3;
    }

    [Serializable]
    public class ShotVfx
    {
        public PowerUp kind;
        public VfxVisual projectile=new VfxVisual();
        public VfxVisual muzzle=new VfxVisual();
        public VfxVisual impact=new VfxVisual();
    }

    [CreateAssetMenu(menuName="Battle Mode/VFX Profile")]
    public class BattleVfxProfile : ScriptableObject
    {
        [Header("Master Stylized Projectiles: one entry per weapon")]
        public List<ShotVfx> shots=new List<ShotVfx>();
        [Header("Cartoon FX: boat statuses and explosions")]
        public VfxVisual burning=new VfxVisual();
        public VfxVisual frozen=new VfxVisual();
        public VfxVisual slowedPoison=new VfxVisual();
        public VfxVisual shield=new VfxVisual();
        public VfxVisual boost=new VfxVisual();
        public VfxVisual shockwave=new VfxVisual();
        public VfxVisual boatExplosion=new VfxVisual();
        public ShotVfx Shot(PowerUp kind)
        {
            ShotVfx fallback=null;
            foreach(var shot in shots)
            {
                if(shot==null) continue;
                if(shot.kind==kind) return shot;
                if(shot.kind==PowerUp.None) fallback=shot;
            }
            return fallback;
        }
    }

    public static class BattleVfx
    {
        public static GameObject Spawn(VfxVisual visual, Vector3 position, Transform parent=null, bool loop=false, Vector2 direction=default)
        {
            if(visual==null || visual.prefab==null) return null;
            // Instantiate inactive so imported demo drivers cannot start moving,
            // destroying or shaking the camera before they have been disabled.
            var root=new GameObject("Battle VFX: "+visual.prefab.name);
            root.SetActive(false);
            root.transform.SetParent(parent,false);
            position.z=-1;
            root.transform.position=position;
            root.transform.rotation=direction.sqrMagnitude>.001f ? Quaternion.LookRotation(new Vector3(direction.x,direction.y,0),Vector3.back) : Quaternion.identity;
            var art=UnityEngine.Object.Instantiate(visual.prefab,root.transform,false);
            art.transform.localPosition=Vector3.zero;
            art.transform.localRotation=Quaternion.Euler(visual.rotation)*art.transform.localRotation;
            art.transform.localScale*=visual.scale;
            foreach(var script in art.GetComponentsInChildren<MonoBehaviour>(true)) if(script!=null) script.enabled=false;
            foreach(var collider in art.GetComponentsInChildren<Collider>(true)) collider.enabled=false;
            foreach(var collider in art.GetComponentsInChildren<Collider2D>(true)) collider.enabled=false;
            foreach(var body in art.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic=true; body.detectCollisions=false; }
            foreach(var body in art.GetComponentsInChildren<Rigidbody2D>(true)) body.simulated=false;
            foreach(var audio in art.GetComponentsInChildren<AudioSource>(true)) audio.enabled=false;
            foreach(var renderer in art.GetComponentsInChildren<Renderer>(true)) { renderer.sortingLayerID=0; renderer.sortingOrder=10; }
            foreach(var particles in art.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main=particles.main;
                main.stopAction=ParticleSystemStopAction.None;
                if(loop) main.loop=true;
                else main.loop=false;
            }
            var life=root.AddComponent<BattleVfxLifetime>();
            life.Remaining=loop ? -1 : visual.lifetime;
            root.SetActive(true);
            foreach(var particles in art.GetComponentsInChildren<ParticleSystem>(true)) particles.Play(false);
            return root;
        }
    }

    public class BattleVfxLifetime : MonoBehaviour
    {
        public float Remaining;
        private bool paused;
        private void Update()
        {
            bool shouldPause=BattleArena.Instance!=null && BattleArena.Instance.Running && !BattleArena.Instance.CanSimulate;
            if(shouldPause!=paused)
            {
                foreach(var particles in GetComponentsInChildren<ParticleSystem>())
                    if(shouldPause) particles.Pause(false); else particles.Play(false);
                paused=shouldPause;
            }
            if(!shouldPause && Remaining>=0 && (Remaining-=Time.deltaTime)<=0) Destroy(gameObject);
        }
    }
}
