using HarmonyLib;
using UnityEditor;

namespace CoderScripts.AnimationPatches
{
    [InitializeOnLoad]
    public class Core
    {
        private static int wait = 0;

        static Core()
        {
            EditorApplication.update -= DoPatches;
            EditorApplication.update += DoPatches;
        }

        public static Harmony harmonyInstance = new Harmony("CoderScripts.AnimationPatches");

        static void DoPatches()
        {
            wait++;
            if (wait > 2)
            {
                EditorApplication.update -= DoPatches;
                harmonyInstance.PatchAll();
            }
        }
    }
}