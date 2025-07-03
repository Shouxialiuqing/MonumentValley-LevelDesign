Shader "Custom/CubeWithSpecialFace_LocalSpace"
{
    Properties
    {
        // 基础材质属性
        _MainTex ("Base Texture", 2D) = "white" {}
        _MainColor ("Base Color", Color) = (1,1,1,1)
        
        // 特殊面材质属性
        _SpecialTex ("Special Face Texture", 2D) = "white" {}
        _SpecialColor ("Special Face Color", Color) = (1,0,0,1)
        _SpecialFaceNormal ("Special Face Direction (Local Space)", Vector) = (0,0,1,0) // 改为局部坐标系
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard

        sampler2D _MainTex;
        fixed4 _MainColor;
        sampler2D _SpecialTex;
        fixed4 _SpecialColor;
        float3 _SpecialFaceNormal; // 注意：这里仍然是局部坐标系的值

        struct Input
        {
            float2 uv_MainTex;
            float3 worldNormal; // 世界空间法线（需要转换）
            float3 worldPos;   // 新增：获取世界坐标以计算局部法线
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // 关键修改：将世界法线转换到物体局部坐标系
            float3 localNormal = mul(unity_WorldToObject, float4(IN.worldNormal, 0)).xyz;
            localNormal = normalize(localNormal);

            // 判断是否在特殊面（使用局部坐标系法线）
            if (dot(localNormal, _SpecialFaceNormal) > 0.9)
            {
                // 特殊面：贴图 × 特殊颜色
                fixed4 specialTex = tex2D(_SpecialTex, IN.uv_MainTex);
                o.Albedo = specialTex.rgb * _SpecialColor.rgb;
            }
            else
            {
                // 其他面：贴图 × 基础颜色
                fixed4 mainTex = tex2D(_MainTex, IN.uv_MainTex);
                o.Albedo = mainTex.rgb * _MainColor.rgb;
            }
        }
        ENDCG
    }
    FallBack "Diffuse"
}