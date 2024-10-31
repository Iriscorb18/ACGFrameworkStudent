#version 450

in vec3 v_position;
in vec3 v_world_position;
in vec3 v_normal;

uniform vec3 u_camera_position;

uniform vec4 u_color;
uniform vec4 u_ambient_light;
uniform float u_absorption_coefficient;  // Absorption coefficient µa

uniform vec3 u_light_position;
uniform vec4 u_light_color;
uniform float u_light_intensity;
uniform float u_light_shininess;
uniform float u_step_length;

uniform vec3 u_box_min; // To receive the min bounds
uniform vec3 u_box_max; // To receive the max bounds

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



// Define the rand function
float rand(float n) {
    return fract(sin(n) * 43758.5453123);
}

#define M_PI 3.14159265358979323846

// Perlin noise functions
float rand(vec2 co) { return fract(sin(dot(co.xy, vec2(12.9898, 78.233))) * 43758.5453); }
float rand(vec2 co, float l) { return rand(vec2(rand(co), l)); }
float rand(vec2 co, float l, float t) { return rand(vec2(rand(co, l), t)); }

float perlin(vec3 p, float dim, float time) {
    vec3 pos = floor(p * dim);
    vec3 posX = pos + vec3(1.0, 0.0, 0.0);
    vec3 posY = pos + vec3(0.0, 1.0, 0.0);
    vec3 posZ = pos + vec3(0.0, 0.0, 1.0);
    vec3 posXY = pos + vec3(1.0, 1.0, 0.0);
    vec3 posXZ = pos + vec3(1.0, 0.0, 1.0);
    vec3 posYZ = pos + vec3(0.0, 1.0, 1.0);
    vec3 posXYZ = pos + vec3(1.0, 1.0, 1.0);
    
    float c = rand(pos.xy, dim, time);
    float cx = rand(posX.xy, dim, time);
    float cy = rand(posY.xy, dim, time);
    float cz = rand(posZ.xy, dim, time);
    float cxy = rand(posXY.xy, dim, time);
    float cxz = rand(posXZ.xy, dim, time);
    float cyz = rand(posYZ.xy, dim, time);
    float cxyz = rand(posXYZ.xy, dim, time);
    
    vec3 d = fract(p * dim);
    d = -0.5 * cos(d * M_PI) + 0.5;
    
    float ccx = mix(c, cx, d.x);
    float cycxy = mix(cy, cxy, d.x);
    float centerX = mix(ccx, cycxy, d.y);
    
    float ccxz = mix(cz, cxz, d.x);
    float cycxyz = mix(cy, cxyz, d.x);
    float centerZ = mix(ccxz, cycxyz, d.y);
    
    return mix(centerX, centerZ, d.z) * 2.0 - 1.0;
}

// p must be normalized!
float perlin(vec3 p, float dim) {
    return perlin(p, dim, 0.0);
}
float mod289(float x){return x - floor(x * (1.0 / 289.0)) * 289.0;}
vec4 mod289(vec4 x){return x - floor(x * (1.0 / 289.0)) * 289.0;}
vec4 perm(vec4 x){return mod289(((x * 34.0) + 1.0) * x);}

float noise(vec3 p){
    vec3 a = floor(p);
    vec3 d = p - a;
    d = d * d * (3.0 - 2.0 * d);

    vec4 b = a.xxyy + vec4(0.0, 1.0, 0.0, 1.0);
    vec4 k1 = perm(b.xyxy);
    vec4 k2 = perm(k1.xyxy + b.zzww);

    vec4 c = k2 + a.zzzz;
    vec4 k3 = perm(c);
    vec4 k4 = perm(c + 1.0);

    vec4 o1 = fract(k3 * (1.0 / 41.0));
    vec4 o2 = fract(k4 * (1.0 / 41.0));

    vec4 o3 = o2 * d.z + o1 * (1.0 - d.z);
    vec2 o4 = o3.yw * d.x + o3.xz * (1.0 - d.x);

    return o4.y * d.y + o4.x * (1.0 - d.y);
}

void main() {
    vec3 ray_origin = u_camera_position;
    vec3 ray_direction = normalize(v_world_position - u_camera_position);

    vec2 tHit = intersectAABB(ray_origin, ray_direction, u_box_min, u_box_max);
    float tb = max(tHit.x, 0.0);  
    float ta = tHit.y;

    vec4 final_color = u_ambient_light; 
    
    if (tb <= ta && ta > 0.0) {
        float t = tb;
        float accumulated_optical_thickness = 0.0;

        while (t < ta) {
            vec3 sample_position = ray_origin + t * ray_direction;

            // Use the 3D noise function to calculate a local absorption coefficient
            float noise_value = perlin(sample_position, 10.0); // Adjust dim parameter as needed
            float local_absorption_coefficient = noise_value * u_absorption_coefficient;

            // Increment optical thickness using the calculated absorption coefficient
            accumulated_optical_thickness += local_absorption_coefficient * u_step_length;

            // Advance the ray using the uniform step length
            t += u_step_length;
        }

        float transmittance = exp(-accumulated_optical_thickness);
        final_color += transmittance;
    }

    FragColor = final_color;
}
// void main()
// {
//     // Initialize a ray from the camera to the current fragment position
//     vec3 ray_origin = u_camera_position;
//     vec3 ray_direction = normalize(v_world_position - u_camera_position);

//     // Compute intersections with the volume auxiliary geometry
//     vec2 tHit = intersectAABB(ray_origin, ray_direction, u_box_min, u_box_max);
//     float tb = tHit.x;  // Clamp near intersection to non-negative
//     float ta = tHit.y;  // Clamp far intersection to non-negative


//     // Default to background color if no hit
//     vec4 final_color = u_ambient_light; 
    
//     if (tb <= ta && ta > 0.0) {
//         // Compute the optical thickness using the Beer-Lambert Law
//         float optical_thickness = (ta - tb) * u_absorption_coefficient;
//         float transmittance = exp(-optical_thickness);

//         // Compute the final color: L(t) = B * e^(-(tb - ta) * µa)
//         final_color += transmittance;
//     }

//     FragColor = final_color;
// }