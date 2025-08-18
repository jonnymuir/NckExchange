using System.ComponentModel.DataAnnotations;

namespace NckExchange.Models;

public class ContactFormViewModel
{
    [Required(ErrorMessage = "Please enter your name.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your message.")]
    [MinLength(10, ErrorMessage = "Your message must be at least 10 characters long.")]
    public string Message { get; set; } = string.Empty;

    [Required(ErrorMessage = "reCAPTCHA verification failed. Please try again.")]
    public string RecaptchaToken { get; set; } = string.Empty; 
}