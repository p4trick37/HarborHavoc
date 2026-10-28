using System;

namespace HarborBattle
{
    // Independent of Unity, devices and weapon effects so slot rules have one owner.
    public sealed class TimedLoadout<T> where T : class
    {
        public T Ready { get; private set; }
        public T Reserve { get; private set; }
        public T Active { get; private set; }
        public float Remaining { get; private set; }

        public void Collect(T item)
        {
            if(item==null) return;
            if(Ready==null && Active==null) Ready=item;
            else Reserve=item;
        }

        public bool TryActivate(Func<T,float> duration)
        {
            if(Active!=null || Ready==null) return false;
            Remaining=Math.Max(.1f,duration(Ready));
            Active=Ready;
            Ready=null;
            return true;
        }

        public bool Tick(float seconds)
        {
            if(Active==null) return false;
            Remaining=Math.Max(0,Remaining-Math.Max(0,seconds));
            if(Remaining>0) return false;
            Active=null;
            Ready=Reserve;
            Reserve=null;
            return true;
        }
    }
}
