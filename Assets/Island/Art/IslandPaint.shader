Shader "Island/Hand Painted"
{
    Properties
    {
        [NoScaleOffset] _Meadow ("Meadow grass", 2D) = "gray" {}
        [NoScaleOffset] _Beach ("Sand and footpaths", 2D) = "gray" {}
        [NoScaleOffset] _Lagoon ("Watercolour ripples", 2D) = "gray" {}
        [NoScaleOffset] _Cedar ("Cedar grain", 2D) = "gray" {}
        [NoScaleOffset] _Canopy ("Canopy leaves", 2D) = "gray" {}
        [NoScaleOffset] _CoastalStone ("Coastal stone", 2D) = "gray" {}
        _Detail ("Paint contrast", Range(0,1.6)) = 1
        _MeadowSize ("Meadow repeat (metres)", Range(2,12)) = 6.4
        _SeaSize ("Ripple repeat (metres)", Range(2,14)) = 8
        _TideSpeed ("Water speed", Range(0,2)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 layer : TEXCOORD2;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 paint : COLOR;
                half3 normal : TEXCOORD0;
                SHADOW_COORDS(1)
                float3 island : TEXCOORD2;
                float3 uvLayer : TEXCOORD3;
                half3 objectNormal : TEXCOORD4;
            };
            sampler2D _Meadow, _Beach, _Lagoon, _Cedar, _Canopy, _CoastalStone;
            half _Detail, _MeadowSize, _SeaSize, _TideSpeed;
            half3 pigment(sampler2D page, float2 uv)
            {
                half3 ink = tex2D(page,uv).rgb;
                #ifndef UNITY_COLORSPACE_GAMMA
                ink = LinearToGammaSpace(ink);
                #endif
                return ink;
            }
            half3 rockOrLeaf(sampler2D page, float3 position, half3 normal)
            {
                half3 w=abs(normal);
                w*=w; w*=w;
                w/=max(dot(w,half3(1,1,1)),.001);
                return pigment(page,position.zy)*w.x + pigment(page,position.xz)*w.y + pigment(page,position.xy)*w.z;
            }
            v2f vert(appdata v)
            {
                v2f o;
                o.island=v.vertex.xyz;
                o.objectNormal=v.normal;
                o.uvLayer=float3(v.uv,v.layer.x);
                if (v.layer.x>7.5 && v.layer.x<8.5)
                {
                    float tide=_Time.y*_TideSpeed;
                    v.vertex.xz+=float2(.038,.024)*sin(dot(v.vertex.xz,float2(.9,.32))+tide*1.15);
                }
                if (v.layer.x>11.5)
                {
                    float height=saturate((v.vertex.y-.62)/.55);
                    v.vertex.x+=height*(.035*sin(_Time.y*3.4+v.vertex.z*8)+.018*sin(_Time.y*5.2));
                    v.vertex.y+=height*.041*sin(_Time.y*4.1+v.vertex.x*15);
                }
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.paint = v.color;
                TRANSFER_SHADOW(o);
                return o;
            }
            half3 paper(v2f i)
            {
                float type=i.uvLayer.z;
                float2 uv=i.uvLayer.xy;
                float2 p=i.island.xz;
                half3 change=0;
                if(type<.5) return i.paint.rgb;
                if(type<1.5)
                {
                    // Bend the texture across the island so broad clover clusters never form an obvious grid.
                    float2 shifted=p+float2(.53*sin(p.y*.61)+.36*cos(p.x*.44),.43*sin(p.x*.38+p.y*.24));
                    half3 ink=pigment(_Meadow,shifted/_MeadowSize);
                    half3 detail=ink-half3(.604,.712,.178);
                    half patches=.83+.17*sin(.42*p.x+.24*p.y)*cos(.39*p.y);
                    change=detail*half3(.48,.47,.35)*patches;
                    change+=.019*sin(p.x*.77+p.y*.29)*cos(p.y*.64);
                }
                else if(type<2.5 || (type>6.5 && type<7.5) || (type>9.5 && type<10.5))
                {
                    half3 ink=pigment(_Beach,uv/(type<2.5?3.2:2.6));
                    change=(ink-half3(.983,.828,.562))*(type<2.5?.52:.48);
                    if(type>6.5 && type<7.5)
                    {
                        // Grass interrupts the softly worn edge of the trail.
                        half tooth=pigment(_Meadow,p/1.85).g;
                        clip(i.paint.a-(tooth-.43)*1.46);
                        change-=half3(.024,.035,.047);
                    }
                }
                else if(type<3.5)
                {
                    float t=_Time.y*_TideSpeed;
                    float2 flow=float2(t*.008,-t*.0039);
                    float2 displaced=p/_SeaSize + float2(.11*sin(p.y*.35+t*.20),.071*sin(p.x*.32-t*.17));
                    half3 broad=pigment(_Lagoon,displaced+flow);
                    half3 cross=pigment(_Lagoon,displaced*1.48+float2(-.24,.32)-flow*.78);
                    half3 depth=lerp(broad,cross,.28)-half3(.392,.778,.700);
                    change=depth*half3(.46,.43,.40);
                    half gleam=smoothstep(.12,.29,depth.r)*(.60+.4*sin(t*.74+p.x*.74));
                    change+=half3(.10,.09,.066)*gleam;
                }
                else if(type<4.5)
                {
                    half3 ink=pigment(_Cedar,uv/float2(1.45,.84));
                    change=(ink-half3(.843,.545,.228))*.52;
                }
                else if(type<5.5)
                {
                    half3 ink=rockOrLeaf(_Canopy,i.island/1.93,i.objectNormal);
                    change=(ink-half3(.456,.608,.185))*.49;
                }
                else if(type<6.5)
                {
                    half3 ink=rockOrLeaf(_CoastalStone,i.island/1.21,i.objectNormal);
                    change=(ink-half3(.655,.648,.565))*.64;
                }
                else if(type<8.5)
                {
                    // Glints and waves follow the same gentle pulse as the shallows.
                    half pulse=.5+.5*sin(_Time.y*_TideSpeed*1.33+dot(p,float2(.81,.32)));
                    change=(pulse-.5)*half3(.17,.12,.08);
                }
                else
                {
                    half3 ink=pigment(_Beach,uv*2.05);
                    change=(ink-half3(.983,.828,.562))*.22;
                }
                return saturate(i.paint.rgb+change*_Detail);
            }
            fixed4 frag(v2f i) : SV_Target
            {
                half3 n = normalize(i.normal);
                half sun = saturate(dot(n, _WorldSpaceLightPos0.xyz));
                half sky = n.y * 0.5 + 0.5;
                half shadow = SHADOW_ATTENUATION(i);
                half3 shade = lerp(half3(.53,.60,.66), half3(.78,.83,.85), sky);
                half3 tint = shade + half3(.34,.28,.19) * sun * shadow;
                half3 color = saturate(paper(i) * tint);
                if (i.uvLayer.z>11.5) color=lerp(color,i.paint.rgb,.82);
                #ifndef UNITY_COLORSPACE_GAMMA
                color = GammaToLinearSpace(color);
                #endif
                return fixed4(color, 1);
            }
            ENDCG
        }
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
    Fallback "Diffuse"
}
