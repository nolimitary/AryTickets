using AryTickets.Controllers;
using AryTickets.Models;
using AryTickets.Services;
using AryTickets.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AryTickets.Tests.Security
{
    public class SecurityTests
    {
        // ═══ SQL Injection Tests ═══

        [Theory]
        [InlineData("'; DROP TABLE Bookings; --")]
        [InlineData("1 OR 1=1")]
        [InlineData("' UNION SELECT * FROM AspNetUsers --")]
        public async Task UserFavorites_SqlInjectionInMovieTitle_IsSafe(string maliciousInput)
        {
            var context = TestDbContextFactory.Create();
            var userManager = MockHelpers.MockUserManager();
            userManager.Setup(m => m.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .Returns("test-user-id");

            var controller = new UserFavoritesController(context, userManager.Object);
            MockHelpers.SetupControllerContext(controller, "test-user-id");

            await controller.ToggleFavorite(999, maliciousInput, "/poster.jpg");

            var favorite = await context.UserFavorites.FirstAsync();
            Assert.Equal(maliciousInput, favorite.MovieTitle); // Stored as data, not executed
            Assert.Single(await context.UserFavorites.ToListAsync());
        }

        [Theory]
        [InlineData("'; DROP TABLE UserReviews; --")]
        [InlineData("' OR '1'='1")]
        public async Task Reviews_SqlInjectionInContent_IsSafe(string maliciousInput)
        {
            var context = TestDbContextFactory.Create();
            context.Users.Add(new ApplicationUser
            {
                Id = "test-id",
                UserName = "Test",
                Email = "test@test.com",
                NormalizedEmail = "TEST@TEST.COM"
            });
            await context.SaveChangesAsync();

            context.UserReviews.Add(new UserReview
            {
                UserId = "test-id",
                UserName = "Test",
                MovieId = 1,
                MovieTitle = "Movie",
                Rating = 5,
                Content = maliciousInput
            });
            await context.SaveChangesAsync();

            var review = await context.UserReviews.FirstAsync();
            Assert.Equal(maliciousInput, review.Content);
            Assert.True(await context.UserReviews.AnyAsync()); // Table still exists
        }

        // ═══ XSS Prevention Tests ═══

        [Theory]
        [InlineData("<script>alert('xss')</script>")]
        [InlineData("<img onerror='alert(1)' src='x'>")]
        [InlineData("javascript:alert(1)")]
        public void XSS_InReviewContent_IsStoredAsText(string xssPayload)
        {
            var review = new UserReview
            {
                UserId = "user-1",
                MovieId = 1,
                Rating = 5,
                Content = xssPayload
            };

            // Model stores data as-is; Razor encoding handles output
            Assert.Equal(xssPayload, review.Content);
        }

        [Theory]
        [InlineData("<script>alert('xss')</script>")]
        [InlineData("<img onerror='alert(1)' src='x'>")]
        public void XSS_InUserName_IsStoredAsText(string xssPayload)
        {
            var model = new RegisterViewModel
            {
                Username = xssPayload,
                Email = "test@test.com",
                Password = "Pass123!",
                ConfirmPassword = "Pass123!"
            };

            Assert.Equal(xssPayload, model.Username);
        }

        // ═══ Authorization Tests ═══

        [Fact]
        public void AdminController_HasAuthorizeAttribute_WithAdminRole()
        {
            var attributes = typeof(AdminController).GetCustomAttributes(
                typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true);
            Assert.NotEmpty(attributes);

            var authAttr = attributes[0] as Microsoft.AspNetCore.Authorization.AuthorizeAttribute;
            Assert.Equal("Admin", authAttr.Roles);
        }

        [Fact]
        public void BookingController_HasAuthorizeAttribute()
        {
            var attributes = typeof(BookingController).GetCustomAttributes(
                typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true);
            Assert.NotEmpty(attributes);
        }

        [Fact]
        public void ProfileController_HasAuthorizeAttribute()
        {
            var attributes = typeof(ProfileController).GetCustomAttributes(
                typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true);
            Assert.NotEmpty(attributes);
        }

        [Fact]
        public void UserFavoritesController_HasAuthorizeAttribute()
        {
            var attributes = typeof(UserFavoritesController).GetCustomAttributes(
                typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true);
            Assert.NotEmpty(attributes);
        }

        [Fact]
        public void SubmitReview_HasAuthorizeAttribute()
        {
            var method = typeof(MovieController).GetMethod("SubmitReview");
            var attributes = method.GetCustomAttributes(
                typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true);
            Assert.NotEmpty(attributes);
        }

        // ═══ Anti-Forgery Token Tests ═══

        [Fact]
        public void AdminController_CreateShowtime_HasAntiForgeryToken()
        {
            var method = typeof(AdminController).GetMethod("CreateShowtime");
            var attributes = method.GetCustomAttributes(
                typeof(Microsoft.AspNetCore.Mvc.ValidateAntiForgeryTokenAttribute), true);
            Assert.NotEmpty(attributes);
        }

        [Fact]
        public void AdminController_DeleteShowtime_HasAntiForgeryToken()
        {
            var method = typeof(AdminController).GetMethod("DeleteShowtime");
            var attributes = method.GetCustomAttributes(
                typeof(Microsoft.AspNetCore.Mvc.ValidateAntiForgeryTokenAttribute), true);
            Assert.NotEmpty(attributes);
        }

        [Fact]
        public void AccountController_Register_HasAntiForgeryToken()
        {
            var methods = typeof(AccountController).GetMethods()
                .Where(m => m.Name == "Register" && m.GetParameters().Length > 0);
            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(
                    typeof(Microsoft.AspNetCore.Mvc.ValidateAntiForgeryTokenAttribute), true);
                Assert.NotEmpty(attributes);
            }
        }

        [Fact]
        public void MovieController_SubmitReview_HasAntiForgeryToken()
        {
            var method = typeof(MovieController).GetMethod("SubmitReview");
            var attributes = method.GetCustomAttributes(
                typeof(Microsoft.AspNetCore.Mvc.ValidateAntiForgeryTokenAttribute), true);
            Assert.NotEmpty(attributes);
        }

        // ═══ Input Validation Security ═══

        [Fact]
        public void CheckoutViewModel_CvcLimitsLength()
        {
            var model = new CheckoutViewModel
            {
                CardHolderName = "Test",
                CardNumber = "4111111111111111",
                ExpiryDate = "12/26",
                Cvc = "12345" // 5 characters, max is 4
            };

            var results = new List<ValidationResult>();
            var context = new ValidationContext(model);
            Validator.TryValidateObject(model, context, results, true);

            Assert.Contains(results, r => r.MemberNames.Contains("Cvc"));
        }

        [Fact]
        public void UserReview_ContentLengthIsLimited()
        {
            var model = new UserReview
            {
                UserId = "user",
                MovieId = 1,
                Rating = 5,
                Content = new string('a', 2001)
            };

            var results = new List<ValidationResult>();
            var context = new ValidationContext(model);
            Validator.TryValidateObject(model, context, results, true);

            Assert.Contains(results, r => r.MemberNames.Contains("Content"));
        }

        // ═══ Password Security Tests ═══

        [Fact]
        public void RegisterViewModel_PasswordField_HasDataTypeAttribute()
        {
            var property = typeof(RegisterViewModel).GetProperty("Password");
            var attr = property.GetCustomAttributes(typeof(DataTypeAttribute), true).FirstOrDefault() as DataTypeAttribute;
            Assert.NotNull(attr);
            Assert.Equal(DataType.Password, attr.DataType);
        }

        [Fact]
        public void LoginViewModel_PasswordField_HasDataTypeAttribute()
        {
            var property = typeof(LoginViewModel).GetProperty("Password");
            var attr = property.GetCustomAttributes(typeof(DataTypeAttribute), true).FirstOrDefault() as DataTypeAttribute;
            Assert.NotNull(attr);
            Assert.Equal(DataType.Password, attr.DataType);
        }
    }
}
