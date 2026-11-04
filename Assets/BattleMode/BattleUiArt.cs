using System.Collections.Generic;
using UnityEngine;

namespace HarborBattle
{
    // Rounded, code-native UI art; nine-sliced cards keep their corner radius.
    public static class BattleUiArt
    {
        private static Sprite rounded,keycap,heart,sloop,solid;
        public static Sprite Rounded => rounded!=null?rounded:rounded=Card(false);
        public static Sprite Keycap => keycap!=null?keycap:keycap=Card(true);
        public static Sprite Solid => solid!=null?solid:solid=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,100);
        public static Sprite Heart => heart!=null?heart:heart=DrawShipOrHeart(false);
        public static Sprite Sloop => sloop!=null?sloop:sloop=DrawShipOrHeart(true);
        private static Sprite Card(bool frame)
        {
            const int size=64;var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=Mathf.Max(Mathf.Abs(x-31.5f)-15.5f,0),dy=Mathf.Max(Mathf.Abs(y-31.5f)-15.5f,0);
                float edge=Mathf.Sqrt(dx*dx+dy*dy);
                if(edge>16)continue;
                Color color=frame?(x<3||x>60||y<3||y>60||edge>13?new Color(.72f,.83f,.88f):new Color(.07f,.13f,.17f)):Color.white;
                color.a=Mathf.Clamp01(16-edge);pixels[y*size+x]=color;
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);texture.SetPixels(pixels);texture.Apply();texture.filterMode=FilterMode.Bilinear;
            return Sprite.Create(texture,new Rect(0,0,size,size),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(18,18,18,18));
        }
        private static Sprite DrawShipOrHeart(bool ship)
        {
            var polygons=new List<List<Vector2>>();
            List<Vector2> Path(Vector2 start,params Vector2[] controls)
            {
                var points=new List<Vector2>{start};Vector2 a=start;
                for(int j=0;j<controls.Length;j+=3)
                {
                    for(int i=1;i<=14;i++)
                    {
                        float t=i/14f,u=1-t;
                        points.Add(u*u*u*a+3*u*u*t*controls[j]+3*u*t*t*controls[j+1]+t*t*t*controls[j+2]);
                    }
                    a=controls[j+2];
                }
                polygons.Add(points);return points;
            }
            if(!ship)
            {
                Path(new Vector2(128,25),new Vector2(105,25),new Vector2(25,99),new Vector2(18,159),
                    new Vector2(8,222),new Vector2(77,251),new Vector2(128,204),
                    new Vector2(181,251),new Vector2(248,222),new Vector2(238,159),
                    new Vector2(231,99),new Vector2(151,25),new Vector2(128,25));
            }
            else
            {
                // Deep curved hull, raised stern, bowsprit, single mast and billowing sails.
                Path(new Vector2(20,109),new Vector2(72,93),new Vector2(156,88),new Vector2(224,119),
                    new Vector2(217,81),new Vector2(201,48),new Vector2(166,36),
                    new Vector2(108,18),new Vector2(47,34),new Vector2(30,65),
                    new Vector2(24,82),new Vector2(22,98),new Vector2(20,109));
                Path(new Vector2(121,205),new Vector2(149,213),new Vector2(174,224),new Vector2(194,199),
                    new Vector2(181,168),new Vector2(187,137),new Vector2(204,116),
                    new Vector2(174,123),new Vector2(146,114),new Vector2(121,113),
                    new Vector2(132,143),new Vector2(130,178),new Vector2(121,205));
                Path(new Vector2(102,211),new Vector2(88,181),new Vector2(64,153),new Vector2(43,132),
                    new Vector2(59,122),new Vector2(83,121),new Vector2(103,123),
                    new Vector2(98,151),new Vector2(98,186),new Vector2(102,211));
                Path(new Vector2(116,233),new Vector2(132,244),new Vector2(143,226),new Vector2(160,237),
                    new Vector2(160,230),new Vector2(159,222),new Vector2(158,217),
                    new Vector2(143,208),new Vector2(131,229),new Vector2(116,220));
            }
            bool Inside(float x,float y,List<Vector2> path)
            {
                bool inside=false;
                for(int i=0,j=path.Count-1;i<path.Count;j=i++)
                    if((path[i].y>y)!=(path[j].y>y) && x<(path[j].x-path[i].x)*(y-path[i].y)/(path[j].y-path[i].y)+path[i].x)inside=!inside;
                return inside;
            }
            bool Stroke(Vector2 point,Vector2 a,Vector2 b,float radius)
            {
                var offset=b-a;float t=Mathf.Clamp01(Vector2.Dot(point-a,offset)/offset.sqrMagnitude);
                return Vector2.Distance(point,a+offset*t)<=radius;
            }
            var pixels=new Color[256*256];
            for(int y=0;y<256;y++)for(int x=0;x<256;x++)
            {
                bool fill=false;foreach(var polygon in polygons)if(Inside(x,y,polygon)){fill=true;break;}
                if(ship)
                {
                    var p=new Vector2(x,y);
                    fill|=Stroke(p,new Vector2(112,91),new Vector2(112,234),4)||Stroke(p,new Vector2(206,113),new Vector2(245,132),3);
                    // Negative-space skull and crossbones in the broad mainsail.
                    bool emblem=Vector2.Distance(p,new Vector2(160,166))<9 || (x>155&&x<165&&y>152&&y<160)
                        || Stroke(p,new Vector2(146,148),new Vector2(176,145),2)
                        || Stroke(p,new Vector2(148,142),new Vector2(174,151),2);
                    if(emblem)fill=false;
                }
                if(fill)pixels[y*256+x]=Color.white;
            }
            var texture=new Texture2D(256,256,TextureFormat.RGBA32,false);texture.SetPixels(pixels);texture.Apply();texture.filterMode=FilterMode.Bilinear;
            return Sprite.Create(texture,new Rect(0,0,256,256),Vector2.one*.5f,100);
        }
    }
}
