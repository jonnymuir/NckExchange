using System.ComponentModel.DataAnnotations;

namespace NckExchange.Models;

public class AdminReplyViewModel
{
    [Required] // Hidden field, but essential
    public int Id { get; set; }

    public string OriginalSenderName { get; set; } = string.Empty;
    
    [Required] // Hidden field, but essential for sending email
    public string OriginalSenderEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Original message content is required.")]
    public string OriginalMessage { get; set; } = string.Empty;

    [Required(ErrorMessage = "Your answer is required.")]
    [MinLength(10, ErrorMessage = "Your answer must be at least 10 characters long.")]
    public string Answer { get; set; } = string.Empty;
}