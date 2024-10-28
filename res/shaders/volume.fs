#version 450

in vec3 v_position;
in vec3 v_world_position;
in vec3 v_normal;

uniform vec3 u_camera_position;

uniform vec4 u_color;
uniform vec4 u_ambient_light;

uniform vec3 u_light_position;
uniform vec4 u_light_color;
uniform float u_light_intensity;
uniform float u_light_shininess;
// Absorption Model parameters
uniform vec4 u_background_color;  // Background color B
uniform float u_absorption_coefficient;  // Absorption coefficient µa

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

void main()
{
    // Initialize a ray from the camera to the current fragment position
    vec3 ray_origin = u_camera_position;
    vec3 ray_direction = normalize(v_world_position - u_camera_position);

    // Compute intersections with the volume auxiliary geometry
    vec2 tHit = intersectAABB(ray_origin, ray_direction, u_box_min, u_box_max);
    float tb = tHit.x;  // Clamp near intersection to non-negative
    float ta = tHit.y;  // Clamp far intersection to non-negative


    // Default to background color if no hit
    vec4 final_color = u_background_color; 
    if (tb <= ta && ta > 0.0) {
        // Compute the optical thickness using the Beer-Lambert Law
        float optical_thickness = (ta - tb) * u_absorption_coefficient;
        float transmittance = exp(-optical_thickness);
        // Compute the final color: L(t) = B * e^(-(tb - ta) * µa)
        final_color += transmittance;
    }

    FragColor = final_color;
}