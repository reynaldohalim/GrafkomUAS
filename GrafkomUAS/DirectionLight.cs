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
    class DirectionLight : Light
    {
        Vector3 direction;
        public DirectionLight(Vector3 position, Vector3 ambient, Vector3 diffuse, Vector3 specular, Vector3 direction)
            : base(position, ambient, diffuse, specular)
        {
            this.direction = direction;
        }

        public Vector3 Direction { get => direction; set => direction = value; }
    }
}

