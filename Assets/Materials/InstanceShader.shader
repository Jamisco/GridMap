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

            int i = 0;
            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);

                o.color = _MeshColors[unity_InstanceID];
                
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