using BepInEx;
using DressmakerMods;
using Dummiesman;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;
using Image = UnityEngine.UI.Image;

namespace ScissorsReplacement
{
    [BepInPlugin("ambrosia.dressmaker.scissorsreplacement.safteyscissors", "Saftey Scissors", "1.0.0")]
    [BepInDependency("yuu.dressmaker.mods")]
    public sealed class ScissorsReplacer : BaseUnityPlugin
    {
        // it's good practice to use const variables to reference things like object names. saves headache later ^^
        private const string scissorsObjectLeftName = "scissors_left";
        private const string scissorsObjectRightName = "scissors_right";

        private ModContext mod;
        private Button CuttingRoomButton;
        private List<GameObject> ScissorsLeft;
        private List<GameObject> ScissorsRight;

        private Mesh ScissorsLeftMesh;
        private Mesh ScissorsRightMesh;
        private GameObject ScissorsLeftGO;
        private GameObject ScissorsRightGO;

        private Texture2D ScissorsTexture;

        private ArtRegistration ScissorsButton;
        private ArtRegistration DiscardButton;
        private ArtRegistration ScissorsSprite;
        private ArtRegistration ScissorsSpriteShadow;

        private string Folder;
        //file info for files loaded from file path, used for GameObject naming and MTL finding
        private FileInfo _objInfo;

        private void Awake()
        {
            mod = ModLoaderApi.Register(this, new ModInfo { Author = "AmbrosiaFae", Name = "Saftey Scissors" });
            Folder = Path.GetDirectoryName(Info.Location);

            Logger.LogInfo("Scissors Replacer Loaded.");
            ModLifecycleApi.Subscribe(mod, ModLifecycleEvent.SceneLoaded, x => SetupButton());
        }

        private void Start()
        {
            //using Yuuma's modloader to register texture replacements
            /* The buttons are being means to me; leaving for reference later
            ScissorsButton = ArtModApi.Register("ambrosiafae.ScissorsReplacer.ScissorsButton",
            new ArtReplacementDefinition
            {
                Kind = "image",
                AssetName = "scissorsSmall",
                Scene = "PatternCutting",
                ObjectPath = "CuttingRoom/BottomPanel/Canvas/Panel/Buttons/Scissors/inner/SeamsVisible",
                Image = "scissors-small.png"
            }, Path.GetDirectoryName(Info.Location));

            DiscardButton = ArtModApi.Register("ambrosiafae.ScissorsReplacer.DiscardButton",
            new ArtReplacementDefinition
            {
                Kind = "image",
                AssetName = "discardFabricIcon",
                Scene = "PatternCutting",
                ObjectPath = "CuttingRoom/BottomPanel/Canvas/Panel/Buttons/DiscardFabric/inner/SeamsVisible",
                Image = "discard.png"
            }, Path.GetDirectoryName(Info.Location));
            */
            ScissorsSprite = ArtModApi.Register("ambrosiafae.ScissorsReplacer.ScissorsSprite",
            new ArtReplacementDefinition
            {
                Kind = "sprite",
                AssetName = "scissors",
                Scene = "PatternCutting",
                ObjectPath = "CuttingRoom/Scissors/ScissorsSprite",
                Image = "scissors-sprite-replacement.png"
            }, Path.GetDirectoryName(Info.Location));

            ScissorsSpriteShadow = ArtModApi.Register("ambrosiafae.ScissorsReplacer.ScissorsSpriteShadow",
            new ArtReplacementDefinition
            {
                Kind = "atlas",
                AssetName = "scissors",
                Scene = "PatternCutting",
                ObjectPath = "CuttingRoom/ScissorRest",
                Image = "shadow.png"
            }, Path.GetDirectoryName(Info.Location));

        }

        // This code runs every time the scene loads. The reason for this is there's not a way to only subscribe to one specific scene (that I am aware of at this time)
        // to prevent undoing / redoing the work, we make a check of some kind to prevent this from rerunning. could later be replaced by an "unsubscribe" if ever added to ModLifeCycleApi
        public void SetupButton()
        {
            if (CuttingRoomButton == null)
            {
                Logger.LogInfo($"Init Scissors Set Up");
                try
                {
                    var field = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "CuttingTable").FirstOrDefault();

                    Logger.LogInfo($"obj: {field.name}");

                    CuttingRoomButton = field.GetComponent<Button>(); // Dressmaker uses the Unity.UI.Button for UI interactions. double check the components you're trying to reference if you keep getting null references
                    Logger.LogInfo($"CuttingRoomButton: {CuttingRoomButton.name}");

                    CuttingRoomButton.onClick.AddListener(RunReplacement);
                }
                catch (Exception e)
                {
                    //Logger.LogInfo("caught exception " + e.ToString());
                }
            }


        }

        public void RunReplacement()
        {
            FindScissorsObjects(scissorsObjectLeftName, scissorsObjectRightName);
        }

