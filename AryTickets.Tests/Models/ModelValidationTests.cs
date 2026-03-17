using AryTickets.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Xunit;

namespace AryTickets.Tests.Models
{
    public class ModelValidationTests
    {
        private IList<ValidationResult> ValidateModel(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model);
            Validator.TryValidateObject(model, context, results, true);
            return results;
        }

        // ═══ RegisterViewModel Tests ═══

        [Fact]
        public void RegisterViewModel_ValidData_PassesValidation()
        {
            var model = new RegisterViewModel
            {
                Username = "TestUser",
                Email = "test@test.com",
                Password = "Test123!",
                ConfirmPassword = "Test123!"
            };

            var results = ValidateModel(model);
            Assert.Empty(results);
        }

        [Fact]
        public void RegisterViewModel_MissingUsername_FailsValidation()
        {
            var model = new RegisterViewModel
            {
                Email = "test@test.com",
                Password = "Test123!",
                ConfirmPassword = "Test123!"
            };

            var results = ValidateModel(model);
            Assert.Contains(results, r => r.MemberNames.Contains("Username"));
        }

        [Fact]
        public void RegisterViewModel_MissingEmail_FailsValidation()
        {
            var model = new RegisterViewModel
            {
                Username = "TestUser",
                Password = "Test123!",
                ConfirmPassword = "Test123!"
            };

            var results = ValidateModel(model);
            Assert.Contains(results, r => r.MemberNames.Contains("Email"));
        }

        [Fact]
        public void RegisterViewModel_InvalidEmail_FailsValidation()
        {
            var model = new RegisterViewModel
            {
                Username = "TestUser",
                Email = "not-an-email",
                Password = "Test123!",
                ConfirmPassword = "Test123!"
            };

            var results = ValidateModel(model);
            Assert.Contains(results, r => r.MemberNames.Contains("Email"));
        }

        [Fact]
        public void RegisterViewModel_PasswordMismatch_FailsValidation()
        {
            var model = new RegisterViewModel
            {
                Username = "TestUser",
                Email = "test@test.com",
                Password = "Test123!",
                ConfirmPassword = "Different123!"
            };

            var results = ValidateModel(model);
            Assert.Contains(results, r => r.MemberNames.Contains("ConfirmPassword"));
        }

        [Fact]
        public void RegisterViewModel_MissingPassword_FailsValidation()
        {
            var model = new RegisterViewModel
            {
                Username = "TestUser",
                Email = "test@test.com"
            };

            var results = ValidateModel(model);
            Assert.Contains(results, r => r.MemberNames.Contains("Password"));
        }

        // ═══ LoginViewModel Tests ═══

        [Fact]
        public void LoginViewModel_ValidData_PassesValidation()
        {
            var model = new LoginViewModel
            {
                Email = "test@test.com",
                Password = "Test123!"
            };

            var results = ValidateModel(model);
            Assert.Empty(results);
        }

        [Fact]
        public void LoginViewModel_MissingEmail_FailsValidation()
        {
            var model = new LoginViewModel { Password = "Test123!" };

            var results = ValidateModel(model);
            Assert.Contains(results, r => r.MemberNames.Contains("Email"));
        }

        [Fact]
        public void LoginViewModel_MissingPassword_FailsValidation()
        {
            var model = new LoginViewModel { Email = "test@test.com" };

            var results = ValidateModel(model);
            Assert.Contains(results, r => r.MemberNames.Contains("Password"));
        }

        // ═══ CheckoutViewModel Tests ═══

        [Fact]
        public void CheckoutViewModel_ValidData_PassesValidation()
        {
            var model = new CheckoutViewModel
            {
                MovieTitle = "Test Movie",
                Showtime = "Mar 20 - 7:00 PM",
                SelectedSeats = "A1, A2",
                TotalPrice = 25.00m,
                CardHolderName = "John Doe",
                CardNumber = "4111111111111111",
                ExpiryDate = "12/26",
                Cvc = "123"
            };

            var results = ValidateModel(model);
            Assert.Empty(results);
        }

        [Fact]
        public void CheckoutViewModel_InvalidExpiryDate_FailsValidation()
        {
            var model = new CheckoutViewModel
            {
                CardHolderName = "John Doe",
                CardNumber = "4111111111111111",
                ExpiryDate = "13/26",
                Cvc = "123"
            };

            var results = ValidateModel(model);
            Assert.Contains(results, r => r.MemberNames.Contains("ExpiryDate"));
        }

