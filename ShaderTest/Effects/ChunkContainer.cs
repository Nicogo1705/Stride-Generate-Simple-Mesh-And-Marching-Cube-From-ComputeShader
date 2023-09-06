using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.ComputeEffect;

namespace ShaderTest.Effects
{
    public class ChunkContainer : SyncScript
    {

        public Vector3 WorldSize = new(5, 5, 5);
        public Vector3 ChunkSize = new(32, 32, 32);


        public override void Start()
        {
            base.Start();
            var half = ((WorldSize - 0.99f) / 2);
            half = new Vector3((int)half.X, (int)half.Y, (int)half.Z);
            for (int x = -(int)half.X; x <= (int)half.X; x++)
            {
                for (int y = -(int)half.Y; y <= (int)half.Y; y++)
                {
                    for (int z = -(int)half.Z; z <= (int)half.Z; z++)
                    {
                        var e = new Entity();
                        Entity.AddChild(e);
                        e.Transform.Position = new Vector3(x * ChunkSize.X, y * ChunkSize.Y, z * ChunkSize.Z);
                        e.GetOrCreate<GenerateMeshMarchingCube>().ChunkSize = ChunkSize;
                    }
                }
            }
        }

        public override void Update()
        {
        }
    }

}
