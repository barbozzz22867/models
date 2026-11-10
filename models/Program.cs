using System.Text.Json;
using FileProcessingPipeline.Models;
using FileProcessingPipeline.Services;

namespace FileProcessingPipeline;

internal class Program
{
    private static async Task Main()
    {
        Console.WriteLine("================================");
        Console.WriteLine("  FILE PROCESSING PIPELINE (П2)");
        Console.WriteLine("================================");

        string dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(dataDirectory);

        // П2, Задание 2: Создаем ситуацию с ошибкой (файл missing.txt не существует)
        var tasks = new List<FileTask>
        {
            new(1, Path.Combine(dataDirectory, "file1.txt")),
            new(2, Path.Combine(dataDirectory, "file2.txt")),
            new(3, Path.Combine(dataDirectory, "file3.txt")),
            new(4, Path.Combine(dataDirectory, "file4.txt")),
            new(5, Path.Combine(dataDirectory, "missing.txt")) // Вызовет IOException
        };

        Console.WriteLine($"Всего заданий: {tasks.Count} (включая 1 несуществующий файл)");

        // П2, Задание 1 и 4: Сравниваем конфигурации.
        int[] workerCounts = { 1, 2, 4, 8 };

        Console.WriteLine("\n--- Сравнение производительности ---");
        Console.WriteLine($"{"Режим",-15} | {"Время (мс)",-12} | {"Успешно",-8} | {"Ошибок",-8}");
        Console.WriteLine(new string('-', 52));

        // Запускаем последовательный режим
        var seqReport = await SequentialProcessor.ProcessAsync(tasks);
        Console.WriteLine($"{"Sequential",-15} | {seqReport.ProcessingTimeMs,-12} | {seqReport.SuccessfulFiles,-8} | {seqReport.FailedFiles,-8}");

        // Запускаем параллельные режимы
        foreach (int count in workerCounts)
        {
            var report = await PipelineProcessor.ProcessAsync(tasks, count);
            Console.WriteLine($"{"Parallel " + count,-15} | {report.ProcessingTimeMs,-12} | {report.SuccessfulFiles,-8} | {report.FailedFiles,-8}");
        }

        // П2, Задание 3: Детальный отчет с ошибками и статистикой воркеров (для 3 воркеров)
        Console.WriteLine("\n--- ДЕТАЛЬНЫЙ ОТЧЕТ (Parallel 3) ---");
        var finalReport = await PipelineProcessor.ProcessAsync(tasks, 3);
        PrintDetailedReport(finalReport);

        // ДЗ: Сохранение в JSON
        await SaveReportAsync(finalReport);
    }

    private static void PrintDetailedReport(AnalysisReport report)
    {
        Console.WriteLine($"Режим: {report.Mode}");
        Console.WriteLine($"Всего файлов: {report.TotalFiles}");
        Console.WriteLine($"Успешно: {report.SuccessfulFiles}");
        Console.WriteLine($"Ошибок: {report.FailedFiles}");

        Console.WriteLine("\nРЕЗУЛЬТАТЫ:");
        foreach (var result in report.Results)
        {
            Console.WriteLine($"{result.FileName} | Worker: {result.WorkerId} | Строк: {result.Lines} | Слов: {result.Words}");
        }

        // П2, Задание 2: Вывод ошибок (безопасная обработка)
        if (report.Errors.Count > 0)
        {
            Console.WriteLine("\nОШИБКИ:");
            foreach (var error in report.Errors)
            {
                Console.WriteLine($"{error.FileName}: {error.ErrorType} - {error.Message}");
            }
        }

        // П2, Задание 3: Статистика исполнителей.
        Console.WriteLine("\n========== СТАТИСТИКА WORKERS ==========");
        if (report.Results.Count > 0)
        {
            var stats = report.Results
                .GroupBy(x => x.WorkerId)
                .Select(g => new { WorkerId = g.Key, Count = g.Count() })
                .OrderBy(x => x.WorkerId);

            int total = 0;
            foreach (var stat in stats)
            {
                Console.WriteLine($"Worker {stat.WorkerId}: {stat.Count} заданий");
                total += stat.Count;
            }
            Console.WriteLine($"\nВсего обработано: {total}");
        }
        else
        {
            Console.WriteLine("Нет успешно обработанных файлов.");
        }
        Console.WriteLine("=======================================");
    }

    private static async Task SaveReportAsync(AnalysisReport report)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(report, options);
        string path = Path.Combine(AppContext.BaseDirectory, "report.json");

        await File.WriteAllTextAsync(path, json);
        Console.WriteLine($"\nОтчёт сохранён: {path}");
    }
}