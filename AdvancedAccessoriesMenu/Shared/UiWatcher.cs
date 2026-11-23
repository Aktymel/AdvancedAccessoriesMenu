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

        // Also its root canvas, which is resized with the game window and by UI scale plugins
        public static void WatchWithCanvas(GameObject go, Action changed)
        {
            Watch(go, changed);
            var canvas = ScreenUi.RootCanvas(go.transform);
            if (canvas != null) Watch(canvas.gameObject, changed);
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
