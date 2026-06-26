#pragma warning disable CS8618
#pragma warning disable CS8602

using System;
using System.IO;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using ScreenCapture.NET;
using System.Windows.Forms;

namespace SilkNetOverlay
{
    class Program
    {
        private static IWindow window;
        private static GL gl;
        private static string[] shaders = Directory.GetFiles("./shaders/", "*.glsl");
        private static int selectedShaderIndex;
        
        private static IScreenCaptureService captureService;
        private static IScreenCapture screenCapture;
        private static ICaptureZone captureZone;
        private static uint programId;
        private static uint textureId;
        private static uint vaoId;
        private static uint vboId;
        private static readonly int Width = Screen.PrimaryScreen.Bounds.Width;
        private static readonly int Height = Screen.PrimaryScreen.Bounds.Height;

        [DllImport("user32.dll")]
        private static extern uint SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private const uint WDA_EXCLUDE = 0x00000011;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint GW_HWNDNEXT = 2;

        private static IntPtr targetGameWindow = IntPtr.Zero;
        private static IntPtr overlayWindowHandle = IntPtr.Zero;
        private static IntPtr consoleWindowHandle = IntPtr.Zero;

        private const int VK_CONTROL = 0x11;
        private const int VK_MENU = 0x12;
        private const int VK_LEFT = 0x25;
        private const int VK_RIGHT = 0x27;
        
        private static bool isKeyPressed = false;

        static void Main(string[] args)
        {
            consoleWindowHandle = GetConsoleWindow();
            targetGameWindow = GetForegroundWindow();

            if (targetGameWindow == consoleWindowHandle && consoleWindowHandle != IntPtr.Zero)
            {
                IntPtr nextWindow = consoleWindowHandle;
                while (nextWindow != IntPtr.Zero)
                {
                    nextWindow = GetWindow(nextWindow, GW_HWNDNEXT);
                    if (nextWindow != IntPtr.Zero && IsWindowVisible(nextWindow))
                    {
                        targetGameWindow = nextWindow;
                        break;
                    }
                }
            }

            if (shaders.Length == 0)
            {
                Console.WriteLine("Error: No .glsl files found in ./shaders/ directory!");
                return;
            }

            while (true)
            {
                Console.WriteLine("Available shaders:");
                int i = 1;
                foreach (string file in shaders)
                {
                    Console.WriteLine("(" + i + ") " + file.Trim('/').Split('/')[2]);
                    i += 1;
                }
                try
                {
                    Console.Write("Select the shader you want to apply: ");
                    string input = Console.ReadLine() ?? "";
                    int index = int.Parse(input);
                    if (index >= 1 && index <= shaders.Length)
                    {
                        Console.WriteLine("The shader has been selected! The overlay will launch automatically.\nTo switch between shaders, use the shortcuts CTRL+ALT+Left (previous) and CTRL+ALT+Right (next).\n");
                        selectedShaderIndex = index - 1;
                        break;
                    } else
                    {
                        Console.WriteLine("Error: An invalid index was provided.\n");
                    }
                }
                catch
                {
                    Console.WriteLine("Error: An invalid index was provided.\n");
                }
            }

            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(Width, Height);
            options.Title = "Silk.NET Shader Overlay";
            options.WindowBorder = WindowBorder.Hidden; 
            options.TopMost = true;                     
            options.TransparentFramebuffer = true;
            options.Position = new Vector2D<int>(0, 0);
            
            window = Window.Create(options);
            window.Load += OnLoad;
            window.Render += OnRender;
            window.Closing += OnClosing;
            window.FocusChanged += OnWindowFocusChanged;
            window.Run();
        }

