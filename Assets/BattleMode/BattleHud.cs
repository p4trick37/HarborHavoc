using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HarborBattle
{
    public class BattleHud : MonoBehaviour
    {
        private BattleArena arena;
        private Canvas canvas;
        private Font font;
        private Sprite heart,boat,xButton,squareButton,keycap;
        private Text title,large,subtitle;
        private Image banner;
        private readonly PlayerPanel[] players=new PlayerPanel[4];
        private bool wasCounting;
        private float goUntil;
        private readonly Color[] colors={Color.cyan,new Color(1,.45f,.25f),Color.yellow,Color.magenta};
        private class ItemPanel {public Image icon,shade,button;public Text seconds,key;public string glyph;}
        private class PlayerPanel {public GameObject root;public Image liquid;public Text percent,name,world;public ItemPanel offense,defense;public float health=1;}

        public void Initialize(BattleArena owner)
        {
            arena=owner;font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            heart=BattleUiArt.Heart;boat=BattleUiArt.Sloop;xButton=Shape(2);squareButton=Shape(3);keycap=BattleUiArt.Keycap;
            var obj=new GameObject("Battle HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));obj.transform.SetParent(transform,false);
            canvas=obj.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=200;
            var scaler=obj.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            for(int i=0;i<4;i++) players[i]=BuildPlayer(i,obj.transform);
            banner=Picture("Match Banner",obj.transform,400,260,1120,560,null,new Color(.015f,.04f,.07f,.78f));
            banner.pixelsPerUnitMultiplier=.5f;
            title=Words("Match Status",obj.transform,360,20,1200,72,34,TextAnchor.MiddleCenter);
            large=Words("Countdown and Result",obj.transform,360,300,1200,300,112,TextAnchor.MiddleCenter);
            subtitle=Words("Match Instructions",obj.transform,430,620,1060,170,34,TextAnchor.MiddleCenter);
        }
        private PlayerPanel BuildPlayer(int index,Transform parent)
        {
            bool right=index%2==1,top=index<2;
            var root=new GameObject("Player "+(index+1)+" Heart HUD",typeof(RectTransform));root.transform.SetParent(parent,false);
            var rect=root.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(right?1:0,top?1:0);rect.pivot=rect.anchorMin;rect.sizeDelta=new Vector2(240,372);rect.anchoredPosition=new Vector2(right?-22:22,top?-16:16);
            var panel=new PlayerPanel {root=root};
            panel.name=Words("Player",root.transform,0,0,240,34,27,TextAnchor.MiddleCenter);panel.name.color=colors[index];
            Picture("Heart shadow",root.transform,7,38,230,200,heart,new Color(0,0,0,.8f));
            Picture("Player-colored heart rim",root.transform,3,31,234,204,heart,colors[index]);
            Picture("Ocean blue empty health",root.transform,10,37,220,192,heart,new Color(.02f,.3f,.47f));
            panel.liquid=Picture("Red health liquid",root.transform,10,37,220,192,heart,new Color(.87f,.035f,.075f));
            panel.liquid.type=Image.Type.Filled;panel.liquid.fillMethod=Image.FillMethod.Vertical;panel.liquid.fillOrigin=(int)Image.OriginVertical.Bottom;
            Picture("Pirate sloop silhouette",root.transform,38,68,164,130,boat,new Color(.015f,.025f,.04f,.95f));
            panel.percent=Words("Health percentage",root.transform,54,145,132,48,42,TextAnchor.MiddleCenter);
            panel.offense=BuildItem(root.transform,8);panel.defense=BuildItem(root.transform,132);
            panel.world=Words("Player world label",parent,0,0,130,45,30,TextAnchor.MiddleCenter);
            panel.world.color=colors[index];return panel;
        }
        private ItemPanel BuildItem(Transform parent,float x)
        {
            var result=new ItemPanel();
            result.button=Picture("Activation button",parent,x,234,100,36,keycap,Color.white);
            result.button.type=Image.Type.Sliced;
            result.key=Words("Button label",parent,x,234,100,36,24,TextAnchor.MiddleCenter);
            Picture("Rounded item box rim",parent,x-2,276,104,92,null,new Color(.6f,.75f,.82f));
            var box=Picture("Rounded item box",parent,x,278,100,88,null,new Color(.035f,.085f,.115f,.97f));
            box.gameObject.AddComponent<Mask>().showMaskGraphic=true;
            result.icon=Picture("Held item",box.transform,12,4,76,76,null,Color.white);result.icon.preserveAspect=true;
            result.shade=Picture("Time used - top to bottom",box.transform,0,0,100,88,BattleUiArt.Solid,new Color(.22f,.24f,.27f,.84f));
            result.shade.type=Image.Type.Filled;result.shade.fillMethod=Image.FillMethod.Vertical;result.shade.fillOrigin=(int)Image.OriginVertical.Top;
            result.seconds=Words("Seconds remaining",box.transform,0,32,100,52,34,TextAnchor.MiddleCenter);
            return result;
        }
        private static RectTransform Position(GameObject obj,Transform parent,float x,float y,float width,float height)
        {
            obj.transform.SetParent(parent,false);var rect=obj.GetComponent<RectTransform>();
            rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.sizeDelta=new Vector2(width,height);rect.anchoredPosition=new Vector2(x,-y);return rect;
        }
        private Image Picture(string name,Transform parent,float x,float y,float width,float height,Sprite sprite,Color color)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(Image));Position(obj,parent,x,y,width,height);
            var image=obj.GetComponent<Image>();image.sprite=sprite!=null?sprite:BattleUiArt.Rounded;image.type=sprite==null?Image.Type.Sliced:Image.Type.Simple;image.color=color;image.raycastTarget=false;return image;
        }
        private Text Words(string name,Transform parent,float x,float y,float width,float height,int size,TextAnchor alignment)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(Text),typeof(Outline));Position(obj,parent,x,y,width,height);
            var text=obj.GetComponent<Text>();text.font=font;text.fontSize=size;text.alignment=alignment;text.color=Color.white;text.raycastTarget=false;
            text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;
            var outline=obj.GetComponent<Outline>();outline.effectColor=new Color(0,0,0,.95f);outline.effectDistance=new Vector2(2,-2);return text;
        }
        private void Update()
        {
            if(arena==null || canvas==null)return;
            for(int i=0;i<4;i++)
            {
                var panel=players[i];bool exists=i<arena.Boats.Count && arena.Boats[i]!=null;
                panel.root.SetActive(exists);panel.world.gameObject.SetActive(false);if(!exists)continue;
                var player=arena.Boats[i];float target=Mathf.Clamp01(player.Health/100f);
                panel.health=Mathf.MoveTowards(panel.health,target,Time.unscaledDeltaTime*1.5f);panel.liquid.fillAmount=panel.health;
                panel.percent.text=Mathf.RoundToInt(target*100)+"%";
                panel.name.text="P"+(i+1)+(player.Alive?(arena.IsInFog(player.transform.position)?"  FOG!":""):"  OUT");
                UpdateItem(panel.offense,player,ItemCategory.Offensive);UpdateItem(panel.defense,player,ItemCategory.Defensive);
                if(player.Alive && !player.IsHidden && Camera.main!=null)
                {
                    Vector3 screen=Camera.main.WorldToScreenPoint(player.transform.position+Vector3.up*1.9f);
                    if(screen.z>0)
                    {
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.GetComponent<RectTransform>(),screen,null,out var local);
                        var labelRect=panel.world.rectTransform;labelRect.anchorMin=labelRect.anchorMax=Vector2.one*.5f;labelRect.pivot=Vector2.one*.5f;labelRect.anchoredPosition=local;
                        panel.world.text="P"+(i+1);panel.world.gameObject.SetActive(true);
                    }
                }
            }
            if(wasCounting && !arena.CountingDown && arena.Running)goUntil=Time.unscaledTime+.65f;
            wasCounting=arena.CountingDown;banner.gameObject.SetActive(!arena.Running);
            title.text=arena.Running?(arena.CanSimulate?(arena.SoloTest?"SOLO TEST":"LAST BOAT AFLOAT"):"CONTROLLER DISCONNECTED"):"BATTLE MODE";
            large.fontSize=arena.CountingDown?144:80;
            if(arena.RoundResult!=null) {large.text=arena.RoundResult.ToUpperInvariant();subtitle.text="Press START or ENTER for a new match";}
            else if(arena.CountingDown) {large.text=Mathf.Max(1,Mathf.CeilToInt(arena.CountdownRemaining)).ToString();subtitle.text="GET READY";}
            else if(arena.Running)
            {
                large.text=!arena.CanSimulate?"RECONNECT CONTROLLER":Time.unscaledTime<goUntil?"GO!":"";subtitle.text="";
            }
            else
            {
                large.fontSize=58;large.text=arena.Boats.Count+" / 4 PLAYERS\nREADY TO SAIL?";
                subtitle.text="START to join · START again to play\nOne player starts a solo test\nENTER adds a keyboard opponent";
            }
        }
        private void UpdateItem(ItemPanel panel,BattleBoat player,ItemCategory category)
        {
            var item=player.HeldItem(category);bool active=player.ItemActive(category);
            panel.icon.gameObject.SetActive(item!=null);panel.icon.sprite=item==null?null:item.icon!=null?item.icon:BattleItemIcons.For(item.kind);
            panel.shade.fillAmount=active?1-player.ItemTimeFraction(category):0;
            panel.seconds.text=active?player.ItemSeconds(category).ToString("0.0")+"s":item==null?"—":"";
            var input=player.GetComponent<PlayerInput>();bool controller=input!=null && input.enabled;bool ps=false;
            if(controller)foreach(var device in input.devices)
            {
                string identity=(device.layout+" "+device.description.manufacturer+" "+device.description.product).ToLowerInvariant();
                if(identity.Contains("dualshock") || identity.Contains("dualsense") || identity.Contains("sony"))ps=true;
            }
            string key=controller?(category==ItemCategory.Offensive?(ps?"square":"X"):(ps?"L2":"LT")):
                category==ItemCategory.Offensive?(player.KeyboardActions!=null?"E":"ENTER"):(player.KeyboardActions!=null?"SHIFT":"R SHIFT");
            if(panel.glyph==key)return;panel.glyph=key;
            panel.button.sprite=key=="square"?squareButton:key=="X"?xButton:keycap;
            panel.button.type=key=="square" || key=="X"?Image.Type.Simple:Image.Type.Sliced;
            panel.button.preserveAspect=true;
            panel.key.text=key=="square" || key=="X"?"":key;
        }
        private static Sprite Shape(int kind)
        {
            const int size=256;var pixels=new Color[size*size];
            bool Polygon(float x,float y,Vector2[] points)
            {
                bool inside=false;for(int i=0,j=points.Length-1;i<points.Length;j=i++)
                    if((points[i].y>y)!=(points[j].y>y) && x<(points[j].x-points[i].x)*(y-points[i].y)/(points[j].y-points[i].y)+points[i].x)inside=!inside;
                return inside;
            }
            var hull=new[]{new Vector2(22,83),new Vector2(236,83),new Vector2(203,37),new Vector2(57,37)};
            var sailA=new[]{new Vector2(130,99),new Vector2(130,221),new Vector2(220,99)};
            var sailB=new[]{new Vector2(112,102),new Vector2(112,200),new Vector2(42,102)};
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                bool fill=false;Color color=Color.white;
                if(kind==0){float a=x/255f*2.6f-1.3f,b=y/255f*2.6f-1.25f,q=a*a+b*b-1;fill=q*q*q-a*a*b*b*b<=0;}
                else if(kind==1)fill=Polygon(x,y,hull)||Polygon(x,y,sailA)||Polygon(x,y,sailB)||(x>119&&x<128&&y>75&&y<230);
                else if(kind==4){fill=x>3&&x<253&&y>12&&y<244;color=x<10||x>245||y<22||y>234?new Color(.8f,.87f,.93f):new Color(.06f,.1f,.14f);}
                else
                {
                    float distance=Vector2.Distance(new Vector2(x,y),new Vector2(128,128));fill=distance<122;color=distance>113?new Color(.65f,.75f,.85f):new Color(.05f,.08f,.12f);
                    if(kind==2 && x>63&&x<193&&y>63&&y<193 && (Mathf.Abs(x-y)<12||Mathf.Abs(x+y-256)<12))color=new Color(.25f,.65f,1);
                    if(kind==3 && x>72&&x<184&&y>72&&y<184&&(x<82||x>174||y<82||y>174))color=new Color(1,.45f,.75f);
                }
                if(fill)pixels[y*size+x]=color;
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);texture.SetPixels(pixels);texture.Apply();texture.filterMode=FilterMode.Bilinear;
            return Sprite.Create(texture,new Rect(0,0,size,size),Vector2.one*.5f,100);
        }
    }
}
