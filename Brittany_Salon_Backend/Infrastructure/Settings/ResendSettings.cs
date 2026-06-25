namespace Brittany_Salon_Backend.Infrastructure.Settings
{
    public class ResendSettings
    {
        public const string SectionName = "ResendSettings";

        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = "Brittany Salon";
    }
}
