#version 450

in vec3 v_position;
in vec3 v_world_position;
in vec3 v_normal;

uniform vec3 u_camera_position;
uniform vec3 u_localcamera_position;

uniform vec4 u_color;
uniform vec4 u_background;
uniform float u_absorption_coefficient; //absoption coefficient 
uniform float u_scatter_coefficient; //scatter coefficient 
uniform float u_g; //G value of phase factor 
uniform vec4 u_emission_color;
uniform float u_emission_intensity;
uniform int u_num_step;

//LIGTH 
uniform vec3 u_light_position;
uniform vec4 u_light_color;
uniform float u_light_intensity;
uniform float u_light_shininess;

uniform float u_step_length; //STEP LENGHT 

//NOISE
uniform float u_noise_scale;
uniform int u_noise_detail;

//VOLUME DENSITIES
uniform float u_density_scale;
uniform int u_density_source; // 0: constant, 1: noise, 2: VDB
uniform sampler3D u_density_texture;

//VOLUME TYPE 
uniform int u_volume_type; //0:Homogeneous, 1:Heterogeneous

//VOLUME BOX
uniform vec3 u_box_min; 
uniform vec3 u_box_max; 

//JITTERING
uniform bool u_jittering; //0:don't use jittering, 1:use jittering
uniform float u_threshold; //threshold 

out vec4 FragColor; //FINAL COLOR 

//FUNCTION TO KNOW THE INTERECTIONS 
vec2 intersectAABB(vec3 rayOrigin, vec3 rayDir, vec3 boxMin, vec3 boxMax) {
    vec3 rayDirSafe = rayDir + vec3(1e-6); // Prevent division by zero
    vec3 tMin = (boxMin - rayOrigin) / rayDirSafe;
    vec3 tMax = (boxMax - rayOrigin) / rayDirSafe;
    vec3 t1 = min(tMin, tMax);
    vec3 t2 = max(tMin, tMax);
    float tNear = max(max(t1.x, t1.y), t1.z);
    float tFar = min(min(t2.x, t2.y), t2.z);
    return vec2(tNear, tFar);
}

// NOISE FUNCTIONS
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

//FUNCTION TO COMPUTE THE DENSITIES DEPEND ON THE TYPES THAT IS USED
float sampleDensity(vec3 position) {
    if (u_density_source == 0) {   //CONSTANT
        return 1.0 * u_density_scale;
    }
    else if (u_density_source == 1) {  //NOISE
        return noise(position * u_noise_scale) * u_density_scale;
    }
    else if (u_density_source == 2) {  //VDB
        vec3 position_texture = (position + vec3(1.0)) / 2.0; //CHANGE THE LOCAL COORDINATES TO A TEXTURE COORDINATES
        return texture(u_density_texture, position_texture).r * u_density_scale;
    }
    return 0.0;
}

// FUNCTION TO GENERATE A RANDOM NUMBER BETWEEN 0 AND 1
float random (vec2 st) {
    return fract(sin(dot(st.xy,
                         vec2(12.9898,78.233)))*
        43758.5453123);
}

//MAIN
void main() {

    //initialize the ray 
    vec3 ray_origin = u_localcamera_position;
    vec3 ray_direction = normalize(v_world_position - u_localcamera_position);

    float jitterOffset = random(gl_FragCoord.xy);

    float jittered_ta = jitterOffset * u_step_length * float(u_jittering); // random offset for the start position

    //find the intersection with the auxiliarity mesh
    vec2 tHit = intersectAABB(ray_origin, ray_direction, u_box_min, u_box_max);
    float tb = tHit.y; //tmax
    float ta = tHit.x + jittered_ta; // tmin with jittered offset

    vec4 final_color = u_background;
    vec4 radiance = vec4(0.0);

    if (u_volume_type == 0) {
        if (ta <= tb && tb > 0.0) {
            float optical_thickness = (tb - ta) * u_absorption_coefficient;
            float transmittance = exp(-optical_thickness);
            final_color *= transmittance;
        }
    
    } else if (u_volume_type != 0) {
        //float fx = 1/(4 * 3.14); //phase function (isotropic)
        if (ta <= tb && tb > 0.0) {
            float t = ta; //
            float accumulated_optical_thickness = 0.0; //T(0, tmax)
            float accumulated_transmittance = 1.0; 

            while (t < tb) {
                vec3 sample_position = ray_origin + t * ray_direction; //initialize the sample position 
                float density = sampleDensity(sample_position); //get the density 

                //if the density higher than threshold, paint
                if (density > u_threshold) {
                    final_color = vec4(1.0,0.0,1.0,1.0);
                    break;
                }
                
                t += u_step_length; // update the t
            }
        }
    }

    FragColor = final_color;
}