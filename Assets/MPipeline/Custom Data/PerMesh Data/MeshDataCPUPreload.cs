using System;
using System.Collections.Generic;
using System.Linq;
using Assets.MPipeline.Custom_Data.PerMesh_Data;
using Assets.MPipeline.Custom_Data.PerMesh_Data.Mesh_Buffer;
using MPipeline.Custom_Data.PerMesh_Data.Mesh_Buffer;
using Unity.Mathematics;
using UnityEngine;

namespace MPipeline.Custom_Data.PerMesh_Data
{ // Odin Package

    public class MeshDataSrcPreload : IPreloadLDDataSource
    {
        public readonly Mesh Mesh;

        public MeshDataSrcPreload(Mesh mesh)
        {
            Mesh = mesh;
        }
    }

    public class MeshDataCPUSrc : MeshDataSrcPreload
    {
        public readonly MeshBufferSource MyMesh;

        public MeshDataCPUSrc(Mesh mesh, MeshBufferSource myMesh) : base(mesh)
        {
            MyMesh = myMesh;
        }
    }

    [CreateAssetMenu(fileName = "MeshDataCPU.asset", menuName = "LDPipeline Data/Mesh/MeshDataCPU", order = 2)]
    public class MeshDataCPUPreload : PerMeshData, ILineDrawingDataPreLoad<MeshDataSrcPreload>
    {
        //------------------------------------------------------------------------------//
        //                                BAKING PROCESS                                //
        //------------------------------------------------------------------------------//
        [ContextMenu("Init Data")]
        public void Bake() // Only for offline baking
        {
            MeshDataSrcPreload srcPreload = new MeshDataSrcPreload(mesh);
            MeshBufferSource myMesh = 
                PerMeshDataPreloadFactory.CreateData<MeshBufferSource>(srcPreload);
            PreloadMeshData(myMesh);
        }
        public void Load(MeshDataSrcPreload srcUnboxed)
        {
            MeshDataCPUSrc src = (MeshDataCPUSrc) srcUnboxed;
            PreloadMeshData(src.MyMesh);
        }

        public override void PreloadMeshData(MeshBufferSource myMesh)
        {
            Mesh meshIn = myMesh.mesh;
            
            base.PreloadMeshData(myMesh);
            Loaded = false;

            // ----------------------------------------------------------------------
            // Checking
            if (myMesh.VertexCount == 0)
            {
                Debug.LogError("Error: Invalid Mesh: Mesh is empty.\n");
                return;
            }

            // ----------------------------------------------------------------------
            // Initializing intermediate data structures for mesh baking

            // Note on fetching data from Mesh class in Unity: ----------------------
            // !!!!!!!! Don't op directly on ANY PROPERTY exposed by mesh class !!!!!!!
            // Instead of doing that, make copies to them before any further ops.

            #region Details

            // -----------------------------------------------------------------------
            // Reason:
            // Mesh class in Unity returns deep copies of its data arrays
            // (including colors, normals, vertices and triangles).
            // That's why these are properties (rather than public fields),
            // and it's why you have to re-assign arrays back to the Mesh after edits;
            //
            // Essentially, you're working on a copy after accessing it.
            // Each property is probably creating a deep copy of the entire array
            // before returning it to the for-loop.
            //
            // That's what generates the huge GC overhead if you access its fields in
            // large loops.
            // -----------------------------------------------------------------------
            // (for ref, see
            // https://answers.unity.com/questions/416049/huge-gc-overhead-when-accessing-trisverts-from-mes.html
            // https://stackoverflow.com/questions/51855127/accessing-mesh-vertices-performance-issue/51856623
            // )

            #endregion

            int bufferType;
            
            // -----------------------------------------------
            // Baking Vertex Data
            // Vertex Position Buffer
            // ---------------------------
            bufferType = MeshBufferPreset.BufferType.VP; // VPList
            MeshBufferExtractor.ExtractFromMesh(myMesh, bufferType, out vpBuffer);
            // Vertex Normal Buffer
            // ---------------------------
            bufferType = MeshBufferPreset.BufferType.VN; // VN List
            MeshBufferExtractor.ExtractFromMesh(myMesh, bufferType, out vnBuffer);
            
            // -----------------------------------------------
            // Baking Edge Data
            // Edge List
            // -----------------
            bufferType = MeshBufferPreset.BufferType.EV;
            MeshBufferExtractor.ExtractFromMesh(myMesh, bufferType, out evBuffer);
            // Edge Triangle List
            // -------------------
            bufferType = MeshBufferPreset.BufferType.ET;
            MeshBufferExtractor.ExtractFromMesh(myMesh, bufferType, out etBuffer);


            // -----------------------------------------------
            // Baking Triangle Data
            // Triangle List
            // --------------------
            bufferType = MeshBufferPreset.BufferType.TV; // TV List
            MeshBufferExtractor.ExtractFromMesh(myMesh, bufferType, out tvBuffer);
            // Triangle Normals
            // ---------------------
            bufferType = MeshBufferPreset.BufferType.TN; // TNList
            MeshBufferExtractor.ExtractFromMesh(myMesh, bufferType, out tnBuffer);
            
            Loaded = true;
        }
        
        public MeshBufferCPU<float4> vpBuffer = null;
        public MeshBufferCPU<float4> vnBuffer = null;

        public MeshBufferCPU<uint> evBuffer = null;
        public MeshBufferCPU<uint> etBuffer = null;

        public MeshBufferCPU<float4> tnBuffer = null;
        public MeshBufferCPU<uint> tvBuffer = null;

        public bool Loaded { get; set; }
    }
}
