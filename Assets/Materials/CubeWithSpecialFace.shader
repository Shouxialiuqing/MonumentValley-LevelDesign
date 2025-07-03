Shader "Custom/CubeWithSpecialFace_LocalSpace"
{
    Properties
    {
        // ������������
        _MainTex ("Base Texture", 2D) = "white" {}
        _MainColor ("Base Color", Color) = (1,1,1,1)
        
        // �������������
        _SpecialTex ("Special Face Texture", 2D) = "white" {}
        _SpecialColor ("Special Face Color", Color) = (1,0,0,1)
        _SpecialFaceNormal ("Special Face Direction (Local Space)", Vector) = (0,0,1,0) // ��Ϊ�ֲ�����ϵ
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
        float3 _SpecialFaceNormal; // ע�⣺������Ȼ�Ǿֲ�����ϵ��ֵ

        struct Input
        {
            float2 uv_MainTex;
            float3 worldNormal; // ����ռ䷨�ߣ���Ҫת����
            float3 worldPos;   // ��������ȡ���������Լ���ֲ�����
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // �ؼ��޸ģ������編��ת��������ֲ�����ϵ
            float3 localNormal = mul(unity_WorldToObject, float4(IN.worldNormal, 0)).xyz;
            localNormal = normalize(localNormal);

            // �ж��Ƿ��������棨ʹ�þֲ�����ϵ���ߣ�
            if (dot(localNormal, _SpecialFaceNormal) > 0.9)
            {
                // �����棺��ͼ �� ������ɫ
                fixed4 specialTex = tex2D(_SpecialTex, IN.uv_MainTex);
                o.Albedo = specialTex.rgb * _SpecialColor.rgb;
            }
            else
            {
                // �����棺��ͼ �� ������ɫ
                fixed4 mainTex = tex2D(_MainTex, IN.uv_MainTex);
                o.Albedo = mainTex.rgb * _MainColor.rgb;
            }
        }
        ENDCG
    }
    FallBack "Diffuse"
}