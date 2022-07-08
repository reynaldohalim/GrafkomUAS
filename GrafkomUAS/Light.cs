using LearnOpenTK.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenTK.Graphics.OpenGL4;

namespace GrafkomUAS
{
    class Light
    {
        Vector3 position;
        Vector3 ambient;
        Vector3 diffuse;
        Vector3 specular;

        public Light(Vector3 position, Vector3 ambient, Vector3 diffuse, Vector3 specular)
        {
            this.position = position;
            this.ambient = ambient;
            this.diffuse = diffuse;
            this.specular = specular;
        }

        public Vector3 Position { get => position; set => position = value; }
        public Vector3 Ambient { get => ambient; set => ambient = value; }
        public Vector3 Diffuse { get => diffuse; set => diffuse = value; }
        public Vector3 Specular { get => specular; set => specular = value; }
    }
}
