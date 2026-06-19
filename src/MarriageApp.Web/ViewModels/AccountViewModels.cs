using System.ComponentModel.DataAnnotations;
using MarriageApp.Core.Enums;

namespace MarriageApp.Web.ViewModels;

public class RegisterViewModel
{
    /// <summary>Chosen on the landing page; decides which profile form the user fills next.</summary>
    [Required]
    public Gender Gender { get; set; }

    [Required(ErrorMessage = "الاسم مطلوب"), Display(Name = "الاسم بالكامل")]
    public string FullName { get; set; } = default!;

    [Required(ErrorMessage = "البريد الإلكتروني مطلوب"), EmailAddress(ErrorMessage = "بريد إلكتروني غير صالح")]
    [Display(Name = "البريد الإلكتروني")]
    public string Email { get; set; } = default!;

    [Required, DataType(DataType.Password), Display(Name = "كلمة المرور")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "كلمة المرور يجب ألا تقل عن 8 أحرف")]
    public string Password { get; set; } = default!;

    [DataType(DataType.Password), Display(Name = "تأكيد كلمة المرور")]
    [Compare(nameof(Password), ErrorMessage = "كلمتا المرور غير متطابقتين")]
    public string ConfirmPassword { get; set; } = default!;
}

public class LoginViewModel
{
    [Required, EmailAddress, Display(Name = "البريد الإلكتروني")]
    public string Email { get; set; } = default!;

    [Required, DataType(DataType.Password), Display(Name = "كلمة المرور")]
    public string Password { get; set; } = default!;

    [Display(Name = "تذكرني")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
