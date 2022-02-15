#version 440 core
layout(location = 0) in vec3 Position;
layout(location = 1) in vec2 TexCoord;

out vec2 texcoord23;
void main(void)
{
	texcoord23 = TexCoord;
    	//texcoord23 = gl_MultiTexCoord0.st;
    	gl_Position = vec4(Position, 1.0);
}

