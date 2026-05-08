using System.ComponentModel.DataAnnotations;
using System.Text;
using EventsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace EventsApp.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ResetPasswordModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Имейл")]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "{0} трябва да е поне {2} и максимум {1} символа.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Нова парола")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Потвърди паролата")]
            [Compare("Password", ErrorMessage = "Паролите не съвпадат.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Required]
            public string Code { get; set; } = string.Empty;
        }

        public IActionResult OnGet(string? code = null, string? email = null)
        {
            if (code == null)
            {
                return BadRequest("Missing password reset code.");
            }

            Input = new InputModel
            {
                Code = code,
                Email = email ?? string.Empty,
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email.Trim());
            if (user == null)
            {
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            string code;
            try
            {
                code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Input.Code));
            }
            catch (FormatException)
            {
                ModelState.AddModelError(string.Empty, "Невалиден линк за смяна на парола.");
                return Page();
            }

            var result = await _userManager.ResetPasswordAsync(user, code, Input.Password);
            if (result.Succeeded)
            {
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }
    }
}
