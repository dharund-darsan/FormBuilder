using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FormsBackend.DTOs;
using FormsBackend.Models.Mongo;
using FormsBackend.Models.Sql;
using FormsBackend.Repositories;
using FormsBackend.Services;
using Moq;
using Xunit;

namespace FormsBackend.Tests.Service
{
    public class SubmissionServiceTest
    {
        private readonly Mock<ISubmissionRepository> _mockSubmissionRepository;
        private readonly Mock<IFormRepository> _mockFormRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly SubmissionService _submissionService;

        public SubmissionServiceTest()
        {
            _mockSubmissionRepository = new Mock<ISubmissionRepository>();
            _mockFormRepository = new Mock<IFormRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _submissionService = new SubmissionService(
                _mockSubmissionRepository.Object,
                _mockFormRepository.Object,
                _mockUserRepository.Object
            );
        }

        #region SubmitFormAsync Tests

        [Fact]
        public async Task SubmitFormAsync_FormNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            var dto = new SubmitFormDto
            {
                FormId = "form123",
                Answers = new List<SubmissionAnswerDto>()
            };
            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((Form)null);

            // Act & Assert
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(() => _submissionService.SubmitFormAsync(dto, 1));
            Assert.Equal("Form not found", exception.Message);
        }

        [Fact]
        public async Task SubmitFormAsync_FormNotPublished_ThrowsInvalidOperationException()
        {
            // Arrange
            var dto = new SubmitFormDto
            {
                FormId = "form123",
                Answers = new List<SubmissionAnswerDto>()
            };
            var form = new Form
            {
                Id = "form123",
                Status = "draft",
                Config = new FormConfig(),
                Questions = new List<FormQuestion>()
            };
            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);

