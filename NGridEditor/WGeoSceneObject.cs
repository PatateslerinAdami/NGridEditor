using System.Windows.Media.Media3D;

namespace NGridMashEditor
{
    public class WGeoSceneObject
    {
        public GeometryModel3D Model { get; private set; }
        public string TextureName { get; set; }
        public string SourceFileName { get; set; }

        public TranslateTransform3D Translation { get; private set; } = new TranslateTransform3D();
        public ScaleTransform3D Scale { get; private set; } = new ScaleTransform3D();
        public AxisAngleRotation3D RotX { get; private set; } = new AxisAngleRotation3D(new Vector3D(1, 0, 0), 0);
        public AxisAngleRotation3D RotY { get; private set; } = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);
        public AxisAngleRotation3D RotZ { get; private set; } = new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0);

        public WGeoSceneObject(GeometryModel3D model, string textureName, string fileName)
        {
            Model = model;
            TextureName = textureName;
            SourceFileName = fileName;

            var group = new Transform3DGroup();
            group.Children.Add(Scale);
            group.Children.Add(new RotateTransform3D(RotX));
            group.Children.Add(new RotateTransform3D(RotY));
            group.Children.Add(new RotateTransform3D(RotZ));
            group.Children.Add(Translation);

            Model.Transform = group;
        }
    }
}