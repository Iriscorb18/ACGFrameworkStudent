#version 450

in vec3 v_position;
in vec3 v_world_position;
in vec3 v_normal;

uniform vec3 u_camera_position;
uniform vec3 u_localcamera_position;

uniform vec4 u_color;
uniform vec4 u_background;
uniform float u_absorption_coefficient;  // Absorption coefficient µa

uniform vec3 u_light_position;
uniform vec4 u_light_color;
uniform float u_light_intensity;
uniform float u_light_shininess;

uniform float u_step_length;
uniform float u_noise_scale;
uniform int u_noise_detail;

uniform int u_volume_type;

uniform vec4 u_emission_color;
uniform float u_emission_intensity;

uniform vec3 u_box_min; 
uniform vec3 u_box_max; 

// Outputs
out vec4 FragColor;

// Function to compute ray-box intersection using AABB
vec2 intersectAABB(vec3 rayOrigin, vec3 rayDir, vec3 boxMin, vec3 boxMax) {
    vec3 tMin = (boxMin - rayOrigin) / rayDir;
    vec3 tMax = (boxMax - rayOrigin) / rayDir;
    vec3 t1 = min(tMin, tMax);
    vec3 t2 = max(tMin, tMax);
    float tNear = max(max(t1.x, t1.y), t1.z);
    float tFar = min(min(t2.x, t2.y), t2.z);
    return vec2(tNear, tFar);
}

// Noise functions
float hash1( float n )
{
    return fract( n*17.0*fract( n*0.3183099 ) );
}

float noise( vec3 x )
{
    vec3 p = floor(x);
    vec3 w = fract(x);
    
    vec3 u = w*w*w*(w*(w*6.0-15.0)+10.0);
    
    float n = p.x + 317.0*p.y + 157.0*p.z;
    
    float a = hash1(n+0.0);
    float b = hash1(n+1.0);
    float c = hash1(n+317.0);
    float d = hash1(n+318.0);
    float e = hash1(n+157.0);
    float f = hash1(n+158.0);
    float g = hash1(n+474.0);
    float h = hash1(n+475.0);

    float k0 =   a;
    float k1 =   b - a;
    float k2 =   c - a;
    float k3 =   e - a;
    float k4 =   a - b - c + d;
    float k5 =   a - c - e + g;
    float k6 =   a - b - e + f;
    float k7 = - a + b + c - d + e - f - g + h;

    return -1.0+2.0*(k0 + k1*u.x + k2*u.y + k3*u.z + k4*u.x*u.y + k5*u.y*u.z + k6*u.z*u.x + k7*u.x*u.y*u.z);
}

#define MAX_OCTAVES 16

float fractal_noise( vec3 P, float detail )
{
    float fscale = 1.0;
    float amp = 1.0;
    float sum = 0.0;
    float octaves = clamp(detail, 0.0, 16.0);
    int n = int(octaves);

    for (int i = 0; i <= MAX_OCTAVES; i++) {
        if (i > n) continue;
        float t = noise(fscale * P);
        sum += t * amp;
        amp *= 0.5;
        fscale *= 2.0;
    }

    return sum;
}

float cnoise( vec3 P, float scale, float detail )
{
    P *= scale;
    return clamp(fractal_noise(P, detail), 0.0, 1.0);
}

// MAIN
void main() {
    vec3 ray_origin = u_camera_position;
    vec3 ray_direction = normalize(v_world_position - u_camera_position);

    vec4 accumulated_color = vec4(0.0);
    vec4 emitted_radiance = vec4(0.0);
    
    vec2 tHit = intersectAABB(ray_origin, ray_direction, u_box_min, u_box_max);
    float tb = tHit.y;  
    float ta = tHit.x;

    vec4 final_color = u_background; 
    
    if(u_volume_type == 0){
	    if (ta <= tb && tb > 0.0) {
        
        float optical_thickness = (tb - ta) * (u_absorption_coefficient);
		float transmittance = exp(-optical_thickness);

        final_color *= transmittance;
    	}
	}

	else if(u_volume_type != 0){
		if (ta <= tb && tb > 0.0) {
        float t = ta;
        float accumulated_optical_thickness = 0.0;
        float accumulated_transmittance = 1.0;

        while (t < tb) {
            vec3 sample_position = ray_origin + t * ray_direction;

            float noise_value = cnoise(sample_position, u_noise_scale, u_noise_detail);
            float local_absorption_coefficient = noise_value * (u_absorption_coefficient);

            accumulated_optical_thickness += local_absorption_coefficient * u_step_length;
            
            float step_transmittance = exp(-local_absorption_coefficient * u_step_length);
            accumulated_transmittance *= step_transmittance;
           
            emitted_radiance += u_emission_color * u_emission_intensity * local_absorption_coefficient * accumulated_transmittance;
            
            t += u_step_length;
        }
        float transmittance = exp(-accumulated_optical_thickness);

        final_color = emitted_radiance + u_background * transmittance;
   		}
	}
    
    FragColor = final_color;
}