        // This code runs every time the "Cutting Room" button is pressed. 
        // This means meshes and textures can be editted and reloaded without closing the game,
        // but also means care needs to be taken that hotloading doesn't leave artifacts in the background
        // Note this function looks for all objects using the same name. I noticed for mannequins, there are multiple different mannequin objects that are in seperate areas.
        // so you may want to find all and not just take the first
        public void FindScissorsObjects(string leftName, string rightName)
        {
            var Folder = Path.GetDirectoryName(Info.Location);
            // Find the names of GameObjects by searching in game with an inspector plugin
            ScissorsLeft = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == leftName).ToList();
            ScissorsRight = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == rightName).ToList();

            ScissorsLeftMesh = LoadObjByFilePath(Path.Combine(Folder, "replace_scissors_left.obj"), ScissorsLeftGO);
            ScissorsRightMesh = LoadObjByFilePath(Path.Combine(Folder, "replace_scissors_right.obj"), ScissorsRightGO);

            ScissorsTexture = LoadImageByFilePath(Path.Combine(Folder, "texture.png"));
            try
            {
                Logger.LogInfo($"Left: {ScissorsLeft.Count}");
                Logger.LogInfo($"Right: {ScissorsRight.Count}");
            }
            catch
            {
                Logger.LogInfo($"failed to find scissors");
            }
            SetScissorsObjectsToNewResources();
            //Prevent your reference meshes from building up! delete them before re-referencing.
            //This is an artifact of me not diving too deep into the ObjLoader code to prevent it from creating a new gameobject instead of using an existing one atm

            var destroy = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "replace_scissors_left" || obj.name == "replace_scissors_right").ToList();

            foreach (var item in destroy)
            {
                Destroy(item);
            }
        }

        public void SetScissorsObjectsToNewResources()
        {
            try
            {
                foreach (var item in ScissorsLeft)
                {
                    var left = item.GetComponentInChildren<MeshFilter>();
                    left.sharedMesh = ScissorsLeftMesh;
                    left.mesh = ScissorsLeftMesh;

                    var leftRenderer = item.GetComponentInChildren<MeshRenderer>();
                    leftRenderer.sharedMaterial.color = Color.white;
                    leftRenderer.sharedMaterial.mainTexture = ScissorsTexture;
                    Logger.LogInfo("material name : " + leftRenderer.sharedMaterial.name);

                    foreach (var mat in leftRenderer.sharedMaterials)
                    {
                        mat.color = Color.white;
                        mat.SetTexture("mainTexture", ScissorsTexture);
                    }
                }

                foreach (var item in ScissorsRight)
                {
                    var left = item.GetComponentInChildren<MeshFilter>();
                    left.sharedMesh = ScissorsRightMesh;
                    left.mesh = ScissorsRightMesh;

                    var rightRenderer = item.GetComponentInChildren<MeshRenderer>();
                    rightRenderer.sharedMaterial.color = Color.white;
                    rightRenderer.sharedMaterial.mainTexture = ScissorsTexture;
                    Logger.LogInfo("material name : " + rightRenderer.sharedMaterial.name);

                    foreach (var mat in rightRenderer.sharedMaterials)
                    {
                        mat.color = Color.white;
                        mat.SetTexture("mainTexture", ScissorsTexture);
                    }
                }


            }
            catch (Exception e)
            {
                Logger.LogInfo(e.ToString());
            }
        }


        /// <summary>
        /// Retrieve a Unity Mesh object by its file path
        /// </summary>
        /// <param name="objFileName"></param>
        /// <returns></returns>
        private Mesh LoadObjByFilePath(string objFileName, GameObject meshObject)
        {
            // Load the .OBJ file from the Resources folder

            meshObject = null;
            //file path
            if (!File.Exists(objFileName))
            {
                Logger.LogInfo("File doesn't exist.");
            }
            else
            {
                if (meshObject != null)
                    Destroy(meshObject);
                try
                {
                    meshObject = new OBJLoader().Load(objFileName, meshObject);
                }
                catch (Exception e)
                {
                    Logger.LogInfo("i done fucked up send this exception to me => Obj loader exception " + e.ToString());
                }

            }
            //throws the reference object in the distance to avoid pink ghosts. after you use it as a reference you can Destroy(obj) it
            meshObject.transform.position = new Vector3(1000, 0, 1000);

            return meshObject.GetComponentInChildren<MeshFilter>().mesh;
        }

        private Texture2D LoadImageByFilePath(string imgFileName)
        {
            Texture2D image = null;
            try
            {
                image = ImageLoader.LoadTexture(imgFileName, 512, 512);

            }
            catch (Exception e)
            {
                Logger.LogInfo(e.ToString());
            }
            return image;
        }

        private void OnDestroy()
        {
            ScissorsButton.Dispose();
            DiscardButton.Dispose();
            ScissorsSprite.Dispose();
            ScissorsSpriteShadow.Dispose();
        }

    }
}
