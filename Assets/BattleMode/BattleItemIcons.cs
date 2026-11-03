using System.Collections.Generic;
using UnityEngine;

namespace HarborBattle
{
    // Readable code-drawn fallback icons. PickupOption.icon always overrides these.
    public static class BattleItemIcons
    {
        private static readonly Dictionary<PowerUp,Sprite> icons=new Dictionary<PowerUp,Sprite>();
        private static Sprite disc;
        public static Sprite Disc
        {
            get
            {
                if(disc==null)
                {
                    var pixels=new Color[64*64];
                    for(int y=0;y<64;y++) for(int x=0;x<64;x++)
                        if(Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f))<=32) pixels[y*64+x]=Color.white;
                    disc=Make(pixels);
                }
                return disc;
            }
        }
        private static Sprite Make(Color[] pixels)
        {
            var texture=new Texture2D(64,64,TextureFormat.RGBA32,false); texture.SetPixels(pixels);texture.Apply();
            texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;
            return Sprite.Create(texture,new Rect(0,0,64,64),Vector2.one*.5f,64);
        }
        public static Sprite For(PowerUp kind)
        {
            if(icons.TryGetValue(kind,out var icon) && icon!=null) return icon;
            var pixels=new Color[4096];
            Color ink=ItemCategories.For(kind)==ItemCategory.Offensive ? new Color(1,.65f,.25f) : new Color(.3f,.8f,1);
            void Dot(float x,float y,float radius)
            {
                for(int py=Mathf.Max(0,(int)(y-radius));py<Mathf.Min(64,y+radius+1);py++)
                    for(int px=Mathf.Max(0,(int)(x-radius));px<Mathf.Min(64,x+radius+1);px++)
                        if(Vector2.Distance(new Vector2(px,py),new Vector2(x,y))<=radius) pixels[py*64+px]=ink;
            }
            void Line(float x,float y,float tx,float ty,float width=2)
            {
                int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(new Vector2(x,y),new Vector2(tx,ty))*2));
                for(int i=0;i<=steps;i++) Dot(Mathf.Lerp(x,tx,i/(float)steps),Mathf.Lerp(y,ty,i/(float)steps),width);
            }
            void Ring(float x,float y,float radius)
            {for(int i=0;i<80;i++){float a=i*Mathf.PI*2/80;Dot(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius,1.5f);}}
            switch(kind)
            {
                case PowerUp.RapidFire:
                    for(int y=18;y<=46;y+=14) {Line(10,y,49,y);Line(42,y+5,51,y);Line(42,y-5,51,y);} break;
                case PowerUp.Shotgun:
                    for(int i=-2;i<=2;i++) Line(12,32,50,32+i*9);break;
                case PowerUp.Laser:
                    Line(8,32,55,32,3);Line(46,20,46,44);Line(38,24,54,40);Line(38,40,54,24);break;
                case PowerUp.FastBoat:
                    for(int x=10;x<50;x+=16) {Line(x,14,x+12,32);Line(x+12,32,x,50);}break;
                case PowerUp.Shield:
                    Line(12,49,32,55);Line(32,55,52,49);Line(52,49,48,26);Line(48,26,32,10);Line(32,10,16,26);Line(16,26,12,49);Line(32,17,32,48);break;
                case PowerUp.Shockwave:
                    Ring(32,32,8);Ring(32,32,16);Ring(32,32,25);break;
                case PowerUp.Shotgun360:
                    Ring(32,32,10);for(int i=0;i<12;i++){float a=i*Mathf.PI/6;Line(32+Mathf.Cos(a)*15,32+Mathf.Sin(a)*15,32+Mathf.Cos(a)*27,32+Mathf.Sin(a)*27);}break;
                case PowerUp.FreezeShot:
                    for(int i=0;i<6;i++){float a=i*Mathf.PI/3;float x=32+Mathf.Cos(a)*24,y=32+Mathf.Sin(a)*24;Line(32,32,x,y);Dot(x,y,3);}break;
                case PowerUp.FireShot:
                    Line(14,19,10,32);Line(10,32,24,51);Line(24,51,24,38);Line(24,38,38,57);Line(38,57,52,29);Line(52,29,46,15);Line(46,15,14,19);Line(25,19,32,37);Line(32,37,39,19);break;
                case PowerUp.HomingMissile:
                    Line(25,15,25,43);Line(25,43,32,56);Line(32,56,39,43);Line(39,43,39,15);Line(25,15,39,15);Line(25,27,14,13);Line(39,27,50,13);Line(32,13,32,5);break;
                case PowerUp.SlowShot:
                    Line(15,53,49,53);Line(15,11,49,11);Line(19,50,45,14);Line(45,50,19,14);Line(19,50,45,50);Line(19,14,45,14);break;
                case PowerUp.Star:
                    for(int i=0;i<10;i++){float a=Mathf.PI/2+i*Mathf.PI/5,b=Mathf.PI/2+(i+1)*Mathf.PI/5;float r=i%2==0?25:11,s=i%2==0?11:25;Line(32+Mathf.Cos(a)*r,32+Mathf.Sin(a)*r,32+Mathf.Cos(b)*s,32+Mathf.Sin(b)*s);}break;
                case PowerUp.SmokeScreen:
                    Dot(19,30,11);Dot(31,39,15);Dot(47,29,11);Line(12,20,53,20,3);break;
                case PowerUp.Underwater:
                    for(int x=8;x<56;x++) Dot(x,14+Mathf.Sin(x*.25f)*3,2);
                    Ring(20,31,6);Ring(39,43,9);Ring(21,54,3);break;
                default: Ring(20,22,9);Line(20,25,49,45,5);break;
            }
            icon=Make(pixels);icons[kind]=icon;return icon;
        }
    }
}
