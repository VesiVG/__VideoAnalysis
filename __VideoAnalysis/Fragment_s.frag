#version 120

uniform sampler2D tex_y;
uniform sampler2D tex_u;
uniform sampler2D tex_v;
uniform int pixelformat;
#define IsYUV420P pixelformat==3
#define mf 1
#define maxf float(mf)
#define imaxf  1.0/maxf
#if mf==255
	#define hmaxf 128.0
#else 
	#define hmaxf 0.5
#endif

vec4 Yuv2rgb(vec4 yuva) {
vec4 rgba;
vec4 yuv2=yuva - vec4(0.0,0.5,0.5,1.0);

	rgba.a = 1.0;

rgba.r = yuv2.x * 1.164 + yuv2.y * 0 + yuv2.z * 1.678;
rgba.g = yuv2.x * 1.164 + yuv2.y * -0.1873 + yuv2.z * -0.6504;
rgba.b = yuv2.x * 1.164 + yuv2.y * 2.1417 + yuv2.z * 0.0;


	return rgba;

}
void main()
{
	vec4 fragc;
	vec2 texcoord23=gl_TexCoord[0].xy;
	fragc = IsYUV420P ? Yuv2rgb(vec4(texture2D(tex_y, texcoord23).r,texture2D(tex_u, texcoord23).r,texture2D(tex_v, texcoord23).r, 1.0)) : texture2D(tex_y, texcoord23).rgba;
	gl_FragColor = fragc;
}