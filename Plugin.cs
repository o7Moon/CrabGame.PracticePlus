using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using TMPro;
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
        public static ConfigEntry<bool> hide_status;
        
        public static ConfigEntry<bool> shift_on_load;
        public override void Load()
        {
            Harmony.CreateAndPatchAll(typeof(Plugin));
            Harmony.CreateAndPatchAll(typeof(patch2));

            save_bind = Config.Bind<KeyCode>("Keys","Save Position",KeyCode.Q,"the key used for setting a savestate");
            load_bind = Config.Bind<KeyCode>("Keys","Load Position",KeyCode.Mouse0,"the key used for teleporting to the current savestate");
            shift_on_load = Config.Bind<bool>("Keys","Shift Load Modifier", true, "if true, must also hold shift to load position");
            disable_player_collision = Config.Bind<bool>("Settings","Disable Player Collision",true,"if true, disables collision with other players in practice.");
            hide_status = Config.Bind<bool>("Settings","Disable status UI", false, "if true, the status ui (player count, health) is hidden in practice.");

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
                    // dont register keybinds when typing in chat
                    if (ChatBox.Instance.transform.GetChild(0).GetChild(1).GetComponent<TMP_InputField>().isFocused) return;
                    if (Input.GetKeyDown(save_bind.Value)){
                        Rigidbody rb =  __instance.GetComponent<Rigidbody>();
                        saved_pos = rb.position;
                        saved_velocity = rb.velocity;
                        PlayerInput input = __instance.GetComponent<PlayerInput>();
                        saved_rot = new Vector2(input.playerCam.rotation.eulerAngles.y,input.GetMouseOffset());
                    }
                    if ((!shift_on_load.Value || Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift)) && Input.GetKeyDown(load_bind.Value)){
                        Rigidbody rb =  __instance.GetComponent<Rigidbody>();
                        
                        PlayerInput input = __instance.GetComponent<PlayerInput>();
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

        [HarmonyPatch(typeof(GameUI), nameof(GameUI.Start))]
        [HarmonyPostfix]
        public static void onUiStart(GameUI __instance) {
            if (!isPractice()) return;
            if (!hide_status.Value) return;

            Transform s = __instance.transform.FindChild("Status");
            if (s != null) s.gameObject.SetActive(false);
        }

        [HarmonyPatch(typeof(PlayerManager),nameof(PlayerManager.Awake))]
        [HarmonyPrefix]
        public static void onPlayerAwake(PlayerManager __instance){
            if (__instance.gameObject.GetComponent<CapsuleCollider>()){// this is the client player
                return;
            }
            if (!disable_player_collision.Value) return;
            if (!isPractice()) return;

            // exclude all of this player's colliders from interacting with all of the client player's colliders.
            // this is a very questionable way of doing this but its the only way that doesnt break nametags.
            foreach (Collider c in __instance.gameObject.GetComponentsInChildren<Collider>()){
                foreach (Collider l in PlayerInput.Instance.gameObject.GetComponentsInChildren<Collider>()) {
                    Physics.IgnoreCollision(c, l, true);
                }
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
            [HarmonyPatch(typeof(MonoBehaviourPublicGataInefObInUnique), "Method_Private_Void_GameObject_Boolean_Vector3_Quaternion_0")]
            [HarmonyPatch(typeof(MonoBehaviourPublicCSDi2UIInstObUIloDiUnique), "Method_Private_Void_0")]
            [HarmonyPatch(typeof(MonoBehaviourPublicVesnUnique), "Method_Private_Void_0")]
            [HarmonyPatch(typeof(MonoBehaviourPublicObjomaOblogaTMObseprUnique), "Method_Public_Void_PDM_2")]
            [HarmonyPatch(typeof(MonoBehaviourPublicTeplUnique), "Method_Private_Void_PDM_32")]
            [HarmonyPrefix]
            public static bool Detect(System.Reflection.MethodBase __originalMethod)
            {
                return false;
            }
        }
    }
}
