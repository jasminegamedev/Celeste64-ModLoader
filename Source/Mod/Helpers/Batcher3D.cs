using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Celeste64.Mod;

/// <summary>
/// A version of the <see cref="Batcher"/> in 3D instead of 2D.
/// </summary>
public class Batcher3D
{
    /// <summary>
    /// Vertex Format of Batcher.Vertex
    /// </summary>
    private static readonly VertexFormat VertexFormat = VertexFormat.Create<Vertex>(
        new VertexFormat.Element(0, VertexType.Float3, false),
        new VertexFormat.Element(1, VertexType.Float2, false),
        new VertexFormat.Element(2, VertexType.UByte4, true)
    );

    /// <summary>
    /// The Vertex Layout used for Sprite Batching
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vertex(Vec3 position, Vec2 texcoord, Color color) : IVertex
    {
        public Vec3 Pos = position;
        public Vec2 Tex = texcoord;
        public Color Col = color;

        public readonly VertexFormat Format => VertexFormat;
    }

    ~Batcher3D()
    {
        if (vertexPtr != IntPtr.Zero)
            Marshal.FreeHGlobal(vertexPtr);
        if (indexPtr != IntPtr.Zero)
            Marshal.FreeHGlobal(indexPtr);
    }

    private IntPtr vertexPtr = IntPtr.Zero;
    private int vertexCount = 0;
    private int vertexCapacity = 0;

    private IntPtr indexPtr = IntPtr.Zero;
    private int indexCount = 0;
    private int indexCapacity = 0;

    private readonly Mesh mesh = new();
    private readonly Material material = new(Assets.Shaders["Sprite"]);
    private bool dirty = false;

    public void Line(Vec3 from, Vec3 to, Color color, float thickness = 0.1f) => Line(from, to, color, Matrix4x4.Identity, thickness);
    public void Line(Vec3 from, Vec3 to, Color color, Matrix transform, float thickness = 0.1f)
    {
        var normal = (to - from).Normalized();
        // The other vector for the cross product can't be parallel to the normal
        var tangent =  Math.Abs(Vec3.Dot(normal, Vec3.UnitX)) < 0.5f ? Vec3.Cross(normal, Vec3.UnitX) : Vec3.Cross(normal, Vec3.UnitY);
        var bitangent = Vec3.Cross(normal, tangent);

        tangent *= thickness;
        bitangent *= thickness;

        Box(from - tangent - bitangent, from - tangent + bitangent, from + tangent - bitangent, from + tangent + bitangent,
            to - tangent - bitangent, to - tangent + bitangent, to + tangent - bitangent, to + tangent + bitangent,
            color, transform);
    }

    public void Cube(Vec3 center, Color color, float thickness = 0.1f) => Cube(center, color, Matrix4x4.Identity, thickness);
    public void Cube(Vec3 center, Color color, Matrix transform, float thickness = 0.1f)
    {
        Box(center + new Vec3(-thickness, -thickness, -thickness),
            center + new Vec3(thickness, -thickness, -thickness),
            center + new Vec3(-thickness, thickness, -thickness),
            center + new Vec3(thickness, thickness, -thickness),
            center + new Vec3(-thickness, -thickness, thickness),
            center + new Vec3(thickness, -thickness, thickness),
            center + new Vec3(-thickness, thickness, thickness),
            center + new Vec3(thickness, thickness, thickness),
            color, transform
        );
    }

