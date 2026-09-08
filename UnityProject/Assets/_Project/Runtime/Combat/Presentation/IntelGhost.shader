Shader "EFTM/IntelGhost"
{
    Properties { _Color ("Old intel", Color) = (1, .78, .04, .38) }
    SubShader
    {
        Tags { "Queue"="Transparent+100" "RenderType"="Transparent" }
        ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Cull Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color;
            float4 vert(appdata_base v) : SV_POSITION { v.vertex.xyz += v.normal * .035; return UnityObjectToClipPos(v.vertex); }
            fixed4 frag() : SV_Target { return fixed4(_Color.rgb, .65); }
            ENDCG
        }
        Pass
        {
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color;
            float4 vert(appdata_base v) : SV_POSITION { return UnityObjectToClipPos(v.vertex); }
            fixed4 frag() : SV_Target { return _Color; }
            ENDCG
        }
    }
}