        private static void OnLoad()
        {
            if (window.Native.Win32.HasValue)
            {
                overlayWindowHandle = window.Native.Win32.Value.Hwnd;
                
                if (overlayWindowHandle != IntPtr.Zero)
                {
                    SetWindowDisplayAffinity(overlayWindowHandle, WDA_EXCLUDE);
                    int initialStyle = GetWindowLong(overlayWindowHandle, GWL_EXSTYLE);
                    SetWindowLong(overlayWindowHandle, GWL_EXSTYLE, initialStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE);
                    SetWindowPos(overlayWindowHandle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                    if (targetGameWindow != IntPtr.Zero && targetGameWindow != consoleWindowHandle)
                    {
                        SetForegroundWindow(targetGameWindow);
                    }
                }
            } 

            gl = GL.GetApi(window);
            try
            {
                captureService = new DX11ScreenCaptureService();
                var cards = captureService.GetGraphicsCards().GetEnumerator();
                if (cards.MoveNext())
                {
                    var card = cards.Current;
                    var displays = captureService.GetDisplays(card).GetEnumerator();
                    if (displays.MoveNext())
                    {
                        var display = displays.Current;
                        screenCapture = captureService.GetScreenCapture(display);
                        captureZone = screenCapture.RegisterCaptureZone(0, 0, display.Width, display.Height);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Screen initialization error: {ex.Message}");
            }

            programId = BuildShaderProgram();
            float[] vertices = {
                -1.0f,  1.0f,  0.0f, 1.0f,
                -1.0f, -1.0f,  0.0f, 0.0f,
                 1.0f, -1.0f,  1.0f, 0.0f,

                -1.0f,  1.0f,  0.0f, 1.0f,
                 1.0f, -1.0f,  1.0f, 0.0f,
                 1.0f,  1.0f,  1.0f, 1.0f
            };
            vaoId = gl.GenVertexArray();
            vboId = gl.GenBuffer();
            gl.BindVertexArray(vaoId);
            gl.BindBuffer(GLEnum.ArrayBuffer, vboId);
            unsafe
            {
                fixed (void* v = vertices)
                {
                    gl.BufferData(GLEnum.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), v, GLEnum.StaticDraw);
                }

                gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 4 * sizeof(float), (void*)0);
                gl.EnableVertexAttribArray(0);
                gl.VertexAttribPointer(1, 2, GLEnum.Float, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
                gl.EnableVertexAttribArray(1);
            }
            textureId = gl.GenTexture();
            gl.BindTexture(GLEnum.Texture2D, textureId);
            gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Linear);
            gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
            unsafe
            {
                gl.TexImage2D(GLEnum.Texture2D, 0, (int)GLEnum.Rgba, (uint)Width, (uint)Height, 0, GLEnum.Bgra, GLEnum.UnsignedByte, null);
            }
            gl.Enable(GLEnum.Blend);
            gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
        }

        private static void OnWindowFocusChanged(bool isFocused)
        {
            if (isFocused && overlayWindowHandle != IntPtr.Zero)
            {
                int initialStyle = GetWindowLong(overlayWindowHandle, GWL_EXSTYLE);
                SetWindowLong(overlayWindowHandle, GWL_EXSTYLE, initialStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE);
                
                if (targetGameWindow != IntPtr.Zero && targetGameWindow != overlayWindowHandle && targetGameWindow != consoleWindowHandle)
                {
                    SetForegroundWindow(targetGameWindow);
                }
            }
        }        

        private static void OnRender(double deltaTime)
        {
            IntPtr currentForeground = GetForegroundWindow();
            if (currentForeground != IntPtr.Zero && 
                currentForeground != overlayWindowHandle && 
                currentForeground != consoleWindowHandle)
            {
                targetGameWindow = currentForeground;
            }

            bool isCtrlDown = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
            bool isAltDown = (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;
            bool isRightArrowDown = (GetAsyncKeyState(VK_RIGHT) & 0x8000) != 0;
            bool isLeftArrowDown = (GetAsyncKeyState(VK_LEFT) & 0x8000) != 0;
            bool isNextCombo = isCtrlDown && isAltDown && isRightArrowDown;
            bool isPrevCombo = isCtrlDown && isAltDown && isLeftArrowDown;

            if ((isNextCombo || isPrevCombo) && !isKeyPressed)
            {
                isKeyPressed = true;
                if (isNextCombo)
                {
                    selectedShaderIndex = (selectedShaderIndex + 1) % shaders.Length;
                }
                else if (isPrevCombo)
                {
                    selectedShaderIndex = (selectedShaderIndex - 1 + shaders.Length) % shaders.Length;
                }
                Console.WriteLine($"Changing shader to: {shaders[selectedShaderIndex].Trim('/').Split('/')[2]}");
                ReloadShader();
                if (targetGameWindow != IntPtr.Zero)
                {
                    SetForegroundWindow(targetGameWindow);
                }
            }
            else if (!isNextCombo && !isPrevCombo)
            {
                isKeyPressed = false;
            }

            if (gl == null) return;
            gl.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            gl.Clear((uint)GLEnum.ColorBufferBit);
            gl.UseProgram(programId);
            gl.BindVertexArray(vaoId);
            gl.BindTexture(GLEnum.Texture2D, textureId);
            if (screenCapture != null && captureZone != null)
            {
                try
                {
                    screenCapture.CaptureScreen();
                    using (captureZone.Lock())
                    {
                        ReadOnlySpan<byte> bufferData = captureZone.RawBuffer;
                        if (bufferData.Length > 0)
                        {
                            unsafe
                            {
                                fixed (byte* p = bufferData)
                                {
                                    gl.TexSubImage2D(GLEnum.Texture2D, 0, 0, 0, (uint)captureZone.Width, (uint)captureZone.Height, GLEnum.Bgra, GLEnum.UnsignedByte, p);
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
            }
            gl.DrawArrays(GLEnum.Triangles, 0, 6);
        }

        private static uint CompileShader(ShaderType type, string code)
        {
            if (gl == null) return 0;
            uint id = gl.CreateShader(type);
            gl.ShaderSource(id, code);
            gl.CompileShader(id);
            string infoLog = gl.GetShaderInfoLog(id);
            if (!string.IsNullOrEmpty(infoLog))
            {
                Console.WriteLine($"Shader ({type}) compilation error: {infoLog}");
            }
            return id;
        }

        private static uint BuildShaderProgram()
        {
            string vertexCode = @"
                #version 330 core
                layout (location = 0) in vec2 aPos;
                layout (location = 1) in vec2 aTexCoords;
                out vec2 TexCoords;
                void main() {
                    gl_Position = vec4(aPos, 0.0, 1.0);
                    TexCoords = vec2(aTexCoords.x, 1.0 - aTexCoords.y);
                }";

            string fragmentCode = File.ReadAllText(shaders[selectedShaderIndex]);

            uint vertexShader = CompileShader(ShaderType.VertexShader, vertexCode);
            uint fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentCode);

            uint program = gl.CreateProgram();

            gl.AttachShader(program, vertexShader);
            gl.AttachShader(program, fragmentShader);

            gl.LinkProgram(program);

            gl.DetachShader(program, vertexShader);
            gl.DetachShader(program, fragmentShader);

            gl.DeleteShader(vertexShader);
            gl.DeleteShader(fragmentShader);

            return program;
        }

        private static void ReloadShader()
        {
            uint newProgram = BuildShaderProgram();

            uint oldProgram = programId;
            programId = newProgram;

            if (oldProgram != 0)
            {
                gl.DeleteProgram(oldProgram);
            }
        }
        
        private static void OnClosing()
        {
            if (gl == null) return;
            gl.DeleteBuffer(vboId);
            gl.DeleteVertexArray(vaoId);
            gl.DeleteTexture(textureId);
            gl.DeleteProgram(programId);
            screenCapture?.Dispose();
            gl.Dispose();
        }
    }
}