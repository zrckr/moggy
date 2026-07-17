using System.Diagnostics;
using System.Numerics;
using Foster.Framework;
using ImGuiNET;

namespace Moggy.Tools;

public class ImGuiRenderer : IDisposable
{
    private static readonly List<(ImGuiKey, Keys)> Keys = new()
    {
        (ImGuiKey.Tab, Foster.Framework.Keys.Tab),
        (ImGuiKey.LeftArrow, Foster.Framework.Keys.Left),
        (ImGuiKey.RightArrow, Foster.Framework.Keys.Right),
        (ImGuiKey.UpArrow, Foster.Framework.Keys.Up),
        (ImGuiKey.DownArrow, Foster.Framework.Keys.Down),
        (ImGuiKey.PageUp, Foster.Framework.Keys.PageUp),
        (ImGuiKey.PageDown, Foster.Framework.Keys.PageDown),
        (ImGuiKey.Home, Foster.Framework.Keys.Home),
        (ImGuiKey.End, Foster.Framework.Keys.End),
        (ImGuiKey.Insert, Foster.Framework.Keys.Insert),
        (ImGuiKey.Delete, Foster.Framework.Keys.Delete),
        (ImGuiKey.Backspace, Foster.Framework.Keys.Backspace),
        (ImGuiKey.Space, Foster.Framework.Keys.Space),
        (ImGuiKey.Enter, Foster.Framework.Keys.Enter),
        (ImGuiKey.Escape, Foster.Framework.Keys.Escape),
        (ImGuiKey.LeftCtrl, Foster.Framework.Keys.LeftControl),
        (ImGuiKey.LeftShift, Foster.Framework.Keys.LeftShift),
        (ImGuiKey.LeftAlt, Foster.Framework.Keys.LeftAlt),
        (ImGuiKey.LeftSuper, Foster.Framework.Keys.LeftOS),
        (ImGuiKey.RightCtrl, Foster.Framework.Keys.RightControl),
        (ImGuiKey.RightShift, Foster.Framework.Keys.RightShift),
        (ImGuiKey.RightAlt, Foster.Framework.Keys.RightAlt),
        (ImGuiKey.RightSuper, Foster.Framework.Keys.RightOS),
        (ImGuiKey.Menu, Foster.Framework.Keys.Menu),
        (ImGuiKey._0, Foster.Framework.Keys.D0),
        (ImGuiKey._1, Foster.Framework.Keys.D1),
        (ImGuiKey._2, Foster.Framework.Keys.D2),
        (ImGuiKey._3, Foster.Framework.Keys.D3),
        (ImGuiKey._4, Foster.Framework.Keys.D4),
        (ImGuiKey._5, Foster.Framework.Keys.D5),
        (ImGuiKey._6, Foster.Framework.Keys.D6),
        (ImGuiKey._7, Foster.Framework.Keys.D7),
        (ImGuiKey._8, Foster.Framework.Keys.D8),
        (ImGuiKey._9, Foster.Framework.Keys.D9),
        (ImGuiKey.A, Foster.Framework.Keys.A),
        (ImGuiKey.B, Foster.Framework.Keys.B),
        (ImGuiKey.C, Foster.Framework.Keys.C),
        (ImGuiKey.D, Foster.Framework.Keys.D),
        (ImGuiKey.E, Foster.Framework.Keys.E),
        (ImGuiKey.F, Foster.Framework.Keys.F),
        (ImGuiKey.G, Foster.Framework.Keys.G),
        (ImGuiKey.H, Foster.Framework.Keys.H),
        (ImGuiKey.I, Foster.Framework.Keys.I),
        (ImGuiKey.J, Foster.Framework.Keys.J),
        (ImGuiKey.K, Foster.Framework.Keys.K),
        (ImGuiKey.L, Foster.Framework.Keys.L),
        (ImGuiKey.M, Foster.Framework.Keys.M),
        (ImGuiKey.N, Foster.Framework.Keys.N),
        (ImGuiKey.O, Foster.Framework.Keys.O),
        (ImGuiKey.P, Foster.Framework.Keys.P),
        (ImGuiKey.Q, Foster.Framework.Keys.Q),
        (ImGuiKey.R, Foster.Framework.Keys.R),
        (ImGuiKey.S, Foster.Framework.Keys.S),
        (ImGuiKey.T, Foster.Framework.Keys.T),
        (ImGuiKey.U, Foster.Framework.Keys.U),
        (ImGuiKey.V, Foster.Framework.Keys.V),
        (ImGuiKey.W, Foster.Framework.Keys.W),
        (ImGuiKey.X, Foster.Framework.Keys.X),
        (ImGuiKey.Y, Foster.Framework.Keys.Y),
        (ImGuiKey.Z, Foster.Framework.Keys.Z),
        (ImGuiKey.F1, Foster.Framework.Keys.F1),
        (ImGuiKey.F2, Foster.Framework.Keys.F2),
        (ImGuiKey.F3, Foster.Framework.Keys.F3),
        (ImGuiKey.F4, Foster.Framework.Keys.F4),
        (ImGuiKey.F5, Foster.Framework.Keys.F5),
        (ImGuiKey.F6, Foster.Framework.Keys.F6),
        (ImGuiKey.F7, Foster.Framework.Keys.F7),
        (ImGuiKey.F8, Foster.Framework.Keys.F8),
        (ImGuiKey.F9, Foster.Framework.Keys.F9),
        (ImGuiKey.F10, Foster.Framework.Keys.F10),
        (ImGuiKey.F11, Foster.Framework.Keys.F11),
        (ImGuiKey.F12, Foster.Framework.Keys.F12),
        (ImGuiKey.Apostrophe, Foster.Framework.Keys.Apostrophe),
        (ImGuiKey.Comma, Foster.Framework.Keys.Comma),
        (ImGuiKey.Minus, Foster.Framework.Keys.Minus),
        (ImGuiKey.Period, Foster.Framework.Keys.Period),
        (ImGuiKey.Slash, Foster.Framework.Keys.Slash),
        (ImGuiKey.Semicolon, Foster.Framework.Keys.Semicolon),
        (ImGuiKey.Equal, Foster.Framework.Keys.Equals),
        (ImGuiKey.LeftBracket, Foster.Framework.Keys.LeftBracket),
        (ImGuiKey.Backslash, Foster.Framework.Keys.Backslash),
        (ImGuiKey.RightBracket, Foster.Framework.Keys.RightBracket),
        (ImGuiKey.GraveAccent, Foster.Framework.Keys.Tilde),
        (ImGuiKey.CapsLock, Foster.Framework.Keys.Capslock),
        (ImGuiKey.ScrollLock, Foster.Framework.Keys.ScrollLock),
        (ImGuiKey.NumLock, Foster.Framework.Keys.Numlock),
        (ImGuiKey.PrintScreen, Foster.Framework.Keys.PrintScreen),
        (ImGuiKey.Pause, Foster.Framework.Keys.Pause),
        (ImGuiKey.Keypad0, Foster.Framework.Keys.Keypad0),
        (ImGuiKey.Keypad1, Foster.Framework.Keys.Keypad1),
        (ImGuiKey.Keypad2, Foster.Framework.Keys.Keypad2),
        (ImGuiKey.Keypad3, Foster.Framework.Keys.Keypad3),
        (ImGuiKey.Keypad4, Foster.Framework.Keys.Keypad4),
        (ImGuiKey.Keypad5, Foster.Framework.Keys.Keypad5),
        (ImGuiKey.Keypad6, Foster.Framework.Keys.Keypad6),
        (ImGuiKey.Keypad7, Foster.Framework.Keys.Keypad7),
        (ImGuiKey.Keypad8, Foster.Framework.Keys.Keypad8),
        (ImGuiKey.Keypad9, Foster.Framework.Keys.Keypad9),
        (ImGuiKey.KeypadDecimal, Foster.Framework.Keys.KeypadPeroid),
        (ImGuiKey.KeypadDivide, Foster.Framework.Keys.KeypadDivide),
        (ImGuiKey.KeypadMultiply, Foster.Framework.Keys.KeypadMultiply),
        (ImGuiKey.KeypadSubtract, Foster.Framework.Keys.KeypadMinus),
        (ImGuiKey.KeypadAdd, Foster.Framework.Keys.KeypadPlus),
        (ImGuiKey.KeypadEnter, Foster.Framework.Keys.KeypadEnter),
        (ImGuiKey.KeypadEqual, Foster.Framework.Keys.KeypadEquals)
    };

