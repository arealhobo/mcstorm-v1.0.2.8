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
    private static GuiOverlay? _gui;

    private static bool _mouseCaptured = false;
    private static Vector2 _lastMousePos;
    private static bool _firstMouse = true;

    private static string? _levelsPath;
    private static World? _currentWorld;
    private static string? _currentWorldPath;

    // Hot-reload
    private static FileSystemWatcher? _watcher;
    private static bool _reloadPending;
    private static DateTime _reloadRequestedAt;
    private const double ReloadDebounceSeconds = 0.5;

    // FPS tracking
    private static double _fps;
    private static double _fpsAccumulator;
    private static int _fpsFrameCount;

    // Tab key debounce
    private static bool _tabWasPressed;

    public static void Main(string[] args)
    {
        _levelsPath = FindLevelsPath();
        if (_levelsPath == null)
        {
            Console.WriteLine("Could not find 'levels' directory. Place this program near your MCStorm installation.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;
        }

        LaunchViewer();
    }

    private static string? FindLevelsPath()
    {
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

    private static void LaunchViewer()
    {
        var options = WindowOptions.Default;
        options.Title = "MCStorm Viewer";
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

        _inputContext = _window!.CreateInput();
        _keyboard = _inputContext.Keyboards.FirstOrDefault();
        _mouse = _inputContext.Mice.FirstOrDefault();

        if (_mouse != null)
        {
            _mouse.Cursor.CursorMode = CursorMode.Normal;
            _mouse.MouseMove += OnMouseMove;
            _mouse.Scroll += OnScroll;
        }

        // Initialize ImGui overlay
        _gui = new GuiOverlay();
        _gui.Initialize(_gl, _window!, _inputContext);

        // Set up FileSystemWatcher on the levels directory
        SetupFileWatcher();
    }

    private static void SetupFileWatcher()
    {
        if (_levelsPath == null) return;

        _watcher = new FileSystemWatcher(_levelsPath, "*.lvl");
        _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime;
        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.Renamed += (_, e) => OnFileChanged(null, e);
        _watcher.EnableRaisingEvents = true;
    }

    private static void OnFileChanged(object? sender, FileSystemEventArgs e)
    {
        if (_currentWorldPath != null &&
            string.Equals(Path.GetFullPath(e.FullPath), Path.GetFullPath(_currentWorldPath), StringComparison.OrdinalIgnoreCase))
        {
            _reloadRequestedAt = DateTime.UtcNow;
            _reloadPending = true;
        }
    }

    private static void LoadWorld(string path)
    {
        try
        {
            var world = LvlParser.Parse(path);

            _renderer!.ClearChunks();
            _currentWorld = world;
            _currentWorldPath = path;

            _gui?.SetCurrentWorld(Path.GetFileNameWithoutExtension(path));
            _window!.Title = $"MCStorm Viewer - {world.Width}x{world.Height}x{world.Length}";

            // Camera at spawn position
            _camera = new Camera(new Vector3(
                world.SpawnX + 0.5f,
                world.SpawnY + 1.7f,
                world.SpawnZ + 0.5f
            ));

            // Mesh all chunks
            for (int cy = 0; cy < world.ChunksY; cy++)
            for (int cz = 0; cz < world.ChunksZ; cz++)
            for (int cx = 0; cx < world.ChunksX; cx++)
            {
                var mesh = ChunkMesher.BuildChunkMesh(world, cx, cy, cz);
                if (mesh.Opaque != null || mesh.Transparent != null)
                    _renderer.UploadChunk(mesh);
            }

            // Set fog distance based on world size
            float maxDim = Math.Max(world.Width, Math.Max(world.Height, world.Length));
            _renderer.FogEnd = maxDim * 1.5f;
            _renderer.FogStart = _renderer.FogEnd * 0.4f;

            // Close browser and capture mouse after loading
            if (_gui != null)
                _gui.ShowBrowser = false;
            SetMouseCaptured(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading world: {ex.Message}");
        }
    }

    private static void SetMouseCaptured(bool captured)
    {
        _mouseCaptured = captured;
        if (_mouse != null)
            _mouse.Cursor.CursorMode = captured ? CursorMode.Raw : CursorMode.Normal;
        if (captured)
            _firstMouse = true;
    }

    private static void OnUpdate(double dt)
    {
        float deltaTime = (float)dt;

        // FPS tracking
        _fpsAccumulator += dt;
        _fpsFrameCount++;
        if (_fpsAccumulator >= 0.5)
        {
            _fps = _fpsFrameCount / _fpsAccumulator;
            _fpsAccumulator = 0;
            _fpsFrameCount = 0;
        }

        if (_keyboard == null) return;

        // Update ImGui
        _gui?.Update(deltaTime);

        // Hot-reload debounce
        if (_reloadPending && (DateTime.UtcNow - _reloadRequestedAt).TotalSeconds >= ReloadDebounceSeconds)
        {
            _reloadPending = false;
            if (_currentWorldPath != null)
                LoadWorld(_currentWorldPath);
        }

        // Tab toggles browser (with debounce)
        bool tabPressed = _keyboard.IsKeyPressed(Key.Tab);
        if (tabPressed && !_tabWasPressed && !(_gui?.WantCaptureKeyboard() ?? false))
        {
            if (_gui != null)
            {
                _gui.ShowBrowser = !_gui.ShowBrowser;
                if (_gui.ShowBrowser)
                    SetMouseCaptured(false);
                else if (_currentWorld != null)
                    SetMouseCaptured(true);
            }
        }
        _tabWasPressed = tabPressed;

        // Escape handling
        if (_keyboard.IsKeyPressed(Key.Escape) && !(_gui?.WantCaptureKeyboard() ?? false))
        {
            if (_mouseCaptured)
            {
                SetMouseCaptured(false);
            }
            else
            {
                _window!.Close();
                return;
            }
        }

        if (_keyboard.IsKeyPressed(Key.F) && !(_gui?.WantCaptureKeyboard() ?? false))
        {
            _renderer!.FogEnabled = !_renderer.FogEnabled;
        }

        // Re-capture mouse on click when browser is closed
        if (_mouse != null && !_mouseCaptured && _mouse.IsButtonPressed(MouseButton.Left) &&
            !(_gui?.WantCaptureMouse() ?? false) && !(_gui?.ShowBrowser ?? false))
        {
            SetMouseCaptured(true);
        }

        if (!_mouseCaptured || _camera == null) return;

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
        if (_gui?.WantCaptureMouse() ?? false) return;
        _camera?.AdjustSpeed(scroll.Y);
    }

    private static void OnRender(double dt)
    {
        if (_renderer == null || _window == null) return;

        float aspect = (float)_window.Size.X / _window.Size.Y;

        if (_camera != null)
            _renderer.Render(_camera, aspect);
        else
        {
            _gl!.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        // Render ImGui overlay on top
        if (_gui != null && _levelsPath != null)
        {
            string? selectedWorld = _gui.RenderWorldBrowser(_levelsPath);
            _gui.RenderStatusBar(_currentWorld, _camera, _fps);
            _gui.Render();

            if (selectedWorld != null)
                LoadWorld(selectedWorld);
        }
    }

    private static void OnResize(Vector2D<int> size)
    {
        _gl?.Viewport(size);
    }

    private static void OnClosing()
    {
        _watcher?.Dispose();
        _gui?.Dispose();
        _inputContext?.Dispose();
        _inputContext = null;
        _renderer?.Dispose();
    }
}
