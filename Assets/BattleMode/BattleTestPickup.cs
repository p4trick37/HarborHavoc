using UnityEngine;

namespace HarborBattle
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class BattleTestPickup : MonoBehaviour
    {
        public PowerUp item=PowerUp.SmokeScreen;
        [Min(.1f)] public float duration=5;
        public bool respawn=true;
        [Min(.1f)] public float respawnSeconds=8;
        private float wait;
        private bool available=true;
        private SpriteRenderer picture;
        private void Awake()
        {
            GetComponent<BoxCollider2D>().isTrigger=true;
            picture=GetComponentInChildren<SpriteRenderer>();
            if(picture==null)
            {
                var visual=new GameObject("Item Icon",typeof(SpriteRenderer));visual.transform.SetParent(transform,false);picture=visual.GetComponent<SpriteRenderer>();
            }
            BattleArena.ApplyArt(gameObject,BattleItemIcons.For(item),1.3f,3);
        }
        private void OnTriggerStay2D(Collider2D other)
        {
            var arena=BattleArena.Instance;
            var player=other.GetComponentInParent<BattleBoat>();
            if(!available || arena==null || !arena.CanSimulate || player==null || !player.Alive)return;
            available=false;wait=respawnSeconds;picture.enabled=false;GetComponent<Collider2D>().enabled=false;
            player.GiveTestItem(item,duration);
        }
        private void Update()
        {
            if(available || !respawn || BattleArena.Instance==null || !BattleArena.Instance.CanSimulate)return;
            if((wait-=Time.deltaTime)<=0){available=true;picture.enabled=true;GetComponent<Collider2D>().enabled=true;}
        }
    }
}
