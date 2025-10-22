using System;
using System.Collections.Generic;

namespace FormsBackend.DTOs
{
    public class SubmitFormDto
    {
        public string FormId { get; set; }
        public List<SubmissionAnswerDto> Answers { get; set; }
    }

    public class SubmissionAnswerDto
    {
        public string QuestionId { get; set; }
        public string AnswerType { get; set; } // text, checkbox, radio, etc.
        public string AnswerText { get; set; } // For text answers or JSON array for checkbox
        public FileUploadDto? File { get; set; } // For file uploads
    }

    public class FileUploadDto
    {
        public string FileName { get; set; }
        public string FileData { get; set; } // Base64 encoded file content
        public string MimeType { get; set; }
    }

    public class SubmissionResponseDto
    {
        public int Id { get; set; }
        public string FormId { get; set; }
        public string FormTitle { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public DateTime SubmittedAt { get; set; }
        public List<AnswerResponseDto> Answers { get; set; }
    }

    public class AnswerResponseDto
    {
        public string QuestionId { get; set; }
        public string QuestionLabel { get; set; }
        public string AnswerType { get; set; }
        public string AnswerText { get; set; }
        public FileResponseDto? File { get; set; }
    }

    public class FileResponseDto
    {
        public string FileName { get; set; }
        public string MimeType { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class MySubmissionsDto
    {
        public int TotalSubmissions { get; set; }
        public List<SubmissionSummaryDto> Submissions { get; set; }
    }

    public class SubmissionSummaryDto
    {
        public int SubmissionId { get; set; }
        public string FormId { get; set; }
        public string FormTitle { get; set; }
        public string FormStatus { get; set; }
        public DateTime SubmittedAt { get; set; }
        public int TotalQuestions { get; set; }
        public int AnsweredQuestions { get; set; }
    }
}