    private readonly App _app;

    private readonly IntPtr _context;

    private readonly Mesh<PosTexColVertex, ushort> _mesh;

    private readonly Material _material;

    private readonly Texture _fontTexture;

    private readonly List<Texture> _boundTextures = [];

    private readonly List<Batcher> _batchersUsed = [];

    private readonly Stack<Batcher> _batchersStack = [];

    private readonly Stack<Batcher> _batcherPool = [];

    private PosTexColVertex[] _vertices = [];

    private ushort[] _indices = [];

    /// <summary>
    /// UI Scaling
    /// </summary>
    public float Scale = 1.0f;

    /// <summary>
    /// Mouse Position relative to ImGui elements
    /// </summary>
    public Vector2 MousePosition => _app?.Input.Mouse.Position / Scale ?? Vector2.Zero;

    /// <summary>
    /// If the ImGui Context wants text input
    /// </summary>
    public bool WantsTextInput { get; private set; }

    public ImGuiRenderer(App app, string? customFontPath = null)
    {
        _app = app;

        // create imgui context
        _context = ImGui.CreateContext(null);
        ImGui.SetCurrentContext(_context);

        var io = ImGui.GetIO();
        io.BackendFlags = ImGuiBackendFlags.None;
        io.ConfigFlags = ImGuiConfigFlags.DockingEnable;

        // load ImGui Font
        {
            if (customFontPath != null && File.Exists(customFontPath))
            {
                io.Fonts.AddFontFromFileTTF(customFontPath, 64);
                io.FontGlobalScale = 16.0f / 64.0f;
            }
            else
            {
                io.Fonts.AddFontDefault();
            }
        }

        // create font texture
        unsafe
        {
            io.Fonts.GetTexDataAsRGBA32(out byte* pixelData, out var width, out var height, out _);
            _fontTexture = new Texture(app.GraphicsDevice, width, height,
                new ReadOnlySpan<byte>(pixelData, width * height * 4));
        }

        // create drawing resources
        _mesh = new Mesh<PosTexColVertex, ushort>(app.GraphicsDevice);
        _material = app.GraphicsDevice.Defaults.TexturedMaterial.Clone();
        ImGui.SetCurrentContext(nint.Zero);
    }

