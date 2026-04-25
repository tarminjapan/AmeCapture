using System.Data.Common;
using AmeCapture.Application.Interfaces;
using AmeCapture.Domain.Entities;

namespace AmeCapture.Infrastructure.Repositories
{
    public class SettingsRepository(IDbConnectionFactory connectionFactory) : ISettingsRepository
    {
        private readonly IDbConnectionFactory _connectionFactory = connectionFactory;

        public async Task<AppSettings> GetAsync()
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT key, value FROM app_settings ORDER BY key";

            var settings = new AppSettings();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string key = reader.GetString(0);
                string value = reader.GetString(1);

                switch (key)
                {
                    case "save_path":
                        settings.SavePath = value;
                        break;
                    case "image_format":
                        settings.ImageFormat = value;
                        break;
                    case "start_minimized":
                        settings.StartMinimized = value == "true";
                        break;
                    case "hotkey_capture_region":
                        settings.HotkeyCaptureRegion = value;
                        break;
                    case "hotkey_capture_fullscreen":
                        settings.HotkeyCaptureFullscreen = value;
                        break;
                    case "hotkey_capture_window":
                        settings.HotkeyCaptureWindow = value;
                        break;
                    default:
                        break;
                }
            }

            return settings;
        }

        public async Task SaveAsync(AppSettings settings)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                await UpsertAsync(connection, transaction, "save_path", settings.SavePath);
                await UpsertAsync(connection, transaction, "image_format", settings.ImageFormat);
                await UpsertAsync(connection, transaction, "start_minimized",
                    settings.StartMinimized ? "true" : "false");
                await UpsertAsync(connection, transaction, "hotkey_capture_region", settings.HotkeyCaptureRegion);
                await UpsertAsync(connection, transaction, "hotkey_capture_fullscreen", settings.HotkeyCaptureFullscreen);
                await UpsertAsync(connection, transaction, "hotkey_capture_window", settings.HotkeyCaptureWindow);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static async Task UpsertAsync(DbConnection connection, DbTransaction transaction,
            string key, string value)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
            INSERT INTO app_settings (key, value) VALUES (@key, @value)
            ON CONFLICT(key) DO UPDATE SET value = @value";
            var keyParam = command.CreateParameter();
            keyParam.ParameterName = "@key";
            keyParam.Value = key;
            _ = command.Parameters.Add(keyParam);

            var valueParam = command.CreateParameter();
            valueParam.ParameterName = "@value";
            valueParam.Value = value;
            _ = command.Parameters.Add(valueParam);

            _ = await command.ExecuteNonQueryAsync();
        }
    }
}
