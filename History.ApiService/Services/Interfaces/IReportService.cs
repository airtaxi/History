using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;

namespace History.ApiService.Services.Interfaces;

public interface IReportService
{
    public Task<Result<ReportRecord>> GetReportRecordByIdAsync(string recordId);
    public Task<Result<List<ReportRecord>>> GetReportRecordsAsync(string fromRecordId = null, int limit = 10);

    public Task<Result> CreateReportRecordAsync(ReportType type, ReportTarget target, string associatedId, string reporterId);
    public Task<Result> ProcessReportAsync(string recordId, string moderatorId, string reason);
    public Task<Result> DeleteReportRecordByIdAsync(string recordId);
    public Task<Result> DeleteReportRecordByPostIdAsync(string postId);
    public Task<Result> DeleteReportRecordByCommentIdAsync(string commentId);
    public Task<Result> DeleteReportRecordByUserIdAsync(string userId);

    public Task<Result<ReportRecordResponseDto>> GenerateReportRecordResponseDtoAsync(ReportRecord record);
    public Task<Result<List<ReportRecordResponseDto>>> GenerateReportRecordResponseDtosAsync(List<ReportRecord> records);
}
