using System;
using System.Collections.Generic;
using gl = OpenTK.Graphics.OpenGL.GL;
using wwgl = OpenTK.Graphics.OpenGL;

namespace __VideoAnalysis
{
    partial class Program
    {
        public partial class GLW
        {
            //
            //Source for shader class
            //https://github.com/opentk/LearnOpenTK/blob/master/Common/Shader.cs
            //
            public class Shader
            {
                public readonly int Prog_handle;
                private readonly Dictionary<string, int> _uniformLocations;
                public Shader(string Vertex_code, string Fragment_code)
                {
                    var shaderSource = LoadSource(Vertex_code);
                    var vertexShader = gl.CreateShader(wwgl.ShaderType.VertexShader);
                    gl.ShaderSource(vertexShader, shaderSource);
                    CompileShader(vertexShader);
                    shaderSource = LoadSource(Fragment_code);
                    var fragmentShader = gl.CreateShader(wwgl.ShaderType.FragmentShader);
                    gl.ShaderSource(fragmentShader, shaderSource);
                    CompileShader(fragmentShader);
                    Prog_handle = gl.CreateProgram();

                    gl./*AttachObject*/AttachShader(Prog_handle, vertexShader);
                    gl./*AttachObject*/AttachShader(Prog_handle, fragmentShader);

                    LinkProgram(Prog_handle);
                    gl./*DetachObject*/DetachShader(Prog_handle, vertexShader);
                    gl./*DetachObject*/DetachShader(Prog_handle, fragmentShader);
                    gl./*DetachObject*/DeleteShader(fragmentShader);
                    gl./*DetachObject*/DeleteShader(vertexShader);

                    gl.GetProgram(Prog_handle, wwgl.GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);
                    _uniformLocations = new Dictionary<string, int>();

                    // Loop over all the uniforms,
                    for (var i = 0; i < numberOfUniforms; i++)
                    {
                        // get the name of this uniform,
                        var key = gl.GetActiveUniform(Prog_handle, i, out _, out _);

                        // get the location,
                        var location = gl.GetUniformLocation(Prog_handle, key);

                        // and then add it to the dictionary.
                        _uniformLocations.Add(key, location);
                    }
                }

                private static void CompileShader(int shader)
                {
                    gl.CompileShader(shader);
                    gl.GetShader(shader, wwgl.ShaderParameter.CompileStatus, out var code);
                    if (code != (int)wwgl.All.True)
                    {
                        var infoLog = gl.GetShaderInfoLog(shader);
                        FFWrapper.logger?.log_m(1, 1, $"Error occurred whilst compiling Shader({shader}).\n\n{infoLog}\n");
                        Environment.Exit(-33);
                    }
                }

                private static unsafe void LinkProgram(int program)
                {
                    int code = 0;
                    gl.LinkProgram(program);
                    gl.GetProgram(program, wwgl.GetProgramParameterName.LinkStatus, &code);
                    if (code == 0)
                    {
                        var infoLog = gl.GetProgramInfoLog(program);
                        FFWrapper.logger?.log_m(1, 1, $"Error occurred whilst linking Program({program}).\n\n{infoLog.Substring(0, Math.Min(infoLog.Length, 1000))}\n");
                        Environment.Exit(-33);
                    }
                    gl.ValidateProgram(program);
                    gl.GetProgram(program, wwgl.GetProgramParameterName.ValidateStatus, &code);
                    if (code == 0)
                    {
                        var infoLog = gl.GetProgramInfoLog(program);
                        FFWrapper.logger?.log_m(1, 1, $"Error occurred whilst validating Program({program}).\n\n{infoLog}\n");
                        Environment.Exit(-33);
                    }
                }

                public void Use()
                {
                    gl.UseProgram(Prog_handle);
                }
                public void Release()
                {
                    gl.UseProgram(0);
                }
                public void SetInt(string name, int data)
                {
                    if (_uniformLocations.ContainsKey(name))
                    {
                        wwgl.GL.Uniform1(_uniformLocations[name], data);
                    }
                }
                private static string LoadSource(string path)
                {
                    if (string.IsNullOrWhiteSpace(path) || System.IO.File.Exists(path) == false)
                        return String.Empty;
                    return System.IO.File.ReadAllText(path);
                }
            }
        }
    }

}
