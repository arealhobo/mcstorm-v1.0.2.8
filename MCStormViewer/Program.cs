using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.OpenGL;

namespace MCStormViewer;

public class Program
{
    private static IWindow? _window;
    private static GL? _gl;
    private static Renderer? _renderer;
    private static Camera? _camera;
    private static IInputContext? _inputContext;
    private static IMouse? _mouse;
    private static IKeyboard? _keyboard;

    private static bool _mouseCaptured = true;
    private static Vector2 _lastMousePos;
    private static bool _firstMouse = true;

    private static string? _levelsPath;
    private static World? _currentWorld;

    public static void Main(string[] args)
    {
        // Find the levels directory
        _levelsPath = FindLevelsPath();
        if (_levelsPath == null)
        {
            Console.WriteLine("Could not find 'levels' directory. Place this program near your MCStorm installation.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;
        }

        while (true)
        {
            string? worldPath = SelectWorld();
            if (worldPath == null) break;

            Console.WriteLine($"Loading {Path.GetFileName(worldPath)}...");

            try
            {
                _currentWorld = LvlParser.Parse(worldPath);
                Console.WriteLine($"  World: {_currentWorld.Width}x{_currentWorld.Height}x{_currentWorld.Length}");
                Console.WriteLine($"  Spawn: ({_currentWorld.SpawnX}, {_currentWorld.SpawnY}, {_currentWorld.SpawnZ})");
                Console.WriteLine("  Meshing chunks...");

                LaunchViewer();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading world: {ex.Message}");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }
    }

    private static string? FindLevelsPath()
    {
        // Search relative to the exe and common parent directories
        string exeDir = AppContext.BaseDirectory;
        string[] searchPaths = new[]
        {
            Path.Combine(exeDir, "levels"),
            Path.Combine(exeDir, "..", "levels"),
            Path.Combine(exeDir, "..", "..", "levels"),
            Path.Combine(exeDir, "..", "..", "..", "levels"),
            Path.Combine(exeDir, "..", "..", "..", "..", "levels"),
            Path.Combine(exeDir, "..", "..", "..", "..", "..", "levels"),
        };

        foreach (var p in searchPaths)
        {
            string full = Path.GetFullPath(p);
            if (Directory.Exists(full))
                return full;
        }

        return null;
    }

    private static string? SelectWorld()
    {
        var files = Directory.GetFiles(_levelsPath!, "*.lvl")
            .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (files.Length == 0)
        {
            Console.WriteLine("No .lvl files found in levels directory.");
            return null;
        }

        Console.WriteLine();
        Console.WriteLine("=== MCStorm World Viewer ===");
        Console.WriteLine($"Found {files.Length} worlds in: {_levelsPath}");
        Console.WriteLine();

        // Paginated display
        int pageSize = 20;
        int page = 0;
        int totalPages = (files.Length + pageSize - 1) / pageSize;

        while (true)
        {
            int start = page * pageSize;
            int end = Math.Min(start + pageSize, files.Length);

            for (int i = start; i < end; i++)
            {
                string name = Path.GetFileNameWithoutExtension(files[i]);
                Console.WriteLine($"  {i + 1,4}. {name}");
            }

            Console.WriteLine();
            Console.WriteLine($"Page {page + 1}/{totalPages} | Enter number to load, N/P for next/prev page, Q to quit");
            Console.Write("> ");
            string? input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input) || input.Equals("q", StringComparison.OrdinalIgnoreCase))
                return null;

            if (input.Equals("n", StringComparison.OrdinalIgnoreCase))
            {
                if (page < totalPages - 1) page++;
                continue;
            }
            if (input.Equals("p", StringComparison.OrdinalIgnoreCase))
            {
                if (page > 0) page--;
                continue;
            }

            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= files.Length)
                return files[choice - 1];

            Console.WriteLine("Invalid selection.");
        }
    }

    private static void LaunchViewer()
    {
        var options = WindowOptions.Default;
        options.Title = $"MCStorm Viewer - {_currentWorld!.Width}x{_currentWorld.Height}x{_currentWorld.Length}";
        options.Size = new Vector2D<int>(1280, 720);
        options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(3, 3));

        _window = Window.Create(options);
        _firstMouse = true;

        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Resize += OnResize;
        _window.Closing += OnClosing;

