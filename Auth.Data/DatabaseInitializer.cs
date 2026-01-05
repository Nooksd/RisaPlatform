using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Auth.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        AuthDbContext context,
        string connectionString,
        ILogger logger,
        CancellationToken ct = default)
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            var databaseName = builder.Database;
            builder.Database = "postgres";
            var masterConnectionString = builder.ToString();

            bool databaseWasCreated = false;

            await using (var masterConnection = new NpgsqlConnection(masterConnectionString))
            {
                await masterConnection.OpenAsync(ct);

                await using var checkCmd = new NpgsqlCommand(
                    $"SELECT 1 FROM pg_database WHERE datname = '{databaseName}'",
                    masterConnection);

                var exists = await checkCmd.ExecuteScalarAsync(ct);

                if (exists is null)
                {
                    logger.LogInformation("Database '{DatabaseName}' não existe. Criando...", databaseName);

                    await using var createCmd = new NpgsqlCommand(
                        $"CREATE DATABASE {databaseName}",
                        masterConnection);

                    await createCmd.ExecuteNonQueryAsync(ct);
                    logger.LogInformation("Database '{DatabaseName}' criado com sucesso", databaseName);

                    databaseWasCreated = true;
                }
                else
                {
                    logger.LogInformation("Database '{DatabaseName}' já existe. Migrações não serão aplicadas automaticamente.", databaseName);
                }
            }

            if (databaseWasCreated)
            {
                logger.LogInformation("Aplicando migrações iniciais no novo banco de dados...");

                await using (var connection = new NpgsqlConnection(connectionString))
                {
                    await connection.OpenAsync(ct);

                    await using var createSchemaCmd = new NpgsqlCommand(
                        "CREATE SCHEMA IF NOT EXISTS auth",
                        connection);

                    await createSchemaCmd.ExecuteNonQueryAsync(ct);
                    logger.LogInformation("Schema 'auth' criado com sucesso");
                }

                var pendingMigrations = await context.Database.GetPendingMigrationsAsync(ct);

                if (pendingMigrations.Any())
                {
                    logger.LogInformation("Aplicando {Count} migrações pendentes...", pendingMigrations.Count());

                    foreach (var migration in pendingMigrations)
                    {
                        logger.LogInformation("Migração pendente: {Migration}", migration);
                    }

                    await context.Database.MigrateAsync(ct);
                    logger.LogInformation("Todas as migrações aplicadas com sucesso no novo banco de dados");
                }
            }
            else
            {
                logger.LogInformation("Para aplicar migrações em banco existente, use: dotnet ef database update");
            }

            // Verificar conexão
            var canConnect = await context.Database.CanConnectAsync(ct);

            if (canConnect)
            {
                logger.LogInformation("Conexão com database verificada com sucesso");
            }
            else
            {
                logger.LogError("Falha ao verificar conexão com database");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao inicializar o database");
            throw;
        }
    }
}