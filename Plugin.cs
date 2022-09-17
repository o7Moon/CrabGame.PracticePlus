using BepInEx;
using BepInEx.IL2CPP;
using HarmonyLib;
using UnityEngine;
using Il2CppSystem;

namespace practicePlus
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BasePlugin
    {
        public static MonoBehaviourPublicCSDi2UIInstObUIloDiUnique gamemodeManager = null;
        public static Vector3 saved_pos;
        public static Vector3 saved_velocity;
        public static Vector2 saved_rot;
        public override void Load()
        {
            Harmony.CreateAndPatchAll(typeof(Plugin));
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
        public static bool isPractice(){
            if (gamemodeManager == null){
                GameObject managers = GameObject.Find("/Managers");
                gamemodeManager = managers.GetComponent<MonoBehaviourPublicCSDi2UIInstObUIloDiUnique>();
            }
            if (gamemodeManager == null){
                return false;
            }
            return gamemodeManager.gameMode.modeName == "Practice";
        }
        //                   PlayerController
        [HarmonyPatch(typeof(MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique),"Update")]
        [HarmonyPostfix]
        public static void playerUpdatePost(MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique __instance){
            if (!__instance.dead){
                if (isPractice()){
                    if (Input.GetKeyDown("q")){
                        Rigidbody rb =  __instance.GetComponent<Rigidbody>();
                        saved_pos = rb.position;
                        saved_velocity = rb.velocity;
                        PlayerInput input = __instance.GetComponent<PlayerInput>();
                        saved_rot = new Vector2(input.playerCam.rotation.eulerAngles.y,input.GetMouseOffset());
                    }
                    if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetMouseButtonDown(0)){
                        Rigidbody rb =  __instance.GetComponent<Rigidbody>();
                        
                        PlayerInput input = __instance.GetComponent<PlayerInput>();
                        Debug.Log(saved_rot.ToString());
                        input.desiredX = saved_rot.x;
                        input.SetMouseOffset(saved_rot.y);
                        input.cameraRot = new Vector3(saved_rot.y,saved_rot.x,input.actualWallRotation);
                        input.playerCam.transform.rotation = Quaternion.Euler(input.cameraRot);
                        
                        rb.position = saved_pos;
                        rb.velocity = saved_velocity;
                    }
                }
            }
        }
    }
}
