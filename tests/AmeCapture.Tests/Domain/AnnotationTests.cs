using AmeCapture.Domain.Entities;

namespace AmeCapture.Tests.Domain
{
    public class AnnotationTests
    {
        [Fact]
        public void ArrowAnnotation_DefaultValues_AreSet()
        {
            var ann = new ArrowAnnotation();

            Assert.NotEqual(string.Empty, ann.Id);
            Assert.Equal("arrow", ann.Type);
            Assert.Equal(0, ann.StartX);
            Assert.Equal(0, ann.StartY);
            Assert.Equal(0, ann.EndX);
            Assert.Equal(0, ann.EndY);
            Assert.Equal("#FF0000", ann.StrokeColor);
            Assert.Equal(3, ann.StrokeWidth);
        }

        [Fact]
        public void RectangleAnnotation_DefaultValues_AreSet()
        {
            var ann = new RectangleAnnotation();

            Assert.NotEqual(string.Empty, ann.Id);
            Assert.Equal("rectangle", ann.Type);
            Assert.Equal(0, ann.X);
            Assert.Equal(0, ann.Y);
            Assert.Equal(0, ann.Width);
            Assert.Equal(0, ann.Height);
            Assert.Equal("#FF0000", ann.StrokeColor);
            Assert.Equal(3, ann.StrokeWidth);
        }

        [Fact]
        public void MosaicAnnotation_DefaultValues_AreSet()
        {
            var ann = new MosaicAnnotation();

            Assert.NotEqual(string.Empty, ann.Id);
            Assert.Equal("mosaic", ann.Type);
            Assert.Equal(0, ann.X);
            Assert.Equal(0, ann.Y);
            Assert.Equal(0, ann.Width);
            Assert.Equal(0, ann.Height);
            Assert.Equal(20, ann.Strength);
        }

        [Fact]
        public void TextAnnotation_DefaultValues_AreSet()
        {
            var ann = new TextAnnotation();

            Assert.NotEqual(string.Empty, ann.Id);
            Assert.Equal("text", ann.Type);
            Assert.Equal(0, ann.X);
            Assert.Equal(0, ann.Y);
            Assert.Equal(string.Empty, ann.Text);
            Assert.Equal(24, ann.FontSize);
            Assert.Equal("#FF0000", ann.StrokeColor);
        }

        [Fact]
        public void CropAnnotation_DefaultValues_AreSet()
        {
            var ann = new CropAnnotation();

            Assert.NotEqual(string.Empty, ann.Id);
            Assert.Equal("crop", ann.Type);
            Assert.Equal(0, ann.X);
            Assert.Equal(0, ann.Y);
            Assert.Equal(0, ann.Width);
            Assert.Equal(0, ann.Height);
        }

        [Fact]
        public void EditorTool_Values_AreCorrect()
        {
            Assert.Equal(0, (int)EditorTool.Select);
            Assert.Equal(1, (int)EditorTool.Arrow);
            Assert.Equal(2, (int)EditorTool.Rectangle);
            Assert.Equal(3, (int)EditorTool.Text);
            Assert.Equal(4, (int)EditorTool.Mosaic);
            Assert.Equal(5, (int)EditorTool.Crop);
        }

        [Fact]
        public void Annotation_Inheritance_WorksCorrectly()
        {
            Annotation arrow = new ArrowAnnotation();
            Annotation rect = new RectangleAnnotation();
            Annotation mosaic = new MosaicAnnotation();
            Annotation text = new TextAnnotation();
            Annotation crop = new CropAnnotation();

            _ = Assert.IsType<ArrowAnnotation>(arrow);
            _ = Assert.IsType<RectangleAnnotation>(rect);
            _ = Assert.IsType<MosaicAnnotation>(mosaic);
            _ = Assert.IsType<TextAnnotation>(text);
            _ = Assert.IsType<CropAnnotation>(crop);

            Assert.Equal("arrow", arrow.Type);
            Assert.Equal("rectangle", rect.Type);
            Assert.Equal("mosaic", mosaic.Type);
            Assert.Equal("text", text.Type);
            Assert.Equal("crop", crop.Type);
        }

        [Fact]
        public void ArrowAnnotation_With_Values()
        {
            var ann = new ArrowAnnotation
            {
                StartX = 10,
                StartY = 20,
                EndX = 100,
                EndY = 200,
                StrokeColor = "#00FF00",
                StrokeWidth = 5,
            };

            Assert.Equal(10, ann.StartX);
            Assert.Equal(20, ann.StartY);
            Assert.Equal(100, ann.EndX);
            Assert.Equal(200, ann.EndY);
            Assert.Equal("#00FF00", ann.StrokeColor);
            Assert.Equal(5, ann.StrokeWidth);
        }

        [Fact]
        public void ArrowAnnotation_WithExpression_CreatesCopy()
        {
            var original = new ArrowAnnotation
            {
                StartX = 10,
                StartY = 20,
                EndX = 100,
                EndY = 200,
                StrokeColor = "#00FF00",
                StrokeWidth = 5,
            };

            var copy = original with { EndX = 150, EndY = 250 };

            Assert.Equal(10, copy.StartX);
            Assert.Equal(20, copy.StartY);
            Assert.Equal(150, copy.EndX);
            Assert.Equal(250, copy.EndY);
            Assert.Equal("#00FF00", copy.StrokeColor);
            Assert.Equal(original.Id, copy.Id);
        }
    }
}
