using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FormsBackend.Models.Mongo;
using FormsBackend.Repositories;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace FormsBackend.Tests.Repositories
{
    public class FormRepositoryTest
    {
        private readonly Mock<IMongoDatabase> _mockDatabase;
        private readonly Mock<IMongoCollection<Form>> _mockCollection;
        private readonly FormRepository _formRepository;

        public FormRepositoryTest()
        {
            _mockDatabase = new Mock<IMongoDatabase>();
            _mockCollection = new Mock<IMongoCollection<Form>>();
            
            _mockDatabase.Setup(x => x.GetCollection<Form>("forms", null))
                .Returns(_mockCollection.Object);
            
            _formRepository = new FormRepository(_mockDatabase.Object);
        }

        #region CreateFormAsync Tests

        [Fact]
        public async Task CreateFormAsync_ValidForm_InsertsAndReturnsForm()
        {
            // Arrange
            var form = new Form
            {
                Id = "form123",
                Title = "Test Form",
                Description = "Test Description",
                Status = "draft"
            };

            _mockCollection.Setup(x => x.InsertOneAsync(
                It.IsAny<Form>(), 
                null, 
                default(CancellationToken)))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _formRepository.CreateFormAsync(form);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(form.Id, result.Id);
            Assert.Equal(form.Title, result.Title);
            _mockCollection.Verify(x => x.InsertOneAsync(
                It.Is<Form>(f => f.Id == form.Id), 
                null, 
                default(CancellationToken)), 
                Times.Once);
        }

        #endregion

        #region GetFormByIdAsync Tests

        [Fact]
        public async Task GetFormByIdAsync_FormExists_ReturnsForm()
        {
            // Arrange
            var formId = "form123";
            var expectedForm = new Form
            {
                Id = formId,
                Title = "Test Form",
                Status = "published"
            };

            var mockCursor = new Mock<IAsyncCursor<Form>>();
            mockCursor.Setup(x => x.Current).Returns(new List<Form> { expectedForm });
            mockCursor.SetupSequence(x => x.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockCollection.Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<FindOptions<Form, Form>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await _formRepository.GetFormByIdAsync(formId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedForm.Id, result.Id);
            Assert.Equal(expectedForm.Title, result.Title);
        }

        [Fact]
        public async Task GetFormByIdAsync_FormNotFound_ReturnsNull()
        {
            // Arrange
            var formId = "nonexistent";

            var mockCursor = new Mock<IAsyncCursor<Form>>();
            mockCursor.Setup(x => x.Current).Returns(new List<Form>());
            mockCursor.SetupSequence(x => x.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockCollection.Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<FindOptions<Form, Form>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await _formRepository.GetFormByIdAsync(formId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetAllFormsAsync Tests

        [Fact]
        public async Task GetAllFormsAsync_FormsExist_ReturnsAllForms()
        {
            // Arrange
            var forms = new List<Form>
            {
                new Form { Id = "form1", Title = "Form 1" },
                new Form { Id = "form2", Title = "Form 2" },
                new Form { Id = "form3", Title = "Form 3" }
            };

            var mockCursor = new Mock<IAsyncCursor<Form>>();
            mockCursor.Setup(x => x.Current).Returns(forms);
            mockCursor.SetupSequence(x => x.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockCollection.Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<FindOptions<Form, Form>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await _formRepository.GetAllFormsAsync();

            // Assert
            Assert.NotNull(result);
            var formsList = result as List<Form> ?? new List<Form>(result);
            Assert.Equal(3, formsList.Count);
            Assert.Contains(formsList, f => f.Id == "form1");
            Assert.Contains(formsList, f => f.Id == "form2");
            Assert.Contains(formsList, f => f.Id == "form3");
        }

        [Fact]
        public async Task GetAllFormsAsync_NoForms_ReturnsEmptyList()
        {
            // Arrange
            var mockCursor = new Mock<IAsyncCursor<Form>>();
            mockCursor.Setup(x => x.Current).Returns(new List<Form>());
            mockCursor.SetupSequence(x => x.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockCollection.Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<FindOptions<Form, Form>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await _formRepository.GetAllFormsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region PublishFormAsync Tests

        [Fact]
        public async Task PublishFormAsync_ValidId_UpdatesAndReturnsPublishedForm()
        {
            // Arrange
            var formId = "form123";
            var publishedBy = "admin";
            var publishedAt = DateTime.UtcNow;
            
            var updatedForm = new Form
            {
                Id = formId,
                Title = "Test Form",
                Status = "published",
                PublishedBy = publishedBy,
                PublishedAt = publishedAt
            };

            _mockCollection.Setup(x => x.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<UpdateDefinition<Form>>(),
                It.IsAny<FindOneAndUpdateOptions<Form, Form>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(updatedForm);

            // Act
            var result = await _formRepository.PublishFormAsync(formId, publishedBy);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(formId, result.Id);
            Assert.Equal("published", result.Status);
            Assert.Equal(publishedBy, result.PublishedBy);
            Assert.NotNull(result.PublishedAt);
            
            _mockCollection.Verify(x => x.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<UpdateDefinition<Form>>(),
                It.Is<FindOneAndUpdateOptions<Form, Form>>(opt => opt.ReturnDocument == ReturnDocument.After),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task PublishFormAsync_FormNotFound_ReturnsNull()
        {
            // Arrange
            var formId = "nonexistent";
            var publishedBy = "admin";

            _mockCollection.Setup(x => x.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<UpdateDefinition<Form>>(),
                It.IsAny<FindOneAndUpdateOptions<Form, Form>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((Form)null);

            // Act
            var result = await _formRepository.PublishFormAsync(formId, publishedBy);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region UpdateFormAsync Tests

        [Fact]
        public async Task UpdateFormAsync_ValidForm_ReplacesAndReturnsForm()
        {
            // Arrange
            var form = new Form
            {
                Id = "form123",
                Title = "Updated Form",
                Description = "Updated Description",
                Status = "draft"
            };

            var replaceResult = new ReplaceOneResult.Acknowledged(1, 1, null);

            _mockCollection.Setup(x => x.ReplaceOneAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<Form>(),
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(replaceResult);

            // Act
            var result = await _formRepository.UpdateFormAsync(form);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(form.Id, result.Id);
            Assert.Equal(form.Title, result.Title);
            Assert.Equal(form.Description, result.Description);
            
            _mockCollection.Verify(x => x.ReplaceOneAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.Is<Form>(f => f.Id == form.Id && f.Title == form.Title),
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateFormAsync_FormNotFound_StillReturnsForm()
        {
            // Arrange
            var form = new Form
            {
                Id = "nonexistent",
                Title = "Test Form"
            };

            var replaceResult = new ReplaceOneResult.Acknowledged(0, 0, null);

            _mockCollection.Setup(x => x.ReplaceOneAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<Form>(),
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(replaceResult);

            // Act
            var result = await _formRepository.UpdateFormAsync(form);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(form.Id, result.Id);
        }

        #endregion

        #region DeleteFormAsync Tests

        [Fact]
        public async Task DeleteFormAsync_FormExists_DeletesAndReturnsTrue()
        {
            // Arrange
            var formId = "form123";
            var deleteResult = new DeleteResult.Acknowledged(1);

            _mockCollection.Setup(x => x.DeleteOneAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(deleteResult);

            // Act
            var result = await _formRepository.DeleteFormAsync(formId);

            // Assert
            Assert.True(result);
            
            _mockCollection.Verify(x => x.DeleteOneAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteFormAsync_FormNotFound_ReturnsFalse()
        {
            // Arrange
            var formId = "nonexistent";
            var deleteResult = new DeleteResult.Acknowledged(0);

            _mockCollection.Setup(x => x.DeleteOneAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(deleteResult);

            // Act
            var result = await _formRepository.DeleteFormAsync(formId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteFormAsync_MultipleDeleted_ReturnsTrue()
        {
            // Arrange
            var formId = "form123";
            var deleteResult = new DeleteResult.Acknowledged(5); // Multiple deleted

            _mockCollection.Setup(x => x.DeleteOneAsync(
                It.IsAny<FilterDefinition<Form>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(deleteResult);

            // Act
            var result = await _formRepository.DeleteFormAsync(formId);

            // Assert
            Assert.True(result);
        }

        #endregion
    }
}
