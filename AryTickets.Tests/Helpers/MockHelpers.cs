using AryTickets.Hubs;
using AryTickets.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.SignalR;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AryTickets.Tests.Helpers
{
    public static class MockHelpers
    {
        public static Mock<UserManager<ApplicationUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var mgr = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
            mgr.Object.UserValidators.Add(new UserValidator<ApplicationUser>());
            mgr.Object.PasswordValidators.Add(new PasswordValidator<ApplicationUser>());
            return mgr;
        }

        public static Mock<SignInManager<ApplicationUser>> MockSignInManager(Mock<UserManager<ApplicationUser>> userManager = null)
        {
            userManager ??= MockUserManager();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            return new Mock<SignInManager<ApplicationUser>>(
                userManager.Object, contextAccessor.Object, claimsFactory.Object, null, null, null, null);
        }

        public static Mock<IHubContext<SeatHub>> MockSeatHub()
        {
            var mockHub = new Mock<IHubContext<SeatHub>>();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
            mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);
            mockHub.Setup(h => h.Clients).Returns(mockClients.Object);
            return mockHub;
        }

        public static Mock<RoleManager<IdentityRole>> MockRoleManager()
        {
            var store = new Mock<IRoleStore<IdentityRole>>();
            return new Mock<RoleManager<IdentityRole>>(store.Object, null, null, null, null);
        }

        public static ClaimsPrincipal CreateUserPrincipal(string userId, string userName = "TestUser", string role = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, userName)
            };

            if (role != null)
                claims.Add(new Claim(ClaimTypes.Role, role));

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        }

        public static void SetupControllerContext(Controller controller, string userId, string userName = "TestUser", string role = null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.User = CreateUserPrincipal(userId, userName, role);

            // Setup session
            var session = new Mock<ISession>();
            var sessionData = new Dictionary<string, byte[]>();
            session.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
                .Callback<string, byte[]>((key, value) => sessionData[key] = value);
            session.Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]>.IsAny))
                .Returns((string key, out byte[] value) =>
                {
                    var found = sessionData.TryGetValue(key, out value);
                    return found;
                });
            httpContext.Session = session.Object;

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Setup TempData
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            // Setup ObjectValidator for TryValidateModel
            var objectValidator = new Mock<IObjectModelValidator>();
            objectValidator.Setup(o => o.Validate(
                It.IsAny<ActionContext>(),
                It.IsAny<ValidationStateDictionary>(),
                It.IsAny<string>(),
                It.IsAny<object>()));
            controller.ObjectValidator = objectValidator.Object;
        }
    }
}
