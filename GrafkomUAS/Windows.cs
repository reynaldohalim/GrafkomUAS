using System;
using System.Collections.Generic;
using System.IO;
using LearnOpenTK.Common;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Drawing;
using System.Drawing.Imaging;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace GrafkomUAS
{
    class Windows : GameWindow
    {
        private Asset3d world;
        private Asset3d player;
        private Asset3d baymax;
        private Asset3d olaf;
        private Asset3d eve;

        private List<Light> lights = new List<Light>();
        private List<Asset3d> objects = new List<Asset3d>();

        private Dictionary<string, List<Material>> mtls_dict = new Dictionary<string, List<Material>>();

        private Camera _camera;
        private bool _firstMove;

        private Shader shader;
        private Shader screenShader;
        private Shader skyboxShader;

        private int cubemap;
        private int _vao_cube;
        private int _vbo_cube;
        readonly float[] skyboxVertices = {
        // positions          
        -1.0f,  1.0f, -1.0f,
        -1.0f, -1.0f, -1.0f,
         1.0f, -1.0f, -1.0f,
         1.0f, -1.0f, -1.0f,
         1.0f,  1.0f, -1.0f,
        -1.0f,  1.0f, -1.0f,

        -1.0f, -1.0f,  1.0f,
        -1.0f, -1.0f, -1.0f,
        -1.0f,  1.0f, -1.0f,
        -1.0f,  1.0f, -1.0f,
        -1.0f,  1.0f,  1.0f,
        -1.0f, -1.0f,  1.0f,

         1.0f, -1.0f, -1.0f,
         1.0f, -1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,
         1.0f,  1.0f, -1.0f,
         1.0f, -1.0f, -1.0f,

        -1.0f, -1.0f,  1.0f,
        -1.0f,  1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,
         1.0f, -1.0f,  1.0f,
        -1.0f, -1.0f,  1.0f,

        -1.0f,  1.0f, -1.0f,
         1.0f,  1.0f, -1.0f,
         1.0f,  1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,
        -1.0f,  1.0f,  1.0f,
        -1.0f,  1.0f, -1.0f,

        -1.0f, -1.0f, -1.0f,
        -1.0f, -1.0f,  1.0f,
         1.0f, -1.0f, -1.0f,
         1.0f, -1.0f, -1.0f,
        -1.0f, -1.0f,  1.0f,
         1.0f, -1.0f,  1.0f
    };

        public Windows(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        //Quad Screen
        readonly float[] quadVertices = { // vertex attributes for a quad that fills the entire screen in Normalized Device Coordinates.
        // positions   // texCoords
        -1.0f,  1.0f,  0.0f, 1.0f,
        -1.0f, -1.0f,  0.0f, 0.0f,
         1.0f, -1.0f,  1.0f, 0.0f,

        -1.0f,  1.0f,  0.0f, 1.0f,
         1.0f, -1.0f,  1.0f, 0.0f,
         1.0f,  1.0f,  1.0f, 1.0f
        };
        private int fbo;
        private int _vao;
        private int _vbo;
        private int texColorBuffer;

        protected override void OnLoad()
        {
            loadOpenGL();
            loadShaders();
            loadBuffers();
            loadMaterials();
            loadCubeMap();
            loadObjects();
            loadLights();
            loadCamera();

            base.OnLoad();
        }
        private void loadOpenGL()
        {
            GL.ClearColor(0.2f, 0.2f, 0.5f, 1.0f);
            GL.Enable(EnableCap.DepthTest);

            //Screen Quad
            GL.GenVertexArrays(1, out _vao);
            GL.GenBuffers(1, out _vbo);
            GL.BindVertexArray(_vbo);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        }

        private void loadShaders()
        {
            shader = new Shader
                    (
                    "../../../Shaders/shader.vert",
                    "../../../Shaders/lighting.frag"
                    );
            shader.Use();

            //Screen Shader
            screenShader = new Shader
                (
                "../../../Shaders/PostProcessing.vert",
                "../../../Shaders/PostProcessing.frag"
                );
            screenShader.Use();
            screenShader.SetInt("screenTexture", 0);
        }

        private void loadBuffers()
        {
            //Frame Buffers
            GL.GenFramebuffers(1, out fbo);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

            //Add Texture to Frame Buffer
            GL.GenTextures(1, out texColorBuffer);
            GL.BindTexture(TextureTarget.Texture2D, texColorBuffer);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, 800, 600, 0, PixelFormat.Rgb, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D
                , texColorBuffer, 0);

            //Render Buffer
            int rbo;
            GL.GenRenderbuffers(1, out rbo);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, 800, 600);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment,
                RenderbufferTarget.Renderbuffer, rbo);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        private void loadCubeMap()
        {
            //Create Cube Map
            string[] skyboxPath =
            {
                "../../../Assets/Skybox/sky.png",
                "../../../Assets/Skybox/sky.png",
                "../../../Assets/Skybox/sky.png",
                "../../../Assets/Skybox/sky.png",
                "../../../Assets/Skybox/sky.png",
                "../../../Assets/Skybox/sky.png",
            };
            GL.GenTextures(1, out cubemap);
            GL.BindTexture(TextureTarget.TextureCubeMap, cubemap);
            for (int i = 0; i < skyboxPath.Length; i++)
            {
                using (var image = new Bitmap(skyboxPath[i]))
                {
                    var data = image.LockBits(
                        new Rectangle(0, 0, image.Width, image.Height),
                        ImageLockMode.ReadOnly,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i,
                        0,
                        PixelInternalFormat.Rgb,
                        256,
                        256,
                        0,
                        PixelFormat.Bgra,
                        PixelType.UnsignedByte,
                        data.Scan0);
                }

                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);

            }

            skyboxShader = new Shader("../../../Shaders/skybox.vert",
                "../../../Shaders/skybox.frag");

            //Vertices
            //Inisialiasi VBO
            _vbo_cube = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo_cube);
            GL.BufferData(BufferTarget.ArrayBuffer, skyboxVertices.Length * sizeof(float),
                skyboxVertices, BufferUsageHint.StaticDraw);

            //Inisialisasi VAO
            _vao_cube = GL.GenVertexArray();
            GL.BindVertexArray(_vao_cube);
            var vertexLocation = skyboxShader.GetAttribLocation("aPosition");
            GL.EnableVertexAttribArray(vertexLocation);
            GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

            skyboxShader.Use();
            skyboxShader.SetInt("skybox", 4);
        }

        private void loadObjects()
        {
            world = LoadObjFile("../../../Assets/grafkom.obj");
            world.loadObject(0.1f, 0.1f);
            objects.Add(world);

            player = LoadObjFile("../../../Assets/baymaxkecil.obj");
            player.loadObject(1.0f, 1.0f);
            player.scale(0.2f);
            player.translate(new Vector3(17, 1, -0.1f));
            objects.Add(player);

            baymax = LoadObjFile("../../../Assets/baymax.obj");
            baymax.loadObject(0.1f, 0.1f);
            baymax.translate(new Vector3(4, 0, -0.1f)); //xzy
            objects.Add(baymax);

            eve = LoadObjFile("../../../Assets/eve.obj");
            eve.loadObject(0.1f, 0.1f);
            eve.translate(new Vector3(6, 0.7f, 10f));
            objects.Add(eve);

            olaf = LoadObjFile("../../../Assets/olaf.obj");
            olaf.loadObject(0.1f, 0.1f);
            olaf.translate(new Vector3(5, 1.5f, -15f));
            objects.Add(olaf);
        }

        private Vector3 cameraPosition()
        {
            return playerPosition() + new Vector3(1, 1, 0);
        }

        private Vector3 playerPosition()
        {
            return player.getTransform().ExtractTranslation();
        }
        
        private Vector3 objPosition(Asset3d obj)
        {
            return obj.getTransform().ExtractTranslation();
        }

        private void loadLights()
        {
            lights.Add(new PointLight(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.05f, 0.05f, 0.05f),
                 new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.0f, 1.0f, 1.0f), 0.5f, 0.5f, 0.5f));
            lights.Add(new PointLight(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.05f, 0.05f, 0.05f),
                new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.0f, 1.0f, 1.0f), 0.5f, 0.5f, 0.5f));
            lights.Add(new PointLight(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.05f, 0.05f, 0.05f),
               new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.0f, 1.0f, 1.0f), 0.5f, 0.5f, 0.5f));
            lights.Add(new DirectionLight(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.05f, 0.05f, 0.05f),
                new Vector3(1.0f, 1.0f, 1.0f), new Vector3(0.0f, 1.0f, 1.0f), new Vector3(0, 0, 0)));

            //x = maju mundur, z = kanan kiri, y = atas/bawah/terang

            lights[0].Position = playerPosition(); //baymaxKecil
            lights[1].Position = new Vector3(3.2f, 2, -15f); //olaf
            lights[2].Position = new Vector3(3.5f, 2, 1.35f); //lampu baymax
            lights[3].Position = new Vector3(0.8f, 13, 23); // bulan

            lights[0].Ambient = new Vector3(0.3f, 0.3f, 0.3f);
            lights[1].Ambient = new Vector3(0.3f, 0.3f, 0.3f);
            lights[2].Ambient = new Vector3(0.25f, 0.25f, 0.25f);
            lights[3].Ambient = new Vector3(0.15f, 0.15f, 0.15f);
        }

        private void loadCamera()
        {
            _camera = new Camera(cameraPosition(), Size.X / (float)Size.Y);
            _camera.Yaw = 135;
            _camera.Fov = 90;
            CursorGrabbed = true;
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            if (GLFW.GetTime() > 0.02)
                GLFW.SetTime(0.0);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            shader.Use();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Enable(EnableCap.DepthTest);
            GL.ActiveTexture(TextureUnit.Texture0);

            for (int i = 0; i < lights.Count; i++)
                foreach (var obj in objects)
                    obj.calculateTextureRender(_camera, lights[i], i);

            //Render Skybox
            GL.DepthFunc(DepthFunction.Lequal);
            skyboxShader.Use();
            Matrix4 skyview = _camera.GetViewMatrix().ClearTranslation().ClearScale();
            skyboxShader.SetMatrix4("view", skyview);

            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView
                (
                MathHelper.DegreesToRadians(_camera.Fov),
                Size.X / (float)Size.Y, 1f, 100f);
            skyboxShader.SetMatrix4("projection", projection);
            skyboxShader.SetInt("skybox", 4);
            GL.BindVertexArray(_vao_cube);
            GL.ActiveTexture(TextureUnit.Texture4);
            GL.BindTexture(TextureTarget.TextureCubeMap, cubemap);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
            GL.DepthFunc(DepthFunction.Less);

            SwapBuffers();

            base.OnRenderFrame(args);
        }

        Vector2 _lastPos;
        float _rotationSpeed = 0.5f;
        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            float cameraSpeed = 5f;
            Vector3 playerPos = playerPosition();
            if (KeyboardState.IsKeyDown(Keys.W))
            {
                Vector3 checkLocation = playerPos - (new Vector3(1, 0, 0) * cameraSpeed * (float)args.Time);
                if (!collisionCheck(checkLocation)){
                    player.translate(-(new Vector3(1, 0, 0) * cameraSpeed * (float)args.Time));
                    _camera.Position = cameraPosition();
                }
            }

            if (KeyboardState.IsKeyDown(Keys.A))
            {
                Vector3 checkLocation = playerPos - (new Vector3(0, 0, -1) * cameraSpeed * (float)args.Time);
                if (!collisionCheck(checkLocation))
                {
                    player.translate(-(new Vector3(0, 0, -1) * cameraSpeed * (float)args.Time));
                    _camera.Position = cameraPosition();
                }
            }

            if (KeyboardState.IsKeyDown(Keys.D))
            {
                Vector3 checkLocation = playerPos + (new Vector3(0, 0, -1) * cameraSpeed * (float)args.Time);
                if (!collisionCheck(checkLocation))
                {
                    player.translate(new Vector3(0, 0, -1) * cameraSpeed * (float)args.Time);
                    _camera.Position = cameraPosition();
                }
            }
            if (KeyboardState.IsKeyDown(Keys.S))
            {
                Vector3 checkLocation = playerPos + (new Vector3(1, 0, 0) * cameraSpeed * (float)args.Time);
                if (!collisionCheck(checkLocation))
                {
                    player.translate((new Vector3(1, 0, 0) * cameraSpeed * (float)args.Time));
                    _camera.Position = cameraPosition();
                }
            }


            if (KeyboardState.IsKeyDown(Keys.Up))
                _camera.Position += _camera.Up * cameraSpeed * (float)args.Time;

            if (KeyboardState.IsKeyDown(Keys.Down))
                _camera.Position -= _camera.Up * cameraSpeed * (float)args.Time;

            if (KeyboardState.IsKeyDown(Keys.Left))
                _camera.Yaw -= cameraSpeed * (float)args.Time * 60;

            if (KeyboardState.IsKeyDown(Keys.Right))
                _camera.Yaw += cameraSpeed * (float)args.Time * 60;

            if (KeyboardState.IsKeyDown(Keys.Space))
            {
                Console.WriteLine(_camera.Position);
                Console.WriteLine("pitch : " + _camera.Pitch);
                Console.WriteLine("yaw : " + _camera.Yaw);
                Console.WriteLine("fov : " + _camera.Fov);
            }


            var mouse = MouseState;
            var senstivity = 0.1f;

            if (_firstMove)
            {
                _lastPos = new Vector2(mouse.X, mouse.Y);
                _firstMove = false;
            }
            else
            {
                var deltaX = mouse.X - _lastPos.X;
                var deltaY = mouse.Y - _lastPos.Y;
                _lastPos = new Vector2(mouse.X, mouse.Y);
                _camera.Yaw += deltaX * senstivity;
                _camera.Pitch -= deltaY * senstivity;
            }

            if (KeyboardState.IsKeyDown(Keys.M))
            {
                var axis = new Vector3(0, 1, 0);
                _camera.Position -= playerPosition();
                _camera.Yaw -= _rotationSpeed;
                _camera.Position = Vector3.Transform(_camera.Position, generateArbRotationMatrix(axis, playerPosition(), -_rotationSpeed).ExtractRotation());
                _camera.Position += playerPosition();
                _camera._front = -Vector3.Normalize(_camera.Position - playerPosition());
            }

            lights[0].Position = playerPosition();

            base.OnUpdateFrame(args);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, Size.X, Size.Y);
            _camera.AspectRatio = Size.X / (float)Size.Y;
        }

        private void loadMaterials()
        {
            List<Material> materials = new List<Material>();
            Texture diffuseMap = Texture.LoadFromFile("../../../Assets/default.jpg");
            Texture textureMap = Texture.LoadFromFile("../../../Assets/default.jpg");
            materials.Add(new Material("Default", 0, new Vector3(0.1f), new Vector3(1f), new Vector3(1f),
                    1.0f, diffuseMap, textureMap));

            mtls_dict.Add("Default", materials);
        }

        public Asset3d LoadObjFile(string path, bool usemtl = true)
        {
            Asset3d mesh = new Asset3d("../../../Shaders/shader.vert",
                "../../../Shaders/lighting.frag");
            List<Vector3> temp_vertices = new List<Vector3>();
            List<Vector3> temp_normals = new List<Vector3>();
            List<Vector3> temp_textureVertices = new List<Vector3>();
            List<uint> temp_vertexIndices = new List<uint>();
            List<uint> temp_normalsIndices = new List<uint>();
            List<uint> temp_textureIndices = new List<uint>();
            List<string> temp_name = new List<string>();
            List<String> temp_materialsName = new List<string>();
            string current_materialsName = "";
            string material_library = "";
            int mesh_count = 0;
            int mesh_created = 0;

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Unable to open \"" + path + "\", does not exist.");
            }

            using (StreamReader streamReader = new StreamReader(path))
            {
                while (!streamReader.EndOfStream)
                {
                    List<string> words = new List<string>(streamReader.ReadLine().Split(' '));
                    words.RemoveAll(s => s == string.Empty);

                    if (words.Count == 0)
                        continue;
                    string type = words[0];

                    words.RemoveAt(0);

                    switch (type)
                    {
                        //Render tergantung nama dan objek apa sehingga bisa buat hirarki
                        case "o":
                            if (mesh_count > 0)
                            {
                                Asset3d mesh_tmp = new Asset3d();
                                //Attach Shader
                                mesh_tmp.setShader(shader);
                                mesh_tmp.setDepthShader(skyboxShader);
                                for (int i = 0; i < temp_vertexIndices.Count; i++)
                                {
                                    uint vertexIndex = temp_vertexIndices[i];
                                    mesh_tmp.AddVertices(temp_vertices[(int)vertexIndex - 1]);
                                }
                                for (int i = 0; i < temp_textureIndices.Count; i++)
                                {
                                    uint textureIndex = temp_textureIndices[i];
                                    mesh_tmp.AddTextureVertices(temp_textureVertices[(int)textureIndex - 1]);
                                }
                                for (int i = 0; i < temp_normalsIndices.Count; i++)
                                {
                                    uint normalIndex = temp_normalsIndices[i];
                                    mesh_tmp.AddNormals(temp_normals[(int)normalIndex - 1]);
                                }
                                mesh_tmp.setName(temp_name[mesh_created]);

                                //Material
                                if (usemtl)
                                {

                                    List<Material> mtl = mtls_dict[material_library];
                                    for (int i = 0; i < mtl.Count; i++)
                                    {
                                        if (mtl[i].Name == current_materialsName)
                                        {
                                            mesh_tmp.setMaterial(mtl[i]);
                                        }
                                    }
                                }
                                else
                                {
                                    List<Material> mtl = mtls_dict["Default"];
                                    for (int i = 0; i < mtl.Count; i++)
                                    {
                                        if (mtl[i].Name == "Default")
                                        {
                                            mesh_tmp.setMaterial(mtl[i]);
                                        }
                                    }
                                }


                                if (mesh_count == 1)
                                {
                                    mesh = mesh_tmp;
                                }
                                else
                                {
                                    mesh.child.Add(mesh_tmp);
                                }

                                mesh_created++;
                            }
                            temp_name.Add(words[0]);
                            mesh_count++;
                            break;
                        case "v":
                            temp_vertices.Add(new Vector3(float.Parse(words[0]) / 10, float.Parse(words[1]) / 10, float.Parse(words[2]) / 10));
                            break;

                        case "vt":
                            temp_textureVertices.Add(new Vector3(float.Parse(words[0]), float.Parse(words[1]),
                                                            words.Count < 3 ? 0 : float.Parse(words[2])));
                            break;

                        case "vn":
                            temp_normals.Add(new Vector3(float.Parse(words[0]), float.Parse(words[1]), float.Parse(words[2])));
                            break;
                        case "mtllib":
                            if (usemtl)
                            {
                                string resourceName = "../../../Assets/" + words[0];
                                string nameWOExt = words[0].Split(".")[0];
                                mtls_dict.Add(nameWOExt, LoadMtlFile(resourceName));
                                material_library = nameWOExt;
                            }

                            break;
                        case "usemtl":
                            if (usemtl)
                            {
                                current_materialsName = words[0];
                            }

                            break;
                        // face
                        case "f":
                            foreach (string w in words)
                            {
                                if (w.Length == 0)
                                    continue;

                                string[] comps = w.Split('/');
                                for (int i = 0; i < comps.Length; i++)
                                {
                                    if (i == 0)
                                    {
                                        if (comps[0].Length > 0)
                                        {
                                            temp_vertexIndices.Add(uint.Parse(comps[0]));
                                        }

                                    }
                                    else if (i == 1)
                                    {
                                        if (comps[1].Length > 0)
                                        {
                                            temp_textureIndices.Add(uint.Parse(comps[1]));
                                        }

                                    }
                                    else if (i == 2)
                                    {
                                        if (comps[2].Length > 0)
                                        {
                                            temp_normalsIndices.Add(uint.Parse(comps[2]));
                                        }

                                    }
                                }

                            }
                            break;

                        default:
                            break;
                    }
                }
            }
            if (mesh_created < mesh_count)
            {

                Asset3d mesh_tmp = new Asset3d();
                //Attach Shader
                mesh_tmp.setShader(shader);
                mesh_tmp.setDepthShader(skyboxShader);
                for (int i = 0; i < temp_vertexIndices.Count; i++)
                {
                    uint vertexIndex = temp_vertexIndices[i];
                    mesh_tmp.AddVertices(temp_vertices[(int)vertexIndex - 1]);
                }
                for (int i = 0; i < temp_textureIndices.Count; i++)
                {
                    uint textureIndex = temp_textureIndices[i];
                    mesh_tmp.AddTextureVertices(temp_textureVertices[(int)textureIndex - 1]);
                }
                for (int i = 0; i < temp_normalsIndices.Count; i++)
                {
                    uint normalIndex = temp_normalsIndices[i];
                    mesh_tmp.AddNormals(temp_normals[(int)normalIndex - 1]);
                }
                mesh_tmp.setName(temp_name[mesh_created]);

                //Material
                if (usemtl)
                {

                    List<Material> mtl = mtls_dict[material_library];
                    for (int i = 0; i < mtl.Count; i++)
                    {
                        if (mtl[i].Name == current_materialsName)
                        {
                            mesh_tmp.setMaterial(mtl[i]);
                        }
                    }
                }
                else
                {
                    List<Material> mtl = mtls_dict["Default"];
                    for (int i = 0; i < mtl.Count; i++)
                    {
                        if (mtl[i].Name == "Default")
                        {
                            mesh_tmp.setMaterial(mtl[i]);
                        }
                    }
                }


                if (mesh_count == 1)
                {
                    mesh = mesh_tmp;
                }
                else
                {
                    mesh.child.Add(mesh_tmp);
                }

                mesh_created++;
            }
            return mesh;
        }

        public List<Material> LoadMtlFile(string path)
        {
            List<Material> materials = new List<Material>();
            List<string> name = new List<string>();
            List<float> shininess = new List<float>();
            List<Vector3> ambient = new List<Vector3>();
            List<Vector3> diffuse = new List<Vector3>();
            List<Vector3> specular = new List<Vector3>();
            List<float> alpha = new List<float>();
            List<string> map_kd = new List<string>();
            List<string> map_ka = new List<string>();

            //komputer ngecek, apakah file bisa diopen atau tidak
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Unable to open \"" + path + "\", does not exist.");
            }
            //lanjut ke sini
            using (StreamReader streamReader = new StreamReader(path))
            {
                while (!streamReader.EndOfStream)
                {
                    List<string> words = new List<string>(streamReader.ReadLine().Split(' '));
                    words.RemoveAll(s => s == string.Empty);

                    if (words.Count == 0)
                        continue;

                    string type = words[0];

                    words.RemoveAt(0);
                    switch (type)
                    {
                        case "newmtl":
                            if (map_kd.Count < name.Count)
                            {
                                map_kd.Add("default.jpg");
                            }
                            if (map_ka.Count < name.Count)
                            {
                                map_ka.Add("default.jpg");
                            }
                            name.Add(words[0]);
                            break;
                        //Shininess
                        case "Ns":
                            shininess.Add(float.Parse(words[0]));
                            break;
                        case "Ka":
                            ambient.Add(new Vector3(float.Parse(words[0]), float.Parse(words[1]), float.Parse(words[2])));
                            break;
                        case "Kd":
                            diffuse.Add(new Vector3(float.Parse(words[0]), float.Parse(words[1]), float.Parse(words[2])));
                            break;
                        case "Ks":
                            specular.Add(new Vector3(float.Parse(words[0]), float.Parse(words[1]), float.Parse(words[2])));
                            break;
                        case "d":
                            alpha.Add(float.Parse(words[0]));
                            break;
                        case "map_Kd":
                            map_kd.Add(words[0]);
                            break;
                        case "map_Ka":
                            map_ka.Add(words[0]);
                            break;
                        default:
                            break;
                    }
                }
            }

            if (map_kd.Count < name.Count)
            {
                map_kd.Add("default.jpg");
            }
            if (map_ka.Count < name.Count)
            {
                map_ka.Add("default.jpg");
            }

            Dictionary<string, Texture> texture_map_Kd = new Dictionary<string, Texture>();
            for (int i = 0; i < map_kd.Count; i++)
            {
                if (!texture_map_Kd.ContainsKey(map_kd[i]))
                {
                    texture_map_Kd.Add(map_kd[i],
                        Texture.LoadFromFile("../../../Assets/" + map_kd[i]));
                }
            }

            Dictionary<string, Texture> texture_map_Ka = new Dictionary<string, Texture>();
            for (int i = 0; i < map_ka.Count; i++)
            {
                if (!texture_map_Ka.ContainsKey(map_ka[i]))
                {
                    texture_map_Ka.Add(map_ka[i],
                        Texture.LoadFromFile("../../../Assets/" + map_ka[i]));
                }
            }

            for (int i = 0; i < name.Count; i++)
            {
                materials.Add(new Material(name[i], shininess[i], ambient[i], diffuse[i], specular[i],
                    alpha[i], texture_map_Kd[map_kd[i]], texture_map_Ka[map_ka[i]]));
            }

            return materials;
        }


        private Matrix4 generateArbRotationMatrix(Vector3 axis, Vector3 center, float degree)
        {
            var rads = MathHelper.DegreesToRadians(degree);

            var secretFormula = new float[4, 4] {
                { (float)Math.Cos(rads) + (float)Math.Pow(axis.X, 2) * (1 - (float)Math.Cos(rads)), axis.X* axis.Y * (1 - (float)Math.Cos(rads)) - axis.Z * (float)Math.Sin(rads),    axis.X * axis.Z * (1 - (float)Math.Cos(rads)) + axis.Y * (float)Math.Sin(rads),   0 },
                { axis.Y * axis.X * (1 - (float)Math.Cos(rads)) + axis.Z * (float)Math.Sin(rads),   (float)Math.Cos(rads) + (float)Math.Pow(axis.Y, 2) * (1 - (float)Math.Cos(rads)), axis.Y * axis.Z * (1 - (float)Math.Cos(rads)) - axis.X * (float)Math.Sin(rads),   0 },
                { axis.Z * axis.X * (1 - (float)Math.Cos(rads)) - axis.Y * (float)Math.Sin(rads),   axis.Z * axis.Y * (1 - (float)Math.Cos(rads)) + axis.X * (float)Math.Sin(rads),   (float)Math.Cos(rads) + (float)Math.Pow(axis.Z, 2) * (1 - (float)Math.Cos(rads)), 0 },
                { 0, 0, 0, 1}
            };
            var secretFormulaMatrix = new Matrix4(
                new Vector4(secretFormula[0, 0], secretFormula[0, 1], secretFormula[0, 2], secretFormula[0, 3]),
                new Vector4(secretFormula[1, 0], secretFormula[1, 1], secretFormula[1, 2], secretFormula[1, 3]),
                new Vector4(secretFormula[2, 0], secretFormula[2, 1], secretFormula[2, 2], secretFormula[2, 3]),
                new Vector4(secretFormula[3, 0], secretFormula[3, 1], secretFormula[3, 2], secretFormula[3, 3])
            );

            return secretFormulaMatrix;
        }

        private bool collisionCheck(Vector3 player)
        {
            bool collision = false;
            float rangeLimit;
            Vector3 pos;

            //baymax
            rangeLimit = 1.5f;
            pos = objPosition(baymax) + new Vector3(2.2f, 0, 0);
            if (Math.Abs(pos.X - player.X) < rangeLimit && Math.Abs(pos.Z - player.Z) < rangeLimit)
                collision = true;

            //olaf
            rangeLimit = 1;
            pos = objPosition(olaf) + new Vector3(-2, 0, -1.5f);
            if (Math.Abs(pos.X - player.X) < rangeLimit && Math.Abs(pos.Z - player.Z) < rangeLimit)
                collision = true;

            //eve
            rangeLimit = 1.5f;
            pos = objPosition(eve) + new Vector3(2.2f, 0, 0);
            if (Math.Abs(pos.X - player.X) < rangeLimit && Math.Abs(pos.Z - player.Z) < rangeLimit)
                collision = true;

            return collision;
        }
    }
}