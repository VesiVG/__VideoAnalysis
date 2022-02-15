#version 120

attribute vec3 Position;
attribute vec2 TexCoord;

void main(void)
{
	gl_TexCoord[0].st=gl_MultiTexCoord0.st;
	gl_Position =ftransform();
}

