// Compute Shader Version of The Grass Painter by MinionsArt
// 1. Some renames
// 2. Added several shortcuts
// 3. Added mesh name
// 4. Mesh component is public
// 5. Remove MeshRenderer component requirement
// Source: https://pastebin.com/xwhSJkFV
// Tutorial: https://www.patreon.com/posts/geometry-grass-46836032

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Only requires mesh filter
[RequireComponent(typeof(MeshFilter))]  // for drawing tool graphics (e.g. solid disc)
[ExecuteInEditMode]
public class GrassPainter : MonoBehaviour
{
    public Mesh mesh;
    private MeshFilter filter;

    [SerializeField]
    List<Vector3> positions = new List<Vector3>();
    [SerializeField]
    List<Color> colors = new List<Color>();
    [SerializeField]
    List<int> indices = new List<int>();
    [SerializeField]
    List<Vector3> normals = new List<Vector3>();
    [SerializeField]
    List<Vector2> grassSizeMultipliers = new List<Vector2>();

    // Grass Limit
    public int grassLimit = 10000;
    public int currentGrassAmount = 0;

    // Paint Status
    public bool painting;
    public bool removing;
    public bool editing;
    public int toolbarInt = 0;

    // Brush Settings
    public LayerMask hitMask = 1;
    public LayerMask paintMask = 1;
    public float brushSize = 1f;
    public float density = 2f;
    public float normalLimit = 1;

    // Grass Size
    public float widthMultiplier = 1f;
    public float heightMultiplier = 1f;

    // Color
    public Color adjustedColor = Color.white;
    public float rangeR, rangeG, rangeB;

    // Stored Values
    [HideInInspector] public Vector3 hitPosGizmo;
    [HideInInspector] public Vector3 hitNormal;

    private Vector3 mousePos;
    private Vector3 hitPos;
    private Vector3 lastPosition = Vector3.zero;

    int[] indi;
#if UNITY_EDITOR
    void OnFocus()
    {
        // Remove delegate listener if it has previously
        // been assigned.
        SceneView.duringSceneGui -= this.OnScene;
        // Add (or re-add) the delegate.
        SceneView.duringSceneGui += this.OnScene;
    }

    void OnDestroy()
    {
        // When the window is destroyed, remove the delegate
        // so that it will no longer do any drawing.
        SceneView.duringSceneGui -= this.OnScene;
    }

    private void OnEnable()
    {
        filter = GetComponent<MeshFilter>();
        SceneView.duringSceneGui += this.OnScene;
    }

    public void ClearMesh()
    {
        currentGrassAmount = 0;
        positions = new List<Vector3>();
        indices = new List<int>();
        colors = new List<Color>();
        normals = new List<Vector3>();
        grassSizeMultipliers = new List<Vector2>();
    }