    /// <summary>
    /// Begins a new ImGui Frame.
    /// ImGui methods are available between BeginLayout and EndLayout.
    /// </summary>
    public void BeginLayout()
    {
        Debug.Assert(ImGui.GetCurrentContext() == nint.Zero);
        ImGui.SetCurrentContext(_context);

        // clear textures for the next frame
        _boundTextures.Clear();

        // clear batches
        _batchersStack.Clear();
        _batchersUsed.ForEach(_batcherPool.Push);
        _batchersUsed.Clear();

        // assign font texture again
        var io = ImGui.GetIO();
        io.Fonts.SetTexID(GetTextureId(_fontTexture));

        // setup io
        io.DeltaTime = _app.Time.Delta;
        io.DisplaySize = new Vector2(_app.Window.WidthInPixels / Scale, _app.Window.HeightInPixels / Scale);
        io.DisplayFramebufferScale = Vector2.One * Scale;

        io.AddMousePosEvent(MousePosition.X, MousePosition.Y);
        io.AddMouseButtonEvent(0, _app.Input.Mouse.LeftDown || _app.Input.Mouse.LeftPressed);
        io.AddMouseButtonEvent(1, _app.Input.Mouse.RightDown || _app.Input.Mouse.RightPressed);
        io.AddMouseButtonEvent(2, _app.Input.Mouse.MiddleDown || _app.Input.Mouse.MiddlePressed);
        io.AddMouseWheelEvent(_app.Input.Mouse.Wheel.X, _app.Input.Mouse.Wheel.Y);

        foreach (var (imGuiKey, fosterKey) in Keys)
        {
            if (_app.Input.Keyboard.Pressed(fosterKey))
            {
                io.AddKeyEvent(imGuiKey, true);
            }

            if (_app.Input.Keyboard.Released(fosterKey))
            {
                io.AddKeyEvent(imGuiKey, false);
            }
        }

        io.AddKeyEvent(ImGuiKey.ModShift, _app.Input.Keyboard.Shift);
        io.AddKeyEvent(ImGuiKey.ModAlt, _app.Input.Keyboard.Alt);
        io.AddKeyEvent(ImGuiKey.ModCtrl, _app.Input.Keyboard.Ctrl);
        io.AddKeyEvent(ImGuiKey.ModSuper, _app.Input.Keyboard.Down(Foster.Framework.Keys.LeftOS) ||
                                          _app.Input.Keyboard.Down(Foster.Framework.Keys.RightOS));

        if (_app.Input.Keyboard.Text.Length > 0)
        {
            for (var i = 0; i < _app.Input.Keyboard.Text.Length; i++)
            {
                io.AddInputCharacter(_app.Input.Keyboard.Text[i]);
            }
        }

        WantsTextInput = io.WantTextInput;

        ImGui.NewFrame();
    }

