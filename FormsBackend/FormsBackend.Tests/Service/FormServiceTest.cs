using Xunit;
using Moq;
using FormsBackend.Services;
using FormsBackend.Repositories;
using FormsBackend.DTOs;
using FormsBackend.Models.Mongo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FormsBackend.Tests.Services
{
    public class FormServiceTests
    {
        private readonly Mock<IFormRepository> _mockFormRepository;
        private readonly FormService _formService;

        public FormServiceTests()
        {
            _mockFormRepository = new Mock<IFormRepository>();
            _formService = new FormService(_mockFormRepository.Object);
        }

        #region CreateFormAsync Tests

        [Fact]
        public async Task CreateFormAsync_WithDraftMode_ShouldCreateDraftForm()
        {
            // Arrange
            var dto = new CreateFormDto
            {
                Title = "Test Form",
                Description = "Test Description",
                Header = "Test Header",
                HeaderDescription = "Test Header Description",
                Config = new FormConfigDto { AllowMultipleSubmissions = false },
                Questions = new List<FormQuestionDto>
                {
                    new FormQuestionDto
                    {
                        Type = "text",
                        Label = "Question 1",
                        IsRequired = true,
                        IsDescription = false,
                        IsMultiSelect = false,
                        DateFormat = "DD MMM YYYY",
                        Order = 1
                    }
                }
            };

            var createdForm = new Form
            {
                Id = "form-id",
                Title = dto.Title,
                Description = dto.Description,
                Header = dto.Header,
                HeaderDescription = dto.HeaderDescription,
                Status = "draft",
                CreatedAt = DateTime.UtcNow,
                Config = new FormConfig { AllowMultipleSubmissions = false },
                Questions = new List<FormQuestion>()
            };

            _mockFormRepository.Setup(r => r.CreateFormAsync(It.IsAny<Form>()))
                .ReturnsAsync(createdForm);

            // Act
            var result = await _formService.CreateFormAsync(dto, "testuser");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("draft", result.Status);
            Assert.Equal(dto.Title, result.Title);
            _mockFormRepository.Verify(r => r.CreateFormAsync(It.Is<Form>(f => 
                f.Status == "draft" && 
                f.PublishedBy == null && 
                f.PublishedAt == null)), Times.Once);
        }

        [Fact]
        public async Task CreateFormAsync_WithDropdownQuestion_ShouldTransformOptions()
        {
            // Arrange
            var dto = new CreateFormDto
            {
                Title = "Test Form",
                Description = "Test Description",
                Header = "Test Header",
                HeaderDescription = "Test Header Description",
                Config = new FormConfigDto { AllowMultipleSubmissions = true },
                Questions = new List<FormQuestionDto>
                {
                    new FormQuestionDto
                    {
                        Type = "dropdown",
                        Label = "Select Option",
                        IsRequired = false,
                        IsDescription = true,
                        IsMultiSelect = true,
                        DateFormat = null,
                        Order = 1,
                        Options = new List<string> { "Option 1", "Option 2" },
                        AllowedTypes = new List<string> { "pdf", "doc" }
                    }
                }
            };

            Form capturedForm = null;
            _mockFormRepository.Setup(r => r.CreateFormAsync(It.IsAny<Form>()))
                .Callback<Form>(f => capturedForm = f)
                .ReturnsAsync((Form f) => f);

            // Act
            var result = await _formService.CreateFormAsync(dto, "testuser");

            // Assert
            Assert.NotNull(capturedForm);
            Assert.NotEmpty(capturedForm.Questions);
            Assert.Equal(2, capturedForm.Questions[0].Options.Count);
            Assert.Equal("Option 1", capturedForm.Questions[0].Options[0].Value);
            Assert.NotEmpty(capturedForm.Questions[0].Options[0].Id);
        }

        [Fact]
        public async Task CreateFormAsync_WithRadioQuestion_ShouldTransformOptions()
        {
            // Arrange
            var dto = new CreateFormDto
            {
                Title = "Test Form",
                Description = "Description",
                Header = "Header",
                HeaderDescription = "Header Desc",
                Config = new FormConfigDto { AllowMultipleSubmissions = false },
                Questions = new List<FormQuestionDto>
                {
                    new FormQuestionDto
                    {
                        Type = "radio",
                        Label = "Choose One",
                        IsRequired = true,
                        Order = 1,
                        Options = new List<string> { "Yes", "No" }
                    }
                }
            };

            Form capturedForm = null;
            _mockFormRepository.Setup(r => r.CreateFormAsync(It.IsAny<Form>()))
                .Callback<Form>(f => capturedForm = f)
                .ReturnsAsync((Form f) => f);

            // Act
            await _formService.CreateFormAsync(dto, "testuser");

            // Assert
            Assert.NotNull(capturedForm);
            Assert.Equal(2, capturedForm.Questions[0].Options.Count);
            Assert.True(capturedForm.Questions[0].Options.All(o => o.IsActive));
        }

        [Fact]
        public async Task CreateFormAsync_WithCheckboxQuestion_ShouldTransformOptions()
        {
            // Arrange
            var dto = new CreateFormDto
            {
                Title = "Test Form",
                Description = "Description",
                Header = "Header",
                HeaderDescription = "Header Desc",
                Config = new FormConfigDto(),
                Questions = new List<FormQuestionDto>
                {
                    new FormQuestionDto
                    {
                        Type = "checkbox",
                        Label = "Select Multiple",
                        IsRequired = false,
                        Order = 1,
                        Options = new List<string> { "A", "B", "C" }
                    }
                }
            };

            Form capturedForm = null;
            _mockFormRepository.Setup(r => r.CreateFormAsync(It.IsAny<Form>()))
                .Callback<Form>(f => capturedForm = f)
                .ReturnsAsync((Form f) => f);

            // Act
            await _formService.CreateFormAsync(dto, "testuser");

            // Assert
            Assert.NotNull(capturedForm);
            Assert.Equal(3, capturedForm.Questions[0].Options.Count);
            Assert.Equal(0, capturedForm.Questions[0].Options[0].Order);
            Assert.Equal(1, capturedForm.Questions[0].Options[1].Order);
            Assert.Equal(2, capturedForm.Questions[0].Options[2].Order);
        }

        [Fact]
        public async Task CreateFormAsync_WithTextQuestion_ShouldNotTransformOptions()
        {
            // Arrange
            var dto = new CreateFormDto
            {
                Title = "Test Form",
                Description = "Description",
                Header = "Header",
                HeaderDescription = "Header Desc",
                Config = new FormConfigDto(),
                Questions = new List<FormQuestionDto>
                {
                    new FormQuestionDto
                    {
                        Type = "text",
                        Label = "Enter Text",
                        IsRequired = true,
                        Order = 1,
                        Options = new List<string> { "Should", "Be", "Ignored" }
                    }
                }
            };

            Form capturedForm = null;
            _mockFormRepository.Setup(r => r.CreateFormAsync(It.IsAny<Form>()))
                .Callback<Form>(f => capturedForm = f)
                .ReturnsAsync((Form f) => f);

            // Act
            await _formService.CreateFormAsync(dto, "testuser");

            // Assert
            Assert.NotNull(capturedForm);
            Assert.Empty(capturedForm.Questions[0].Options);
        }

        [Fact]
        public async Task CreateFormAsync_WithNullOptions_ShouldCreateEmptyOptionsList()
        {
            // Arrange
            var dto = new CreateFormDto
            {
                Title = "Test Form",
                Description = "Description",
                Header = "Header",
                HeaderDescription = "Header Desc",
                Config = new FormConfigDto(),
                Questions = new List<FormQuestionDto>
                {
                    new FormQuestionDto
                    {
                        Type = "dropdown",
                        Label = "Dropdown",
                        IsRequired = false,
                        Order = 1,
                        Options = null
                    }
                }
            };

            Form capturedForm = null;
            _mockFormRepository.Setup(r => r.CreateFormAsync(It.IsAny<Form>()))
                .Callback<Form>(f => capturedForm = f)
                .ReturnsAsync((Form f) => f);

            // Act
            await _formService.CreateFormAsync(dto, "testuser");

            // Assert
            Assert.NotNull(capturedForm);
            Assert.Empty(capturedForm.Questions[0].Options);
        }

        [Fact]
        public async Task CreateFormAsync_WithEmptyOptions_ShouldCreateEmptyOptionsList()
        {
            // Arrange
            var dto = new CreateFormDto
            {
                Title = "Test Form",
                Description = "Description",
                Header = "Header",
                HeaderDescription = "Header Desc",
                Config = new FormConfigDto(),
                Questions = new List<FormQuestionDto>
                {
                    new FormQuestionDto
                    {
                        Type = "radio",
                        Label = "Radio",
                        IsRequired = false,
                        Order = 1,
                        Options = new List<string>()
                    }
                }
            };

            Form capturedForm = null;
            _mockFormRepository.Setup(r => r.CreateFormAsync(It.IsAny<Form>()))
                .Callback<Form>(f => capturedForm = f)
                .ReturnsAsync((Form f) => f);

            // Act
            await _formService.CreateFormAsync(dto, "testuser");

            // Assert
            Assert.NotNull(capturedForm);
            Assert.Empty(capturedForm.Questions[0].Options);
        }

        [Fact]
        public async Task CreateFormAsync_WithNullAllowedTypes_ShouldUseEmptyList()
        {
            // Arrange
            var dto = new CreateFormDto
            {
                Title = "Test Form",
                Description = "Description",
                Header = "Header",
                HeaderDescription = "Header Desc",
                Config = new FormConfigDto(),
                Questions = new List<FormQuestionDto>
                {
                    new FormQuestionDto
                    {
                        Type = "file",
                        Label = "Upload File",
                        IsRequired = true,
                        Order = 1,
                        AllowedTypes = null
                    }
                }
            };

            Form capturedForm = null;
            _mockFormRepository.Setup(r => r.CreateFormAsync(It.IsAny<Form>()))
                .Callback<Form>(f => capturedForm = f)
                .ReturnsAsync((Form f) => f);

            // Act
            await _formService.CreateFormAsync(dto, "testuser");

            // Assert
            Assert.NotNull(capturedForm);
            Assert.Empty(capturedForm.Questions[0].AllowedTypes);
        }

        #endregion

        #region GetFormByIdAsync Tests

        [Fact]
        public async Task GetFormByIdAsync_WithExistingForm_ShouldReturnMappedForm()
        {
            // Arrange
            var formId = "test-form-id";
            var form = new Form
            {
                Id = formId,
                Title = "Test Form",
                Description = "Description",
                Header = "Header",
                HeaderDescription = "Header Desc",
                Status = "published",
                CreatedAt = DateTime.UtcNow,
                PublishedAt = DateTime.UtcNow,
                Config = new FormConfig { AllowMultipleSubmissions = true },
                Questions = new List<FormQuestion>
                {
                    new FormQuestion
                    {
                        Id = "q1",
                        Type = "text",
                        Label = "Question 1",
                        IsRequired = true,
                        IsDescription = false,
                        IsMultiSelect = false,
                        Order = 1,
                        Options = new List<QuestionOption>(),
                        AllowedTypes = new List<string>()
                    }
                }
            };

            _mockFormRepository.Setup(r => r.GetFormByIdAsync(formId))
                .ReturnsAsync(form);

            // Act
            var result = await _formService.GetFormByIdAsync(formId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(formId, result.Id);
            Assert.Equal("Test Form", result.Title);
            Assert.True(result.Config.AllowMultipleSubmissions);
            Assert.Single(result.Questions);
        }

        [Fact]
        public async Task GetFormByIdAsync_WithNullForm_ShouldReturnNull()
        {
            // Arrange
            _mockFormRepository.Setup(r => r.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((Form)null);

            // Act
            var result = await _formService.GetFormByIdAsync("non-existent");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetFormByIdAsync_WithDropdownQuestion_ShouldReturnOptionsWithIds()
        {
            // Arrange
            var form = new Form
            {
                Id = "form-id",
                Title = "Test",
                Description = "Desc",
                Header = "Header",
                HeaderDescription = "Header Desc",
                Status = "draft",
                CreatedAt = DateTime.UtcNow,
                Config = new FormConfig(),
                Questions = new List<FormQuestion>
                {
                    new FormQuestion
                    {
                        Id = "q1",
                        Type = "dropdown",
                        Label = "Select",
                        IsRequired = false,
                        IsDescription = true,
                        IsMultiSelect = true,
                        Order = 1,
                        Options = new List<QuestionOption>
                        {
                            new QuestionOption { Id = "opt1", Value = "Option 1", Order = 0, IsActive = true },
                            new QuestionOption { Id = "opt2", Value = "Option 2", Order = 1, IsActive = false },
                            new QuestionOption { Id = "opt3", Value = "Option 3", Order = 2, IsActive = true }
                        },
                        AllowedTypes = new List<string>()
                    }
                }
            };

            _mockFormRepository.Setup(r => r.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);

            // Act
            var result = await _formService.GetFormByIdAsync("form-id");

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Questions[0].Options);
            Assert.Equal(2, result.Questions[0].Options.Count); // Only active options
            Assert.Equal("opt1", result.Questions[0].Options[0].Id);
            Assert.Equal("Option 1", result.Questions[0].Options[0].Value);
        }

        [Fact]
        public async Task GetFormByIdAsync_WithNullQuestionsAndConfig_ShouldHandleGracefully()
        {
            // Arrange
            var form = new Form
            {
                Id = "form-id",
                Title = "Test",
                Description = "Desc",
                Header = "Header",
                HeaderDescription = "Header Desc",
                Status = "draft",
                CreatedAt = DateTime.UtcNow,
                Config = null,
                Questions = null
            };

            _mockFormRepository.Setup(r => r.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);

            // Act
            var result = await _formService.GetFormByIdAsync("form-id");

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Config.AllowMultipleSubmissions);
            Assert.Empty(result.Questions);
        }

        #endregion

        #region GetAllFormsAsync Tests

        [Fact]
        public async Task GetAllFormsAsync_WithMultipleForms_ShouldReturnAllMapped()
        {
            // Arrange
            var forms = new List<Form>
            {
                new Form
                {
                    Id = "form1",
                    Title = "Form 1",
                    Description = "Desc 1",
                    Header = "Header 1",
                    HeaderDescription = "Header Desc 1",
                    Status = "draft",
                    CreatedAt = DateTime.UtcNow,
                    Config = new FormConfig(),
                    Questions = new List<FormQuestion>()
                },
                new Form
                {
                    Id = "form2",
                    Title = "Form 2",
                    Description = "Desc 2",
                    Header = "Header 2",
                    HeaderDescription = "Header Desc 2",
                    Status = "published",
                    CreatedAt = DateTime.UtcNow,
                    PublishedAt = DateTime.UtcNow,
                    Config = new FormConfig { AllowMultipleSubmissions = true },
                    Questions = new List<FormQuestion>()
                }
            };

            _mockFormRepository.Setup(r => r.GetAllFormsAsync())
                .ReturnsAsync(forms);

            // Act
            var result = await _formService.GetAllFormsAsync();

            // Assert
            Assert.NotNull(result);
            var formsList = result.ToList();
            Assert.Equal(2, formsList.Count);
            Assert.Equal("form1", formsList[0].Id);
            Assert.Equal("form2", formsList[1].Id);
        }

        [Fact]
        public async Task GetAllFormsAsync_WithEmptyList_ShouldReturnEmptyList()
        {
            // Arrange
            _mockFormRepository.Setup(r => r.GetAllFormsAsync())
                .ReturnsAsync(new List<Form>());

            // Act
            var result = await _formService.GetAllFormsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region PublishFormAsync Tests

        [Fact]
        public async Task PublishFormAsync_WithDraftForm_ShouldPublishSuccessfully()
        {
            // Arrange
            var formId = "form-id";
            var existingForm = new Form
            {
                Id = formId,
                Title = "Test Form",
                Description = "Description",
                Header = "Header",
                HeaderDescription = "Header Desc",
                Status = "draft",
                CreatedAt = DateTime.UtcNow,
                Config = new FormConfig(),
                Questions = new List<FormQuestion>()
            };

            _mockFormRepository.Setup(r => r.GetFormByIdAsync(formId))
                .ReturnsAsync(existingForm);
            _mockFormRepository.Setup(r => r.UpdateFormAsync(It.IsAny<Form>()))
                .ReturnsAsync((Form f) => f);

            // Act
            var result = await _formService.PublishFormAsync(formId, "publisher");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("published", result.Status);
            _mockFormRepository.Verify(r => r.UpdateFormAsync(It.Is<Form>(f => 
                f.Status == "published" && 
                f.PublishedBy == "publisher" && 
                f.PublishedAt != null)), Times.Once);
        }

        [Fact]
        public async Task PublishFormAsync_WithNonExistentForm_ShouldThrowException()
        {
            // Arrange
            _mockFormRepository.Setup(r => r.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((Form)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _formService.PublishFormAsync("non-existent", "publisher"));
            Assert.Equal("Form not found", exception.Message);
        }

        [Fact]
        public async Task PublishFormAsync_WithAlreadyPublishedForm_ShouldThrowException()
        {
            // Arrange
            var formId = "form-id";
            var existingForm = new Form
            {
                Id = formId,
                Status = "published",
                PublishedAt = DateTime.UtcNow.AddDays(-1)
            };

            _mockFormRepository.Setup(r => r.GetFormByIdAsync(formId))
                .ReturnsAsync(existingForm);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _formService.PublishFormAsync(formId, "publisher"));
            Assert.Equal("Form is already published", exception.Message);
        }

        [Fact]
        public async Task PublishFormAsync_WithPublishedUppercaseStatus_ShouldThrowException()
        {
            // Arrange
            var formId = "form-id";
            var existingForm = new Form
            {
                Id = formId,
                Status = "PUBLISHED"
            };

            _mockFormRepository.Setup(r => r.GetFormByIdAsync(formId))
                .ReturnsAsync(existingForm);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _formService.PublishFormAsync(formId, "publisher"));
            Assert.Equal("Form is already published", exception.Message);
        }

        #endregion

        #region UpdateFormAsync Tests

        [Fact]
        public async Task UpdateFormAsync_WithDraftForm_ShouldUpdateSuccessfully()
        {
            // Arrange
            var formId = "form-id";
            var existingForm = new Form
            {
                Id = formId,
                Title = "Old Title",
                Status = "draft",
                Questions = new List<FormQuestion>()
            };

            var updateDto = new UpdateFormDto
            {
                Id = formId,
                Title = "New Title",
                Description = "New Description",
                Header = "New Header",
                HeaderDescription = "New Header Desc",
                Mode = "draft",
                Config = new FormConfigDto { AllowMultipleSubmissions = true },
                Questions = new List<FormQuestionDto>
                {
                    new FormQuestionDto
                    {
                        Id = null, // New question
                        Type = "text",
                        Label = "New Question",
                        IsRequired = true,
                        Order = 1
                    }
                }
            };

            _mockFormRepository.Setup(r => r.GetFormByIdAsync(formId))
                .ReturnsAsync(existingForm);
            _mockFormRepository.Setup(r => r.UpdateFormAsync(It.IsAny<Form>()))
                .ReturnsAsync((Form f) => f);

            // Act
            var result = await _formService.UpdateFormAsync(updateDto, "updater");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Title", result.Title);
            Assert.Equal("draft", result.Status);
            _mockFormRepository.Verify(r => r.UpdateFormAsync(It.Is<Form>(f => 
                f.Title == "New Title" && 
                f.Config.AllowMultipleSubmissions == true)), Times.Once);
        }

        [Fact]
        public async Task UpdateFormAsync_WithNonExistentForm_ShouldThrowException()
        {
            // Arrange
            var updateDto = new UpdateFormDto
            {
                Id = "non-existent",
                Title = "Title",
                Description = "Desc",
                Header = "Header",
                HeaderDescription = "Header Desc"
            };

            _mockFormRepository.Setup(r => r.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((Form)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _formService.UpdateFormAsync(updateDto, "updater"));
            Assert.Equal("Form not found", exception.Message);
        }

        [Fact]
        public async Task UpdateFormAsync_WithPublishedFormAndDraftMode_ShouldThrowException()
        {
            // Arrange
            var formId = "form-id";
            var existingForm = new Form
            {
                Id = formId,
                Status = "published"
            };

            var updateDto = new UpdateFormDto
            {
                Id = formId,
                Title = "New Title",
                Description = "Desc",
                Header = "Header",
                HeaderDescription = "Header Desc",
                Mode = "draft"
            };

            _mockFormRepository.Setup(r => r.GetFormByIdAsync(formId))
                .ReturnsAsync(existingForm);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _formService.UpdateFormAsync(updateDto, "updater"));
            Assert.Equal("Cannot edit a published form. Published forms are read-only.", exception.Message);
        }

        [Fact]
        public async Task UpdateFormAsync_WithPublishedFormAndNullMode_ShouldThrowException()
        {
            // Arrange
            var formId = "form-id";
            var existingForm = new Form
            {
                Id = formId,
                Status = "PUBLISHED" // Test with uppercase
            };

            var updateDto = new UpdateFormDto
            {
                Id = formId,
                Title = "New Title",
                Description = "Desc",
                Header = "Header",
                HeaderDescription = "Header Desc",
                Mode = null
            };

            _mockFormRepository.Setup(r => r.GetFormByIdAsync(formId))
                .ReturnsAsync(existingForm);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _formService.UpdateFormAsync(updateDto, "updater"));
            Assert.Equal("Cannot edit a published form. Published forms are read-only.", exception.Message);
        }

        [Fact]
        public async Task UpdateFormAsync_WithPublishedFormAndPublishMode_ShouldAllowUpdate()
        {
            // Arrange
            var formId = "form-id";
                        var existingForm = new Form
            {
                Id = formId,
                Status = "published",
                Questions = new List<FormQuestion>()
            };

            var updateDto = new UpdateFormDto
            {
                Id = formId,
                Title = "Updated Title",
                Description = "Updated Desc",
                Header = "Updated Header",
                HeaderDescription = "Updated Header Desc",
                Mode = "publish",
                Config = new FormConfigDto(),
                Questions = new List<FormQuestionDto>()
            };

            _mockFormRepository.Setup(r => r.GetFormByIdAsync(formId))
                .ReturnsAsync(existingForm);
            _mockFormRepository.Setup(r => r.UpdateFormAsync(It.IsAny<Form>()))
                .ReturnsAsync((Form f) => f);

            // Act
            var result = await _formService.UpdateFormAsync(updateDto, "updater");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Title", result.Title);
            _mockFormRepository.Verify(r => r.UpdateFormAsync(It.IsAny<Form>()), Times.Once);
        }

        [Fact]
        public async Task UpdateFormAsync_WithInvalidMode_ShouldThrowArgumentException()
        {
            // Arrange
            var formId = "form-id";
            var existingForm = new Form
            {
                Id = formId,
                Status = "draft"
            };

            var updateDto = new UpdateFormDto
            {
                Id = formId,
                Title = "Title",
                Description = "Desc",
                Header = "Header",
                HeaderDescription = "Header Desc",
                Mode = "invalid"
            };

            _mockFormRepository.Setup(r => r.GetFormByIdAsync(formId))
                .ReturnsAsync(existingForm);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _formService.UpdateFormAsync(updateDto, "updater"));
            Assert.Equal("Mode must be either 'draft' or 'publish'", exception.Message);
        }

        [Fact]
        public async Task UpdateFormAsync_WithDraftFormAndPublishMode_ShouldPublish()
        {
            // Arrange
            var formId = "form-id";
            var existingForm = new Form
            {
                Id = formId,
                Status = "draft",
                Questions = new List<FormQuestion>()
            };

            var updateDto = new UpdateFormDto
            {
                Id = formId,
                Title = "Title",
                Description = "Desc",
                Header = "Header",
                HeaderDescription = "Header Desc",
                Mode = "Publish", // Test with uppercase
                Config = new FormConfigDto(),
                Questions = new List<FormQuestionDto>()
            };

            Form capturedForm = null;
            _mockFormRepository.Setup(r => r.GetFormByIdAsync(formId))
                .ReturnsAsync(existingForm);
            _mockFormRepository.Setup(r => r.UpdateFormAsync(It.IsAny<Form>()))
                .Callback<Form>(f => capturedForm = f)
                .ReturnsAsync((Form f) => f);

            // Act
            var result = await _formService.UpdateFormAsync(updateDto, "publisher");

            // Assert
            Assert.NotNull(capturedForm);
            Assert.Equal("published", capturedForm.Status);
            Assert.Equal("publisher", capturedForm.PublishedBy);
            Assert.NotNull(capturedForm.PublishedAt);
        }

        [Fact]
        public async Task UpdateFormAsync_WithExistingQuestions_ShouldPreserveOptionIds()
        {
            // Arrange
            var formId = "form-id";
            var existingForm = new Form
            {
                Id = formId,
                Status = "draft",
                Questions = new List<FormQuestion>
                {
                    new FormQuestion
                    {
                        Id = "q1",
                        Type = "dropdown",
                        Label = "Old Label",
                        Options = new List<QuestionOption>
                        {
                            new QuestionOption { Id = "opt1", Value = "Option 1", Order = 0, IsActive = true },
                            new QuestionOption { Id = "opt2", Value = "Option 2", Order = 1, IsActive = true }
                        }
                    }
                }
            };

            var updateDto = new UpdateFormDto
            {
                Id = formId,
                Title = "Title",
                Description = "Desc",
                Header = "Header",
                HeaderDescription = "Header Desc",
                Config = new FormConfigDto(),
                Questions = new List<FormQuestionDto>
                {
                    new FormQuestionDto
                    {
                        Id = "q1",
                        Type = "dropdown",
                        Label = "New Label",
                        IsRequired = true,
                        IsDescription = false,
                        IsMultiSelect = true,
                        Order = 1,
                        Options = new List<string> { "Option 1", "Option 2", "Option 3" }
                    }
                }
            };

            Form capturedForm = null;
            _mockFormRepository.Setup(r => r.GetFormByIdAsync(formId))
                .ReturnsAsync(existingForm);
            _mockFormRepository.Setup(r => r.UpdateFormAsync(It.IsAny<Form>()))
                .Callback<Form>(f => capturedForm = f)
                .ReturnsAsync((Form f) => f);

            // Act
            await _formService.UpdateFormAsync(updateDto, "updater");

            // Assert
            Assert.NotNull(capturedForm);
            var question = capturedForm.Questions[0];
            Assert.Equal("New Label", question.Label);
            Assert.Equal(3, question.Options.Count);
            Assert.Equal("opt1", question.Options[0].Id); // Preserved ID
            Assert.Equal("opt2", question.Options[1].Id); // Preserved ID
            Assert.NotEqual("opt1", question.Options[2].Id); // New ID for new option
        }

        [Fact]
        public async Task UpdateFormAsync_WithEmptyQuestionId_ShouldGenerateNewId()
        {
            // Arrange
            var formId = "form-id";
            var existingForm = new Form
            {
                Id = formId,
                Status = "draft",
                Questions = new List<FormQuestion>()
            };

            var updateDto = new UpdateFormDto
            {
                Id = formId,
                Title = "Title",
                Description = "Desc",
                Header = "Header",
                HeaderDescription = "Header Desc",
                Config = new FormConfigDto(),
                Questions = new List<FormQuestionDto>
                {
                    new FormQuestionDto
                    {
                        Id = "", // Empty ID
                        Type = "text",
                        Label = "Question",
                        Order = 1
                    }
                }
            };

            Form capturedForm = null;
            _mockFormRepository.Setup(r => r.GetFormByIdAsync(formId))
                .ReturnsAsync(existingForm);
            _mockFormRepository.Setup(r => r.UpdateFormAsync(It.IsAny<Form>()))
                .Callback<Form>(f => capturedForm = f)
                .ReturnsAsync((Form f) => f);

            // Act
            await _formService.UpdateFormAsync(updateDto, "updater");

            // Assert
            Assert.NotNull(capturedForm);
            Assert.NotEmpty(capturedForm.Questions[0].Id);
        }

        [Fact]
        public async Task UpdateFormAsync_WithNullQuestions_ShouldUseEmptyList()
        {
            // Arrange
            var formId = "form-id";
            var existingForm = new Form
            {
                Id = formId,
                Status = "draft",
                Questions = new List<FormQuestion> { new FormQuestion { Id = "q1" } }
            };

            var updateDto = new UpdateFormDto
            {
                Id = formId,
                Title = "Title",
                Description = "Desc",
                Header = "Header",
                HeaderDescription = "Header Desc",
                Config = new FormConfigDto(),
                Questions = null
            };

            Form capturedForm = null;
            _mockFormRepository.Setup(r => r.GetFormByIdAsync(formId))
                .ReturnsAsync(existingForm);
            _mockFormRepository.Setup(r => r.UpdateFormAsync(It.IsAny<Form>()))
                .Callback<Form>(f => capturedForm = f)
                .ReturnsAsync((Form f) => f);

            // Act
            await _formService.UpdateFormAsync(updateDto, "updater");

            // Assert
            Assert.NotNull(capturedForm);
            Assert.Empty(capturedForm.Questions);
        }

        [Fact]
        public async Task UpdateFormAsync_WithRadioOptionsUpdate_ShouldTransformCorrectly()
        {
            // Arrange
            var formId = "form-id";
            var existingForm = new Form
            {
                Id = formId,
                Status = "draft",
                Questions = new List<FormQuestion>()
            };

            var updateDto = new UpdateFormDto
            {
                Id = formId,
                Title = "Title",
                Description = "Desc",
                Header = "Header",
                HeaderDescription = "Header Desc",
                Config = new FormConfigDto(),
                Questions = new List<FormQuestionDto>
                {
                    new FormQuestionDto
                    {
                        Type = "radio",
                        Label = "Radio Question",
                        Order = 1,
                        Options = new List<string> { "Yes", "No" }
                    }
                }
            };

            Form capturedForm = null;
            _mockFormRepository.Setup(r => r.GetFormByIdAsync(formId))
                .ReturnsAsync(existingForm);
            _mockFormRepository.Setup(r => r.UpdateFormAsync(It.IsAny<Form>()))
                .Callback<Form>(f => capturedForm = f)
                .ReturnsAsync((Form f) => f);

            // Act
            await _formService.UpdateFormAsync(updateDto, "updater");

            // Assert
            Assert.NotNull(capturedForm);
            Assert.Equal(2, capturedForm.Questions[0].Options.Count);
            Assert.True(capturedForm.Questions[0].Options.All(o => o.IsActive));
        }

        [Fact]
        public async Task UpdateFormAsync_WithCheckboxOptionsUpdate_ShouldTransformCorrectly()
        {
            // Arrange
            var formId = "form-id";
            var existingForm = new Form
            {
                Id = formId,
                Status = "draft",
                Questions = new List<FormQuestion>()
            };

            var updateDto = new UpdateFormDto
            {
                Id = formId,
                Title = "Title",
                Description = "Desc",
                Header = "Header",
                HeaderDescription = "Header Desc",
                Config = new FormConfigDto(),
                Questions = new List<FormQuestionDto>
                {
                    new FormQuestionDto
                    {
                        Type = "checkbox",
                        Label = "Checkbox Question",
                        Order = 1,
                        Options = null // Test with null options
                    }
                }
            };

            Form capturedForm = null;
            _mockFormRepository.Setup(r => r.GetFormByIdAsync(formId))
                .ReturnsAsync(existingForm);
            _mockFormRepository.Setup(r => r.UpdateFormAsync(It.IsAny<Form>()))
                .Callback<Form>(f => capturedForm = f)
                .ReturnsAsync((Form f) => f);

            // Act
            await _formService.UpdateFormAsync(updateDto, "updater");

            // Assert
            Assert.NotNull(capturedForm);
            Assert.Empty(capturedForm.Questions[0].Options);
        }

        [Fact]
        public async Task UpdateFormAsync_WithNonDropdownType_ShouldNotTransformOptions()
        {
            // Arrange
            var formId = "form-id";
            var existingForm = new Form
            {
                Id = formId,
                Status = "draft",
                Questions = new List<FormQuestion>()
            };

            var updateDto = new UpdateFormDto
            {
                Id = formId,
                Title = "Title",
                Description = "Desc",
                Header = "Header",
                HeaderDescription = "Header Desc",
                Config = new FormConfigDto(),
                Questions = new List<FormQuestionDto>
                {
                    new FormQuestionDto
                    {
                        Type = "date",
                        Label = "Date Question",
                        Order = 1,
                        Options = new List<string> { "Should", "Be", "Ignored" }
                    }
                }
            };

            Form capturedForm = null;
            _mockFormRepository.Setup(r => r.GetFormByIdAsync(formId))
                .ReturnsAsync(existingForm);
            _mockFormRepository.Setup(r => r.UpdateFormAsync(It.IsAny<Form>()))
                .Callback<Form>(f => capturedForm = f)
                .ReturnsAsync((Form f) => f);

            // Act
            await _formService.UpdateFormAsync(updateDto, "updater");

            // Assert
            Assert.NotNull(capturedForm);
            Assert.Empty(capturedForm.Questions[0].Options);
        }

        #endregion

        #region DeleteFormAsync Tests

        [Fact]
        public async Task DeleteFormAsync_WithExistingDraftForm_ShouldReturnTrue()
        {
            // Arrange
            var formId = "form-id";
            var form = new Form
            {
                Id = formId,
                Status = "draft"
            };

            _mockFormRepository.Setup(r => r.GetFormByIdAsync(formId))
                .ReturnsAsync(form);
            _mockFormRepository.Setup(r => r.DeleteFormAsync(formId))
                .ReturnsAsync(true);

            // Act
            var result = await _formService.DeleteFormAsync(formId);

            // Assert
            Assert.True(result);
            _mockFormRepository.Verify(r => r.DeleteFormAsync(formId), Times.Once);
        }

        [Fact]
        public async Task DeleteFormAsync_WithNonExistentForm_ShouldReturnFalse()
        {
            // Arrange
            _mockFormRepository.Setup(r => r.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((Form)null);

            // Act
            var result = await _formService.DeleteFormAsync("non-existent");

            // Assert
            Assert.False(result);
            _mockFormRepository.Verify(r => r.DeleteFormAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteFormAsync_WithPublishedForm_ShouldReturnFalse()
        {
            // Arrange
            var formId = "form-id";
            var form = new Form
            {
                Id = formId,
                Status = "published"
            };

            _mockFormRepository.Setup(r => r.GetFormByIdAsync(formId))
                .ReturnsAsync(form);

            // Act
            var result = await _formService.DeleteFormAsync(formId);

            // Assert
            Assert.False(result);
            _mockFormRepository.Verify(r => r.DeleteFormAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteFormAsync_WithPublishedUppercaseStatus_ShouldReturnFalse()
        {
            // Arrange
            var formId = "form-id";
            var form = new Form
            {
                Id = formId,
                Status = "PUBLISHED"
            };

            _mockFormRepository.Setup(r => r.GetFormByIdAsync(formId))
                .ReturnsAsync(form);

            // Act
            var result = await _formService.DeleteFormAsync(formId);

            // Assert
            Assert.False(result);
            _mockFormRepository.Verify(r => r.DeleteFormAsync(It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region CanUserSubmitFormAsync Tests

        [Fact]
        public async Task CanUserSubmitFormAsync_WithNonExistentForm_ShouldReturnFalse()
        {
            // Arrange
            _mockFormRepository.Setup(r => r.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((Form)null);

            // Act
            var result = await _formService.CanUserSubmitFormAsync("form-id", "user-id");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanUserSubmitFormAsync_WithDraftForm_ShouldReturnFalse()
        {
            // Arrange
            var form = new Form
            {
                Id = "form-id",
                Status = "draft",
                Config = new FormConfig()
            };

            _mockFormRepository.Setup(r => r.GetFormByIdAsync("form-id"))
                .ReturnsAsync(form);

            // Act
            var result = await _formService.CanUserSubmitFormAsync("form-id", "user-id");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanUserSubmitFormAsync_WithPublishedFormAndMultipleSubmissions_ShouldReturnTrue()
        {
            // Arrange
            var form = new Form
            {
                Id = "form-id",
                Status = "published",
                Config = new FormConfig { AllowMultipleSubmissions = true }
            };

            _mockFormRepository.Setup(r => r.GetFormByIdAsync("form-id"))
                .ReturnsAsync(form);

            // Act
            var result = await _formService.CanUserSubmitFormAsync("form-id", "user-id");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CanUserSubmitFormAsync_WithPublishedFormAndNoMultipleSubmissions_ShouldReturnTrue()
        {
            // Arrange
            var form = new Form
            {
                Id = "form-id",
                Status = "PUBLISHED", // Test with uppercase
                Config = new FormConfig { AllowMultipleSubmissions = false }
            };

            _mockFormRepository.Setup(r => r.GetFormByIdAsync("form-id"))
                .ReturnsAsync(form);

            // Act
            var result = await _formService.CanUserSubmitFormAsync("form-id", "user-id");

            // Assert
            Assert.True(result); // Returns true as placeholder
        }

        #endregion

        #region MapToResponse Tests

        [Fact]
        public async Task MapToResponse_WithRadioQuestionAndActiveOptions_ShouldMapCorrectly()
        {
            // Arrange
            var form = new Form
            {
                Id = "form-id",
                Title = "Test",
                Description = "Desc",
                Header = "Header",
                HeaderDescription = "Header Desc",
                Status = "published",
                CreatedAt = DateTime.UtcNow,
                PublishedAt = DateTime.UtcNow,
                Config = new FormConfig(),
                Questions = new List<FormQuestion>
                {
                    new FormQuestion
                    {
                        Id = "q1",
                        Type = "Radio", // Test with different case
                        Label = "Radio Q",
                        IsRequired = true,
                        Order = 1,
                        Options = new List<QuestionOption>
                        {
                            new QuestionOption { Id = "opt1", Value = "Yes", IsActive = true },
                            new QuestionOption { Id = "opt2", Value = "No", IsActive = false }
                        }
                    }
                }
            };

            _mockFormRepository.Setup(r => r.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);

            // Act
            var result = await _formService.GetFormByIdAsync("form-id");

            // Assert
            Assert.NotNull(result.Questions[0].Options);
            Assert.Single(result.Questions[0].Options); // Only active option
            Assert.Equal("opt1", result.Questions[0].Options[0].Id);
        }

        [Fact]
        public async Task MapToResponse_WithCheckboxQuestionAndNoActiveOptions_ShouldReturnEmptyOptions()
        {
            // Arrange
            var form = new Form
            {
                Id = "form-id",
                Title = "Test",
                Description = "Desc",
                Header = "Header",
                HeaderDescription = "Header Desc",
                Status = "draft",
                CreatedAt = DateTime.UtcNow,
                Config = new FormConfig(),
                Questions = new List<FormQuestion>
                {
                    new FormQuestion
                    {
                        Id = "q1",
                        Type = "CHECKBOX", // Test with uppercase
                        Label = "Checkbox Q",
                        Options = new List<QuestionOption>
                        {
                            new QuestionOption { Id = "opt1", Value = "A", IsActive = false },
                            new QuestionOption { Id = "opt2", Value = "B", IsActive = false }
                        }
                    }
                }
            };

            _mockFormRepository.Setup(r => r.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);

            // Act
            var result = await _formService.GetFormByIdAsync("form-id");

            // Assert
            Assert.Empty(result.Questions[0].Options);
        }

        [Fact]
        public async Task MapToResponse_WithTextQuestionAndOptions_ShouldReturnNullOptions()
        {
            // Arrange
            var form = new Form
            {
                Id = "form-id",
                Title = "Test",
                Description = "Desc",
                Header = "Header",
                HeaderDescription = "Header Desc",
                Status = "draft",
                CreatedAt = DateTime.UtcNow,
                Config = new FormConfig(),
                Questions = new List<FormQuestion>
                {
                    new FormQuestion
                    {
                        Id = "q1",
                        Type = "text",
                        Label = "Text Q",
                        Options = new List<QuestionOption>
                        {
                            new QuestionOption { Id = "opt1", Value = "Should be ignored", IsActive = true }
                        }
                    }
                }
            };

            _mockFormRepository.Setup(r => r.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);

            // Act
            var result = await _formService.GetFormByIdAsync("form-id");

            // Assert
            Assert.Null(result.Questions[0].Options);
        }

        [Fact]
        public async Task MapToResponse_WithNullOptions_ShouldReturnNull()
        {
            // Arrange
            var form = new Form
            {
                Id = "form-id",
                Title = "Test",
                Description = "Desc",
                Header = "Header",
                HeaderDescription = "Header Desc",
                Status = "draft",
                CreatedAt = DateTime.UtcNow,
                Config = new FormConfig(),
                Questions = new List<FormQuestion>
                {
                    new FormQuestion
                    {
                        Id = "q1",
                        Type = "dropdown",
                        Label = "Dropdown Q",
                        Options = null
                    }
                }
            };

            _mockFormRepository.Setup(r => r.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);

            // Act
            var result = await _formService.GetFormByIdAsync("form-id");

            // Assert
            Assert.Null(result.Questions[0].Options);
        }

        [Fact]
        public async Task MapToResponse_WithEmptyOptions_ShouldReturnNull()
        {
            // Arrange
            var form = new Form
            {
                Id = "form-id",
                Title = "Test",
                Description = "Desc",
                Header = "Header",
                HeaderDescription = "Header Desc",
                Status = "draft",
                CreatedAt = DateTime.UtcNow,
                Config = new FormConfig(),
                Questions = new List<FormQuestion>
                {
                    new FormQuestion
                    {
                        Id = "q1",
                        Type = "dropdown",
                        Label = "Dropdown Q",
                        Options = new List<QuestionOption>()
                    }
                }
            };

            _mockFormRepository.Setup(r => r.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);

            // Act
            var result = await _formService.GetFormByIdAsync("form-id");

            // Assert
            Assert.Null(result.Questions[0].Options);
        }

        #endregion
    }
}
