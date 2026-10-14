using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Runs;

namespace SpireLegacy;

// Opt-in isolated bridge only. These probes preserve native disabled-control
// gates and disconnect callbacks; they never force signals or mutate snapshots.
public static class CoopTimingOracle
{
    public static async Task<object> Select(string control)
    {
        var tree=(SceneTree)Engine.GetMainLoop();
        var screen=NGame.Instance!.MainMenu!.SubmenuStack.GetSubmenuType<NCharacterSelectScreen>();
        NClickableControl button=control switch {
            "ready"=>screen.GetNode<NClickableControl>("ConfirmButton"),
            "unready"=>screen.GetNode<NClickableControl>("UnreadyButton"),
            "ironclad"=>NativeTestBridge.Descendants(screen).OfType<NCharacterSelectButton>().Single(b=>b.Character is MegaCrit.Sts2.Core.Models.Characters.Ironclad),
            "heir"=>NativeTestBridge.Descendants(screen).OfType<NCharacterSelectButton>().Single(b=>b.Character is HeirCharacter),
            _=>throw new InvalidDataException("Unknown lobby control.")
        };
        var enabled=button.IsEnabled;
        var releases=0;
        void Released(NClickableControl _) => releases++;
        button.Released+=Released;
        try {
            // Route through Godot's actual focus/disabled-control dispatch.
            // This remains separate from physical Steam Input validation.
            if(button.FocusMode!=Control.FocusModeEnum.None)button.GrabFocus();
            var focused=tree.Root.GuiGetFocusOwner()==button;
            var action=control=="unready"?MegaInput.cancel:MegaInput.select;
            Input.ParseInputEvent(new InputEventAction{Action=action,Pressed=true,Strength=1});
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            Input.ParseInputEvent(new InputEventAction{Action=action,Pressed=false});
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            return new{control,enabled,focused,releases,state=CoopOracle.State()};
        } finally {button.Released-=Released;}
    }
    public static object Disconnect()
    {
        var service=CoopBloodlines.Lobby?.NetService??RunManager.Instance.NetService;
        var connected=service.IsConnected;
        service.Disconnect(NetError.Quit,now:true);
        return new{wasConnected=connected,connected=service.IsConnected};
    }
}
