  Shader "Example/TestShader" 
  { 
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex("Texture", 2D) = "white" {}
        _HeightRange("HeightRange", Vector) = (0,5, 0,0)
        _HeightColor("HeightColor", Vector) = (1,10,0,0)
    }
    
    SubShader 
    {
       Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        
        Pass
        {

            //Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM   
            
            #include "UnityCG.cginc"
            
            #pragma enable_d3d11_debug_symbols
            #pragma vertex vert
            #pragma fragment frag
       
            float2 _HeightRange;
            float2 _HeightColor;
            
            float4 _Color;
            
            sampler2D _MainTex;
            
            struct appdata {
                float4 vertex : POSITION;
                float4 color : COLOR; // Assuming the colors are provided as float4 in the C# script
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float y: float;
            };

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                o.y = v.vertex.y;
                return o;
            }

            // Fragment shader
            // This shader simply passes through the color to the output

            fixed4 frag(v2f i) : SV_Target {
            
                fixed4 col = tex2D(_MainTex, i.uv);

                col *= i.color * _Color;
                
                float ay = (_HeightColor.y - _HeightColor.x) * ((i.y - _HeightRange.x) / (_HeightRange.y - _HeightRange.x)) + _HeightColor.x;
                
                col *= ay;
                return col;
            }   
            
            ENDHLSL
        }  
    }
}