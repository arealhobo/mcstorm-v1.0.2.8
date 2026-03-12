using System.Numerics;

namespace MCStormViewer;

public record ChunkMesh(float[]? Opaque, float[]? Transparent);

public static class ChunkMesher
{
    // Each vertex: position (3) + color RGBA (4) + normal (3) = 10 floats
    public const int FloatsPerVertex = 10;
    public const int VerticesPerFace = 6; // Two triangles

    public static ChunkMesh BuildChunkMesh(World world, int chunkX, int chunkY, int chunkZ)
    {
        var opaqueVerts = new List<float>(4096);
        var transVerts = new List<float>(1024);

        int startX = chunkX * 16;
        int startY = chunkY * 16;
        int startZ = chunkZ * 16;
        int endX = Math.Min(startX + 16, world.Width);
        int endY = Math.Min(startY + 16, world.Height);
        int endZ = Math.Min(startZ + 16, world.Length);

        for (int y = startY; y < endY; y++)
        for (int z = startZ; z < endZ; z++)
        for (int x = startX; x < endX; x++)
        {
            byte block = world.GetBlock(x, y, z);
            if (block == 0) continue; // Skip air

            Vector3 color = BlockColors.GetColor(block);
            float alpha = BlockColors.GetAlpha(block);
            bool isTransparent = BlockColors.IsTransparent(block);
            var target = alpha < 1.0f ? transVerts : opaqueVerts;

            // Check each face - emit if neighbor is air or transparent (and we're not both transparent of same type)
            // +X face
            if (ShouldDrawFace(world, x + 1, y, z, block, isTransparent))
                AddFace(target, x, y, z, Face.PosX, color, alpha);
            // -X face
            if (ShouldDrawFace(world, x - 1, y, z, block, isTransparent))
                AddFace(target, x, y, z, Face.NegX, color, alpha);
            // +Y face (top)
            if (ShouldDrawFace(world, x, y + 1, z, block, isTransparent))
                AddFace(target, x, y, z, Face.PosY, color, alpha);
            // -Y face (bottom)
            if (ShouldDrawFace(world, x, y - 1, z, block, isTransparent))
                AddFace(target, x, y, z, Face.NegY, color, alpha);
            // +Z face
            if (ShouldDrawFace(world, x, y, z + 1, block, isTransparent))
                AddFace(target, x, y, z, Face.PosZ, color, alpha);
            // -Z face
            if (ShouldDrawFace(world, x, y, z - 1, block, isTransparent))
                AddFace(target, x, y, z, Face.NegZ, color, alpha);
        }

        return new ChunkMesh(
            opaqueVerts.Count > 0 ? opaqueVerts.ToArray() : null,
            transVerts.Count > 0 ? transVerts.ToArray() : null
        );
    }

    private static bool ShouldDrawFace(World world, int nx, int ny, int nz, byte thisBlock, bool thisTransparent)
    {
        byte neighbor = world.GetBlock(nx, ny, nz);
        if (neighbor == 0) return true; // Air neighbor - always draw
        if (!BlockColors.IsTransparent(neighbor)) return false; // Opaque neighbor - don't draw
        // Transparent neighbor: draw if we're opaque, or if we're a different block type
        if (!thisTransparent) return true;
        return !SameTransparentGroup(thisBlock, neighbor);
    }

    private static bool SameTransparentGroup(byte a, byte b)
    {
        if (a == b) return true;
        // Treat flowing water (8) and still water (9) as the same
        if ((a == 8 || a == 9) && (b == 8 || b == 9)) return true;
        // Treat flowing lava (10) and still lava (11) as the same
        if ((a == 10 || a == 11) && (b == 10 || b == 11)) return true;
        return false;
    }

    private enum Face { PosX, NegX, PosY, NegY, PosZ, NegZ }

    private static void AddFace(List<float> verts, int x, int y, int z, Face face, Vector3 color, float alpha)
    {
        // Face shade multiplier for basic ambient occlusion feel
        // Skip directional shading for translucent blocks to avoid visible grid edges
        float shade = alpha < 1.0f ? 1.0f : face switch
        {
            Face.PosY => 1.0f,
            Face.NegY => 0.5f,
            Face.PosX or Face.NegX => 0.8f,
            _ => 0.7f // PosZ, NegZ
        };

        Vector3 c = color * shade;

        Span<float> positions = stackalloc float[18]; // 6 vertices * 3 coords
        Vector3 normal;

        switch (face)
        {
            case Face.PosX:
                normal = new Vector3(1, 0, 0);
                SetQuad(positions, x+1,y,z, x+1,y+1,z, x+1,y+1,z+1, x+1,y,z+1);
                break;
            case Face.NegX:
                normal = new Vector3(-1, 0, 0);
                SetQuad(positions, x,y,z+1, x,y+1,z+1, x,y+1,z, x,y,z);
                break;
            case Face.PosY:
                normal = new Vector3(0, 1, 0);
                SetQuad(positions, x,y+1,z, x,y+1,z+1, x+1,y+1,z+1, x+1,y+1,z);
                break;
            case Face.NegY:
                normal = new Vector3(0, -1, 0);
                SetQuad(positions, x,y,z+1, x,y,z, x+1,y,z, x+1,y,z+1);
                break;
            case Face.PosZ:
                normal = new Vector3(0, 0, 1);
                SetQuad(positions, x+1,y,z+1, x+1,y+1,z+1, x,y+1,z+1, x,y,z+1);
                break;
            case Face.NegZ:
                normal = new Vector3(0, 0, -1);
                SetQuad(positions, x,y,z, x,y+1,z, x+1,y+1,z, x+1,y,z);
                break;
            default:
                return;
        }

        // 6 vertices per face (two triangles)
        for (int i = 0; i < 6; i++)
        {
            verts.Add(positions[i * 3]);
            verts.Add(positions[i * 3 + 1]);
            verts.Add(positions[i * 3 + 2]);
            verts.Add(c.X);
            verts.Add(c.Y);
            verts.Add(c.Z);
            verts.Add(alpha);
            verts.Add(normal.X);
            verts.Add(normal.Y);
            verts.Add(normal.Z);
        }
    }

    private static void SetQuad(Span<float> p,
        float x0, float y0, float z0,
        float x1, float y1, float z1,
        float x2, float y2, float z2,
        float x3, float y3, float z3)
    {
        // Triangle 1: 0,1,2
        p[0] = x0; p[1] = y0; p[2] = z0;
        p[3] = x1; p[4] = y1; p[5] = z1;
        p[6] = x2; p[7] = y2; p[8] = z2;
        // Triangle 2: 0,2,3
        p[9] = x0; p[10] = y0; p[11] = z0;
        p[12] = x2; p[13] = y2; p[14] = z2;
        p[15] = x3; p[16] = y3; p[17] = z3;
    }
}
