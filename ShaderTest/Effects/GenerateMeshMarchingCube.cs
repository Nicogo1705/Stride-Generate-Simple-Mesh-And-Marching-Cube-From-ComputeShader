using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
using BulletSharp;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.ComputeEffect;
using Valve.VR;

namespace ShaderTest.Effects
{
    //https://github.com/SebLague/Marching-Cubes/tree/master
    public class GenerateMeshMarchingCube : SyncScript
    {
        public Vector3 ChunkSize = new Vector3(4, 4, 4);
        public float IsoLevel = 0;
        public int MaxVectrices => (int)ChunkSize.X * (int)ChunkSize.Y * (int)ChunkSize.Z * 5;
        public struct VoxelData
        {
            public float value;
        };

        private VoxelData[] VoxelPoints = new VoxelData[0];
        private Buffer<int> edges; //Const buffer
        private Buffer<int> triangulation; //Const buffer
        private Buffer<VoxelData> points; //input buffer

        private Buffer<VertexPositionNormalTexture> triangles; //output buffer
        private Buffer<uint> trianglesCount; //output buffer


        //See stride doc (needed for compute shader)
        private RenderContext drawEffectContext;
        private ComputeEffectShader ComputeShader;
        private Entity entity;
        private Model model;
        private RenderDrawContext renderDrawContext;

        private void SetUpRender()
        {
            ComputeShader?.Dispose();
            renderDrawContext?.Dispose();

            drawEffectContext = RenderContext.GetShared(Services);
            ComputeShader = new ComputeEffectShader(drawEffectContext) { ShaderSourceName = "GenerateMarchingCube", ThreadGroupCounts = new Int3((int)ChunkSize.X, (int)ChunkSize.Y, (int)ChunkSize.Z), Enabled = true };
            renderDrawContext = new RenderDrawContext(Services, drawEffectContext, Game.GraphicsContext);
        }
        private void SetUpVoxelPoints()
        {
            VoxelPoints = new VoxelData[(int)(ChunkSize.X * ChunkSize.Y * ChunkSize.Z)];
            for (int x = 0; x < ChunkSize.X; x++)
            {
                for (int y = 0; y < ChunkSize.Y; y++)
                {
                    for (int z = 0; z < ChunkSize.Z; z++)
                    {
                        var index = (int)(x + ChunkSize.X * (y + ChunkSize.Y * z));
                        //VoxelPoints[index].position = new Vector3(x, y, z);
                        if (x == 0 || x == ChunkSize.Z - 1)
                            VoxelPoints[index].value = -1;
                        else if (y == 0 || y == ChunkSize.Y - 1)
                            VoxelPoints[index].value = -1;
                        else if (z == 0 || z == ChunkSize.Z - 1)
                            VoxelPoints[index].value = -1;
                        else if (x % 3 == 1)
                            VoxelPoints[index].value = -1;
                        else if (y % 3 == 1)
                            VoxelPoints[index].value = -1;
                        else if (z % 3 == 1)
                            VoxelPoints[index].value = -1;
                        else
                            VoxelPoints[index].value = 1;
                    }
                }
            }
        }
        private void SetUpBuffers()
        {
            if (edges != null)
                edges.Dispose();
            if (triangulation != null)
                triangulation.Dispose();
            if (points != null)
                points.Dispose();
            if (triangles != null)
                triangles.Dispose();
            if (trianglesCount != null)
                trianglesCount.Dispose();
            //alloc buffers
            edges = Buffer.New<int>(GraphicsDevice, TriangleTable.edgeTable.Length, BufferFlags.StructuredAppendBuffer);
            triangulation = Buffer.New<int>(GraphicsDevice, TriangleTable.triTable.Length, BufferFlags.StructuredAppendBuffer);
            points = Buffer.New<VoxelData>(GraphicsDevice, VoxelPoints.Length, BufferFlags.StructuredAppendBuffer);
            triangles = Buffer.New<VertexPositionNormalTexture>(GraphicsDevice, MaxVectrices, BufferFlags.ShaderResource | BufferFlags.UnorderedAccess | BufferFlags.StructuredAppendBuffer);
            trianglesCount = Buffer.New<uint>(GraphicsDevice, 2, BufferFlags.StructuredAppendBuffer);
        }
        private void SetUpParameters()
        {
            ComputeShader.Parameters.Set(GenerateMarchingCubeKeys.SizeX, (uint)ChunkSize.X);
            ComputeShader.Parameters.Set(GenerateMarchingCubeKeys.SizeY, (uint)ChunkSize.Y);
            ComputeShader.Parameters.Set(GenerateMarchingCubeKeys.SizeZ, (uint)ChunkSize.Z);
            ComputeShader.Parameters.Set(GenerateMarchingCubeKeys.isoLevel, IsoLevel);
            ComputeShader.Parameters.Set(GenerateMarchingCubeKeys.maxVectrices, MaxVectrices);

            ComputeShader.Parameters.Set(GenerateMarchingCubeKeys.edges, edges);
            ComputeShader.Parameters.Set(GenerateMarchingCubeKeys.triangulation, triangulation);
            ComputeShader.Parameters.Set(GenerateMarchingCubeKeys.points, points);
            ComputeShader.Parameters.Set(GenerateMarchingCubeKeys.triangles, triangles);
            ComputeShader.Parameters.Set(GenerateMarchingCubeKeys.trianglesCount, trianglesCount);
        }
        private void DefineStaticsValues()
        {
            edges.SetData(Game.GraphicsContext.CommandList, TriangleTable.edgeTable);
            triangulation.SetData(Game.GraphicsContext.CommandList, TriangleTable.triTable);
        }
        private void DefineDynamicsValues()
        {
            points.SetData(Game.GraphicsContext.CommandList, VoxelPoints);
            trianglesCount.SetData(Game.GraphicsContext.CommandList, new uint[] { 0 });

            ComputeShader.Parameters.Set(GenerateMarchingCubeKeys.SizeX, (uint)ChunkSize.X);
            ComputeShader.Parameters.Set(GenerateMarchingCubeKeys.SizeY, (uint)ChunkSize.Y);
            ComputeShader.Parameters.Set(GenerateMarchingCubeKeys.SizeZ, (uint)ChunkSize.Z);
            ComputeShader.Parameters.Set(GenerateMarchingCubeKeys.isoLevel, IsoLevel);
            ComputeShader.Parameters.Set(GenerateMarchingCubeKeys.maxVectrices, MaxVectrices);
        }
        private void SetUpMeshAndEntity()
        {
            if (entity != null)
            {
                SceneSystem.SceneInstance.RootScene.Entities.Remove(entity);
                entity.Dispose();
            }

            //generate the mesh and link it to the vertex & index buffers.
            var mesh = new Mesh
            {
                Draw = new MeshDraw
                {
                    PrimitiveType = PrimitiveType.TriangleList,
                    DrawCount = MaxVectrices,
                    VertexBuffers = new[] { new VertexBufferBinding(triangles, VertexPositionNormalTexture.Layout, MaxVectrices) },
                },
                MaterialIndex = 0,
            };

            // Create an entity and add the mesh to it
            entity = new Entity();
            model = new Model { Meshes = { mesh } };
            model.Materials.Add(Content.Load<Material>("Materials/MaterialBase")); //loading a material
            entity.GetOrCreate<ModelComponent>().Model = model;
            entity.Get<ModelComponent>().IsShadowCaster = true;

            SceneSystem.SceneInstance.RootScene.Entities.Add(entity);
        }

