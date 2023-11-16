  Shader "Example/TestShader" 
  { 
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Texture1("Texture", 2D) = "white" {}
        _Texture2("Texture", 2D) = "white" {}
        
        _UseColor("UseColor", Float) = 0
        _Color ("Color", Color) = (1,1,1,1)
        
        _HeightColor("HeightColor", Vector) = (1,10,0,0)
        _HeightRange("HeightRange", Vector) = (0,5, 0,0)
        
        _Text2Lerp("Texture2Blend", Range(0,1)) = 0
        _Text3Lerp("Texture3Blend", Range(0,1)) = 0
        
    }
    
    SubShader 
    {
       Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        
        Pass
        {

            //Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM   
            
            #include "UnityCG.cginc"
            
            #pragma vertex vert
            #pragma fragment frag
       
            float _UseColor;
            float4 _Color;
            
            float2 _HeightColor;
            float2 _HeightRange;

            float _Text2Lerp;
            float _Text3Lerp;
            
            
            sampler2D _MainTex;
            sampler2D _Texture1;
            sampler2D _Texture2;
            
            struct appdata {
                float4 vertex : POSITION;
                float4 color : COLOR; // Assuming the colors are provided as float4 in the C# script
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float y : float;
            };
            
            float2 hash2D2D (float2 s)
            {
	            //magic numbers
	            return frac(sin(fmod(float2(dot(s, float2(127.1,311.7)), dot(s, float2(269.5,183.3))), 3.14159))*43758.5453);
            }
 
            //stochastic sampling
            float4 tex2DStochastic(sampler2D tex, float2 UV)
            {
	            //triangle vertices and blend weights
	            //BW_vx[0...2].xyz = triangle verts
	            //BW_vx[3].xy = blend weights (z is unused)
	            float4x3 BW_vx;
 
	            //uv transformed into triangular grid space with UV scaled by approximation of 2*sqrt(3)
	            float2 skewUV = mul(float2x2 (1.0 , 0.0 , -0.57735027 , 1.15470054), UV * 3.464);
 
	            //vertex IDs and barycentric coords
	            float2 vxID = float2 (floor(skewUV));
	            float3 barry = float3 (frac(skewUV), 0);
	            barry.z = 1.0-barry.x-barry.y;
 
	            BW_vx = ((barry.z>0) ? 
		            float4x3(float3(vxID, 0), float3(vxID + float2(0, 1), 0), float3(vxID + float2(1, 0), 0), barry.zyx) :
		            float4x3(float3(vxID + float2 (1, 1), 0), float3(vxID + float2 (1, 0), 0), float3(vxID + float2 (0, 1), 0), float3(-barry.z, 1.0-barry.y, 1.0-barry.x)));
 
	            //calculate derivatives to avoid triangular grid artifacts
	            float2 dx = ddx(UV);
	            float2 dy = ddy(UV);
 
	            //blend samples with calculated weights
	            return mul(tex2D(tex, UV + hash2D2D(BW_vx[0].xy), dx, dy), BW_vx[3].x) + 
			            mul(tex2D(tex, UV + hash2D2D(BW_vx[1].xy), dx, dy), BW_vx[3].y) + 
			            mul(tex2D(tex, UV + hash2D2D(BW_vx[2].xy), dx, dy), BW_vx[3].z);
            }

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                
                o.y = mul(unity_ObjectToWorld, v.vertex).y;
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target 
            {                     
                if(_UseColor == 1)
				{
					float4 final = i.color * _Color;
                    
                    fixed4 white = (1,1,1,1);
                    
                    return lerp(final, white, _Text2Lerp);
				}
                
                fixed4 col1 = tex2D(_MainTex, i.uv);
                fixed4 col2 = tex2D(_Texture1, i.uv);
                fixed4 col3 = tex2D(_Texture2, i.uv);

                //fixed4 lerp1 = lerp(col1, col2, _Text2Lerp);

                //fixed4 final_color = lerp(lerp1, col3, _Text3Lerp);
            
                float4 col = col1 * col2;
                
                //float4 stoch = tex2DStochastic(_MainTex, i.uv);               
                //float4 col = stoch;
                
                fixed4 ay = (_HeightColor.y - _HeightColor.x) * ((i.y - _HeightRange.x) / (_HeightRange.y - _HeightRange.x)) + _HeightColor.x;
                
                col *= ay;
                return col;
            }   
            
            ENDHLSL
        }  
    }
}