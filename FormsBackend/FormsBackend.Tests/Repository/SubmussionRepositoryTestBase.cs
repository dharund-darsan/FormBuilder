using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using Moq;
using FormsBackend.Repositories;
using FormsBackend.Data;

namespace FormsBackend.Tests.Repositories
{
    public abstract class SubmissionRepositoryTestBase : IDisposable
    {
        protected readonly SqliteConnection _connection;
        protected readonly SubmissionRepository _repository;
        protected readonly Mock<IDbConnectionFactory> _mockFactory;

        public SubmissionRepositoryTestBase()
        {
            // 1️⃣ Create in-memory SQLite DB
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // 2️⃣ Create tables (matching the actual SQL structure used in SubmissionRepository)
            InitializeDatabase();

            // 3️⃣ Mock the factory to return our live SQLite connection
            _mockFactory = new Mock<IDbConnectionFactory>();
            _mockFactory.Setup(f => f.CreateConnection()).Returns(_connection);

            // 4️⃣ Create repository using the mocked factory
            _repository = new SubmissionRepository(_mockFactory.Object);
        }

        private void InitializeDatabase()
        {
            // Create Users table (needed for JOIN operations)
            _connection.Execute(@"
                CREATE TABLE Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL,
                    Email TEXT NOT NULL UNIQUE,
                    PasswordHash TEXT NOT NULL,
                    Role TEXT NOT NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );
            ");

            // Create FormSubmissions table with all required columns
            _connection.Execute(@"
                CREATE TABLE FormSubmissions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FormId TEXT NOT NULL,
                    UserId INTEGER NOT NULL,
                    SubmittedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (UserId) REFERENCES Users(Id)
                );
            ");

            // Create index for better query performance
            _connection.Execute(@"
                CREATE INDEX IX_FormSubmissions_FormId ON FormSubmissions(FormId);
                CREATE INDEX IX_FormSubmissions_UserId ON FormSubmissions(UserId);
            ");

            // Create SubmissionAnswers table with all required columns
            _connection.Execute(@"
                CREATE TABLE SubmissionAnswers (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SubmissionId INTEGER NOT NULL,
                    QuestionId TEXT NOT NULL,
                    AnswerType TEXT NOT NULL,
                    AnswerText TEXT,
                    FOREIGN KEY (SubmissionId) REFERENCES FormSubmissions(Id)
                );
            ");

            // Create index for better query performance
            _connection.Execute(@"
                CREATE INDEX IX_SubmissionAnswers_SubmissionId ON SubmissionAnswers(SubmissionId);
            ");

            // Create SubmissionFiles table with all required columns
            _connection.Execute(@"
                CREATE TABLE SubmissionFiles (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SubmissionId INTEGER NOT NULL,
                    QuestionId TEXT NOT NULL,
                    FileName TEXT NOT NULL,
                    FileData TEXT NOT NULL,
                    MimeType TEXT,
                    UploadedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (SubmissionId) REFERENCES FormSubmissions(Id)
                );
            ");

            // Create index for better query performance
            _connection.Execute(@"
                CREATE INDEX IX_SubmissionFiles_SubmissionId ON SubmissionFiles(SubmissionId);
            ");

            // Optionally, seed a test user for tests that need it
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Create a default test user for tests that need user data
            _connection.Execute(@"
                INSERT INTO Users (Id, Username, Email, PasswordHash, Role, CreatedAt)
                VALUES 
                (1, 'testuser1', 'test1@example.com', 'hash1', 'Learner', datetime('now')),
                (2, 'testuser2', 'test2@example.com', 'hash2', 'Learner', datetime('now')),
                (3, 'testuser3', 'test3@example.com', 'hash3', 'Admin', datetime('now')),
                (4, 'testuser4', 'test4@example.com', 'hash4', 'Learner', datetime('now'))
            ");
        }

        protected async Task<int> CreateTestSubmission(string formId, int userId, DateTime? submittedAt = null)
        {
            // Helper method to create test submissions
            var sql = @"
                INSERT INTO FormSubmissions (FormId, UserId, SubmittedAt) 
                VALUES (@FormId, @UserId, @SubmittedAt);
                SELECT last_insert_rowid();";
            
            return await _connection.QuerySingleAsync<int>(sql, new 
            { 
                FormId = formId, 
                UserId = userId, 
                SubmittedAt = submittedAt ?? DateTime.UtcNow 
            });
        }

        protected async Task<int> CreateTestAnswer(int submissionId, string questionId, string answerType, string answerText)
        {
            // Helper method to create test answers
            var sql = @"
                INSERT INTO SubmissionAnswers (SubmissionId, QuestionId, AnswerType, AnswerText) 
                VALUES (@SubmissionId, @QuestionId, @AnswerType, @AnswerText);
                SELECT last_insert_rowid();";
            
            return await _connection.QuerySingleAsync<int>(sql, new 
            { 
                SubmissionId = submissionId, 
                QuestionId = questionId, 
                AnswerType = answerType, 
                AnswerText = answerText 
            });
        }

        protected async Task<int> CreateTestFile(int submissionId, string questionId, string fileName, string fileData, string mimeType)
        {
            // Helper method to create test files
            var sql = @"
                INSERT INTO SubmissionFiles (SubmissionId, QuestionId, FileName, FileData, MimeType, UploadedAt) 
                VALUES (@SubmissionId, @QuestionId, @FileName, @FileData, @MimeType, @UploadedAt);
                SELECT last_insert_rowid();";
            
            return await _connection.QuerySingleAsync<int>(sql, new 
            { 
                SubmissionId = submissionId, 
                QuestionId = questionId, 
                FileName = fileName, 
                FileData = fileData, 
                MimeType = mimeType, 
                UploadedAt = DateTime.UtcNow 
            });
        }

        protected async Task ClearAllData()
        {
            // Helper method to clear all test data (except users)
            await _connection.ExecuteAsync("DELETE FROM SubmissionFiles");
            await _connection.ExecuteAsync("DELETE FROM SubmissionAnswers");
            await _connection.ExecuteAsync("DELETE FROM FormSubmissions");
            // Reset the auto-increment counters
            await _connection.ExecuteAsync("DELETE FROM sqlite_sequence WHERE name IN ('SubmissionFiles', 'SubmissionAnswers', 'FormSubmissions')");
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