            // Act & Assert
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(() => _submissionService.SubmitFormAsync(dto, 1));
            Assert.Equal("Form is not published. Only published forms can be submitted.", exception.Message);
        }

        [Fact]
        public async Task SubmitFormAsync_MultipleSubmissionsNotAllowed_UserAlreadySubmitted_ThrowsException()
        {
            // Arrange
            var dto = new SubmitFormDto
            {
                FormId = "form123",
                Answers = new List<SubmissionAnswerDto>()
            };
            var form = new Form
            {
                Id = "form123",
                Status = "published",
                Config = new FormConfig { AllowMultipleSubmissions = false },
                Questions = new List<FormQuestion>()
            };
            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);
            _mockSubmissionRepository.Setup(x => x.HasUserSubmittedFormAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act & Assert
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(() => _submissionService.SubmitFormAsync(dto, 1));
            Assert.Equal("You have already submitted this form. Multiple submissions are not allowed.",
                exception.Message);
        }

        [Fact]
        public async Task SubmitFormAsync_UserNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            var dto = new SubmitFormDto
            {
                FormId = "form123",
                Answers = new List<SubmissionAnswerDto>()
            };
            var form = new Form
            {
                Id = "form123",
                Status = "published",
                Config = new FormConfig { AllowMultipleSubmissions = true },
                Questions = new List<FormQuestion>()
            };
            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);
            _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((User)null);

            // Act & Assert
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(() => _submissionService.SubmitFormAsync(dto, 1));
            Assert.Equal("User not found", exception.Message);
        }

        [Fact]
        public async Task SubmitFormAsync_QuestionNotFoundInForm_ThrowsInvalidOperationException()
        {
            // Arrange
            var dto = new SubmitFormDto
            {
                FormId = "form123",
                Answers = new List<SubmissionAnswerDto>
                {
                    new SubmissionAnswerDto
                    {
                        QuestionId = "q1",
                        AnswerType = "text",
                        AnswerText = "answer"
                    }
                }
            };
            var form = new Form
            {
                Id = "form123",
                Status = "published",
                Config = new FormConfig { AllowMultipleSubmissions = true },
                Questions = new List<FormQuestion>()
            };
            var user = new User { Id = 1, Username = "testuser" };

            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);
            _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(user);

            // Act & Assert
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(() => _submissionService.SubmitFormAsync(dto, 1));
            Assert.Equal("Question q1 not found in form", exception.Message);
        }

        [Fact]
        public async Task SubmitFormAsync_FileUpload_Success()
        {
            // Arrange
            var dto = new SubmitFormDto
            {
                FormId = "form123",
                Answers = new List<SubmissionAnswerDto>
                {
                    new SubmissionAnswerDto
                    {
                        QuestionId = "q1",
                        AnswerType = "file",
                        File = new FileUploadDto
                        {
                            FileName = "test.pdf",
                            FileData = "base64data",
                            MimeType = "application/pdf"
                        }
                    }
                }
            };
            var form = new Form
            {
                Id = "form123",
                Title = "Test Form",
                Status = "published",
                Config = new FormConfig { AllowMultipleSubmissions = true },
                Questions = new List<FormQuestion>
                {
                    new FormQuestion { Id = "q1", Type = "file", Label = "Upload File" }
                }
            };
            var user = new User { Id = 1, Username = "testuser" };
            var savedSubmission = new FormSubmission
            {
                Id = 1,
                FormId = "form123",
                UserId = 1,
                SubmittedAt = DateTime.UtcNow,
                Answers = new List<SubmissionAnswer>(),
                Files = new List<SubmissionFile>
                {
                    new SubmissionFile
                    {
                        Id = 1,
                        QuestionId = "q1",
                        FileName = "test.pdf",
                        FileData = "base64data",
                        MimeType = "application/pdf"
                    }
                }
            };

            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);
            _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(user);
            _mockSubmissionRepository.Setup(x => x.CreateSubmissionAsync(It.IsAny<FormSubmission>()))
                .ReturnsAsync(savedSubmission);

            // Act
            var result = await _submissionService.SubmitFormAsync(dto, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("form123", result.FormId);
        }

        [Fact]
        public async Task SubmitFormAsync_DropdownWithSelectedOptionIds_Success()
        {
            // Arrange
            var selectedIds = new List<string> { "opt1", "opt2" };
            var dto = new SubmitFormDto
            {
                FormId = "form123",
                Answers = new List<SubmissionAnswerDto>
                {
                    new SubmissionAnswerDto
                    {
                        QuestionId = "q1",
                        AnswerType = "dropdown",
                        SelectedOptionIds = selectedIds
                    }
                }
            };
            var form = new Form
            {
                Id = "form123",
                Title = "Test Form",
                Status = "published",
                Config = new FormConfig { AllowMultipleSubmissions = true },
                Questions = new List<FormQuestion>
                {
                    new FormQuestion
                    {
                        Id = "q1",
                        Type = "dropdown",
                        Label = "Select Option",
                        Options = new List<QuestionOption>
                        {
                            new QuestionOption { Id = "opt1", Value = "Option 1" },
                            new QuestionOption { Id = "opt2", Value = "Option 2" }
                        }
                    }
                }
            };
            var user = new User { Id = 1, Username = "testuser" };
            var savedSubmission = new FormSubmission
            {
                Id = 1,
                FormId = "form123",
                UserId = 1,
                SubmittedAt = DateTime.UtcNow,
                Answers = new List<SubmissionAnswer>
                {
                    new SubmissionAnswer
                    {
                        QuestionId = "q1",
                        AnswerType = "dropdown",
                        AnswerText = JsonSerializer.Serialize(selectedIds)
                    }
                },
                Files = new List<SubmissionFile>()
            };

            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);
            _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(user);
            _mockSubmissionRepository.Setup(x => x.CreateSubmissionAsync(It.IsAny<FormSubmission>()))
                .ReturnsAsync(savedSubmission);

            // Act
            var result = await _submissionService.SubmitFormAsync(dto, 1);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Answers);
            var answer = result.Answers.First();
            Assert.Equal(selectedIds, answer.SelectedOptionsIds);
        }

        [Fact]
        public async Task SubmitFormAsync_CheckboxWithSelectedOptionIds_Success()
        {
            // Arrange
            var selectedIds = new List<string> { "opt1", "opt2" };
            var dto = new SubmitFormDto
            {
                FormId = "form123",
                Answers = new List<SubmissionAnswerDto>
                {
                    new SubmissionAnswerDto
                    {
                        QuestionId = "q1",
                        AnswerType = "checkbox",
                        SelectedOptionIds = selectedIds
                    }
                }
            };
            var form = new Form
            {
                Id = "form123",
                Title = "Test Form",
                Status = "published",
                Config = new FormConfig { AllowMultipleSubmissions = true },
                Questions = new List<FormQuestion>
                {
                    new FormQuestion { Id = "q1", Type = "checkbox", Label = "Select Options" }
                }
            };
            var user = new User { Id = 1, Username = "testuser" };
            var savedSubmission = new FormSubmission
            {
                Id = 1,
                FormId = "form123",
                UserId = 1,
                Answers = new List<SubmissionAnswer>
                {
                    new SubmissionAnswer
                    {
                        QuestionId = "q1",
                        AnswerType = "checkbox",
                        AnswerText = JsonSerializer.Serialize(selectedIds)
                    }
                },
                Files = new List<SubmissionFile>()
            };

            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);
            _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(user);
            _mockSubmissionRepository.Setup(x => x.CreateSubmissionAsync(It.IsAny<FormSubmission>()))
                .ReturnsAsync(savedSubmission);

            // Act
            var result = await _submissionService.SubmitFormAsync(dto, 1);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task SubmitFormAsync_RadioWithSelectedOptionIds_Success()
        {
            // Arrange
            var selectedIds = new List<string> { "opt1" };
            var dto = new SubmitFormDto
            {
                FormId = "form123",
                Answers = new List<SubmissionAnswerDto>
                {
                    new SubmissionAnswerDto
                    {
                        QuestionId = "q1",
                        AnswerType = "radio",
                        SelectedOptionIds = selectedIds
                    }
                }
            };
            var form = new Form
            {
                Id = "form123",
                Title = "Test Form",
                Status = "published",
                Config = new FormConfig { AllowMultipleSubmissions = true },
                Questions = new List<FormQuestion>
                {
                    new FormQuestion { Id = "q1", Type = "radio", Label = "Select One" }
                }
            };
            var user = new User { Id = 1, Username = "testuser" };
            var savedSubmission = new FormSubmission
            {
                Id = 1,
                FormId = "form123",
                UserId = 1,
                Answers = new List<SubmissionAnswer>
                {
                    new SubmissionAnswer
                    {
                        QuestionId = "q1",
                        AnswerType = "radio",
                        AnswerText = JsonSerializer.Serialize(selectedIds)
                    }
                },
                Files = new List<SubmissionFile>()
            };

            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);
            _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(user);
            _mockSubmissionRepository.Setup(x => x.CreateSubmissionAsync(It.IsAny<FormSubmission>()))
                .ReturnsAsync(savedSubmission);

            // Act
            var result = await _submissionService.SubmitFormAsync(dto, 1);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task SubmitFormAsync_TextAnswer_Success()
        {
            // Arrange
            var dto = new SubmitFormDto
            {
                FormId = "form123",
                Answers = new List<SubmissionAnswerDto>
                {
                    new SubmissionAnswerDto
                    {
                        QuestionId = "q1",
                        AnswerType = "text",
                        AnswerText = "Some text answer"
                    }
                }
            };
            var form = new Form
            {
                Id = "form123",
                Title = "Test Form",
                Status = "published",
                Config = new FormConfig { AllowMultipleSubmissions = true },
                Questions = new List<FormQuestion>
                {
                    new FormQuestion { Id = "q1", Type = "text", Label = "Enter Text" }
                }
            };
            var user = new User { Id = 1, Username = "testuser" };
            var savedSubmission = new FormSubmission
            {
                Id = 1,
                FormId = "form123",
                UserId = 1,
                Answers = new List<SubmissionAnswer>
                {
                    new SubmissionAnswer
                    {
                        QuestionId = "q1",
                        AnswerType = "text",
                        AnswerText = "Some text answer"
                    }
                },
                Files = new List<SubmissionFile>()
            };

            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);
            _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(user);
            _mockSubmissionRepository.Setup(x => x.CreateSubmissionAsync(It.IsAny<FormSubmission>()))
                .ReturnsAsync(savedSubmission);

            // Act
            var result = await _submissionService.SubmitFormAsync(dto, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Some text answer", result.Answers.First().AnswerText);
        }

        [Fact]
        public async Task SubmitFormAsync_NullAnswerText_SetsEmptyString()
        {
            // Arrange
            var dto = new SubmitFormDto
            {
                FormId = "form123",
                Answers = new List<SubmissionAnswerDto>
                {
                    new SubmissionAnswerDto
                    {
                        QuestionId = "q1",
                        AnswerType = "text",
                        AnswerText = null
                    }
                }
            };
            var form = new Form
            {
                Id = "form123",
                Title = "Test Form",
                Status = "published",
                Config = new FormConfig { AllowMultipleSubmissions = true },
                Questions = new List<FormQuestion>
                {
                    new FormQuestion { Id = "q1", Type = "text", Label = "Enter Text" }
                }
            };
            var user = new User { Id = 1, Username = "testuser" };
            var savedSubmission = new FormSubmission
            {
                Id = 1,
                FormId = "form123",
                UserId = 1,
                Answers = new List<SubmissionAnswer>
                {
                    new SubmissionAnswer
                    {
                        QuestionId = "q1",
                        AnswerType = "text",
                        AnswerText = ""
                    }
                },
                Files = new List<SubmissionFile>()
            };

            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);
            _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(user);
            _mockSubmissionRepository.Setup(x => x.CreateSubmissionAsync(It.IsAny<FormSubmission>()))
                .ReturnsAsync(savedSubmission);

            // Act
            var result = await _submissionService.SubmitFormAsync(dto, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("", result.Answers.First().AnswerText);
        }

        #endregion

        #region GetMySubmissionsAsync Tests

        [Fact]
        public async Task GetMySubmissionsAsync_ReturnsSubmissions_Success()
        {
            // Arrange
            var submissions = new List<FormSubmission>
            {
                new FormSubmission
                {
                    Id = 1,
                    FormId = "form1",
                    UserId = 1,
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<SubmissionAnswer> { new SubmissionAnswer() },
                    Files = new List<SubmissionFile> { new SubmissionFile() }
                }
            };
            var form = new Form
            {
                Id = "form1",
                Title = "Test Form",
                Status = "published",
                Questions = new List<FormQuestion>
                {
                    new FormQuestion(),
                    new FormQuestion()
                }
            };

            _mockSubmissionRepository.Setup(x => x.GetSubmissionsByUserIdAsync(It.IsAny<int>()))
                .ReturnsAsync(submissions);
            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);

            // Act
            var result = await _submissionService.GetMySubmissionsAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalSubmissions);
            Assert.Equal(2, result.Submissions.First().TotalQuestions);
            Assert.Equal(2, result.Submissions.First().AnsweredQuestions);
        }

        [Fact]
        public async Task GetMySubmissionsAsync_FormNotFound_SkipsSubmission()
        {
            // Arrange
            var submissions = new List<FormSubmission>
            {
                new FormSubmission
                {
                    Id = 1,
                    FormId = "form1",
                    UserId = 1,
                    SubmittedAt = DateTime.UtcNow
                }
            };

            _mockSubmissionRepository.Setup(x => x.GetSubmissionsByUserIdAsync(It.IsAny<int>()))
                .ReturnsAsync(submissions);
            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((Form)null);

            // Act
            var result = await _submissionService.GetMySubmissionsAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalSubmissions);
            Assert.Empty(result.Submissions);
        }

        #endregion

        #region GetSubmissionDetailsAsync Tests

        [Fact]
        public async Task GetSubmissionDetailsAsync_SubmissionNotFound_ThrowsException()
        {
            // Arrange
            _mockSubmissionRepository.Setup(x => x.GetSubmissionByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((FormSubmission)null);

            // Act & Assert
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    _submissionService.GetSubmissionDetailsAsync(1, 1));
            Assert.Equal("Submission not found", exception.Message);
        }

        [Fact]
        public async Task GetSubmissionDetailsAsync_UserDoesNotOwnSubmission_ThrowsUnauthorizedException()
        {
            // Arrange
            var submission = new FormSubmission
            {
                Id = 1,
                UserId = 2,
                FormId = "form1"
            };

            _mockSubmissionRepository.Setup(x => x.GetSubmissionByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(submission);

            // Act & Assert
            var exception =
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                    _submissionService.GetSubmissionDetailsAsync(1, 1));
            Assert.Equal("You don't have permission to view this submission", exception.Message);
        }

        [Fact]
        public async Task GetSubmissionDetailsAsync_Success()
        {
            // Arrange
            var submission = new FormSubmission
            {
                Id = 1,
                UserId = 1,
                FormId = "form1",
                Answers = new List<SubmissionAnswer>(),
                Files = new List<SubmissionFile>()
            };
            var form = new Form { Id = "form1", Title = "Test Form" };
            var user = new User { Id = 1, Username = "testuser" };

            _mockSubmissionRepository.Setup(x => x.GetSubmissionByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(submission);
            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);
            _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(user);

            // Act
            var result = await _submissionService.GetSubmissionDetailsAsync(1, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
        }

        #endregion

        #region GetFormSubmissionsAsync Tests

        [Fact]
        public async Task GetFormSubmissionsAsync_ReturnsAllSubmissions()
        {
            // Arrange
            var submissions = new List<FormSubmission>
            {
                new FormSubmission
                {
                    Id = 1,
                    UserId = 1,
                    FormId = "form1",
                    Answers = new List<SubmissionAnswer>(),
                    Files = new List<SubmissionFile>()
                },
                new FormSubmission
                {
                    Id = 2,
                    UserId = 2,
                    FormId = "form1",
                    Answers = new List<SubmissionAnswer>(),
                    Files = new List<SubmissionFile>()
                }
            };
            var form = new Form { Id = "form1", Title = "Test Form" };
            var user1 = new User { Id = 1, Username = "user1" };
            var user2 = new User { Id = 2, Username = "user2" };

            _mockSubmissionRepository.Setup(x => x.GetSubmissionsByFormIdAsync(It.IsAny<string>()))
                .ReturnsAsync(submissions);
            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);
            _mockUserRepository.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(user1);
            _mockUserRepository.Setup(x => x.GetByIdAsync(2))
                .ReturnsAsync(user2);

            // Act
            var result = await _submissionService.GetFormSubmissionsAsync("form1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        #endregion

        #region MapToSubmissionResponse Tests

        [Fact]
        public async Task MapToSubmissionResponse_WithInvalidJsonAnswer_TreatsAsPlainText()
        {
            // Arrange
            var submission = new FormSubmission
            {
                Id = 1,
                FormId = "form1",
                UserId = 1,
                Answers = new List<SubmissionAnswer>
                {
                    new SubmissionAnswer
                    {
                        QuestionId = "q1",
                        AnswerType = "dropdown",
                        AnswerText = "invalid json"
                    }
                },
                Files = new List<SubmissionFile>()
            };
            var form = new Form
            {
                Id = "form1",
                Title = "Test Form",
                Questions = new List<FormQuestion>
                {
                    new FormQuestion { Id = "q1", Type = "dropdown", Label = "Question 1" }
                }
            };
            var user = new User { Id = 1, Username = "testuser" };

            _mockSubmissionRepository.Setup(x => x.GetSubmissionByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(submission);
            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);
            _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(user);

            // Act
            var result = await _submissionService.GetSubmissionDetailsAsync(1, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("invalid json", result.Answers.First().AnswerText);
        }

       
        [Fact]
        public async Task MapToSubmissionResponse_WithFileAnswer_IncludesFileDetails()
        {
            // Arrange
            var submission = new FormSubmission
            {
                Id = 1,
                FormId = "form1",
                UserId = 1,
                Answers = new List<SubmissionAnswer>(),
                Files = new List<SubmissionFile>
                {
                    new SubmissionFile
                    {
                        Id = 1,
                        QuestionId = "q1",
                        FileName = "test.pdf",
                        MimeType = "application/pdf",
                        UploadedAt = DateTime.UtcNow
                    }
                }
            };
            var form = new Form
            {
                Id = "form1",
                Title = "Test Form",
                Questions = new List<FormQuestion>
                {
                    new FormQuestion { Id = "q1", Type = "file", Label = "Upload File" }
                }
            };
            var user = new User { Id = 1, Username = "testuser" };

            _mockSubmissionRepository.Setup(x => x.GetSubmissionByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(submission);
            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);
            _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(user);

            // Act
            var result = await _submissionService.GetSubmissionDetailsAsync(1, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Answers);
            var fileAnswer = result.Answers.First();
            Assert.Equal("file", fileAnswer.AnswerType);
            Assert.NotNull(fileAnswer.File);
            Assert.Equal("test.pdf", fileAnswer.File.FileName);
            Assert.Equal(1, fileAnswer.File.Id);
        }

        [Fact]
        public async Task MapToSubmissionResponse_QuestionNotInForm_UsesUnknownQuestion()
        {
            // Arrange
            var submission = new FormSubmission
            {
                Id = 1,
                FormId = "form1",
                UserId = 1,
                Answers = new List<SubmissionAnswer>
                {
                    new SubmissionAnswer
                    {
                        QuestionId = "q999",
                        AnswerType = "text",
                        AnswerText = "answer"
                    }
                },
                Files = new List<SubmissionFile>()
            };
            var form = new Form
            {
                Id = "form1",
                Title = "Test Form",
                Questions = new List<FormQuestion>()
            };
            var user = new User { Id = 1, Username = "testuser" };

            _mockSubmissionRepository.Setup(x => x.GetSubmissionByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(submission);
            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);
            _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(user);

            // Act
            var result = await _submissionService.GetSubmissionDetailsAsync(1, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Unknown Question", result.Answers.First().QuestionLabel);
        }

        [Fact]
        public async Task MapToSubmissionResponse_NullForm_UsesUnknownForm()
        {
            // Arrange
            var submission = new FormSubmission
            {
                Id = 1,
                FormId = "form1",
                UserId = 1,
                Answers = new List<SubmissionAnswer>(),
                Files = new List<SubmissionFile>()
            };
            var user = new User { Id = 1, Username = "testuser" };

            _mockSubmissionRepository.Setup(x => x.GetSubmissionByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(submission);
            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((Form)null);
            _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(user);

            // Act
            var result = await _submissionService.GetSubmissionDetailsAsync(1, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Unknown Form", result.FormTitle);
        }

        [Fact]
        public async Task MapToSubmissionResponse_NullUser_UsesUnknownUser()
        {
            // Arrange
            var submission = new FormSubmission
            {
                Id = 1,
                FormId = "form1",
                UserId = 1,
                Answers = new List<SubmissionAnswer>(),
                Files = new List<SubmissionFile>()
            };
            var form = new Form { Id = "form1", Title = "Test Form" };

            _mockSubmissionRepository.Setup(x => x.GetSubmissionByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(submission);
            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);
            _mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((User)null);

            // Act
            var result = await _submissionService.GetSubmissionDetailsAsync(1, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Unknown User", result.Username);
        }

        #endregion

        #region GetFormFilesAsync Tests

        [Fact]
        public async Task GetFormFilesAsync_FormNotFound_ThrowsException()
        {
            // Arrange
            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((Form)null);

            // Act & Assert
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    _submissionService.GetFormFilesAsync("form1"));
            Assert.Equal("Form not found", exception.Message);
        }

        [Fact]
        public async Task GetFormFilesAsync_ReturnsFiles_Success()
        {
            // Arrange
            var form = new Form
            {
                Id = "form1",
                Title = "Test Form",
                Questions = new List<FormQuestion>
                {
                    new FormQuestion { Id = "q1", Label = "Upload File" }
                }
            };
            var files = new List<SubmissionFile>
            {
                new SubmissionFile
                {
                    Id = 1,
                    SubmissionId = 1,
                    QuestionId = "q1",
                    FileName = "test.pdf",
                    FileData = "YmFzZTY0ZGF0YQ==", // base64 encoded "base64data"
                    MimeType = "application/pdf",
                    UploadedAt = DateTime.UtcNow,
                    Submission = new FormSubmission
                    {
                        UserId = 1,
                        User = new User { Username = "testuser" }
                    }
                }
            };

            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);
            _mockSubmissionRepository.Setup(x => x.GetFilesByFormIdAsync(It.IsAny<string>()))
                .ReturnsAsync(files);

            // Act
            var result = await _submissionService.GetFormFilesAsync("form1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("form1", result.FormId);
            Assert.Equal("Test Form", result.FormTitle);
            Assert.Equal(1, result.TotalFiles);
            Assert.Single(result.Files);
            Assert.Equal("testuser", result.Files.First().SubmittedBy);
        }

        [Fact]
        public async Task GetFormFilesAsync_QuestionNotFound_UsesUnknownQuestion()
        {
            // Arrange
            var form = new Form
            {
                Id = "form1",
                Title = "Test Form",
                Questions = new List<FormQuestion>()
            };
            var files = new List<SubmissionFile>
            {
                new SubmissionFile
                {
                    Id = 1,
                    SubmissionId = 1,
                    QuestionId = "q999",
                    FileName = "test.pdf",
                    FileData = "YmFzZTY0ZGF0YQ==",
                    MimeType = "application/pdf",
                    UploadedAt = DateTime.UtcNow,
                    Submission = new FormSubmission
                    {
                        UserId = 1,
                        User = null
                    }
                }
            };

            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);
            _mockSubmissionRepository.Setup(x => x.GetFilesByFormIdAsync(It.IsAny<string>()))
                .ReturnsAsync(files);

            // Act
            var result = await _submissionService.GetFormFilesAsync("form1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Unknown Question", result.Files.First().QuestionLabel);
            Assert.Equal("Unknown User", result.Files.First().SubmittedBy);
        }

        #endregion

        #region GetFileForDownloadAsync Tests

        [Fact]
        public async Task GetFileForDownloadAsync_FileNotFound_ThrowsException()
        {
            // Arrange
            _mockSubmissionRepository.Setup(x => x.GetFileByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((SubmissionFile)null);

            // Act & Assert
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    _submissionService.GetFileForDownloadAsync(1));
            Assert.Equal("File not found", exception.Message);
        }

        [Fact]
        public async Task GetFileForDownloadAsync_NotAdminAndNotOwner_ThrowsUnauthorizedException()
        {
            // Arrange
            var file = new SubmissionFile
            {
                Id = 1,
                FileData = "YmFzZTY0ZGF0YQ==",
                Submission = new FormSubmission { UserId = 2 }
            };

            _mockSubmissionRepository.Setup(x => x.GetFileByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(file);

            // Act & Assert
            var exception =
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                    _submissionService.GetFileForDownloadAsync(1, 1, false));
            Assert.Equal("You don't have permission to download this file", exception.Message);
        }

        [Fact]
        public async Task GetFileForDownloadAsync_AdminCanDownloadAnyFile()
        {
            // Arrange
            var file = new SubmissionFile
            {
                Id = 1,
                FileName = "test.pdf",
                FileData = "YmFzZTY0ZGF0YQ==",
                MimeType = "application/pdf",
                Submission = new FormSubmission { UserId = 2 }
            };

            _mockSubmissionRepository.Setup(x => x.GetFileByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(file);

            // Act
            var result = await _submissionService.GetFileForDownloadAsync(1, 1, true);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test.pdf", result.FileName);
            Assert.NotNull(result.FileBytes);
        }

        [Fact]
        public async Task GetFileForDownloadAsync_OwnerCanDownloadOwnFile()
        {
            // Arrange
            var file = new SubmissionFile
            {
                Id = 1,
                FileName = "test.pdf",
                FileData = "YmFzZTY0ZGF0YQ==",
                MimeType = "application/pdf",
                Submission = new FormSubmission { UserId = 1 }
            };

            _mockSubmissionRepository.Setup(x => x.GetFileByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(file);

            // Act
            var result = await _submissionService.GetFileForDownloadAsync(1, 1, false);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test.pdf", result.FileName);
        }

        [Fact]
        public async Task GetFileForDownloadAsync_NoUserIdProvided_ReturnsFile()
        {
            // Arrange
            var file = new SubmissionFile
            {
                Id = 1,
                FileName = "test.pdf",
                FileData = "YmFzZTY0ZGF0YQ==",
                MimeType = "application/pdf",
                Submission = new FormSubmission { UserId = 1 }
            };

            _mockSubmissionRepository.Setup(x => x.GetFileByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(file);

            // Act
            var result = await _submissionService.GetFileForDownloadAsync(1, null, true);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test.pdf", result.FileName);
        }

        #endregion

        #region GetSubmissionFilesAsync Tests

        [Fact]
        public async Task GetSubmissionFilesAsync_SubmissionNotFound_ThrowsException()
        {
            // Arrange
            _mockSubmissionRepository.Setup(x => x.GetSubmissionByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((FormSubmission)null);

            // Act & Assert
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    _submissionService.GetSubmissionFilesAsync(1));
            Assert.Equal("Submission not found", exception.Message);
        }

        [Fact]
        public async Task GetSubmissionFilesAsync_NotAdminAndNotOwner_ThrowsUnauthorizedException()
        {
            // Arrange
            var submission = new FormSubmission
            {
                Id = 1,
                UserId = 2,
                FormId = "form1"
            };

            _mockSubmissionRepository.Setup(x => x.GetSubmissionByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(submission);

            // Act & Assert
            var exception =
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                    _submissionService.GetSubmissionFilesAsync(1, 1, false));
            Assert.Equal("You don't have permission to view these files", exception.Message);
        }

        [Fact]
        public async Task GetSubmissionFilesAsync_AdminCanViewAnyFiles()
        {
            // Arrange
            var submission = new FormSubmission
            {
                Id = 1,
                UserId = 2,
                FormId = "form1",
                User = new User { Username = "testuser" }
            };
            var form = new Form
            {
                Id = "form1",
                Questions = new List<FormQuestion>
                {
                    new FormQuestion { Id = "q1", Label = "Upload File" }
                }
            };
            var files = new List<SubmissionFile>
            {
                new SubmissionFile
                {
                    Id = 1,
                    SubmissionId = 1,
                    QuestionId = "q1",
                    FileName = "test.pdf",
                    FileData = "YmFzZTY0ZGF0YQ==",
                    MimeType = "application/pdf",
                    UploadedAt = DateTime.UtcNow
                }
            };

            _mockSubmissionRepository.Setup(x => x.GetSubmissionByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(submission);
            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);
            _mockSubmissionRepository.Setup(x => x.GetFilesBySubmissionIdAsync(It.IsAny<int>()))
                .ReturnsAsync(files);

            // Act
            var result = await _submissionService.GetSubmissionFilesAsync(1, 1, true);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("testuser", result.First().SubmittedBy);
        }

        [Fact]
        public async Task GetSubmissionFilesAsync_OwnerCanViewOwnFiles()
        {
            // Arrange
            var submission = new FormSubmission
            {
                Id = 1,
                UserId = 1,
                FormId = "form1",
                User = new User { Username = "testuser" }
            };
            var form = new Form
            {
                Id = "form1",
                Questions = new List<FormQuestion>()
            };
            var files = new List<SubmissionFile>
            {
                new SubmissionFile
                {
                    Id = 1,
                    SubmissionId = 1,
                    QuestionId = "q1",
                    FileName = "test.pdf",
                    FileData = "YmFzZTY0ZGF0YQ==",
                    MimeType = "application/pdf",
                    UploadedAt = DateTime.UtcNow
                }
            };

            _mockSubmissionRepository.Setup(x => x.GetSubmissionByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(submission);
            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);
            _mockSubmissionRepository.Setup(x => x.GetFilesBySubmissionIdAsync(It.IsAny<int>()))
                .ReturnsAsync(files);

            // Act
            var result = await _submissionService.GetSubmissionFilesAsync(1, 1, false);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetSubmissionFilesAsync_NoUserIdProvided_ReturnsFiles()
        {
            // Arrange
            var submission = new FormSubmission
            {
                Id = 1,
                UserId = 1,
                FormId = "form1",
                User = null
            };
            var files = new List<SubmissionFile>();

            _mockSubmissionRepository.Setup(x => x.GetSubmissionByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(submission);
            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((Form)null);
            _mockSubmissionRepository.Setup(x => x.GetFilesBySubmissionIdAsync(It.IsAny<int>()))
                .ReturnsAsync(files);

            // Act
            var result = await _submissionService.GetSubmissionFilesAsync(1, null, true);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region CalculateBase64FileSize Tests

        [Fact]
        public async Task CalculateBase64FileSize_EmptyString_ReturnsZero()
        {
            // Arrange
            var form = new Form
            {
                Id = "form1",
                Title = "Test Form",
                Questions = new List<FormQuestion>()
            };
            var files = new List<SubmissionFile>
            {
                new SubmissionFile
                {
                    Id = 1,
                    SubmissionId = 1,
                    QuestionId = "q1",
                    FileName = "test.pdf",
                    FileData = "",
                    MimeType = "application/pdf",
                    UploadedAt = DateTime.UtcNow,
                    Submission = new FormSubmission { UserId = 1 }
                }
            };

            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);
            _mockSubmissionRepository.Setup(x => x.GetFilesByFormIdAsync(It.IsAny<string>()))
                .ReturnsAsync(files);

            // Act
            var result = await _submissionService.GetFormFilesAsync("form1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Files.First().FileSizeBytes);
        }

        [Fact]
        public async Task CalculateBase64FileSize_WithDoublePadding_CalculatesCorrectly()
        {
            // Arrange
            var form = new Form
            {
                Id = "form1",
                Title = "Test Form",
                Questions = new List<FormQuestion>()
            };
            var files = new List<SubmissionFile>
            {
                new SubmissionFile
                {
                    Id = 1,
                    SubmissionId = 1,
                    QuestionId = "q1",
                    FileName = "test.pdf",
                    FileData = "YmFzZTY0ZGF0YQ==", // Has == padding
                    MimeType = "application/pdf",
                    UploadedAt = DateTime.UtcNow,
                    Submission = new FormSubmission { UserId = 1 }
                }
            };

            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);
            _mockSubmissionRepository.Setup(x => x.GetFilesByFormIdAsync(It.IsAny<string>()))
                .ReturnsAsync(files);

            // Act
            var result = await _submissionService.GetFormFilesAsync("form1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.Files.First().FileSizeBytes); // (16 * 3) / 4 - 2 = 10
        }

        [Fact]
        public async Task CalculateBase64FileSize_WithSinglePadding_CalculatesCorrectly()
        {
            // Arrange
            var form = new Form
            {
                Id = "form1",
                Title = "Test Form",
                Questions = new List<FormQuestion>()
            };
            var files = new List<SubmissionFile>
            {
                new SubmissionFile
                {
                    Id = 1,
                    SubmissionId = 1,
                    QuestionId = "q1",
                    FileName = "test.pdf",
                    FileData = "YmFzZTY0ZGF0YQo=", // Has = padding
                    MimeType = "application/pdf",
                    UploadedAt = DateTime.UtcNow,
                    Submission = new FormSubmission { UserId = 1 }
                }
            };

            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);
            _mockSubmissionRepository.Setup(x => x.GetFilesByFormIdAsync(It.IsAny<string>()))
                .ReturnsAsync(files);

            // Act
            var result = await _submissionService.GetFormFilesAsync("form1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(11, result.Files.First().FileSizeBytes); // (16 * 3) / 4 - 1 = 11
        }

        [Fact]
        public async Task CalculateBase64FileSize_NoPadding_CalculatesCorrectly()
        {
            // Arrange
            var form = new Form
            {
                Id = "form1",
                Title = "Test Form",
                Questions = new List<FormQuestion>()
            };
            var files = new List<SubmissionFile>
            {
                new SubmissionFile
                {
                    Id = 1,
                    SubmissionId = 1,
                    QuestionId = "q1",
                    FileName = "test.pdf",
                    FileData = "YmFzZTY0ZGF0YQ", // No padding
                    MimeType = "application/pdf",
                    UploadedAt = DateTime.UtcNow,
                    Submission = new FormSubmission { UserId = 1 }
                }
            };

            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);
            _mockSubmissionRepository.Setup(x => x.GetFilesByFormIdAsync(It.IsAny<string>()))
                .ReturnsAsync(files);

            // Act
            var result = await _submissionService.GetFormFilesAsync("form1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.Files.First().FileSizeBytes); // (14 * 3) / 4 = 10.5 -> 10
        }

        [Fact]
        public async Task CalculateBase64FileSize_NullString_ReturnsZero()
        {
            // Arrange
            var form = new Form
            {
                Id = "form1",
                Title = "Test Form",
                Questions = new List<FormQuestion>()
            };
            var files = new List<SubmissionFile>
            {
                new SubmissionFile
                {
                    Id = 1,
                    SubmissionId = 1,
                    QuestionId = "q1",
                    FileName = "test.pdf",
                    FileData = null,
                    MimeType = "application/pdf",
                    UploadedAt = DateTime.UtcNow,
                    Submission = new FormSubmission { UserId = 1 }
                }
            };

            _mockFormRepository.Setup(x => x.GetFormByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(form);
            _mockSubmissionRepository.Setup(x => x.GetFilesByFormIdAsync(It.IsAny<string>()))
                .ReturnsAsync(files);

            // Act
            var result = await _submissionService.GetFormFilesAsync("form1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Files.First().FileSizeBytes);
        }

        #endregion

        

    }
}

