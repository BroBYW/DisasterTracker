using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Networking;
using FinalAssignment.Models;
using FinalAssignment.Services;
using System.Globalization;

namespace FinalAssignment.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;

        [ObservableProperty]
        ObservableCollection<IncidentLog> incidentHistory;

        [ObservableProperty]
        ObservableCollection<Pin> mapPins;

        public List<string> DisasterTypes { get; } = new List<string>
        {
            "Flood", "Fire", "Landslide", "Medical", "Earthquake", "Road Accident"
        };

        [ObservableProperty]
        string selectedDisasterType;

        [ObservableProperty]
        string incidentId;

        // NEW: Controls visibility of the ID in the top-right
        [ObservableProperty]
        bool isIdVisible;

        [ObservableProperty]
        string latitudeDisplay = "Drag Map to Select";

        [ObservableProperty]
        string longitudeDisplay = "...";

        [ObservableProperty]
        string connectivityStatus = "Checking...";

        public MainViewModel(DatabaseService dbService)
        {
            _dbService = dbService;
            IncidentHistory = new ObservableCollection<IncidentLog>();
            MapPins = new ObservableCollection<Pin>();

            // Don't generate ID here. Start hidden.
            IsIdVisible = false;

            Task.Run(LoadDataAsync);
            InitializeSensorsAsync();
        }

        // Helper to generate the ID string
        private string GenerateIdString()
        {
            return $"SOS-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
        }

        public void ResetLocationDisplay()
        {
            LatitudeDisplay = "Drag Map to Select";
            LongitudeDisplay = "...";

            // Hide the ID when resetting to input mode
            IncidentId = string.Empty;
            IsIdVisible = false;
        }

        public void UpdateSelectionFromMapDrag(Location location)
        {
            LatitudeDisplay = location.Latitude.ToString("F5", CultureInfo.InvariantCulture);
            LongitudeDisplay = location.Longitude.ToString("F5", CultureInfo.InvariantCulture);
        }

        [RelayCommand]
        public Task InitializeSensorsAsync()
        {
            var access = Connectivity.Current.NetworkAccess;
            if (access == NetworkAccess.Internet)
                ConnectivityStatus = "ONLINE";
            else
                ConnectivityStatus = "OFFLINE MODE";

            return Task.CompletedTask;
        }

        [RelayCommand]
        public async Task SaveLogAsync()
        {
            if (string.IsNullOrEmpty(SelectedDisasterType))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Select Type", "OK");
                return;
            }

            if (LatitudeDisplay.StartsWith("Drag"))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Drag map to location", "OK");
                return;
            }

            // Generate ID specifically for this Save
            string newId = GenerateIdString();

            var newLog = new IncidentLog
            {
                IncidentId = newId,
                DisasterType = SelectedDisasterType,
                LocationCoordinates = $"{LatitudeDisplay},{LongitudeDisplay}",
                Timestamp = DateTime.Now,
                NetworkStatus = ConnectivityStatus
            };

            await _dbService.AddLogAsync(newLog);

            IncidentHistory.Insert(0, newLog);
            AddPinToMap(newLog);

            await Application.Current.MainPage.DisplayAlert("Saved", "Incident Logged", "OK");

            // Reset UI for next input
            SelectedDisasterType = null;
            // Note: We do NOT show the ID after saving, we just go back to clean input state.
        }

        public async Task DeleteLogAsync(IncidentLog log)
        {
            if (log == null) return;

            await _dbService.DeleteLogAsync(log);
            IncidentHistory.Remove(log);

            var pinToRemove = MapPins.FirstOrDefault(p => p.Address == log.IncidentId);
            if (pinToRemove != null)
            {
                MapPins.Remove(pinToRemove);
            }
        }

        private async Task LoadDataAsync()
        {
            var logs = await _dbService.GetLogsAsync();
            foreach (var log in logs)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    IncidentHistory.Add(log);
                    AddPinToMap(log);
                });
            }
        }

        private void AddPinToMap(IncidentLog log)
        {
            if (string.IsNullOrEmpty(log.LocationCoordinates)) return;
            var parts = log.LocationCoordinates.Split(',');

            if (parts.Length >= 2 &&
                double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
            {
                var pin = new Pin
                {
                    Label = log.DisasterType,
                    Address = $"{log.IncidentId}",
                    Type = PinType.Place,
                    Location = new Location(lat, lon)
                };
                MapPins.Add(pin);
            }
        }
    }
}