    /// <summary>
    /// Ends an ImGui Frame.
    /// Call this at the end of your Update method.
    /// </summary>
    public void EndLayout()
    {
        Debug.Assert(ImGui.GetCurrentContext() == _context);
        ImGui.Render();
        ImGui.SetCurrentContext(nint.Zero);
    }

    /// <summary>
    /// Begin a new Batch in an ImGui Window. Returns true if any batch contents
    /// will be visible. Call <see cref="EndBatch"/> regardless of return value.
    /// </summary>
    public bool BeginBatch(out Batcher batch, out Rect bounds)
    {
        return BeginBatch(ImGui.GetContentRegionAvail(), out batch, out bounds);
    }

    /// <summary>
    /// Begin a new Batch in an ImGui Window. Returns true if any batch contents
    /// will be visible. Call <see cref="EndBatch"/> regardless of return value.
    /// </summary>
    public bool BeginBatch(Vector2 size, out Batcher batch, out Rect bounds)
    {
        var min = ImGui.GetCursorScreenPos();
        var max = min + size;
        var screenspace = Rect.Between(min, max);
        var clip = Rect.Between(ImGui.GetWindowDrawList().GetClipRectMin(), ImGui.GetWindowDrawList().GetClipRectMax());
        var scissor = screenspace.GetIntersection(clip).Scale(Scale).Int();

        // create a dummy element of the given size
        ImGui.Dummy(size);

        // get recycled batcher, add to list
        batch = _batcherPool.Count > 0 ? _batcherPool.Pop() : new Batcher(_app.GraphicsDevice);
        batch.Clear();
        _batchersUsed.Add(batch);
        _batchersStack.Push(batch);

        // notify imgui
        ImGui.GetWindowDrawList().AddCallback(new IntPtr(_batchersUsed.Count), new IntPtr(0));

        // push relative coords
        batch.PushScissor(scissor);
        batch.PushMatrix(Matrix3x2.CreateScale(Scale));
        batch.PushMatrix(screenspace.TopLeft);

        bounds = new Rect(0, 0, screenspace.Width, screenspace.Height);

        return scissor.Width > 0 && scissor.Height > 0;
    }

    /// <summary>
    /// End a Batch in an ImGui Window
    /// </summary>
    public void EndBatch()
    {
        var batch = _batchersStack.Pop();
        batch.PopMatrix();
        batch.PopMatrix();
        batch.PopScissor();
    }