    public void Torus(Vec3 center, float radius, int resolution, Color color, float thickness = 0.1f) => Torus(center, radius, resolution, color, Matrix4x4.Identity, thickness);
    public void Torus(Vec3 center, float radius, int resolution, Color color, Matrix transform, float thickness = 0.1f)
    {
        var points = new Vec3[resolution];

        float angleStep = Calc.TAU / resolution;
        float angle = 0.0f;
        for (int i = 0; i < resolution; i++, angle += angleStep)
        {
            points[i] = new Vec3(Calc.AngleToVector(angle, radius), 0.0f);
        }

        EnsureVertexCapacity(vertexCount + resolution * 4); // 4 vertices each
        EnsureIndexCapacity(indexCount + resolution * 4 * 2 * 3); // 4 faces * 2 triangles * 3 vertices each

        unsafe
        {
            Span<Vertex> vertices = new((Vertex*)vertexPtr + vertexCount, resolution * 4);
            Span<int> indices = new((int*)indexPtr + indexCount, resolution * 4 * 2 * 3);

            for (int i = 0; i < resolution; i++)
            {
                var normal = points[i].Normalized() * thickness;
                var up = new Vec3(0.0f, 0.0f, thickness);

                vertices[i * 4 + 0].Pos = Vec3.Transform(center + points[i] + normal - up, transform);
                vertices[i * 4 + 1].Pos = Vec3.Transform(center + points[i] - normal - up, transform);
                vertices[i * 4 + 2].Pos = Vec3.Transform(center + points[i] + normal + up, transform);
                vertices[i * 4 + 3].Pos = Vec3.Transform(center + points[i] - normal + up, transform);
                vertices[i * 4 + 0].Col = color;
                vertices[i * 4 + 1].Col = color;
                vertices[i * 4 + 2].Col = color;
                vertices[i * 4 + 3].Col = color;
            }

            for (int i = 0; i < resolution; i++)
            {
                int curr = i;
                int prev = i == 0 ? resolution - 1 : i - 1; // Wrap around to the end

                // Bottom
                indices[i * (4 * 2 * 3) + 0] = vertexCount + prev * 4 + 0;
                indices[i * (4 * 2 * 3) + 1] = vertexCount + curr * 4 + 0;
                indices[i * (4 * 2 * 3) + 2] = vertexCount + prev * 4 + 1;
                indices[i * (4 * 2 * 3) + 3] = vertexCount + prev * 4 + 0;
                indices[i * (4 * 2 * 3) + 4] = vertexCount + curr * 4 + 0;
                indices[i * (4 * 2 * 3) + 5] = vertexCount + curr * 4 + 1;
                // Top
                indices[i * (4 * 2 * 3) + 6] = vertexCount + prev * 4 + 2;
                indices[i * (4 * 2 * 3) + 7] = vertexCount + prev * 4 + 3;
                indices[i * (4 * 2 * 3) + 8] = vertexCount + curr * 4 + 3;
                indices[i * (4 * 2 * 3) + 9] = vertexCount + prev * 4 + 2;
                indices[i * (4 * 2 * 3) + 10] = vertexCount + curr * 4 + 3;
                indices[i * (4 * 2 * 3) + 11] = vertexCount + curr * 4 + 2;
                // Outer
                indices[i * (4 * 2 * 3) + 12] = vertexCount + prev * 4 + 0;
                indices[i * (4 * 2 * 3) + 13] = vertexCount + curr * 4 + 2;
                indices[i * (4 * 2 * 3) + 14] = vertexCount + prev * 4 + 2;
                indices[i * (4 * 2 * 3) + 15] = vertexCount + prev * 4 + 0;
                indices[i * (4 * 2 * 3) + 16] = vertexCount + curr * 4 + 0;
                indices[i * (4 * 2 * 3) + 17] = vertexCount + curr * 4 + 2;
                // Inner
                indices[i * (4 * 2 * 3) + 18] = vertexCount + prev * 4 + 1;
                indices[i * (4 * 2 * 3) + 19] = vertexCount + prev * 4 + 3;
                indices[i * (4 * 2 * 3) + 20] = vertexCount + curr * 4 + 3;
                indices[i * (4 * 2 * 3) + 21] = vertexCount + prev * 4 + 1;
                indices[i * (4 * 2 * 3) + 22] = vertexCount + curr * 4 + 3;
                indices[i * (4 * 2 * 3) + 23] = vertexCount + curr * 4 + 1;
            }
        }

        vertexCount += resolution * 4;
        indexCount += resolution * 4 * 2 * 3;
        dirty = true;
    }

