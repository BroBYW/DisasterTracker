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
            // 1. Get the visual element (Frame) that was tapped
            var frame = sender as Frame;

            // 2. Extract the data (IncidentLog) attached to that Frame
            var selectedLog = frame?.BindingContext as IncidentLog;

            if (selectedLog == null) return;

            // --- DEBUG ALERT (You can remove this later if it works) ---
            // await DisplayAlert("Tap Worked!", $"Selected: {selectedLog.DisasterType}", "OK");
            // ---------------------------------------------------------

            // 3. Parse Coordinates
            var parts = selectedLog.LocationCoordinates.Split(',');

            if (parts.Length >= 2 &&
                double.TryParse(parts[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(parts[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
            {
                var targetLocation = new Location(lat, lon);

                // 4. Move Map
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    DisasterMap.MoveToRegion(MapSpan.FromCenterAndRadius(targetLocation, Distance.FromKilometers(0.5)));
                });

                // 5. Show Details
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