using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using UnityEditor;

namespace Code.GameCore.Editor
{
    /// <summary>
    /// This is a workaround for ModTemplate only.
    /// There is a class EditorExitConfirmation which holds this
    /// functionality but it's inside Code.Editor assembly.
    /// This assembly isn't shipped with ModTemplate because
    /// of lot of unwanted dependencies. 
    /// </summary>
    [InitializeOnLoad]
    public class BlueprintServerModTemplateDestroy
    {
        static BlueprintServerModTemplateDestroy()
        {
            #if OWLCAT_MODS
            EditorApplication.wantsToQuit += StopBlueprintServer;
            #endif 
        }

        private static bool StopBlueprintServer()
        {
            #if OWLCAT_MODS
            DatabaseServerConnector.KillServer();
            #endif
            return true;
        }
    }
}