    /// <summary>
    /// Renders the ImGui buffers. Call this in your Render method.
    /// </summary>
    public unsafe void Render()
    {
        Debug.Assert(ImGui.GetCurrentContext() == nint.Zero);
        ImGui.SetCurrentContext(_context);

        var data = ImGui.GetDrawData();
        if (data.NativePtr == null || data.TotalVtxCount <= 0)
        {
            ImGui.SetCurrentContext(nint.Zero);
            return;
        }

        // build vertex/index buffer lists
        {
            // calculate total size
            var vertexCount = 0;
            var indexCount = 0;
            for (var i = 0; i < data.CmdListsCount; i++)
            {
                vertexCount += data.CmdLists[i].VtxBuffer.Size;
                indexCount += data.CmdLists[i].IdxBuffer.Size;
            }

            // make sure we have enough space
            if (vertexCount > _vertices.Length)
            {
                Array.Resize(ref _vertices, vertexCount);
            }

            if (indexCount > _indices.Length)
            {
                Array.Resize(ref _indices, indexCount);
            }

            // copy data to arrays
            vertexCount = indexCount = 0;
            for (var i = 0; i < data.CmdListsCount; i++)
            {
                var list = data.CmdLists[i];
                var vertexSrc = new Span<PosTexColVertex>((void*)list.VtxBuffer.Data, list.VtxBuffer.Size);
                var indexSrc = new Span<ushort>((void*)list.IdxBuffer.Data, list.IdxBuffer.Size);

                vertexSrc.CopyTo(_vertices.AsSpan()[vertexCount..]);
                indexSrc.CopyTo(_indices.AsSpan()[indexCount..]);

                vertexCount += vertexSrc.Length;
                indexCount += indexSrc.Length;
            }

            // upload buffers to mesh
            _mesh.SetVertices(_vertices.AsSpan(0, vertexCount));
            _mesh.SetIndices(_indices.AsSpan(0, indexCount));
        }

        var size = new Point2(_app.Window.WidthInPixels, _app.Window.HeightInPixels);

        // create pass
        var pass = new DrawCommand(_app.Window, _mesh, _material)
        {
            BlendMode = new BlendMode(BlendOp.Add, BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha)
        };

        // setup ortho matrix
        var mat =
            Matrix4x4.CreateScale(data.FramebufferScale.X, data.FramebufferScale.Y, 1.0f) *
            Matrix4x4.CreateOrthographicOffCenter(0, size.X, size.Y, 0, 0.1f, 1000.0f);
        _material.Vertex.SetUniformBuffer(mat);

        // draw imgui buffers to the screen
        var globalVtxOffset = 0;
        var globalIdxOffset = 0;
        for (var i = 0; i < data.CmdListsCount; i++)
        {
            var imList = data.CmdLists[i];
            var imCommands = (ImDrawCmd*)imList.CmdBuffer.Data;

            // draw each command
            for (var cmd = imCommands; cmd < imCommands + imList.CmdBuffer.Size; cmd++)
            {
                var scissor = new Rect(
                    cmd->ClipRect.X,
                    cmd->ClipRect.Y,
                    cmd->ClipRect.Z - cmd->ClipRect.X,
                    cmd->ClipRect.W - cmd->ClipRect.Y).Scale(data.FramebufferScale).Int();

                if (scissor.Width <= 0 || scissor.Height <= 0)
                {
                    continue;
                }

                if (cmd->UserCallback != IntPtr.Zero)
                {
                    var batchIndex = cmd->UserCallback.ToInt32() - 1;
                    if (batchIndex >= 0 && batchIndex < _batchersUsed.Count)
                    {
                        _batchersUsed[batchIndex].Render(_app.Window, null, scissor);
                    }
                }
                else
                {
                    var textureIndex = cmd->TextureId.ToInt32();
                    if (textureIndex < _boundTextures.Count)
                    {
                        _material.Fragment.Samplers[0] =
                            new BoundSampler(_boundTextures[textureIndex], new TextureSampler());
                    }

                    pass.VertexOffset = (int)(cmd->VtxOffset + globalVtxOffset);
                    pass.IndexOffset = (int)(cmd->IdxOffset + globalIdxOffset);
                    pass.IndexCount = (int)cmd->ElemCount;
                    pass.Scissor = scissor;

                    _app.GraphicsDevice.Draw(pass);
                }
            }

            globalVtxOffset += imList.VtxBuffer.Size;
            globalIdxOffset += imList.IdxBuffer.Size;
        }

        ImGui.SetCurrentContext(nint.Zero);
    }

    /// <summary>
    /// Gets a Texture ID to draw in ImGui
    /// </summary>
    public IntPtr GetTextureId(Texture? texture)
    {
        var id = new IntPtr(_boundTextures.Count);
        if (texture != null)
        {
            _boundTextures.Add(texture);
        }

        return id;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        ImGui.DestroyContext(_context);
    }
}
