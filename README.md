# Stride ShaderTest - GenerateMarchingCube

GenerateMarchingCube is a Stride project that demonstrates how to use a compute shader to generate a mesh from a 3D voxel arrary using maching cube algo.


# Stride ShaderTest - GenerateMeshFromPoints

GenerateMeshFromPoints is a Stride project that demonstrates how to use a compute shader to generate a simple triangle mesh from a list of voxel points. This README provides an overview of how the project works and how to make modifications.

## Overview

The project consists of a Stride sync script (`GenerateMeshFromPointsComponent`) and a compute shader (`GenerateMesh`) that work together to generate and render a triangle mesh based on voxel point data. Here's how it works:

1. The `GenerateMeshFromPointsComponent` script initializes and manages the data buffers and the rendering pipeline.

2. The `GenerateMesh` compute shader takes voxel point data as input and generates vertices and indices for a triangle mesh based on the input points.

3. The generated mesh is rendered using Stride's rendering pipeline and can be manipulated in real-time.

## How to Use

### Adding/Removing Parameters

You can add or remove parameters to customize the behavior of the project. For example, to add a new parameter, follow these steps:

1. Define a new parameter in the `GenerateMesh` compute shader's constant buffer:

   ```csharp
   cbuffer param
   {
       float MarchingRadius;
       uint PointCount;
       // Add your new parameter here
       float MyParameter;
   };
   ```

2. In the `GenerateMeshFromPointsComponent` script, update the value of your new parameter in the `Update` method if the parameter can be modified during runtime else, you can set it in `start`:

   ```csharp   
       ComputeShader.Parameters.Set(GenerateMeshKeys.MyParameter, newValue);  
   ```

### Sending Uint Data to the Shader

To send `uint` data to the shader, follow these steps:

1. Create a new buffer for `uint` data in the `GenerateMeshFromPointsComponent` script:

   ```csharp
   private Buffer<uint> uintBuffer;
   ```

2. Initialize the `uintBuffer` and set your data if needed:

   ```csharp
   uint[] uintData = new uint[] { 1, 2, 3, 4, 5 };
   uintBuffer = Buffer.New<uint>(GraphicsDevice, uintData.Length, BufferFlags.StructuredAppendBuffer);
   uintBuffer.SetData(Game.GraphicsContext.CommandList, uintData);
   ```

3. Bind the `uintBuffer` to the compute shader (in start):

   ```csharp
   ComputeShader.Parameters.Set(GenerateMeshKeys.YourUintBuffer, uintBuffer);
   ```

### Conclusion

GenerateMeshFromPoints serves as a starting point for generating and rendering meshes in Stride using compute shaders. Feel free to modify and expand upon it to suit your project's requirements.

## Credits

Special thanks to TebJan for their valuable contributions to this project.
An other thanks for Sebastian Lague (https://github.com/SebLague/Marching-Cubes/tree/master & https://www.youtube.com/watch?v=M3iI2l0ltbE)

