using System;
using System.Collections.Generic;
using CementTools;
using CoreNet.Model;
using Costumes;
using Femur;
using GB.Core;
using GB.Game.Data.Waves;
using GB.Networking.Objects;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SpawnEnemies
{
    public class KeybindManager
    {
        private ModFile _file;
        private Dictionary<string, KeybindData> _keybinds = new Dictionary<string, KeybindData>();
        private Dictionary<string, bool> _pressedLastFrame = new Dictionary<string, bool>();

        public KeybindManager(ModFile file)
        {
            this._file = file;
        }

        private bool WasPressedLastFrame(string p)
        {
            if (!this._pressedLastFrame.ContainsKey(p))
            {
                this._pressedLastFrame[p] = false;
                return false;
            }
            return this._pressedLastFrame[p];
        }

        public void CheckInputs()
        {
            foreach (InputDevice inputDevice in InputSystem.devices)
            {
                foreach (string text in this._keybinds.Keys)
                {
                    InputControl inputControl = inputDevice.TryGetChildControl(text);
                    if (inputControl != null && InputControlExtensions.IsPressed(inputControl, 0f))
                    {
                        if (!this._keybinds[text].held && this.WasPressedLastFrame(text))
                        {
                            this._keybinds[text].action();
                            this._pressedLastFrame[text] = true;
                        }
                    }
                    else
                    {
                        this._pressedLastFrame[text] = false;
                    }
                }
            }
        }

        public void BindSmall(Action a, bool held = true)
        {
            this._keybinds[this._file.GetString("KeybindSpawnSmall")] = new KeybindData(a, held);
        }

        public void BindMedium(Action a, bool held = true)
        {
            this._keybinds[this._file.GetString("KeybindSpawnMedium")] = new KeybindData(a, held);
        }

        public void BindLarge(Action a, bool held = true)
        {
            this._keybinds[this._file.GetString("KeybindSpawnLarge")] = new KeybindData(a, held);
        }

        private class KeybindData
        {
            public readonly Action action;
            public readonly bool held;

            public KeybindData(Action a, bool h)
            {
                this.action = a;
                this.held = h;
            }
        }
    }

    public class SpawnEnemies : CementMod
    {
        private int enemyCount = 0;
        private WavesData waveInformation;
        private KeybindManager keybindManager;
        private bool inGame = false;
        private GameObject localPlayer;

        public void SpawnEnemy(int type, Vector3 position)
        {
            Debug.Log("SPAWNING ENEMY");
            if (this.waveInformation == null) return;

            Wave wave = this.waveInformation.levelWaves[0];
            CostumeSaveEntry costumeByPresetName = MonoSingleton<Global>.Instance.Costumes.CostumePresetDatabase.GetCostumeByPresetName(this.waveInformation.GetRandomCostume());
            NetCostume netCostume = new NetCostume(costumeByPresetName);
            Color color = new Color(Random.value, Random.value, Random.value);
            int gangID = wave.beasts[0].gangID;
            NetBeast netBeast = new NetBeast(200 + this.enemyCount, netCostume, color, color, gangID, 1, false);
            GameObject gameObject = Object.Instantiate(this.waveInformation.GetSpawnObject(type), position, Quaternion.identity);

            if (gameObject != null)
            {
                netBeast.Instance = gameObject;
                Actor component = gameObject.GetComponent<Actor>();
                if (component != null)
                {
                    component.ControlledBy = 1;
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

        public void Start()
        {
            this.keybindManager = new KeybindManager(this.modFile);
            this.keybindManager.BindLarge(new Action(this.SpawnLarge), false);
            this.keybindManager.BindMedium(new Action(this.SpawnMedium), false);
            this.keybindManager.BindSmall(new Action(this.SpawnSmall), false);
            string text = "Waves/Grind";
            this.waveInformation = Resources.Load<WavesData>(text);
            SceneManager.sceneLoaded += new UnityAction<Scene, LoadSceneMode>(this.SceneLoaded);
        }

        public void SceneLoaded(Scene scene, LoadSceneMode _)
        {
            this.inGame = scene.name != Global.MENU_SCENE_NAME;
        }

        private Vector3 GetMousePositionInWorld()
        {
            Vector3 vector = Mouse.current.position.ReadValue();
            vector.z = 2f;
            Ray ray = Camera.main.ScreenPointToRay(vector);
            if (Physics.Raycast(ray, out RaycastHit raycastHit))
            {
                return raycastHit.point;
            }
            else
            {
                return new Vector3(0f, -1000f, 0f);
            }
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

        private void SpawnSmall()
        {
            Vector3 position = this.GetFirstPlayerPositionInWorld() + Vector3.up;
            this.SpawnEnemy(2, position);
        }

        private void SpawnMedium()
        {
            Vector3 position = this.GetFirstPlayerPositionInWorld() + Vector3.up;
            this.SpawnEnemy(0, position);
        }

        private void SpawnLarge()
        {
            Vector3 position = this.GetFirstPlayerPositionInWorld() + Vector3.up;
            this.SpawnEnemy(1, position);
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

            if (this.inGame)
            {
                this.keybindManager.CheckInputs();
            }
        }
    }
}