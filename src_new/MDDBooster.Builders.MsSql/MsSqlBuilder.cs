namespace MDDBooster.Builders.MsSql;

/// <summary>
/// MS-SQL script generator
/// </summary>
public class MsSqlBuilder : IBuilder
{
    /// <summary>
    /// Builder type identifier
    /// </summary>
    public string BuilderType => "MsSql";

    /// <summary>
    /// Create a builder config instance specific to this builder
    /// </summary>
    public IBuilderConfig CreateDefaultConfig()
    {
        return new MsSqlBuilderConfig
        {
            ProjectPath = string.Empty,
            TablePath = "dbo/Tables_",
            GenerateIndividualFiles = true,
            GenerateCompleteFile = true,
            SchemaOnly = false,
            UseCreateIfNotExists = true,
            IncludeIndexes = true,
            ClearOutputDirectoryBeforeGeneration = true // Add default for new property
        };
    }

    /// <summary>
    /// Process an MDD document with the provided configuration
    /// </summary>
    /// <param name="document">The document to process</param>
    /// <param name="config">Builder-specific configuration</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool Process(MDDDocument document, IBuilderConfig config)
    {
        if (!(config is MsSqlBuilderConfig msSqlConfig))
        {
            AppLog.Error("Invalid configuration type for MsSqlBuilder");
            return false;
        }

        string outputDir = msSqlConfig.GetFullOutputPath();
        if (string.IsNullOrEmpty(outputDir))
        {
            AppLog.Error("No output directory specified for MsSqlBuilder");
            return false;
        }

        AppLog.Information("Generating MS-SQL, output directory: {OutputDir}", outputDir);

        try
        {
            // Create directory if it doesn't exist
            if (!Directory.Exists(outputDir))
            {
                AppLog.Debug("Creating output directory: {OutputDir}", outputDir);
                Directory.CreateDirectory(outputDir);
            }
            // Clear existing files in the directory if configured to do so
            else if (msSqlConfig.ClearOutputDirectoryBeforeGeneration)
            {
                AppLog.Information("Clearing output directory: {OutputDir}", outputDir);
                ClearDirectory(outputDir);
            }

            // Create SQL generator
            AppLog.Debug("Creating SQL script generator");
            var generator = document.CreateScriptGenerator(msSqlConfig.UseSchemaNamespace, msSqlConfig.SchemaName);

            // Generate complete script if enabled
            if (msSqlConfig.GenerateCompleteFile)
            {
                string sql;
                if (msSqlConfig.SchemaOnly)
                {
                    AppLog.Debug("Generating schema-only SQL");
                    sql = generator.GenerateScripts()["schema"];
                }
                else
                {
                    AppLog.Debug("Generating complete SQL");
                    sql = generator.GenerateCompleteScript();
                }

                string fileName = Path.GetFileNameWithoutExtension(document.BaseDocument.Namespace);
                if (string.IsNullOrEmpty(fileName))
                    fileName = "complete";

                string outputPath = Path.Combine(outputDir, $"{fileName}_complete.sql");

                // Write to file
                AppLog.Debug("Writing SQL to file: {OutputPath}", outputPath);
                File.WriteAllText(outputPath, sql);
                Console.WriteLine($"Complete SQL script written to: {outputPath}");
            }

            // Generate individual table files if enabled
            if (msSqlConfig.GenerateIndividualFiles)
            {
                GenerateIndividualTableFiles(document, outputDir, msSqlConfig.SchemaOnly, msSqlConfig.UseSchemaNamespace, msSqlConfig.SchemaName);
            }

            AppLog.Information("MS-SQL generation completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Error generating MS-SQL");
            return false;
        }
    }

    /// <summary>
    /// Clears all files in the specified directory but leaves the directory structure intact
    /// </summary>
    /// <param name="directory">Directory path to clear</param>
    private void ClearDirectory(string directory)
    {
        try
        {
            // Get all files in the directory
            string[] files = Directory.GetFiles(directory);
            int fileCount = files.Length;

            // Delete each file
            foreach (string file in files)
            {
                try
                {
                    File.Delete(file);
                    AppLog.Debug("Deleted file: {FilePath}", file);
                }
                catch (Exception ex)
                {
                    AppLog.Warning(ex, "Failed to delete file: {FilePath}", file);
                }
            }

            AppLog.Information("Cleared {Count} files from directory: {Directory}", fileCount, directory);
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Error clearing directory: {Directory}", directory);
            throw;
        }
    }

    private static void GenerateIndividualTableFiles(MDDDocument document, string outputDir, bool schemaOnly, bool useSchemaNamespace, string schemaNameOverride)
    {
        AppLog.Information("Generating individual table SQL files");

        try
        {
            // Loop through all non-abstract models
            var nonAbstractModels = document.Models.Where(m => !m.BaseModel.IsAbstract).ToList();

            foreach (var model in nonAbstractModels)
            {
                // Create a temporary document with just this model
                var tempDoc = new MDDDocument
                {
                    BaseDocument = new M3LParser.Models.M3LDocument
                    {
                        Namespace = document.BaseDocument.Namespace,
                        Models = new List<M3LParser.Models.M3LModel> { model.BaseModel },
                        Interfaces = document.BaseDocument.Interfaces,
                        Enums = document.BaseDocument.Enums
                    },
                    Models = new List<MDDModel> { model },
                    Interfaces = document.Interfaces,
                    Enums = document.Enums
                };

                // Generate SQL for just this table
                var schemaGenerator = new MsSqlSchemaGenerator(tempDoc, useSchemaNamespace, schemaNameOverride);
                var tableSql = schemaGenerator.GenerateSchema();

                // Add triggers if not schema-only
                if (!schemaOnly)
                {
                    var triggerGenerator = new MsSqlTriggerGenerator(tempDoc);
                    var triggers = triggerGenerator.GenerateTriggers();

                    if (!string.IsNullOrWhiteSpace(triggers))
                    {
                        tableSql += "\n\n-- Triggers\n" + triggers;
                    }
                }

                // Write to file
                string outputPath = Path.Combine(outputDir, $"{model.BaseModel.Name}.sql");
                AppLog.Debug("Writing table SQL to file: {OutputPath}", outputPath);
                File.WriteAllText(outputPath, tableSql);
            }

            AppLog.Information("Generated {Count} individual table SQL files", nonAbstractModels.Count);
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Error generating individual table SQL files");
        }
    }
}