using System;
using HarborBattle;
static void Check(bool condition,string message) { if(!condition) throw new Exception(message); }
var slots=new TimedLoadout<string>();
slots.Collect("RapidFire"); slots.Collect("Shotgun"); slots.Collect("Shield");
Check(slots.Ready=="RapidFire" && slots.Reserve=="Shield","Full inventory must replace reserve only");
Check(slots.TryActivate(_=>5),"Ready item must activate");
Check(!slots.TryActivate(_=>5),"Active item must lock out reserve");
slots.Collect("Freeze");
Check(slots.Active=="RapidFire" && slots.Reserve=="Freeze","Collection must not interrupt active item");
Check(!slots.Tick(4) && slots.Remaining==1,"Item must last its full duration without firing");
Check(slots.Tick(1),"Item must expire at five seconds");
Check(slots.Active==null && slots.Ready=="Freeze" && slots.Reserve==null,"Reserve must promote without automatic activation");
slots.Tick(20); Check(slots.Ready=="Freeze","Ready item must not expire while waiting");
Check(slots.TryActivate(_=>2),"Promoted item must activate on fresh input");
slots.Tick(-10); Check(slots.Remaining==2,"Negative elapsed time must not extend an item");
Check(slots.Tick(2) && slots.Ready==null && slots.Active==null,"Custom duration and empty expiry must work");
slots.Collect("Shotgun"); slots.TryActivate(_=>0); Check(slots.Remaining==.1f,"Invalid duration must be clamped");
Console.WriteLine("PASS: reserve replacement, no combination, timed expiry, manual promotion, custom duration, duration guard.");
