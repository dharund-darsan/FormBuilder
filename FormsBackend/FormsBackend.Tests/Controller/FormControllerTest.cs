using System;
using System.Collections.Generic;
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
    public class FormControllerTest
    {
        private readonly Mock<IFormService> _mockFormService;
        private readonly FormsController _controller;

        public FormControllerTest()
        {
            _mockFormService = new Mock<IFormService>();
            _controller = new FormsController(_mockFormService.Object);
            
            // Setup default user context
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Role, UserRole.Admin.ToString())
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        #region CreateForm Tests

        [Fact]
        public async Task CreateForm_Success_ReturnsCreatedResult()
        {
            // Arrange
            var createDto = new CreateFormDto
            {
                Title = "Test Form",
                Description = "Test Description",
                Header = "Test Header",
                HeaderDescription = "Test Header Description",
                Config = new FormConfigDto { AllowMultipleSubmissions = false },
                Questions = new List<FormQuestionDto>()
            };

            var formResponse = new FormResponseDto
            {
                Id = "test-id",
                Title = "Test Form",
                Status = "draft"
            };

            _mockFormService.Setup(x => x.CreateFormAsync(It.IsAny<CreateFormDto>(), It.IsAny<string>()))
                .ReturnsAsync(formResponse);

            // Act
            var result = await _controller.CreateForm(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<FormStatusResponseDto>(createdResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Form created as draft successfully", response.Message);
            
            _mockFormService.Verify(x => x.CreateFormAsync(It.IsAny<CreateFormDto>(), "testuser"), Times.Once);
        }

        
        [Fact]
        public async Task CreateForm_ArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var createDto = new CreateFormDto
            {
                Title = "Test Form",
                Config = new FormConfigDto(),
                Questions = new List<FormQuestionDto>()
            };

            _mockFormService.Setup(x => x.CreateFormAsync(It.IsAny<CreateFormDto>(), It.IsAny<string>()))
                .ThrowsAsync(new ArgumentException("Invalid argument"));

            // Act
            var result = await _controller.CreateForm(createDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<FormStatusResponseDto>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Invalid argument", response.Message);
        }

        [Fact]
        public async Task CreateForm_GeneralException_ReturnsInternalServerError()
        {
            // Arrange
            var createDto = new CreateFormDto
            {
                Title = "Test Form",
                Config = new FormConfigDto(),
                Questions = new List<FormQuestionDto>()
            };

            _mockFormService.Setup(x => x.CreateFormAsync(It.IsAny<CreateFormDto>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Server error"));

            // Act
            var result = await _controller.CreateForm(createDto);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var response = Assert.IsType<FormStatusResponseDto>(statusCodeResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Error creating form: Server error", response.Message);
        }

        #endregion

        #region GetFormById Tests

        [Fact]
        public async Task GetFormById_ExistingForm_ReturnsOk()
        {
            // Arrange
            var formId = "test-id";
            var formResponse = new FormResponseDto
            {
                Id = formId,
                Title = "Test Form"
            };

            _mockFormService.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync(formResponse);

            // Act
            var result = await _controller.GetFormById(formId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<FormResponseDto>(okResult.Value);
            Assert.Equal(formId, response.Id);
        }

        [Fact]
        public async Task GetFormById_NonExistingForm_ReturnsNotFound()
        {
            // Arrange
            var formId = "non-existing-id";
            _mockFormService.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync((FormResponseDto)null);

            // Act
            var result = await _controller.GetFormById(formId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var value = notFoundResult.Value;
            var messageProperty = value.GetType().GetProperty("message");
            Assert.Equal("Form not found", messageProperty.GetValue(value));
        }

        #endregion

        #region GetAllForms Tests

        [Fact]
        public async Task GetAllForms_ReturnsOkWithForms()
        {
            // Arrange
            var forms = new List<FormResponseDto>
            {
                new FormResponseDto { Id = "1", Title = "Form 1" },
                new FormResponseDto { Id = "2", Title = "Form 2" }
            };

            _mockFormService.Setup(x => x.GetAllFormsAsync())
                .ReturnsAsync(forms);

            // Act
            var result = await _controller.GetAllForms();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<List<FormResponseDto>>(okResult.Value);
            Assert.Equal(2, response.Count);
        }

        #endregion

        #region PublishForm Tests

        [Fact]
        public async Task PublishForm_Success_ReturnsOk()
        {
            // Arrange
            var formId = "test-id";
            var formResponse = new FormResponseDto
            {
                Id = formId,
                Title = "Test Form",
                Status = "published"
            };

            _mockFormService.Setup(x => x.PublishFormAsync(formId, It.IsAny<string>()))
                .ReturnsAsync(formResponse);

            // Act
            var result = await _controller.PublishForm(formId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<FormResponseDto>(okResult.Value);
            Assert.Equal("published", response.Status);
            _mockFormService.Verify(x => x.PublishFormAsync(formId, "testuser"), Times.Once);
        }

        [Fact]
        public async Task PublishForm_InvalidOperation_ReturnsBadRequest()
        {
            // Arrange
            var formId = "test-id";
            _mockFormService.Setup(x => x.PublishFormAsync(formId, It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Form already published"));

            // Act
            var result = await _controller.PublishForm(formId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<FormStatusResponseDto>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Form already published", response.Message);
        }

        #endregion

        #region EditForm Tests

        [Fact]
        public async Task EditForm_DraftMode_Success_ReturnsOk()
        {
            // Arrange
            var updateDto = new UpdateFormDto
            {
                Id = "test-id",
                Title = "Updated Form",
                Mode = "draft",
                Config = new FormConfigDto(),
                Questions = new List<FormQuestionDto>()
            };

            var formResponse = new FormResponseDto
            {
                Id = "test-id",
                Title = "Updated Form",
                Status = "draft"
            };

            _mockFormService.Setup(x => x.UpdateFormAsync(It.IsAny<UpdateFormDto>(), It.IsAny<string>()))
                .ReturnsAsync(formResponse);

            // Act
            var result = await _controller.EditForm(updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<FormStatusResponseDto>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Form updated successfully", response.Message);
        }

        [Fact]
        public async Task EditForm_PublishMode_Success_ReturnsOk()
        {
            // Arrange
            var updateDto = new UpdateFormDto
            {
                Id = "test-id",
                Title = "Updated Form",
                Mode = "publish",
                Config = new FormConfigDto(),
                Questions = new List<FormQuestionDto>()
            };

            var formResponse = new FormResponseDto
            {
                Id = "test-id",
                Title = "Updated Form",
                Status = "published"
            };

            _mockFormService.Setup(x => x.UpdateFormAsync(It.IsAny<UpdateFormDto>(), It.IsAny<string>()))
                .ReturnsAsync(formResponse);

            // Act
            var result = await _controller.EditForm(updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<FormStatusResponseDto>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Form updated and published successfully", response.Message);
        }

        [Fact]
        public async Task EditForm_NullMode_DefaultsToDraft()
        {
            // Arrange
            var updateDto = new UpdateFormDto
            {
                Id = "test-id",
                Title = "Updated Form",
                Mode = null,
                Config = new FormConfigDto(),
                Questions = new List<FormQuestionDto>()
            };

            var formResponse = new FormResponseDto
            {
                Id = "test-id",
                Title = "Updated Form",
                Status = "draft"
            };

            _mockFormService.Setup(x => x.UpdateFormAsync(It.IsAny<UpdateFormDto>(), It.IsAny<string>()))
                .ReturnsAsync(formResponse);

            // Act
            var result = await _controller.EditForm(updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<FormStatusResponseDto>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Form updated successfully", response.Message);
        }

        [Fact]
        public async Task EditForm_InvalidOperation_ReturnsBadRequest()
        {
            // Arrange
            var updateDto = new UpdateFormDto
            {
                Id = "test-id",
                Title = "Updated Form",
                Config = new FormConfigDto()
            };

            _mockFormService.Setup(x => x.UpdateFormAsync(It.IsAny<UpdateFormDto>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Cannot edit published form"));

            // Act
            var result = await _controller.EditForm(updateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<FormStatusResponseDto>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Cannot edit published form", response.Message);
        }

        [Fact]
        public async Task EditForm_ArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var updateDto = new UpdateFormDto
            {
                Id = "test-id",
                Title = "Updated Form",
                Config = new FormConfigDto()
            };

            _mockFormService.Setup(x => x.UpdateFormAsync(It.IsAny<UpdateFormDto>(), It.IsAny<string>()))
                .ThrowsAsync(new ArgumentException("Invalid mode"));

            // Act
            var result = await _controller.EditForm(updateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<FormStatusResponseDto>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Invalid mode", response.Message);
        }

        [Fact]
        public async Task EditForm_GeneralException_ReturnsInternalServerError()
        {
            // Arrange
            var updateDto = new UpdateFormDto
            {
                Id = "test-id",
                Title = "Updated Form",
                Config = new FormConfigDto()
            };

            _mockFormService.Setup(x => x.UpdateFormAsync(It.IsAny<UpdateFormDto>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Server error"));

            // Act
            var result = await _controller.EditForm(updateDto);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var response = Assert.IsType<FormStatusResponseDto>(statusCodeResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Error updating form: Server error", response.Message);
        }

        #endregion

        #region DeleteForm Tests

        [Fact]
        public async Task DeleteForm_Success_ReturnsOk()
        {
            // Arrange
            var formId = "test-id";
            var formResponse = new FormResponseDto
            {
                Id = formId,
                Title = "Test Form",
                Status = "draft"
            };

            _mockFormService.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync(formResponse);
            _mockFormService.Setup(x => x.DeleteFormAsync(formId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteForm(formId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<DeleteResponseDto>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Form deleted successfully", response.Message);
        }

        [Fact]
        public async Task DeleteForm_FormNotFound_ReturnsNotFound()
        {
            // Arrange
            var formId = "non-existing-id";
            _mockFormService.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync((FormResponseDto)null);

            // Act
            var result = await _controller.DeleteForm(formId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<DeleteResponseDto>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Form not found", response.Message);
        }

        [Fact]
        public async Task DeleteForm_PublishedForm_ReturnsBadRequest()
        {
            // Arrange
            var formId = "test-id";
            var formResponse = new FormResponseDto
            {
                Id = formId,
                Title = "Test Form",
                Status = "published"
            };

            _mockFormService.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync(formResponse);

            // Act
            var result = await _controller.DeleteForm(formId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<DeleteResponseDto>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Cannot delete a published form. Published forms must be archived instead of deleted.", response.Message);
        }

        [Fact]
        public async Task DeleteForm_PublishedFormCaseInsensitive_ReturnsBadRequest()
        {
            // Arrange
            var formId = "test-id";
            var formResponse = new FormResponseDto
            {
                Id = formId,
                Title = "Test Form",
                Status = "PUBLISHED" // Testing case insensitivity
            };

            _mockFormService.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync(formResponse);

            // Act
            var result = await _controller.DeleteForm(formId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<DeleteResponseDto>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Cannot delete a published form. Published forms must be archived instead of deleted.", response.Message);
        }

        [Fact]
        public async Task DeleteForm_DeleteFails_ReturnsBadRequest()
        {
            // Arrange
            var formId = "test-id";
            var formResponse = new FormResponseDto
            {
                Id = formId,
                Title = "Test Form",
                Status = "draft"
            };

            _mockFormService.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync(formResponse);
            _mockFormService.Setup(x => x.DeleteFormAsync(formId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteForm(formId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<DeleteResponseDto>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Failed to delete form", response.Message);
        }

        [Fact]
        public async Task DeleteForm_GeneralException_ReturnsInternalServerError()
        {
            // Arrange
            var formId = "test-id";
            _mockFormService.Setup(x => x.GetFormByIdAsync(formId))
                .ThrowsAsync(new Exception("Server error"));

            // Act
            var result = await _controller.DeleteForm(formId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var response = Assert.IsType<DeleteResponseDto>(statusCodeResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Error deleting form: Server error", response.Message);
        }

        #endregion
    }
}
