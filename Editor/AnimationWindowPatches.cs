using System;
using HarmonyLib;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEditor.IMGUI.Controls;

namespace CoderScripts.AnimationPatches
{
    public class AnimationWindowPatches
    {

        private static readonly Type AnimationWindowClipPopupType = AccessTools.TypeByName("UnityEditor.AnimationWindowClipPopup");
        private static readonly Type AnimationWindowUtilityType = AccessTools.TypeByName("UnityEditorInternal.AnimationWindowUtility");

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
                    .Property<String>("Name").Value;
                var activeAnimationPlayer = stateField.Property<Component>("activeAnimationPlayer").Value;
                var activeAnimationClipProp = stateField.Property<AnimationClip>("activeAnimationClip");
                var AnimationWindowUtilityTr = Traverse.Create(AnimationWindowUtilityType);

                var CreateNewClipMethod = AnimationWindowUtilityType.GetMethod("CreateNewClip",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
                var AddClipToAnimationPlayerComponentMethod = AnimationWindowUtilityType
                    .GetMethod("AddClipToAnimationPlayerComponent",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);

                Rect controlRect = EditorGUILayout.GetControlRect(false, 18f, style);
                Event current = Event.current;
                switch (current.type)
                {
                    case EventType.Repaint:
                    {
                        Font font = style.font;
                        if ((bool)font && font == EditorStyles.miniFont)
                        {
                            style.font = EditorStyles.miniBoldFont;
                        }

                        GUIContent gUIContent = new GUIContent();
                        if (clip  != null)
                        {
                            gUIContent.text = clip.name;
                            gUIContent.tooltip = AssetDatabase.GetAssetPath(clip);
                        }

                        style.Draw(controlRect, gUIContent, false, false, false, false);
                        style.font = font;
                        break;
                    }
                    case EventType.MouseDown:
                        if (current.button == 0 && controlRect.Contains(current.mousePosition))
                        {
                            AnimationClip[] Clips = Traverse.Create(__instance).Method("GetOrderedClipList").GetValue<AnimationClip[]>();
                            var dropdown = new AnimationClipListSearch(Clips, animationClip =>
                            {
                                activeAnimationClipProp.Value = animationClip;
                            });
                            AdvancedDropdownExtensions.Show(dropdown, controlRect, 200);
                            current.Use();
                        } 
                        break;
                }

                if (GUILayout.Button(EditorGUIUtility.IconContent("d_Toolbar Plus", "|Create New Clip"), EditorStyles.toolbarButton))
                {
                    AnimationClip animationClip = CreateNewClipMethod.Invoke(null, new []{ rootGameObjectName }) as AnimationClip;
                    if ((bool)animationClip)
                    {
                        AddClipToAnimationPlayerComponentMethod.Invoke(null, new object[]{activeAnimationPlayer, animationClip});
                        __result = animationClip;
                    }
                }
                return false;
            }
        }
    }


    public class AnimationClipListSearch : AdvancedDropdown
    {
        public AnimationClip[] Clips;
        public Action<AnimationClip> SelectedAction;
        public AnimationClip SelectedClip;
        public AnimationClipListSearch(AnimationClip[] List, Action<AnimationClip> selectedAction) : base(new AdvancedDropdownState())
        {
            minimumSize = new Vector2(270f, 50f);
            Clips = List;
            SelectedAction = selectedAction;
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Select Clip");

            var ClipsCount = Clips.Length;
            for (var i = 0; i < ClipsCount; i++)
            {
                var ClipName = Clips[i].name;
                var ClipItem = new ClipDropdownItem(Clips[i], ClipName);
                root.AddChild(ClipItem);
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            var op = item as ClipDropdownItem;
            SelectedClip = op.Clip;
            SelectedAction(SelectedClip);
        }

        public AnimationClip GetClip() => SelectedClip;

        public class ClipDropdownItem : AdvancedDropdownItem
        {
            public AnimationClip Clip;
            public ClipDropdownItem(AnimationClip clip, string name) : base(name)
            {
                Clip = clip;
            }
        }
    }
}




