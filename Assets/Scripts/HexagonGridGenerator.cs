using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class HexagonGridGenerator : MonoBehaviour
{
    [Header("Mesh Properties -------------------------------------------------------------")]
    [Range(1,10)]public int hexagonSize;
    [SerializeField] Material meshMaterial;

    [Header("Grid Properties -------------------------------------------------------------")]
    [SerializeField] int gridScale;
    Texture2D noise2D;
    [Range(0,1)]public float lessThan;
    [SerializeField] float multiplierHeight;
    [SerializeField] bool exclude;
    [SerializeField] int chunks = 1;
    [Range(0.001f,1f)][SerializeField] float updateTime;

    [Header("Noise Properties ------------------------------------------------------------")]
    [Range(1, 3)] [SerializeField] int octaves;
    [Range(0.0001f, 0.001f)] [SerializeField] float noiseScale;
    [SerializeField] bool generate;
    [SerializeField] Vector2 offset;

    [Header("Testing ---------------------------------------------------------------------")]
    [SerializeField] MeshRenderer plane;
    
    [Header("Mesh References -------------------------------------------------------------")]
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private bool isBuilding;


    private void Start()
    {
        StartCoroutine(BuildGrid());
        SetPlaneProperties();
        
        // Get references
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = meshMaterial;

        isBuilding = true;
    }

    private void SetPlaneProperties()
    {
        plane.transform.localScale = Vector3.one * (gridScale / 10);
        NoiseS3D.octaves = octaves;
        noise2D = NoiseS3D.GetNoiseTexture(gridScale * 10, gridScale * 10, offset.x * 100, offset.y * 100, noiseScale, true);
        plane.sharedMaterial.mainTexture = noise2D;
    }


    /// <summary>
    /// Called every time a script value changes
    /// </summary>
    private void OnValidate()
    {
        SetPlaneProperties();
        if (meshFilter != null && generate) { isBuilding = true; }
    }

    IEnumerator BuildGrid()
    {
        while (true)
        {
            if (isBuilding)
            {
                isBuilding = true;
                Vector3[] gridPoints = GetGridPoints(exclude, lessThan, multiplierHeight);
                List<MeshFilter> hexagons = new List<MeshFilter>();
                for (int i = 0; i < gridPoints.Length; i += chunks)
                {
                    for (int c = 0; c < chunks; c++)
                    {
                        hexagons.Add(GenerateHexagonMesh(GetHexagonPoints(gridPoints[i + c], false), i + c));
                    }
                }

                CombineInstance[] combine = new CombineInstance[hexagons.Count];

                int a = 0;
                while (a < hexagons.Count)
                {
                    combine[a].mesh = hexagons[a].mesh;
                    combine[a].transform = hexagons[a].transform.localToWorldMatrix;
                    Destroy(hexagons[a].gameObject);
                    a++;
                }
                meshFilter.mesh = new Mesh();
                meshFilter.mesh.CombineMeshes(combine);
                isBuilding = false;
            }


            yield return new WaitForSeconds(updateTime);
        }
    }

    /// <summary>
    /// Returns the points of the grid. This will be the origin of each hexagon
    /// </summary>
    private Vector3[] GetGridPoints(bool exclude = false, float filter = 0.5f, float heightMultiplier = 1)
    {
        int min = -(gridScale / 2);
        int max = (gridScale / 2);

        List<Vector3> points = new List<Vector3>();
        float yDeviation = (hexagonSize * 4.33f) / 5;
        int count = 0;
        for (float i = min; i < max; i += yDeviation)
        {
            for (float a = min; a < max; a += (hexagonSize * 3))
            {
                float temp = a;
                if (count % 2 != 0) { temp = (a + (hexagonSize * 2) * 0.75f); if (temp > max) { continue; } }

                
                float xP = (temp+(gridScale/2))*1000 / (gridScale * 10);
                float yP = (i+(gridScale/2))*1000 / (gridScale * 10);

                xP = (xP * gridScale * 10) / 100;
                yP = (yP * gridScale * 10) / 100;

                //Debug.Log($"({temp + (gridScale / 2)} : {i + (gridScale / 2)})  |  ({xP} : {yP})");

                float height = noise2D.GetPixel((int)xP, (int)yP).grayscale;
                
                if (exclude)
                {
                    if (height > filter)
                    {
                        height *= heightMultiplier;
                        points.Add(new Vector3(temp, height, i));
                    }
                }
                else
                {
                    height *= heightMultiplier;
                    points.Add(new Vector3(temp, height, i));
                }

            }
            count++;
        }

        return points.ToArray();
    }

    /// <summary>
    /// Returns an array filled with all points of the hexagons with "center" as the origin
    /// </summary>
    private Vector3[] GetHexagonPoints(Vector3 center, bool onlyTop)
    {
        List<Vector3> points = new List<Vector3>();
        points.Add(center);

        for (int i = 1; i < 7; i++)
        {
            float radians = ((60 * i) * Mathf.PI) / 180f;
            float x = hexagonSize * Mathf.Cos(radians);
            float y = hexagonSize * Mathf.Sin(radians);
            points.Add(center + new Vector3(x, 0, y));
        }

        if (!onlyTop)
        {
            for (int l = 0; l < 2; l++)
            {
                points.Add(center);
                for (int i = 1; i < 7; i++)
                {
                    points.Add(points[i]);
                }
                foreach (Vector3 v in GetHexagonPoints(new Vector3(center.x, 0, center.z), true))
                {
                    points.Add(v);
                }
            }
        }
        return points.ToArray();
    }

    /// <summary>
    /// Generates a meshed hexagon given the points of the hexagon, the MeshFilter of the target and the index of the hexagon in case its an array of hexagons
    /// </summary>
    private MeshFilter GenerateHexagonMesh(Vector3[] points, int index = 0)
    {
        GameObject g = new GameObject();
        g.AddComponent<MeshFilter>();
        g.GetComponent<MeshFilter>().mesh = new Mesh();
        g.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();

        // SET VERTICES
        List<Vector3> _vert = new List<Vector3>();
        foreach(Vector3 v in mesh.vertices) { _vert.Add(v); }
        foreach(Vector3 v in points) { _vert.Add(v); }
        mesh.vertices = _vert.ToArray();

        // SET  TRIANGLES
        List<int> _triang = new List<int>();
        foreach(int t in mesh.triangles) { _triang.Add(t); }
        int[] triangles = new int[54] // 54
        {
            // TOP
            2,1,0,

            3,2,0,

            4,3,0,

            5,4,0,

            6,5,0,

            1,6,0,

            // SIDE
            8,9,15,
            15,9,16,

            23,24,30,
            30,24,31,

            10,11,17,
            17,11,18,

            25,26,32,
            32,26,33,

            12,13,19,
            19,13,20,

            27,22,34,
            34,22,29
        };
        foreach (int t in triangles) { _triang.Add(t); }
        mesh.triangles = _triang.ToArray();

        // CALCULATE NORMALS
        mesh.RecalculateNormals();
        g.GetComponent<MeshFilter>().mesh = mesh;
        return g.GetComponent<MeshFilter>();
    }
}
