using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Meetups.Models;
using LoopMeet.App.Services;

namespace LoopMeet.App.Features.Meetups.ViewModels;

public sealed partial class CreateMeetupViewModel : ObservableObject
{
    private readonly MeetupsApi _meetupsApi;
    private readonly PlacesApi _placesApi;
    private CancellationTokenSource? _searchCts;

    public ObservableCollection<PlacePrediction> Predictions { get; } = new();

    [ObservableProperty]
    private string _locationSearchText = string.Empty;

    [ObservableProperty]
    private bool _showPredictions;

    [ObservableProperty]
    private string _locationDisplay = "TBD";

    [ObservableProperty]
    private bool _hasSelectedLocation;

    [ObservableProperty]
    private bool _isLocationSearchActive;

    partial void OnIsLocationSearchActiveChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowFormFields));
    }

    public bool ShowFormFields => !IsLocationSearchActive;

    [ObservableProperty]
    private Guid _groupId;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private DateTime _scheduledDate = DateTime.Today.AddDays(1);

    public DateTime MinimumDate => DateTime.Today;

    [ObservableProperty]
    private TimeSpan _scheduledTime = new(18, 0, 0);

    [ObservableProperty]
    private string? _placeName;

    [ObservableProperty]
    private string? _placeAddress;

    [ObservableProperty]
    private double? _latitude;

    [ObservableProperty]
    private double? _longitude;

    [ObservableProperty]
    private string? _placeId;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    public CreateMeetupViewModel(MeetupsApi meetupsApi, PlacesApi placesApi)
    {
        _meetupsApi = meetupsApi;
        _placesApi = placesApi;
    }

    partial void OnErrorMessageChanged(string value)
    {
        HasError = !string.IsNullOrWhiteSpace(value);
    }

    partial void OnLocationSearchTextChanged(string value)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        IsLocationSearchActive = !string.IsNullOrWhiteSpace(value) && value.Length >= 2;

        if (!IsLocationSearchActive)
        {
            Predictions.Clear();
            ShowPredictions = false;
            return;
        }
        _ = SearchPlacesAsync(value, token);
    }

    private async Task SearchPlacesAsync(string query, CancellationToken token)
    {
        try
        {
            await Task.Delay(300, token);
            if (token.IsCancellationRequested) return;
            var result = await _placesApi.AutocompleteAsync(query);
            if (token.IsCancellationRequested) return;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Predictions.Clear();
                foreach (var p in result.Predictions) Predictions.Add(p);
                ShowPredictions = Predictions.Count > 0;
            });
        }
        catch (TaskCanceledException) { }
        catch
        {
            MainThread.BeginInvokeOnMainThread(() => ShowPredictions = false);
        }
    }

    [RelayCommand]
    private async Task SelectPredictionAsync(PlacePrediction? prediction)
    {
        if (prediction is null) return;
        try
        {
            var detail = await _placesApi.GetPlaceDetailAsync(prediction.PlaceId);
            PlaceName = detail.Name;
            PlaceAddress = detail.FormattedAddress;
            Latitude = detail.Latitude;
            Longitude = detail.Longitude;
            PlaceId = prediction.PlaceId;
            LocationDisplay = detail.Name;
            HasSelectedLocation = true;
            LocationSearchText = string.Empty;
            Predictions.Clear();
            ShowPredictions = false;
            IsLocationSearchActive = false;
        }
        catch { }
    }

    [RelayCommand]
    private void ClearLocation()
    {
        PlaceName = null;
        PlaceAddress = null;
        Latitude = null;
        Longitude = null;
        PlaceId = null;
        LocationDisplay = "TBD";
        HasSelectedLocation = false;
        LocationSearchText = string.Empty;
        Predictions.Clear();
        ShowPredictions = false;
        IsLocationSearchActive = false;
    }

    public void ApplyGroupId(Guid groupId)
    {
        GroupId = groupId;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy) return;

        ErrorMessage = string.Empty;
        var trimmedTitle = Title.Trim();
        if (string.IsNullOrWhiteSpace(trimmedTitle))
        {
            ErrorMessage = "Please provide a meetup title.";
            return;
        }

        var localDateTime = ScheduledDate.Date + ScheduledTime;
        var scheduledAt = new DateTimeOffset(localDateTime, TimeZoneInfo.Local.GetUtcOffset(localDateTime));
        if (scheduledAt <= DateTimeOffset.UtcNow)
        {
            ErrorMessage = "Scheduled time must be in the future.";
            return;
        }

        IsBusy = true;
        try
        {
            await _meetupsApi.CreateMeetupAsync(GroupId, new CreateMeetupRequest
            {
                Title = trimmedTitle,
                ScheduledAt = scheduledAt,
                PlaceName = PlaceName,
                PlaceAddress = PlaceAddress,
                Latitude = Latitude,
                Longitude = Longitude,
                PlaceId = PlaceId
            });
            await Shell.Current.GoToAsync("..");
        }
        catch
        {
            ErrorMessage = "Could not create the meetup. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
