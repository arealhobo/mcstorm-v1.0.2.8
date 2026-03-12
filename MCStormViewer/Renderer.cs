using System.Numerics;
using Silk.NET.OpenGL;

namespace MCStormViewer;

public class Renderer : IDisposable
{
    private readonly GL _gl;
    private uint _shaderProgram;
    private int _viewLoc, _projLoc, _fogEnabledLoc, _fogColorLoc, _fogStartLoc, _fogEndLoc, _sunDirLoc;

    private readonly List<(uint vao, uint vbo, int vertexCount)> _opaqueChunks = new();
    private readonly List<(uint vao, uint vbo, int vertexCount)> _transparentChunks = new();

    public bool FogEnabled { get; set; } = true;
    public float FogStart { get; set; } = 100f;
    public float FogEnd { get; set; } = 250f;
    public Vector3 FogColor { get; set; } = new(0.6f, 0.75f, 0.95f);
    public Vector3 SunDirection { get; set; } = Vector3.Normalize(new Vector3(0.3f, 1.0f, 0.5f));

    public Renderer(GL gl)
    {
        _gl = gl;
    }

    public void Initialize()
    {
        _gl.Enable(EnableCap.DepthTest);
        _gl.Enable(EnableCap.CullFace);
        _gl.CullFace(TriangleFace.Back);
        _gl.ClearColor(FogColor.X, FogColor.Y, FogColor.Z, 1.0f);

        CompileShaders();
    }

    private void CompileShaders()
    {
        string basePath = AppContext.BaseDirectory;
        string vertSource = File.ReadAllText(Path.Combine(basePath, "Shaders", "vertex.glsl"));
        string fragSource = File.ReadAllText(Path.Combine(basePath, "Shaders", "fragment.glsl"));

        uint vertShader = _gl.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(vertShader, vertSource);
        _gl.CompileShader(vertShader);
        CheckShaderError(vertShader, "vertex");

        uint fragShader = _gl.CreateShader(ShaderType.FragmentShader);
        _gl.ShaderSource(fragShader, fragSource);
        _gl.CompileShader(fragShader);
        CheckShaderError(fragShader, "fragment");

        _shaderProgram = _gl.CreateProgram();
        _gl.AttachShader(_shaderProgram, vertShader);
        _gl.AttachShader(_shaderProgram, fragShader);
        _gl.LinkProgram(_shaderProgram);
        _gl.GetProgram(_shaderProgram, ProgramPropertyARB.LinkStatus, out int status);
        if (status == 0)
            throw new Exception("Shader link error: " + _gl.GetProgramInfoLog(_shaderProgram));

        _gl.DeleteShader(vertShader);
        _gl.DeleteShader(fragShader);

        _viewLoc = _gl.GetUniformLocation(_shaderProgram, "uView");
        _projLoc = _gl.GetUniformLocation(_shaderProgram, "uProjection");
        _fogEnabledLoc = _gl.GetUniformLocation(_shaderProgram, "uFogEnabled");
        _fogColorLoc = _gl.GetUniformLocation(_shaderProgram, "uFogColor");
        _fogStartLoc = _gl.GetUniformLocation(_shaderProgram, "uFogStart");
        _fogEndLoc = _gl.GetUniformLocation(_shaderProgram, "uFogEnd");
        _sunDirLoc = _gl.GetUniformLocation(_shaderProgram, "uSunDir");
    }

    private void CheckShaderError(uint shader, string name)
    {
        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
        if (status == 0)
            throw new Exception($"Shader compile error ({name}): " + _gl.GetShaderInfoLog(shader));
    }

    public unsafe void UploadChunk(ChunkMesh mesh)
    {
        if (mesh.Opaque != null)
            UploadBuffer(mesh.Opaque, _opaqueChunks);
        if (mesh.Transparent != null)
            UploadBuffer(mesh.Transparent, _transparentChunks);
    }

    private unsafe void UploadBuffer(float[] vertexData, List<(uint vao, uint vbo, int vertexCount)> target)
    {
        uint vao = _gl.GenVertexArray();
        uint vbo = _gl.GenBuffer();

        _gl.BindVertexArray(vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

        fixed (float* ptr = vertexData)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertexData.Length * sizeof(float)), ptr, BufferUsageARB.StaticDraw);
        }

        uint stride = ChunkMesher.FloatsPerVertex * sizeof(float);

        // Position (location 0) - vec3
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
        _gl.EnableVertexAttribArray(0);

        // Color (location 1) - vec4 (RGBA)
        _gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));
        _gl.EnableVertexAttribArray(1);

        // Normal (location 2) - vec3
        _gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, stride, (void*)(7 * sizeof(float)));
        _gl.EnableVertexAttribArray(2);

        _gl.BindVertexArray(0);

        int vertexCount = vertexData.Length / ChunkMesher.FloatsPerVertex;
        target.Add((vao, vbo, vertexCount));
    }

    public unsafe void Render(Camera camera, float aspectRatio)
    {
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        _gl.UseProgram(_shaderProgram);

        var view = camera.GetViewMatrix();
        var proj = Camera.GetProjectionMatrix(aspectRatio);

        _gl.UniformMatrix4(_viewLoc, 1, false, (float*)&view);
        _gl.UniformMatrix4(_projLoc, 1, false, (float*)&proj);

        _gl.Uniform1(_fogEnabledLoc, FogEnabled ? 1 : 0);
        _gl.Uniform3(_fogColorLoc, FogColor.X, FogColor.Y, FogColor.Z);
        _gl.Uniform1(_fogStartLoc, FogStart);
        _gl.Uniform1(_fogEndLoc, FogEnd);
        _gl.Uniform3(_sunDirLoc, SunDirection.X, SunDirection.Y, SunDirection.Z);

        // Pass 1: Opaque chunks
        foreach (var (vao, _, count) in _opaqueChunks)
        {
            _gl.BindVertexArray(vao);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)count);
        }

        // Pass 2: Transparent chunks
        if (_transparentChunks.Count > 0)
        {
            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            _gl.DepthMask(false);
            _gl.Disable(EnableCap.CullFace);

            foreach (var (vao, _, count) in _transparentChunks)
            {
                _gl.BindVertexArray(vao);
                _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)count);
            }

            _gl.Disable(EnableCap.Blend);
            _gl.DepthMask(true);
            _gl.Enable(EnableCap.CullFace);
        }
    }

    public void ClearChunks()
    {
        ClearList(_opaqueChunks);
        ClearList(_transparentChunks);
    }

    private void ClearList(List<(uint vao, uint vbo, int vertexCount)> list)
    {
        foreach (var (vao, vbo, _) in list)
        {
            _gl.DeleteVertexArray(vao);
            _gl.DeleteBuffer(vbo);
        }
        list.Clear();
    }

    public void Dispose()
    {
        ClearChunks();
        _gl.DeleteProgram(_shaderProgram);
    }
}
