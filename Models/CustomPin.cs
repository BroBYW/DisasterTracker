using Microsoft.Maui.Controls.Maps;

namespace FinalAssignment.Models
{
    // Inherit from the standard Pin so it works with the Map control
    public class CustomPin : Pin
    {
        public string ImageName { get; set; }
    }
}