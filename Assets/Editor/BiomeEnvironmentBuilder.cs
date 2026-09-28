using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DragonBattle.EditorTools
{
    [InitializeOnLoad]
    internal static class BiomeEnvironmentBuilder
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string GeneratedRootName = "Biome Decorations";
        private const string RebuiltKey = "DragonBattle.BiomeEnvironmentBuilder.SpawnEntrancesRebuilt";

        static BiomeEnvironmentBuilder()
        {
            EditorApplication.delayCall += RebuildOpenSampleScene;
        }

        private static void RebuildOpenSampleScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorPrefs.GetBool(RebuiltKey)) return;
            if (SceneManager.GetActiveScene().path != ScenePath) return;

            if (Build(true))
                EditorPrefs.SetBool(RebuiltKey, true);
        }

        [MenuItem("Tools/Dragon Battle/Build Biome Environment")]
        private static void BuildFromMenu()
        {
            if (Build(true))
                EditorPrefs.SetBool(RebuiltKey, true);
        }

        private static bool Build(bool replaceExisting)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                Debug.LogWarning($"Open {ScenePath} before building the biome environment.");
                return false;
            }

            GameObject environment = GameObject.Find("Enviroment");
            GameObject ground = GameObject.Find("Ground");
            Renderer groundRenderer = ground != null ? ground.GetComponent<Renderer>() : null;
            if (environment == null || groundRenderer == null)
            {
                Debug.LogError("Could not find Enviroment or a Ground renderer in SampleScene.");
                return false;
            }

            Bounds groundBounds = groundRenderer.bounds;
            float edgeX = groundBounds.extents.x - 7f;
            float edgeZ = groundBounds.extents.z - 7f;
            Vector3 center = groundBounds.center;

            Transform existing = environment.transform.Find(GeneratedRootName);
            if (existing != null)
            {
                if (!replaceExisting)
                {
                    return true;
                }

                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            GameObject decorations = new GameObject(GeneratedRootName);
            Undo.RegisterCreatedObjectUndo(decorations, "Build biome environment");
            decorations.transform.SetParent(environment.transform, false);

            GameObject rock = LoadPrefab("PT_Generic_Rock_01 Variant.prefab");
            GameObject ore = LoadPrefab("PT_Ore_Rock_01_split Variant.prefab");
            GameObject deadTree = LoadPrefab("PT_Fruit_Tree_01_dead Variant.prefab");
            GameObject grass = LoadPrefab("PT_Grass_02 Variant.prefab");
            GameObject appleTree = LoadPrefab("PT_Fruit_Tree_01_apples Variant.prefab");

            if (rock == null || ore == null || deadTree == null || grass == null || appleTree == null)
            {
                Undo.DestroyObjectImmediate(decorations);
                return false;
            }

            AddRockPerimeter(decorations.transform, rock, center, edgeX, edgeZ);
            AddWasteland(decorations.transform, ore, deadTree, center);
            AddGreenland(decorations.transform, grass, appleTree, center);
            AddBiomeTransitions(decorations.transform, deadTree, appleTree, grass, ore, center);
            AddCentralTreeRing(decorations.transform, deadTree, appleTree, center);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Built diagonal biomes with a central tree ring open to both spawn routes.");
            return true;
        }

        private static void AddWasteland(Transform parent, GameObject ore, GameObject deadTree, Vector3 center)
        {
            AddDeadGrove(parent, deadTree, center, new Vector2(43f, 18f), 12f);
            AddDeadGrove(parent, deadTree, center, new Vector2(22f, 43f), 285f);
            AddDeadGrove(parent, deadTree, center, new Vector2(55f, 36f), 45f);

            AddOreCluster(parent, ore, center, new Vector2(39f, 31f));
            AddOreCluster(parent, ore, center, new Vector2(58f, 18f));
            AddOreCluster(parent, ore, center, new Vector2(17f, 58f));
        }

        private static void AddGreenland(Transform parent, GameObject grass, GameObject appleTree, Vector3 center)
        {
            AddAppleGrove(parent, appleTree, center, new Vector2(-43f, -18f), 12f);
            AddAppleGrove(parent, appleTree, center, new Vector2(-22f, -43f), 285f);
            AddAppleGrove(parent, appleTree, center, new Vector2(-55f, -36f), 45f);

            AddGrassPatch(parent, grass, center, new Vector2(-35f, -30f));
            AddGrassPatch(parent, grass, center, new Vector2(-58f, -18f));
            AddGrassPatch(parent, grass, center, new Vector2(-18f, -58f));
            AddGrassPatch(parent, grass, center, new Vector2(-48f, -52f));
        }

        private static void AddBiomeTransitions(Transform parent, GameObject deadTree, GameObject appleTree,
            GameObject grass, GameObject ore, Vector3 center)
        {
            // Keep the boundary sparse: native props approach each other without crossing biomes.
            AddAppleGrove(parent, appleTree, center, new Vector2(-43f, 42f), 65f);
            AddDeadGrove(parent, deadTree, center, new Vector2(43f, -42f), 245f);
            AddGrassPatch(parent, grass, center, new Vector2(-52f, 34f));
            AddOreCluster(parent, ore, center, new Vector2(52f, -34f));
        }

        private static void AddCentralTreeRing(Transform parent, GameObject deadTree, GameObject appleTree, Vector3 center)
        {
            const int treeCount = 16;
            const float radius = 21f;

            for (int i = 0; i < treeCount; i++)
            {
                float angleDegrees = i * 360f / treeCount + 11.25f;
                if (Mathf.Abs(Mathf.DeltaAngle(angleDegrees, 45f)) <= 34f ||
                    Mathf.Abs(Mathf.DeltaAngle(angleDegrees, 225f)) <= 34f)
                    continue;

                float angle = angleDegrees * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                GameObject prefab = x + z > 0f ? deadTree : appleTree;
                AddPrefab(parent, prefab, center + new Vector3(x, 0.5f, z), 3f, i * 47f);
            }
        }

        private static void AddDeadGrove(Transform parent, GameObject prefab, Vector3 center, Vector2 groveCenter, float rotation)
        {
            Vector2[] offsets =
            {
                new Vector2(-5f, -2f), new Vector2(-1f, 4f), new Vector2(4f, 1f), new Vector2(2f, -5f)
            };
            for (int i = 0; i < offsets.Length; i++)
            {
                Vector2 point = groveCenter + Rotate(offsets[i], rotation);
                AddPrefab(parent, prefab, center + new Vector3(point.x, 0.5f, point.y), 3f, rotation + i * 73f);
            }
        }

        private static void AddAppleGrove(Transform parent, GameObject prefab, Vector3 center, Vector2 groveCenter, float rotation)
        {
            Vector2[] offsets =
            {
                new Vector2(-5f, -2f), new Vector2(-1f, 4f), new Vector2(4f, 1f), new Vector2(2f, -5f)
            };
            for (int i = 0; i < offsets.Length; i++)
            {
                Vector2 point = groveCenter + Rotate(offsets[i], rotation);
                AddPrefab(parent, prefab, center + new Vector3(point.x, 0.5f, point.y), 3f, rotation + i * 73f);
            }
        }

        private static void AddOreCluster(Transform parent, GameObject prefab, Vector3 center, Vector2 clusterCenter)
        {
            Vector2[] offsets =
            {
                new Vector2(-3f, -1f), new Vector2(2f, 3f), new Vector2(4f, -3f)
            };
            for (int i = 0; i < offsets.Length; i++)
            {
                Vector2 point = clusterCenter + offsets[i];
                AddPrefab(parent, prefab, center + new Vector3(point.x, 0.5f, point.y), 2f, i * 67f);
            }
        }

        private static void AddGrassPatch(Transform parent, GameObject prefab, Vector3 center, Vector2 patchCenter)
        {
            Vector2[] offsets =
            {
                new Vector2(-3f, 0f), new Vector2(1f, 2f), new Vector2(4f, -1f), new Vector2(-1f, -4f)
            };
            for (int i = 0; i < offsets.Length; i++)
            {
                Vector2 point = patchCenter + offsets[i];
                AddPrefab(parent, prefab, center + new Vector3(point.x, 0.5f, point.y), 2f, i * 91f);
            }
        }

        private static Vector2 Rotate(Vector2 point, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(point.x * cos - point.y * sin, point.x * sin + point.y * cos);
        }

        private static GameObject LoadPrefab(string fileName)
        {
            string path = $"Assets/Prefabs/{fileName}";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"Could not find {fileName} in Assets/Prefabs.");
                return null;
            }

            return prefab;
        }

        private static void AddRockPerimeter(Transform parent, GameObject prefab, Vector3 center, float edgeX, float edgeZ)
        {
            float[] positions = { -0.9f, -0.54f, -0.18f, 0.18f, 0.54f, 0.9f };

            for (int i = 0; i < positions.Length; i++)
            {
                float x = positions[i] * edgeX;
                float z = positions[i] * edgeZ;
                AddPrefab(parent, prefab, new Vector3(center.x + x, center.y + 0.1f, center.z + edgeZ), 100f, i * 27f);
                AddPrefab(parent, prefab, new Vector3(center.x + x, center.y + 0.1f, center.z - edgeZ), 100f, i * 41f + 40f);

                if (i > 0 && i < positions.Length - 1)
                {
                    AddPrefab(parent, prefab, new Vector3(center.x + edgeX, center.y + 0.1f, center.z + z), 100f, i * 35f + 90f);
                    AddPrefab(parent, prefab, new Vector3(center.x - edgeX, center.y + 0.1f, center.z + z), 100f, i * 23f + 210f);
                }
            }
        }

        private static void AddPrefab(Transform parent, GameObject prefab, Vector3 position, float scale, float yaw)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            Undo.RegisterCreatedObjectUndo(instance, "Add biome decoration");
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            instance.transform.localScale = Vector3.one * scale;
        }
    }
}