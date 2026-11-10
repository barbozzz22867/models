using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using FileProcessingPipeline.Models;

namespace FileProcessingPipeline.Services;

public static class PipelineProcessor
{
    public static async Task<AnalysisReport> ProcessAsync(IEnumerable<FileTask> tasks, int workerCount)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        if (workerCount < 1) throw new ArgumentOutOfRangeException(nameof(workerCount));

        var taskList = tasks.ToList();
        var results = new ConcurrentBag<FileAnalysisResult>();
        var errors = new ConcurrentBag<FileProcessingError>();

        var stopwatch = Stopwatch.StartNew();

        // TODO П1: Создать механизм передачи заданий.
        var channel = Channel.CreateUnbounded<FileTask>();

        // TODO П1: Запустить workerCount обработчиков.
        var workers = Enumerable.Range(1, workerCount)
            .Select(workerId => Task.Run(async () =>
            {
                // TODO П1: Организовать получение и обработку заданий.
                await foreach (var task in channel.Reader.ReadAllAsync())
                {
                    await WorkerAsync(workerId, task, results, errors);
                }
            }))
            .ToArray();

        // Отправляем задания в канал.
        foreach (var task in taskList)
        {
            await channel.Writer.WriteAsync(task);
        }

        // Закрываем канал для записи.
        channel.Writer.Complete();

        // TODO П1: Дождаться завершения всех обработчиков.
        await Task.WhenAll(workers);

        stopwatch.Stop();

        return ReportService.Create(
            $"Pipeline ({workerCount} workers)",
            taskList.Count, results.ToList(), errors.ToList(), workerCount, stopwatch.ElapsedMilliseconds);
    }

    private static async Task WorkerAsync(
        int workerId, FileTask task, ConcurrentBag<FileAnalysisResult> results, ConcurrentBag<FileProcessingError> errors)
    {
        try
        {
            // TODO 1 (Задание 3): Вывести ID обработчика.
            Console.WriteLine($"[WORKER {workerId}] Начал: {task.FilePath}");

            // TODO 2 (Задание 3): Выполнить анализ файла.
            var result = await FileAnalyzer.AnalyzeAsync(task, workerId);
            results.Add(result);

            // TODO 3 (Задание 3): Вывести результат.
            Console.WriteLine($"[WORKER {workerId}] Завершил: {result.FileName}");
        }
        catch (IOException ex)
        {
            errors.Add(new FileProcessingError(task.Id, Path.GetFileName(task.FilePath), nameof(IOException), ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            errors.Add(new FileProcessingError(task.Id, Path.GetFileName(task.FilePath), nameof(UnauthorizedAccessException), ex.Message));
        }
    }
}