Shader "Custom/Triplanar"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        
        [Header(NormalMap)]
        _BumpTex ("Normal Map", 2D) = "bump"{}
        _BumpStrength ("Normal Strength", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal; INTERNAL_DATA
        };
        sampler2D _MainTex;
        float4 _MainTex_ST;

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        sampler2D _BumpTex;
        float _BumpStrength;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            IN.worldNormal = WorldNormalVector(IN, float3(0,0,1));
            
            float2 topUV = IN.worldPos.xz * _MainTex_ST.xy + _MainTex_ST.zw;
            float4 topColor = tex2D(_MainTex, topUV);
            
            float4 c = topColor;

            // Bump
            float3 topNormal = UnpackNormalWithScale(tex2D(_BumpTex, topUV), _BumpStrength);
            //topNormal = lerp(float3(0,0,1), topNormal, IN.worldNormal.y);
            
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;

            o.Normal = topNormal;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
