/*  Cave Generator v1
 * 
 *  Date: 2019-04-25
 * 
 *  Made by: Leo McCarthy-Kennedy
 * 
 *  Description:    Generates a 3D voxel cave using cellular automata.
 *                  Cave includes a custom mesh and collisions. UV's
 *                  are not calculated which results in weird lighting
 *                  when using realtime or baked.
 */

using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField, Range(1, 16), Header("Recommended: 8")]
    private int mapSize = 8;

    [SerializeField, Range(0, 100), Header("Recommended: 66")]
    private int fillPercentage = 66;

    [SerializeField, Range(0, 26), Header("Recommended: 12")]
    private int filtering = 12;

    [SerializeField]
    private Material mat;

    private int size;

    private int[,,] map;

    private void Awake()
    {
        size = mapSize * 8;

        GenerateMap();
    }

    private void GenerateMap()
    {
        map = new int[size, size, size];

        // initialize map
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    if (x == 0 || x == size - 1)        // edges to solid
                    {
                        map[x, y, z] = 1;
                    }
                    else if (y == 0 || y == size - 1)   // edges to solid
                    {
                        map[x, y, z] = 1;
                    }
                    else if (z == 0 || z == size - 1)   // edges to solid
                    {
                        map[x, y, z] = 1;
                    }
                    else
                    {
                        map[x, y, z] = Random.Range(0, 100) < fillPercentage ? 1 : 0;   // random fill (1 or 0)
                    }
                }
            }
        }

        // noise filter map
        int[,,] filterMap = new int[size, size, size];

        // create map copy
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    filterMap[x, y, z] = map[x, y, z];
                }
            }
        }

        // filter noise
        for (int i = 0; i < filtering; i++)
        {
            for (int x = 1; x < size - 1; x++)
            {
                for (int y = 1; y < size - 1; y++)
                {
                    for (int z = 1; z < size - 1; z++)
                    {
                        int count = 0;

                        for (int xn = -1; xn < 2; xn++)
                        {
                            for (int yn = -1; yn < 2; yn++)
                            {
                                for (int zn = -1; zn < 2; zn++)
                                {
                                    if ((xn == 0 && yn == 0) && zn == 0)
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        count += map[x + xn, y + yn, z + zn] == 1 ? 1 : 0;
                                    }
                                }
                            }
                        }

                        filterMap[x, y, z] = count > 15 ? 1 : 0;
                    }
                }
            }

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        map[x, y, z] = filterMap[x, y, z];
                    }
                }
            }
        }

        int cur = 2;    // current room
        int max = 2;    // largest room
        int volume = 0; // largest room volume

        // fill rooms
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    if (map[x, y, z] == 0)
                    {
                        int v = FloodFill(x, y, z, cur);

                        if (v > volume)
                        {
                            volume = v;
                            max = cur;
                        }

                        cur++;
                    }
                }
            }
        }

        // removes all but largest room
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    if (map[x, y, z] != 1 && map[x, y, z] != max)
                    {
                        map[x, y, z] = 1;
                    }
                    else if (map[x, y, z] == max)
                    {
                        map[x, y, z] = 0;
                    }
                }
            }
        }

        // logs the volume of the final cave
        Debug.Log(volume);

        BuildMap();
    }

    private void BuildMap()
    {
        if (map == null)
        {
            return;
        }

        GameObject chunks = new GameObject("Chunks");   // chunk parent

        chunks.transform.parent = gameObject.transform;

        // loops for chunk count
        for (int cx = 0; cx < size / 8; cx++)
        {
            for (int cy = 0; cy < size / 8; cy++)
            {
                for (int cz = 0; cz < size / 8; cz++)
                {
                    GameObject chunk = new GameObject("Chunk " + cx + "." + cy + "." + cz); // chunk child

                    chunk.transform.parent = chunks.transform;

                    // adds required components
                    chunk.AddComponent<MeshFilter>();
                    chunk.AddComponent<MeshRenderer>();
                    chunk.AddComponent<MeshCollider>();

                    Mesh mesh = new Mesh();

                    List<Vector3> vertices = new List<Vector3>();   // stores chunk vertices
                    List<int> triangles = new List<int>();          // stores chunk faces

                    int i = 0;  // last vertice added

                    // loops for chunk size
                    for (int x = cx * 8; x < cx * 8 + 8; x++)
                    {
                        for (int y = cy * 8; y < cy * 8 + 8; y++)
                        {
                            for (int z = cz * 8; z < cz * 8 + 8; z++)
                            {
                                // only creates mesh where blocks exist
                                if (map[x, y, z] == 1)
                                {
                                    // adds vertices corresponding to block position
                                    vertices.Add(new Vector3(x, y, z));             // 0
                                    vertices.Add(new Vector3(x, y + 1, z));         // 1
                                    vertices.Add(new Vector3(x, y + 1, z + 1));     // 2
                                    vertices.Add(new Vector3(x, y, z + 1));         // 3
                                    vertices.Add(new Vector3(x + 1, y, z + 1));     // 4
                                    vertices.Add(new Vector3(x + 1, y + 1, z + 1)); // 5
                                    vertices.Add(new Vector3(x + 1, y + 1, z));     // 6
                                    vertices.Add(new Vector3(x + 1, y, z));         // 7

                                    // checks if x face is drawn
                                    if (x - 1 >= 0)
                                    {
                                        if (map[x - 1, y, z] == 0)
                                        {
                                            triangles.Add(i + 2);   // -x
                                            triangles.Add(i + 1);
                                            triangles.Add(i);

                                            triangles.Add(i);       // -x
                                            triangles.Add(i + 3);
                                            triangles.Add(i + 2);
                                        }
                                    }

                                    if (x + 1 < size)
                                    {
                                        if (map[x + 1, y, z] == 0)
                                        {
                                            triangles.Add(i + 7);   // x
                                            triangles.Add(i + 6);
                                            triangles.Add(i + 5);

                                            triangles.Add(i + 5);   // x
                                            triangles.Add(i + 4);
                                            triangles.Add(i + 7);
                                        }
                                    }

                                    // checks if y face is drawn
                                    if (y - 1 >= 0)
                                    {
                                        if (map[x, y - 1, z] == 0)
                                        {
                                            triangles.Add(i + 3);   // -y
                                            triangles.Add(i);
                                            triangles.Add(i + 7);

                                            triangles.Add(i + 7);   // -y
                                            triangles.Add(i + 4);
                                            triangles.Add(i + 3);
                                        }
                                    }

                                    if (y + 1 < size)
                                    {
                                        if (map[x, y + 1, z] == 0)
                                        {
                                            triangles.Add(i + 2);   // y
                                            triangles.Add(i + 5);
                                            triangles.Add(i + 6);

                                            triangles.Add(i + 6);   // y
                                            triangles.Add(i + 1);
                                            triangles.Add(i + 2);
                                        }
                                    }

                                    // checks if z face is drawn
                                    if (z + 1 < size)
                                    {
                                        if (map[x, y, z + 1] == 0)
                                        {
                                            triangles.Add(i + 2);   // z
                                            triangles.Add(i + 3);
                                            triangles.Add(i + 4);

                                            triangles.Add(i + 4);   // z
                                            triangles.Add(i + 5);
                                            triangles.Add(i + 2);
                                        }
                                    }

                                    if (z - 1 >= 0)
                                    {
                                        if (map[x, y, z - 1] == 0)
                                        {

                                            triangles.Add(i + 7);   // -z
                                            triangles.Add(i);
                                            triangles.Add(i + 1);

                                            triangles.Add(i + 1);   // -z
                                            triangles.Add(i + 6);
                                            triangles.Add(i + 7);
                                        }
                                    }

                                    i += 8; // new vertice index
                                }
                            }
                        }
                    }

                    // finalizes mesh

                    mesh.Clear();

                    mesh.SetVertices(vertices);
                    mesh.SetTriangles(triangles, 0);

                    mesh.RecalculateBounds();
                    mesh.RecalculateNormals();

                    chunk.GetComponent<MeshFilter>().sharedMesh = mesh;
                    chunk.GetComponent<Renderer>().material = mat;
                    chunk.GetComponent<MeshCollider>().sharedMesh = mesh;
                }
            }
        }
    }

    private int FloodFill(int x, int y, int z, int fill)
    {
        // volume of room
        int volume = 1;

        // uses BFS flood fill algorithm to fill each room in cave with the room index.

        List<Vector3Int> list = new List<Vector3Int>();

        list.Add(new Vector3Int(x, y, z));

        while (list.Count != 0)
        {
            Vector3Int node = list[list.Count - 1];

            list.RemoveAt(list.Count - 1);

            int a = node.x;
            int b = node.y;
            int c = node.z;

            map[a, b, c] = fill;

            for (int nx = -1; nx < 2; nx++)
            {
                for (int ny = -1; ny < 2; ny++)
                {
                    for (int nz = -1; nz < 2; nz++)
                    {
                        if (map[a + nx, b + ny, c + nz] == 0)
                        {
                            list.Add(new Vector3Int(a + nx, b + ny, c + nz));
                            volume++;
                        }
                    }
                }
            }
        }

        // returns the volume of the room filled
        return volume;
    }
}
