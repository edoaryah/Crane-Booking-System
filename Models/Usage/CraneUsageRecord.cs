// using System.ComponentModel.DataAnnotations;
// using System.ComponentModel.DataAnnotations.Schema;

// namespace AspnetCoreMvcFull.Models
// {
//   public class CraneUsageRecord
//   {
//     [Key]
//     public int Id { get; set; }

//     [Required]
//     public int BookingId { get; set; }

//     [Required]
//     public DateTime Date { get; set; }

//     [Required]
//     public UsageCategory Category { get; set; }

//     [Required]
//     public int SubcategoryId { get; set; }

//     // New fields for time tracking
//     [Required]
//     public TimeSpan StartTime { get; set; }

//     [Required]
//     public TimeSpan EndTime { get; set; }

//     // Duration will be calculated from StartTime and EndTime
//     [NotMapped]
//     public TimeSpan Duration => CalculateDuration();

//     [Required]
//     public DateTime CreatedAt { get; set; } = DateTime.Now;

//     [StringLength(100)]
//     public required string CreatedBy { get; set; } = string.Empty;

//     public DateTime? UpdatedAt { get; set; }

//     [StringLength(100)]
//     public string? UpdatedBy { get; set; }

//     // Navigation properties
//     public virtual Booking? Booking { get; set; }

//     // Calculate duration based on start time and end time
//     private TimeSpan CalculateDuration()
//     {
//       // If end time is less than start time, it means the activity spans across midnight
//       if (EndTime < StartTime)
//       {
//         // Add 24 hours to end time and calculate duration
//         return (EndTime.Add(new TimeSpan(24, 0, 0))) - StartTime;
//       }
//       else
//       {
//         return EndTime - StartTime;
//       }
//     }
//   }
// }
