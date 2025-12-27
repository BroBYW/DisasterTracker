using FinalAssignment.ViewModels;
using FinalAssignment.Models;
using System.Globalization;
using Microsoft.Maui.Maps; // Required for MapSpan
using Map = Microsoft.Maui.Controls.Maps.Map;

namespace FinalAssignment.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        // 1. Initial Map Load
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // --- NEW FIX START: Handle Permissions & Refresh Map Button ---

            // 1. Check if we have permission
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            // 2. If not, ASK for it immediately
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            // 3. If granted, toggle the setting to FORCE the button to appear
            if (status == PermissionStatus.Granted)
            {
                // We flip it False -> True to wake up the map control
                DisasterMap.IsShowingUser = false;
                DisasterMap.IsShowingUser = true;
            }
            // --- NEW FIX END ---------------------------------------------


            // 4. Continue with your existing loading logic...
            await Task.Delay(500);

            if (BindingContext is MainViewModel vm)
            {
                await vm.InitializeSensorsAsync();
                try
                {
                    var location = await Geolocation.Default.GetLastKnownLocationAsync();
                    if (location != null)
                    {
                        DisasterMap.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromKilometers(1)));
                    }
                }
                catch { }
            }
        }

        // 2. Drag Logic
        private void OnMapPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Map.VisibleRegion))
            {
                if (DisasterMap.VisibleRegion != null)
                {
                    var center = DisasterMap.VisibleRegion.Center;
                    if (BindingContext is MainViewModel vm)
                    {
                        vm.UpdateSelectionFromMapDrag(center);
                    }
                }
            }
        }

        // 3. History Click Logic (Move Map + Show Details)
        // This runs when you tap the GREY BOX (Frame) directly
        private async void OnItemTapped(object sender, TappedEventArgs e)
        {
            // FIX: Cast 'sender' to Border instead of Frame
            var border = sender as Border;

            // Get the data from the Border's BindingContext
            var selectedLog = border?.BindingContext as IncidentLog;

            if (selectedLog == null) return;

            // ... (The rest of your logic remains exactly the same) ...

            var parts = selectedLog.LocationCoordinates.Split(',');

            if (parts.Length >= 2 &&
                double.TryParse(parts[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(parts[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
            {
                var targetLocation = new Location(lat, lon);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    DisasterMap.MoveToRegion(MapSpan.FromCenterAndRadius(targetLocation, Distance.FromKilometers(0.5)));
                });

                await DisplayAlert(
                    title: $"Details: {selectedLog.DisasterType}",
                    message: $"ID: {selectedLog.IncidentId}\n" +
                             $"Date: {selectedLog.Timestamp:g}\n" +
                             $"Location: {selectedLog.LocationCoordinates}",
                    cancel: "OK");
            }
        }
    }
}