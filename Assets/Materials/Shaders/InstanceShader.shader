Shader "Custom/InstanceShader"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex   : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 _MeshColors[1023];   // Max instanced batch size.
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID // use this to access instanced properties in the fragment shader.
            };

            v2f vert(appdata v)
            {
                int i = 0;
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);

                // accesing the instance id must be done inside this block!
                #ifdef INSTANCING_ON
                    i = v.instanceID;
                
                #endif

                // if you get an error, that is because you are tring to access the instance if outside the #ifdef INSTANCING_ON block,
                // see  UNITY_VERTEX_INPUT_INSTANCE_ID in link https://docs.unity3d.com/2021.2/Documentation/Manual/gpu-instancing-shader.html
                o.color = _MeshColors[i];
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
            	return i.color;
            }
            ENDCG
        }
    }
}