    public void Disk(Vec3 center, float radius, int resolution, Color color, float thickness = 0.1f) => Disk(center, radius, resolution, color, Matrix4x4.Identity, thickness);
    public void Disk(Vec3 center, float radius, int resolution, Color color, Matrix transform, float thickness = 0.1f)
    {
        var points = new Vec3[resolution];

        float angleStep = Calc.TAU / resolution;
        float angle = 0.0f;
        for (int i = 0; i < resolution; i++, angle += angleStep)
        {
            points[i] = new Vec3(Calc.AngleToVector(angle, radius), 0.0f);
        }

        EnsureVertexCapacity(vertexCount + resolution * 2 + 2); // 2 vertices each + 2 in the center
        EnsureIndexCapacity(indexCount + resolution * 4 * 3); // 1 faces for outside + 2 triangles on top/bottom = 4 triangles * 3 vertices each

        unsafe
        {
            Span<Vertex> vertices = new((Vertex*)vertexPtr + vertexCount, resolution * 2 + 2);
            Span<int> indices = new((int*)indexPtr + indexCount, resolution * 4 * 3);

            var up = new Vec3(0.0f, 0.0f, thickness);
            vertices[0].Pos = Vec3.Transform(center - up, transform);
            vertices[1].Pos = Vec3.Transform(center + up, transform);
            vertices[0].Col = color;
            vertices[1].Col = color;

            for (int i = 0; i < resolution; i++)
            {
                vertices[(i * 2 + 2) + 0].Pos = Vec3.Transform(center + points[i] - up, transform);
                vertices[(i * 2 + 2) + 1].Pos = Vec3.Transform(center + points[i] + up, transform);
                vertices[(i * 2 + 2) + 0].Col = color;
                vertices[(i * 2 + 2) + 1].Col = color;
            }

            for (int i = 0; i < resolution; i++)
            {
                int curr = i;
                int prev = i == 0 ? resolution - 1 : i - 1; // Wrap around to the end

                // Bottom
                indices[i * (4 * 3) + 0] = vertexCount + (prev * 2 + 2) + 0;
                indices[i * (4 * 3) + 1] = vertexCount + (curr * 2 + 2) + 0;
                indices[i * (4 * 3) + 2] = vertexCount + 0;
                // Top
                indices[i * (4 * 3) + 3] = vertexCount + (curr * 2 + 2) + 1;
                indices[i * (4 * 3) + 4] = vertexCount + (prev * 2 + 2) + 1;
                indices[i * (4 * 3) + 5] = vertexCount + 1;
                // Outer
                indices[i * (4 * 3) + 6] = vertexCount + (curr * 2 + 2) + 1;
                indices[i * (4 * 3) + 7] = vertexCount + (curr * 2 + 2) + 0;
                indices[i * (4 * 3) + 8] = vertexCount + (prev * 2 + 2) + 0;
                indices[i * (4 * 3) + 9] = vertexCount + (curr * 2 + 2) + 1;
                indices[i * (4 * 3) + 10] = vertexCount + (prev * 2 + 2) + 0;
                indices[i * (4 * 3) + 11] = vertexCount + (prev * 2 + 2) + 1;
            }
        }

        vertexCount += resolution * 2 + 2;
        indexCount += resolution * 4 * 3;
        dirty = true;
    }

    public void Sphere(Vec3 center, float radius, int resolution, Color color) => Sphere(center, radius, resolution, color, Matrix4x4.Identity);
    public void Sphere(Vec3 center, float radius, int resolution, Color color, Matrix transform)
    {
        // Taken and adapted from Utils.CreateSphere()
        int stackCount = resolution;
        int sliceCount = resolution;

        EnsureVertexCapacity(vertexCount + 2 + (stackCount - 1) * sliceCount);
        EnsureIndexCapacity(indexCount + sliceCount * 6 + (stackCount - 2) * sliceCount * 6);

        unsafe
        {
            Span<Vertex> vertices = new((Vertex*)vertexPtr + vertexCount, 2 + (stackCount - 1) * sliceCount);
            Span<int> indices = new((int*)indexPtr + indexCount, sliceCount * 6 + (stackCount - 2) * sliceCount * 6);

            int vtx = 0;
            int idx = 0;

            // Add top vertex
            int v0 = vertexCount + vtx;
            vertices[vtx++].Pos = center + new Vec3(0.0f, 0.0f, radius);

            // Generate vertices per stack / slice
            for (int i = 0; i < stackCount - 1; i++)
            {
                float phi = MathF.PI * (i + 1) / (float)(stackCount);
                for (int j = 0; j < sliceCount; j++)
                {
                    float theta = 2.0f * MathF.PI * (j) / (float)(sliceCount);
                    float x = radius * MathF.Sin(phi) * MathF.Cos(theta);
                    float y = radius * MathF.Sin(phi) * MathF.Sin(theta);
                    float z = radius * MathF.Cos(phi);
                    vertices[vtx++].Pos = center + new Vec3(x, y, z);
                }
            }

            // Add bottom vertex
            int v1 = vertexCount + vtx;
            vertices[vtx++].Pos = center + new Vec3(0.0f, 0.0f, -radius);

            // Fill-in color
            for (int i = 0; i < vtx; i++)
                vertices[i].Col = color;

            // Add top / bottom triangles
            for (int i = 0; i < sliceCount; ++i)
            {
                int i0 = i + 1;
                int i1 = (i + 1) % sliceCount + 1;
                indices[idx++] = v0;
                indices[idx++] = vertexCount + i1;
                indices[idx++] = vertexCount + i0;

                i0 = i + sliceCount * (stackCount - 2) + 1;
                i1 = (i + 1) % sliceCount + sliceCount * (stackCount - 2) + 1;
                indices[idx++] = v1;
                indices[idx++] = vertexCount + i0;
                indices[idx++] = vertexCount + i1;
            }

            // Add quads per stack / slice
            for (int j = 0; j < stackCount - 2; j++)
            {
                int j0 = j * sliceCount + 1;
                int j1 = (j + 1) * sliceCount + 1;
                for (int i = 0; i < sliceCount; i++)
                {
                    int i0 = j0 + i;
                    int i1 = j0 + (i + 1) % sliceCount;
                    int i2 = j1 + (i + 1) % sliceCount;
                    int i3 = j1 + i;
                    indices[idx++] = vertexCount + i0;
                    indices[idx++] = vertexCount + i1;
                    indices[idx++] = vertexCount + i2;
                    indices[idx++] = vertexCount + i0;
                    indices[idx++] = vertexCount + i2;
                    indices[idx++] = vertexCount + i3;
                }
            }

            vertexCount += vtx;
            indexCount += idx;
            dirty = true;
        }
    }

