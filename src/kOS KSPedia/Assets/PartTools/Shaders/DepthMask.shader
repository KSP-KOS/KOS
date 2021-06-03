Shader "DepthMask" {
    SubShader {
    	Tags { "Queue" = "Background" }
        Lighting Off
        ZTest LEqual
        ZWrite On
        Cull Off
        ColorMask 0
        Pass {}
    }
}