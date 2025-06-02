using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;

namespace History.ApiService.Services.Interfaces;

public interface IModerationService
{
    public Task<Result<ModerationRecord>> GetModerationRecordByIdAsync(string recordId);
    public Task<Result<List<ModerationRecord>>> GetModerationRecordsAsync(string fromRecordId = null, int limit = 10);

    public Task<Result> DeleteModerationRecordByIdAsync(string recordId);
    public Task<Result> DeleteModerationRecordByUserIdAsync(string userId);

    public Task<Result> DeletePostAsync(string postId, string moderatorId, string reason, ReportType reportType);
    public Task<Result> DeleteCommentAsync(string commentId, string moderatorId, string reason, ReportType reportType);

    public Task<Result<ModerationRecordResponseDto>> GenerateModerationRecordResponseDtoAsync(ModerationRecord record);
    public Task<Result<List<ModerationRecordResponseDto>>> GenerateModerationRecordResponseDtosAsync(List<ModerationRecord> records);
}