    public void Box(Vec3 min, Vec3 max, Color color) => Box(min, max, color, Matrix.Identity);
    public void Box(Vec3 min, Vec3 max, Color color, Matrix transform) =>
        Box(min with { Z = max.Z },
            min with { X = max.X, Z = max.Z },
            min,
            min with { X = max.X },

            max with { X = min.X },
            max,
            max with { X = min.X, Z = min.Z },
            max with { Z = min.Z },
            color, transform);

    /// <summary>
    /// Renders a box of a solid color.
    /// </summary>
    /// <param name="v0">Front Top Left</param>
    /// <param name="v1">Front Top Right</param>
    /// <param name="v2">Front Bottom Left</param>
    /// <param name="v3">Front Bottom Right</param>
    /// <param name="v4">Back Top Left</param>
    /// <param name="v5">Back Top Right</param>
    /// <param name="v6">Back Bottom Left</param>
    /// <param name="v7">Back Bottom Right</param>
    /// <param name="color">Box color</param>
    public void Box(Vec3 v0, Vec3 v1, Vec3 v2, Vec3 v3,
                    Vec3 v4, Vec3 v5, Vec3 v6, Vec3 v7,
                    Color color, Matrix transform)
    {
        EnsureVertexCapacity(vertexCount + 8);
        EnsureIndexCapacity(indexCount + 6 * 2 * 3); // 6 faces * 2 triangles * 3 vertices

        unsafe
        {
            Span<Vertex> vertices = new((Vertex*)vertexPtr + vertexCount, 8);
            Span<int> indices = new((int*)indexPtr + indexCount, 36);

            vertices[0].Pos = Vec3.Transform(v0, transform);
            vertices[1].Pos = Vec3.Transform(v1, transform);
            vertices[2].Pos = Vec3.Transform(v2, transform);
            vertices[3].Pos = Vec3.Transform(v3, transform);
            vertices[4].Pos = Vec3.Transform(v4, transform);
            vertices[5].Pos = Vec3.Transform(v5, transform);
            vertices[6].Pos = Vec3.Transform(v6, transform);
            vertices[7].Pos = Vec3.Transform(v7, transform);
            vertices[0].Col = color;
            vertices[1].Col = color;
            vertices[2].Col = color;
            vertices[3].Col = color;
            vertices[4].Col = color;
            vertices[5].Col = color;
            vertices[6].Col = color;
            vertices[7].Col = color;

            // Front
            indices[0] = vertexCount + 0;
            indices[1] = vertexCount + 2;
            indices[2] = vertexCount + 1;
            indices[3] = vertexCount + 2;
            indices[4] = vertexCount + 3;
            indices[5] = vertexCount + 1;
            // Back
            indices[6] = vertexCount + 4;
            indices[7] = vertexCount + 5;
            indices[8] = vertexCount + 6;
            indices[9] = vertexCount + 5;
            indices[10] = vertexCount + 7;
            indices[11] = vertexCount + 6;
            // Left
            indices[12] = vertexCount + 0;
            indices[13] = vertexCount + 6;
            indices[14] = vertexCount + 2;
            indices[15] = vertexCount + 0;
            indices[16] = vertexCount + 4;
            indices[17] = vertexCount + 6;
            // Right
            indices[18] = vertexCount + 1;
            indices[19] = vertexCount + 3;
            indices[20] = vertexCount + 7;
            indices[21] = vertexCount + 1;
            indices[22] = vertexCount + 7;
            indices[23] = vertexCount + 5;
            // Top
            indices[24] = vertexCount + 0;
            indices[25] = vertexCount + 1;
            indices[26] = vertexCount + 5;
            indices[27] = vertexCount + 0;
            indices[28] = vertexCount + 5;
            indices[29] = vertexCount + 4;
            // Bottom
            indices[30] = vertexCount + 2;
            indices[31] = vertexCount + 7;
            indices[32] = vertexCount + 6;
            indices[33] = vertexCount + 2;
            indices[34] = vertexCount + 3;
            indices[35] = vertexCount + 7;
        }

        vertexCount += 8;
        indexCount += 6 * 2 * 3;
        dirty = true;
    }

