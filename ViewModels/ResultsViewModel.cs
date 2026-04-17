using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using SqlStressRunner.Commands;
using SqlStressRunner.Infrastructure;
using SqlStressRunner.Models;

namespace SqlStressRunner.ViewModels;

public class ResultsViewModel : ViewModelBase
{
    private MetricsSummary? _currentSummary;
    private ObservableCollection<IterationResult> _iterationResults = new();
    private ObservableCollection<StoredProcedureMetricSummary> _storedProcedureMetrics = new();

    public ResultsViewModel()
    {
        CopyRunIdCommand = new RelayCommand(CopyRunIdToClipboard, CanCopyRunId);
    }

    public RelayCommand CopyRunIdCommand { get; }

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
}
