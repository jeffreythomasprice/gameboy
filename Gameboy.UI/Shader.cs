using Microsoft.Extensions.Logging;
using OpenTK.Graphics.OpenGL4;

namespace Gameboy.UI;

public class Shader : IDisposable
{
	public record AttributeInfo(
		int Location,
		string Name,
		int Size,
		ActiveAttribType Type
	)
	{ }

	public record UniformInfo(
		int Location,
		string Name,
		int Size,
		ActiveUniformType Type
	)
	{ }

	private readonly ILogger logger;
	private bool isDisposed = false;
	private int program;

	public readonly IReadOnlyDictionary<string, AttributeInfo> Attributes;
	public readonly IReadOnlyDictionary<string, UniformInfo> Uniforms;

	public Shader(ILoggerFactory loggerFactory, string vertexShaderSource, string fragmentShaderSource)
	{
		logger = loggerFactory.CreateLogger<Shader>();

		int compileShader(ShaderType type, string source)
		{
			logger.LogTrace($"creating shader of type {type}, source:\n{source}");
			var result = GL.CreateShader(type);
			GL.ShaderSource(result, source);
			GL.CompileShader(result);
			GL.GetShader(result, ShaderParameter.CompileStatus, out var status);
			if (status == 0)
			{
				var log = GL.GetShaderInfoLog(result);
				GL.DeleteShader(result);
				throw new Exception($"shader compile error for shader of type {type}\n{log}");
			}
			return result;
		}

		var vertexShader = compileShader(ShaderType.VertexShader, vertexShaderSource);

		int fragmentShader;
		try
		{
			fragmentShader = compileShader(ShaderType.FragmentShader, fragmentShaderSource);
		}
		catch
		{
			GL.DeleteShader(vertexShader);
			throw;
		}

		logger.LogTrace("linking shader program");
		program = GL.CreateProgram();
		GL.AttachShader(program, vertexShader);
		GL.AttachShader(program, fragmentShader);
		GL.LinkProgram(program);
		GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var status);
		if (status == 0)
		{
			var log = GL.GetProgramInfoLog(program);
			GL.DeleteShader(vertexShader);
			GL.DeleteShader(fragmentShader);
			GL.DeleteProgram(program);
			throw new Exception($"shader program link error:\n{log}");
		}

		GL.DetachShader(program, vertexShader);
		GL.DetachShader(program, fragmentShader);
		GL.DeleteShader(vertexShader);
		GL.DeleteShader(fragmentShader);

		var attributes = new List<AttributeInfo>();
		GL.GetProgram(program, GetProgramParameterName.ActiveAttributes, out var activeAttributes);
		for (var i = 0; i < activeAttributes; i++)
		{
			var name = GL.GetActiveAttrib(program, i, out var size, out var type);
			var location = GL.GetAttribLocation(program, name);
			var info = new AttributeInfo(location, name, size, type);
			logger.LogTrace($"{info}");
			attributes.Add(info);
		}
		this.Attributes = attributes.ToDictionary(x => x.Name);

		var uniforms = new List<UniformInfo>();
		GL.GetProgram(program, GetProgramParameterName.ActiveUniforms, out var activeUniforms);
		for (var i = 0; i < activeUniforms; i++)
		{
			var name = GL.GetActiveUniform(program, i, out var size, out var type);
			var location = GL.GetUniformLocation(program, name);
			var info = new UniformInfo(location, name, size, type);
			logger.LogTrace($"{info}");
			uniforms.Add(info);
		}
		this.Uniforms = uniforms.ToDictionary(x => x.Name);
	}

	~Shader()
	{
		Dispose(false);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(true);
	}

	public void Use()
	{
		GL.UseProgram(program);
	}

	private void Dispose(bool disposing)
	{
		if (!isDisposed)
		{
			isDisposed = true;
			GL.DeleteProgram(program);
		}
	}
}