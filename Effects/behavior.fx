float uTime;

float2 uStep;

float2 uCircleCenter;
float uCircleRadius;
float uCircleRotation;
float uCircleCentrifuge;

texture2D uTex0;
sampler2D uImage0 = sampler_state
{
    Texture = <uTex0>;
    MinFilter = Point;
    MagFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

texture2D uTex1;
sampler2D uImage1 = sampler_state
{
    Texture = <uTex1>;
    MinFilter = Point;
    MagFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

texture2D uTex2;
sampler2D uImageMask = sampler_state
{
    Texture = <uTex2>;
    MinFilter = Point;
    MagFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

texture2D uTex3;
sampler2D uImageLight = sampler_state
{
    Texture = <uTex3>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

// Compute

struct ComputeFragmentIn
{
    float4 pos : POSITION0;
    float2 coords : TEXCOORD0;
};

float4 computeFrag(ComputeFragmentIn input) : COLOR0
{
    float4 center = tex2D(uImage1, input.coords);
    float4 centerm = tex2D(uImageMask, input.coords);
    float4 down   = tex2D(uImageMask, input.coords + float2(0, uStep.y));
    float4 downm  = tex2D(uImage1, input.coords + float2(0, uStep.y));
    float4 up     = tex2D(uImage1, input.coords + float2(0, -uStep.y));
    float4 left   = tex2D(uImage1, input.coords + float2(-uStep.x, 0));
    float4 leftm  = tex2D(uImageMask, input.coords + float2(-uStep.x, 0));
    float4 right  = tex2D(uImage1, input.coords + float2(uStep.x, 0));
    float4 upl    = tex2D(uImage1, input.coords + float2(-uStep.x, -uStep.y));
    float4 downr  = tex2D(uImage1, input.coords + float2(uStep.x, uStep.y));
    float4 downrm = tex2D(uImageMask, input.coords + float2(uStep.x, uStep.y));
    // float upr = tex2D(uImage1, input.coords + float2( uStep.x, -uStep.y));
    // if (input.coords.y + 2 * uStep.y >= 1.0) return float4(1, 0, 0, 1);
    if (centerm.a > 0)
        return float4(0, 0, 0, 0);
    if (center.a > 0)
    {
        // 下方被挡住
        // 右下被挡住或者被右侧落沙占用
        // 才能保证这个粒子不流走
		if ((down.a > 0 || downm.a > 0) && (downr.a > 0 || downrm.a > 0 || right.a > 0))
			return center;
        return float4(0, 0, 0, 0);
    }
    if (up.a > 0)
    {
        return up;
    }
    else if ((left.a > 0 || leftm.a > 0) && upl.a > 0)
    {
        return upl;
    }
    else
    {
        return float4(0, 0, 0, 0);
    }
}

float4 displayFrag(ComputeFragmentIn input) : COLOR0
{
    float4 center = tex2D(uImage1, input.coords);
    float4 light = tex2D(uImageLight, input.coords);
    if (center.a > 0)
		return float4(center.rgb * light.rgb, 1);
    return float4(0, 0, 0, 0);
}

float4 blackHoleFrag(ComputeFragmentIn input) : COLOR0
{
    float2 uv = input.coords;
    float2 d = uv - uCircleCenter;
    float L = length(d);
    if (L >= uCircleRadius)
		return tex2D(uImage1, uv);
    if (L <= uCircleCentrifuge * uCircleRadius)
        return float4(0, 0, 0, 0);
    float rot = (exp(1 - L / uCircleRadius) - 1) * uCircleRotation;
    float2 rotatedD = float2(cos(rot), sin(rot)) * d.x + float2(-sin(rot), cos(rot)) * d.y;
    uv = uCircleCenter + rotatedD / L * lerp(0, uCircleRadius, (L / uCircleRadius - uCircleCentrifuge) / (1 - uCircleCentrifuge));
    return tex2D(uImage1, uv);
}

technique Technique233
{
    pass Compute
    {
        PixelShader  = compile ps_3_0 computeFrag(); 
    }

    pass Display
    {
        PixelShader  = compile ps_3_0 displayFrag(); 
    }

    pass BlackHole
    {
        PixelShader  = compile ps_3_0 blackHoleFrag(); 
    }
}

