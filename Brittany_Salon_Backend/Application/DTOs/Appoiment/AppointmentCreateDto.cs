using Brittany_Salon_Backend.Application.DTOs.Appointment;
using System;
using System.Collections.Generic;

namespace Brittany_Salon_Backend.Application.DTOs.Appointment
{
    public class AppointmentCreateDto
    {
        public DateTime AppointmentDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? AppointmentStatus { get; set; }

        public int ClientId { get; set; }

        public List<AppointmentServiceCreateDto> Services { get; set; } = new();
        public List<AppointmentProductCreateDto> Products { get; set; } = new();

        public int? HairLengthOption { get; set; }
    }
}
