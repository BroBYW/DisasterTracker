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

        // Collections
        [ObservableProperty]
        ObservableCollection<IncidentLog> incidentHistory;

        [ObservableProperty]
        ObservableCollection<Pin> mapPins;

        // Dropdown List
        public List<string> DisasterTypes { get; } = new List<string>
        {
            "Flood", "Fire", "Landslide", "Medical", "Earthquake", "Road Accident"
        };

        // Inputs
        [ObservableProperty]
        string selectedDisasterType;

        [ObservableProperty]
        string incidentId;

        // Display (Linked to Dragging)
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

            GenerateNewId();
            Task.Run(LoadDataAsync); // Load previous pins
            InitializeSensorsAsync();
        }

        private void GenerateNewId()
        {
            string randomSegment = Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
            IncidentId = $"SOS-{randomSegment}";
        }

        public void UpdateSelectionFromMapDrag(Location location)
        {
            LatitudeDisplay = location.Latitude.ToString("F5", CultureInfo.InvariantCulture);
            LongitudeDisplay = location.Longitude.ToString("F5", CultureInfo.InvariantCulture);
        }

        [RelayCommand]
        public async Task InitializeSensorsAsync()
        {
            var access = Connectivity.Current.NetworkAccess;
            ConnectivityStatus = access == NetworkAccess.Internet ? "ONLINE" : "OFFLINE";
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

            var newLog = new IncidentLog
            {
                IncidentId = IncidentId,
                DisasterType = SelectedDisasterType,
                LocationCoordinates = $"{LatitudeDisplay},{LongitudeDisplay}",
                Timestamp = DateTime.Now,
                NetworkStatus = ConnectivityStatus
            };

            await _dbService.AddLogAsync(newLog);

            // Update UI
            IncidentHistory.Insert(0, newLog);
            AddPinToMap(newLog);

            await Application.Current.MainPage.DisplayAlert("Saved", "Incident Logged", "OK");
            GenerateNewId();
            SelectedDisasterType = null;
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
                    Type = PinType.Place, // Standard Red Pin
                    Location = new Location(lat, lon)
                };
                MapPins.Add(pin);
            }
        }
    }
}