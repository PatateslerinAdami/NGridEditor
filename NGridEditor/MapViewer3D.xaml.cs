using LoLNGRIDConverter;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace NGridMashEditor
{
    public partial class MapViewer3D : Window
    {
        private NGrid _grid;
        private GeometryModel3D _model;
        private Point _lastMousePos;

        private Point3D _camPos;
        private double _yaw = 0;
        private double _pitch = -45;

        private HashSet<Key> _keysDown = new HashSet<Key>();
        private System.Diagnostics.Stopwatch _timer = new System.Diagnostics.Stopwatch();
        private DispatcherTimer _uiTimer;

        private string _textureFolderPath = "";
        private Model3DGroup _wgeoGroup;
        private ModelVisual3D _wgeoVisual;

        public MapViewer3D(NGrid grid)
        {
            InitializeComponent();
            _grid = grid;

            GenerateMesh();
            UpdateTexture();

            double midX = (grid.MaxBounds.x + grid.MinBounds.x) / 2;
            double midZ = (grid.MaxBounds.z + grid.MinBounds.z) / 2;

            _camPos = new Point3D(midX, 3000, -midZ + 2000);
            _yaw = 0;
            UpdateCamera();

            this.MouseRightButtonDown += (s, e) => { _lastMousePos = e.GetPosition(this); this.Cursor = Cursors.None; this.Focus(); };
            this.MouseRightButtonUp += (s, e) => this.Cursor = Cursors.Arrow;
            this.MouseLeftButtonDown += (s, e) => { _lastMousePos = e.GetPosition(this); this.Cursor = Cursors.Hand; this.Focus(); };
            this.MouseLeftButtonUp += (s, e) => this.Cursor = Cursors.Arrow;
            this.MouseMove += OnMouseMove;

            this.KeyDown += (s, e) => { if (!_keysDown.Contains(e.Key)) _keysDown.Add(e.Key); };
            this.KeyUp += (s, e) => { if (_keysDown.Contains(e.Key)) _keysDown.Remove(e.Key); };

            _timer.Start();
            CompositionTarget.Rendering += OnGameLoop;

            _uiTimer = new DispatcherTimer();
            _uiTimer.Interval = TimeSpan.FromMilliseconds(100);
            _uiTimer.Tick += (s, e) => UpdateCursorInfo();
            _uiTimer.Start();
        }

        private void OnGameLoop(object sender, EventArgs e)
        {
            double deltaTime = _timer.Elapsed.TotalSeconds;
            _timer.Restart();

            double speed = 2000.0 * deltaTime;
            if (_keysDown.Contains(Key.LeftShift)) speed *= 4.0;

            double radYaw = _yaw * Math.PI / 180.0;
            double radPitch = _pitch * Math.PI / 180.0;

            double fx = Math.Sin(radYaw) * Math.Cos(radPitch);
            double fy = Math.Sin(radPitch);
            double fz = -Math.Cos(radYaw) * Math.Cos(radPitch);
            Vector3D forward = new Vector3D(fx, fy, fz);

            Vector3D right = new Vector3D(Math.Cos(radYaw), 0, Math.Sin(radYaw));
            Vector3D up = new Vector3D(0, 1, 0);

            if (_keysDown.Contains(Key.W)) _camPos += forward * speed;
            if (_keysDown.Contains(Key.S)) _camPos -= forward * speed;
            if (_keysDown.Contains(Key.A)) _camPos -= right * speed;
            if (_keysDown.Contains(Key.D)) _camPos += right * speed;
            if (_keysDown.Contains(Key.Q)) _camPos -= up * speed;
            if (_keysDown.Contains(Key.E)) _camPos += up * speed;

            UpdateCamera();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            Point currentPos = e.GetPosition(this);
            double dx = currentPos.X - _lastMousePos.X;
            double dy = currentPos.Y - _lastMousePos.Y;

            if (e.RightButton == MouseButtonState.Pressed)
            {
                _yaw += dx * 0.2;
                _pitch -= dy * 0.2;

                if (_pitch > 89) _pitch = 89;
                if (_pitch < -89) _pitch = -89;
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                double radYaw = _yaw * Math.PI / 180.0;
                Vector3D right = new Vector3D(Math.Cos(radYaw), 0, Math.Sin(radYaw));
                Vector3D forwardFlat = new Vector3D(Math.Sin(radYaw), 0, -Math.Cos(radYaw));

                double panSpeed = (_camPos.Y > 500 ? _camPos.Y / 500.0 : 1.0) * 2.0;

                _camPos -= right * dx * panSpeed;
                _camPos += forwardFlat * dy * panSpeed;
            }

            _lastMousePos = currentPos;
            UpdateCamera();
        }

        private void UpdateCamera()
        {
            double radYaw = _yaw * Math.PI / 180.0;
            double radPitch = _pitch * Math.PI / 180.0;

            double x = Math.Sin(radYaw) * Math.Cos(radPitch);
            double y = Math.Sin(radPitch);
            double z = -Math.Cos(radYaw) * Math.Cos(radPitch);

            Cam.Position = _camPos;
            Cam.LookDirection = new Vector3D(x, y, z);
            Cam.UpDirection = new Vector3D(0, 1, 0);
        }

        private void UpdateCursorInfo()
        {
            Point center = new Point(MainViewport.ActualWidth / 2, MainViewport.ActualHeight / 2);
            PointHitTestParameters pointparams = new PointHitTestParameters(center);
            VisualTreeHelper.HitTest(MainViewport, null, HitTestResult, pointparams);
        }

        private HitTestResultBehavior HitTestResult(HitTestResult result)
        {
            RayMeshGeometry3DHitTestResult meshResult = result as RayMeshGeometry3DHitTestResult;
            if (meshResult != null && meshResult.ModelHit == _model)
            {
                Point3D hitPoint = meshResult.PointHit;
                double mapWidth = _grid.MaxBounds.x - _grid.MinBounds.x;
                double mapDepth = _grid.MaxBounds.z - _grid.MinBounds.z;
                double cellStepX = mapWidth / _grid.CellCountX;
                double cellStepZ = mapDepth / _grid.CellCountZ;

                double gameZ = -hitPoint.Z;

                double rawX = (hitPoint.X - _grid.MinBounds.x) / cellStepX;
                double rawZ = (gameZ - _grid.MinBounds.z) / cellStepZ;

                int cellX = (int)rawX;
                int cellZ = (int)rawZ;

                if (cellX < 0) cellX = 0;
                if (cellZ < 0) cellZ = 0;
                if (cellX >= _grid.CellCountX) cellX = _grid.CellCountX - 1;
                if (cellZ >= _grid.CellCountZ) cellZ = _grid.CellCountZ - 1;

                NavGridCell cell = _grid.GetCell(cellX, cellZ);

                if (cell != null)
                {
                    TxtCursorInfo.Text = $"[CROSSHAIR TARGET]\n" +
                                         $"Game X: {hitPoint.X:F1}\n" +
                                         $"Game Y: {hitPoint.Y:F1}\n" +
                                         $"Game Z: {gameZ:F1}\n" +
                                         $"Grid:   [{cellX}, {cellZ}]\n" +
                                         $"Flag:   {cell.visionPathingFlags}";
                }
                return HitTestResultBehavior.Stop;
            }
            TxtCursorInfo.Text = "";
            return HitTestResultBehavior.Continue;
        }

        private void UpdateVisuals(object sender, RoutedEventArgs e) { UpdateTexture(); this.Focus(); }

        private void GenerateMesh()
        {
            MeshGeometry3D mesh = new MeshGeometry3D();
            int w = _grid.HeightSampleCountX;
            int h = _grid.HeightSampleCountZ;
            float stepX = (_grid.MaxBounds.x - _grid.MinBounds.x) / (w - 1);
            float stepZ = (_grid.MaxBounds.z - _grid.MinBounds.z) / (h - 1);

            for (int z = 0; z < h; z++)
            {
                for (int x = 0; x < w; x++)
                {
                    float heightVal = _grid.HeightSamples[(z * w) + x];
                    double vx = _grid.MinBounds.x + (x * stepX);
                    double vz = -1 * (_grid.MinBounds.z + (z * stepZ));
                    mesh.Positions.Add(new Point3D(vx, heightVal, vz));
                    mesh.TextureCoordinates.Add(new Point(x / (double)(w - 1), 1.0 - (z / (double)(h - 1))));
                }
            }
            for (int z = 0; z < h - 1; z++)
            {
                for (int x = 0; x < w - 1; x++)
                {
                    int tl = (z * w) + x; int tr = tl + 1; int bl = ((z + 1) * w) + x; int br = bl + 1;
                    mesh.TriangleIndices.Add(tl); mesh.TriangleIndices.Add(bl); mesh.TriangleIndices.Add(tr);
                    mesh.TriangleIndices.Add(tr); mesh.TriangleIndices.Add(bl); mesh.TriangleIndices.Add(br);
                }
            }
            _model = new GeometryModel3D(mesh, new DiffuseMaterial(Brushes.White));
            _model.BackMaterial = _model.Material;
            TerrainVisual.Content = _model;
            TxtStats.Text = $"Vertices: {mesh.Positions.Count:N0}";
        }

        private void UpdateTexture()
        {
            bool showGrid = ChkShowGrid.IsChecked == true;
            bool useHeatmap = ChkHeatmap.IsChecked == true;
            int w = _grid.CellCountX; int h = _grid.CellCountZ;
            int scale = showGrid ? 4 : 1;
            int texW = w * scale; int texH = h * scale;
            WriteableBitmap wbmp = new WriteableBitmap(texW, texH, 96, 96, PixelFormats.Bgra32, null);
            int[] pixels = new int[texW * texH];
            float minH = float.MaxValue, maxH = float.MinValue;
            if (useHeatmap) { foreach (var val in _grid.HeightSamples) { if (val < minH) minH = val; if (val > maxH) maxH = val; } }
            float range = maxH - minH; if (range == 0) range = 1;

            unsafe
            {
                for (int z = 0; z < h; z++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        Color c;
                        if (useHeatmap)
                        {
                            float height = _grid.HeightSamples[(z * _grid.HeightSampleCountX) + x];
                            float ratio = (height - minH) / range;
                            byte r = (byte)(ratio * 255); byte b = (byte)((1 - ratio) * 255);
                            c = Color.FromRgb(r, (byte)(ratio * 200), b);
                        }
                        else
                        {
                            var cell = _grid.Cells[(z * w) + x];
                            c = GetColorForCell(cell);
                        }
                        int colorInt = (c.A << 24) | (c.R << 16) | (c.G << 8) | c.B;
                        int gridColor = (255 << 24) | (50 << 16) | (50 << 8) | 50;
                        int visualY = h - 1 - z;
                        for (int dy = 0; dy < scale; dy++)
                        {
                            for (int dx = 0; dx < scale; dx++)
                            {
                                int px = (x * scale) + dx; int py = (visualY * scale) + dy;
                                bool isEdge = showGrid && (dx == 0 || dy == 0);
                                pixels[(py * texW) + px] = isEdge ? gridColor : colorInt;
                            }
                        }
                    }
                }
            }
            wbmp.WritePixels(new Int32Rect(0, 0, texW, texH), pixels, texW * 4, 0);
            ImageBrush brush = new ImageBrush(wbmp);
            brush.ViewportUnits = BrushMappingMode.Absolute; brush.TileMode = TileMode.None;
            RenderOptions.SetBitmapScalingMode(brush, BitmapScalingMode.NearestNeighbor);
            _model.Material = new DiffuseMaterial(brush); _model.BackMaterial = _model.Material;
        }

        private Color GetColorForCell(NavGridCell cell)
        {
            if ((cell.visionPathingFlags & VisionPathingFlags.Wall) != 0) return NGridPalette.Wall;
            if ((cell.visionPathingFlags & VisionPathingFlags.StructureWall) != 0) return NGridPalette.Structure;
            if ((cell.visionPathingFlags & VisionPathingFlags.Brush) != 0) return NGridPalette.Brush;
            if ((cell.riverRegionFlags & RiverRegionFlags.River) != 0) return NGridPalette.River;
            return NGridPalette.Walkable;
        }

        private void BtnSelectTexFolder_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Select ANY file inside the Textures folder";
            ofd.Filter = "Texture Files|*.dds;*.png;*.jpg|All Files|*.*";
            ofd.CheckFileExists = true;

            if (ofd.ShowDialog() == true)
            {
                _textureFolderPath = System.IO.Path.GetDirectoryName(ofd.FileName);
                MessageBox.Show($"Texture folder set to:\n{_textureFolderPath}");
            }
        }

        private void BtnLoadWgeo_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "World Geometry|*.wgeo" };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    var models = WGeoReader.Load(ofd.FileName);
                    _wgeoGroup = new Model3DGroup();

                    foreach (var modelData in models)
                    {
                        GeometryModel3D geom = new GeometryModel3D();
                        geom.Geometry = modelData.Mesh;
                        Material mat = FindAndLoadTexture(modelData.TextureName);
                        geom.Material = mat;
                        geom.BackMaterial = mat;
                        _wgeoGroup.Children.Add(geom);
                    }

                    if (_wgeoVisual == null)
                    {
                        _wgeoVisual = new ModelVisual3D();
                        MainViewport.Children.Add(_wgeoVisual);
                    }
                    _wgeoVisual.Content = _wgeoGroup;

                    if (ChkShowWgeo != null) ChkShowWgeo.IsChecked = true;

                    MessageBox.Show($"Loaded {models.Count} mesh parts.");
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
        }

        private Material FindAndLoadTexture(string textureName)
        {
            var defaultMat = new DiffuseMaterial(Brushes.Gray);

            if (string.IsNullOrEmpty(_textureFolderPath)) return defaultMat;
            if (string.IsNullOrEmpty(textureName)) return defaultMat;

            string fileName = System.IO.Path.GetFileName(textureName);
            string[] files = Directory.GetFiles(_textureFolderPath, fileName, SearchOption.AllDirectories);

            if (files.Length > 0)
            {
                try
                {
                    var imageSource = DdsTextureLoader.LoadDDS(files[0]);
                    if (imageSource != null)
                    {
                        var brush = new ImageBrush(imageSource);
                        brush.ViewportUnits = BrushMappingMode.Absolute;
                        brush.TileMode = TileMode.Tile;
                        return new DiffuseMaterial(brush);
                    }
                }
                catch { return new DiffuseMaterial(Brushes.Red); }
            }

            return defaultMat;
        }

        private void OnToggleNGrid(object sender, RoutedEventArgs e)
        {
            if (TerrainVisual == null) return;
            if (ChkShowNGrid.IsChecked == true) TerrainVisual.Content = _model;
            else TerrainVisual.Content = null;
        }

        private void OnToggleWgeo(object sender, RoutedEventArgs e)
        {
            if (_wgeoVisual == null) return;
            if (ChkShowWgeo.IsChecked == true) _wgeoVisual.Content = _wgeoGroup;
            else _wgeoVisual.Content = null;
        }
    }
}