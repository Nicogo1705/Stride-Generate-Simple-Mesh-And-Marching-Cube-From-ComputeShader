using System.Reflection;
using System.Runtime.InteropServices;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.ComputeEffect;

namespace ShaderTest.Effects
{
    public class GenerateMeshFromPointsComponent : SyncScript
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct VoxelPoint //input data to give to the shader
        {
            public Vector3 Position;
        };

        private VoxelPoint[] VoxelPoints = new[] { //Default data
                new VoxelPoint() { Position = new Vector3(0, 0, 0) },
                new VoxelPoint() { Position = new Vector3(2, 0, 0) }
        };

        private Buffer<VoxelPoint> voxelPointBuffer; //input buffer
        private Buffer<VertexPositionNormalTexture> vertexBuffer; //output vertexBuffer
        private Buffer<uint> indexBuffer; //output index Buffer

        //See stride doc (needed for compute shader)
        private RenderContext drawEffectContext;
        private ComputeEffectShader ComputeShader;
        private Entity entity;
        private Model model;
        private RenderDrawContext renderDrawContext;

        public override void Start()
        {
            base.Start();

            //alloc buffers
            voxelPointBuffer = Buffer.New<VoxelPoint>(GraphicsDevice, VoxelPoints.Length, BufferFlags.StructuredAppendBuffer);
            vertexBuffer = Buffer.New<VertexPositionNormalTexture>(GraphicsDevice, 100, BufferFlags.ShaderResource | BufferFlags.UnorderedAccess | BufferFlags.RawBuffer);
            indexBuffer = Buffer.New<uint>(GraphicsDevice, 100, BufferFlags.ShaderResource | BufferFlags.UnorderedAccess | BufferFlags.RawBuffer);

            //setting up compute effect
            drawEffectContext = RenderContext.GetShared(Services);
            ComputeShader = new ComputeEffectShader(drawEffectContext) { ShaderSourceName = "GenerateMesh", ThreadGroupCounts = new Int3(10, 1, 1), Enabled = true };
            renderDrawContext = new RenderDrawContext(Services, drawEffectContext, Game.GraphicsContext);

            //bind voxelPointBuffer
            ComputeShader.Parameters.Set(GenerateMeshKeys.VoxelPointBuffer, voxelPointBuffer);

            //generate the mesh and link it to the vertex & index buffers.
            var mesh = new Mesh
            {
                Draw = new MeshDraw
                {
                    PrimitiveType = PrimitiveType.TriangleList,
                    DrawCount = indexBuffer.ElementCount,
                    VertexBuffers = new[] { new VertexBufferBinding(vertexBuffer, VertexPositionNormalTexture.Layout, vertexBuffer.ElementCount) },
                    IndexBuffer = new IndexBufferBinding(indexBuffer, true, indexBuffer.ElementCount),
                },
                MaterialIndex = 0,
            };

            // Create an entity and add the mesh to it
            entity = new Entity();
            model = new Model { Meshes = { mesh } };
            model.Materials.Add(Content.Load<Material>("Materials/MaterialBase")); //loading a material
            entity.GetOrCreate<ModelComponent>().Model = model;
            SceneSystem.SceneInstance.RootScene.Entities.Add(entity);
        }
        public override void Update()
        {
            if (Input.IsKeyDown(Stride.Input.Keys.NumPad8))
            {
                VoxelPoints[0].Position += new Vector3(0, 0.1f, 0);
            }
            if (Input.IsKeyDown(Stride.Input.Keys.NumPad2))
            {
                VoxelPoints[0].Position -= new Vector3(0, 0.1f, 0);
            }
            if (Input.IsKeyDown(Stride.Input.Keys.NumPad6))
            {
                VoxelPoints[0].Position += new Vector3(0.1f, 0, 0);
            }
            if (Input.IsKeyDown(Stride.Input.Keys.NumPad4))
            {
                VoxelPoints[0].Position -= new Vector3(0.1f, 0, 0);
            }
            voxelPointBuffer.SetData(Game.GraphicsContext.CommandList, VoxelPoints); //update dataPoints[]
            ComputeShader.Parameters.Set(GenerateMeshKeys.PointCount, (uint)VoxelPoints.Length); //set field
            ComputeShader.Parameters.Set(GenerateMeshKeys.VertexBuffer, vertexBuffer); //rebind vertextBuffer
            ComputeShader.Parameters.Set(GenerateMeshKeys.IndexBuffer, indexBuffer); //.. IndexBuff

            ComputeShader.Draw(renderDrawContext); //Compute shader

            UnsetUAV(Game.GraphicsContext.CommandList, ComputeShader.Parameters, GenerateMeshKeys.VertexBuffer); //Unbind !!!!! it will free the acces to the buffer for the render.
            UnsetUAV(Game.GraphicsContext.CommandList, ComputeShader.Parameters, GenerateMeshKeys.IndexBuffer); //Saame !!!!!!
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
