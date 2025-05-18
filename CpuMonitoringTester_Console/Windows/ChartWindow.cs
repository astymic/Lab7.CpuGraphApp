using Lab7.CpuMonitoringLibrary;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Diagnostics;

namespace Lab7.CpuMonitoringTester_Console.Windows;

public class ChartWindow : GameWindow
{
    private readonly ICpuDataProvider _cpuDataProvider;
    private readonly string _shaderPath = Path.Combine(Environment.CurrentDirectory, "Shaders");

    private int _vertexBufferObject;
    private int _vertexArrayObject;
    private int _shaderProgram;
    private int _projectionLocation;


    private readonly float[] _vertices;
    private readonly Stopwatch _timer = new ();

    private readonly int _updateInterval;

    public ChartWindow(ICpuDataProvider cpuDataProvider)
        : base(GameWindowSettings.Default, NativeWindowSettings.Default)
    {
        Title = "CPU Usage Graph";
        _cpuDataProvider = cpuDataProvider;

        var historyCapacity = cpuDataProvider.HistoryCapacity;
        _updateInterval = cpuDataProvider.UpdateInterval;

        _vertices = new float[historyCapacity * 2]; // Ammout of dots in the chart history * axes count.
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);

        string vertexShaderSource = File.ReadAllText(Path.Combine(_shaderPath, "shader.vert"));
        string fragmentShaderSource = File.ReadAllText(Path.Combine(_shaderPath, "shader.frag"));

        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexShaderSource);
        GL.CompileShader(vertexShader);
        GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int success);

        if (success == 0)
            throw new InvalidOperationException(GL.GetShaderInfoLog(vertexShader));

        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);
        GL.CompileShader(fragmentShader);
        GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out success);

        if (success == 0)
            throw new InvalidOperationException(GL.GetShaderInfoLog(fragmentShader));

        _shaderProgram = GL.CreateProgram();
        GL.AttachShader(_shaderProgram, vertexShader);
        GL.AttachShader(_shaderProgram, fragmentShader);
        GL.LinkProgram(_shaderProgram);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        _vertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(_vertexArrayObject);

        _vertexBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.DynamicDraw);

        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        _projectionLocation = GL.GetUniformLocation(_shaderProgram, "uProjection");

        _timer.Start();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, Size.X, Size.Y);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        // Exit program.
        if (KeyboardState.IsKeyDown(Keys.Escape))
            Close();

        // Update chart.
        if (_timer.ElapsedMilliseconds >= _updateInterval)
        {
            UpdateVertexData();
            _timer.Restart();
        }
    }

    private void UpdateVertexData()
    {
        var history = _cpuDataProvider.GetCpuUsageHistory(); // Length is 60.

        for (int i = 0; i < history.Count; i++)
        {
            float x = (float)i / (history.Count - 1) * Size.X;
            float y = history[i] / 100f * Size.Y;

            _vertices[i * 2] = x;
            _vertices[i * 2 + 1] = y;
        }

        // Update buffer.
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _vertices.Length * sizeof(float), _vertices);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        Matrix4 projection = Matrix4.CreateOrthographicOffCenter(0, Size.X, 0, Size.Y, -1f, 1f);
        GL.UniformMatrix4(_projectionLocation, false, ref projection);

        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.UseProgram(_shaderProgram);
        GL.BindVertexArray(_vertexArrayObject);

        int points = _vertices.Length / 2;
        GL.DrawArrays(PrimitiveType.LineStrip, 0, points);

        SwapBuffers();
    }

    protected override void OnUnload()
    {
        GL.DeleteBuffer(_vertexBufferObject);
        GL.DeleteVertexArray(_vertexArrayObject);
        GL.DeleteProgram(_shaderProgram);
        base.OnUnload();
    }
}
