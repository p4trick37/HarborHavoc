using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HarborBattle
{
    // The battle HUD on a uGUI canvas so it can use the menu font: the game number and wins
    // in the top middle, a corner panel per player, the countdown, and "P1" tags over boats.
    public class BattleHud : MonoBehaviour
    {
        private class PlayerPanel
        {
            public GameObject Root;
            public TMP_Text Header, Status, Items, Tag;
            public RectTransform Fill;
            public Image FillImage;
        }

        private RectTransform canvasRect;
        private TMP_Text top, center, help;
        private readonly PlayerPanel[] panels=new PlayerPanel[BattleLobby.MaxPlayers];
        private TMP_FontAsset font;

        private void Start()
        {
            font=BattleArena.Instance.menuFont;
            var root=new GameObject("Battle HUD",typeof(Canvas),typeof(CanvasScaler));
            root.transform.SetParent(transform,false);
            var canvas=root.GetComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder=50; // Under the end screen menu.
            var scaler=root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);
            scaler.matchWidthOrHeight=.5f;
            canvasRect=(RectTransform)root.transform;

            top=Text("Series",canvasRect,new Vector2(.5f,1),new Vector2(0,-20),new Vector2(1000,130),46);
            top.alignment=TextAlignmentOptions.Top;
            center=Text("Center",canvasRect,new Vector2(.5f,.5f),new Vector2(0,120),new Vector2(1200,200),110);
            help=Text("Help",canvasRect,new Vector2(.5f,.5f),new Vector2(0,-120),new Vector2(1300,260),28);
            for(int i=0;i<panels.Length;i++) panels[i]=Panel(i);
        }

        private void LateUpdate()
        {
            var arena=BattleArena.Instance;
            if(arena==null || canvasRect==null) return;
            bool live=arena.Running || arena.CountingDown;

            // Top middle: just the game number and each player's wins, during a series.
            if(BattleLobby.IsSeries && live && arena.Result==null)
            {
                string wins="";
                foreach(var boat in arena.Boats)
                    wins+="<color=#"+ColorUtility.ToHtmlStringRGB(BattleLobby.PlayerColor(boat.Index))+">P"+(boat.Index+1)+"  "+BattleLobby.Wins[boat.Index]+"</color>      ";
                top.text="Game "+(BattleLobby.GamesPlayed+1)+"\n<size=40>"+wins.TrimEnd()+"</size>";
            }
            else top.text="";

            center.text=arena.CountingDown ? Mathf.CeilToInt(arena.Countdown).ToString()
                : arena.Running && !arena.CanSimulate ? "<size=50>Reconnect controller to resume</size>"
                : arena.Running && arena.SoloTest ? "<size=40>SOLO TEST</size>"
                : "";
            // Only shown in the arena's own join lobby (when the scene is opened directly).
            help.text=!live && arena.Result==null
                ? arena.JoinedCount+" / 4 joined\nSTART: join, press again to start (one player = solo test)\nENTER: add keyboard opponent / start\n\n"
                  +"P1: WASD / mouse / click / E offense / Shift defense / F mine\nP2: arrows / Right Ctrl fire / Enter offense / Right Shift defense / slash mine\nController: sticks / RT fire / X offense / LT defense / RB mine"
                : "";

            for(int i=0;i<panels.Length;i++)
            {
                var panel=panels[i];
                var boat=i<arena.Boats.Count ? arena.Boats[i] : null;
                panel.Root.SetActive(boat!=null);
                panel.Tag.gameObject.SetActive(boat!=null && boat.Alive && !boat.IsHidden);
                if(boat==null) continue;
                panel.Header.text="P"+(i+1)+"   Lives: "+boat.Lives;
                panel.Status.text=boat.Eliminated ? "ELIMINATED" : !boat.Alive ? "Respawning..."
                    : "Hull: "+boat.Health+"    Mines: "+boat.Mines+(arena.IsInFog(boat.transform.position) ? "    FOG!" : "");
                panel.Items.text=boat.PowerLabel;
                panel.Fill.anchorMax=new Vector2(Mathf.Clamp01(boat.Health/100f),1);
                if(panel.Tag.gameObject.activeSelf)
                {
                    Vector2 screen=arena.ArenaCamera.WorldToScreenPoint(boat.transform.position);
                    panel.Tag.rectTransform.position=screen+Vector2.up*55*canvasRect.localScale.y;
                }
            }
        }

        private PlayerPanel Panel(int index)
        {
            bool left=index%2==0, upper=index<2;
            var corner=new Vector2(left ? 0 : 1,upper ? 1 : 0);
            var color=BattleLobby.PlayerColor(index);
            var panel=new PlayerPanel();
            var rect=Box("P"+(index+1)+" Panel",canvasRect,new Color(0,0,0,.55f));
            rect.anchorMin=rect.anchorMax=rect.pivot=corner;
            rect.anchoredPosition=new Vector2(left ? 16 : -16,upper ? -16 : 16);
            rect.sizeDelta=new Vector2(420,190);
            panel.Root=rect.gameObject;

            panel.Header=Text("Header",rect,new Vector2(0,1),new Vector2(16,-8),new Vector2(390,44),34);
            panel.Header.alignment=TextAlignmentOptions.TopLeft; panel.Header.color=color;
            panel.Status=Text("Status",rect,new Vector2(0,1),new Vector2(16,-50),new Vector2(390,34),26);
            panel.Status.alignment=TextAlignmentOptions.TopLeft;

            var bar=Box("Health Bar",rect,new Color(.08f,.1f,.12f));
            bar.anchorMin=bar.anchorMax=bar.pivot=new Vector2(0,1);
            bar.anchoredPosition=new Vector2(16,-88); bar.sizeDelta=new Vector2(388,18);
            panel.Fill=Box("Fill",bar,color);
            panel.Fill.anchorMin=Vector2.zero; panel.Fill.anchorMax=Vector2.one;
            panel.Fill.offsetMin=panel.Fill.offsetMax=Vector2.zero;
            panel.Fill.pivot=new Vector2(0,.5f);

            panel.Items=Text("Items",rect,new Vector2(0,1),new Vector2(16,-114),new Vector2(390,70),24);
            panel.Items.alignment=TextAlignmentOptions.TopLeft;

            panel.Tag=Text("P"+(index+1)+" Tag",canvasRect,new Vector2(.5f,.5f),Vector2.zero,new Vector2(120,50),34);
            panel.Tag.text="P"+(index+1); panel.Tag.color=color;
            panel.Tag.rectTransform.anchorMin=panel.Tag.rectTransform.anchorMax=Vector2.zero;
            return panel;
        }

        private RectTransform Box(string name,Transform parent,Color color)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(Image));
            var rect=obj.GetComponent<RectTransform>();
            rect.SetParent(parent,false);
            var image=obj.GetComponent<Image>(); image.color=color; image.raycastTarget=false;
            return rect;
        }

        private TMP_Text Text(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size,float fontSize)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));
            var rect=obj.GetComponent<RectTransform>();
            rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=anchor;
            rect.pivot=new Vector2(anchor.x==0 ? 0 : anchor.x==1 ? 1 : .5f,anchor.y==0 ? 0 : anchor.y==1 ? 1 : .5f);
            rect.anchoredPosition=position; rect.sizeDelta=size;
            var text=obj.GetComponent<TextMeshProUGUI>();
            if(font!=null) text.font=font;
            text.fontSize=fontSize; text.color=Color.white;
            text.alignment=TextAlignmentOptions.Center;
            text.raycastTarget=false; text.richText=true;
            // Dark outline keeps text readable over the water.
            text.outlineWidth=.18f; text.outlineColor=new Color32(0,0,0,200);
            return text;
        }
    }
}
