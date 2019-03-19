/**
 * Copyright (c) 2019 Elie Michel
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to
 * deal in the Software without restriction, including without limitation the
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
 * sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
 * IN THE SOFTWARE.
 *
 * This file is part of Lily Render 360, a unity tool for equirectangular
 * rendering, available at https://github.com/eliemichel/LilyRender360
 */

/**
 * Hey, it's already specified in the license lines above, but nobody reads it,
 * so again, PLEASE credit me if you copy paste those, it took me some time to
 * do the math here.
 */

#define PI 3.141592654

float4 sampleCube(float x, float y, float z, sampler2D tex, float b, out float2 px)
{
	float scale = 1.0 / x * b;
	px.x = (z * scale + 1.0) / 2.0f;
	px.y = 1 - (y * scale + 1.0) / 2.0f;

	float wz = smoothstep(b, 1, abs(z) * scale);
	float wy = smoothstep(b, 1, abs(y) * scale);
	float4 color;
	color.a = 1 - max(wy, wz);
	color.rgb = tex2D(tex, px.xy) * color.a;
	return color;
}

fixed4 equirectangularSmooth(
	v2f i,
	sampler2D faceTexPX,
	sampler2D faceTexNX,
	sampler2D faceTexPY,
	sampler2D faceTexNY,
	sampler2D faceTexPZ,
	sampler2D faceTexNZ,
#ifdef ORIENT_CUBE
	float3x3 orientMatrix,
#endif
	float beta,
	float hfov,
	float vfov,
	out float2 px)
{
	float theta = (i.uv.x - 0.5) * hfov;
	float phi = -(i.uv.y - 0.5) * vfov;

	float x = cos(phi) * sin(theta);
	float y = sin(phi);
	float z = cos(phi) * cos(theta);

#ifdef ORIENT_CUBE
	float3 orientedDir = mul(orientMatrix, float3(x, y, z));
	x = orientedDir.x;
	y = orientedDir.y;
	z = orientedDir.z;
#endif

	float4 color = float4(0, 0, 0, 0);
	float2 px2;
	px = float2(2, 2);

#ifdef SHOW_STITCH_LINES
	float stitchline = 0.0;
#endif

	if (abs(y) * beta <= abs(x) && abs(z) * beta <= abs(x))
	{
		float sign = x < 0 ? -1.0 : 1.0;
		if (x < 0) {
			color += sampleCube(sign * x, y, -sign * z, faceTexNX, beta, px);
		} else {
			color += sampleCube(sign * x, y, -sign * z, faceTexPX, beta, px);
		}
#ifdef SHOW_STITCH_LINES
		stitchline += 1;
#endif
	}

	if (abs(z) * beta <= abs(y) && abs(x) * beta <= abs(y))
	{
		float sign = y < 0 ? -1.0 : 1.0;
		if (y < 0) {
			color += sampleCube(sign * y, -sign * z, x, faceTexNY, beta, px2);
		} else {
			color += sampleCube(sign * y, -sign * z, x, faceTexPY, beta, px2);
		}

#ifdef TWO_CUBES
		px = lerp(px2, px, step(abs(px - 0.5), abs(px2 - 0.5))); // keep the smallest
#endif

#ifdef SHOW_STITCH_LINES
		stitchline += 1;
#endif
	}

	if (abs(x) * beta <= abs(z) && abs(y) * beta <= abs(z))
	{
		float sign = z < 0 ? -1.0 : 1.0;
		if (z < 0) {
			color += sampleCube(sign * z, y, sign * x, faceTexNZ, beta, px2);
		} else {
			color += sampleCube(sign * z, y, sign * x, faceTexPZ, beta, px2);
		}

#ifdef TWO_CUBES
		px = lerp(px2, px, step(abs(px - 0.5), abs(px2 - 0.5))); // keep the smallest
#endif

#ifdef SHOW_STITCH_LINES
		stitchline += 1;
#endif
	}

	color /= color.a;

#ifdef SHOW_STITCH_LINES
	color = lerp(color, 1 - color, saturate(stitchline - 1));
#endif

	px = (px - 0.5) / beta + 0.5; // for double render fusion, px must be in [0, 1]

	return color;
}

fixed4 equirectangular(
	v2f i,
	sampler2D faceTexPX,
	sampler2D faceTexNX,
	sampler2D faceTexPY,
	sampler2D faceTexNY,
	sampler2D faceTexPZ,
	sampler2D faceTexNZ,
#ifdef ORIENT_CUBE
	float3x3 orientMatrix,
#endif
	float beta,
	float hfov,
	float vfov,
	out float2 px)
{
	float theta = (i.uv.x - 0.5) * hfov;
	float phi = -(i.uv.y - 0.5) * vfov;

	float x = cos(phi) * sin(theta);
	float y = sin(phi);
	float z = cos(phi) * cos(theta);

#ifdef ORIENT_CUBE
	float3 orientedDir = mul(orientMatrix, float3(x, y, z));
	x = orientedDir.x;
	y = orientedDir.y;
	z = orientedDir.z;
#endif

	float4 color = float4(0, 0, 0, 0);
	
	if (abs(y) <= abs(x) && abs(z) <= abs(x))
	{
		float sign = x < 0 ? -1.0 : 1.0;
		if (x < 0) {
			color += sampleCube(sign * x, y, -sign * z, faceTexNX, beta, px);
		} else {
			color += sampleCube(sign * x, y, -sign * z, faceTexPX, beta, px);
		}
	}
	else if (abs(z) <= abs(y))
	{
		float sign = y < 0 ? -1.0 : 1.0;
		if (y < 0) {
			color += sampleCube(sign * y, -sign * z, x, faceTexNY, beta, px);
		} else {
			color += sampleCube(sign * y, -sign * z, x, faceTexPY, beta, px);
		}
	}
	else
	{
		float sign = z < 0 ? -1.0 : 1.0;
		if (z < 0) {
			color += sampleCube(sign * z, y, sign * x, faceTexNZ, beta, px);
		} else {
			color += sampleCube(sign * z, y, sign * x, faceTexPZ, beta, px);
		}
	}

	color.a = 1.0;

#ifdef SHOW_STITCH_LINES
	color = lerp(color, 1 - color, max(smoothstep(0.49*beta, 0.5*beta, abs(px.x - 0.5)), smoothstep(0.49*beta, 0.5*beta, abs(px.y - 0.5))));
#endif

	px = (px - 0.5) / beta + 0.5; // for double render fusion, px must be in [0, 1]

	return color;
}
