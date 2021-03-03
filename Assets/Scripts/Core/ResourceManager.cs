/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;

namespace VRtist
{
    public enum PrefabID
    {
        SunLight,
        SpotLight,
        PointLight,
        Camera,
        Locator
    }

    public enum MaterialID
    {
        ObjectOpaque,
        ObjectTransparent,
        ObjectOpaqueUnlit,
        ObjectTransparentUnlit,
    }

    public class ResourceManager
    {
        readonly Dictionary<PrefabID, string> prefabsMap = new Dictionary<PrefabID, string>();
        readonly Dictionary<PrefabID, GameObject> prefabs = new Dictionary<PrefabID, GameObject>();

        readonly Dictionary<MaterialID, string> materialsMap = new Dictionary<MaterialID, string>();
        readonly Dictionary<MaterialID, Material> materials = new Dictionary<MaterialID, Material>();

        private static ResourceManager _instance;
        public static ResourceManager Instance
        {
            get
            {
                if (null == _instance)
                {
                    _instance = new ResourceManager();
                }
                return _instance;
            }
        }

        private ResourceManager()
        {
            prefabsMap.Add(PrefabID.SunLight, "Prefabs/Sun");
            prefabsMap.Add(PrefabID.SpotLight, "Prefabs/Spot");
            prefabsMap.Add(PrefabID.PointLight, "Prefabs/Point");
            prefabsMap.Add(PrefabID.Camera, "Prefabs/Camera");
            prefabsMap.Add(PrefabID.Locator, "Prefabs/Primitives/Axis_locator");

            materialsMap.Add(MaterialID.ObjectOpaque, "Materials/ObjectOpaque");
            materialsMap.Add(MaterialID.ObjectTransparent, "Materials/ObjectTransparent");
            materialsMap.Add(MaterialID.ObjectOpaqueUnlit, "Materials/ObjectOpaqueUnlit");
            materialsMap.Add(MaterialID.ObjectTransparentUnlit, "Materials/ObjectTransparentUnlit");
        }

        public static GameObject GetPrefab(PrefabID resource)
        {
            if (!Instance.prefabs.TryGetValue(resource, out GameObject prefab))
            {
                Assert.IsTrue(Instance.prefabsMap.ContainsKey(resource));
                prefab = Resources.Load<GameObject>(Instance.prefabsMap[resource]);
                Instance.prefabs.Add(resource, prefab);
            }
            return prefab;
        }

        public static Material GetMaterial(MaterialID resource)
        {
            if (!Instance.materials.TryGetValue(resource, out Material material))
            {
                Assert.IsTrue(Instance.materialsMap.ContainsKey(resource));
                material = Resources.Load<Material>(Instance.materialsMap[resource]);
                Instance.materials.Add(resource, material);
            }
            return material;
        }
    }
}
