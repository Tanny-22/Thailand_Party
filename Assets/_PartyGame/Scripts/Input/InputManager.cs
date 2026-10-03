using System;
using PartyGame.Core;
using PartyGame.Persistence;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PartyGame.Input
{
    public sealed class InputManager : PersistentService<InputManager>
    {
        [SerializeField] InputActionAsset actions;
        public InputActionAsset Actions => actions;
        protected override void Awake()
        {
            base.Awake();
            if (!actions) return;
            var json = SaveManager.Instance ? SaveManager.Instance.BindingOverrides : string.Empty;
            if (!string.IsNullOrWhiteSpace(json)) actions.LoadBindingOverridesFromJson(json);
        }
        void OnEnable() => actions?.Enable();
        void OnDisable() => actions?.Disable();
        public void SaveOverrides() { if (actions && SaveManager.Instance) SaveManager.Instance.BindingOverrides = actions.SaveBindingOverridesAsJson(); }
        public void ResetOverrides() { actions?.RemoveAllBindingOverrides(); SaveOverrides(); }
        public InputActionRebindingExtensions.RebindingOperation StartRebind(InputAction action, int bindingIndex, Action<string> completed, Action cancelled)
        {
            action.Disable();
            return action.PerformInteractiveRebinding(bindingIndex).WithCancelingThrough("<Keyboard>/escape")
                .OnCancel(op => { op.Dispose(); action.Enable(); cancelled?.Invoke(); })
                .OnComplete(op => { op.Dispose(); action.Enable(); SaveOverrides(); completed?.Invoke(action.GetBindingDisplayString(bindingIndex)); }).Start();
        }
    }
}
