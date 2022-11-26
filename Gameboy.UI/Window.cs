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
	private int? texture;

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
				-1, -1, 0, 0,
				1, -1, 1, 0,
				1, 1, 1, 1,
				1, 1, 1, 1,
				-1, 1, 0, 1,
				-1, -1, 0, 0,
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

		// TODO testing
		{
			var pixels = new byte[Video.ScreenWidth * Video.ScreenHeight * 4];
			for (var y = 0; y < Video.ScreenHeight; y++)
			{
				var b = (byte)((double)y / (double)(Video.ScreenHeight - 1) * 255);
				for (var x = 0; x < Video.ScreenWidth; x++)
				{
					var a = (byte)((double)x / (double)(Video.ScreenWidth - 1) * 255);
					var i = (x + y * Video.ScreenWidth) * 4;
					pixels[i++] = a;
					pixels[i++] = b;
					pixels[i++] = a;
					pixels[i++] = 255;
				}
			}
			GL.BindTexture(TextureTarget.Texture2D, texture.Value);
			GL.TexSubImage2D<byte>(TextureTarget.Texture2D, 0, 0, 0, Video.ScreenWidth, Video.ScreenHeight, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
			GL.BindTexture(TextureTarget.Texture2D, 0);
		}
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

			GL.BindVertexArray(vertexArray.Value);
			GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
			GL.BindVertexArray(0);

			GL.BindTexture(TextureTarget.Texture2D, 0);

			GL.UseProgram(0);
		}

		Context.SwapBuffers();
	}
}
