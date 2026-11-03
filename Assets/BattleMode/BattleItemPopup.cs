using UnityEngine;
using UnityEngine.UI;

namespace HarborBattle
{
    public class BattleItemPopup : MonoBehaviour
    {
        private BattleBoat owner;
        private Canvas canvas;
        private Image icon;
        private Text label;
        private float remaining;
        public static void Show(BattleBoat boat,PickupOption item)
        {
            var popup=boat.GetComponentInChildren<BattleItemPopup>(true);
            if(popup==null)
            {
                var obj=new GameObject("Received Item",typeof(RectTransform),typeof(Canvas));
                obj.transform.SetParent(boat.transform,false);
                obj.transform.localScale=Vector3.one*.012f;
                var rect=obj.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(270,78);
                popup=obj.AddComponent<BattleItemPopup>(); popup.owner=boat;
                popup.canvas=obj.GetComponent<Canvas>();popup.canvas.renderMode=RenderMode.WorldSpace;popup.canvas.sortingOrder=80;
                var background=obj.AddComponent<Image>();background.color=new Color(.04f,.07f,.1f,.96f);
                var picture=new GameObject("Item Icon",typeof(RectTransform),typeof(Image));picture.transform.SetParent(obj.transform,false);
                var imageRect=picture.GetComponent<RectTransform>();imageRect.sizeDelta=new Vector2(60,60);imageRect.anchoredPosition=new Vector2(-95,0);
                popup.icon=picture.GetComponent<Image>();popup.icon.preserveAspect=true;
                var title=new GameObject("Item Name",typeof(RectTransform),typeof(Text));title.transform.SetParent(obj.transform,false);
                var textRect=title.GetComponent<RectTransform>();textRect.sizeDelta=new Vector2(184,70);textRect.anchoredPosition=new Vector2(35,0);
                popup.label=title.GetComponent<Text>();popup.label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");popup.label.fontSize=19;popup.label.alignment=TextAnchor.MiddleLeft;
                background.raycastTarget=false;popup.icon.raycastTarget=false;popup.label.raycastTarget=false;
            }
            popup.icon.sprite=item.icon!=null ? item.icon : BattleItemIcons.For(item.kind);
            popup.label.text=item.Category+"\n"+Name(item.kind);
            popup.remaining=BattleArena.Instance.pickupPopupSeconds;
        }
        public static string Name(PowerUp item)
        {
            if(item==PowerUp.Shotgun360) return "360 Shotgun";
            return System.Text.RegularExpressions.Regex.Replace(item.ToString(),"([a-z])([A-Z])","$1 $2");
        }
        private void LateUpdate()
        {
            if(owner==null) return;
            if(BattleArena.Instance.CanSimulate) remaining-=Time.deltaTime;
            canvas.enabled=remaining>0 && owner.Alive && !owner.IsHidden;
            float scale=Camera.main!=null ? Mathf.Clamp(Camera.main.orthographicSize*2/Mathf.Max(1,Screen.height)*.85f,.006f,.08f) : .012f;
            transform.localScale=Vector3.one*scale;
            transform.position=owner.transform.position+new Vector3(0,2+39*scale,-2);
            transform.rotation=Quaternion.identity;
        }
    }
}
