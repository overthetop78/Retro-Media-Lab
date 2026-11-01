using System.Collections.Specialized;
using RetroMediaLab.App.ViewModels.Operations;
using RetroMediaLab.App.Views.Controls.Read;
using RetroMediaLab.Tests.Application.TestInfrastructure;

namespace RetroMediaLab.Tests.Interface.ReadViews;

[Collection("WPF")]
public sealed class ReadCaptureHistoryTests(StaExecutionScenarios sta)
{
    private const string FirstPath = @"virtual\first.scp";
    private const string SecondPath = @"virtual\second.ssd";
    private const string Summary = "capture completed";

    [Fact]
    public Task HistoryPublishesCompletionArchivingAndRemoval() => sta.Run(() =>
    {
        var view = new ReadCompletionBanner();
        var changes = new List<NotifyCollectionChangedAction>();
        ((INotifyCollectionChanged)view.Captures).CollectionChanged += (_, args) => changes.Add(args.Action);
        view.BeginCapture(FirstPath);
        var first = Assert.Single(view.Captures);
        Assert.Equal(FirstPath, first.Path);
        Assert.Equal("first.scp", first.FileName);
        Assert.False(first.IsCompleted);
        Assert.False(first.IsArchived);
        var properties = new List<string?>();
        first.PropertyChanged += (_, args) => properties.Add(args.PropertyName);
        view.CompleteCapture(FirstPath, Summary);
        Assert.True(first.IsCompleted);
        Assert.Equal(Summary, first.Summary);
        Assert.Equal(Summary, view.SummaryTextBlock.Text);
        view.BeginCapture(SecondPath);
        Assert.True(first.IsArchived);
        Assert.Same(first, view.Captures[1]);
        Assert.Equal(SecondPath, view.Captures[0].Path);
        view.DiscardCapture(SecondPath);
        Assert.Same(first, Assert.Single(view.Captures));
        Assert.Equal(new[] { nameof(ReadCaptureViewModel.Summary), nameof(ReadCaptureViewModel.IsCompleted), nameof(ReadCaptureViewModel.IsArchived) }, properties);
        Assert.Equal(new[] { NotifyCollectionChangedAction.Add, NotifyCollectionChangedAction.Add, NotifyCollectionChangedAction.Remove }, changes);
    });

    [Fact]
    public Task NewPresentationUsesTheExistingConsultationEvents() => sta.Run(() =>
    {
        var view = new ReadCompletionBanner();
        var explored = new List<string>();
        var visualized = new List<string>();
        view.ExploreRequested += explored.Add;
        view.VisualizeRequested += visualized.Add;
        view.BeginCapture(FirstPath);
        var first = Assert.Single(view.Captures);
        view.RequestExplore(first);
        view.RequestVisualization(first);
        Assert.Empty(explored);
        Assert.Empty(visualized);
        view.CompleteCapture(FirstPath, Summary);
        view.RequestExplore(first);
        view.RequestVisualization(first);
        Assert.Equal(new[] { FirstPath }, explored);
        Assert.Equal(new[] { FirstPath }, visualized);
        var foreign = new ReadCaptureViewModel(FirstPath);
        foreign.Complete(Summary);
        view.RequestExplore(foreign);
        Assert.Single(explored);
        view.BeginCapture(SecondPath);
        view.RequestExplore(first);
        Assert.Equal(2, explored.Count);
    });

    [Fact]
    public Task DiscardedOrMismatchedCaptureCannotPublishSuccess() => sta.Run(() =>
    {
        var view = new ReadCompletionBanner();
        var requested = false;
        view.ExploreRequested += _ => requested = true;
        view.BeginCapture(FirstPath);
        var first = Assert.Single(view.Captures);
        view.CompleteCapture(SecondPath, Summary);
        view.DiscardCapture(SecondPath);
        Assert.False(first.IsCompleted);
        Assert.Same(first, Assert.Single(view.Captures));
        view.CompleteCapture(FirstPath, Summary);
        view.DiscardCapture(FirstPath);
        Assert.Empty(view.Captures);
        view.RequestExplore(first);
        Assert.False(requested);
        view.CompleteCapture(FirstPath, Summary);
        Assert.Empty(view.Captures);
    });
}
