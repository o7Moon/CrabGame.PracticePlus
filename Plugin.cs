using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using Il2CppSystem;

namespace practicePlus
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BasePlugin
    {
        public static Vector3 saved_pos;
        public static Vector3 saved_velocity;
        public static Vector2 saved_rot;
        public static ConfigEntry<KeyCode> save_bind;
        public static ConfigEntry<KeyCode> load_bind;
        public static ConfigEntry<bool> disable_player_collision;
        public override void Load()
        {
            Harmony.CreateAndPatchAll(typeof(Plugin));
            Harmony.CreateAndPatchAll(typeof(patch2));

            save_bind = Config.Bind<KeyCode>("Keys","Save Position",KeyCode.Q,"the key used for setting a savestate");
            load_bind = Config.Bind<KeyCode>("Keys","Load Position",KeyCode.Mouse0,"the key used for teleporting to the current savestate");
            disable_player_collision = Config.Bind<bool>("Settings","Disable Player Collision",true,"if true, disables collision with other players in practice.");

            SceneManager.sceneLoaded += (UnityAction<Scene,LoadSceneMode>) onSceneLoad;

            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        public void onSceneLoad(Scene s, LoadSceneMode m){
            Config.Reload();
        }
        public static bool isPractice(){
            if (LobbyManager.Instance == null){
                return false;
            }
            return LobbyManager.Instance.gameMode.modeName == "Practice";
        }
        [HarmonyPatch(typeof(PlayerMovement),nameof(PlayerMovement.Update))]
        [HarmonyPostfix]
        public static void playerUpdatePost(PlayerMovement __instance){
            if (!__instance.dead){
                if (isPractice()){
                    if (Input.GetKeyDown(save_bind.Value)){
                        Rigidbody rb =  __instance.GetComponent<Rigidbody>();
                        saved_pos = rb.position;
                        saved_velocity = rb.velocity;
                        PlayerInput input = __instance.GetComponent<PlayerInput>();
                        saved_rot = new Vector2(input.playerCam.rotation.eulerAngles.y,input.GetMouseOffset());
                    }
                    if (Input.GetKeyDown(load_bind.Value)){
                        Rigidbody rb =  __instance.GetComponent<Rigidbody>();
                        
                        PlayerInput input = __instance.GetComponent<PlayerInput>();
                        //Debug.Log(saved_rot.ToString());
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

        [HarmonyPatch(typeof(PlayerManager),nameof(PlayerManager.Awake))]
        [HarmonyPrefix]
        public static void onPlayerAwake(PlayerManager __instance){
            if (__instance.gameObject.GetComponent<CapsuleCollider>()){// this is the client player
                return;
            }
            if (!disable_player_collision.Value) return;
            if (!isPractice()) return;

            foreach (Collider c in __instance.gameObject.GetComponentsInChildren<Collider>()){
                MonoBehaviour.Destroy(c);
            }
        }

        // bepinex detection
        [HarmonyPatch]
        [HarmonyPatch(typeof(MonoBehaviourPublicGataInefObInUnique), "Method_Private_Void_GameObject_Boolean_Vector3_Quaternion_0")]
        [HarmonyPatch(typeof(MonoBehaviourPublicCSDi2UIInstObUIloDiUnique), "Method_Private_Void_0")]
        [HarmonyPatch(typeof(MonoBehaviourPublicVesnUnique), "Method_Private_Void_0")]
        [HarmonyPatch(typeof(MonoBehaviourPublicObjomaOblogaTMObseprUnique), "Method_Public_Void_PDM_2")]
        [HarmonyPatch(typeof(MonoBehaviourPublicTeplUnique), "Method_Private_Void_PDM_32")]
        static class patch2{
            [HarmonyPrefix]
            public static bool Detect(System.Reflection.MethodBase __originalMethod)
            {
                return false;
            }
        }
    }
}
