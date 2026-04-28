namespace LoopMeet.App.Features.Meetups.ViewModels;

public sealed class MeetupLocationLookupBehavior
{
    private bool _hasPromptedPermission;

    public async Task<LocationLookupContext> GetLookupContextAsync(CancellationToken cancellationToken)
    {
        try
        {
            var permissionStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (permissionStatus != PermissionStatus.Granted)
            {
                if (!_hasPromptedPermission)
                {
                    _hasPromptedPermission = true;
                    permissionStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (permissionStatus != PermissionStatus.Granted)
                {
                    return LocationLookupContext.WithoutBias("Location permission denied. Showing broader results.");
                }
            }

            var location = await Geolocation.Default.GetLastKnownLocationAsync();
            if (location is null)
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5));
                location = await Geolocation.Default.GetLocationAsync(request, cancellationToken);
            }

            if (location is null)
            {
                return LocationLookupContext.WithoutBias("Current location unavailable. Showing broader results.");
            }

            return LocationLookupContext.WithBias(location.Latitude, location.Longitude, 50_000);
        }
        catch (PermissionException)
        {
            return LocationLookupContext.WithoutBias("Location permission denied. Showing broader results.");
        }
        catch
        {
            return LocationLookupContext.WithoutBias("Current location unavailable. Showing broader results.");
        }
    }
}

public sealed record LocationLookupContext(double? Latitude, double? Longitude, int? RadiusMeters, bool IsBiasEnabled, string FallbackMessage)
{
    public static LocationLookupContext WithBias(double latitude, double longitude, int radiusMeters)
        => new(latitude, longitude, radiusMeters, true, string.Empty);

    public static LocationLookupContext WithoutBias(string message)
        => new(null, null, null, false, message);
}
