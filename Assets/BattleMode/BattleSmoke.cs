using System.Collections.Generic;
using UnityEngine;

namespace HarborBattle
{
    // Fixed world area; rendering and labels are hidden independently of damage.
    public class BattleSmoke : MonoBehaviour
    {
        private static readonly List<BattleSmoke> areas=new List<BattleSmoke>();
        private float radius,remaining;
        private void OnEnable() {areas.Add(this);}
        private void OnDisable() {areas.Remove(this);}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAreas() {areas.Clear();}
        public static bool Contains(Vector2 point)
        {
            foreach(var area in areas)
                if(area!=null && area.isActiveAndEnabled && Vector2.Distance(point,area.transform.position)<=area.radius) return true;
            return false;
        }
        public static Vector2 ClipLine(Vector2 start,Vector2 end)
        {
            Vector2 delta=end-start; float distance=delta.magnitude;
            if(distance<.001f) return end;
            Vector2 direction=delta/distance;
            foreach(var area in areas)
            {
                if(area==null || !area.isActiveAndEnabled) continue;
                Vector2 offset=start-(Vector2)area.transform.position;
                float b=Vector2.Dot(offset,direction), c=offset.sqrMagnitude-area.radius*area.radius;
                float discriminant=b*b-c;
                if(c<=0) return start;
                if(discriminant<0) continue;
                float entry=-b-Mathf.Sqrt(discriminant);
                if(entry>=0) distance=Mathf.Min(distance,entry);
            }
            return start+direction*distance;
        }
        public static BattleSmoke Deploy(Vector2 position,float radius,float duration)
        {
            var obj=new GameObject("Smoke Screen"); obj.transform.position=position;
            var smoke=obj.AddComponent<BattleSmoke>(); smoke.radius=radius;smoke.remaining=duration;
            var renderer=obj.AddComponent<SpriteRenderer>();
            renderer.sprite=BattleItemIcons.Disc;
            renderer.color=new Color(.23f,.25f,.28f,1);
            renderer.sortingOrder=100;
            obj.transform.localScale=Vector3.one*radius*2;
            // The opaque disc guarantees concealment; imported smoke decorates it.
            var particles=BattleVfx.Spawn(BattleArena.Instance.vfx?.smoke,position,null,true);
            if(particles!=null)
            {
                particles.transform.SetParent(obj.transform,true);
                foreach(var effect in particles.GetComponentsInChildren<Renderer>()) effect.sortingOrder=101;
            }
            return smoke;
        }
        private void Update()
        {
            if(BattleArena.Instance!=null && BattleArena.Instance.CanSimulate && (remaining-=Time.deltaTime)<=0)
            {gameObject.SetActive(false);Destroy(gameObject);}
        }
    }

    public class BattleVisibility : MonoBehaviour
    {
        private readonly Dictionary<Renderer,bool> original=new Dictionary<Renderer,bool>();
        public static void Apply(Transform root,bool hidden)
        {
            var state=root.GetComponent<BattleVisibility>() ?? root.gameObject.AddComponent<BattleVisibility>();
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if(!state.original.ContainsKey(renderer)) state.original.Add(renderer,renderer.enabled);
                renderer.enabled=!hidden && state.original[renderer];
            }
        }
    }
}
