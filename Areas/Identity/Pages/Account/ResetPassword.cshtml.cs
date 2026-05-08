using System.ComponentModel.DataAnnotations;
using System.Text;
using EventsApp.Data;
using EventsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;

namespace EventsApp.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;

        public ResetPasswordModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
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

            public string Code { get; set; } = string.Empty;

            public string ResetRequestId { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync(string? r = null, string? code = null, string? email = null)
        {
            if (!string.IsNullOrWhiteSpace(r))
            {
                var resetRequest = await GetValidResetRequestAsync(r);
                if (resetRequest == null)
                {
                    ModelState.AddModelError(string.Empty, "Линкът за смяна на парола е невалиден или изтекъл. Пусни нова заявка.");
                    return Page();
                }

                Input = new InputModel
                {
                    ResetRequestId = resetRequest.Id,
                    Email = resetRequest.Email,
                };

                return Page();
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                ModelState.AddModelError(string.Empty, "Линкът за смяна на парола е невалиден. Пусни нова заявка.");
                return Page();
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

            PasswordResetRequest? resetRequest = null;
            ApplicationUser? user;
            var encodedCode = Input.Code;

            if (!string.IsNullOrWhiteSpace(Input.ResetRequestId))
            {
                resetRequest = await GetValidResetRequestAsync(Input.ResetRequestId);
                if (resetRequest == null)
                {
                    ModelState.AddModelError(string.Empty, "Линкът за смяна на парола е невалиден или изтекъл. Пусни нова заявка.");
                    return Page();
                }

                user = await _userManager.FindByIdAsync(resetRequest.UserId);
                encodedCode = resetRequest.Code;
                Input.Email = resetRequest.Email;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(Input.Code))
                {
                    ModelState.AddModelError(string.Empty, "Линкът за смяна на парола е невалиден. Пусни нова заявка.");
                    return Page();
                }

                user = await _userManager.FindByEmailAsync(Input.Email.Trim());
            }

            if (user == null)
            {
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            string code;
            try
            {
                code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedCode));
            }
            catch (FormatException)
            {
                ModelState.AddModelError(string.Empty, "Невалиден линк за смяна на парола.");
                return Page();
            }

            var result = await _userManager.ResetPasswordAsync(user, code, Input.Password);
            if (result.Succeeded)
            {
                if (resetRequest != null)
                {
                    resetRequest.UsedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                }

                return RedirectToPage("./ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        private Task<PasswordResetRequest?> GetValidResetRequestAsync(string resetRequestId)
        {
            var now = DateTime.UtcNow;
            return _dbContext.PasswordResetRequests
                .AsTracking()
                .Where(r => r.Id == resetRequestId.Trim()
                    && r.UsedAt == null
                    && r.ExpiresAt > now)
                .FirstOrDefaultAsync();
        }
    }
}
