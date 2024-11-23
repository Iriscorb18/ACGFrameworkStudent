#pragma once

#include <glm/vec3.hpp>
#include <glm/vec4.hpp>
#include <glm/matrix.hpp>

#include "../framework/camera.h"
#include "mesh.h"
#include "texture.h"
#include "openvdbReader.h"
#include "bbox.h"
#include "shader.h"

class Material {
public:

	Shader* shader = NULL;
	Texture* texture = NULL;
	glm::vec4 color;

	virtual void setUniforms(Camera* camera, glm::mat4 model) = 0;
	virtual void render(Mesh* mesh, glm::mat4 model, Camera* camera) = 0;
	virtual void renderInMenu() = 0;
};

class FlatMaterial : public Material {
public:

	FlatMaterial(glm::vec4 color = glm::vec4(1.f));
	~FlatMaterial();

	void setUniforms(Camera* camera, glm::mat4 model);
	void render(Mesh* mesh, glm::mat4 model, Camera* camera);
	void renderInMenu();
};

class WireframeMaterial : public FlatMaterial {
public:

	WireframeMaterial();
	~WireframeMaterial();

	void render(Mesh* mesh, glm::mat4 model, Camera* camera);
};

class StandardMaterial : public Material {
public:

	bool first_pass = false;

	bool show_normals = false;
	Shader* base_shader = NULL;
	Shader* normal_shader = NULL;

	StandardMaterial(glm::vec4 color = glm::vec4(1.f));
	~StandardMaterial();

	void setUniforms(Camera* camera, glm::mat4 model);
	void render(Mesh* mesh, glm::mat4 model, Camera* camera);
	void renderInMenu();
};

// VolumeMaterial class added for volumetric rendering
enum VolumeType {
    HOMOGENEOUS,
    HETEROGENEOUS
};

enum ShaderType {
	ABSORPTION,
	ABSORPTION_EMISSION
};

enum DensitySourceType {
	CONSTANT_DENSITY,
	NOISE_DENSITY,
	VDB_DENSITY
};

class VolumeMaterial : public Material {
public:

    VolumeMaterial(glm::vec4 color = glm::vec4(1.f));
    ~VolumeMaterial();

	VolumeType volumeType;
    ShaderType shaderType;

    // Shader-related parameters
    glm::vec3 boxMin, boxMax;

    float absorptionCoefficient;

    float stepLength;
    float noiseScale;
    int noiseDetail;

    glm::vec4 emissiveColor;
    float emissiveIntensity;
	float scatterCoefficient;
	glm::vec4 light_color;
	float gValue;

	float densityScale;
	DensitySourceType densitySource;
	int numSteps;

	void loadVDB(std::string file_path);
	void estimate3DTexture(easyVDB::OpenVDBReader* vdbReader);

    void setUniforms(Camera* camera, glm::mat4 model);
    void render(Mesh* mesh, glm::mat4 model, Camera* camera);
    void renderInMenu();
};