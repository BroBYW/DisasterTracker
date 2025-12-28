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

        // User Input for the Trip ID / Nickname (Required for Q2)
        [ObservableProperty]
        string tripIdInput;

        // Used for displaying the ID of a selected historical pin
        [ObservableProperty]
        string incidentId;

        [ObservableProperty]
        bool isIdVisible;

        [ObservableProperty]
        string latitudeDisplay = "Drag Map to Select";

        [ObservableProperty]
        string longitudeDisplay = "...";

        // Required for Q1b - Displaying Connectivity
        [ObservableProperty]
        string connectivityStatus = "Checking...";

        [ObservableProperty]
        Color connectivityColor = Colors.Gray;

        public MainViewModel(DatabaseService dbService)
        {
            _dbService = dbService;
            IncidentHistory = new ObservableCollection<IncidentLog>();
            MapPins = new ObservableCollection<Pin>();

            IsIdVisible = false;

            // Load data and setup sensors
            Task.Run(LoadDataAsync);
            InitializeSensorsAsync();
        }

        public void ResetLocationDisplay()
        {
            LatitudeDisplay = "Drag Map to Select";
            LongitudeDisplay = "...";
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
            // Initial check
            UpdateConnectivity(Connectivity.Current.NetworkAccess);

            // Listener for real-time updates (Scenario implies real-time data)
            Connectivity.Current.ConnectivityChanged += (sender, e) =>
            {
                UpdateConnectivity(e.NetworkAccess);
            };

            return Task.CompletedTask;
        }

        private void UpdateConnectivity(NetworkAccess access)
        {
            if (access == NetworkAccess.Internet)
            {
                ConnectivityStatus = "ONLINE";
                ConnectivityColor = Colors.Green;
            }
            else
            {
                ConnectivityStatus = "OFFLINE";
                ConnectivityColor = Colors.Red;
            }
        }

        [RelayCommand]
        public async Task SaveLogAsync()
        {
            // 1. Validation Logic (Required for Q2 and Q1a Rubric)
            if (string.IsNullOrWhiteSpace(TripIdInput))
            {
                await Application.Current.MainPage.DisplayAlert("Validation Error", "Please enter a Trip ID or Nickname.", "OK");
                return;
            }

            if (TripIdInput.Length < 3 || !TripIdInput.All(char.IsLetterOrDigit))
            {
                await Application.Current.MainPage.DisplayAlert("Validation Error", "Trip ID must be at least 3 alphanumeric characters.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(SelectedDisasterType))
            {
                await Application.Current.MainPage.DisplayAlert("Validation Error", "Please select a Disaster Type.", "OK");
                return;
            }

            if (LatitudeDisplay.StartsWith("Drag"))
            {
                await Application.Current.MainPage.DisplayAlert("Location Error", "Please drag the map to pinpoint the location.", "OK");
                return;
            }

            // 2. Save Data
            var newLog = new IncidentLog
            {
                IncidentId = TripIdInput.ToUpper(), // User defined ID
                DisasterType = SelectedDisasterType,
                LocationCoordinates = $"{LatitudeDisplay},{LongitudeDisplay}",
                Timestamp = DateTime.Now,
                NetworkStatus = ConnectivityStatus
            };

            await _dbService.AddLogAsync(newLog);

            IncidentHistory.Insert(0, newLog);
            AddPinToMap(newLog);

            await Application.Current.MainPage.DisplayAlert("Success", "Incident Logged Successfully", "OK");

            // 3. Reset UI
            TripIdInput = string.Empty; // Clear input field
            SelectedDisasterType = null;
        }

        public async Task DeleteLogAsync(IncidentLog log)
        {
            if (log == null) return;
            await _dbService.DeleteLogAsync(log);
            IncidentHistory.Remove(log);
            var pinToRemove = MapPins.FirstOrDefault(p => p.Address == log.IncidentId);
            if (pinToRemove != null) MapPins.Remove(pinToRemove);
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