using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace CoderScripts.AnimationPatches
{
    public class AnimationWindowPatches
    {
        private static readonly Type AnimationWindowClipPopupType = AccessTools.TypeByName("UnityEditor.AnimationWindowClipPopup");
        private static readonly Type AnimationWindowUtilityType = AccessTools.TypeByName("UnityEditorInternal.AnimationWindowUtility");

        private static readonly MethodInfo CreateNewClipMethod = AnimationWindowUtilityType.GetMethod("CreateNewClip",
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo AddClipToAnimationPlayerComponentMethod = AnimationWindowUtilityType
            .GetMethod("AddClipToAnimationPlayerComponent",
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);

        [HarmonyPatch]
        class PatchAnimationClipPopup
        {
            [HarmonyTargetMethod]
            static MethodBase TargetMethod() => AccessTools.Method(AnimationWindowClipPopupType, "DoClipPopup");

            [HarmonyPrefix]
            static bool DoClipPopupPrefix(ref AnimationClip __result, object __instance, AnimationClip clip, GUIStyle style)
            {
                var stateField = Traverse.Create(__instance).Field("state");
                var rootGameObjectName = stateField.Property("selection")
                    .Property("rootGameObject")
                    .Property<string>("Name").Value;
                var activeAnimationPlayer = stateField.Property<Component>("activeAnimationPlayer").Value;
                var activeAnimationClipProp = stateField.Property<AnimationClip>("activeAnimationClip");

                Rect controlRect = EditorGUILayout.GetControlRect(false, 18f, style);
                Event current = Event.current;

                switch (current.type)
                {
                    case EventType.Repaint:
                        HandleRepaint(clip, style, controlRect);
                        break;

                    case EventType.MouseDown:
                        HandleMouseDown(__instance, clip, controlRect, current, activeAnimationClipProp);
                        break;
                }

                HandleCreateNewClipButton(rootGameObjectName, activeAnimationPlayer, ref __result);

                return false;
            }

            private static void HandleRepaint(AnimationClip clip, GUIStyle style, Rect controlRect)
            {
                Font originalFont = style.font;
                if (originalFont != null && originalFont == EditorStyles.miniFont)
                {
                    style.font = EditorStyles.miniBoldFont;
                }

                GUIContent content = new GUIContent();
                if (clip != null)
                {
                    content.text = clip.name;
                    content.tooltip = AssetDatabase.GetAssetPath(clip);
                }

                style.Draw(controlRect, content, false, false, false, false);
                style.font = originalFont;
            }

            private static void HandleMouseDown(object instance, AnimationClip clip, Rect controlRect, Event current, Traverse<AnimationClip> activeAnimationClipProp)
            {
                if (current.button == 0 && controlRect.Contains(current.mousePosition))
                {
                    AnimationClip[] clips = Traverse.Create(instance).Method("GetOrderedClipList").GetValue<AnimationClip[]>();
                    var dropdown = new AnimationClipListSearch(clips, clip, animationClip =>
                    {
                        activeAnimationClipProp.Value = animationClip;
                    });
                    dropdown.Show(controlRect, 200);
                    dropdown.Update();
                    current.Use();
                }
            }

            private static void HandleCreateNewClipButton(string rootGameObjectName, Component activeAnimationPlayer, ref AnimationClip result)
            {
                if (GUILayout.Button(EditorGUIUtility.IconContent("d_Toolbar Plus", "|Create New Clip"), EditorStyles.toolbarButton))
                {
                    AnimationClip animationClip = CreateNewClipMethod.Invoke(null, new[] { rootGameObjectName }) as AnimationClip;
                    if (animationClip != null)
                    {
                        AddClipToAnimationPlayerComponentMethod.Invoke(null, new object[] { activeAnimationPlayer, animationClip });
                        result = animationClip;
                    }
                }
            }
        }
    }

    public class AnimationClipListSearch : AdvancedDropdown
    {
        private readonly AnimationClip[] clips;
        private readonly AnimationClip selectedClip;
        private readonly Action<AnimationClip> onClipSelected;
        private AdvancedDropdownItem root;

        public AnimationClipListSearch(AnimationClip[] clips, AnimationClip selected, Action<AnimationClip> onClipSelected) : base(new AdvancedDropdownState())
        {
            minimumSize = new Vector2(270f, 50f);
            this.clips = clips;
            this.selectedClip = selected;
            this.onClipSelected = onClipSelected;
        }

        public void Update()
        {
            int selectedIndex = clips.ToList().FindIndex(clip => clip == selectedClip);
            Traverse.Create(this).Field("m_State")
                .Method("SetSelectionOnItem", root, selectedIndex)
                .GetValue();
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var selectedPath = AssetDatabase.GetAssetPath(selectedClip);
            var selectedIDs = Traverse.Create(this).Field("m_DataSource").Field<List<int>>("m_SelectedIDs");
            selectedIDs.Value.Add(selectedPath.GetHashCode());

            root = new AdvancedDropdownItem($"Clips: {clips.Length}");

            foreach (var clip in clips)
            {
                string clipPath = AssetDatabase.GetAssetPath(clip);
                var clipItem = new ClipDropdownItem(clip, clip.name, clipPath);
                root.AddChild(clipItem);
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            var clipItem = item as ClipDropdownItem;
            onClipSelected?.Invoke(clipItem.Clip);
        }

        private class ClipDropdownItem : AdvancedDropdownItem
        {
            public AnimationClip Clip { get; }

            public ClipDropdownItem(AnimationClip clip, string name, string path) : base(name)
            {
                Traverse.Create(this).Property<string>("tooltip").Value = path;
                id = path.GetHashCode();
                Clip = clip;
            }
        }
    }
}