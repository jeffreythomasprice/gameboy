namespace Gameboy.UI;

using System.Drawing;
using Microsoft.Extensions.Logging;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

public class Window : GameWindow
{
	private readonly ILoggerFactory loggerFactory;
	private readonly ILogger logger;

	private Shader? shader = null;
	private int? vertexBuffer;
	private int? vertexArray;

	public Window(ILoggerFactory loggerFactory) : base(
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
		logger = loggerFactory.CreateLogger<Window>();
		logger.LogDebug("window properties");
		logger.LogDebug($"APIVersion = {APIVersion}");
		logger.LogDebug($"Profile = {Profile}");
		logger.LogDebug($"Flags = {Flags}");
	}

	public void ScanlineAvailable(int y, Color[] data)
	{
		// TODO JEFF draw scanline to screen
	}

	protected override void OnLoad()
	{
		base.OnLoad();

		shader = new Shader(
			loggerFactory,
			"""
			#version 330 core
			
			layout (location = 0) in vec2 positionAttribute;

			void main()
			{
				gl_Position = vec4(positionAttribute, 0.0, 1.0);
			}
			""",
			"""
			#version 330 core

			out vec4 outColor;

			void main()
			{
				outColor = vec4(1.0f, 0.5f, 0.2f, 1.0f);
			}
			"""
		);

		vertexBuffer = GL.GenBuffer();
		GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer.Value);
		GL.BufferData(
			BufferTarget.ArrayBuffer,
			sizeof(float) * 2 * 6,
			new float[] {
					-1, -1,
					1, -1,
					1, 1,
					1, 1,
					-1, 1,
					-1, -1,
			},
			BufferUsageHint.StaticDraw
		);
		GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

		vertexArray = GL.GenVertexArray();
		GL.BindVertexArray(vertexArray.Value);
		GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer.Value);
		var positionAttribute = shader.Attributes["positionAttribute"]!;
		GL.VertexAttribPointer(positionAttribute.Location, 2, VertexAttribPointerType.Float, false, 0, 0);
		GL.EnableVertexAttribArray(positionAttribute.Location);
		GL.BindVertexArray(0);
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

		if (shader != null && vertexArray.HasValue)
		{
			shader.Use();
			GL.BindVertexArray(vertexArray.Value);
			GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
			GL.BindVertexArray(0);
			GL.UseProgram(0);
		}

		Context.SwapBuffers();
	}
}
