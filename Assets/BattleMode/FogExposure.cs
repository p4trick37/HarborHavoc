using System;

namespace HarborBattle
{
    // Keep fractional damage between physics ticks; reset exposure on re-entry.
    public sealed class FogExposure
    {
        private double damage;
        public static bool Outside(float x,float y,float halfWidth,float halfHeight) => Math.Abs(x)>halfWidth || Math.Abs(y)>halfHeight;
        public int Tick(bool outside,float seconds,float damagePerSecond)
        {
            if(!outside) { damage=0; return 0; }
            damage+=Math.Max(0,seconds)*Math.Max(0,damagePerSecond);
            int whole=(int)Math.Floor(damage+0.000001);
            damage-=whole;
            return whole;
        }
    }
}
