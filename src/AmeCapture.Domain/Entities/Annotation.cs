namespace AmeCapture.Domain.Entities
{
    public enum EditorTool
    {
        Select,
        Arrow,
        Rectangle,
        Text,
        Mosaic,
        Crop
    }

    public abstract record Annotation
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public abstract string Type { get; }
    }

    public record ArrowAnnotation : Annotation
    {
        public override string Type => "arrow";
        public double StartX { get; init; }
        public double StartY { get; init; }
        public double EndX { get; init; }
        public double EndY { get; init; }
        public string StrokeColor { get; init; } = "#FF0000";
        public int StrokeWidth { get; init; } = 3;
    }

    public record RectangleAnnotation : Annotation
    {
        public override string Type => "rectangle";
        public double X { get; init; }
        public double Y { get; init; }
        public double Width { get; init; }
        public double Height { get; init; }
        public string StrokeColor { get; init; } = "#FF0000";
        public int StrokeWidth { get; init; } = 3;
    }

    public record MosaicAnnotation : Annotation
    {
        public override string Type => "mosaic";
        public double X { get; init; }
        public double Y { get; init; }
        public double Width { get; init; }
        public double Height { get; init; }
        public int Strength { get; init; } = 20;
    }

    public record TextAnnotation : Annotation
    {
        public override string Type => "text";
        public double X { get; init; }
        public double Y { get; init; }
        public string Text { get; init; } = string.Empty;
        public double FontSize { get; init; } = 24;
        public string StrokeColor { get; init; } = "#FF0000";
    }

    public record CropAnnotation : Annotation
    {
        public override string Type => "crop";
        public double X { get; init; }
        public double Y { get; init; }
        public double Width { get; init; }
        public double Height { get; init; }
    }
}
