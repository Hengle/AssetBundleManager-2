// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Zephyr/NormalTangentSpaceCartoon"
{
	Properties{
		_Color("Color", Color) = (1,1,1,1)
        _MainTex("Main Tex", 2D) = "white"{}
        _BumpMap("Normal Map", 2D) = "bump"{}
        _BumpScale("Bump Scale", Float) = 1.0
        _Specular("Specular", Color) = (1,1,1,1)
        _Gloss("Gloss", Range(8,256)) = 20
        _Floors("Floors", int) = 4
	}

	SubShader{
		Pass{
			Tags{
				"LightMode" = "ForwardBase"
			}

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			
			#include "Lighting.cginc"

			fixed4 _Color;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _BumpMap;
            float4 _BumpMap_ST;
            float _BumpScale;
            fixed4 _Specular;
            float _Gloss;
            int _Floors;

			struct a2v{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float4 texcoord : TEXCOORD0;
			};

			struct v2f{
				float4 pos : SV_POSITION;
                float4 uv : TEXCOORD0;
                float3 lightDir : TEXCOORD1;
				float3 viewDir : TEXCOORD2;
			};

			v2f vert(a2v v){
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);

                o.uv.xy = v.texcoord.xy * _MainTex_ST.xy +_MainTex_ST.zw;
                o.uv.zw = v.texcoord.xy * _BumpMap_ST.xy +_BumpMap_ST.zw;

                float3 binormal = cross(normalize(v.normal), normalize(v.tangent.xyz)) * v.tangent.w;
                float3x3 rotation = float3x3(v.tangent.xyz, binormal, v.normal);
                
				o.lightDir = mul(rotation, ObjSpaceLightDir(v.vertex)).xyz;
				o.viewDir = mul(rotation, ObjSpaceViewDir(v.vertex)).xyz;

				return o;
			}

			fixed4 frag(v2f i) : SV_TARGET{                
				fixed3 tangentLightDir = normalize(i.lightDir);
                fixed3 tangentViewDir = normalize(i.viewDir);

                fixed4 packedNormal = tex2D(_BumpMap, i.uv.zw);
                fixed3 tangentNormal;
                tangentNormal = UnpackNormal(packedNormal);
                tangentNormal.xy *= _BumpScale;
                tangentNormal.z = sqrt(1.0-saturate(dot(tangentNormal.xy, tangentNormal.xy)));
                
                fixed3 albedo = tex2D(_MainTex, i.uv).rgb * _Color.rgb;
                fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz * albedo;
                
                //将光线强度阶梯化
                float lightStrength = dot(tangentNormal, tangentLightDir)*0.5+0.5;
                float flooredLightStrenth = (float)(((int)(lightStrength*_Floors)))/_Floors;
                fixed3 diffuse = _LightColor0.rgb * albedo * flooredLightStrenth;

                fixed3 halfDir = normalize(tangentLightDir + tangentViewDir);

                //specular高光只留一阶
                float specularStrength = pow(saturate(dot(tangentNormal, halfDir)), _Gloss);
                float flooredSpecularStrength = (float)(((int)(specularStrength*2)))/2;

                fixed3 specular = _LightColor0.rgb * _Specular.rgb * flooredSpecularStrength;
                
				return fixed4(ambient + diffuse + specular, 1);
			}

			ENDCG
		}
	}
}
