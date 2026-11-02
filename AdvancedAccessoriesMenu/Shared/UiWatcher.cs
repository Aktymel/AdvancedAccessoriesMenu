using System;
using UnityEngine;

namespace AdvancedAccessoriesMenu
{
    // Unity calls these when a UI element is shown, hidden, faded by a CanvasGroup, resized or moved to another parent
    internal sealed class UiWatcher : MonoBehaviour
    {
        public event Action Changed;

        public static void Watch(GameObject go, Action changed)
        {
            var watcher = go.GetComponent<UiWatcher>();
            if (watcher == null) watcher = go.AddComponent<UiWatcher>();
            watcher.Changed -= changed;
            watcher.Changed += changed;
        }

        private void OnEnable() => Raise();
        private void OnDisable() => Raise();
        private void OnCanvasGroupChanged() => Raise();
        private void OnRectTransformDimensionsChange() => Raise();
        private void OnTransformParentChanged() => Raise();

        private void Raise()
        {
            Changed?.Invoke();
        }
    }
}