        [Fact]
        public void CheckoutViewModel_CvcTooShort_FailsValidation()
        {
            var model = new CheckoutViewModel
            {
                CardHolderName = "John Doe",
                CardNumber = "4111111111111111",
                ExpiryDate = "12/26",
                Cvc = "12"
            };

            var results = ValidateModel(model);
            Assert.Contains(results, r => r.MemberNames.Contains("Cvc"));
        }

        [Fact]
        public void CheckoutViewModel_CvcTooLong_FailsValidation()
        {
            var model = new CheckoutViewModel
            {
                CardHolderName = "John Doe",
                CardNumber = "4111111111111111",
                ExpiryDate = "12/26",
                Cvc = "12345"
            };

            var results = ValidateModel(model);
            Assert.Contains(results, r => r.MemberNames.Contains("Cvc"));
        }

        [Fact]
        public void CheckoutViewModel_MissingCardHolder_FailsValidation()
        {
            var model = new CheckoutViewModel
            {
                CardNumber = "4111111111111111",
                ExpiryDate = "12/26",
                Cvc = "123"
            };

            var results = ValidateModel(model);
            Assert.Contains(results, r => r.MemberNames.Contains("CardHolderName"));
        }

        // ═══ UserReview Tests ═══

        [Fact]
        public void UserReview_ValidData_PassesValidation()
        {
            var model = new UserReview
            {
                UserId = "user-1",
                MovieId = 100,
                Rating = 8,
                Content = "Great movie!"
            };

            var results = ValidateModel(model);
            Assert.Empty(results);
        }

        [Fact]
        public void UserReview_RatingOutOfRange_FailsValidation()
        {
            var model = new UserReview
            {
                UserId = "user-1",
                MovieId = 100,
                Rating = 11,
                Content = "Too good!"
            };

            var results = ValidateModel(model);
            Assert.Contains(results, r => r.MemberNames.Contains("Rating"));
        }

        [Fact]
        public void UserReview_RatingZero_FailsValidation()
        {
            var model = new UserReview
            {
                UserId = "user-1",
                MovieId = 100,
                Rating = 0,
                Content = "Bad"
            };

            var results = ValidateModel(model);
            Assert.Contains(results, r => r.MemberNames.Contains("Rating"));
        }

        [Fact]
        public void UserReview_MissingContent_FailsValidation()
        {
            var model = new UserReview
            {
                UserId = "user-1",
                MovieId = 100,
                Rating = 5
            };

            var results = ValidateModel(model);
            Assert.Contains(results, r => r.MemberNames.Contains("Content"));
        }

        [Fact]
        public void UserReview_ContentTooLong_FailsValidation()
        {
            var model = new UserReview
            {
                UserId = "user-1",
                MovieId = 100,
                Rating = 5,
                Content = new string('x', 2001)
            };

            var results = ValidateModel(model);
            Assert.Contains(results, r => r.MemberNames.Contains("Content"));
        }

        // ═══ Booking Tests ═══

        [Fact]
        public void Booking_ValidData_PassesValidation()
        {
            var model = new Booking
            {
                UserId = "user-1",
                MovieTitle = "Test Movie"
            };

            var results = ValidateModel(model);
            Assert.Empty(results);
        }

        [Fact]
        public void Booking_MissingUserId_FailsValidation()
        {
            var model = new Booking { MovieTitle = "Test Movie" };

            var results = ValidateModel(model);
            Assert.Contains(results, r => r.MemberNames.Contains("UserId"));
        }

        [Fact]
        public void Booking_MissingMovieTitle_FailsValidation()
        {
            var model = new Booking { UserId = "user-1" };

            var results = ValidateModel(model);
            Assert.Contains(results, r => r.MemberNames.Contains("MovieTitle"));
        }

        [Fact]
        public void Booking_QrCodeUrl_ContainsConfirmationCode()
        {
            var booking = new Booking
            {
                ConfirmationCode = "TEST1234",
                MovieTitle = "Test",
                Showtime = "7PM",
                Seats = "A1"
            };

            Assert.Contains("TEST1234", booking.QrCodeUrl);
        }

        // ═══ Showtime Tests ═══

