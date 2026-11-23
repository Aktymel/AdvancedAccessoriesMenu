using KKAPI.Utilities;
using UnityEngine;

namespace AdvancedAccessoriesMenu
{
    internal static class ScreenUi
    {
        // UI sizes are made for 1080p. Screen.height can't be used: the IMGUI HiDPI patcher fakes it.
        public static float Scale(Canvas canvas)
        {
            return canvas == null ? 1 : canvas.pixelRect.height / 1080f;
        }

        // Screen pixels to IMGUI coordinates, which GUI.matrix may scale (IMGUI HiDPI patcher)
        public static Rect ToGui(Rect rect)
        {
            var m = GUI.matrix;
            return new Rect(rect.x / m.m00, rect.y / m.m11, rect.width / m.m00, rect.height / m.m11);
        }

        // The button that switches between the game's slot list and the Accessories menu; rect in screen pixels
        public static bool DrawToggleButton(Rect rect, bool advancedOpen)
        {
            rect = ToGui(rect);
            GuiTheme.Begin();
            var clicked = GUI.Button(rect, advancedOpen ? "Original" : "Advanced");
            GuiTheme.End();
            IMGUIUtils.EatInputInRect(rect);
            return clicked;
        }

        // Maker hides its menus by CanvasGroup alpha, so activeInHierarchy alone is not enough
        public static bool IsShown(Component component)
        {
            if (component == null || !component.gameObject.activeInHierarchy) return false;
            foreach (var group in component.GetComponentsInParent<CanvasGroup>())
            {
                if (group.alpha <= 0.01f) return false;
                if (group.ignoreParentGroups) break;
            }
            return true;
        }

        private static readonly Vector3[] Corners = new Vector3[4];

        // Screen area of a UI element in screen pixels, top left origin
        public static Rect ScreenRect(RectTransform rt)
        {
            return ScreenRect(rt, RootCanvas(rt));
        }

        public static Rect ScreenRect(RectTransform rt, Canvas canvas)
        {
            var cam = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var height = canvas == null ? Screen.height : canvas.pixelRect.height;
            rt.GetWorldCorners(Corners);
            var min = RectTransformUtility.WorldToScreenPoint(cam, Corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(cam, Corners[2]);
            return new Rect(min.x, height - max.y, max.x - min.x, max.y - min.y);
        }

        public static Canvas RootCanvas(Transform t)
        {
            var canvas = t.GetComponentInParent<Canvas>();
            return canvas == null ? null : canvas.rootCanvas;
        }

        public static void Hide(CanvasGroup group, bool hidden)
        {
            group.alpha = hidden ? 0 : 1;
            group.interactable = !hidden;
            group.blocksRaycasts = !hidden;
        }

        // Also finds components on maker pages that are inactive at the moment
        public static T FindInScene<T>() where T : Component
        {
            foreach (var component in Resources.FindObjectsOfTypeAll<T>())
                if (component.gameObject.scene.IsValid()) return component;
            return null;
        }

        public static CanvasGroup GetOrAddCanvasGroup(GameObject go)
        {
            var group = go.GetComponent<CanvasGroup>();
            return group != null ? group : go.AddComponent<CanvasGroup>();
        }
    }
}
