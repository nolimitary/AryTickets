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
                var user = new ApplicationUser { UserName = model.Username, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    var code = new Random().Next(1000, 9999).ToString("D4");
                    user.EmailVerificationCode = code;
                    user.VerificationCodeExpiry = DateTime.UtcNow.AddMinutes(10);
                    await _userManager.UpdateAsync(user);

                    await _emailSender.SendEmailAsync(model.Email, "Confirm your account",
                        $"<h1>Welcome to AryTix!</h1><p>Your verification code is: <strong>{code}</strong></p>");

                    HttpContext.Session.SetString("EmailForConfirmation", model.Email);

                    return RedirectToAction("ConfirmEmail");
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
    }
}