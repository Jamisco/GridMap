using Assets.Scripts.Miscellaneous;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.WorldMap
{
    public class FusedMesh
    {
        private List<int> MeshHashes;
        // vertex and triangle size
        private List<(int vertexCount, int triangleCount)> MeshSizes;

        private List<Vector3> Vertices;
        private List<int> Triangles;
        private List<Vector2> UVs;
        private List<Color> Colors;

        public int VertexCount { get { return Vertices.Count; } }
        public int TriangleCount { get { return Triangles.Count; } }

        public Mesh Mesh;


        private void Init()
        {
            MeshHashes = new List<int>();
            MeshSizes = new List<(int, int)>();

            Vertices = new List<Vector3>();
            Triangles = new List<int>();
            Colors = new List<Color>();
            UVs = new List<Vector2>();

            Mesh = new Mesh();
            Mesh.MarkDynamic();
        }
        public FusedMesh()
        {
            Init();
        }

        public FusedMesh(List<Mesh> meshes, List<int> hashes, List<Vector3> offsets)
        {
            Init();

            if (!(meshes.Count == hashes.Count && meshes.Count == offsets.Count))
            {
                throw new Exception("List must be thesame size");
            }

            for (int i = 0; i < meshes.Count; i++)
            {
                AddMesh_NoUpdate(meshes[i], hashes[i], offsets[i]);
            }

            UpdateMesh();
        }

        /// <summary>
        /// This constructors uses multithreading to fuse the meshes. This is faster but can cause problems when the meshes are not properly indexed. Make sure your arrays are in order
        /// </summary>
        /// <param name="meshes"></param>
        /// <param name="hashes"></param>
        /// <param name="offsets"></param>
        /// <param name="vertTriIndex"></param>
        /// <param name="totalCounts"></param>
        /// <exception cref="Exception"></exception>
        public FusedMesh(List<MeshData> meshes, List<int> hashes, List<Vector3> offsets,
                         List<(int vertCount, int triStart)> vertTriIndex,
                            (int totalVerts, int triStart) totalCounts)
        {
            Init();

            if (!(meshes.Count == hashes.Count && meshes.Count == offsets.Count
                                               && meshes.Count == vertTriIndex.Count))
            {
                throw new Exception("List must be thesame size");
            }

            FillList();


            bool hasColors = true;
            
            Parallel.For(0, meshes.Count, i =>
            {
                InsertMesh_NoUpdate(meshes[i], hashes[i], offsets[i], vertTriIndex[i], i);
            });
            
            void InsertMesh_NoUpdate(MeshData mesh, int hash, Vector3 offset,
                                    (int vertexCount, int triStart) counts, int index)
            {
                MeshHashes[index] = hash;
                MeshSizes[index] = (mesh.VertexCount, mesh.TriangleCount);

                List<Vector3> hexVertices = new List<Vector3>();
                List<int> hexTris = new List<int>();

                foreach (Vector3 v in mesh.vertices)
                {
                    hexVertices.Add(v + offset);
                }

                foreach (int tri in mesh.triangles)
                {
                    hexTris.Add(tri + counts.vertexCount);
                }


                // we check if the mesh has colors, if it does we add them to the list
                // since colors and vertex count MUST match, we simply check if the count is the same
                // The reason we clear the colors array is because if one mesh has colors and the other doesn't the mesh will be invalid because color count and vertex count must match. Thus, ALl meshes must either have a color or not have one
                if (mesh.colors.Count != hexVertices.Count && hasColors == true)
                {
                    hasColors = false;
                    Colors.Clear();
                }
                            
                int x = 0;
                int start = counts.vertexCount;
                for (int i = start; i < start + hexVertices.Count; i++)
                {
                    Vertices[i] = hexVertices[x];

                    // there might exist an error here if the mesh we are fusing all have colors, but one mesh doesnt have colors. A thread issue my occur where we are tring to access the i index of colors array but it got cleared...this is rare but it might happen
                    if (hasColors)
                    {
                        Colors[i] = mesh.colors[x];
                    }

                    UVs[i] = mesh.uvs[x];

                    x++;

                }

                x = 0;
                for (int i = counts.triStart; i < counts.triStart + hexTris.Count; i++)
                {
                    Triangles[i] = hexTris[x];
                    x++;
                }
            }

            void FillList()
            {
                for (int i = 0; i < meshes.Count; i++)
                {
                    MeshSizes.Add((0, 0));
                    MeshHashes.Add(0);
                }

                for (int i = 0; i < totalCounts.totalVerts; i++)
                {
                    Vertices.Add(Vector3.zero);
                    Colors.Add(Color.white);
                    UVs.Add(Vector2.zero);
                }

                for (int i = 0; i < totalCounts.triStart; i++)
                {
                    Triangles.Add(0);
                }
            }

            UpdateMesh();
        }


        private void AddMesh_NoUpdate(Mesh mesh, int hash, Vector3 offset)
        {
            int index = MeshHashes.IndexOf(hash);

            if (index != -1)
            {
                RemoveMesh(hash, index);
            }

            AddToList(hash, mesh.vertexCount, mesh.triangles.Length);

            AddMeshAtEnd(mesh, offset);
        }

        /// <summary>
        /// Returns true or false if mesh was successfully removed
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private bool RemoveMesh_NoUpdate(int hash, int position = -1)
        {
            // this allows us to skip having to refind the index again
            int index = position == -1 ? MeshHashes.IndexOf(hash) : position;

            if (index != -1)
            {
                var size = MeshSizes[index];

                int triIndex = 0;
                int vertIndex = 0;

                for (int i = 0; i < index; i++)
                {
                    triIndex += MeshSizes[i].triangleCount;
                    vertIndex += MeshSizes[i].vertexCount;
                }

                try
                {
                    Exception e = new Exception("Error when removing mesh");

                    // error might occur if some of the below list are empty.
                    // this might be because they were never filled to begin with
                    Vertices.TryRemoveElementsInRange(vertIndex, size.vertexCount, out e);
                    Triangles.TryRemoveElementsInRange(triIndex, size.triangleCount, out e);
                    Colors.TryRemoveElementsInRange(vertIndex, size.vertexCount, out e);
                    UVs.TryRemoveElementsInRange(vertIndex, size.vertexCount, out e);

                }
                catch (Exception)
                {

                }

                RemoveFromList(index);

                RecalculateTriangles(-size.vertexCount, triIndex);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Fused the given mesh into the new one. Be advised that if you are adding a a mesh with a hash that already exists, the old mesh will be removed and the new one will be added
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="hash"></param>
        /// <param name="offset"></param>
        public void AddMesh(Mesh mesh, int hash, Vector3 offset)
        {
            AddMesh_NoUpdate(mesh, hash, offset);

            UpdateMesh();
        }
        /// <summary>
        /// Returns true or false if mesh was successfully removed
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="position">The position you want to start searching from </param>
        /// <returns></returns>
        public bool RemoveMesh(int hash, int position = -1)
        {
            bool removed = RemoveMesh_NoUpdate(hash, position);

            if (removed)
            {
                UpdateMesh();
            }

            return removed;
        }

        private void AddToList(int hash, int vertexCount, int triangleCount)
        {
            MeshHashes.Add(hash);
            MeshSizes.Add((vertexCount, triangleCount));
        }
        private void RemoveFromList(int index)
        {
            MeshHashes.RemoveAt(index);
            MeshSizes.RemoveAt(index);
        }
        private void AddMeshAtEnd(Mesh aMesh, Vector3 offset)
        {
            List<Vector3> hexVertices = new List<Vector3>();
            List<int> hexTris = new List<int>();

            foreach (Vector3 v in aMesh.vertices)
            {
                hexVertices.Add(v + offset);
            }

            foreach (int tri in aMesh.triangles)
            {
                hexTris.Add(tri + Vertices.Count);
            }

            Vertices.AddRange(hexVertices);
            Triangles.AddRange(hexTris);
            Colors.AddRange(aMesh.colors);
            UVs.AddRange(aMesh.uv);
        }

        private void RecalculateTriangles(int offset, int startIndex = 0)
        {
            for (int i = startIndex; i < Triangles.Count; i++)
            {
                Triangles[i] += offset;
            }
        }

        public bool HasMesh(int hash)
        {
            return MeshHashes.IndexOf(hash) == -1 ? false : true;
        }
        public Mesh GetMesh()
        {
            Mesh mesh = new Mesh();

            mesh.vertices = Vertices.ToArray();

            mesh.triangles = Triangles.ToArray();
            mesh.colors = Colors.ToArray();
            mesh.uv = UVs.ToArray();

            return mesh;
        }

        public void SetMesh(ref Mesh mesh)
        {
            mesh.Clear();

            mesh.vertices = Vertices.ToArray();

            mesh.triangles = Triangles.ToArray();
            mesh.colors = Colors.ToArray();
            mesh.uv = UVs.ToArray();
        }
        private void UpdateMesh()
        {
            //It is important to call Clear before assigning new vertices or triangles. Unity always checks the supplied triangle indices whether they don't reference out of bounds vertices. Calling Clear then assigning vertices then triangles makes sure you never have out of bounds data.

            Mesh.Clear();

            Mesh.vertices = Vertices.ToArray();
            Mesh.triangles = Triangles.ToArray();
            Mesh.colors = Colors.ToArray();
            Mesh.uv = UVs.ToArray();
        }

        public static implicit operator Mesh(FusedMesh f)
        {
            return f.Mesh;
        }
        public static Mesh CombineToSubmesh(List<FusedMesh> subMesh)
        {
            Mesh newMesh = new Mesh();

            CombineInstance[] tempArray = new CombineInstance[subMesh.Count];

            for (int i = 0; i < subMesh.Count; i++)
            {
                CombineInstance subInstance = new CombineInstance();

                subInstance.mesh = subMesh[i];

                tempArray[i] = subInstance;
            }

            newMesh.CombineMeshes(tempArray, false, false);

            return newMesh;
        }

        public static Mesh CombineToSubmesh(List<Mesh> subMesh)
        {
            Mesh newMesh = new Mesh();

            CombineInstance[] tempArray = new CombineInstance[subMesh.Count];

            for (int i = 0; i < subMesh.Count; i++)
            {
                CombineInstance subInstance = new CombineInstance();

                subInstance.mesh = subMesh[i];

                tempArray[i] = subInstance;
            }

            newMesh.CombineMeshes(tempArray, false, false);

            return newMesh;
        }

        public Mesh CombineToSubmesh(Mesh subMesh)
        {
            Mesh newMesh = new Mesh();

            newMesh = Mesh;

            CombineInstance subInstance = new CombineInstance();

            subInstance.mesh = subMesh;

            CombineInstance[] tempArray = new CombineInstance[0];

            newMesh.CombineMeshes(tempArray);

            return newMesh;
        }

        // create a struct to hold MeshData

        public struct MeshData
        {
            public List<Vector3> vertices;
            public List<int> triangles;
            public List<Color> colors;
            public List<Vector2> uvs;

            public int VertexCount { get { return vertices.Count; } }
            public int TriangleCount { get { return triangles.Count; } }

            public MeshData(Mesh data)
            {
                vertices = new List<Vector3>();
                triangles = new List<int>();
                colors = new List<Color>();
                uvs = new List<Vector2>();

                vertices.AddRange(data.vertices);
                triangles.AddRange(data.triangles);
                colors.AddRange(data.colors);
                uvs.AddRange(data.uv);
            }
        }
    }
}
