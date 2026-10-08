using UnityEditor;

namespace BiologyVR.ArteryRoute.Editor
{
    // One-shot recovery of the local MCP editor bridge after a domain reload lost its old socket.
    [InitializeOnLoad]
    public static class ArteryBridgeRecovery
    {
        static ArteryBridgeRecovery()
        {
            EditorApplication.delayCall+=Recover;
        }
        static void Recover()
        {
            if(SessionState.GetBool("BiologyVR.McpRecovery8091",false))return;
            SessionState.SetBool("BiologyVR.McpRecovery8091",true);
            var server=McpUnity.Unity.McpUnityServer.Instance;
            if(server!=null)server.RestartServer();
        }
    }
}
