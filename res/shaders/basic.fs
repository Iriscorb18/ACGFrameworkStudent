#version 450 core

// Uniforms for user-controlled parameters
uniform vec3 u_backgroundColor;      // Background color B
uniform float u_absorptionCoefficient; // Absorption coefficient μa

// Input for ray parameters (assuming you already calculate these)
in vec3 v_rayOrigin;  // Make sure to declare this in the vertex shader too
in vec3 v_rayDirection; // Make sure to declare this in the vertex shader too

// Output color for each fragment
out vec4 FragColor; // Correctly declare the output color

// Function to compute the intersection points (e.g., with a box)
bool intersectVolume(vec3 origin, vec3 direction, out float tEnter, out float tExit) {
    // Calculate intersection points with volume geometry (a cube or sphere, for example)
    // Example of a unit box [-1,1] for simplicity.
    vec3 tMin = (vec3(-1.0) - origin) / direction;
    vec3 tMax = (vec3(1.0) - origin) / direction;

    vec3 t1 = min(tMin, tMax);
    vec3 t2 = max(tMin, tMax);

    tEnter = max(max(t1.x, t1.y), t1.z);
    tExit = min(min(t2.x, t2.y), t2.z);

    return tEnter < tExit && tExit > 0.0;
}

void main() {
    float tEnter, tExit;
    if (intersectVolume(v_rayOrigin, v_rayDirection, tEnter, tExit)) {
        // Calculate the optical thickness
        float pathLength = tExit - tEnter;
        float opticalThickness = pathLength * u_absorptionCoefficient;

        // Compute the transmittance using Beer-Lambert law
        float transmittance = exp(-opticalThickness);

        // Calculate the final color by scaling the background color
        vec3 radiance = u_backgroundColor * transmittance;

        // Output the color as vec4
        FragColor = vec4(radiance, 1.0);
    } else {
        // If no intersection, set color to background
        FragColor = vec4(u_backgroundColor, 1.0);
    }
}
