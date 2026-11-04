using HarborBattle;

// "Match: First to 3" on the battle settings screen; cycles through BattleLobby.Formats.
public class MatchFormatStepper : SettingStepper
{
    protected override void Change(int step)
    {
        BattleLobby.FormatIndex += step;
    }

    protected override string Label => "Match: " + BattleLobby.Format.Label;
}
