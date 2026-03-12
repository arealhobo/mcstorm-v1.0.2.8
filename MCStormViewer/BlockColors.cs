using System.Numerics;

namespace MCStormViewer;

public static class BlockColors
{
    // Whether a block is transparent (air, water, glass, etc. let faces show through)
    private static readonly bool[] _transparent = new bool[256];
    private static readonly Vector3[] _colors = new Vector3[256];

    static BlockColors()
    {
        // Default: opaque gray
        for (int i = 0; i < 256; i++)
            _colors[i] = new Vector3(0.5f, 0.5f, 0.5f);

        // Air
        _transparent[0] = true;
        _colors[0] = Vector3.Zero;

        Set(1, 0.50f, 0.50f, 0.50f); // Stone
        Set(2, 0.36f, 0.67f, 0.27f); // Grass
        Set(3, 0.55f, 0.36f, 0.20f); // Dirt
        Set(4, 0.40f, 0.40f, 0.40f); // Cobblestone
        Set(5, 0.74f, 0.60f, 0.36f); // Planks
        Set(6, 0.30f, 0.60f, 0.15f); // Sapling (transparent)
        _transparent[6] = true;
        Set(7, 0.20f, 0.20f, 0.20f); // Bedrock
        Set(8, 0.24f, 0.41f, 0.85f); // Water (flowing)
        _transparent[8] = true;
        Set(9, 0.24f, 0.41f, 0.85f); // Water (still)
        _transparent[9] = true;
        Set(10, 0.90f, 0.45f, 0.10f); // Lava (flowing)
        Set(11, 0.90f, 0.45f, 0.10f); // Lava (still)
        Set(12, 0.86f, 0.82f, 0.55f); // Sand
        Set(13, 0.55f, 0.50f, 0.45f); // Gravel
        Set(14, 0.60f, 0.55f, 0.30f); // Gold ore
        Set(15, 0.55f, 0.50f, 0.45f); // Iron ore
        Set(16, 0.35f, 0.35f, 0.35f); // Coal ore
        Set(17, 0.45f, 0.30f, 0.15f); // Log
        Set(18, 0.25f, 0.50f, 0.15f); // Leaves
        _transparent[18] = true;
        Set(19, 0.75f, 0.75f, 0.30f); // Sponge
        Set(20, 0.70f, 0.80f, 0.90f); // Glass
        _transparent[20] = true;

        // Wool colors (21-36)
        Set(21, 0.85f, 0.20f, 0.20f); // Red
        Set(22, 0.90f, 0.55f, 0.15f); // Orange
        Set(23, 0.90f, 0.90f, 0.25f); // Yellow
        Set(24, 0.45f, 0.75f, 0.20f); // Lime
        Set(25, 0.25f, 0.70f, 0.25f); // Green
        Set(26, 0.30f, 0.70f, 0.55f); // Aqua green
        Set(27, 0.30f, 0.60f, 0.80f); // Cyan
        Set(28, 0.25f, 0.35f, 0.80f); // Blue
        Set(29, 0.50f, 0.25f, 0.80f); // Purple
        Set(30, 0.40f, 0.15f, 0.60f); // Indigo
        Set(31, 0.60f, 0.30f, 0.65f); // Violet
        Set(32, 0.80f, 0.30f, 0.55f); // Magenta
        Set(33, 0.85f, 0.45f, 0.55f); // Pink
        Set(34, 0.15f, 0.15f, 0.15f); // Black
        Set(35, 0.55f, 0.55f, 0.55f); // Gray
        Set(36, 0.90f, 0.90f, 0.90f); // White

        Set(37, 0.90f, 0.90f, 0.20f); // Dandelion
        _transparent[37] = true;
        Set(38, 0.85f, 0.25f, 0.20f); // Rose
        _transparent[38] = true;
        Set(39, 0.60f, 0.45f, 0.25f); // Brown mushroom
        _transparent[39] = true;
        Set(40, 0.80f, 0.20f, 0.20f); // Red mushroom
        _transparent[40] = true;

        Set(41, 0.95f, 0.85f, 0.30f); // Gold block
        Set(42, 0.80f, 0.80f, 0.80f); // Iron block
        Set(43, 0.55f, 0.55f, 0.55f); // Double slab
        Set(44, 0.55f, 0.55f, 0.55f); // Slab
        Set(45, 0.65f, 0.30f, 0.25f); // Brick
        Set(46, 0.80f, 0.35f, 0.30f); // TNT
        Set(47, 0.55f, 0.40f, 0.25f); // Bookshelf
        Set(48, 0.35f, 0.45f, 0.35f); // Mossy cobblestone
        Set(49, 0.15f, 0.10f, 0.20f); // Obsidian

        // CPE / extended Classic blocks (50-65)
        Set(50, 0.45f, 0.30f, 0.15f); // Cobblestone slab
        Set(51, 0.65f, 0.55f, 0.45f); // Rope
        _transparent[51] = true;
        Set(52, 0.80f, 0.75f, 0.60f); // Sandstone
        Set(53, 0.70f, 0.60f, 0.55f); // Snow
        Set(54, 0.75f, 0.20f, 0.20f); // Fire
        _transparent[54] = true;
        Set(55, 0.60f, 0.40f, 0.25f); // Light brown wool
        Set(56, 0.30f, 0.50f, 0.25f); // Forest green wool
        Set(57, 0.55f, 0.40f, 0.30f); // Maroon wool
        Set(58, 0.25f, 0.35f, 0.55f); // Deep blue wool
        Set(59, 0.30f, 0.25f, 0.40f); // Dark purple wool
        Set(60, 0.25f, 0.55f, 0.55f); // Teal wool
        Set(61, 0.65f, 0.35f, 0.30f); // Salmon wool
        Set(62, 0.10f, 0.10f, 0.10f); // Dark coal
        Set(63, 0.85f, 0.80f, 0.70f); // Light
        Set(64, 0.85f, 0.40f, 0.60f); // Hot pink wool
        Set(65, 0.55f, 0.50f, 0.40f); // Dark gray wool
    }

    private static void Set(int id, float r, float g, float b)
    {
        _colors[id] = new Vector3(r, g, b);
    }

    public static Vector3 GetColor(byte blockId) => _colors[blockId];
    public static bool IsTransparent(byte blockId) => _transparent[blockId];
    public static float GetAlpha(byte blockId) => blockId switch
    {
        8 or 9 => 0.55f,  // Water
        20 => 0.4f,       // Glass
        _ => 1.0f,
    };
}
