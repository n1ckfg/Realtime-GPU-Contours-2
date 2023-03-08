using System;
using MPipeline.Custom_Data.PerMesh_Data;
using MPipeline.GeometryProcessing;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.MPipeline.Custom_Data.PerMesh_Data
{
    [CreateAssetMenu(fileName = "MeshDataSource.asset", menuName = "LDPipeline Data/Mesh/MeshBufferSource", order = 1)]
    public class MeshBufferSource : ScriptableObject, ILineDrawingDataPreLoad<MeshDataSrcPreload>
    {
        public Mesh mesh;

        public float3[] vertexPosition = null;

        public float3[] vertexNormal = null;

        public int[] vertexEdges = null;
        
        public int[] triangleVerts = null;

        public float4[] triangleNormal = null;

        public int[] triangleTriangles = null;

        public int[] edgeVerts = null;

        public int[] edgeTriangles = null;

        public int VertexCount => vertexPosition.Length;
        public int TriangleListSize => triangleVerts.Length;
        public int TriangleCount => TriangleListSize / 3;
        public int EdgeCount => edgeVerts.Length / 2;

        public int NumEdges => NumNonConcaveEdges + NumConcaveEdges;
        public int NumNonConcaveEdges => NumNormalEdges + NumBoundaryEdges;
        
        // Convex, Non-boundary, or to say, 2-Manifold Edges
        public int NumNormalEdges => numNormalEdges;

        public int NumConcaveEdges => numConcaveEdges;
        public int NumBoundaryEdges => numBoundaryEdges;
        public int NumSingularEdges => numSingularEdges;

        [SerializeField] private int numNormalEdges;
        [SerializeField] private int numConcaveEdges;
        [SerializeField] private int numBoundaryEdges;
        [SerializeField] private int numSingularEdges;
        [SerializeField] private int maxVertexValence;

        [ContextMenu("Extract Data")]
        public void Load()
        {
            if (mesh == null)
            {
                Debug.LogError("Null mesh ref, exit");
                return;
            }

            Load(new MeshDataSrcPreload(mesh));

            Debug.Log("Mesh buffers extracted successfully.");
        }

        public void Load(MeshDataSrcPreload src)
        {
            mesh = src.Mesh;
            if (mesh == null)
            {
                Debug.LogError("null mesh input");
                return;
            }
            
            // Note: make sure that lists initialize in 
            // the correct order.
            // ----------------------------------------
            InitVpList(mesh);
            InitVnList(mesh);

            InitTvList(mesh);
            InitTnList();

            InitEdgeBuffers();

            InitVEList();
        }


        // Utilities ----------------------------------------------------------
        private void InitTnList()
        {
            if (triangleVerts == null)
            {
                Debug.LogError("Null tvlist.");
                return;
            }

            if (vertexPosition == null)
            {
                Debug.LogError("Null vpList.");
                return;
            }

            triangleNormal = TriMeshProcessor.GetTriangleNormalList(triangleVerts, vertexPosition).ToArray();
        }

        private void InitEdgeBuffers()
        {
            TriMeshProcessor.ExtractEdgeBuffers(
                // inputs <==
                vertexPosition,
                triangleVerts,
                triangleNormal,
                // ==> output params
                out numNormalEdges,
                out numConcaveEdges,
                out numBoundaryEdges,
                out numSingularEdges,
                // ==> output buffers
                out edgeVerts,
                out edgeTriangles,
                true);
        }

        private void InitVpList(Mesh mesh)
        {
            Vector3[] vpCopy = mesh.vertices;
            vertexPosition = Array.ConvertAll(
                vpCopy,
                srcVal => (float3) srcVal
            );
        }

        private void InitVnList(Mesh mesh)
        {
            Vector3[] vnCopy = mesh.normals;
            vertexNormal = Array.ConvertAll(
                vnCopy,
                srcVal => (float3) srcVal
            );
        }

        private void InitVEList()
        {
            vertexEdges = TriMeshProcessor.GetVertexAdjEdgeList(edgeVerts, VertexCount, out maxVertexValence);
        }

        private void InitTvList(Mesh mesh)
        {
            triangleVerts = mesh.triangles;
            // Note: !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // TODO: this function leaves some isolated verts,
            // TODO: might have some side effects for later mesh ops,
            // TODO: which needs further investigation
            TriMeshProcessor.MergeVertsOnUVBoundary(ref triangleVerts, vertexPosition);
        }

        public bool Loaded { get; set; }
    }
}