        public override void Start()
        {
            base.Start();
            SetUpRender();
            SetUpVoxelPoints();
            SetUpBuffers();
            SetUpParameters();
            DefineStaticsValues();
            DefineDynamicsValues();
            SetUpMeshAndEntity();
        }
        public override void Update()
        {
            if (Input.IsKeyDown(Stride.Input.Keys.Space))
            {
                return;
            }
            //Buffer counter reset ? 
            //if (trianglesCount != null)
            //    trianglesCount.Dispose();
            //trianglesCount = Buffer.New<uint>(GraphicsDevice, 1, BufferFlags.StructuredAppendBuffer);
            //ComputeShader.Parameters.Set(GenerateMarchingCubeKeys.trianglesCount, trianglesCount);

            trianglesCount.InitialCounterOffset = 0;
            DefineDynamicsValues();

            ComputeShader.Parameters.Set(GenerateMarchingCubeKeys.triangles, triangles);
            ComputeShader.Draw(renderDrawContext); //Compute shader
            UnsetUAV(Game.GraphicsContext.CommandList, ComputeShader.Parameters, GenerateMarchingCubeKeys.triangles); //Unbind !!!!! it will free the acces to the buffer for the render.

            //Debug : Read data from each buffer
            //var a = edges.GetData(Game.GraphicsContext.CommandList);
            //var b = triangulation.GetData(Game.GraphicsContext.CommandList);
            //var c = points.GetData(Game.GraphicsContext.CommandList);
            //var d = triangles.GetData(Game.GraphicsContext.CommandList);
            //var e = trianglesCount.GetData(Game.GraphicsContext.CommandList);
        }

        #region hack tebjan (Thansks a lot!)

        MethodInfo unsetUAV;
        object[] unsetUAVArg = new object[1];
        void UnsetUAV(CommandList commandList, ParameterCollection parameters, ParameterKey resourceKey)
        {
            var gr = parameters?.GetObject(resourceKey);

            GraphicsResource resource = null;
            if (gr is Buffer b)
            {
                if ((b.ViewFlags & BufferFlags.UnorderedAccess) != 0)
                    resource = b;

            }
            else if (gr is Texture t)
            {
                if ((t.ViewFlags & TextureFlags.UnorderedAccess) != 0)
                    resource = t;
            }

            if (resource != null)
            {
                unsetUAV ??= typeof(CommandList).GetMethod("UnsetUnorderedAccessView", BindingFlags.NonPublic | BindingFlags.Instance);
                unsetUAVArg[0] = resource;
                unsetUAV.Invoke(commandList, unsetUAVArg);
            }
        }

        #endregion
    }
}
