using LoLNGRIDConverter;
using Microsoft.Win32;
using System.Windows;

namespace NGridMashEditor
{
    public partial class GenerationSettingsWindow : Window
    {
        public float CellSize { get; private set; }
        public Vector3 MinBounds { get; private set; }
        public Vector3 MaxBounds { get; private set; }

        public float SampleOffsetX { get; private set; }
        public float SampleOffsetZ { get; private set; }
        public int? TemplateSampleCountX { get; private set; }
        public int? TemplateSampleCountZ { get; private set; }

        public GenerationSettingsWindow()
        {
            InitializeComponent();
        }

        private void BtnLoadTemplate_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "AI Mesh NGrid|*.aimesh_ngrid" };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    NGrid template = NGridFileReader.Load(ofd.FileName);

                    TxtMinX.Text = template.MinBounds.x.ToString();
                    TxtMinY.Text = template.MinBounds.y.ToString();
                    TxtMinZ.Text = template.MinBounds.z.ToString();
                    TxtCellSize.Text = template.CellSize.ToString();

                    MaxBounds = template.MaxBounds;

                    SampleOffsetX = template.HeightSampleOffsetX;
                    SampleOffsetZ = template.HeightSampleOffsetZ;
                    TemplateSampleCountX = template.HeightSampleCountX;
                    TemplateSampleCountZ = template.HeightSampleCountZ;

                    MessageBox.Show($"Template Loaded!\n\nCopied Bounds, Cell Size, and Grid Offsets.\n\nOffsets: {SampleOffsetX}, {SampleOffsetZ}");
                }
                catch (System.Exception ex) { MessageBox.Show("Error reading template: " + ex.Message); }
            }
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (!float.TryParse(TxtCellSize.Text, out float size)) { MessageBox.Show("Invalid Cell Size"); return; }
            if (!float.TryParse(TxtMinX.Text, out float x)) { MessageBox.Show("Invalid Min X"); return; }
            if (!float.TryParse(TxtMinY.Text, out float y)) { MessageBox.Show("Invalid Min Y"); return; }
            if (!float.TryParse(TxtMinZ.Text, out float z)) { MessageBox.Show("Invalid Min Z"); return; }

            CellSize = size;
            MinBounds = new Vector3(x, y, z);

            if (MaxBounds.x == 0 && MaxBounds.y == 0 && MaxBounds.z == 0)
            {
                MaxBounds = new Vector3(0, 0, 0);
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}