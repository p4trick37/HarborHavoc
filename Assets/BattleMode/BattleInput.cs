using UnityEngine;
using UnityEngine.InputSystem;

namespace HarborBattle
{
    // Read the same action asset used by the campaign prefab, scoped to one device.
    public static class BattleInput
    {
        public static InputActionAsset ForDevice(InputActionAsset source, InputDevice device)
        {
            if(source==null) return null;
            var actions=Object.Instantiate(source);
            actions.devices=new[] {device};
            actions.bindingMask=InputBinding.MaskByGroup("Controller");
            actions.Enable();
            return actions;
        }

        public static bool Pressed(InputActionAsset actions,string name) => actions!=null && actions.FindAction(name,false)?.WasPressedThisFrame()==true;
        public static InputActionAsset ForKeyboard(InputActionAsset source)
        {
            if(source==null || Keyboard.current==null) return null;
            var actions=Object.Instantiate(source);
            actions.devices=Mouse.current!=null ? new InputDevice[] {Keyboard.current,Mouse.current} : new InputDevice[] {Keyboard.current};
            actions.bindingMask=InputBinding.MaskByGroup("Keyboard");
            actions.Enable();
            return actions;
        }
        public static BoatCommand Read(InputActionAsset actions)
        {
            if(actions==null) return default;
            return new BoatCommand
            {
                Move=actions.FindAction("Player/Move",false)?.ReadValue<Vector2>() ?? Vector2.zero,
                Aim=actions.FindAction("Player/Look",false)?.ReadValue<Vector2>() ?? Vector2.zero,
                Fire=actions.FindAction("Player/Shoot",false)?.IsPressed()==true,
                Boost=actions.FindAction("Battle/Boost",false)?.IsPressed()==true,
                Activate=Pressed(actions,"Battle/Activate")
            };
        }
    }
}