    /// <summary>
    /// Draws the Batcher3D to the given Target with the given RenderState
    /// </summary>
    public void Render(ref RenderState state)
    {
        if (indexPtr == IntPtr.Zero || vertexPtr == IntPtr.Zero)
            return;

        // Upload our data if we've been modified since the last time we rendered
        if (dirty)
        {
            mesh.SetIndices(indexPtr, indexCount, IndexFormat.ThirtyTwo);
            mesh.SetVertices(vertexPtr, vertexCount, VertexFormat);
            dirty = false;
        }

        if (material.Shader?.Has("u_matrix") ?? false)
            material.Set("u_matrix", state.Camera.ViewProjection);
        if (material.Shader?.Has("u_far") ?? false)
            material.Set("u_far", state.Camera.FarPlane);
        if (material.Shader?.Has("u_near") ?? false)
            material.Set("u_near", state.Camera.NearPlane);
        if (material.Shader?.Has("u_texture") ?? false)
            material.Set("u_texture", Assets.Textures["white"]);

        var call = new DrawCommand(state.Camera.Target, mesh, material)
        {
            // BlendMode = BlendMode.Screen,
            DepthCompare = state.DepthCompare,
            DepthMask = state.DepthMask,
            // DepthMask = false,
            // DepthCompare = DepthCompare.Less,
            CullMode = CullMode.None,
            MeshIndexStart = 0,
            MeshIndexCount = indexCount,
        };
        call.Submit();
        state.Calls++;
        state.Triangles += indexCount / 3;
    }

    /// <summary>
    /// Clears the Batcher3D.
    /// </summary>
    public void Clear()
    {
        vertexCount = 0;
        indexCount = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void EnsureVertexCapacity(int capacity)
    {
        if (capacity < vertexCapacity) return;

        if (vertexCapacity == 0)
            vertexCapacity = 32;

        while (capacity >= vertexCapacity)
            vertexCapacity *= 2;

        IntPtr newPtr = Marshal.AllocHGlobal(sizeof(Vertex) * vertexCapacity);

        if (vertexCount > 0)
            Buffer.MemoryCopy((void*)vertexPtr, (void*)newPtr, vertexCapacity * sizeof(Vertex), vertexCount * sizeof(Vertex));

        if (vertexPtr != IntPtr.Zero)
            Marshal.FreeHGlobal(vertexPtr);

        vertexPtr = newPtr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void EnsureIndexCapacity(int capacity)
    {
        if (capacity < indexCapacity) return;

        if (indexCapacity == 0)
            indexCapacity = 32;

        while (capacity >= indexCapacity)
            indexCapacity *= 2;

        IntPtr newPtr = Marshal.AllocHGlobal(sizeof(int) * indexCapacity);

        if (indexCount > 0)
            Buffer.MemoryCopy((void*)indexPtr, (void*)newPtr, indexCapacity * sizeof(int), indexCount * sizeof(int));

        if (indexPtr != IntPtr.Zero)
            Marshal.FreeHGlobal(indexPtr);

        indexPtr = newPtr;
    }
}