    void OnScene(SceneView scene)
    {
        // Fix an error
        // https://forum.unity.com/threads/the-object-of-type-x-has-been-destroyed-but-you-are-still-trying-to-access-it.454891/
        if (!this)
        {
            return;
        }

        // only allow painting while this object is selected
        if (Selection.Contains(gameObject))
        {
            Event e = Event.current;

            RaycastHit terrainHit;
            mousePos = e.mousePosition;
            float ppp = EditorGUIUtility.pixelsPerPoint;
            mousePos.y = scene.camera.pixelHeight - mousePos.y * ppp;
            mousePos.x *= ppp;

            // ray for gizmo (disc)
            Ray rayGizmo = scene.camera.ScreenPointToRay(mousePos);
            RaycastHit hitGizmo;

            if (Physics.Raycast(rayGizmo, out hitGizmo, 200f, hitMask.value))
            {
                hitPosGizmo = hitGizmo.point;
            }


            // Events
            if (e.type == EventType.MouseDown && e.button == 2)
            {
                if (e.control)
                {
                    Event.current.Use();
                    toolbarInt = (toolbarInt + 1) % 3;
                }
            }

            // Change Brush Settings
            // -- Brush Size              : Control + Scroll Wheel
            // -- Brush Density           : Alt/Shift/Command + Scroll Wheel
            // -- Grass Height Multiplier : Control + Alt/Shift/Command + Scroll Wheel
            // ----------------------------------------
            // -- Shift + Mouse does not work on macOS
            // -- but Shift + Trackpad works fine
            if (e.type == EventType.ScrollWheel)
            {
                float deltaY = -e.delta.y;

                if (e.control)
                {
                    Event.current.Use();  // ignore zooming in scene

                    if (e.alt || e.command || e.shift)
                        // Change Grass Height Multiplier
                        heightMultiplier += 0.005f * deltaY;
                    else
                        // Change Brush Size
                        brushSize += 0.03f * deltaY;
                }
                else if (e.alt || e.command || e.shift)
                {
                    Event.current.Use();  // ignore zooming in scene

                    // Change Brush Density
                    density += 0.03f * deltaY;
                }
            }


            // when any of the modifier keys is pressed
            bool isModifiedHold = e.control || e.alt || e.shift || e.command;

            // Adding
            // -- In ADDING mode, ANY MODIFIER KEYS + RIGHT BUTTON
            bool isAdding = isModifiedHold && e.type == EventType.MouseDrag && e.button == 1 && toolbarInt == 0;
            if (isAdding)
            {
                // place based on density
                for (int k = 0; k < density; k++)
                {
                    // brush range
                    float t = 2f * Mathf.PI * Random.Range(0f, brushSize);
                    float u = Random.Range(0f, brushSize) + Random.Range(0f, brushSize);
                    float r = (u > 1 ? 2 - u : u);
                    Vector3 origin = Vector3.zero;

                    // place random in radius, except for first one
                    if (k != 0)
                    {
                        origin.x += r * Mathf.Cos(t);
                        origin.y += r * Mathf.Sin(t);
                    }
                    else
                    {
                        origin = Vector3.zero;
                    }

                    // add random range to ray
                    Ray ray = scene.camera.ScreenPointToRay(mousePos);
                    ray.origin += origin;

                    // if the ray hits something thats on the layer mask,
                    // within the grass limit and within the y normal limit

                    if (Physics.Raycast(ray, out terrainHit, 200f, hitMask.value) &&
                        currentGrassAmount < grassLimit &&
                        terrainHit.normal.y <= (1 + normalLimit) &&
                        terrainHit.normal.y >= (1 - normalLimit))
                    {
                        if ((paintMask.value & (1 << terrainHit.transform.gameObject.layer)) > 0)
                        {
                            hitPos = terrainHit.point;
                            hitNormal = terrainHit.normal;
                            if (k != 0)
                            {
                                var grassPosition = hitPos; // + Vector3.Cross(origin, hitNormal);
                                grassPosition -= this.transform.position;

                                positions.Add((grassPosition));
                                indices.Add(currentGrassAmount);
                                grassSizeMultipliers.Add(new Vector2(widthMultiplier, heightMultiplier));
                                // add random color variations                          
                                colors.Add(new Color(
                                    adjustedColor.r + (Random.Range(0, 1.0f) * rangeR),
                                    adjustedColor.g + (Random.Range(0, 1.0f) * rangeG),
                                    adjustedColor.b + (Random.Range(0, 1.0f) * rangeB), 1));

                                //colors.Add(temp);
                                normals.Add(terrainHit.normal);
                                currentGrassAmount++;
                            }
                            else
                            {
                                // to not place everything at once, check if the first placed point far enough away from the last placed first one
                                if (Vector3.Distance(terrainHit.point, lastPosition) > brushSize)
                                {
                                    var grassPosition = hitPos;
                                    grassPosition -= this.transform.position;
                                    positions.Add((grassPosition));
                                    indices.Add(currentGrassAmount);
                                    grassSizeMultipliers.Add(new Vector2(widthMultiplier, heightMultiplier));
                                    colors.Add(new Color(
                                        adjustedColor.r + (Random.Range(0, 1.0f) * rangeR),
                                        adjustedColor.g + (Random.Range(0, 1.0f) * rangeG),
                                        adjustedColor.b + (Random.Range(0, 1.0f) * rangeB), 1));
                                    normals.Add(terrainHit.normal);
                                    currentGrassAmount++;

                                    if (origin == Vector3.zero)
                                    {
                                        lastPosition = hitPos;
                                    }
                                }
                            }
                        }
                    }
                }

                e.Use();
            }

            // removing mesh points
            // -- In REMOVING Mode, ANY MODIFIER KEYS + RIGHT BUTTON
            // -- In ANY modes, ANY MODIFIER KEYS + LEFT BUTTON
            bool isRemoving = isModifiedHold && e.type == EventType.MouseDrag && e.button == 1 && toolbarInt == 1;
            isRemoving |= (e.alt || e.command) && e.type == EventType.MouseDrag && e.button == 0;  // ALT / COMMAND + RIGHT CLICK
            if (isRemoving)
            {
                Event.current.Use();  // ignore selecting in scene

                Ray ray = scene.camera.ScreenPointToRay(mousePos);

                if (Physics.Raycast(ray, out terrainHit, 200f, hitMask.value))
                {
                    hitPos = terrainHit.point;
                    hitPosGizmo = hitPos;
                    hitNormal = terrainHit.normal;
                    for (int j = 0; j < positions.Count; j++)
                    {
                        Vector3 pos = positions[j];

                        pos += this.transform.position;
                        float dist = Vector3.Distance(terrainHit.point, pos);

                        // if its within the radius of the brush, remove all info
                        if (dist <= brushSize)
                        {
                            positions.RemoveAt(j);
                            colors.RemoveAt(j);
                            normals.RemoveAt(j);
                            grassSizeMultipliers.RemoveAt(j);
                            indices.RemoveAt(j);
                            currentGrassAmount--;
                            for (int i = 0; i < indices.Count; i++)
                            {
                                indices[i] = i;
                            }
                        }
                    }
                }

                e.Use();
            }

            // Editing
            // -- In EDITING mode, ANY MODIFIER KEYS + RIGHT BUTTON
            bool isEditing = isModifiedHold && e.type == EventType.MouseDrag && e.button == 1 && toolbarInt == 2;
            if (isEditing)
            {
                Ray ray = scene.camera.ScreenPointToRay(mousePos);

                if (Physics.Raycast(ray, out terrainHit, 200f, hitMask.value))
                {
                    hitPos = terrainHit.point;
                    hitPosGizmo = hitPos;
                    hitNormal = terrainHit.normal;
                    for (int j = 0; j < positions.Count; j++)
                    {
                        Vector3 pos = positions[j];

                        pos += this.transform.position;
                        float dist = Vector3.Distance(terrainHit.point, pos);

                        // if its within the radius of the brush, remove all info
                        if (dist <= brushSize)
                        {
                            colors[j] = (new Color(
                                adjustedColor.r + (Random.Range(0, 1.0f) * rangeR),
                                adjustedColor.g + (Random.Range(0, 1.0f) * rangeG),
                                adjustedColor.b + (Random.Range(0, 1.0f) * rangeB), 1));

                            grassSizeMultipliers[j] = new Vector2(widthMultiplier, heightMultiplier);
                        }
                    }
                }

                e.Use();
            }

            // set all info to mesh
            mesh = new Mesh();
            mesh.name = "Grass Mesh - " + name;
            mesh.SetVertices(positions);
            indi = indices.ToArray();
            mesh.SetIndices(indi, MeshTopology.Points, 0);
            mesh.SetUVs(0, grassSizeMultipliers);
            mesh.SetColors(colors);
            mesh.SetNormals(normals);
            filter.mesh = mesh;
        }
    }
#endif
}