        _window.Run();
        _window.Dispose();
    }

    private static void OnLoad()
    {
        _gl = _window!.CreateOpenGL();
        _renderer = new Renderer(_gl);
        _renderer.Initialize();

        // Set up input
        _inputContext = _window!.CreateInput();
        _keyboard = _inputContext.Keyboards.FirstOrDefault();
        _mouse = _inputContext.Mice.FirstOrDefault();

        if (_mouse != null)
        {
            _mouse.Cursor.CursorMode = CursorMode.Raw;
            _mouse.MouseMove += OnMouseMove;
            _mouse.Scroll += OnScroll;
        }

        // Camera at spawn position (add 1.7 for eye height)
        _camera = new Camera(new Vector3(
            _currentWorld!.SpawnX + 0.5f,
            _currentWorld.SpawnY + 1.7f,
            _currentWorld.SpawnZ + 0.5f
        ));

        // Mesh all chunks
        int totalChunks = _currentWorld.ChunksX * _currentWorld.ChunksY * _currentWorld.ChunksZ;
        int meshed = 0;

        for (int cy = 0; cy < _currentWorld.ChunksY; cy++)
        for (int cz = 0; cz < _currentWorld.ChunksZ; cz++)
        for (int cx = 0; cx < _currentWorld.ChunksX; cx++)
        {
            var mesh = ChunkMesher.BuildChunkMesh(_currentWorld, cx, cy, cz);
            if (mesh.Opaque != null || mesh.Transparent != null)
                _renderer.UploadChunk(mesh);
            meshed++;
            if (meshed % 100 == 0)
                Console.Write($"\r  Meshing: {meshed}/{totalChunks} chunks...");
        }
        Console.WriteLine($"\r  Meshing complete: {totalChunks} chunks processed.    ");

        // Set fog distance based on world size
        float maxDim = Math.Max(_currentWorld.Width, Math.Max(_currentWorld.Height, _currentWorld.Length));
        _renderer.FogEnd = maxDim * 1.5f;
        _renderer.FogStart = _renderer.FogEnd * 0.4f;
    }

    private static void OnUpdate(double dt)
    {
        float deltaTime = (float)dt;

        if (_keyboard == null || _camera == null) return;

        if (_keyboard.IsKeyPressed(Key.Escape))
        {
            if (_mouseCaptured)
            {
                _mouseCaptured = false;
                if (_mouse != null)
                    _mouse.Cursor.CursorMode = CursorMode.Normal;
            }
            else
            {
                _window!.Close();
                return;
            }
        }

        if (_keyboard.IsKeyPressed(Key.Tab))
        {
            _window!.Close();
            return;
        }

        if (_keyboard.IsKeyPressed(Key.F))
        {
            _renderer!.FogEnabled = !_renderer.FogEnabled;
        }

        // Re-capture mouse on click
        if (_mouse != null && !_mouseCaptured && _mouse.IsButtonPressed(MouseButton.Left))
        {
            _mouseCaptured = true;
            _mouse.Cursor.CursorMode = CursorMode.Raw;
            _firstMouse = true;
        }

        if (!_mouseCaptured) return;

        _camera.ProcessMovement(
            _keyboard.IsKeyPressed(Key.W),
            _keyboard.IsKeyPressed(Key.S),
            _keyboard.IsKeyPressed(Key.A),
            _keyboard.IsKeyPressed(Key.D),
            _keyboard.IsKeyPressed(Key.Space),
            _keyboard.IsKeyPressed(Key.ShiftLeft) || _keyboard.IsKeyPressed(Key.ShiftRight),
            deltaTime
        );
    }

    private static void OnMouseMove(IMouse mouse, Vector2 position)
    {
        if (!_mouseCaptured || _camera == null) return;

        if (_firstMouse)
        {
            _lastMousePos = position;
            _firstMouse = false;
            return;
        }

        float deltaX = position.X - _lastMousePos.X;
        float deltaY = position.Y - _lastMousePos.Y;
        _lastMousePos = position;

        _camera.ProcessMouseMovement(deltaX, deltaY);
    }

    private static void OnScroll(IMouse mouse, ScrollWheel scroll)
    {
        _camera?.AdjustSpeed(scroll.Y);
    }

    private static void OnRender(double dt)
    {
        if (_renderer == null || _camera == null || _window == null) return;
        float aspect = (float)_window.Size.X / _window.Size.Y;
        _renderer.Render(_camera, aspect);
    }

    private static void OnResize(Vector2D<int> size)
    {
        _gl?.Viewport(size);
    }

    private static void OnClosing()
    {
        _inputContext?.Dispose();
        _inputContext = null;
        _renderer?.Dispose();
    }
}
