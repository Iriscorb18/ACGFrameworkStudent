#version 450

in vec3 v_position;
in vec3 v_world_position;
in vec3 v_normal;

uniform vec3 u_camera_position;
uniform vec3 u_localcamera_position;

uniform vec4 u_color;
uniform vec4 u_ambient_light;
uniform float u_absorption_coefficient;  // Absorption coefficient µa

uniform vec3 u_light_position;
uniform vec4 u_light_color;
uniform float u_light_intensity;
uniform float u_light_shininess;
uniform float u_step_length;
uniform float u_random;


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

float fbm(vec3 x) {
	float v = 0.0;
	float a = 0.5;
	vec3 shift = vec3(100);
	for (int i = 0; i < 5; ++i) {
		v += a * noise(x);
		x = x * 2.0 + shift;
		a *= 0.5;
	}
	return v;
}

vec3 random3(vec3 c) {
	float j = 4096.0*sin(dot(c,vec3(17.0, 59.4, 15.0)));
	vec3 r;
	r.z = fract(512.0*j);
	j *= .125;
	r.x = fract(512.0*j);
	j *= .125;
	r.y = fract(512.0*j);
	return r-0.5;
}

const float F3 =  0.3333333;
const float G3 =  0.1666667;
float snoise(vec3 p) {

	vec3 s = floor(p + dot(p, vec3(F3)));
	vec3 x = p - s + dot(s, vec3(G3));
	 
	vec3 e = step(vec3(0.0), x - x.yzx);
	vec3 i1 = e*(1.0 - e.zxy);
	vec3 i2 = 1.0 - e.zxy*(1.0 - e);
	 	
	vec3 x1 = x - i1 + G3;
	vec3 x2 = x - i2 + 2.0*G3;
	vec3 x3 = x - 1.0 + 3.0*G3;
	 
	vec4 w, d;
	 
	w.x = dot(x, x);
	w.y = dot(x1, x1);
	w.z = dot(x2, x2);
	w.w = dot(x3, x3);
	 
	w = max(0.6 - w, 0.0);
	 
	d.x = dot(random3(s), x);
	d.y = dot(random3(s + i1), x1);
	d.z = dot(random3(s + i2), x2);
	d.w = dot(random3(s + 1.0), x3);
	 
	w *= w;
	w *= w;
	d *= w;
	 
	return dot(d, vec4(52.0));
}

float snoiseFractal(vec3 m) {
	return   0.5333333* snoise(m)
				+0.2666667* snoise(2.0*m)
				+0.1333333* snoise(4.0*m)
				+0.0666667* snoise(8.0*m);
}

void main() {
    vec3 ray_origin =u_localcamera_position;
    vec3 ray_direction = normalize(v_world_position - u_localcamera_position);

    vec2 tHit = intersectAABB(ray_origin, ray_direction, u_box_min, u_box_max);
    float tb = max(tHit.x, 0.0);  
    float ta = tHit.y;
    float random = u_random;

    vec4 final_color = u_ambient_light; 
    
    if (tb <= ta && ta > 0.0) {
        float t = tb;
        float accumulated_optical_thickness = 0.0;

        while (t < ta) {
            vec3 sample_position = ray_origin + t * ray_direction;

            // Use the 3D noise function to calculate a local absorption coefficient
            float noise_value = snoiseFractal(v_world_position); 
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