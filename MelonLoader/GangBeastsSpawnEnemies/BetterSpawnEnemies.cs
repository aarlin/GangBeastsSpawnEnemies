using Il2CppCoreNet.Model;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Il2Cpp;
using Il2CppCoreNet.Objects;
using Il2CppCostumes;
using Il2CppFemur;
using Il2CppGB.Core;
using Il2CppGB.Game.Data;
using Il2CppGB.Networking.Objects;
using MelonLoader;
using Il2CppGB.Core.Loading;
using Il2CppCoreNet.Utils;
using Il2CppGB.Game;
using Il2CppGB.Networking.Utils;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityEngine.AddressableAssets;
using Il2CppGB.Data.Loading;
using Il2CppGB.Core;
using Resources = Il2CppGB.Core.Resources;
using Il2CppInterop.Runtime.Runtime;
using UnityEngine.ResourceManagement.AsyncOperations;
using JetBrains.Annotations;

[assembly: MelonInfo(typeof(BetterSpawnEnemies), "BetterSpawnEnemies", "0.0.1", "dotpy")]
public class BetterSpawnEnemies : MelonMod
{
    int enemyCount = 0;

    bool inGame = false;

    int selectedEnemyType = -1;

    const string DESELECT_TEXT = "Deselect";

    string[] DEFAULT_BUTTON_TEXT =
    {
        "Spawn medium",
        "Spawn large",
        "Spawn small",
    };

    // DEFAULT_BUTTON_TEXT is copied over on initiliase
    string[] currentButtonTexts;


    List<WavesData> wavesData = new();
    void LoadWaveData(AsyncOperationStatus status, AssetReference item, object data)
    {
        MelonLogger.Msg("Trying to load wave data.");

        if (status != AsyncOperationStatus.Succeeded)
        {
            MelonLogger.Error("Failed to load scene data!");
            return;
        }

        wavesData.Add(((Il2CppSystem.Object)data).Cast<SceneData>().WavesData);
    }

    string[] wavesMaps = { 
        "Grind",
        "Incinerator",
        "Rooftop",
        "Subway"
    };
    private void TryLoadAllWavesData()
    {
        wavesData.Clear();
        foreach (string wavesMap in wavesMaps) {
            var sceneDataReference = Global.Instance.SceneLoader._sceneList[wavesMap];
            Il2CppSystem.Object data;
            Resources.LoadLoadedAsset(sceneDataReference, wavesMap + "-Data", out data, (Resources.OnLoaded)LoadWaveData);
        }
    }

    private WavesData GetRandomWavesData()
    {
        if (wavesData.Count == 0) return null;
        return wavesData[Random.RandomRangeInt(0, wavesData.Count)];
    }

    private List<NetBeast> aiNetPlayers = new();
    public void SpawnEnemy(int type, Vector3 position)
    {
        //if (!bakedNavigationMeshAlready)
            //BakeNavigationMesh();

        Debug.Log("SPAWNING ENEMY");
        WavesData data = GetRandomWavesData();
        if (data == null)
        {
            MelonLogger.Error("WavesData is null!");
            return;
        }

        Wave spawnList = data.levelWaves[0];

        string name = data.GetRandomCostume();
        int num = data.GetRandomColour();
        int gang = spawnList.beasts[0].gangID;

        bool newNet = false;
        NetBeast netBeastRef = null;
        CostumeSaveEntry costumeSaveEntry = MonoSingleton<Global>.Instance.Costumes.CostumePresetDatabase.GetCostumeByPresetName(name);
        if (costumeSaveEntry == null)
        {
            costumeSaveEntry = CostumePool.I.GetCostumeEntry(0);
        }
        NetCostume netCostume = new NetCostume(costumeSaveEntry);
        netCostume.Voice = Actor.GetRandomVoice(false, true);
        ColorObject colorOjectWithID = CostumePool.I.PlayerColorDatabase.GetColorOjectWithID((ushort)num);
        Color primaryColor = Color.gray;
        Color costumeColor = Color.gray;
        if (colorOjectWithID != null && colorOjectWithID.Colors.Length != 0)
        {
            primaryColor = colorOjectWithID.Colors[0];
        }
        if (colorOjectWithID != null && colorOjectWithID.Colors.Length > 1)
        {
            costumeColor = colorOjectWithID.Colors[1];
        }

        for (int j = 0; j < this.aiNetPlayers.Count; j++)
        {
            if (!this.aiNetPlayers[j].Alive)
            {
                netBeastRef = this.aiNetPlayers[j];
                netBeastRef.Alive = true;
                netBeastRef.Costume = netCostume;
                netBeastRef.PrimaryColor = primaryColor;
                netBeastRef.CostumeColor = costumeColor;
            }
        }
        if (netBeastRef == null)
        {
            netBeastRef = new NetBeast(GameMode_Waves.AI_CONTROLLER_STARTIDEX + this.aiNetPlayers.Count, netCostume, primaryColor, costumeColor, gang, NetPlayer.PlayerType.AI, false);
            netBeastRef.Alive = true;
            this.aiNetPlayers.Add(netBeastRef);
            newNet = true;
        }

        GameObject gameObject = null;
        gameObject = UnityEngine.Object.Instantiate<GameObject>(data.GetSpawnObject(type), position, Quaternion.identity);

        if (gameObject != null)
        {
            netBeastRef.Instance = gameObject;
            Actor component = gameObject.GetComponent<Actor>();
            if (component != null)
            {
                NetModel model = GameObject.FindObjectOfType<NetModel>();

                component.ControlledBy = Actor.ControlledTypes.AI;
                component.playerID = -1;
                component.IsAI = true;
                component.controllerID = netBeastRef.ControllerId;
                component.gangID = gang;
                
                if (!newNet)
                {
                    model.UpdateCollectionItem<NetBeast>("NET_PLAYERS", netBeastRef);
                }
                else
                {
                    model.Add<NetBeast>("NET_PLAYERS", netBeastRef);
                }
                component.DressBeast();
                NetworkServer.Spawn(gameObject);
                GBNetUtils.SetBeastsGang(netBeastRef);
            }
        }
    }

