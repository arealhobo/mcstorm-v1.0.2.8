namespace MCStormViewer;

public class World
{
    public ushort Version { get; set; }
    public int Width { get; set; }  // X
    public int Length { get; set; } // Z
    public int Height { get; set; } // Y
    public float SpawnX { get; set; }
    public float SpawnY { get; set; }
    public float SpawnZ { get; set; }
    public byte SpawnYaw { get; set; }
    public byte SpawnPitch { get; set; }
    public byte[] Blocks { get; set; } = Array.Empty<byte>();

    public byte GetBlock(int x, int y, int z)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height || z < 0 || z >= Length)
            return 0; // Air outside bounds
        return Blocks[(y * Length + z) * Width + x];
    }

    public int ChunksX => (Width + 15) / 16;
    public int ChunksY => (Height + 15) / 16;
    public int ChunksZ => (Length + 15) / 16;
}
