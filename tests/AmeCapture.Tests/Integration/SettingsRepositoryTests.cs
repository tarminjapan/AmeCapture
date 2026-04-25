using AmeCapture.Application.Interfaces;
using AmeCapture.Domain.Entities;
using AmeCapture.Infrastructure.Database;
using AmeCapture.Infrastructure.Repositories;

namespace AmeCapture.Tests.Integration
{
    public class SettingsRepositoryTests : IAsyncLifetime
    {
        private readonly string _dbPath;
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly SettingsRepository _repo;

        public SettingsRepositoryTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.db");
            _connectionFactory = new SqliteConnectionFactory(_dbPath);
            _repo = new SettingsRepository(_connectionFactory);
        }

        public async Task InitializeAsync()
        {
            await DatabaseInitializer.InitializeAsync(_connectionFactory);
        }

        public Task DisposeAsync()
        {
            try
            {
                if (File.Exists(_dbPath))
                {
                    File.Delete(_dbPath);
                }
            }
            catch
            {
            }
            return Task.CompletedTask;
        }

        [Fact]
        public async Task GetAsync_EmptyDb_ReturnsDefaultSettings()
        {
            var settings = await _repo.GetAsync();

            Assert.Equal(string.Empty, settings.SavePath);
            Assert.Equal("png", settings.ImageFormat);
            Assert.False(settings.StartMinimized);
            Assert.Equal("Ctrl+Shift+S", settings.HotkeyCaptureRegion);
            Assert.Equal("Ctrl+Shift+F", settings.HotkeyCaptureFullscreen);
            Assert.Equal("Ctrl+Shift+W", settings.HotkeyCaptureWindow);
        }

        [Fact]
        public async Task SaveAsync_And_GetAsync_Roundtrip()
        {
            var settings = new AppSettings
            {
                SavePath = @"C:\Users\user\Pictures\AmeCapture",
                ImageFormat = "jpg",
                StartMinimized = true,
                HotkeyCaptureRegion = "Ctrl+Alt+S",
                HotkeyCaptureFullscreen = "Ctrl+Alt+F",
                HotkeyCaptureWindow = "Ctrl+Alt+W"
            };

            await _repo.SaveAsync(settings);
            var loaded = await _repo.GetAsync();

            Assert.Equal(@"C:\Users\user\Pictures\AmeCapture", loaded.SavePath);
            Assert.Equal("jpg", loaded.ImageFormat);
            Assert.True(loaded.StartMinimized);
            Assert.Equal("Ctrl+Alt+S", loaded.HotkeyCaptureRegion);
            Assert.Equal("Ctrl+Alt+F", loaded.HotkeyCaptureFullscreen);
            Assert.Equal("Ctrl+Alt+W", loaded.HotkeyCaptureWindow);
        }

        [Fact]
        public async Task SaveAsync_UpdatesExistingSettings()
        {
            var settings = new AppSettings { SavePath = "/original" };
            await _repo.SaveAsync(settings);

            settings.SavePath = "/updated";
            await _repo.SaveAsync(settings);

            var loaded = await _repo.GetAsync();
            Assert.Equal("/updated", loaded.SavePath);
        }
    }
}