    private bool bakedNavigationMeshAlready = false;
    public void BakeNavigationMesh()
    {
        if (bakedNavigationMeshAlready) return;
        bakedNavigationMeshAlready = true;

        foreach (Collider collider in GameObject.FindObjectsOfType<Collider>(true))
        {
            GameObject gamerMan = collider.gameObject;
            //Rigidbody rb = gamerMan.GetComponent<Rigidbody>();
            //if (rb != null && !rb.isKinematic)
            //{
            //    continue;
            //}

            gamerMan.AddComponent<NavMeshSurface>().BuildNavMesh();
        }
    }

    public override void OnLateInitializeMelon()
    {
        currentButtonTexts = (string[])DEFAULT_BUTTON_TEXT.Clone();

        string path = "Waves/Grind";

        SceneManager.sceneLoaded += (Action<Scene, LoadSceneMode>)SceneLoaded;
    }

    public void SceneLoaded(Scene scene, LoadSceneMode _)
    {
        bakedNavigationMeshAlready = false;
        inGame = scene.name != Global.MENU_SCENE_NAME;
        if (inGame && wavesData.Count == 0)
            TryLoadAllWavesData();
    }

    public Vector3? GetMousePosition(Vector3 offset)
    {
        Vector3 mousePos = Mouse.current.position.ReadValue();
        mousePos.z = 10f;

        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        RaycastHit info;
        
        if (Physics.Raycast(ray, out info))
        {
            return info.point + offset;
        }
        return null;
    }

    public void CheckIfPlayerDead()
    {
        if (aiNetPlayers.Count == 0) return;

        foreach (Actor actor in Actor.CachedActors)
        {
            if (actor.ControlledBy == Actor.ControlledTypes.Human && actor.actorState != Actor.ActorState.Dead) return;
        }

        NetModel model = GameObject.FindObjectOfType<NetModel>();
        foreach (var ai in aiNetPlayers)
        {
            if (ai == null || ai.Instance == null) continue;
            model.Remove<NetBeast>("NET_PLAYERS", ai);
            /*ai.Alive = false;
            Actor actor = ai.Instance.GetComponent<Actor>();
            if (actor != null) actor.actorState = Actor.ActorState.Dead;*/
        }

        aiNetPlayers.Clear();
    }

    public override void OnLateUpdate()
    {
        CheckIfPlayerDead();

        if (!inGame || selectedEnemyType == -1) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // check if mouse is close to buttons
            Vector2 value = Mouse.current.position.ReadValue();
            if (value.x < 100 && value.y > Screen.height - 100) return;
            Vector3? pos = GetMousePosition(Vector3.up);
            if (pos == null) return;
            SpawnEnemy(selectedEnemyType, pos.Value);
        }
        
        if (Gamepad.current != null && Gamepad.current.rightTrigger.wasPressedThisFrame)
        {
            Vector3? pos = GetMousePosition(Vector3.up);
            if (pos == null) return;
            SpawnEnemy(selectedEnemyType, pos.Value);
        }
    }

    public void HandleButtonPress(int typeID)
    {
        if (typeID != selectedEnemyType)
        {
            if (selectedEnemyType != -1)
                currentButtonTexts[selectedEnemyType] = DEFAULT_BUTTON_TEXT[selectedEnemyType];

            currentButtonTexts[typeID] = DESELECT_TEXT;
            selectedEnemyType = typeID;
        }
        else
        {
            currentButtonTexts[selectedEnemyType] = DEFAULT_BUTTON_TEXT[selectedEnemyType];
            selectedEnemyType = -1;
        }
    }

    // so that it goes small, medium, large
    int[] LOOP_ORDER = new int[3] { 2, 0, 1 };
    public override void OnGUI()
    {
        foreach (var index in LOOP_ORDER)
            if (GUILayout.Button(currentButtonTexts[index])) HandleButtonPress(index);
    }
}
