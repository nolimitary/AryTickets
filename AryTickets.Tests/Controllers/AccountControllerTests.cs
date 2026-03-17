using AryTickets.Controllers;
using AryTickets.Models;
using AryTickets.Services;
using AryTickets.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AryTickets.Tests.Controllers
{
    public class AccountControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManager;
        private readonly Mock<SignInManager<ApplicationUser>> _signInManager;
        private readonly Mock<IEmailSender> _emailSender;

        public AccountControllerTests()
        {
            _userManager = MockHelpers.MockUserManager();
            _signInManager = MockHelpers.MockSignInManager(_userManager);
            _emailSender = new Mock<IEmailSender>();
        }

        private AccountController CreateController()
        {
            var controller = new AccountController(_userManager.Object, _signInManager.Object, _emailSender.Object);
            MockHelpers.SetupControllerContext(controller, "test-user-id");
            return controller;
        }

        // ═══ Login Tests ═══

        [Fact]
        public void Login_Get_ReturnsView()
        {
            var controller = CreateController();
            var result = controller.Login();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Login_ValidCredentials_RedirectsToHome()
        {
            var user = new ApplicationUser { Id = "id", UserName = "Test", Email = "test@test.com" };
            _userManager.Setup(m => m.FindByEmailAsync("test@test.com")).ReturnsAsync(user);
            _signInManager.Setup(m => m.PasswordSignInAsync(user, "Pass123!", false, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            var controller = CreateController();
            var model = new LoginViewModel { Email = "test@test.com", Password = "Pass123!" };

            var result = await controller.Login(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Home", redirect.ControllerName);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsViewWithError()
        {
            _userManager.Setup(m => m.FindByEmailAsync("test@test.com"))
                .ReturnsAsync((ApplicationUser)null);

            var controller = CreateController();
            var model = new LoginViewModel { Email = "test@test.com", Password = "Wrong!" };

            var result = await controller.Login(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Login_EmailNotConfirmed_RedirectsToConfirmEmail()
        {
            var user = new ApplicationUser { Id = "id", Email = "test@test.com" };
            _userManager.Setup(m => m.FindByEmailAsync("test@test.com")).ReturnsAsync(user);
            _signInManager.Setup(m => m.PasswordSignInAsync(user, "Pass123!", false, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.NotAllowed);

            var controller = CreateController();
            var model = new LoginViewModel { Email = "test@test.com", Password = "Pass123!" };

            var result = await controller.Login(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ConfirmEmail", redirect.ActionName);
        }

        [Fact]
        public async Task Login_InvalidModelState_ReturnsView()
        {
            var controller = CreateController();
            controller.ModelState.AddModelError("Email", "Required");

            var result = await controller.Login(new LoginViewModel());

            Assert.IsType<ViewResult>(result);
        }

        // ═══ Register Tests ═══

        [Fact]
        public void Register_Get_ReturnsView()
        {
            var controller = CreateController();
            var result = controller.Register();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Register_ValidData_CreatesUserAndRedirects()
        {
            _userManager.Setup(m => m.FindByEmailAsync("new@test.com"))
                .ReturnsAsync((ApplicationUser)null);
            _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), "Pass123!"))
                .ReturnsAsync(IdentityResult.Success);

            var controller = CreateController();
            var model = new RegisterViewModel
            {
                Username = "NewUser",
                Email = "new@test.com",
                Password = "Pass123!",
                ConfirmPassword = "Pass123!"
            };

            var result = await controller.Register(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ConfirmEmail", redirect.ActionName);
        }

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsViewWithError()
        {
            _userManager.Setup(m => m.FindByEmailAsync("existing@test.com"))
                .ReturnsAsync(new ApplicationUser { Email = "existing@test.com" });

            var controller = CreateController();
            var model = new RegisterViewModel
            {
                Username = "NewUser",
                Email = "existing@test.com",
                Password = "Pass123!",
                ConfirmPassword = "Pass123!"
            };

            var result = await controller.Register(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Register_FailedCreation_ReturnsViewWithErrors()
        {
            _userManager.Setup(m => m.FindByEmailAsync("new@test.com"))
                .ReturnsAsync((ApplicationUser)null);
            _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));

            var controller = CreateController();
            var model = new RegisterViewModel
            {
                Username = "NewUser",
                Email = "new@test.com",
                Password = "weak",
                ConfirmPassword = "weak"
            };

            var result = await controller.Register(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Register_EmailSendFails_StillRedirects()
        {
            _userManager.Setup(m => m.FindByEmailAsync("new@test.com"))
                .ReturnsAsync((ApplicationUser)null);
            _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), "Pass123!"))
                .ReturnsAsync(IdentityResult.Success);
            _emailSender.Setup(m => m.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Email service down"));

            var controller = CreateController();
            var model = new RegisterViewModel
            {
                Username = "NewUser",
                Email = "new@test.com",
                Password = "Pass123!",
                ConfirmPassword = "Pass123!"
            };

            var result = await controller.Register(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ConfirmEmail", redirect.ActionName);
            Assert.NotNull(controller.TempData["EmailError"]);
        }

        // ═══ ConfirmEmail Tests ═══

        [Fact]
        public void ConfirmEmail_Get_NoSession_RedirectsToLogin()
        {
            var controller = CreateController();

            var result = controller.ConfirmEmail();

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
        }

        [Fact]
        public async Task ConfirmEmail_ValidCode_ConfirmsAndSignsIn()
        {
            var user = new ApplicationUser
            {
                Id = "id",
                Email = "test@test.com",
                EmailVerificationCode = "1234",
                VerificationCodeExpiry = DateTime.UtcNow.AddMinutes(10)
            };
            _userManager.Setup(m => m.FindByEmailAsync("test@test.com")).ReturnsAsync(user);
            _userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var controller = CreateController();
            var model = new ConfirmEmailViewModel { Email = "test@test.com", Code = "1234" };

            var result = await controller.ConfirmEmail(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.True(user.EmailConfirmed);
            Assert.Null(user.EmailVerificationCode);
        }

        [Fact]
        public async Task ConfirmEmail_ExpiredCode_ReturnsError()
        {
            var user = new ApplicationUser
            {
                Id = "id",
                Email = "test@test.com",
                EmailVerificationCode = "1234",
                VerificationCodeExpiry = DateTime.UtcNow.AddMinutes(-5)
            };
            _userManager.Setup(m => m.FindByEmailAsync("test@test.com")).ReturnsAsync(user);

            var controller = CreateController();
            var model = new ConfirmEmailViewModel { Email = "test@test.com", Code = "1234" };

            var result = await controller.ConfirmEmail(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task ConfirmEmail_WrongCode_ReturnsError()
        {
            var user = new ApplicationUser
            {
                Id = "id",
                Email = "test@test.com",
                EmailVerificationCode = "1234",
                VerificationCodeExpiry = DateTime.UtcNow.AddMinutes(10)
            };
            _userManager.Setup(m => m.FindByEmailAsync("test@test.com")).ReturnsAsync(user);

            var controller = CreateController();
            var model = new ConfirmEmailViewModel { Email = "test@test.com", Code = "9999" };

            var result = await controller.ConfirmEmail(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
        }

        // ═══ Logout Tests ═══

        [Fact]
        public async Task Logout_RedirectsToHome()
        {
            var controller = CreateController();

            var result = await controller.Logout();

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Home", redirect.ControllerName);
            _signInManager.Verify(m => m.SignOutAsync(), Times.Once);
        }
    }
}
