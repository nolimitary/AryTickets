using AryTickets.Models;
using AryTickets.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AryTickets.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Index", "Home");
                    }
                    if (result.IsNotAllowed)
                    {
                        HttpContext.Session.SetString("EmailForConfirmation", model.Email);
                        return RedirectToAction("ConfirmEmail");
                    }
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if email is already taken
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError(string.Empty, "An account with this email already exists.");
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    UserName = model.Username,
                    Email = model.Email,
                    EmailConfirmed = true  // Email verification removed — accounts are usable immediately.
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "User");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }


        [HttpGet]
        public IActionResult ConfirmEmail()
        {
            var email = HttpContext.Session.GetString("EmailForConfirmation");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }
            var model = new ConfirmEmailViewModel { Email = email };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmEmail(ConfirmEmailViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null && user.EmailVerificationCode == model.Code && user.VerificationCodeExpiry > DateTime.UtcNow)
                {
                    user.EmailConfirmed = true;
                    user.EmailVerificationCode = null;
                    user.VerificationCodeExpiry = null;
                    await _userManager.UpdateAsync(user);

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, "Invalid or expired verification code.");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendCode()
        {
            var email = HttpContext.Session.GetString("EmailForConfirmation");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || user.EmailConfirmed)
                return RedirectToAction("Login");

            var code = new Random().Next(1000, 9999).ToString();
            user.EmailVerificationCode = code;
            user.VerificationCodeExpiry = DateTime.UtcNow.AddMinutes(15);
            await _userManager.UpdateAsync(user);

            try
            {
                await SendVerificationCodeEmail(email, code);
                TempData["ResendSuccess"] = "A new verification code has been sent to your email.";
            }
            catch (Exception ex)
            {
                TempData["EmailError"] = $"Failed to send email: {ex.Message}";
            }

            return RedirectToAction("ConfirmEmail");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SkipVerification()
        {
            var email = HttpContext.Session.GetString("EmailForConfirmation");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return RedirectToAction("Login");

            user.EmailConfirmed = true;
            user.EmailVerificationCode = null;
            user.VerificationCodeExpiry = null;
            await _userManager.UpdateAsync(user);

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        private async Task SendVerificationCodeEmail(string email, string code)
        {
            var emailBody = $"<div style='font-family:Arial,sans-serif;background:#09090b;color:#e4e4e7;padding:40px;text-align:center;'>" +
                $"<div style='max-width:400px;margin:0 auto;background:#141416;border-radius:16px;padding:32px;border:1px solid rgba(255,255,255,0.06);'>" +
                $"<h1 style='font-size:24px;margin-bottom:4px;'><span style='color:#fff;'>Ary</span><span style='color:#e11d48;'>Tix</span></h1>" +
                $"<p style='color:#71717a;font-size:13px;margin-bottom:24px;'>Verify your email</p>" +
                $"<div style='background:#09090b;border-radius:12px;padding:20px;margin-bottom:20px;'>" +
                $"<p style='font-size:32px;font-weight:700;color:#fff;letter-spacing:0.3em;margin:0;'>{code}</p></div>" +
                $"<p style='color:#52525b;font-size:12px;'>This code expires in 15 minutes.</p></div></div>";
            await _emailSender.SendEmailAsync(email, "Verify your AryTix account", emailBody);
        }
    }
}