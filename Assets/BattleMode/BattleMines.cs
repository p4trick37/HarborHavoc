using UnityEngine;

namespace HarborBattle
{
    // A sea mine is the campaign Animal, dropped behind a boat. It lurks where it was
    // dropped; when an opposing boat comes near it charges and lunges, exactly like the
    // campaign AnimalController, and is destroyed when its hit lands.
    public class BattleAnimal : MonoBehaviour
    {
        public BattleBoat Owner { get; private set; }

        // Copied from the prefab's AnimalController / EnemyHealth so tuning stays in one place.
        private float detectionRange=8, attackRange=2, moveSpeed=2, lungeForce=12, waitBeforeLunge=.75f;
        private float lungeDuration=.4f, lungeCooldown=2, dragDuringLunge=6, normalDrag=0;
        private int contactDamage=10;
        private float health=50;

        private Rigidbody2D body;
        private BattleBoat target;
        private float armTimer=1;
        private bool lunging;
        private float lungeTimer;
        private bool launched;

        public static BattleAnimal Drop(BattleBoat owner)
        {
            var arena=BattleArena.Instance;
            Vector2 behind=(Vector2)owner.transform.position-(Vector2)owner.transform.up*1.6f;
            GameObject obj;
            BattleAnimal animal;
            if(arena.animalPrefab!=null)
            {
                // Read the campaign settings, then strip the campaign scripts before they run;
                // they target the campaign player and use its health system.
                var staging=new GameObject("Battle Animal Staging");
                staging.SetActive(false);
                obj=Instantiate(arena.animalPrefab,staging.transform,false);
                animal=obj.AddComponent<BattleAnimal>();
                var campaign=obj.GetComponent<AnimalController>();
                if(campaign!=null)
                {
                    animal.detectionRange=campaign.detectionRange; animal.attackRange=campaign.attackRange;
                    animal.moveSpeed=campaign.moveSpeed; animal.lungeForce=campaign.lungeForce;
                    animal.waitBeforeLunge=campaign.waitBeforeLunge; animal.lungeDuration=campaign.lungeDuration;
                    animal.lungeCooldown=campaign.lungeCooldown; animal.dragDuringLunge=campaign.dragDuringLunge;
                    animal.normalDrag=campaign.normalDrag; animal.contactDamage=Mathf.RoundToInt(campaign.contactDamage);
                }
                var enemyHealth=obj.GetComponent<EnemyHealth>();
                if(enemyHealth!=null) animal.health=enemyHealth.maxHealth;
                foreach(var script in obj.GetComponentsInChildren<MonoBehaviour>(true))
                    if(script!=animal) DestroyImmediate(script);
                obj.tag="Untagged";
                obj.transform.position=behind;
                obj.transform.SetParent(null,true);
                Destroy(staging);
            }
            else
            {
                obj=BattleArena.Visual("Sea Creature",behind,new Vector2(.9f,1.3f),new Color(.3f,.7f,.4f));
                obj.AddComponent<CircleCollider2D>().radius=.5f;
                animal=obj.AddComponent<BattleAnimal>();
            }
            obj.name="Mine Animal (P"+(owner.Index+1)+")";
            foreach(var sprite in obj.GetComponentsInChildren<SpriteRenderer>()) sprite.sortingOrder=3;
            animal.Owner=owner;
            animal.body=obj.GetComponent<Rigidbody2D>();
            if(animal.body==null) animal.body=obj.AddComponent<Rigidbody2D>();
            animal.body.bodyType=RigidbodyType2D.Dynamic;
            animal.body.gravityScale=0; animal.body.freezeRotation=true;
            animal.body.linearDamping=animal.normalDrag;
            animal.body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            animal.body.rotation=owner.GetComponent<Rigidbody2D>().rotation+180;
            // Never attacks or blocks the player who dropped it.
            foreach(var mine in obj.GetComponentsInChildren<Collider2D>())
                foreach(var boat in owner.GetComponentsInChildren<Collider2D>())
                    Physics2D.IgnoreCollision(mine,boat);
            return animal;
        }

        public void Hit(int amount)
        {
            if((health-=amount)>0) return;
            BattleVfx.Spawn(BattleArena.Instance.vfx?.Shot(PowerUp.None)?.impact,transform.position);
            Destroy(gameObject);
        }

        private void FixedUpdate()
        {
            var arena=BattleArena.Instance;
            if(!arena.CanSimulate) { body.linearVelocity=Vector2.zero; return; }
            float dt=Time.fixedDeltaTime;
            if(armTimer>0) { armTimer-=dt; body.linearVelocity=Vector2.zero; return; }
            if(lunging) { Lunge(dt); return; }

            target=Nearest();
            if(target==null) { body.linearVelocity=Vector2.zero; return; }
            Vector2 offset=target.transform.position-transform.position;
            float distance=offset.magnitude;
            if(distance<=attackRange)
            {
                lunging=true; launched=false; lungeTimer=0;
                body.linearVelocity=Vector2.zero; body.linearDamping=normalDrag;
            }
            else
            {
                // Charge toward the boat it spotted.
                Vector2 direction=offset/distance;
                body.MovePosition(body.position+direction*moveSpeed*dt);
                body.rotation=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-90;
            }
        }

        // Same rhythm as the campaign: pause, lunge, drag to a stop, cool down.
        private void Lunge(float dt)
        {
            lungeTimer+=dt;
            if(!launched)
            {
                body.linearVelocity=Vector2.zero;
                if(lungeTimer<waitBeforeLunge) return;
                launched=true;
                if(target!=null && target.CanBeTargeted)
                {
                    Vector2 direction=((Vector2)(target.transform.position-transform.position)).normalized;
                    body.rotation=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-90;
                    body.linearDamping=0;
                    body.AddForce(direction*lungeForce,ForceMode2D.Impulse);
                }
                return;
            }
            float sinceLaunch=lungeTimer-waitBeforeLunge;
            if(sinceLaunch>=lungeDuration) body.linearDamping=dragDuringLunge;
            if(sinceLaunch>=lungeDuration+lungeCooldown) { body.linearDamping=normalDrag; lunging=false; }
        }

        // The closest opposing boat inside detection range.
        private BattleBoat Nearest()
        {
            BattleBoat best=null; float closest=detectionRange;
            foreach(var boat in BattleArena.Instance.Boats)
            {
                if(boat==Owner || !boat.CanBeTargeted) continue;
                float distance=Vector2.Distance(boat.transform.position,transform.position);
                if(distance<=closest) { best=boat; closest=distance; }
            }
            return best;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            var boat=collision.collider.GetComponentInParent<BattleBoat>();
            if(boat==null || boat==Owner || !boat.Alive || armTimer>0) return;
            // A shielded boat shrugs it off; otherwise the hit lands and the Animal is destroyed.
            if(!boat.Hit(contactDamage,PowerUp.None,Vector2.zero)) return;
            BattleVfx.Spawn(BattleArena.Instance.vfx?.Shot(PowerUp.None)?.impact,collision.GetContact(0).point);
            Destroy(gameObject);
        }
    }
}
