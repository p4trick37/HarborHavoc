using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HarborBattle.Editor
{
    public static class BattleSceneBuilder
    {
        private static Sprite Art(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();

        [MenuItem("Battle Mode/Rebuild Battle Scene")]
        public static void Build()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var cameraObject=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));
            cameraObject.tag="MainCamera";
            cameraObject.transform.position=new Vector3(0,0,-10);
            var camera=cameraObject.GetComponent<Camera>();
            camera.orthographic=true;
            camera.orthographicSize=11.5f;
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.025f,.15f,.22f);
            var manager=new GameObject("Battle Arena").AddComponent<BattleArena>();
            manager.boatArt=Art("Assets/Art/ship 1.png");
            manager.originalPlayerPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            manager.cannonArt=Art("Assets/Art/cannon.png");
            manager.islandArt=Art("Assets/Art/small_island.png");
            manager.pickupArt=Art("Assets/Art/treasureclosed.png");
            manager.shotArt=Art("Assets/Art/Bullets/PlayerShot.png");
            var campaign=EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity",OpenSceneMode.Additive);
            var sourceWater=campaign.GetRootGameObjects().First(obj=>obj.name=="Water");
            var water=new GameObject("Battle Water",typeof(SpriteRenderer));
            SceneManager.MoveGameObjectToScene(water,scene);
            EditorUtility.CopySerialized(sourceWater.GetComponent<SpriteRenderer>(),water.GetComponent<SpriteRenderer>());
            water.transform.position=sourceWater.transform.position;
            water.transform.localScale=sourceWater.transform.localScale;
            EditorSceneManager.CloseScene(campaign,true);
            SceneManager.SetActiveScene(scene);
            var islands=new GameObject("Arena Islands");
            foreach(float y in new[] {-3.8f,3.8f})
            {
                var island=new GameObject("Island",typeof(SpriteRenderer),typeof(CircleCollider2D));
                island.transform.SetParent(islands.transform);
                island.transform.position=new Vector3(0,y,0);
                island.transform.localScale=Vector3.one*(3.5f/manager.islandArt.bounds.size.y);
                var renderer=island.GetComponent<SpriteRenderer>();
                renderer.sprite=manager.islandArt;
                renderer.sharedMaterial=new Material(Shader.Find("Sprites/Default"));
                // Save an asset-backed material so both editor and runtime use the same unlit art.
                const string materialPath="Assets/BattleMode/BattleSprites.mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if(material==null) { material=renderer.sharedMaterial; AssetDatabase.CreateAsset(material,materialPath); }
                renderer.sharedMaterial=material;
                island.GetComponent<CircleCollider2D>().radius=1.55f/island.transform.localScale.x;
            }
            EditorSceneManager.SaveScene(scene,"Assets/BattleMode/BattleArena.unity");
            Debug.Log("Battle scene saved: camera, manager, five project sprite references, two island colliders.");
            if(Application.isBatchMode) EditorSceneManager.CloseScene(scene,true);
        }
    }
}
