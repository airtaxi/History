using History.Commons;
using History.Commons.DataTypes;

namespace History.ApiService.Services.Interfaces;

public interface IModerationService
{
    public Task<Result<RestrictionRecord>> GetRestrictionRecordByIdAsync(string recordId);
    public Task<Result> DeleteRestrictionRecordByIdAsync(string recordId);
    public Task<Result> DeletePostAsync(string postId, string moderatorId, string reason);
    public Task<Result> DeleteCommentAsync(string commentId, string moderatorId, string reason);
}
