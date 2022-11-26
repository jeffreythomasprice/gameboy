namespace Gameboy.UI;

using System.Drawing;
using Microsoft.Extensions.Logging;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

public class Window : GameWindow
{
	private readonly ILoggerFactory loggerFactory;
	private readonly ILogger logger;
	private readonly Keypad keypad;

	private Shader? shader = null;
	private int? vertexBuffer;
	private int? vertexArray;
	private int? texture;

	private List<Action> textureUpdates = new();

	public Window(ILoggerFactory loggerFactory, Video video, Keypad keypad) : base(
		GameWindowSettings.Default,
		new NativeWindowSettings
		{
			Title = "Gameboy",
			Size = new(1024, 768),
			MaximumSize = null,
			MinimumSize = new(160, 144),
			RedBits = 8,
			GreenBits = 8,
			BlueBits = 8,
			AlphaBits = 0,
			DepthBits = 0,
			StencilBits = 0,
			StartFocused = true,
			StartVisible = true,
		}
	)
	{
		this.loggerFactory = loggerFactory;
		this.keypad = keypad;

		logger = loggerFactory.CreateLogger<Window>();
		logger.LogDebug("window properties");
		logger.LogDebug($"APIVersion = {APIVersion}");
		logger.LogDebug($"Profile = {Profile}");
		logger.LogDebug($"Flags = {Flags}");

		// TODO multiple palettes to switch between
		var palette = new Color[]
		{
			// approximately 0.8
			Color.FromArgb(205,205,205),
			// approximately 0.6
			Color.FromArgb(154,154,154),
			// approximately 0.4
			Color.FromArgb(102,102,102),
			// approximately 0.2
			Color.FromArgb(51,51,51),
		};
		video.ScanlineAvailable += (y, data) =>
		{
			var rgbaData = data
				.Select(color => palette[color])
				.SelectMany(c => new byte[] { c.R, c.G, c.B, c.A })
				.ToArray();
			lock (textureUpdates)
			{
				textureUpdates.Add(() =>
				{
					GL.TexSubImage2D<byte>(
						TextureTarget.Texture2D,
						0,
						0,
						y,
						Video.ScreenWidth,
						1,
						PixelFormat.Rgba,
						PixelType.UnsignedByte,
						rgbaData
					);
				});
			}
		};
	}

	protected override void OnLoad()
	{
		base.OnLoad();

		shader = new Shader(
			loggerFactory,
			"""
			#version 330 core
			
			layout (location = 0) in vec2 positionAttribute;
			layout (location = 1) in vec2 textureCoordinateAttribute;

			out vec2 textureCoordinateVarying;

			void main()
			{
				gl_Position = vec4(positionAttribute, 0.0, 1.0);
				textureCoordinateVarying = textureCoordinateAttribute;
			}
			""",
			"""
			#version 330 core

			uniform sampler2D textureUniform;

			in vec2 textureCoordinateVarying;

			out vec4 outColor;

			void main()
			{
				outColor = texture(textureUniform, textureCoordinateVarying);
			}
			"""
		);

		vertexBuffer = GL.GenBuffer();
		GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer.Value);
		GL.BufferData(
			BufferTarget.ArrayBuffer,
			sizeof(float) * 4 * 6,
			new float[] {
				-1, -1, 0, 1,
				1, -1, 1, 1,
				1, 1, 1, 0,
				1, 1, 1, 0,
				-1, 1, 0, 0,
				-1, -1, 0, 1,
			},
			BufferUsageHint.StaticDraw
		);
		GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

		vertexArray = GL.GenVertexArray();
		GL.BindVertexArray(vertexArray.Value);
		GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer.Value);
		var positionAttribute = shader.Attributes["positionAttribute"];
		GL.VertexAttribPointer(positionAttribute.Location, 2, VertexAttribPointerType.Float, false, sizeof(float) * 4, 0);
		GL.EnableVertexAttribArray(positionAttribute.Location);
		var textureCoordinateAttribute = shader.Attributes["textureCoordinateAttribute"];
		GL.VertexAttribPointer(textureCoordinateAttribute.Location, 2, VertexAttribPointerType.Float, false, sizeof(float) * 4, sizeof(float) * 2);
		GL.EnableVertexAttribArray(textureCoordinateAttribute.Location);
		GL.BindVertexArray(0);

		texture = GL.GenTexture();
		GL.BindTexture(TextureTarget.Texture2D, texture.Value);
		GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Video.ScreenWidth, Video.ScreenHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, 0);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Linear);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
		GL.BindTexture(TextureTarget.Texture2D, 0);
	}

	protected override void OnUnload()
	{
		base.OnUnload();

		shader?.Dispose();
		if (vertexBuffer.HasValue)
		{
			GL.DeleteBuffer(vertexBuffer.Value);
		}
		if (vertexArray.HasValue)
		{
			GL.DeleteVertexArray(vertexArray.Value);
		}
		if (texture.HasValue)
		{
			GL.DeleteTexture(texture.Value);
		}
	}

	protected override void OnResize(ResizeEventArgs e)
	{
		base.OnResize(e);

		GL.Viewport(new Size(Size.X, Size.Y));
	}

	protected override void OnUpdateFrame(FrameEventArgs args)
	{
		base.OnUpdateFrame(args);

		GL.ClearColor(0.25f, 0.5f, 0.75f, 1);
		GL.Clear(ClearBufferMask.ColorBufferBit);

		if (shader != null && vertexArray.HasValue && texture.HasValue)
		{
			shader.Use();

			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, texture.Value);
			GL.Uniform1(shader.Uniforms["textureUniform"].Location, 0);

			lock (textureUpdates)
			{
				foreach (var udpate in textureUpdates)
				{
					udpate();
				}
				textureUpdates.Clear();
			}

			GL.BindVertexArray(vertexArray.Value);
			GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
			GL.BindVertexArray(0);

			GL.BindTexture(TextureTarget.Texture2D, 0);

			GL.UseProgram(0);
		}

		Context.SwapBuffers();

		// TODO configurable key bindings
		keypad.SetPressed(Key.Left, KeyboardState.IsKeyDown(Keys.Left) || KeyboardState.IsKeyDown(Keys.A));
		keypad.SetPressed(Key.Right, KeyboardState.IsKeyDown(Keys.Right) || KeyboardState.IsKeyDown(Keys.D));
		keypad.SetPressed(Key.Up, KeyboardState.IsKeyDown(Keys.Up) || KeyboardState.IsKeyDown(Keys.W));
		keypad.SetPressed(Key.Down, KeyboardState.IsKeyDown(Keys.Down) || KeyboardState.IsKeyDown(Keys.S));
		keypad.SetPressed(Key.Start, KeyboardState.IsKeyDown(Keys.Enter));
		keypad.SetPressed(Key.Select, KeyboardState.IsKeyDown(Keys.RightShift));
		keypad.SetPressed(Key.A, KeyboardState.IsKeyDown(Keys.Z));
		keypad.SetPressed(Key.B, KeyboardState.IsKeyDown(Keys.X));
	}
}