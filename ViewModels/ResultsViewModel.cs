using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using SqlStressRunner.Commands;
using SqlStressRunner.Infrastructure;
using SqlStressRunner.Models;
using SqlStressRunner.Services;

namespace SqlStressRunner.ViewModels;

public class ResultsViewModel : ViewModelBase
{
    private readonly ReportGenerationService _reportGenerationService = new();
    private MetricsSummary? _currentSummary;
    private ObservableCollection<IterationResult> _iterationResults = new();
    private ObservableCollection<StoredProcedureMetricSummary> _storedProcedureMetrics = new();

    public ResultsViewModel()
    {
        CopyRunIdCommand = new RelayCommand(CopyRunIdToClipboard, CanCopyRunId);
        GenerateReportCommand = new RelayCommand(GenerateReport, CanGenerateReport);
    }

    public RelayCommand CopyRunIdCommand { get; }
    public RelayCommand GenerateReportCommand { get; }

    public MetricsSummary? CurrentSummary
    {
        get => _currentSummary;
        set => SetProperty(ref _currentSummary, value);
    }

    public ObservableCollection<IterationResult> IterationResults
    {
        get => _iterationResults;
        set => SetProperty(ref _iterationResults, value);
    }

    public ObservableCollection<StoredProcedureMetricSummary> StoredProcedureMetrics
    {
        get => _storedProcedureMetrics;
        set => SetProperty(ref _storedProcedureMetrics, value);
    }

    public string RunIdText => CurrentSummary != null ? CurrentSummary.RunId.ToString() : "N/A";
    public string TotalDurationText => CurrentSummary != null ? $"{CurrentSummary.TotalDurationSeconds:F2} seconds ({CurrentSummary.TotalDurationMs} ms)" : "N/A";
    public string TotalIterationsText => CurrentSummary?.TotalIterations.ToString() ?? "0";
    public string SuccessfulIterationsText => CurrentSummary?.SuccessfulIterations.ToString() ?? "0";
    public string FailedIterationsText => CurrentSummary?.FailedIterations.ToString() ?? "0";
    public string TpsPerIterationText => CurrentSummary != null ? $"{CurrentSummary.TpsPerIteration:F2}" : "0.00";
    public string TpsPerSpText => CurrentSummary != null ? $"{CurrentSummary.TpsPerStoredProcedure:F2}" : "0.00";
    public string AverageLatencyText => CurrentSummary != null ? $"{CurrentSummary.AverageLatencyMs:F2} ms" : "N/A";
    public string MinLatencyText => CurrentSummary != null ? $"{CurrentSummary.MinLatencyMs} ms" : "N/A";
    public string MaxLatencyText => CurrentSummary != null ? $"{CurrentSummary.MaxLatencyMs} ms" : "N/A";
    public string P95LatencyText => CurrentSummary != null ? $"{CurrentSummary.P95LatencyMs} ms" : "N/A";
    public string P99LatencyText => CurrentSummary != null ? $"{CurrentSummary.P99LatencyMs} ms" : "N/A";

    public void UpdateResults(MetricsSummary summary)
    {
        CurrentSummary = summary;
        IterationResults = new ObservableCollection<IterationResult>(
            summary.IterationResults.OrderBy(r => r.IterationNumber));
        StoredProcedureMetrics = new ObservableCollection<StoredProcedureMetricSummary>(
            summary.AverageSpDurations
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => new StoredProcedureMetricSummary
                {
                    StoredProcedureName = kvp.Key,
                    AverageDurationMs = kvp.Value
                }));

        OnPropertyChanged(nameof(RunIdText));
        OnPropertyChanged(nameof(TotalDurationText));
        OnPropertyChanged(nameof(TotalIterationsText));
        OnPropertyChanged(nameof(SuccessfulIterationsText));
        OnPropertyChanged(nameof(FailedIterationsText));
        OnPropertyChanged(nameof(TpsPerIterationText));
        OnPropertyChanged(nameof(TpsPerSpText));
        OnPropertyChanged(nameof(AverageLatencyText));
        OnPropertyChanged(nameof(MinLatencyText));
        OnPropertyChanged(nameof(MaxLatencyText));
        OnPropertyChanged(nameof(P95LatencyText));
        OnPropertyChanged(nameof(P99LatencyText));
        CopyRunIdCommand.RaiseCanExecuteChanged();
        GenerateReportCommand.RaiseCanExecuteChanged();
    }

    private bool CanCopyRunId()
    {
        return CurrentSummary != null;
    }

    private void CopyRunIdToClipboard()
    {
        if (CurrentSummary != null)
        {
            Clipboard.SetText(CurrentSummary.RunId.ToString());
            MessageBox.Show("RunId copied to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private bool CanGenerateReport()
    {
        return CurrentSummary != null;
    }

    private void GenerateReport()
    {
        if (CurrentSummary == null)
        {
            return;
        }

        try
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save Results Report",
                Filter = "Markdown report (*.md)|*.md|Text report (*.txt)|*.txt",
                FileName = $"SqlStressRunner_Report_{CurrentSummary.RunId:N}.md",
                DefaultExt = ".md",
                AddExtension = true
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var report = _reportGenerationService.GenerateMarkdownReport(CurrentSummary);
            File.WriteAllText(dialog.FileName, report);

            MessageBox.Show($"Report generated successfully:\n{dialog.FileName}",
                "Report Generated",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error generating report: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
