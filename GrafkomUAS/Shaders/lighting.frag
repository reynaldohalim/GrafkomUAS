#version 330 core
out vec4 FragColor;

struct Material {
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
    sampler2D diffuse_sampler;
    sampler2D specular_sampler;
    float shininess; //Shininess is the power the specular light is raised to
};
struct Light {
    vec3 position;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;

    bool usePoint;
    float constant;
    float linear;
    float quadratic;
 
    bool useDirection;
    vec3 direction;
};
//We create the light and the material struct as uniforms.
#define NR_POINT_LIGHTS 4
uniform Light lights[NR_POINT_LIGHTS];
uniform Material material;

uniform vec3 viewPos;
uniform bool blinn;
uniform bool gamma;

in vec3 Normal;
in vec3 FragPos;
in vec2 TexCoords;

//Using Blinn-Phong Model Shader
vec3 CalcPointLight(Light light, vec3 normal, vec3 fragPos, vec3 viewDir, Material material)
{
    //Transition from Point and Directional Light
    vec3 lightDir = normalize(light.position - fragPos);
    if(light.useDirection)
    {
         lightDir = normalize(-light.direction);
    }

    vec3 halfwayDir = normalize(lightDir + viewDir);

    // diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);
    // specular shading
    vec3 reflectDir = reflect(-lightDir, normal);

    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess); //Phong Model
    
    if (blinn)
    {
        spec = pow(max(dot(normal, halfwayDir), 0.0), material.shininess); //Blinn-Phong Model
    }
    
    
    // attenuation
    float distance    = length(light.position - fragPos);
    float attenuation = 0.0f;
    if(gamma)
    {
        attenuation = 1.0 / (light.constant + light.linear * distance + 
  			     light.quadratic * (distance * distance));
    }
    else
    {
        attenuation = 1.0 / (light.constant + light.linear * distance);   
    }
    
    // combine results
    vec3 ambient  = light.ambient  * vec3(texture(material.diffuse_sampler, TexCoords).rgb) * material.ambient;
    vec3 diffuse  = light.diffuse  * diff * vec3(texture(material.diffuse_sampler, TexCoords).rgb) * material.diffuse;
    vec3 specular = light.specular * spec * vec3(texture(material.specular_sampler, TexCoords).rgb) * material.specular;
    if(!light.useDirection)
    {
        ambient  *= attenuation;
        diffuse  *= attenuation;
        specular *= attenuation;
    }
    
    return (ambient + diffuse + specular);
}

void main()
{
    vec3 norm = normalize(Normal);
    vec3 viewDir = normalize(viewPos - FragPos);
    
    vec3 result = vec3(0.0, 0.0, 0.0);
    for(int i = 0; i < 4; i++)
    {
        result += CalcPointLight(lights[i], norm, FragPos, viewDir, material);
    }

    if(gamma)
    {
        result = pow(result, vec3(1.0/2.2));
    }
    FragColor = vec4(result, 1.0);
}

