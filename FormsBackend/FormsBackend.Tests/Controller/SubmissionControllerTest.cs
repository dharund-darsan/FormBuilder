using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FormsBackend.Controllers;
using FormsBackend.DTOs;
using FormsBackend.Enums;
using FormsBackend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace FormsBackend.Tests.Controller
{
    public class SubmissionControllerTest
    {
        private readonly Mock<ISubmissionService> _mockSubmissionService;
        private readonly Mock<IFormService> _mockFormService;
        private readonly SubmissionController _controller;

        public SubmissionControllerTest()
        {
            _mockSubmissionService = new Mock<ISubmissionService>();
            _mockFormService = new Mock<IFormService>();
            _controller = new SubmissionController(_mockSubmissionService.Object, _mockFormService.Object);
        }

        private void SetupUser(int userId, string role = nameof(UserRole.Learner))
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        #region SubmitForm Tests - Lines 24-48

       
        
        
        [Fact]
        public async Task SubmitForm_NoUserIdInClaim_UsesZero()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, nameof(UserRole.Learner)) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var dto = new SubmitFormDto { FormId = "form123", Answers = new List<SubmissionAnswerDto>() };
            var submissionResponse = new SubmissionResponseDto { Id = 1, FormId = "form123" };
            
            _mockSubmissionService.Setup(x => x.SubmitFormAsync(dto, 0))
                .ReturnsAsync(submissionResponse);

            // Act
            var result = await _controller.SubmitForm(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            _mockSubmissionService.Verify(x => x.SubmitFormAsync(dto, 0), Times.Once);
        }

        #endregion

        #region GetMySubmissions Tests - Lines 51-69

       
       
        [Fact]
        public async Task GetMySubmissions_NoUserIdInClaim_UsesZero()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, nameof(UserRole.Learner)) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var mySubmissions = new MySubmissionsDto
            {
                TotalSubmissions = 0,
                Submissions = new List<SubmissionSummaryDto>()
            };
            
            _mockSubmissionService.Setup(x => x.GetMySubmissionsAsync(0))
                .ReturnsAsync(mySubmissions);

            // Act
            var result = await _controller.GetMySubmissions();

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            _mockSubmissionService.Verify(x => x.GetMySubmissionsAsync(0), Times.Once);
        }

        #endregion

        #region GetSubmissionDetails Tests - Lines 72-102

       
        [Fact]
        public async Task GetSubmissionDetails_UnauthorizedAccessException_ReturnsForbid()
        {
            // Arrange
            SetupUser(1, nameof(UserRole.Learner));
            _mockSubmissionService.Setup(x => x.GetSubmissionDetailsAsync(1, 1))
                .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

            // Act
            var result = await _controller.GetSubmissionDetails(1);

            // Assert
            var forbidResult = Assert.IsType<ForbidResult>(result.Result);
            Assert.Equal("Access denied", forbidResult.AuthenticationSchemes.FirstOrDefault());
        }

       
        
        [Fact]
        public async Task GetSubmissionDetails_NoUserIdInClaim_UsesZero()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, nameof(UserRole.Learner)) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var submission = new SubmissionResponseDto { Id = 1 };
            _mockSubmissionService.Setup(x => x.GetSubmissionDetailsAsync(1, 0))
                .ReturnsAsync(submission);

            // Act
            var result = await _controller.GetSubmissionDetails(1);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            _mockSubmissionService.Verify(x => x.GetSubmissionDetailsAsync(1, 0), Times.Once);
        }

        #endregion

        #region GetFormSubmissions Tests - Lines 105-123

       
        #endregion

        #region GetFormFiles Tests - Lines 125-162

       
        
        [Fact]
        public async Task GetFormFiles_LearnerWithoutSubmission_ReturnsForbid()
        {
            // Arrange
            SetupUser(1, nameof(UserRole.Learner));
            _mockSubmissionService.Setup(x => x.HasUserSubmittedFormAsync(1, "form123"))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.GetFormFiles("form123");

            // Assert
            var forbidResult = Assert.IsType<ForbidResult>(result.Result);
            Assert.Equal("You don't have permission to view files for this form", 
                forbidResult.AuthenticationSchemes.FirstOrDefault());
        }

        
        
        [Fact]
        public async Task GetFormFiles_LearnerNoUserIdInClaim_UsesZero()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, nameof(UserRole.Learner)) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            _mockSubmissionService.Setup(x => x.HasUserSubmittedFormAsync(0, "form123"))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.GetFormFiles("form123");

            // Assert
            var forbidResult = Assert.IsType<ForbidResult>(result.Result);
            _mockSubmissionService.Verify(x => x.HasUserSubmittedFormAsync(0, "form123"), Times.Once);
        }

        #endregion

        #region GetFile Tests - Lines 164-218

        [Fact]
        public async Task GetFile_DownloadAction_ReturnsFileWithDownloadHeaders()
        {
            // Arrange
            SetupUser(1, nameof(UserRole.Learner));
            var fileDownload = new FileDownloadDto
            {
                FileName = "test.pdf",
                MimeType = "application/pdf",
                FileData = "base64data",
                FileBytes = new byte[] { 1, 2, 3 }
            };
            
            _mockSubmissionService.Setup(x => x.GetFileForDownloadAsync(1, 1, false))
                .ReturnsAsync(fileDownload);

            // Act
            var result = await _controller.GetFile(1, "download");

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("test.pdf", fileResult.FileDownloadName);
            Assert.Equal("application/pdf", fileResult.ContentType);
        }

        
        [Fact]
        public async Task GetFile_InvalidAction_DefaultsToDownload()
        {
            // Arrange
            SetupUser(1, nameof(UserRole.Learner));
            var fileDownload = new FileDownloadDto
            {
                FileName = "test.pdf",
                MimeType = "application/pdf",
                FileData = "base64data",
                FileBytes = new byte[] { 1, 2, 3 }
            };
            
            _mockSubmissionService.Setup(x => x.GetFileForDownloadAsync(1, 1, false))
                .ReturnsAsync(fileDownload);

            // Act
            var result = await _controller.GetFile(1, "invalidaction");

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("test.pdf", fileResult.FileDownloadName); // Defaults to download
        }

        [Fact]
        public async Task GetFile_NullAction_DefaultsToDownload()
        {
            // Arrange
            SetupUser(1, nameof(UserRole.Learner));
            var fileDownload = new FileDownloadDto
            {
                FileName = "test.pdf",
                MimeType = "application/pdf",
                FileData = "base64data",
                FileBytes = new byte[] { 1, 2, 3 }
            };
            
            _mockSubmissionService.Setup(x => x.GetFileForDownloadAsync(1, 1, false))
                .ReturnsAsync(fileDownload);

            // Act
            var result = await _controller.GetFile(1, null);

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("test.pdf", fileResult.FileDownloadName);
        }

        [Fact]
        public async Task GetFile_AsAdmin_PassesIsAdminTrue()
        {
                        SetupUser(1, nameof(UserRole.Admin));
            var fileDownload = new FileDownloadDto
            {
                FileName = "test.pdf",
                MimeType = "application/pdf",
                FileData = "base64data",
                FileBytes = new byte[] { 1, 2, 3 }
            };
            
            _mockSubmissionService.Setup(x => x.GetFileForDownloadAsync(1, 1, true))
                .ReturnsAsync(fileDownload);

            // Act
            var result = await _controller.GetFile(1, "download");

            // Assert
            Assert.IsType<FileContentResult>(result);
            _mockSubmissionService.Verify(x => x.GetFileForDownloadAsync(1, 1, true), Times.Once);
        }

        [Fact]
        public async Task GetFile_AsLearner_PassesIsAdminFalse()
        {
            // Arrange
            SetupUser(1, nameof(UserRole.Learner));
            var fileDownload = new FileDownloadDto
            {
                FileName = "test.pdf",
                MimeType = "application/pdf",
                FileData = "base64data",
                FileBytes = new byte[] { 1, 2, 3 }
            };
            
            _mockSubmissionService.Setup(x => x.GetFileForDownloadAsync(1, 1, false))
                .ReturnsAsync(fileDownload);

            // Act
            var result = await _controller.GetFile(1, "download");

            // Assert
            Assert.IsType<FileContentResult>(result);
            _mockSubmissionService.Verify(x => x.GetFileForDownloadAsync(1, 1, false), Times.Once);
        }

        [Fact]
        public async Task GetFile_UnauthorizedAccessException_ReturnsForbid()
        {
            // Arrange
            SetupUser(1, nameof(UserRole.Learner));
            _mockSubmissionService.Setup(x => x.GetFileForDownloadAsync(1, 1, false))
                .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

            // Act
            var result = await _controller.GetFile(1, "download");

            // Assert
            var forbidResult = Assert.IsType<ForbidResult>(result);
            Assert.Equal("Access denied", forbidResult.AuthenticationSchemes.FirstOrDefault());
        }

       
        
        [Fact]
        public async Task GetFile_NoUserIdInClaim_UsesZero()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, nameof(UserRole.Learner)) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var fileDownload = new FileDownloadDto
            {
                FileName = "test.pdf",
                MimeType = "application/pdf",
                FileData = "base64data",
                FileBytes = new byte[] { 1, 2, 3 }
            };
            
            _mockSubmissionService.Setup(x => x.GetFileForDownloadAsync(1, 0, false))
                .ReturnsAsync(fileDownload);

            // Act
            var result = await _controller.GetFile(1, "download");

            // Assert
            Assert.IsType<FileContentResult>(result);
            _mockSubmissionService.Verify(x => x.GetFileForDownloadAsync(1, 0, false), Times.Once);
        }

        [Fact]
        public async Task GetFile_NoRoleInClaim_AssumesNotAdmin()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var fileDownload = new FileDownloadDto
            {
                FileName = "test.pdf",
                MimeType = "application/pdf",
                FileData = "base64data",
                FileBytes = new byte[] { 1, 2, 3 }
            };
            
            _mockSubmissionService.Setup(x => x.GetFileForDownloadAsync(1, 1, false))
                .ReturnsAsync(fileDownload);

            // Act
            var result = await _controller.GetFile(1, "download");

            // Assert
            Assert.IsType<FileContentResult>(result);
            _mockSubmissionService.Verify(x => x.GetFileForDownloadAsync(1, 1, false), Times.Once);
        }

        [Fact]
        public async Task GetFile_EmptyAction_DefaultsToDownload()
        {
            // Arrange
            SetupUser(1, nameof(UserRole.Learner));
            var fileDownload = new FileDownloadDto
            {
                FileName = "test.pdf",
                MimeType = "application/pdf",
                FileData = "base64data",
                FileBytes = new byte[] { 1, 2, 3 }
            };
            
            _mockSubmissionService.Setup(x => x.GetFileForDownloadAsync(1, 1, false))
                .ReturnsAsync(fileDownload);

            // Act
            var result = await _controller.GetFile(1, "");

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("test.pdf", fileResult.FileDownloadName);
        }

        [Fact]
        public async Task GetFile_DownloadActionCaseInsensitive_DefaultsToDownload()
        {
            // Arrange
            SetupUser(1, nameof(UserRole.Learner));
            var fileDownload = new FileDownloadDto
            {
                FileName = "test.pdf",
                MimeType = "application/pdf",
                FileData = "base64data",
                FileBytes = new byte[] { 1, 2, 3 }
            };
            
            _mockSubmissionService.Setup(x => x.GetFileForDownloadAsync(1, 1, false))
                .ReturnsAsync(fileDownload);

            // Act
            var result = await _controller.GetFile(1, "DOWNLOAD");

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("test.pdf", fileResult.FileDownloadName);
        }

        [Fact]
        public async Task GetFile_PreviewActionCaseInsensitive_DefaultsToDownload()
        {
            // Arrange
            SetupUser(1, nameof(UserRole.Learner));
            var fileDownload = new FileDownloadDto
            {
                FileName = "test.pdf",
                MimeType = "application/pdf",
                FileData = "base64data",
                FileBytes = new byte[] { 1, 2, 3 }
            };
            
            _mockSubmissionService.Setup(x => x.GetFileForDownloadAsync(1, 1, false))
                .ReturnsAsync(fileDownload);

            // Act
            var result = await _controller.GetFile(1, "PREVIEW");

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("test.pdf", fileResult.FileDownloadName); // Case-sensitive check fails, defaults to download
        }

        #endregion

        #region Edge Cases and Additional Coverage

        [Fact]
        public async Task GetFormFiles_UserWithNoRole_ChecksIsInRole()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            _mockSubmissionService.Setup(x => x.HasUserSubmittedFormAsync(1, "form123"))
                .ReturnsAsync(true);
            
            var formFiles = new FormFilesResponseDto
            {
                FormId = "form123",
                FormTitle = "Test Form",
                TotalFiles = 1,
                Files = new List<FormFileDto>()
            };
            
            _mockSubmissionService.Setup(x => x.GetFormFilesAsync("form123"))
                .ReturnsAsync(formFiles);

            // Act
            var result = await _controller.GetFormFiles("form123");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            _mockSubmissionService.Verify(x => x.HasUserSubmittedFormAsync(1, "form123"), Times.Once);
        }

        
        [Fact]
        public async Task SubmitForm_NullClaims_UsesZeroUserId()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            var dto = new SubmitFormDto { FormId = "form123", Answers = new List<SubmissionAnswerDto>() };
            var submissionResponse = new SubmissionResponseDto { Id = 1, FormId = "form123" };
            
            _mockSubmissionService.Setup(x => x.SubmitFormAsync(dto, 0))
                .ReturnsAsync(submissionResponse);

            // Act
            var result = await _controller.SubmitForm(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            _mockSubmissionService.Verify(x => x.SubmitFormAsync(dto, 0), Times.Once);
        }

       
        [Fact]
        public async Task GetFile_SpecialCharactersInAction_DefaultsToDownload()
        {
            // Arrange
            SetupUser(1, nameof(UserRole.Learner));
            var fileDownload = new FileDownloadDto
            {
                FileName = "test.pdf",
                MimeType = "application/pdf",
                FileData = "base64data",
                FileBytes = new byte[] { 1, 2, 3 }
            };
            
            _mockSubmissionService.Setup(x => x.GetFileForDownloadAsync(1, 1, false))
                .ReturnsAsync(fileDownload);

            // Act
            var result = await _controller.GetFile(1, "down!@#$%load");

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("test.pdf", fileResult.FileDownloadName); // Invalid action, defaults to download
        }

        [Fact]
        public async Task GetFile_WhitespaceAction_DefaultsToDownload()
        {
            // Arrange
            SetupUser(1, nameof(UserRole.Learner));
            var fileDownload = new FileDownloadDto
            {
                FileName = "test.pdf",
                MimeType = "application/pdf",
                FileData = "base64data",
                FileBytes = new byte[] { 1, 2, 3 }
            };
            
            _mockSubmissionService.Setup(x => x.GetFileForDownloadAsync(1, 1, false))
                .ReturnsAsync(fileDownload);

            // Act
            var result = await _controller.GetFile(1, "   ");

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("test.pdf", fileResult.FileDownloadName);
        }

        
        #endregion
    }
}

