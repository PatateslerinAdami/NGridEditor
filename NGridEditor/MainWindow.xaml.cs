using LoLNGRIDConverter;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using NGridMashEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Globalization;

namespace LoLNGRIDConverter
{
    public partial class MainWindow : Window
    {
        private NGrid _currentGrid;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void UpdateInfoDisplay()
        {
            if (_currentGrid == null) return;

            TxtVersion.Text = $"{_currentGrid.MajorVersion}.{_currentGrid.MinorVersion}";
            TxtDimensions.Text = $"{_currentGrid.CellCountX} x {_currentGrid.CellCountZ}";
            TxtCellSize.Text = $"{_currentGrid.CellSize:F4}";
            TxtTotalCells.Text = $"{_currentGrid.Cells.Count:N0}";

            TxtMinBounds.Text = _currentGrid.MinBounds.ToString();
            TxtMaxBounds.Text = _currentGrid.MaxBounds.ToString();
        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "AI Mesh NGrid|*.aimesh_ngrid" };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    _currentGrid = NGridFileReader.Load(ofd.FileName);
                    ImgMap.Source = NGridRenderer.Render(_currentGrid);
                    UpdateInfoDisplay();
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
        }
        private void BtnExportBMP_Click(object sender, RoutedEventArgs e)
        {
            if (_currentGrid == null) return;
            SaveFileDialog sfd = new SaveFileDialog { Filter = "Bitmap Image|*.bmp", FileName = "map_export.bmp" };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    WriteableBitmap wbmp = NGridRenderer.Render(_currentGrid);
                    BmpBitmapEncoder encoder = new BmpBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(wbmp));
                    using (FileStream stream = new FileStream(sfd.FileName, FileMode.Create)) encoder.Save(stream);
                    MessageBox.Show("BMP Exported!");
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
        }
        private void BtnCreateFromBMP_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Bitmap Image|*.bmp;*.png", Title = "Select Flag Map (Colors)" };

            if (ofd.ShowDialog() == true)
            {
                GenerationSettingsWindow settings = new GenerationSettingsWindow();
                settings.Owner = this;

                if (settings.ShowDialog() == true)
                {
                    try
                    {
                        _currentGrid = NGridGenerator.FromBitmap(
                            ofd.FileName,
                            settings.CellSize,
                            settings.MinBounds,
                            settings.MaxBounds,
                            settings.SampleOffsetX,
                            settings.SampleOffsetZ,
                            settings.TemplateSampleCountX,
                            settings.TemplateSampleCountZ
                        );

                        ImgMap.Source = NGridRenderer.Render(_currentGrid);
                        UpdateInfoDisplay();
                        MessageBox.Show("New NGrid created! (Offsets applied)");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message);
                    }
                }
            }
        }
        private void BtnInjectBMP_Click(object sender, RoutedEventArgs e)
        {
            if (_currentGrid == null) { MessageBox.Show("Load a map first."); return; }
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Bitmap Image|*.bmp;*.png" };

            if (ofd.ShowDialog() == true)
            {
                try
                {
                    BitmapImage bmp = new BitmapImage(new Uri(ofd.FileName));
                    if (bmp.PixelWidth != _currentGrid.CellCountX || bmp.PixelHeight != _currentGrid.CellCountZ)
                    {
                        MessageBox.Show($"Size Mismatch!\nGrid: {_currentGrid.CellCountX}x{_currentGrid.CellCountZ}\nBMP: {bmp.PixelWidth}x{bmp.PixelHeight}");
                        return;
                    }

                    FormatConvertedBitmap converted = new FormatConvertedBitmap(bmp, System.Windows.Media.PixelFormats.Bgra32, null, 0);
                    int stride = converted.PixelWidth * 4;
                    byte[] pixels = new byte[converted.PixelHeight * stride];
                    converted.CopyPixels(pixels, stride, 0);

                    for (int i = 0; i < _currentGrid.Cells.Count; i++)
                    {
                        var cell = _currentGrid.Cells[i];

                        int visualY = _currentGrid.CellCountZ - 1 - cell.z;

                        int idx = (visualY * stride) + (cell.x * 4);

                        byte b = pixels[idx], g = pixels[idx + 1], r = pixels[idx + 2];

                        cell.visionPathingFlags &= ~(VisionPathingFlags.Wall | VisionPathingFlags.Brush | VisionPathingFlags.StructureWall);
                        cell.riverRegionFlags &= ~RiverRegionFlags.River;

                        if (NGridPalette.AreColorsEqual(NGridPalette.Wall, r, g, b)) cell.visionPathingFlags |= VisionPathingFlags.Wall;
                        else if (NGridPalette.AreColorsEqual(NGridPalette.Brush, r, g, b)) cell.visionPathingFlags |= VisionPathingFlags.Brush;
                        else if (NGridPalette.AreColorsEqual(NGridPalette.Structure, r, g, b)) cell.visionPathingFlags |= VisionPathingFlags.StructureWall;
                        else if (NGridPalette.AreColorsEqual(NGridPalette.River, r, g, b)) cell.riverRegionFlags |= RiverRegionFlags.River;
                    }

                    ImgMap.Source = NGridRenderer.Render(_currentGrid);
                    MessageBox.Show("Walls etc. updated successfully!");
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
        }

        private void BtnShiftHeight_Click(object sender, RoutedEventArgs e)
        {
            if (_currentGrid == null) return;
            string input = Microsoft.VisualBasic.Interaction.InputBox("Enter Offset (e.g. 100.0 to go up):", "Shift Height", "0.0");
            if (float.TryParse(input, out float offset) && offset != 0)
            {
                _currentGrid.MinBounds = new Vector3(_currentGrid.MinBounds.x, _currentGrid.MinBounds.y + offset, _currentGrid.MinBounds.z);
                _currentGrid.MaxBounds = new Vector3(_currentGrid.MaxBounds.x, _currentGrid.MaxBounds.y + offset, _currentGrid.MaxBounds.z);
                for (int i = 0; i < _currentGrid.HeightSamples.Count; i++) _currentGrid.HeightSamples[i] += offset;

                UpdateInfoDisplay();
                MessageBox.Show($"Map shifted by {offset}.");
            }
        }
        private void BtnExportHeight_Click(object sender, RoutedEventArgs e)
        {
            if (_currentGrid == null) return;
            SaveFileDialog sfd = new SaveFileDialog { Filter = "Bitmap Image|*.bmp", FileName = "height_export.bmp" };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    int w = _currentGrid.HeightSampleCountX;
                    int h = _currentGrid.HeightSampleCountZ;

                    float minH = float.MaxValue;
                    float maxH = float.MinValue;
                    foreach (float val in _currentGrid.HeightSamples)
                    {
                        if (val < minH) minH = val;
                        if (val > maxH) maxH = val;
                    }
                    float range = maxH - minH;
                    if (range == 0) range = 1;

                    byte[] pixels = new byte[w * h];

                    for (int z = 0; z < h; z++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            float val = _currentGrid.HeightSamples[(z * w) + x];
                            byte gray = (byte)(((val - minH) / range) * 255);

                            int visualY = h - 1 - z;
                            pixels[(visualY * w) + x] = gray;
                        }
                    }

                    WriteableBitmap wbmp = new WriteableBitmap(w, h, 96, 96, System.Windows.Media.PixelFormats.Gray8, null);
                    wbmp.WritePixels(new Int32Rect(0, 0, w, h), pixels, w, 0);

                    using (FileStream stream = new FileStream(sfd.FileName, FileMode.Create))
                    {
                        BmpBitmapEncoder encoder = new BmpBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(wbmp));
                        encoder.Save(stream);
                    }

                    string txtPath = System.IO.Path.ChangeExtension(sfd.FileName, ".txt");

                    string metadata = $"MinX={_currentGrid.MinBounds.x.ToString(System.Globalization.CultureInfo.InvariantCulture)}\n" +
                                      $"MinZ={_currentGrid.MinBounds.z.ToString(System.Globalization.CultureInfo.InvariantCulture)}\n" +
                                      $"MaxX={_currentGrid.MaxBounds.x.ToString(System.Globalization.CultureInfo.InvariantCulture)}\n" +
                                      $"MaxZ={_currentGrid.MaxBounds.z.ToString(System.Globalization.CultureInfo.InvariantCulture)}\n" +
                                      $"MinHeight={minH.ToString(System.Globalization.CultureInfo.InvariantCulture)}\n" +
                                      $"MaxHeight={maxH.ToString(System.Globalization.CultureInfo.InvariantCulture)}\n" +
                                      $"Width={w}\n" +
                                      $"Height={h}";

                    File.WriteAllText(txtPath, metadata);

                    MessageBox.Show($"Exported BMP and Full Metadata!\n\nMin Height: {minH:F2}\nMax Height: {maxH:F2}");
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
        }
        private void BtnView3D_Click(object sender, RoutedEventArgs e)
        {
            if (_currentGrid == null) return;

            MapViewer3D viewer = new MapViewer3D(_currentGrid);
            viewer.Owner = this;
            viewer.Show();
        }

        private void BtnInjectHeight_Click(object sender, RoutedEventArgs e)
        {
            if (_currentGrid == null) return;
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Bitmap Image|*.bmp;*.png;*.jpg" };

            if (ofd.ShowDialog() == true)
            {
                try
                {
                    BitmapImage bmp = new BitmapImage(new Uri(ofd.FileName));

                    float minH = 0, maxH = 0;
                    float imgMinX = 0, imgMinZ = 0, imgMaxX = 0, imgMaxZ = 0;
                    bool hasWorldData = false;

                    string txtPath = System.IO.Path.ChangeExtension(ofd.FileName, ".txt");
                    if (File.Exists(txtPath))
                    {
                        string[] lines = File.ReadAllLines(txtPath);
                        foreach (string line in lines)
                        {
                            var val = line.Split('=')[1];
                            if (line.StartsWith("MinHeight=")) float.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out minH);
                            if (line.StartsWith("MaxHeight=")) float.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out maxH);

                            if (line.StartsWith("MinX=")) { float.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out imgMinX); hasWorldData = true; }
                            if (line.StartsWith("MinZ=")) float.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out imgMinZ);
                            if (line.StartsWith("MaxX=")) float.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out imgMaxX);
                            if (line.StartsWith("MaxZ=")) float.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out imgMaxZ);
                        }
                    }

                    if (minH == 0 && maxH == 0)
                    {
                        string minInput = Microsoft.VisualBasic.Interaction.InputBox("Enter Height for BLACK:", "Height", _currentGrid.MinBounds.y.ToString());
                        string maxInput = Microsoft.VisualBasic.Interaction.InputBox("Enter Height for WHITE:", "Height", (_currentGrid.MinBounds.y + 1000).ToString());
                        if (!float.TryParse(minInput, out minH) || !float.TryParse(maxInput, out maxH)) return;
                    }

                    float range = maxH - minH;

                    FormatConvertedBitmap grayBmp = new FormatConvertedBitmap(bmp, System.Windows.Media.PixelFormats.Gray8, null, 0);
                    int srcW = grayBmp.PixelWidth;
                    int srcH = grayBmp.PixelHeight;
                    byte[] pixels = new byte[srcW * srcH];
                    grayBmp.CopyPixels(pixels, srcW, 0);

                    int tgtW = _currentGrid.HeightSampleCountX;
                    int tgtH = _currentGrid.HeightSampleCountZ;

                    float tgtStepX = (_currentGrid.MaxBounds.x - _currentGrid.MinBounds.x) / (tgtW - 1);
                    float tgtStepZ = (_currentGrid.MaxBounds.z - _currentGrid.MinBounds.z) / (tgtH - 1);

                    float imgWidth = imgMaxX - imgMinX;
                    float imgDepth = imgMaxZ - imgMinZ;

                    bool flipY = ChkFlipY.IsChecked == true;
                    bool invertVal = ChkInvert.IsChecked == true;

                    float actualMin = float.MaxValue;
                    float actualMax = float.MinValue;

                    for (int z = 0; z < tgtH; z++)
                    {
                        for (int x = 0; x < tgtW; x++)
                        {
                            float srcX, srcY;

                            if (hasWorldData && imgWidth > 0 && imgDepth > 0)
                            {
                                float worldX = _currentGrid.MinBounds.x + (x * tgtStepX);
                                float worldZ = _currentGrid.MinBounds.z + (z * tgtStepZ);

                                float u = (worldX - imgMinX) / imgWidth;
                                float v = (worldZ - imgMinZ) / imgDepth;

                                srcX = u * (srcW - 1);

                                if (flipY) srcY = (1.0f - v) * (srcH - 1);
                                else srcY = v * (srcH - 1);
                            }
                            else
                            {
                                // stretching method but might be a bad idea
                                float u = x / (float)(tgtW - 1);
                                float v = z / (float)(tgtH - 1);
                                srcX = u * (srcW - 1);
                                if (flipY) srcY = (1.0f - v) * (srcH - 1);
                                else srcY = v * (srcH - 1);
                            }

                            if (srcX < 0 || srcX >= srcW - 1 || srcY < 0 || srcY >= srcH - 1)
                            {
                                continue;
                            }

                            float pixelVal = GetBilinearPixel(pixels, srcW, srcH, srcX, srcY);
                            if (invertVal) pixelVal = 255 - pixelVal;

                            float finalHeight = minH + ((pixelVal / 255.0f) * range);
                            _currentGrid.HeightSamples[(z * tgtW) + x] = finalHeight;

                            if (finalHeight < actualMin) actualMin = finalHeight;
                            if (finalHeight > actualMax) actualMax = finalHeight;
                        }
                    }

                    _currentGrid.MinBounds = new Vector3(_currentGrid.MinBounds.x, actualMin, _currentGrid.MinBounds.z);
                    _currentGrid.MaxBounds = new Vector3(_currentGrid.MaxBounds.x, actualMax, _currentGrid.MaxBounds.z);

                    UpdateInfoDisplay();
                    MessageBox.Show(hasWorldData ? "Height Injected using World Coordinates!" : "Height Injected using Stretching (No Metadata) ().");
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
        }
        private float GetBilinearPixel(byte[] pixels, int w, int h, float x, float y)
        {
            int x0 = (int)x; int y0 = (int)y;
            int x1 = Math.Min(x0 + 1, w - 1); int y1 = Math.Min(y0 + 1, h - 1);
            float tx = x - x0; float ty = y - y0;

            float p00 = pixels[(y0 * w) + x0];
            float p10 = pixels[(y0 * w) + x1];
            float p01 = pixels[(y1 * w) + x0];
            float p11 = pixels[(y1 * w) + x1];

            float top = p00 + (p10 - p00) * tx;
            float bottom = p01 + (p11 - p01) * tx;
            return top + (bottom - top) * ty;
        }
        private float GetBilinearPixel(byte[] pixels, int stride, int w, int h, float x, float y)
        {
            if (x < 0) x = 0;
            if (y < 0) y = 0;
            if (x >= w - 1) x = w - 1.001f;
            if (y >= h - 1) y = h - 1.001f;

            int x0 = (int)x;
            int y0 = (int)y;
            int x1 = x0 + 1;
            int y1 = y0 + 1;

            float tx = x - x0;
            float ty = y - y0;

            float p00 = pixels[(y0 * stride) + x0];
            float p10 = pixels[(y0 * stride) + x1];
            float p01 = pixels[(y1 * stride) + x0];
            float p11 = pixels[(y1 * stride) + x1];

            float top = p00 + (p10 - p00) * tx;
            float bottom = p01 + (p11 - p01) * tx;

            return top + (bottom - top) * ty;
        }

        private void BtnProjectHeight_Click(object sender, RoutedEventArgs e)
        {
            if (_currentGrid == null) return;

            MessageBox.Show("Please select the source map to copy terrain from.");
            OpenFileDialog ofd = new OpenFileDialog { Filter = "AI Mesh NGrid|*.aimesh_ngrid" };

            if (ofd.ShowDialog() == true)
            {
                try
                {
                    NGrid sourceMap = NGridFileReader.Load(ofd.FileName);

                    string input = Microsoft.VisualBasic.Interaction.InputBox("Enter optional offset (e.g. 0.0):", "Height Offset", "0.0");
                    float.TryParse(input, out float offset);

                    ApplyHeightProjection(_currentGrid, sourceMap, offset);

                    UpdateInfoDisplay();
                    MessageBox.Show("Terrain projected successfully!");
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
        }

        private void ApplyHeightProjection(NGrid target, NGrid source, float offset)
        {
            float srcWidth = source.MaxBounds.x - source.MinBounds.x;
            float srcDepth = source.MaxBounds.z - source.MinBounds.z;
            float srcStepX = srcWidth / (source.HeightSampleCountX - 1);
            float srcStepZ = srcDepth / (source.HeightSampleCountZ - 1);

            float tgtWidth = target.MaxBounds.x - target.MinBounds.x;
            float tgtDepth = target.MaxBounds.z - target.MinBounds.z;
            float tgtStepX = tgtWidth / (target.HeightSampleCountX - 1);
            float tgtStepZ = tgtDepth / (target.HeightSampleCountZ - 1);

            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;

            for (int z = 0; z < target.HeightSampleCountZ; z++)
            {
                for (int x = 0; x < target.HeightSampleCountX; x++)
                {
                    float worldX = target.MinBounds.x + (x * tgtStepX);
                    float worldZ = target.MinBounds.z + (z * tgtStepZ);

                    float relativeX = (worldX - source.MinBounds.x) / srcStepX;
                    float relativeZ = (worldZ - source.MinBounds.z) / srcStepZ;

                    float height = GetHeightAtLocation(source, relativeX, relativeZ);
                    float finalHeight = height + offset;

                    target.HeightSamples[(z * target.HeightSampleCountX) + x] = finalHeight;

                    if (finalHeight < minHeight) minHeight = finalHeight;
                    if (finalHeight > maxHeight) maxHeight = finalHeight;
                }
            }

            target.MinBounds = new Vector3(target.MinBounds.x, minHeight, target.MinBounds.z);
            target.MaxBounds = new Vector3(target.MaxBounds.x, maxHeight, target.MaxBounds.z);
        }

        private float GetHeightAtLocation(NGrid grid, float x, float z)
        {
            if (x < 0) x = 0;
            if (z < 0) z = 0;
            if (x >= grid.HeightSampleCountX - 1) x = grid.HeightSampleCountX - 1.001f;
            if (z >= grid.HeightSampleCountZ - 1) z = grid.HeightSampleCountZ - 1.001f;

            int x0 = (int)x;
            int z0 = (int)z;
            int x1 = x0 + 1;
            int z1 = z0 + 1;

            float tx = x - x0;
            float tz = z - z0;

            float h00 = grid.HeightSamples[(z0 * grid.HeightSampleCountX) + x0];
            float h10 = grid.HeightSamples[(z0 * grid.HeightSampleCountX) + x1];
            float h01 = grid.HeightSamples[(z1 * grid.HeightSampleCountX) + x0];
            float h11 = grid.HeightSamples[(z1 * grid.HeightSampleCountX) + x1];

            float top = ((1.0f - tx) * h00) + (tx * h10);
            float bottom = ((1.0f - tx) * h01) + (tx * h11);

            return top + ((bottom - top) * tz);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_currentGrid == null) return;
            SaveFileDialog sfd = new SaveFileDialog { Filter = "AI Mesh NGrid|*.aimesh_ngrid", FileName = "modified.aimesh_ngrid" };
            if (sfd.ShowDialog() == true)
            {
                try
                {
                    int version = 3; // Default (Index 0)
                    if (CmbVersion.SelectedIndex == 1) version = 5;
                    if (CmbVersion.SelectedIndex == 2) version = 7;

                    NGridWriter.Save(_currentGrid, sfd.FileName, version);
                    MessageBox.Show($"Saved as Version {version}!");
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
        }

        private void ImgMap_MouseMove(object sender, MouseEventArgs e)
        {
            if (_currentGrid == null) return;
            Point p = e.GetPosition(ImgMap);
            int x = (int)p.X;
            int z = (int)p.Y;
            NavGridCell cell = _currentGrid.GetCell(x, z);

            if (cell != null)
            {
                // Adding 0.5 * CellSize to get the center of the cell
                float worldX = _currentGrid.MinBounds.x + (cell.x * _currentGrid.CellSize) + (_currentGrid.CellSize / 2);
                float worldZ = _currentGrid.MinBounds.z + (cell.z * _currentGrid.CellSize) + (_currentGrid.CellSize / 2);

                TxtCellDetails.Text = $"Grid: [{cell.x}, {cell.z}]\n" +
                                      $"Game: ({worldX:F0}, {worldZ:F0})\n" +
                                      $"----------------\n" +
                                      $"Flags: {cell.visionPathingFlags}\n" +
                                      $"Region: {cell.riverRegionFlags}";
            }
        }

        private void BtnMoveMap_Click(object sender, RoutedEventArgs e)
        {
            if (_currentGrid == null) return;

            string currentX = _currentGrid.MinBounds.x.ToString("F2");
            string currentZ = _currentGrid.MinBounds.z.ToString("F2");

            string inputX = Microsoft.VisualBasic.Interaction.InputBox(
                $"Current Min X: {currentX}\nEnter New Min X:",
                "Move Map X",
                currentX);

            string inputZ = Microsoft.VisualBasic.Interaction.InputBox(
                $"Current Min Z: {currentZ}\nEnter New Min Z:",
                "Move Map Z",
                currentZ);

            if (float.TryParse(inputX, out float newX) && float.TryParse(inputZ, out float newZ))
            {
                float offsetX = newX - _currentGrid.MinBounds.x;
                float offsetZ = newZ - _currentGrid.MinBounds.z;

                _currentGrid.MinBounds = new Vector3(newX, _currentGrid.MinBounds.y, newZ);

                _currentGrid.MaxBounds = new Vector3(
                    _currentGrid.MaxBounds.x + offsetX,
                    _currentGrid.MaxBounds.y,
                    _currentGrid.MaxBounds.z + offsetZ
                );

                UpdateInfoDisplay();
                MessageBox.Show($"Map moved to:\nX: {newX}\nZ: {newZ}");
            }
            else
            {
                MessageBox.Show("Invalid coordinates.");
            }
        }
        private async void BtnWgeoToNGrid_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "World Geometry|*.wgeo;*.nvr" };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    List<WGeoModelData> models;
                    if (ofd.FileName.EndsWith(".nvr"))
                        models = NvrReader.Load(ofd.FileName);
                    else
                        models = WGeoReader.Load(ofd.FileName);

                    if (models.Count == 0) { MessageBox.Show("No meshes found."); return; }

                    string inputSize = Microsoft.VisualBasic.Interaction.InputBox(
                        "Enter Cell Size (e.g. 50.0):", "Generation Settings", "50.0");
                    if (!float.TryParse(inputSize, out float cellSize) || cellSize <= 0) return;

                    float offset = 0.0f;

                    this.Cursor = Cursors.Wait;
                    TxtCursorInfo.Text = "Generating NGrid from Mesh... Please wait.";

                    NGrid newGrid = await WGeoToNGridGenerator.GenerateAsync(models, cellSize, offset);

                    _currentGrid = newGrid;
                    ImgMap.Source = NGridRenderer.Render(_currentGrid);
                    UpdateInfoDisplay();

                    this.Cursor = Cursors.Arrow;
                    MessageBox.Show($"Generated NGrid!\n\nApplied Offset: {offset}");
                }
                catch (Exception ex)
                {
                    this.Cursor = Cursors.Arrow;
                    MessageBox.Show("Error generating grid: " + ex.Message);
                }
            }
        }
    }
}