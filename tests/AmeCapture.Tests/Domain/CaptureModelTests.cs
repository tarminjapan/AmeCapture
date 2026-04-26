using AmeCapture.Application.Models;

namespace AmeCapture.Tests.Domain
{
    public class CaptureModelTests
    {
        [Fact]
        public void CaptureRegion_DefaultValues()
        {
            var region = new CaptureRegion();
            Assert.Equal(0, region.X);
            Assert.Equal(0, region.Y);
            Assert.Equal(0u, region.Width);
            Assert.Equal(0u, region.Height);
        }

        [Fact]
        public void CaptureRegion_SetValues()
        {
            var region = new CaptureRegion { X = 10, Y = 20, Width = 100, Height = 200 };
            Assert.Equal(10, region.X);
            Assert.Equal(20, region.Y);
            Assert.Equal(100u, region.Width);
            Assert.Equal(200u, region.Height);
        }

        [Fact]
        public void CaptureResult_DefaultValues()
        {
            var result = new CaptureResult();
            Assert.Equal(string.Empty, result.FilePath);
            Assert.Equal(0u, result.Width);
            Assert.Equal(0u, result.Height);
        }

        [Fact]
        public void RegionCaptureInfo_DefaultValues()
        {
            var info = new RegionCaptureInfo();
            Assert.Equal(string.Empty, info.TempPath);
            Assert.Equal(0u, info.ScreenWidth);
            Assert.Equal(0u, info.ScreenHeight);
        }
    }
}
