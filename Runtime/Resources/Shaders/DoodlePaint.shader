Shader "Hidden/DoodlePaint"
{
    SubShader
    {
        Lighting Off
        Blend One Zero

        Pass
        {
            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

            float4 _Pos;
            float  _Radius;
            float4 _Color;

            float squareDist(float2 a, float2 b, float2 c)
            {
                float2 ab = b - a;
                float2 ac = c - a;
                float2 bc = c - b;

                float e = dot(ac, ab);
                if (e <= 0.0)
                    return dot(ac, ac);

                float f = dot(ab, ab);
                if (e >= f)
                    return dot(bc, bc);
                return dot(ac, ac) - e * e / f;
            }

            float4 frag(v2f_customrendertexture IN) : COLOR
            {
                float2 coord = IN.globalTexcoord.xy;
                float2 a = float2(_Pos.x, _Pos.y);
                float2 b = float2(_Pos.z, _Pos.w);
                float2 c = float2(coord.x * _CustomRenderTextureWidth, coord.y * _CustomRenderTextureHeight);
                float sqDist = squareDist(a, b, c);
                float sqRadius = _Radius * _Radius;
                if(sqDist < sqRadius)
                    return _Color;
                return tex2D(_SelfTexture2D, IN.globalTexcoord.xy);
            }
            ENDCG
        }
    }
}