using System;
using System.Collections.Generic;
using CementTools;
using CementTools.Modules.InputModule;
using CoreNet.Model;
using Costumes;
using Femur;
using GB.Core;
using GB.Game.Data;
using GB.Networking.Objects;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Resources = GB.Core.Resources;

namespace GangBeastsSpawnEnemies
{

    public class SpawnEnemies : CementMod
    {
        private int enemyCount = 0;
        private WavesData waveInformation;
        private bool inGame = false;
        private GameObject localPlayer;

        public void SpawnEnemy(int type, Vector3 position)
        {
            Debug.Log("SPAWNING ENEMY");
            if (this.waveInformation == null) return;

            Wave wave = this.waveInformation.levelWaves[0];
            CostumeSaveEntry costumeByPresetName = MonoSingleton<Global>.Instance.Costumes.CostumePresetDatabase.GetCostumeByPresetName(this.waveInformation.GetRandomCostume());
            NetCostume netCostume = new NetCostume(costumeByPresetName);
            Color color = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
            int gangID = wave.beasts[0].gangID;
            NetBeast netBeast = new NetBeast(200 + this.enemyCount, netCostume, color, color, gangID, (CoreNet.Objects.NetPlayer.PlayerType)1, false);
            GameObject gameObject = UnityEngine.Object.Instantiate(this.waveInformation.GetSpawnObject(type), position, Quaternion.identity);

            if (gameObject != null)
            {
                netBeast.Instance = gameObject;
                Actor component = gameObject.GetComponent<Actor>();
                if (component != null)
                {
                    component.ControlledBy = (Actor.ControlledTypes)1;
                    component.playerID = -1;
                    component.IsAI = true;
                    component.controllerID = netBeast.ControllerId;
                    component.gangID = gangID;
                    MonoSingleton<Global>.Instance.GetComponentInChildren<NetModel>().Add("NET_PLAYERS", netBeast);
                    component.DressBeast();
                }
                MonoSingleton<Global>.Instance.GetComponentInChildren<NetModel>().Remove("NET_PLAYERS", netBeast);
                this.enemyCount++;
            }
        }

        private void SpawnSmall(Actor beast)
        {
            Vector3 position = this.GetPlayerPosition(beast) + Vector3.up;
            this.SpawnEnemy(2, position);
        }

        private void SpawnMedium(Actor beast)
        {
            Vector3 position = this.GetPlayerPosition(beast) + Vector3.up;
            this.SpawnEnemy(0, position);
        }

        private void SpawnLarge(Actor beast)
        {
            Vector3 position = this.GetPlayerPosition(beast) + Vector3.up;
            this.SpawnEnemy(1, position);
        }

        public void Start()
        {
            string text = "Waves/Grind";
            // TODO: fix issue with wave information loading
            this.waveInformation = Resources.LoadItem<WavesData>(text);
            SceneManager.sceneLoaded += new UnityAction<Scene, LoadSceneMode>(this.SceneLoaded);
            BindKeys();
        }

        private void BindKeys()
        {
            InputManager.onInput(CementTools.Modules.InputModule.Input.i).bind(SpawnSmall);
            InputManager.onInput(CementTools.Modules.InputModule.Input.dpadLeft).bind(SpawnSmall);

            InputManager.onInput(CementTools.Modules.InputModule.Input.o).bind(SpawnMedium);
            InputManager.onInput(CementTools.Modules.InputModule.Input.dpadDown).bind(SpawnMedium);

            InputManager.onInput(CementTools.Modules.InputModule.Input.p).bind(SpawnLarge);
            InputManager.onInput(CementTools.Modules.InputModule.Input.dpadRight).bind(SpawnLarge);
        }

        public void SceneLoaded(Scene scene, LoadSceneMode _)
        {
            this.inGame = scene.name != Global.MENU_SCENE_NAME;
        }

        private Vector3 GetFirstPlayerPositionInWorld()
        {
            foreach (Actor actor in Object.FindObjectsOfType<Actor>())
            {
                if (actor.isLocalPlayer)
                {
                    GameObject localPlayer = actor.gameObject;
                    Vector3 firstPlayerPosition = localPlayer.transform.position;
                    return firstPlayerPosition;
                }
            }
            return new Vector3(0f, -1000f, 0f);
        }

        private Vector3 GetPlayerPosition(Actor beast)
        {
            GameObject localPlayer = beast.gameObject;
            return localPlayer.transform.position;
        }

        public void Update()
        {
            if (this.localPlayer == null)
            {
                foreach (Actor actor in Object.FindObjectsOfType<Actor>())
                {
                    if (actor.isLocalPlayer)
                    {
                        this.localPlayer = actor.gameObject;
                        actor.gangID = 1;
                    }
                }
            }
        }
    }
}
