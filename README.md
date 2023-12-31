# Stride Generate Simple Mesh and Marching Cube From Compute Shader - intro

## Introduction

This project aims to provide a practical implementation of generating simple 3D meshes and applying the Marching Cubes algorithm using a Compute Shader in Stride (formerly known as Xenko). It offers a foundation for creating complex voxel-based terrain, medical imaging, or other applications where surface reconstruction from volumetric data is required.

GeneateMesh :
![image](https://github.com/Nicogo1705/Stride-Generate-Simple-Mesh-And-Marching-Cube-From-ComputeShader/assets/20603105/df8b1373-c3ad-42b2-b9b2-a105805d3abb)

GenerateMarchingCube
![image](https://github.com/Nicogo1705/Stride-Generate-Simple-Mesh-And-Marching-Cube-From-ComputeShader/assets/20603105/d5e4067f-8140-40fd-9547-0ffb75137d9b)
![image](https://github.com/Nicogo1705/Stride-Generate-Simple-Mesh-And-Marching-Cube-From-ComputeShader/assets/20603105/70a16eeb-14a4-49bc-a3cc-758791a9ff89)


**Context of Use**

The project finds relevance in various contexts, including but not limited to:

- **3D Visualization:** It enables the conversion of volumetric data, such as medical scans or scientific simulations, into visually appealing 3D models.
- **Game Development:** Game developers can use this technique to create dynamic terrains, destructible environments, or procedural content generation.
- **Scientific Simulations:** Researchers can visualize complex simulations in 3D, facilitating data analysis and comprehension.
- **Computer Graphics:** This project contributes to the field of computer graphics, offering insights into GPU-based mesh generation techniques.

# Informations - Compute shader & Mesh Generation

## GenerateMesh.sdsl

### Overview

The `GenerateMesh` compute shader is designed to create a simple triangle mesh using the Stride (formerly known as Xenko) game engine's compute shader capabilities. This document provides detailed information about the constants, structures, buffers, and methods used in this shader.

### Constants

- `PointCount`: This constant defines the number of voxel points to process in the shader. It is stored in a constant buffer (`cbuffer`) named `param`.

### Structures

#### `VoxelPoint`

- `float3 Position`: A structure representing a voxel point's 3D position.

#### `VertexOutput`

- `float3 Position`: The 3D position of a vertex.
- `float3 Normal`: The normal vector of the vertex.
- `float2 TextureCoordinate`: The texture coordinates of the vertex.

### Buffers

- `VoxelPointBuffer`: This read-write structured buffer contains voxel point data.
- `VertexBuffer`: This read-write structured buffer holds vertex data, including position, normal, and texture coordinates.
- `IndexBuffer`: This read-write structured buffer stores vertex indices.

### Methods

#### `void Compute()`

- This method is the entry point for the compute shader.
- It calculates vertex and index offsets based on the `dispatchThreadID`.
- If the `dispatchThreadID` exceeds `PointCount`, the method returns without performing any further calculations.
- For each voxel point, it generates a simple triangle based on the voxel point's position and normal.
- The generated vertices are stored in the `VertexBuffer` along with their attributes.
- The vertex indices are stored in the `IndexBuffer` to define the triangles.

### Usage

You can customize the mesh generation logic within the `Compute()` method to suit your specific needs. The provided code generates a simple triangle for each voxel point, but you can modify it to create more complex mesh structures.

This compute shader can be integrated into various applications, such as 3D visualization, game development, scientific simulations, and computer graphics, to efficiently generate 3D meshes from voxel data.

## GenerateMeshFromPointsComponent.cs

### Overview

The `GenerateMeshFromPointsComponent` is a C# script designed to work with the Stride game engine. It provides functionality for generating a 3D mesh from voxel points using a compute shader. This script includes data structures, buffers, and methods to facilitate the mesh generation process.

### Structures

#### `VoxelPoint`

- `Vector3 Position`: Represents the 3D position of a voxel point. This structure is used as input data for the shader.

### Fields

- `private VoxelPoint[] VoxelPoints`: An array containing voxel points. You can customize this array to specify the voxel point positions.

- `public float marchingRadius`: A public field that can be set in the Unity Inspector. It determines a radius used in the shader.

### Buffers

- `private Buffer<VoxelPoint> voxelPointBuffer`: A buffer that holds the voxel point data. This buffer is used as input for the compute shader.

- `private Buffer<VertexPositionNormalTexture> vertexBuffer`: A buffer for storing vertex data generated by the compute shader. It is used for rendering.

- `private Buffer<uint> indexBuffer`: A buffer for storing vertex indices generated by the compute shader. It is also used for rendering.

### Methods

#### `public override void Start()`

- This method is called when the script starts.
- It initializes the buffers, sets up the compute effect shader, and creates an entity with a mesh for rendering.

#### `public override void Update()`

- This method is called every frame to update the script's logic.
- It allows you to interactively modify voxel point positions using keyboard input.
- The voxel point buffer is updated with the modified positions.
- The compute shader parameters are set and executed to generate the mesh.
- The shader's unordered access views (UAVs) are unset to release buffer access for rendering.

### Private Method - `UnsetUAV()`

- This method is used internally to unset unordered access views (UAVs) for buffers and textures. It is essential to release buffer access after the compute shader has finished using them.

### Usage

1. Attach this script to a GameObject in your Stride project.
2. Customize the `VoxelPoints` array to define the initial positions of voxel points.
3. Use keyboard input (NumPad keys) to interactively modify voxel point positions during runtime.
4. The mesh generation process occurs in the compute shader.
5. The generated mesh is associated with an entity and rendered in the scene.

# Informations - Marching cube

## GenerateMarchingCube.sdsl

### Overview

The `GenerateMarchingCube` compute shader is designed to create a triangle mesh using the Marching Cubes algorithm within the Stride (formerly known as Xenko) game engine. This shader converts voxel data into a detailed and visually appealing 3D surface representation. This document provides detailed information about the constants, structures, buffers, and methods used in this shader.

### Constants

- `uint SizeX`: The size of the voxel grid along the X-axis.
- `uint SizeY`: The size of the voxel grid along the Y-axis.
- `uint SizeZ`: The size of the voxel grid along the Z-axis.
- `float isoLevel`: The isosurface threshold value.
- `int maxVertices`: The maximum number of vertices the shader can generate.

### Structures

#### `VoxelData`

- `float value`: Represents the value associated with a voxel point.

#### `VertexOutput`

- `float3 Position`: The 3D position of a vertex.
- `float3 Normal`: The normal vector of the vertex.
- `float2 TextureCoordinate`: The texture coordinates of the vertex.

### Static Arrays

- `cornerIndexAFromEdge[12]`: An array defining indices of corner points A for each of the 12 edges of a cube.
- `cornerIndexBFromEdge[12]`: An array defining indices of corner points B for each of the 12 edges of a cube.

### Buffers

- `RWStructuredBuffer<int> edges`: A read-write structured buffer used to store edge data.
- `RWStructuredBuffer<int> triangulation`: A read-write structured buffer used for triangulation data.
- `RWStructuredBuffer<VoxelData> points`: A read-write structured buffer holding voxel data.
- `RWStructuredBuffer<VertexOutput> triangles`: A read-write structured buffer to store generated triangle vertices.
- `RWStructuredBuffer<uint> trianglesCount`: A read-write structured buffer to keep track of the triangle count.

### Methods

#### `void Compute()`

- This method is the entry point for the compute shader.
- It calculates vertex and index offsets based on the dispatchThreadID.
- If the thread ID exceeds the voxel grid dimensions, it returns.
- The algorithm determines the cube index based on the voxel values.
- For each cube, it generates triangles using the Marching Cubes algorithm.
- Triangles are generated by interpolating vertex positions based on the isosurface threshold.
- The resulting vertices are stored in the `triangles` buffer.

#### `bool Same(float3 a, float3 b)`

- A helper method to check if two 3D points are the same.

#### `float3 interpolateVerts(float4 v1, float4 v2)`

- A helper method to interpolate between two 3D vertices based on the isosurface threshold.

#### `uint indexFromCoord(uint3 p)`

- A helper method to calculate the buffer index from voxel grid coordinates.

### Usage

- Attach this compute shader to an appropriate component or entity within your Stride project.
- Set the constants, particularly `SizeX`, `SizeY`, `SizeZ`, `isoLevel`, and `maxVertices` as needed.
- Provide voxel data in the `points` buffer.
- The compute shader will generate triangle vertices and store them in the `triangles` buffer.
- You can then use the generated vertices for rendering and visualization.

## GenerateMeshMarchingCubeComponent.cs

### Overview

The `GenerateMeshMarchingCubeComponent` class is designed to facilitate the generation of a triangle mesh using the Marching Cubes algorithm within the Stride game engine. This component provides functionality for converting voxel data into a detailed 3D surface representation. This document provides a comprehensive overview of the class and its usage.

### Fields

#### `public Vector3 ChunkSize`

- Type: `Vector3`
- Description: An integer vector representing the size of the voxel grid along the X, Y, and Z axes.

#### `public float IsoLevel`

- Type: `float`
- Description: A floating-point value indicating the isosurface threshold level.

#### `public int MaxVectrices`

- Type: `int`
- Description: The maximum number of vertices that the shader can generate. This value is calculated based on `ChunkSize` and is used to allocate buffers.

### Structs

#### `public struct VoxelData`

- Description: A struct representing voxel data, containing a single `float` value.

### Fields and Buffers

- `private VoxelData[] VoxelPoints`: An array of `VoxelData` representing voxel data on the CPU side.

- `private Buffer<int> edges`: A buffer used for holding constant data related to Marching Cubes on the GPU.

- `private Buffer<int> triangulation`: A buffer used for holding constant triangulation data on the GPU.

- `private Buffer<VoxelData> points`: A buffer for storing input voxel data on the GPU.

- `private Buffer<VertexPositionNormalTexture> triangles`: A buffer for storing the output triangle vertices on the GPU.

- `private Buffer<uint> trianglesCount`: A buffer for maintaining a counter of generated triangles on the GPU.

### Methods

#### `private void SetUpRender()`

- Description: Sets up rendering-related objects, including the compute shader and rendering contexts.

#### `private void SetUpVoxelPoints()`

- Description: Initializes the `VoxelPoints` array with voxel data based on the specified `ChunkSize` and `IsoLevel`.

#### `private void SetUpBuffers()`

- Description: Allocates GPU buffers for edges, triangulation, voxel data, triangle vertices, and triangle counts.

#### `private void SetUpParameters()`

- Description: Sets up the parameters for the compute shader, including constants, buffers, and resources.

#### `private void DefineStaticsValues()`

- Description: Defines static values for the `edges` and `triangulation` GPU buffers.

#### `private void DefineDynamicsValues()`

- Description: Updates dynamic GPU data, resets the triangle count, and updates the `points` buffer.

#### `private void SetUpMeshAndEntity()`

- Description: Generates the mesh using the computed triangles and attaches it to a Stride entity for rendering.

#### `public override void Start()`

- Description: Initializes the component by setting up rendering, voxel data, buffers, parameters, and the mesh.

#### `public override void Update()`

- Description: Updates the component, triggering the Marching Cubes computation, binding the vertex shader, and updating GPU buffers. The mesh is only regenerated when the voxel data changes.

### Private Helper Methods

- `private void UnsetUAV(CommandList commandList, ParameterCollection parameters, ParameterKey resourceKey)`: A private helper method for unsetting unordered access views.

### Usage

- Attach this component to a suitable entity within your Stride project.
- Set the `ChunkSize`, `IsoLevel`, and other relevant parameters as needed.
- Initialize the `VoxelPoints` array with your voxel data.
- The Marching Cubes algorithm will generate the mesh on the GPU when the voxel data changes.
- The mesh is automatically attached to the entity for rendering.

## TriangleTable.cs

### Overview

The `TriangleTable` class is a utility class that provides static tables used in the Marching Cubes algorithm for generating triangle meshes from voxel data. These tables are essential for determining the configuration of vertices and triangles during the mesh generation process. This document provides an overview of the class and its purpose.

### Fields

#### `public static int[] edgeTable`

- Type: `int[]`
- Description: An array representing the edge table used in the Marching Cubes algorithm. This table contains 256 integer values that define the edges connecting cube vertices in various configurations.

#### `public static int[] triTable`

- Type: `int[]`
- Description: An array representing the triangulation table used in the Marching Cubes algorithm. This table contains 256x16 integer values that specify the vertex indices forming triangles for each possible cube configuration.

### Usage

The `TriangleTable` class provides precomputed tables that are crucial for the Marching Cubes algorithm's functionality. You can get tables by calling them as they are static.

## ChunkContainer.cs

### Overview

The `ChunkContainer` class is responsible for creating and managing a grid of chunks within a 3D world. Each chunk represents a portion of the world and can contain voxel data. This document provides an overview of the class and its purpose.

### Fields

#### `public Vector3 WorldSize`

- Type: `Vector3`
- Description: A vector specifying the size of the entire world in three dimensions (X, Y, and Z). This field determines the overall size of the world containing the grid of chunks.

#### `public Vector3 ChunkSize`

- Type: `Vector3`
- Description: A vector specifying the size of each individual chunk in the grid. Chunks are blocks within the world, and this field determines their dimensions.

### Methods

#### `public override void Start()`

- Description: This method is called when the script starts executing. Within this method, the class creates a grid of chunks within the world based on the specified `WorldSize` and `ChunkSize` values.

### Usage

The `ChunkContainer` class allows you to create and manage a grid of chunks within your 3D world. Use as a component to an Entity in stride.

## Credits

Special thanks to Tebjan for valuable contributions to render programming, and to Sebastian Lague for insights and resources related to the Marching Cubes algorithm. Sebastian Lague's GitHub repository [here](https://github.com/SebLague/Marching-Cubes/tree/master) and his informative YouTube tutorial [here](https://www.youtube.com/watch?v=M3iI2l0ltbE) have been instrumental in this project's development.

## Notes

This README was written with the assistance of ChatGPT, a language model developed by OpenAI. ( well, i guess it's fair to let the chatbot make it's ad (: )
If you encounter any errors, have suggestions for improvements, please don't hesitate to create an issue. Your feedback and contributions are highly appreciated, and they help improve the quality of this project.
