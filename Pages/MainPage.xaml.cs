using FinalAssignment.ViewModels;
using FinalAssignment.Models;
using System.Globalization;
using Microsoft.Maui.Maps;
using Map = Microsoft.Maui.Controls.Maps.Map;

#if ANDROID
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
#endif

namespace FinalAssignment.Pages
{
    public partial class MainPage : ContentPage
    {
        // DRAWER SETTINGS
        private bool _isDrawerOpen = false;
        // Height 600 - Visible 60 = 540 Hidden
        private const double DrawerHiddenY = 540;
        private const double DrawerVisibleY = 0;
        private double _startDragY;

        private IncidentLog _selectedLog;

        public MainPage(MainViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;

            DisasterMap.HandlerChanged += (s, e) =>
            {
#if ANDROID
                if (DisasterMap.Handler?.PlatformView is MapView mapView)
                {
                    mapView.GetMapAsync(new MapCallbackHandler());
                }
#endif
            };
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status == PermissionStatus.Granted)
            {
                DisasterMap.IsShowingUser = false;
                DisasterMap.IsShowingUser = true;
            }

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

        private void OnMapPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Map.VisibleRegion) && DisasterMap.VisibleRegion != null)
            {
                var center = DisasterMap.VisibleRegion.Center;
                if (BindingContext is MainViewModel vm)
                    vm.UpdateSelectionFromMapDrag(center);
            }
        }

        private void OnZoomIn(object sender, EventArgs e)
        {
            if (DisasterMap.VisibleRegion != null)
            {
                var lat = DisasterMap.VisibleRegion.LatitudeDegrees / 2;
                var lon = DisasterMap.VisibleRegion.LongitudeDegrees / 2;
                DisasterMap.MoveToRegion(new MapSpan(DisasterMap.VisibleRegion.Center, lat, lon));
            }
        }

        private void OnZoomOut(object sender, EventArgs e)
        {
            if (DisasterMap.VisibleRegion != null)
            {
                var lat = DisasterMap.VisibleRegion.LatitudeDegrees * 2;
                var lon = DisasterMap.VisibleRegion.LongitudeDegrees * 2;
                if (lat > 180) lat = 180;
                if (lon > 360) lon = 360;
                DisasterMap.MoveToRegion(new MapSpan(DisasterMap.VisibleRegion.Center, lat, lon));
            }
        }

        // --- DRAWER LOGIC ---

        private async void OnDrawerToggle(object sender, EventArgs e)
        {
            if (_isDrawerOpen)
            {
                await HistoryDrawer.TranslateTo(0, DrawerHiddenY, 250, Easing.CubicOut);
                _isDrawerOpen = false;
            }
            else
            {
                await HistoryDrawer.TranslateTo(0, DrawerVisibleY, 250, Easing.CubicOut);
                _isDrawerOpen = true;
            }
        }

        private async void OnDrawerPan(object sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    _startDragY = HistoryDrawer.TranslationY;
                    break;
                case GestureStatus.Running:
                    double newY = _startDragY + e.TotalY;
                    if (newY < DrawerVisibleY) newY = DrawerVisibleY;
                    if (newY > DrawerHiddenY) newY = DrawerHiddenY;
                    HistoryDrawer.TranslationY = newY;
                    break;
                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                    if (HistoryDrawer.TranslationY < (DrawerHiddenY / 2))
                    {
                        await HistoryDrawer.TranslateTo(0, DrawerVisibleY, 200, Easing.CubicOut);
                        _isDrawerOpen = true;
                    }
                    else
                    {
                        await HistoryDrawer.TranslateTo(0, DrawerHiddenY, 200, Easing.CubicOut);
                        _isDrawerOpen = false;
                    }
                    break;
            }
        }

        // --- ITEM SELECTION ---

        private void OnItemTapped(object sender, TappedEventArgs e)
        {
            var border = sender as Border;
            var log = border?.BindingContext as IncidentLog;
            if (log == null) return;

            _selectedLog = log;
            var parts = log.LocationCoordinates.Split(',');
            if (parts.Length >= 2 &&
                double.TryParse(parts[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(parts[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
            {
                var targetLocation = new Location(lat, lon);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    DisasterMap.MoveToRegion(MapSpan.FromCenterAndRadius(targetLocation, Distance.FromKilometers(0.5)));
                });
            }

            SaveSection.IsVisible = false;
            HistoryDrawer.IsVisible = false;

            DetailTypeLabel.Text = log.DisasterType;
            DetailTimeLabel.Text = log.Timestamp.ToString("g");
            DetailLocationLabel.Text = log.LocationCoordinates;
            DetailPanel.IsVisible = true;

            if (BindingContext is MainViewModel vm)
            {
                vm.IncidentId = log.IncidentId;
                vm.IsIdVisible = true;
                if (parts.Length >= 2)
                {
                    vm.LatitudeDisplay = parts[0].Trim();
                    vm.LongitudeDisplay = parts[1].Trim();
                }
            }
        }

        private void OnCloseDetail(object sender, EventArgs e)
        {
            DetailPanel.IsVisible = false;
            SaveSection.IsVisible = true;
            HistoryDrawer.IsVisible = true;
            _selectedLog = null;
            if (BindingContext is MainViewModel vm) vm.ResetLocationDisplay();
        }

        private async void OnDeleteLog(object sender, EventArgs e)
        {
            if (_selectedLog == null) return;
            bool confirm = await DisplayAlert("Delete Log", "Are you sure you want to delete this incident?", "Delete", "Cancel");
            if (confirm)
            {
                if (BindingContext is MainViewModel vm) await vm.DeleteLogAsync(_selectedLog);
                OnCloseDetail(sender, e);
            }
        }
    }

#if ANDROID
    public class MapCallbackHandler : Java.Lang.Object, IOnMapReadyCallback
    {
        public void OnMapReady(GoogleMap googleMap)
        {
            googleMap.UiSettings.ZoomControlsEnabled = false;
        }
    }
#endif
}