using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using EventsApp.Data;
using EventsApp.Models;
using EventsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace EventsApp.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly IEmailSender _emailSender;
        private readonly IAppLinkService _appLinks;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            IEmailSender emailSender,
            IAppLinkService appLinks,
            ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _emailSender = emailSender;
            _appLinks = appLinks;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Имейл")]
            public string Email { get; set; } = string.Empty;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var email = Input.Email.Trim();
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var resetRequest = new PasswordResetRequest
            {
                Id = Guid.NewGuid().ToString("N"),
                UserId = user.Id,
                Email = email,
                Code = code,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(2),
            };

            _dbContext.PasswordResetRequests.Add(resetRequest);
            await _dbContext.SaveChangesAsync();

            var resetQuery = new Dictionary<string, string?>
            {
                ["r"] = resetRequest.Id,
            };
            var resetPath = QueryHelpers.AddQueryString("/reset-password", resetQuery);
            var resetUrl = _appLinks.ToAbsoluteUrl(Request, resetPath);
            if (!Uri.TryCreate(resetUrl, UriKind.Absolute, out var resetUri) ||
                string.IsNullOrWhiteSpace(resetUri.Host) ||
                (!string.Equals(resetUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(resetUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
            {
                resetUrl = QueryHelpers.AddQueryString("https://evento.business/reset-password", resetQuery);
            }
            var encodedUrl = HtmlEncoder.Default.Encode(resetUrl);

            try
            {
                await _emailSender.SendEmailAsync(
                    email,
                    "Смяна на парола - Evento",
                    $"""
                    <div style="margin:0;padding:0;background:#f2f5ff">
                        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background:#f2f5ff;border-collapse:collapse">
                            <tr>
                                <td align="center" style="padding:28px 14px">
                                    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="max-width:560px;background:#ffffff;border-collapse:collapse;border-radius:18px;overflow:hidden">
                                        <tr>
                                            <td style="background:#4f46e5;color:#ffffff;padding:28px 30px;font-family:Arial,sans-serif">
                                                <div style="font-size:12px;font-weight:800;letter-spacing:1px;text-transform:uppercase">Evento</div>
                                                <h1 style="margin:14px 0 0;font-size:28px;line-height:1.15;color:#ffffff">Смяна на парола</h1>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style="padding:28px 30px;font-family:Arial,sans-serif;color:#111827;font-size:15px;line-height:1.55">
                                                <p style="margin:0 0 18px">Получихме заявка за смяна на паролата в Evento. Линкът е валиден 2 часа.</p>
                                                <table role="presentation" cellspacing="0" cellpadding="0" border="0" style="border-collapse:collapse;margin:0 0 20px">
                                                    <tr>
                                                        <td bgcolor="#5b4bff" style="border-radius:12px">
                                                            <a href="{encodedUrl}" style="display:inline-block;padding:13px 20px;font-family:Arial,sans-serif;font-size:15px;font-weight:800;color:#ffffff;text-decoration:none;border-radius:12px">Смени паролата</a>
                                                        </td>
                                                    </tr>
                                                </table>
                                                <p style="margin:0 0 8px;color:#475569">Ако бутонът не се отваря, копирай този линк в браузъра:</p>
                                                <p style="margin:0 0 18px;word-break:break-all"><a href="{encodedUrl}" style="color:#4f46e5;text-decoration:underline">{encodedUrl}</a></p>
                                                <p style="margin:0;color:#475569">Ако не си заявил това, можеш спокойно да игнорираш този имейл.</p>
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                        </table>
                    </div>
                    """);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}.", email);
            }

            return RedirectToPage("./ForgotPasswordConfirmation");
        }
    }
}
