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
                        if (clip != null)
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
                            var dropdown = new AnimationClipListSearch(Clips, clip, animationClip =>
                            {
                                activeAnimationClipProp.Value = animationClip;
                            });
                            dropdown.Show(controlRect, 200);
                            dropdown.Update();
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
        public AnimationClip Selected;
        public Action<AnimationClip> SelectedAction;
        public AnimationClip SelectedClip;
        public AdvancedDropdownItem Root;
        public AnimationClipListSearch(AnimationClip[] List, AnimationClip selected, Action<AnimationClip> selectedAction) : base(new AdvancedDropdownState())
        {
            minimumSize = new Vector2(270f, 50f);
            Clips = List;
            Selected = selected;
            SelectedAction = selectedAction;
        }

        public void Update()
        {
            Traverse.Create(this).Field("m_State")
                .Method("SetSelectionOnItem", Root, Clips.ToList().FindIndex(i => i == Selected))
                .GetValue();
        }


        protected override AdvancedDropdownItem BuildRoot()
        {
            var selectedPath = AssetDatabase.GetAssetPath(Selected);
            var m_SelectedIDs = Traverse.Create(this).Field("m_DataSource").Field<List<int>>("m_SelectedIDs");
                m_SelectedIDs.Value.Add(selectedPath.GetHashCode());                


            var ClipsCount = Clips.Length;
            var root = new AdvancedDropdownItem("Clips: " + ClipsCount);
            for (var i = 0; i < ClipsCount; i++)
            {
                var ClipName = Clips[i].name;
                string ClipPath = AssetDatabase.GetAssetPath(Clips[i]);
                var ClipItem = new ClipDropdownItem(Clips[i], ClipName, ClipPath);
                
                root.AddChild(ClipItem);
            }

            Root = root;
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
            public ClipDropdownItem(AnimationClip clip, string name, string path) : base(name)
            {
                Traverse.Create(this).Property<string>("tooltip").Value = path;
                id = path.GetHashCode();
                Clip = clip;
            }
        }
    }
}




