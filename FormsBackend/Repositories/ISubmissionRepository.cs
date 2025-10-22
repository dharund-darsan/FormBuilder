using FormsBackend.Models.Sql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FormsBackend.Repositories
{
    public interface ISubmissionRepository
    {
        Task<FormSubmission> CreateSubmissionAsync(FormSubmission submission);
        Task<FormSubmission> GetSubmissionByIdAsync(int id);
        Task<IEnumerable<FormSubmission>> GetSubmissionsByUserIdAsync(int userId);
        Task<IEnumerable<FormSubmission>> GetSubmissionsByFormIdAsync(string formId);
        Task<bool> HasUserSubmittedFormAsync(int userId, string formId);
        Task<int> GetSubmissionCountByFormIdAsync(string formId);
        Task<IEnumerable<SubmissionFile>> GetFilesByFormIdAsync(string formId);
        Task<SubmissionFile> GetFileByIdAsync(int fileId);
        Task<IEnumerable<SubmissionFile>> GetFilesBySubmissionIdAsync(int submissionId);
    }
}