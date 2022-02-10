#version 440 compatibility
out vec4 rendered_color;
in vec2 texcoord23;
uniform sampler2D plane0;
void main()
{
	vec4 fragc = texture(plane0, texcoord23).rgba;
	rendered_color = fragc;
}