using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Sashiel_ST10028058_PROG6212_Part2.Models;
using Sashiel_ST10028058_PROG6212_Part2.Controllers;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using Moq;
using Sashiel_ST10028058_PROG6212_Part2.Data;


    public class ClaimsControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly ClaimsController _controller;

        public ClaimsControllerTests()
        {
            // Use a unique in-memory database for each test to avoid conflicts
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging() // Enable detailed logging for better error tracking
                .Options;

            _context = new ApplicationDbContext(options);
            _controller = new ClaimsController(_context);
        }

        // Helper method to create a fully valid Claim instance
        private Claim CreateValidClaim(int id, string status = "Pending")
        {
            return new Claim
            {
                ClaimId = id,
                LecturerName = "John Doe",  // Required field
                Notes = "Some notes",  // Required field
                SupportingDocumentPath = "/uploads/sample.pdf",  // Required field
                Status = status,
                HoursWorked = 10,  // Valid numeric value
                HourlyRate = 20    // Valid numeric value
            };
        }

        [Fact]
        public void SubmitClaim_Get_ReturnsViewResult()
        {
            // Act
            var result = _controller.SubmitClaim();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task SubmitClaim_Post_InvalidFile_ReturnsViewWithModelError()
        {
            // Arrange
            var claim = CreateValidClaim(1);
            var mockFile = new Mock<IFormFile>();

            mockFile.Setup(f => f.Length).Returns(1024); // Small file size
            mockFile.Setup(f => f.FileName).Returns("invalid.txt"); // Invalid file extension

            // Act
            var result = await _controller.SubmitClaim(claim, mockFile.Object) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.True(_controller.ModelState["document"].Errors.Count > 0);
        }

        [Fact]
        public async Task ViewPendingClaims_ReturnsViewWithPendingClaims()
        {
            // Arrange
            var claim1 = CreateValidClaim(1);
            var claim2 = CreateValidClaim(2);

            _context.Claims.AddRange(claim1, claim2); // Add claims to the in-memory DB
            await _context.SaveChangesAsync(); // Save the changes

            // Act
            var result = await _controller.ViewPendingClaims() as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsAssignableFrom<List<Claim>>(result.Model);
            Assert.Equal(2, model.Count); // Ensure both claims are retrieved
        }

        [Fact]
        public async Task ApproveClaim_ValidId_ChangesStatusAndSaves()
        {
            // Arrange
            var claim = CreateValidClaim(1);
            _context.Claims.Add(claim);
            await _context.SaveChangesAsync(); // Save the initial claim

            // Act
            await _controller.ApproveClaim(1);

            // Assert
            var updatedClaim = await _context.Claims.FindAsync(1);
            Assert.Equal("Approved", updatedClaim.Status); // Ensure the status is updated
        }

        [Fact]
        public async Task RejectClaim_ValidId_ChangesStatusAndSaves()
        {
            // Arrange
            var claim = CreateValidClaim(2);
            _context.Claims.Add(claim);
            await _context.SaveChangesAsync(); // Save the initial claim

            // Act
            await _controller.RejectClaim(2);

            // Assert
            var updatedClaim = await _context.Claims.FindAsync(2);
            Assert.Equal("Rejected", updatedClaim.Status); // Ensure the status is updated
        }

        [Fact]
        public async Task DeleteClaim_ValidId_RemovesClaimAndSaves()
        {
            // Arrange
            var claim = CreateValidClaim(3);
            _context.Claims.Add(claim);
            await _context.SaveChangesAsync(); // Save the initial claim

            // Act
            await _controller.DeleteClaim(3);

            // Assert
            var deletedClaim = await _context.Claims.FindAsync(3);
            Assert.Null(deletedClaim); // Ensure the claim is deleted
        }
    }
