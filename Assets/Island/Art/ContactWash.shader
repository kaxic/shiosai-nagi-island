Shader "Island/Contact Wash"
{
    Properties
    {
        _Depth ("Pigment strength", Range(0,1)) = .40
        _Tint ("Shadow pigment", Color) = (.28,.44,.49,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent-20" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Cull Off
            ZWrite Off
            Blend DstColor Zero
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            half _Depth;
            half4 _Tint;
            struct input { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct output { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            output vert(input v)
            {
                output o;
                o.pos=UnityObjectToClipPos(v.vertex);
                o.uv=v.uv*2-1;
                return o;
            }
            fixed4 frag(output i) : SV_Target
            {
                // A feathered, slightly squared oval fits roots, stones, and rectangular furniture alike.
                float2 q=abs(i.uv);
                float d=pow(dot(q*q,q),1.0/3.0);
                half body=1-smoothstep(.34,.99,d);
                half core=1-smoothstep(0,.54,d);
                half density=(body*.76+core*.24)*_Depth;
                return fixed4(lerp(half3(1,1,1),_Tint.rgb,density),1);
            }
            ENDCG
        }
    }
}
