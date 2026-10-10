using Godot;
using MegaCrit.Sts2.Core.ControllerInput;

namespace SpireLegacy;

/// <summary>Frame callback works while the modal is paused and needs no custom Godot script registration.</summary>
public class ManorInput : Control
{
    private StringName held="";
    private ulong repeatAt;
    private int direction;
    private bool paging;
    public void Poll()
    {
        if(!ManorUi.IsOpen)return;
        if(Input.IsActionJustPressed(MegaInput.cancel) || Input.IsActionJustPressed(MegaInput.pauseAndBack)) {ManorUi.GoBack();return;}
        // Godot buttons handle ui_accept; ui_select is the game's normal A/Select action.
        if(Input.IsActionJustPressed(MegaInput.select) && GetViewport().GuiGetFocusOwner() is not LineEdit) {ManorUi.Activate();return;}
        foreach(var action in new[]{MegaInput.up,MegaInput.down,MegaInput.left,MegaInput.right,MegaInput.viewDeckAndTabLeft,MegaInput.viewExhaustPileAndTabRight})
        {
            if(!Input.IsActionJustPressed(action))continue;
            if(GetViewport().GuiGetFocusOwner() is LineEdit && (action==MegaInput.left || action==MegaInput.right))continue;
            held=action;direction=action==MegaInput.up || action==MegaInput.left || action==MegaInput.viewDeckAndTabLeft ? -1 : 1;
            paging=action==MegaInput.viewDeckAndTabLeft || action==MegaInput.viewExhaustPileAndTabRight;
            Move();repeatAt=Time.GetTicksMsec()+400;return;
        }
        if(held.IsEmpty || !Input.IsActionPressed(held))return;
        if(Time.GetTicksMsec()<repeatAt)return;
        Move();repeatAt=Time.GetTicksMsec()+130;
    }
    private void Move() {if(paging)ManorUi.Page(direction);else ManorUi.Navigate(direction);}
}
