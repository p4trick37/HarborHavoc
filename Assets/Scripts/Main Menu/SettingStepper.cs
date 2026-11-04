using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A selectable settings row such as "Lives: 3". Left/right on a controller or keyboard changes
// the value; the arrow buttons beside it call Decrease/Increase for the mouse.
public abstract class SettingStepper : Selectable
{
    [SerializeField] private TMP_Text valueText;

    protected abstract void Change(int step);
    protected abstract string Label { get; }

    protected override void OnEnable()
    {
        base.OnEnable();
        Refresh();
    }

    public override void OnMove(AxisEventData eventData)
    {
        if (eventData.moveDir == MoveDirection.Left)
        {
            Decrease();
            return;
        }
        if (eventData.moveDir == MoveDirection.Right)
        {
            Increase();
            return;
        }
        base.OnMove(eventData);
    }

    public void Increase()
    {
        Change(1);
        Refresh();
    }

    public void Decrease()
    {
        Change(-1);
        Refresh();
    }

    private void Refresh()
    {
        if (valueText != null)
        {
            valueText.text = Label;
        }
    }
}
