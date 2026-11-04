using HarborBattle;

// "Lives: 3" on the battle settings screen.
public class LivesStepper : SettingStepper
{
    protected override void Change(int step)
    {
        BattleLobby.Lives += step;
    }

    protected override string Label => "Lives: " + BattleLobby.Lives;
}
