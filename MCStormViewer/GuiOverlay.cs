using System.Numerics;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace MCStormViewer;

public class GuiOverlay : IDisposable
{
    private ImGuiController? _controller;
    private string _filterText = "";
    private string? _currentWorldName;
    private bool _showBrowser = true;

    public bool ShowBrowser
    {
        get => _showBrowser;
        set => _showBrowser = value;
    }

    public void Initialize(GL gl, IView window, IInputContext input)
    {
        _controller = new ImGuiController(gl, window, input);
    }

    public void Update(float dt)
    {
        _controller?.Update(dt);
    }

    public void SetCurrentWorld(string? worldName)
    {
        _currentWorldName = worldName;
    }

    public bool WantCaptureMouse()
    {
        return ImGui.GetIO().WantCaptureMouse;
    }

    public bool WantCaptureKeyboard()
    {
        return ImGui.GetIO().WantCaptureKeyboard;
    }

    public string? RenderWorldBrowser(string levelsPath)
    {
        string? selected = null;

        if (!_showBrowser) return null;

        var viewport = ImGui.GetMainViewport();

        ImGui.SetNextWindowPos(new Vector2(20, 20), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(380, viewport.Size.Y - 60), ImGuiCond.FirstUseEver);

        bool open = _showBrowser;
        if (ImGui.Begin("World Browser", ref open, ImGuiWindowFlags.NoCollapse))
        {
            ImGui.Text("Filter:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##filter", ref _filterText, 256);

            ImGui.Separator();

            string[] files;
            try
            {
                files = Directory.GetFiles(levelsPath, "*.lvl");
                Array.Sort(files, (a, b) =>
                    string.Compare(Path.GetFileName(a), Path.GetFileName(b), StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                ImGui.TextColored(new Vector4(1, 0.4f, 0.4f, 1), "Could not read levels directory.");
                ImGui.End();
                return null;
            }

            ImGui.BeginChild("FileList", new Vector2(0, -30), true);

            foreach (var file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);

                if (!string.IsNullOrEmpty(_filterText) &&
                    !name.Contains(_filterText, StringComparison.OrdinalIgnoreCase))
                    continue;

                bool isCurrent = name == _currentWorldName;
                if (isCurrent)
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.4f, 1f, 0.4f, 1f));

                long size = 0;
                try { size = new FileInfo(file).Length; } catch { }
                string sizeStr = size < 1024 * 1024
                    ? $"{size / 1024.0:F0} KB"
                    : $"{size / (1024.0 * 1024.0):F1} MB";

                if (ImGui.Selectable($"{name}  ({sizeStr})", isCurrent))
                    selected = file;

                if (isCurrent)
                    ImGui.PopStyleColor();
            }

            ImGui.EndChild();

            ImGui.Text($"{files.Length} worlds found");
        }
        ImGui.End();
        _showBrowser = open;

        return selected;
    }

    public void RenderStatusBar(World? world, Camera? camera, double fps)
    {
        var viewport = ImGui.GetMainViewport();
        float barHeight = 28;
        ImGui.SetNextWindowPos(new Vector2(viewport.Pos.X, viewport.Pos.Y + viewport.Size.Y - barHeight));
        ImGui.SetNextWindowSize(new Vector2(viewport.Size.X, barHeight));

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(0, 0));

        if (ImGui.Begin("##statusbar", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoFocusOnAppearing))
        {
            if (world != null && camera != null)
            {
                ImGui.Text(
                    $"World: {world.Width}x{world.Height}x{world.Length}  |  " +
                    $"Pos: ({camera.Position.X:F1}, {camera.Position.Y:F1}, {camera.Position.Z:F1})  |  " +
                    $"Speed: {camera.Speed:F0}  |  " +
                    $"FPS: {fps:F0}  |  " +
                    $"[Tab] Browser  [F] Fog  [Esc] Quit");
            }
            else
            {
                ImGui.Text("[Tab] Open World Browser to load a world  |  FPS: " + $"{fps:F0}");
            }
        }
        ImGui.End();

        ImGui.PopStyleVar(2);
    }

    public void Render()
    {
        _controller?.Render();
    }

    public void Dispose()
    {
        _controller?.Dispose();
    }
}
