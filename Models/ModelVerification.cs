using System.ComponentModel.DataAnnotations;

public class ModelVerification
{
    [Required]
    [StringLength(25)]
    [EmailAddress]
    public string Email { get; set; }
    [Required]
    [StringLength(6)]
    public string Code { get; set; }
}

