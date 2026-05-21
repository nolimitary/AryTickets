using AryTickets.Controllers;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace AryTickets.Tests.Security
{
    // Verifies that controllers we expect to be protected actually carry the right
    // authorization attributes — a small but high-value defensive check that catches
    // regressions where someone accidentally removes an [Authorize] decoration.
    public class AuthorizationAttributeTests
    {
        [Fact]
        public void AdminController_RequiresAdminRole()
        {
            var attr = typeof(AdminController).GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(attr);
            Assert.Equal("Admin", attr!.Roles);
        }

        [Fact]
        public void BookingController_RequiresAuthentication()
        {
            var attr = typeof(BookingController).GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(attr);
        }

        [Fact]
        public void UserFavoritesController_RequiresAuthentication()
        {
            var attr = typeof(UserFavoritesController).GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(attr);
        }

        [Fact]
        public void ProfileController_RequiresAuthentication()
        {
            var attr = typeof(ProfileController).GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(attr);
        }

        [Fact]
        public void HomeController_AllowsAnonymous()
        {
            // Home is the landing page; it must not require authentication.
            var attr = typeof(HomeController).GetCustomAttribute<AuthorizeAttribute>();
            Assert.Null(attr);
        }

        [Fact]
        public void AccountController_AllowsAnonymous()
        {
            // Account hosts login/register/confirm — must remain anonymous-accessible.
            var attr = typeof(AccountController).GetCustomAttribute<AuthorizeAttribute>();
            Assert.Null(attr);
        }

        [Fact]
        public void AllMutatingActions_RequireAntiForgeryToken()
        {
            // Every public POST action in our controllers must be CSRF-protected.
            var controllers = typeof(BookingController).Assembly
                .GetTypes()
                .Where(t => t.Namespace == "AryTickets.Controllers" && typeof(Microsoft.AspNetCore.Mvc.Controller).IsAssignableFrom(t));

            foreach (var controller in controllers)
            {
                var postActions = controller
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => m.GetCustomAttribute<Microsoft.AspNetCore.Mvc.HttpPostAttribute>() != null);

                foreach (var action in postActions)
                {
                    var hasToken = action.GetCustomAttribute<Microsoft.AspNetCore.Mvc.ValidateAntiForgeryTokenAttribute>() != null;
                    Assert.True(hasToken,
                        $"{controller.Name}.{action.Name} is a POST action and must carry [ValidateAntiForgeryToken].");
                }
            }
        }
    }
}
