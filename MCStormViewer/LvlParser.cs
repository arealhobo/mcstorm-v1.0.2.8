using System.IO.Compression;

namespace MCStormViewer;

public static class LvlParser
{
    public static World Parse(string path)
    {
        using var fileStream = File.OpenRead(path);
        using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
        using var reader = new BinaryReader(gzipStream);

        var world = new World();

        world.Version = reader.ReadUInt16();
        world.Width = reader.ReadUInt16();
        world.Length = reader.ReadUInt16();
        world.Height = reader.ReadUInt16();

        ushort spawnX = reader.ReadUInt16();
        ushort spawnZ = reader.ReadUInt16();
        ushort spawnY = reader.ReadUInt16();

        world.SpawnX = spawnX;
        world.SpawnY = spawnY;
        world.SpawnZ = spawnZ;

        world.SpawnYaw = reader.ReadByte();
        world.SpawnPitch = reader.ReadByte();

        // visitPerm and buildPerm
        reader.ReadByte();
        reader.ReadByte();

        int totalBlocks = world.Width * world.Length * world.Height;
        world.Blocks = reader.ReadBytes(totalBlocks);

        return world;
    }
}
