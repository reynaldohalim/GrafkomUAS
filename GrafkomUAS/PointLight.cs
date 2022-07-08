using OpenTK.Mathematics;

namespace GrafkomUAS
{
    class PointLight : Light
    {
        float constant;
        float linear;
        float quadratic;

        public PointLight(Vector3 position, Vector3 ambient, Vector3 diffuse, Vector3 specular,
            float constant, float linear, float quadratic) 
            : base(position, ambient, diffuse, specular)
        {
            this.constant = constant;
            this.linear = linear;
            this.quadratic = quadratic;
        }

        public float Constant { get => constant; set => constant = value; }
        public float Linear { get => linear; set => linear = value; }
        public float Quadratic { get => quadratic; set => quadratic = value; }
    }
}
