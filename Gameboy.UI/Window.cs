namespace Gameboy.UI;

using System.Drawing;
using Microsoft.Extensions.Logging;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

public class Window : GameWindow
{
	private readonly ILoggerFactory loggerFactory;
	private readonly ILogger logger;
	private readonly Video video;
	private readonly Keypad keypad;

	private Shader? shader = null;
	private int? vertexBuffer;
	private int? vertexArray;
	private int? texture;

	private Matrix4 orthoMatrix;
	private Matrix4 modelviewMatrix;

	private class ScanlineUpdateBuffer
	{
		public readonly byte[] Pixels = new byte[Video.ScreenWidth * Video.ScreenHeight * 4];
		public int MinimumY;
		public int MaximumY;
	}

	// this one is where we'll write new scanline data to as it comes in
	private ScanlineUpdateBuffer incomingScanlineBuffer = new();
	// this one is where we'll copy tex data to opengl
	private ScanlineUpdateBuffer copyToTextureBuffer = new();
	private bool needsTextureUpdate = false;

	// TODO multiple palettes to switch between
	private readonly Color[] palette = new[]
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
		this.video = video;
		this.keypad = keypad;

		logger = loggerFactory.CreateLogger<Window>();
		logger.LogTrace($"""
		window properties:
		APIVersion = {APIVersion}
		Profile = {Profile}
		Flags = {Flags}
		""");
	}

	protected override void OnLoad()
	{
		base.OnLoad();

		GL.ClearColor(palette[0]);

		shader = new Shader(
			loggerFactory,
			"""
			#version 330 core
			
			uniform mat4 projectionMatrixUniform;
			uniform mat4 modelviewMatrixUniform;

			layout (location = 0) in vec2 positionAttribute;
			layout (location = 1) in vec2 textureCoordinateAttribute;

			out vec2 textureCoordinateVarying;

			void main()
			{
				gl_Position = projectionMatrixUniform * modelviewMatrixUniform * vec4(positionAttribute, 0.0, 1.0);
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
				0, 0, 0, 0,
				Video.ScreenWidth, 0, 1, 0,
				Video.ScreenWidth, Video.ScreenHeight, 1, 1,
				Video.ScreenWidth, Video.ScreenHeight, 1, 1,
				0, Video.ScreenHeight, 0, 1,
				0, 0, 0, 0,
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

		video.ScanlineAvailable += ScanlineAvailable;
		video.VSync += VSync;
	}

	protected override void OnUnload()
	{
		base.OnUnload();

		video.ScanlineAvailable -= ScanlineAvailable;
		video.VSync -= VSync;

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

		orthoMatrix = Matrix4.CreateOrthographicOffCenter(0, Size.X, Size.Y, 0, -1, 1);

		var scale = Math.Min((double)Size.X / (double)Video.ScreenWidth, (double)Size.Y / (double)Video.ScreenHeight);
		modelviewMatrix =
			Matrix4.CreateScale((float)scale) *
			Matrix4.CreateTranslation(
				(float)(((double)Size.X - (double)Video.ScreenWidth * scale) / 2.0),
				(float)(((double)Size.Y - (double)Video.ScreenHeight * scale) / 2.0),
				0.0f
			);
	}

	protected override void OnRenderFrame(FrameEventArgs args)
	{
		base.OnRenderFrame(args);

		GL.Clear(ClearBufferMask.ColorBufferBit);

		if (shader != null && vertexArray.HasValue && texture.HasValue)
		{
			shader.Use();

			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, texture.Value);
			GL.Uniform1(shader.Uniforms["textureUniform"].Location, 0);

			GL.UniformMatrix4(shader.Uniforms["projectionMatrixUniform"].Location, false, ref orthoMatrix);
			GL.UniformMatrix4(shader.Uniforms["modelviewMatrixUniform"].Location, false, ref modelviewMatrix);

			// copy the new texture data
			if (needsTextureUpdate)
			{
				GL.PixelStore(PixelStoreParameter.UnpackSkipRows, copyToTextureBuffer.MinimumY);
				GL.TexSubImage2D<byte>(
					TextureTarget.Texture2D,
					0,
					0,
					copyToTextureBuffer.MinimumY,
					Video.ScreenWidth,
					copyToTextureBuffer.MaximumY - copyToTextureBuffer.MinimumY + 1,
					PixelFormat.Rgba,
					PixelType.UnsignedByte,
					copyToTextureBuffer.Pixels
				);
				// done copying, when this one gets used next it has no rows in int
				copyToTextureBuffer.MinimumY = Video.ScreenHeight;
				copyToTextureBuffer.MaximumY = -1;
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

	private void ScanlineAvailable(int y, Video.Color[] data)
	{
		var buffer = incomingScanlineBuffer;
		var bufferOffset = y * Video.ScreenWidth * 4;
		for (var x = 0; x < Video.ScreenWidth; x++)
		{
			var color = palette[data[x].Value];
			buffer.Pixels[bufferOffset++] = color.R;
			buffer.Pixels[bufferOffset++] = color.G;
			buffer.Pixels[bufferOffset++] = color.B;
			buffer.Pixels[bufferOffset++] = color.A;
		}
		buffer.MinimumY = Math.Min(buffer.MinimumY, y);
		buffer.MaximumY = Math.Max(buffer.MaximumY, y);
	}

	private new void VSync()
	{
		// swap the buffers
		copyToTextureBuffer = Interlocked.Exchange<ScanlineUpdateBuffer>(ref incomingScanlineBuffer, copyToTextureBuffer);
		needsTextureUpdate = copyToTextureBuffer.MinimumY >= 0 && copyToTextureBuffer.MinimumY <= copyToTextureBuffer.MaximumY;
	}
}
