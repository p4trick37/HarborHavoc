using UnityEngine;

namespace HarborBattle
{
    [RequireComponent(typeof(Rigidbody2D),typeof(BoxCollider2D))]
    public class BattleAiBoat : MonoBehaviour
    {
        [Header("Cargo boat")]
        public ItemCategory category;
        [Min(1)] public int maxHealth=30;
        [Min(.1f)] public float speed=5;
        [Header("Optional combat (off for cargo traffic)")]
        public bool canShoot=false;
        [Min(.1f)] public float shotInterval=1.5f;
        [Min(0)] public float shootingRange=20;
        [Min(1)] public int shotDamage=10;
        public Transform turret;
        public int Health { get; private set; }
        public bool Alive => Health>0 && !finished;
        private bool finished;
        private Vector2 start,controlA,controlB,end,knockback;
        private float progress,length,fireTimer,frozen,slowed,burning,burnTick;
        private Rigidbody2D body;
        private GameObject burnVisual,freezeVisual,slowVisual;

        private void Awake()
        {
            Health=maxHealth;
            body=GetComponent<Rigidbody2D>();
            body.bodyType=RigidbodyType2D.Kinematic;
            body.gravityScale=0;
            body.freezeRotation=true;
        }

        public static Vector2 Point(Vector2 a,Vector2 b,Vector2 c,Vector2 d,float t)
        {
            float u=1-t;
            return u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d;
        }

        public void SetRoute(Vector2 a,Vector2 b,Vector2 c,Vector2 d)
        {
            start=a; controlA=b; controlB=c; end=d; progress=0; length=0;
            var previous=a;
            for(int i=1;i<=40;i++) { var point=Point(a,b,c,d,i/40f); length+=Vector2.Distance(previous,point); previous=point; }
            transform.position=a;
            body.position=a;
        }

        public void Hit(int damage,PowerUp effect,Vector2 push)
        {
            if(!Alive || !BattleArena.Instance.CanSimulate) return;
            Health=Mathf.Max(0,Health-Mathf.Max(0,damage));
            if(Health==0)
            {
                finished=true;
                GetComponent<Collider2D>().enabled=false;
                BattleArena.Instance.DropLoot(transform.position,category);
                BattleVfx.Spawn(BattleArena.Instance.vfx?.boatExplosion,transform.position);
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }
            var arena=BattleArena.Instance;
            if(effect==PowerUp.FreezeShot) frozen=Mathf.Max(frozen,arena.freezeSeconds);
            if(effect==PowerUp.SlowShot) slowed=Mathf.Max(slowed,arena.slowSeconds);
            if(effect==PowerUp.FireShot) burning=Mathf.Max(burning,arena.burnSeconds);
            knockback+=push;
        }

        private void FixedUpdate()
        {
            var arena=BattleArena.Instance;
            if(arena==null || !arena.CanSimulate || !Alive) return;
            float dt=Time.fixedDeltaTime;
            frozen=Mathf.Max(0,frozen-dt); slowed=Mathf.Max(0,slowed-dt);
            if(burning>0)
            {
                burning-=dt; burnTick+=arena.burnDamagePerSecond*dt;
                int damage=Mathf.FloorToInt(burnTick);
                if(damage>0) { burnTick-=damage; Hit(damage,PowerUp.None,Vector2.zero); }
                if(!Alive) return;
            }
            else burnTick=0;
            Status(ref burnVisual,burning>0,arena.vfx?.burning);
            Status(ref freezeVisual,frozen>0,arena.vfx?.frozen);
            Status(ref slowVisual,slowed>0,arena.vfx?.slowedPoison);
            if(frozen<=0) progress+=dt*speed*(slowed>0 ? arena.slowMultiplier : 1)/Mathf.Max(1,length);
            if(progress>=1) { finished=true; gameObject.SetActive(false); Destroy(gameObject); return; } // Escaped cargo drops nothing.
            Vector2 point=Point(start,controlA,controlB,end,progress);
            Vector2 next=Point(start,controlA,controlB,end,Mathf.Min(1,progress+.01f));
            Vector2 heading=next-point;
            body.MovePosition(point+knockback);
            knockback=Vector2.Lerp(knockback,Vector2.zero,dt*4);
            if(heading.sqrMagnitude>.001f) body.rotation=Mathf.Atan2(heading.y,heading.x)*Mathf.Rad2Deg-90;
            if(!canShoot) return;
            BattleBoat target=null; float closest=shootingRange;
            foreach(var player in arena.Boats)
            {
                float distance=Vector2.Distance(transform.position,player.transform.position);
                if(player.CanBeTargeted && distance<closest) {closest=distance;target=player;}
            }
            fireTimer-=dt;
            if(target==null) return;
            Vector2 aim=((Vector2)target.transform.position-body.position).normalized;
            if(turret!=null) turret.rotation=Quaternion.Euler(0,0,Mathf.Atan2(aim.y,aim.x)*Mathf.Rad2Deg-90);
            if(fireTimer<=0) { BattleShot.SpawnFromAi(this,aim,shotDamage); fireTimer=shotInterval; }
        }

        private void LateUpdate() => BattleVisibility.Apply(transform,BattleSmoke.Contains(transform.position));

        private void Status(ref GameObject instance,bool active,VfxVisual visual)
        {
            if(active && instance==null) instance=BattleVfx.Spawn(visual,transform.position,transform,true);
            else if(!active && instance!=null) {Destroy(instance);instance=null;}
        }
    }
}
