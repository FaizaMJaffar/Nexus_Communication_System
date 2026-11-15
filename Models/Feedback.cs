using System.ComponentModel.DataAnnotations;

namespace NexusCommunicationSystem.Models;

// Feedback sent by a customer
public class Feedback
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; }

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(120)]
    public string Subject { get; set; }

    [Required(ErrorMessage = "Please write your message.")]
    [StringLength(1000, ErrorMessage = "Message must be under 1000 characters.")]
    public string Message { get; set; }

    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; } = 5;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
