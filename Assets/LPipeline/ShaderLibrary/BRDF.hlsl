#ifndef CUSTOM_BRDF_INCLUDED
#define CUSTOM_BRDF_INCLUDED
#define MIN_REFLECTIVITY 0.04
#define SPEC_MULTIPIER 800

float OneMinusReflectivity (float metallic) {
	float range = 1.0 - MIN_REFLECTIVITY;
	return range - metallic * range;
}

struct BRDF {
	float3 diffuse;
	float3 specular;
	float roughness;
};

BRDF GetBRDF (inout Surface surface, bool applyAlphaToDiffuse = false) {
	BRDF brdf;
	brdf.diffuse = surface.color * OneMinusReflectivity(surface.metallic);
    if(applyAlphaToDiffuse)
    {
        brdf.diffuse *= surface.alpha;
    }
	brdf.specular =  lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);
	float perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
	brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
	return brdf;
}

float MinimalistSpecular (Surface surface, BRDF brdf, Light light) {
	float3 h = SafeNormalize(light.direction + surface.viewDirection);
	float nh2 = Square(saturate(dot(surface.normal, h)));
	float lh2 = Square(saturate(dot(light.direction, h)));
	float r2 = Square(brdf.roughness);
	float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
	float normalization = brdf.roughness * 4.0 + 2.0;
	return r2 / (d2 * max(0.1, lh2) * normalization);
}

float3 GetMinimalistDirectBRDF (Surface surface, BRDF brdf, Light light) {
	return MinimalistSpecular(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

// Reference: http://graphicrants.blogspot.com/2013/08/specular-brdf-reference.html
float D_GGX(in float alpha, in float NoH)
{
    float a2 = alpha*alpha;
    float cos2 = NoH*NoH;
    return (1.0f/M_PI) * sqr(alpha/(cos2 * (a2 - 1) + 1));
}

float G_Schlick(in float alpha, in float NoV)
{
    float k = alpha/2;
    return NoV/(NoV * (1 - k) + k);
}

float F_Schlick(in float f0, in float LoH)
{
    return (f0 + (1.0f - f0) * pow(1.0f - LoH, 5.0f));
}

float3 GetDirectBRDF(Surface surface, BRDF brdf, Light light)
{
    float alpha = brdf.roughness * brdf.roughness;
	float3 L = light.direction;
	float3 V = surface.viewDirection;
	float3 N = surface.normal;
    float3 H = normalize(L+V);
    float NoL = dot(N, L);
    float NoV = dot(N, V);
    float NoH = dot(N, H);
    float LoH = dot(L, H);
    // refractive index
    float n = 1.5;
    float f0 = pow((1 - n)/(1 + n), 2);
    // the specular fresnel
    float Fs = F_Schlick(f0, LoH);
	// the diffuse fresnel
    float Fd = F_Schlick(f0, NoL);
    // the geometry
    float G = G_Schlick(alpha, NoV);
    // the distribution
    float D = D_GGX(alpha, NoH);
    // specular
    float3 Rs = (brdf.specular*SPEC_MULTIPIER/M_PI *(Fs * G * D))/(4 * NoL * NoV);
	// diffuse
    float3 Rd = brdf.diffuse/M_PI * (1.0f - Fd);
    return (Rd + Rs);
}

#endif