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

    private float[] _vertices = new float[120]; // 60 точок по 2 координати (x, y)
    private Stopwatch _timer = new Stopwatch();

    public ChartWindow(ICpuDataProvider cpuDataProvider)
        : base(GameWindowSettings.Default, NativeWindowSettings.Default)
    {
        Title = "CPU Usage Graph";
        Size = new Vector2i(800, 600);
        _cpuDataProvider = cpuDataProvider;
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
            throw new Exception(GL.GetShaderInfoLog(vertexShader));

        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);
        GL.CompileShader(fragmentShader);
        GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out success);
        if (success == 0)
            throw new Exception(GL.GetShaderInfoLog(fragmentShader));

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

        _timer.Start();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (KeyboardState.IsKeyDown(Keys.Escape))
            Close();

        // Оновлюємо кожну секунду
        if (_timer.ElapsedMilliseconds >= 1000)
        {
            UpdateVertexData();
            _timer.Restart();
        }
    }

    private void UpdateVertexData()
    {
        var history = _cpuDataProvider.GetCpuUsageHistory(); // має повертати List<float> з максимум 60 значень

        // Перетворюємо значення в нормалізовані координати
        for (int i = 0; i < history.Count; i++)
        {
            float x = -1.0f + 2.0f * i / (history.Count - 1);         // від -1 до 1
            float y = -1.0f + 2.0f * history[i] / 100f;               // 0–100% -> -1 до 1

            _vertices[i * 2] = x;
            _vertices[i * 2 + 1] = y;
        }

        // Оновлюємо GPU-буфер
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _vertices.Length * sizeof(float), _vertices);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

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
