using FileProcessingPipeline.Models;
using FileProcessingPipeline.Services;

namespace FileProcessingPipeline;

internal class Program
{
    private static async Task Main()
    {
        Console.WriteLine("================================");
        Console.WriteLine("     FILE PROCESSING PIPELINE (П1)");
        Console.WriteLine("================================");

        string dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");

        // Создаем коллекцию заданий
        var tasks = new List<FileTask>
        {
            new(1, Path.Combine(dataDirectory, "file1.txt")),
            new(2, Path.Combine(dataDirectory, "file2.txt")),
            new(3, Path.Combine(dataDirectory, "file3.txt")),
            new(4, Path.Combine(dataDirectory, "file4.txt"))
        };

        Console.WriteLine($"Создано заданий: {tasks.Count}");
        Console.WriteLine("Запуск параллельной обработки (3 обработчика)...");
        Console.WriteLine();

        // Запуск в параллельном режиме
        var report = await PipelineProcessor.ProcessAsync(tasks, 3);

        Console.WriteLine("\n========== ИТОГИ П1 ==========");
        Console.WriteLine($"Успешно обработано: {report.SuccessfulFiles}");
        Console.WriteLine($"Ошибок: {report.FailedFiles}");
        Console.WriteLine($"Время: {report.ProcessingTimeMs} мс");

        Console.WriteLine("\nВсе задачи распределены между воркерами!");
    }
}