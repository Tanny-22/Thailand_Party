using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PartyGame.UI.Shared
{
    public sealed class SelectionCardView : MonoBehaviour
    {
        [SerializeField] Image thumbnail;
        [SerializeField] TMP_Text title;
        [SerializeField] Toggle toggle;
        [SerializeField] Image selectedHighlight;
        string stableId;
        Action<string, bool> changed;

        void Awake() => toggle?.onValueChanged.AddListener(OnChanged);
        void OnDestroy() => toggle?.onValueChanged.RemoveListener(OnChanged);

        public void Bind(string id, string displayName, Sprite sprite, bool selected, Action<string, bool> callback)
        {
            stableId = id;
            changed = callback;
            if (title) title.text = displayName;
            if (thumbnail) { thumbnail.sprite = sprite; thumbnail.enabled = sprite; }
            if (toggle) toggle.SetIsOnWithoutNotify(selected);
            if (selectedHighlight) selectedHighlight.enabled = selected;
        }

        void OnChanged(bool value)
        {
            if (selectedHighlight) selectedHighlight.enabled = value;
            changed?.Invoke(stableId, value);
        }
    }
}