        [Fact]
        public void Showtime_FormattedDateTime_ReturnsCorrectFormat()
        {
            var showtime = new Showtime
            {
                ShowDateTime = new System.DateTime(2026, 3, 20, 19, 0, 0)
            };

            Assert.Equal("Mar 20, 2026 - 7:00 PM", showtime.FormattedDateTime);
        }

        [Fact]
        public void Showtime_FullPosterPath_WithPoster_ReturnsFullUrl()
        {
            var showtime = new Showtime { PosterPath = "/poster.jpg" };
            Assert.Equal("https://image.tmdb.org/t/p/w500/poster.jpg", showtime.FullPosterPath);
        }

        [Fact]
        public void Showtime_FullPosterPath_NoPoster_ReturnsPlaceholder()
        {
            var showtime = new Showtime { PosterPath = null };
            Assert.Contains("placehold", showtime.FullPosterPath);
        }

        [Fact]
        public void Showtime_DefaultValues_AreCorrect()
        {
            var showtime = new Showtime();
            Assert.Equal("Hall 1", showtime.Hall);
            Assert.Equal(12.50m, showtime.Price);
            Assert.Equal(96, showtime.TotalSeats);
            Assert.True(showtime.IsActive);
        }

        // ═══ UserFavorite Tests ═══

        [Fact]
        public void UserFavorite_FullPosterPath_WithPoster_ReturnsFullUrl()
        {
            var fav = new UserFavorite { PosterPath = "/fav.jpg" };
            Assert.Equal("https://image.tmdb.org/t/p/w500/fav.jpg", fav.FullPosterPath);
        }

        [Fact]
        public void UserFavorite_FullPosterPath_NoPoster_ReturnsPlaceholder()
        {
            var fav = new UserFavorite { PosterPath = null };
            Assert.Contains("placehold", fav.FullPosterPath);
        }

        // ═══ CriticApplication Tests ═══

        [Fact]
        public void CriticApplication_ReviewLinks_SerializesCorrectly()
        {
            var app = new CriticApplication();
            app.ReviewLinks = new List<string> { "https://link1.com", "https://link2.com" };

            Assert.Contains("link1.com", app.ReviewLinksJson);
            Assert.Contains("link2.com", app.ReviewLinksJson);
        }

        [Fact]
        public void CriticApplication_ReviewLinks_DeserializesCorrectly()
        {
            var app = new CriticApplication
            {
                ReviewLinksJson = "[\"https://test.com\"]"
            };

            Assert.Single(app.ReviewLinks);
            Assert.Equal("https://test.com", app.ReviewLinks[0]);
        }

        [Fact]
        public void CriticApplication_ReviewLinks_EmptyJson_ReturnsEmptyList()
        {
            var app = new CriticApplication { ReviewLinksJson = "" };
            Assert.Empty(app.ReviewLinks);
        }

        [Fact]
        public void CriticApplication_DefaultStatus_IsPending()
        {
            var app = new CriticApplication();
            Assert.Equal(ApplicationStatus.Pending, app.Status);
        }

        // ═══ SeatReservation Tests ═══

        [Fact]
        public void SeatReservation_MissingSeatNumber_FailsValidation()
        {
            var model = new SeatReservation
            {
                ShowtimeId = 1,
                BookingId = 1
            };

            var results = ValidateModel(model);
            Assert.Contains(results, r => r.MemberNames.Contains("SeatNumber"));
        }

        // ═══ ApplicationUser Tests ═══

        [Fact]
        public void ApplicationUser_DefaultValues_AreCorrect()
        {
            var user = new ApplicationUser();
            Assert.False(user.IsCritic);
            Assert.Null(user.EmailVerificationCode);
            Assert.Null(user.VerificationCodeExpiry);
        }

        // ═══ SettingsViewModel Tests ═══

        [Fact]
        public void SettingsViewModel_ValidData_PassesValidation()
        {
            var model = new SettingsViewModel
            {
                Username = "TestUser",
                Email = "test@test.com",
                OldPassword = "Old123!",
                NewPassword = "New123!",
                ConfirmPassword = "New123!"
            };

            var results = ValidateModel(model);
            Assert.Empty(results);
        }

        // ═══ ConfirmEmailViewModel Tests ═══

        [Fact]
        public void ConfirmEmailViewModel_MissingCode_FailsValidation()
        {
            var model = new ConfirmEmailViewModel
            {
                Email = "test@test.com"
            };

            var results = ValidateModel(model);
            Assert.Contains(results, r => r.MemberNames.Contains("Code"));
        }
    }
}
