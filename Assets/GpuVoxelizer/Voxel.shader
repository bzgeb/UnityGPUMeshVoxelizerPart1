Shader "Custom/Voxel"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0, 0, 1)
    }

    SubShader
    {
        Pass 
        {
            Name "Draw Points"
            Tags { "RenderType" = "Opaque" }
            Blend SrcAlpha one

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            
            #pragma target 5.0
            struct v2f
            {
                float4 position : SV_POSITION;
                float4 color : COLOR;
                float size : PSIZE;
            };

            float4x4 _LocalToWorldMatrix;
            StructuredBuffer<float4> _VoxelGridPoints;
            float4 _Color;

            v2f vert(uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID)
            {
                v2f o;
                float4 pos = _VoxelGridPoints[instance_id];
                pos.w = 1.0;
                o.position = UnityWorldToClipPos(mul(_LocalToWorldMatrix, pos));
                o.size = 5;
                o.color = _Color;
                return o;
            }

            float4 frag(v2f i) : COLOR
            {
                return i.color;
            }
            ENDCG
        }
    }
    FallBack Off
}