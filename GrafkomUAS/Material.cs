using LearnOpenTK.Common;
using OpenTK.Mathematics;
using System;


namespace GrafkomUAS
{
    class Material
    {
        string name;
        float shininess;
        Vector3 ambient;
        Vector3 diffuse;
        Vector3 specular;
        float alpha;

        Texture map_Kd;
        Texture map_Ka;

        public Material(string name, float shininess, Vector3 ambient, Vector3 diffuse, Vector3 specular,
            float alpha, Texture map_Kd, Texture map_Ka)
        {
            this.name = name;
            this.shininess = shininess;
            this.ambient = ambient;
            this.diffuse = diffuse;
            this.specular = specular;
            this.alpha = alpha;
            this.map_Kd = map_Kd;
            this.map_Ka = map_Ka;
        }

        public string Name { get => name; set => name = value; }
        public float Shininess { get => shininess; set => shininess = value; }
        public Vector3 Ambient { get => ambient; set => ambient = value; }
        public Vector3 Diffuse { get => diffuse; set => diffuse = value; }
        public Vector3 Specular { get => specular; set => specular = value; }
        public Texture Map_Kd { get => map_Kd; set => map_Kd = value; }
        public Texture Map_Ka { get => map_Ka; set => map_Ka = value; }
    }
}
