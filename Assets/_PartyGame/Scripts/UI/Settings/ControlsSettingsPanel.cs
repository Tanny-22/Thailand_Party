using System.Collections.Generic;
using System.Linq;
using PartyGame.Input;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PartyGame.UI.Settings
{
    public sealed class ControlsSettingsPanel : MonoBehaviour
    {
        [SerializeField] TMP_Dropdown actionDropdown;
        [SerializeField] TMP_Text currentBindingLabel;
        [SerializeField] TMP_Text statusLabel;
        [SerializeField] Button rebindButton;
        [SerializeField] Button resetButton;
        [SerializeField] Button cancelButton;
        readonly List<InputAction> configurableActions = new();
        InputActionRebindingExtensions.RebindingOperation operation;

        void Awake()
        {
            rebindButton?.onClick.AddListener(BeginRebind); resetButton?.onClick.AddListener(ResetSelected); cancelButton?.onClick.AddListener(CancelRebind);
            actionDropdown?.onValueChanged.AddListener(_ => RefreshBinding());
        }
        void OnDestroy()
        {
            rebindButton?.onClick.RemoveListener(BeginRebind); resetButton?.onClick.RemoveListener(ResetSelected); cancelButton?.onClick.RemoveListener(CancelRebind);
            operation?.Dispose();
        }
        void OnEnable() => Populate();
        void Populate()
        {
            configurableActions.Clear();
            var asset = InputManager.Instance?.Actions; if (!asset) return;
            configurableActions.AddRange(asset.Where(a => a.bindings.Any(b => !b.isComposite && !b.isPartOfComposite)));
            actionDropdown.ClearOptions(); actionDropdown.AddOptions(configurableActions.Select(a => $"{a.actionMap.name} / {a.name}").ToList()); RefreshBinding();
        }
        InputAction Selected => configurableActions.Count == 0 ? null : configurableActions[Mathf.Clamp(actionDropdown.value, 0, configurableActions.Count - 1)];
        int BindingIndex(InputAction action) { for (var i = 0; i < action.bindings.Count; i++) if (!action.bindings[i].isComposite && !action.bindings[i].isPartOfComposite) return i; return -1; }
        void RefreshBinding() { var action = Selected; var index = action == null ? -1 : BindingIndex(action); if (currentBindingLabel) currentBindingLabel.text = index < 0 ? "Not configurable" : action.GetBindingDisplayString(index); }
        void BeginRebind()
        {
            var action = Selected; var index = action == null ? -1 : BindingIndex(action); if (index < 0 || operation != null) return;
            if (statusLabel) statusLabel.text = "Waiting for input... (Esc to cancel)";
            operation = InputManager.Instance.StartRebind(action, index, _ => { operation = null; ShowConflict(action, index); RefreshBinding(); }, () => { operation = null; if (statusLabel) statusLabel.text = "Rebind cancelled."; });
        }
        void ShowConflict(InputAction changed, int index)
        {
            var path = changed.bindings[index].effectivePath;
            var conflict = configurableActions.FirstOrDefault(a => a != changed && a.bindings.Any(b => b.effectivePath == path));
            if (statusLabel) statusLabel.text = conflict == null ? "Binding saved." : $"Warning: also used by {conflict.name}.";
        }
        void ResetSelected() { var action = Selected; if (action == null) return; action.RemoveAllBindingOverrides(); InputManager.Instance.SaveOverrides(); RefreshBinding(); if (statusLabel) statusLabel.text = "Binding reset."; }
        void CancelRebind() => operation?.Cancel();
    }
}
