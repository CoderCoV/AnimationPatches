using HarmonyLib;
using UnityEditor;

namespace CoderScripts.AnimationPatches
{
    [InitializeOnLoad]
    public class Core
    {
        private static readonly Harmony harmonyInstance = new Harmony("CoderScripts.AnimationPatches");

        static Core()
        {
            EditorApplication.delayCall += ApplyPatches;
        }

        private static void ApplyPatches()
        {
            harmonyInstance.PatchAll();
        }
    }
}