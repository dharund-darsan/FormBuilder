using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using FormsBackend.Models.Sql;
using FormsBackend.Tests.Repositories;
using Xunit;

namespace FormsBackend.Tests.Repository
{
    public class SubmissionRepositoryIntegrationTest : SubmissionRepositoryTestBase
    {
        #region CreateSubmissionAsync Tests - Lines 36-68

        
       
       
        #endregion

        #region GetSubmissionByIdAsync Tests - Lines 94-117

        [Fact]
        public async Task GetSubmissionByIdAsync_WithMultipleAnswersAndFiles_MapsCorrectly()
        {
            // Arrange
            // Create submission with multiple answers and files
            await _connection.ExecuteAsync(
                "INSERT INTO FormSubmissions (FormId, UserId, SubmittedAt) VALUES (@FormId, @UserId, @SubmittedAt)",
                new { FormId = "form100", UserId = 1, SubmittedAt = DateTime.UtcNow });
            
            var submissionId = await _connection.ExecuteScalarAsync<int>(
                "SELECT MAX(Id) FROM FormSubmissions");
            
            // Insert multiple answers
            await _connection.ExecuteAsync(
                @"INSERT INTO SubmissionAnswers (SubmissionId, QuestionId, AnswerType, AnswerText) 
                  VALUES (@SubmissionId, @QuestionId, @AnswerType, @AnswerText)",
                new[]
                {
                    new { SubmissionId = submissionId, QuestionId = "q1", AnswerType = "text", AnswerText = "Answer1" },
                    new { SubmissionId = submissionId, QuestionId = "q2", AnswerType = "radio", AnswerText = "[\"opt1\"]" },
                    new { SubmissionId = submissionId, QuestionId = "q3", AnswerType = "checkbox", AnswerText = "[\"opt2\",\"opt3\"]" }
                });
            
            // Insert multiple files
            await _connection.ExecuteAsync(
                @"INSERT INTO SubmissionFiles (SubmissionId, QuestionId, FileName, FileData, MimeType, UploadedAt) 
                  VALUES (@SubmissionId, @QuestionId, @FileName, @FileData, @MimeType, @UploadedAt)",
                new[]
                {
                    new { SubmissionId = submissionId, QuestionId = "q4", FileName = "file1.pdf", FileData = "data1", MimeType = "application/pdf", UploadedAt = DateTime.UtcNow },
                    new { SubmissionId = submissionId, QuestionId = "q5", FileName = "file2.jpg", FileData = "data2", MimeType = "image/jpeg", UploadedAt = DateTime.UtcNow }
                });

            // Act
            var result = await _repository.GetSubmissionByIdAsync(submissionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(submissionId, result.Id);
            Assert.Equal("form100", result.FormId);
            Assert.Equal(1, result.UserId);
            
            // Verify answers collection
            Assert.NotNull(result.Answers);
            Assert.Equal(3, result.Answers.Count);
            
            var answersList = result.Answers.ToList();
            Assert.Equal("q1", answersList[0].QuestionId);
            Assert.Equal("text", answersList[0].AnswerType);
            Assert.Equal("Answer1", answersList[0].AnswerText);
            
            Assert.Equal("q2", answersList[1].QuestionId);
            Assert.Equal("radio", answersList[1].AnswerType);
            Assert.Equal("[\"opt1\"]", answersList[1].AnswerText);
            
            Assert.Equal("q3", answersList[2].QuestionId);
            Assert.Equal("checkbox", answersList[2].AnswerType);
            Assert.Equal("[\"opt2\",\"opt3\"]", answersList[2].AnswerText);
            
            // Verify files collection
            Assert.NotNull(result.Files);
            Assert.Equal(2, result.Files.Count);
            
            var filesList = result.Files.ToList();
            Assert.Equal("q4", filesList[0].QuestionId);
            Assert.Equal("file1.pdf", filesList[0].FileName);
            
            Assert.Equal("q5", filesList[1].QuestionId);
            Assert.Equal("file2.jpg", filesList[1].FileName);
        }

        [Fact]
        public async Task GetSubmissionByIdAsync_NoDuplicatesInCollections()
        {
            // Arrange
            // This test ensures the dictionary logic prevents duplicates
            await _connection.ExecuteAsync(
                "INSERT INTO FormSubmissions (FormId, UserId, SubmittedAt) VALUES (@FormId, @UserId, @SubmittedAt)",
                new { FormId = "form101", UserId = 2, SubmittedAt = DateTime.UtcNow });
            
            var submissionId = await _connection.ExecuteScalarAsync<int>(
                "SELECT MAX(Id) FROM FormSubmissions");
            
            // Insert answers
            await _connection.ExecuteAsync(
                @"INSERT INTO SubmissionAnswers (SubmissionId, QuestionId, AnswerType, AnswerText) 
                  VALUES (@SubmissionId, @QuestionId, @AnswerType, @AnswerText)",
                new { SubmissionId = submissionId, QuestionId = "q1", AnswerType = "text", AnswerText = "Answer" });
            
            await _connection.ExecuteAsync(
                @"INSERT INTO SubmissionFiles (SubmissionId, QuestionId, FileName, FileData, MimeType, UploadedAt) 
                  VALUES (@SubmissionId, @QuestionId, @FileName, @FileData, @MimeType, @UploadedAt)",
                new { SubmissionId = submissionId, QuestionId = "q2", FileName = "file.pdf", FileData = "data", MimeType = "application/pdf", UploadedAt = DateTime.UtcNow });

            // Act
            var result = await _repository.GetSubmissionByIdAsync(submissionId);

            // Assert
            Assert.NotNull(result);
            
            // Check that there are no duplicates (the dictionary logic should prevent them)
            Assert.Single(result.Answers);
            Assert.Single(result.Files);
            
            // Verify the IDs are unique
            var answerIds = result.Answers.Select(a => a.Id).ToList();
            Assert.Equal(answerIds.Count, answerIds.Distinct().Count());
            
            var fileIds = result.Files.Select(f => f.Id).ToList();
            Assert.Equal(fileIds.Count, fileIds.Distinct().Count());
        }

        [Fact]
        public async Task GetSubmissionByIdAsync_OnlyAnswersNoFiles_MapsCorrectly()
        {
            // Arrange
            await _connection.ExecuteAsync(
                "INSERT INTO FormSubmissions (FormId, UserId, SubmittedAt) VALUES (@FormId, @UserId, @SubmittedAt)",
                new { FormId = "form102", UserId = 3, SubmittedAt = DateTime.UtcNow });
            
            var submissionId = await _connection.ExecuteScalarAsync<int>(
                "SELECT MAX(Id) FROM FormSubmissions");
            
            await _connection.ExecuteAsync(
                @"INSERT INTO SubmissionAnswers (SubmissionId, QuestionId, AnswerType, AnswerText) 
                  VALUES (@SubmissionId, @QuestionId, @AnswerType, @AnswerText)",
                new { SubmissionId = submissionId, QuestionId = "q1", AnswerType = "text", AnswerText = "Only Answer" });

            // Act
            var result = await _repository.GetSubmissionByIdAsync(submissionId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Answers);
            Assert.Empty(result.Files);
        }

        [Fact]
        public async Task GetSubmissionByIdAsync_OnlyFilesNoAnswers_MapsCorrectly()
        {
            // Arrange
            await _connection.ExecuteAsync(
                "INSERT INTO FormSubmissions (FormId, UserId, SubmittedAt) VALUES (@FormId, @UserId, @SubmittedAt)",
                new { FormId = "form103", UserId = 4, SubmittedAt = DateTime.UtcNow });
            
            var submissionId = await _connection.ExecuteScalarAsync<int>(
                "SELECT MAX(Id) FROM FormSubmissions");
            
            await _connection.ExecuteAsync(
                @"INSERT INTO SubmissionFiles (SubmissionId, QuestionId, FileName, FileData, MimeType, UploadedAt) 
                  VALUES (@SubmissionId, @QuestionId, @FileName, @FileData, @MimeType, @UploadedAt)",
                new { SubmissionId = submissionId, QuestionId = "q1", FileName = "only.pdf", FileData = "data", MimeType = "application/pdf", UploadedAt = DateTime.UtcNow });

            // Act
            var result = await _repository.GetSubmissionByIdAsync(submissionId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Answers);
            Assert.Single(result.Files);
        }

        #endregion

        

        #region GetSubmissionsByFormIdAsync Tests - Lines 185-200

        [Fact]
        public async Task GetSubmissionsByFormIdAsync_MultipleUsersSubmissions_MapsCorrectly()
        {
            // Arrange
            var formId = "form501";
            
            // Create submissions from different users
            for (int userId = 1; userId <= 3; userId++)
            {
                await _connection.ExecuteAsync(
                    "INSERT INTO FormSubmissions (FormId, UserId, SubmittedAt) VALUES (@FormId, @UserId, @SubmittedAt)",
                    new { FormId = formId, UserId = userId, SubmittedAt = DateTime.UtcNow.AddHours(-userId) });
                
                var submissionId = await _connection.ExecuteScalarAsync<int>("SELECT MAX(Id) FROM FormSubmissions");
                
                // Add answer for each submission
                await _connection.ExecuteAsync(
                    @"INSERT INTO SubmissionAnswers (SubmissionId, QuestionId, AnswerType, AnswerText) 
                      VALUES (@SubmissionId, @QuestionId, @AnswerType, @AnswerText)",
                    new { SubmissionId = submissionId, QuestionId = $"q{userId}", AnswerType = "text", AnswerText = $"Answer from user {userId}" });
                
                // Add file for some submissions
                if (userId % 2 == 1)
                {
                    await _connection.ExecuteAsync(
                        @"INSERT INTO SubmissionFiles (SubmissionId, QuestionId, FileName, FileData, MimeType, UploadedAt) 
                          VALUES (@SubmissionId, @QuestionId, @FileName, @FileData, @MimeType, @UploadedAt)",
                        new { SubmissionId = submissionId, QuestionId = $"qf{userId}", FileName = $"file{userId}.pdf", FileData = $"data{userId}", MimeType = "application/pdf", UploadedAt = DateTime.UtcNow });
                }
            }

            // Act
            var result = await _repository.GetSubmissionsByFormIdAsync(formId);

            // Assert
            Assert.NotNull(result);
            var submissions = result.ToList();
            Assert.Equal(3, submissions.Count);
            
            // Verify all submissions are for the correct form
            Assert.All(submissions, s => Assert.Equal(formId, s.FormId));
            
            // Verify each submission has its answer
            Assert.All(submissions, s => Assert.Single(s.Answers));
            
            // Verify files are present for odd user IDs
            var submissionsWithFiles = submissions.Where(s => s.UserId % 2 == 1).ToList();
            Assert.All(submissionsWithFiles, s => Assert.Single(s.Files));
            
            var submissionsWithoutFiles = submissions.Where(s => s.UserId % 2 == 0).ToList();
            Assert.All(submissionsWithoutFiles, s => Assert.Empty(s.Files));
        }

        [Fact]
        public async Task GetSubmissionsByFormIdAsync_OrderedBySubmittedAtDesc()
        {
            // Arrange
            var formId = "form502";
            var now = DateTime.UtcNow;
            
            // Create submissions with different timestamps
            await _connection.ExecuteAsync(
                @"INSERT INTO FormSubmissions (FormId, UserId, SubmittedAt) VALUES 
                  (@FormId, @UserId1, @SubmittedAt1),
                  (@FormId, @UserId2, @SubmittedAt2),
                  (@FormId, @UserId3, @SubmittedAt3)",
                new 
                { 
                    FormId = formId,
                    UserId1 = 1, SubmittedAt1 = now.AddDays(-3),
                    UserId2 = 2, SubmittedAt2 = now,
                    UserId3 = 3, SubmittedAt3 = now.AddDays(-1)
                });

            // Act
            var result = await _repository.GetSubmissionsByFormIdAsync(formId);

            // Assert
            Assert.NotNull(result);
            var submissions = result.ToList();
            Assert.Equal(3, submissions.Count);
            
            // Verify order is descending by SubmittedAt
            for (int i = 0; i < submissions.Count - 1; i++)
            {
                Assert.True(submissions[i].SubmittedAt >= submissions[i + 1].SubmittedAt,
                    $"Submissions not ordered correctly at index {i}");
            }
        }

        [Fact]
        public async Task GetSubmissionsByFormIdAsync_ComplexScenario_MapsAllDataCorrectly()
        {
            // Arrange
            var formId = "form503";
            
            // User 1 submission with multiple answers and files
            await _connection.ExecuteAsync(
                "INSERT INTO FormSubmissions (FormId, UserId, SubmittedAt) VALUES (@FormId, @UserId, @SubmittedAt)",
                new { FormId = formId, UserId = 1, SubmittedAt = DateTime.UtcNow });
            var submission1Id = await _connection.ExecuteScalarAsync<int>("SELECT MAX(Id) FROM FormSubmissions");
            
            await _connection.ExecuteAsync(
                @"INSERT INTO SubmissionAnswers (SubmissionId, QuestionId, AnswerType, AnswerText) 
                  VALUES (@SubmissionId, @QuestionId, @AnswerType, @AnswerText)",
                new[]
                {
                    new { SubmissionId = submission1Id, QuestionId = "q1", AnswerType = "text", AnswerText = "User1 Answer1" },
                    new { SubmissionId = submission1Id, QuestionId = "q2", AnswerType = "checkbox", AnswerText = "[\"opt1\",\"opt2\"]" }
                });
            
            await _connection.ExecuteAsync(
                @"INSERT INTO SubmissionFiles (SubmissionId, QuestionId, FileName, FileData, MimeType, UploadedAt) 
                  VALUES (@SubmissionId, @QuestionId, @FileName, @FileData, @MimeType, @UploadedAt)",
                new { SubmissionId = submission1Id, QuestionId = "q3", FileName = "user1.pdf", FileData = "user1data", MimeType = "application/pdf", UploadedAt = DateTime.UtcNow });
            
            // User 2 submission with only answers
            await _connection.ExecuteAsync(
                "INSERT INTO FormSubmissions (FormId, UserId, SubmittedAt) VALUES (@FormId, @UserId, @SubmittedAt)",
                new { FormId = formId, UserId = 2, SubmittedAt = DateTime.UtcNow.AddMinutes(-30) });
            var submission2Id = await _connection.ExecuteScalarAsync<int>("SELECT MAX(Id) FROM FormSubmissions");
            
            await _connection.ExecuteAsync(
                @"INSERT INTO SubmissionAnswers (SubmissionId, QuestionId, AnswerType, AnswerText) 
                  VALUES (@SubmissionId, @QuestionId, @AnswerType, @AnswerText)",
                new { SubmissionId = submission2Id, QuestionId = "q1", AnswerType = "text", AnswerText = "User2 Answer" });

            // Act
            var result = await _repository.GetSubmissionsByFormIdAsync(formId);

            // Assert
            Assert.NotNull(result);
            var submissions = result.ToList();
            Assert.Equal(2, submissions.Count);
            
            // Verify User 1 submission
            var user1Submission = submissions.FirstOrDefault(s => s.UserId == 1);
            Assert.NotNull(user1Submission);
            Assert.Equal(2, user1Submission.Answers.Count);
            Assert.Single(user1Submission.Files);
            
            // Verify User 2 submission
            var user2Submission = submissions.FirstOrDefault(s => s.UserId == 2);
            Assert.NotNull(user2Submission);
            Assert.Single(user2Submission.Answers);
            Assert.Empty(user2Submission.Files);
            
            // Verify no duplicate entries
            foreach (var submission in submissions)
            {
                var answerIds = submission.Answers.Select(a => a.Id);
                Assert.Equal(submission.Answers.Count, answerIds.Distinct().Count());
                
                var fileIds = submission.Files.Select(f => f.Id);
                Assert.Equal(submission.Files.Count, fileIds.Distinct().Count());
            }
        }

        #endregion
        
        
        //////////
        ///
        /// 
        #region Tests for Lines 36-68 (Answer and File Insertion)

       
       
        
        #endregion

        #region Tests for Lines 139-160 (GetSubmissionsByUserIdAsync mapping)

        [Fact]
        public async Task GetSubmissionsByUserIdAsync_MultipleSubmissionsWithAnswersAndFiles_MapsAllDataCorrectly()
        {
            // Arrange - This tests lines 139-160 (multi-map query and dictionary logic)
            // Create first submission with answers and files
            var submissionId1 = await CreateTestSubmission("form-user-1", 1);
            await CreateTestAnswer(submissionId1, "q1", "text", "User submission 1 answer 1");
            await CreateTestAnswer(submissionId1, "q2", "checkbox", "[\"opt1\",\"opt2\"]");
            await CreateTestFile(submissionId1, "q3", "file1.pdf", "filedata1", "application/pdf");

            // Create second submission with different data
            var submissionId2 = await CreateTestSubmission("form-user-2", 1, DateTime.UtcNow.AddHours(-2));
            await CreateTestAnswer(submissionId2, "q4", "radio", "[\"optionA\"]");
            await CreateTestFile(submissionId2, "q5", "file2.jpg", "filedata2", "image/jpeg");
            await CreateTestFile(submissionId2, "q6", "file3.docx", "filedata3", "application/msword");

            // Act
            var result = await _repository.GetSubmissionsByUserIdAsync(1);

            // Assert - Verify multi-map worked correctly
            var submissions = result.ToList();
            Assert.Equal(2, submissions.Count);

            // Check first submission mapping
            var submission1 = submissions.FirstOrDefault(s => s.Id == submissionId1);
            Assert.NotNull(submission1);
            Assert.Equal("form-user-1", submission1.FormId);
            Assert.Equal(2, submission1.Answers.Count);
            Assert.Single(submission1.Files);
            
            // Verify answers are correctly mapped to submission1
            Assert.Contains(submission1.Answers, a => a.QuestionId == "q1" && a.AnswerText == "User submission 1 answer 1");
            Assert.Contains(submission1.Answers, a => a.QuestionId == "q2" && a.AnswerText == "[\"opt1\",\"opt2\"]");
            
            // Verify file is correctly mapped to submission1
            Assert.Contains(submission1.Files, f => f.QuestionId == "q3" && f.FileName == "file1.pdf");

            // Check second submission mapping
            var submission2 = submissions.FirstOrDefault(s => s.Id == submissionId2);
            Assert.NotNull(submission2);
            Assert.Equal("form-user-2", submission2.FormId);
            Assert.Single(submission2.Answers);
            Assert.Equal(2, submission2.Files.Count);
            
            // Verify no cross-contamination between submissions
            Assert.DoesNotContain(submission1.Answers, a => a.QuestionId == "q4");
            Assert.DoesNotContain(submission2.Answers, a => a.QuestionId == "q1");
        }

        [Fact]
        public async Task GetSubmissionsByUserIdAsync_DuplicateRowsFromJoin_DeduplicatesCorrectly()
        {
            // Arrange - This specifically tests the dictionary deduplication logic in lines 147-157
            // Create submission with multiple answers and files to generate multiple join rows
            var submissionId = await CreateTestSubmission("form-dedup-test", 2);
            
            // Add multiple answers
            await CreateTestAnswer(submissionId, "q1", "text", "Answer 1");
            await CreateTestAnswer(submissionId, "q2", "text", "Answer 2");
            await CreateTestAnswer(submissionId, "q3", "text", "Answer 3");
            
            // Add multiple files
            await CreateTestFile(submissionId, "qf1", "file1.pdf", "data1", "application/pdf");
            await CreateTestFile(submissionId, "qf2", "file2.pdf", "data2", "application/pdf");

            // Act
            var result = await _repository.GetSubmissionsByUserIdAsync(2);

            // Assert
            var submissions = result.ToList();
            Assert.Single(submissions); // Should be deduplicated to single submission
            
            var submission = submissions.First();
            Assert.Equal(submissionId, submission.Id);
            
            // Verify all answers are present without duplicates
            Assert.Equal(3, submission.Answers.Count);
            Assert.Equal(3, submission.Answers.Select(a => a.Id).Distinct().Count());
            
            // Verify all files are present without duplicates
            Assert.Equal(2, submission.Files.Count);
            Assert.Equal(2, submission.Files.Select(f => f.Id).Distinct().Count());
            
            // Verify correct question IDs
            var questionIds = submission.Answers.Select(a => a.QuestionId).ToList();
            Assert.Contains("q1", questionIds);
            Assert.Contains("q2", questionIds);
            Assert.Contains("q3", questionIds);
            
            var fileQuestionIds = submission.Files.Select(f => f.QuestionId).ToList();
            Assert.Contains("qf1", fileQuestionIds);
            Assert.Contains("qf2", fileQuestionIds);
        }

        #endregion
    }
}
