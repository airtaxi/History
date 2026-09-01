using CommunityToolkit.Maui.Alerts;
using History.Commons.DataTypes.Contents;
using History.MobileClient.ViewModels;
#if !WINDOWS
using NativeMedia;
#endif
using SkiaSharp;

namespace History.MobileClient.Helpers;

/// <summary>
/// Renders post contents into a single vertical PNG image using SkiaSharp.
/// Contents are stacked vertically (no carousel), spoilers are never hidden,
/// and videos are exported as their thumbnail with a play overlay.
/// </summary>
public static class PostImageRendererHelper
{
    // Layout constants (3x scale of the XAML templates)
    private const int CanvasWidth = 1520;
    private const float Padding = 48f;
    private const float ContentSpacing = 24f;
    private const float BodyFontSize = 42f;
    private const float TitleFontSize = 48f;
    private const float SmallFontSize = 36f;
    private const float StickerSize = 360f;
    private const float ExternalUrlHeight = 750f;
    private const float MediaCornerRadius = 18f;
    private const float CardCornerRadius = 36f;
    private const float OptionCornerRadius = 24f;
    private const float CardPadding = 36f;
    private const float OptionPaddingX = 36f;
    private const float OptionPaddingY = 30f;
    private const float DescriptionBarHeight = 120f;
    private const float PlayCircleRadius = 225f;
    private const float ProgressBarHeight = 18f;
    private const float OptionSpacing = 12f;

    // Header layout (PostContentTemplate 3x: 48dp profile, ColumnSpacing 8, bold name 16dp, timestamp 14dp)
    private const float HeaderProfileSize = 144f;
    private const float HeaderColumnSpacing = 24f;

    // Comment layout (CommentTemplate 3x: 32dp profile, ColumnSpacing 8, name, 12dp timestamp)
    private const float CommentProfileSize = 96f;
    private const float CommentColumnSpacing = 24f;
    private const float CommentMediaMaxWidth = 600f;
    private const float CommentRowSpacing = 12f;
    private const float CommentSeparatorHeight = 3f;

    private static readonly SKColor PrimaryColor = new(0xED, 0x66, 0x4D);
    private static readonly SKColor TextColor = new(0x24, 0x24, 0x24);
    private static readonly SKColor SecondaryTextColor = new(0x80, 0x80, 0x80);
    private static readonly SKColor LightTextColor = new(0xCC, 0xCC, 0xCC);
    private static readonly SKColor CardColor = new(0xF0, 0xF0, 0xF0);
    private static readonly SKColor ProgressTrackColor = new(0xE0, 0xE0, 0xE0);
    private static readonly SKColor OverlayColor = new(0x00, 0x00, 0x00, 0x80);
    private static readonly SKColor ExternalOverlayColor = new(0x00, 0x00, 0x00, 0x66);

    private static readonly HttpClient s_httpClient = new();
    private static readonly SemaphoreSlim s_typefaceSemaphore = new(1, 1);
    private static SKTypeface s_regularTypeface;
    private static SKTypeface s_boldTypeface;

    /// <summary>
    /// Builds the absolute timestamp text for image export headers.
    /// Photos have no "live" state, so relative stamps ("5분 전") are meaningless;
    /// always render the full absolute local time.
    /// </summary>
    public static string BuildFullTimestampText(DateTime createdAt, DateTime? modifiedAt = null)
    {
        var result = $"{createdAt.ToLocalTime():yyyy년 M월 d일 H시 m분 s초}";
        if (modifiedAt != null) result += " (수정됨)";
        return result;
    }

    /// <summary>
    /// Renders the given post contents into PNG bytes.
    /// Optional header (profile image, nickname, timestamp) is drawn above the contents;
    /// header values are derived from the shared post view model surface, and the
    /// timestamp is always absolute (relative timestamps are meaningless in exported images).
    /// Optional comments are drawn below the contents under a thin separator, with the
    /// same absolute timestamp rule; contents are built from the comment view model surface.
    /// When excludeMediaExceptFirst is set, only the first MediaContent is rendered and
    /// every later MediaContent is skipped so it can be attached as a regular file instead.
    /// </summary>
    public static async Task<byte[]> RenderAsync(IEnumerable<BaseContent> contents, BasePostViewModel post = null, IEnumerable<BaseCommentViewModel> comments = null, bool excludeMediaExceptFirst = false) => await Task.Run(async () =>
    {
        var contentList = contents?.ToList() ?? [];
        var hasHeader = post != null;
        if (contentList.Count == 0 && !hasHeader && comments == null) return null;

        var (regularTypeface, boldTypeface) = await GetTypefacesAsync();

        using var bodyFont = new SKFont(regularTypeface, BodyFontSize);
        using var boldFont = new SKFont(boldTypeface, BodyFontSize);
        using var titleFont = new SKFont(boldTypeface, TitleFontSize);
        using var smallFont = new SKFont(regularTypeface, SmallFontSize);
        using var textPaint = new SKPaint { Color = TextColor, IsAntialias = true };
        using var primaryPaint = new SKPaint { Color = PrimaryColor, IsAntialias = true };
        using var secondaryPaint = new SKPaint { Color = SecondaryTextColor, IsAntialias = true };
        using var lightTextPaint = new SKPaint { Color = LightTextColor, IsAntialias = true };
        using var whitePaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        using var fillPaint = new SKPaint { IsAntialias = true };

        var metrics = bodyFont.Metrics;
        var style = new RenderStyle(bodyFont, boldFont, titleFont, smallFont, textPaint, primaryPaint, secondaryPaint, lightTextPaint, whitePaint, fillPaint, (metrics.Descent - metrics.Ascent) * 1.2f);

        var contentWidth = CanvasWidth - Padding * 2;
        var blocks = await BuildBlocksAsync(contentList, contentWidth, contentWidth, style, excludeMediaExceptFirst);
        if (hasHeader)
        {
            var headerBlock = await BuildHeaderBlockAsync(post.ProfileMedia?.Uri, post.Nickname, BuildFullTimestampText(post.CreatedAt, post.ModifiedAt), style);
            if (headerBlock != null) blocks.Insert(0, headerBlock);
        }
        if (comments != null) blocks.AddRange(await BuildCommentBlocksAsync(comments, contentWidth, style, excludeMediaExceptFirst));
        if (blocks.Count == 0) return null;

        var totalHeight = (int)Math.Ceiling(Padding * 2 + blocks.Sum(x => x.Height) + ContentSpacing * Math.Max(0, blocks.Count - 1));
        var info = new SKImageInfo(CanvasWidth, Math.Max(totalHeight, 1));
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        var y = Padding;
        foreach (var block in blocks)
        {
            block.Draw(canvas, Padding, y);
            y += block.Height + ContentSpacing;
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    });

    /// <summary>
    /// Renders the post contents with a header derived from the shared post view model
    /// surface (profile media URI, nickname, absolute timestamp) and saves the resulting
    /// PNG to the device gallery. Comments are appended below the contents when provided.
    /// Mirrors the save flow of FullScreenMediaViewerPage (permission → temp file →
    /// gallery → cleanup) and surfaces the result through a toast or an error alert.
    /// </summary>
    public static async Task SaveAsync(IEnumerable<BaseContent> contents, BasePostViewModel post = null, IEnumerable<BaseCommentViewModel> comments = null)
    {
#if !WINDOWS
        var status = await Permissions.RequestAsync<SaveMediaPermission>();
        if (status != PermissionStatus.Granted) return;
#endif

        byte[] bytes;
        try { bytes = await RenderAsync(contents, post, comments); }
        catch
        {
            await App.TopPage.DisplayAlertAsync("오류", "게시글 이미지 생성 중 오류가 발생하였습니다.", Constants.PromptOk);
            return;
        }

        if (bytes == null)
        {
            await App.TopPage.DisplayAlertAsync("오류", "이미지로 저장할 내용이 없습니다.", Constants.PromptOk);
            return;
        }

        var fileName = $"post_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        var filePath = Path.Combine(Path.GetTempPath(), fileName);

        try
        {
            await File.WriteAllBytesAsync(filePath, bytes);
#if WINDOWS
            await WindowsMediaPickerHelper.SaveMediaAsync(filePath);
#else
            await MediaGallery.SaveAsync(MediaFileType.Image, filePath);
#endif
            await Toast.Make("게시글 이미지가 저장되었습니다.").Show();
        }
        catch { await App.TopPage.DisplayAlertAsync("오류", "게시글 이미지 저장 중 오류가 발생하였습니다.", Constants.PromptOk); }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    private static async Task<List<RenderBlock>> BuildBlocksAsync(List<BaseContent> contents, float contentWidth, float maxMediaWidth, RenderStyle style, bool excludeMediaExceptFirst = false)
    {
        var blocks = new List<RenderBlock>();
        var textRuns = new List<TextRun>();
        var firstMediaRendered = false;

        void FlushText()
        {
            if (textRuns.Count == 0) return;

            var lines = WrapRuns(textRuns, contentWidth);
            if (lines.Count > 0)
            {
                var height = lines.Count * style.LineHeight;
                blocks.Add(new RenderBlock(height, (canvas, x, y) =>
                {
                    var baseline = y - style.BodyFont.Metrics.Ascent;
                    foreach (var line in lines)
                    {
                        var cursor = x;
                        foreach (var run in line.Runs)
                        {
                            canvas.DrawText(run.Text, cursor, baseline, SKTextAlign.Left, run.Font, run.Paint);
                            cursor += run.Font.MeasureText(run.Text);
                        }
                        baseline += style.LineHeight;
                    }
                }));
            }
            textRuns.Clear();
        }

        foreach (var content in contents)
        {
            if (content is TextContent textContent) AddRuns(textRuns, textContent.Text ?? string.Empty, style.BodyFont, style.TextPaint);
            else if (content is ProfileContent profileContent) AddRuns(textRuns, profileContent.Nickname ?? string.Empty, style.BoldFont, style.PrimaryPaint);
            else if (content is HashtagContent hashtagContent) AddRuns(textRuns, $"#{hashtagContent.Tag}", style.BoldFont, style.PrimaryPaint);
            else if (content is HyperlinkContent hyperlinkContent) AddRuns(textRuns, hyperlinkContent.Url ?? string.Empty, style.BodyFont, style.PrimaryPaint);
            else
            {
                FlushText();

                if (content is StickerContent stickerContent)
                {
                    var block = await BuildStickerBlockAsync(stickerContent, style);
                    if (block != null) blocks.Add(block);
                }
                else if (content is MediaContent mediaContent && (!excludeMediaExceptFirst || !firstMediaRendered))
                {
                    var block = await BuildMediaBlockAsync(mediaContent, contentWidth, maxMediaWidth, style);
                    if (block != null)
                    {
                        firstMediaRendered = true;
                        blocks.Add(block);
                    }
                }
                else if (content is PollContent pollContent)
                {
                    blocks.Add(BuildPollBlock(pollContent, contentWidth, style));
                }
                else if (content is ExternalUrlContent externalUrlContent)
                {
                    var block = await BuildExternalBlockAsync(externalUrlContent, contentWidth, style);
                    if (block != null) blocks.Add(block);
                }
            }
        }
        FlushText();

        return blocks;
    }

    private static async Task<RenderBlock> BuildHeaderBlockAsync(string profileImageUrl, string nickname, string timestampText, RenderStyle style)
    {
        var hasProfile = profileImageUrl != null;
        var image = hasProfile ? await DownloadProfileImageOrDefaultAsync(profileImageUrl) : null;
        if (image == null) hasProfile = false;

        var hasName = !string.IsNullOrEmpty(nickname);
        var hasTimestamp = !string.IsNullOrEmpty(timestampText);
        if (!hasProfile && !hasName && !hasTimestamp) return null;

        var maxTextWidth = CanvasWidth - Padding * 2 - HeaderProfileSize - HeaderColumnSpacing;
        var nameText = hasName ? TruncateText(nickname, style.TitleFont, maxTextWidth) : null;
        var timeText = hasTimestamp ? TruncateText(timestampText, style.BodyFont, maxTextWidth) : null;

        // PostContentTemplate: the name(16dp bold) + timestamp(14dp) stack is
        // vertically centered inside the 48dp header cell (VerticalOptions=Center).
        // Center the stack by its exact line-height sum (no extra gap — the font
        // line height provides the natural spacing between the two rows).
        var nameLineHeight = style.TitleFont.Metrics.Descent - style.TitleFont.Metrics.Ascent;
        var timeLineHeight = style.BodyFont.Metrics.Descent - style.BodyFont.Metrics.Ascent;
        var stackHeight = nameLineHeight + timeLineHeight;

        return new RenderBlock(HeaderProfileSize, (canvas, x, y) =>
        {
            try
            {
                if (hasProfile)
                {
                    canvas.Save();
                    var center = new SKPoint(x + HeaderProfileSize / 2, y + HeaderProfileSize / 2);
                    var circleBuilder = new SKPathBuilder();
                    circleBuilder.AddCircle(center.X, center.Y, HeaderProfileSize / 2);
                    canvas.ClipPath(circleBuilder.Detach());

                    // Cover crop (AspectFill) so the full circle is filled
                    var scale = Math.Max(HeaderProfileSize / (float)image.Width, HeaderProfileSize / (float)image.Height);
                    var drawWidth = image.Width * scale;
                    var drawHeight = image.Height * scale;
                    var dest = new SKRect(center.X - drawWidth / 2, center.Y - drawHeight / 2, center.X + drawWidth / 2, center.Y + drawHeight / 2);
                    canvas.DrawImage(image, dest, new SKSamplingOptions(SKFilterMode.Linear));
                    canvas.Restore();
                }

                var textLeft = x + HeaderProfileSize + HeaderColumnSpacing;
                var middleY = y + HeaderProfileSize / 2;
                if (hasName && hasTimestamp)
                {
                    var stackTop = middleY - stackHeight / 2;
                    var nameBaseline = stackTop - style.TitleFont.Metrics.Ascent;
                    canvas.DrawText(nameText, textLeft, nameBaseline, SKTextAlign.Left, style.TitleFont, style.TextPaint);
                    canvas.DrawText(timeText, textLeft, nameBaseline + nameLineHeight, SKTextAlign.Left, style.BodyFont, style.SecondaryPaint);
                }
                else if (hasName)
                {
                    canvas.DrawText(nameText, textLeft, middleY - style.TitleFont.Metrics.Ascent, SKTextAlign.Left, style.TitleFont, style.TextPaint);
                }
                else if (hasTimestamp)
                {
                    canvas.DrawText(timeText, textLeft, middleY - style.BodyFont.Metrics.Ascent, SKTextAlign.Left, style.BodyFont, style.SecondaryPaint);
                }
            }
            finally { image?.Dispose(); }
        });
    }

    // Assembles a single comment block mirroring CommentTemplate: a circular profile
    // avatar on the left and a text column on the right (bold nickname, contents via
    // the shared block builders, absolute timestamp). Media inside a comment is capped
    // at the comment UI width (200dp x 3) because the comment carousel limits it.
    private static async Task<RenderBlock> BuildCommentBlockAsync(BaseCommentViewModel comment, float contentWidth, RenderStyle style, bool excludeMediaExceptFirst = false)
    {
        var image = await DownloadProfileImageOrDefaultAsync(comment.ProfileMedia?.Uri);
        var hasProfile = image != null;

        var columnWidth = contentWidth - CommentProfileSize - CommentColumnSpacing;
        var innerBlocks = await BuildBlocksAsync(comment.GetRenderRawContents() ?? [], columnWidth, CommentMediaMaxWidth, style, excludeMediaExceptFirst);
        var innerHeight = innerBlocks.Count > 0 ? innerBlocks.Sum(block => block.Height) + ContentSpacing * (innerBlocks.Count - 1) : 0;

        var nameLineHeight = style.BoldFont.Metrics.Descent - style.BoldFont.Metrics.Ascent;
        var timestampLineHeight = style.SmallFont.Metrics.Descent - style.SmallFont.Metrics.Ascent;
        var columnHeight = nameLineHeight + CommentRowSpacing + innerHeight + CommentRowSpacing + timestampLineHeight;
        var height = Math.Max(CommentProfileSize, columnHeight);

        return new RenderBlock(height, (canvas, x, y) =>
        {
            try
            {
                if (hasProfile)
                {
                    canvas.Save();
                    var center = new SKPoint(x + CommentProfileSize / 2, y + CommentProfileSize / 2);
                    var circleBuilder = new SKPathBuilder();
                    circleBuilder.AddCircle(center.X, center.Y, CommentProfileSize / 2);
                    canvas.ClipPath(circleBuilder.Detach());

                    // Cover crop (AspectFill) so the full circle is filled
                    var scale = Math.Max(CommentProfileSize / (float)image.Width, CommentProfileSize / (float)image.Height);
                    var drawWidth = image.Width * scale;
                    var drawHeight = image.Height * scale;
                    var dest = new SKRect(center.X - drawWidth / 2, center.Y - drawHeight / 2, center.X + drawWidth / 2, center.Y + drawHeight / 2);
                    canvas.DrawImage(image, dest, new SKSamplingOptions(SKFilterMode.Linear));
                    canvas.Restore();
                }

                var textLeft = x + CommentProfileSize + CommentColumnSpacing;
                var nickname = TruncateText(comment.Nickname, style.BoldFont, columnWidth);
                if (nickname != null) canvas.DrawText(nickname, textLeft, y - style.BoldFont.Metrics.Ascent, SKTextAlign.Left, style.BoldFont, style.TextPaint);

                var cursorY = y + nameLineHeight + CommentRowSpacing;
                for (var index = 0; index < innerBlocks.Count; index++)
                {
                    innerBlocks[index].Draw(canvas, textLeft, cursorY);
                    cursorY += innerBlocks[index].Height;
                    if (index < innerBlocks.Count - 1) cursorY += ContentSpacing;
                }

                canvas.DrawText(BuildFullTimestampText(comment.CreatedAt, comment.ModifiedAt), textLeft, cursorY + CommentRowSpacing - style.SmallFont.Metrics.Ascent, SKTextAlign.Left, style.SmallFont, style.SecondaryPaint);
            }
            finally { image?.Dispose(); }
        });
    }

    // Assembles all comment blocks into a single list, inserting a thin light-gray
    // separator before the section so the comment area is visually distinct from the
    // post contents. Returns an empty list when there are no comments.
    private static async Task<List<RenderBlock>> BuildCommentBlocksAsync(IEnumerable<BaseCommentViewModel> comments, float contentWidth, RenderStyle style, bool excludeMediaExceptFirst = false)
    {
        var blocks = new List<RenderBlock>();
        var commentList = comments?.ToList() ?? [];
        if (commentList.Count == 0) return blocks;

        blocks.Add(new RenderBlock(CommentSeparatorHeight + ContentSpacing * 2, (canvas, x, y) =>
        {
            style.FillPaint.Color = ProgressTrackColor;
            canvas.DrawRect(x, y + ContentSpacing, contentWidth, CommentSeparatorHeight, style.FillPaint);
        }));

        foreach (var comment in commentList)
        {
            var block = await BuildCommentBlockAsync(comment, contentWidth, style, excludeMediaExceptFirst);
            if (block != null) blocks.Add(block);
        }
        return blocks;
    }

    // Downloads the profile image, falling back to the bundled default profile image
    // when the URL is missing or the download fails.
    private static async Task<SKImage> DownloadProfileImageOrDefaultAsync(string profileImageUrl)
    {
        if (profileImageUrl != null)
        {
            var image = await DownloadImageAsync(profileImageUrl);
            if (image != null) return image;
        }

        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(Constants.DefaultProfileImageFileName);
            using var data = SKData.Create(stream);
            return SKImage.FromEncodedData(data);
        }
        catch { return null; }
    }

    private static async Task<RenderBlock> BuildStickerBlockAsync(StickerContent stickerContent, RenderStyle style)
    {
        var mediaId = stickerContent.StickerMediaId;
        var isKakaoEmoticon = mediaId != null && mediaId.StartsWith(KakaoEmoticonUriHelper.EmoticonUrlPrefix, StringComparison.Ordinal);
        var url = Uri.IsWellFormedUriString(mediaId, UriKind.Absolute) ? mediaId : Utils.GenerateMediaUri(mediaId);
        var image = await DownloadImageAsync(url, isKakaoEmoticon ? KakaoEmoticonUriHelper.KakaoStoryReferer : null);
        if (image == null) return null;

        return new RenderBlock(StickerSize, (canvas, x, y) =>
        {
            try
            {
                var scale = Math.Min(StickerSize / (float)image.Width, StickerSize / (float)image.Height);
                var drawWidth = image.Width * scale;
                var drawHeight = image.Height * scale;
                var dest = new SKRect(x, y + (StickerSize - drawHeight) / 2, x + drawWidth, y + (StickerSize + drawHeight) / 2);
                canvas.DrawImage(image, dest, new SKSamplingOptions(SKFilterMode.Linear), style.WhitePaint);
            }
            finally { image.Dispose(); }
        });
    }

    private static async Task<RenderBlock> BuildMediaBlockAsync(MediaContent mediaContent, float contentWidth, float maxMediaWidth, RenderStyle style)
    {
        var isVideo = mediaContent.IsVideo;
        var mediaId = isVideo ? mediaContent.ThumbnailMediaId ?? mediaContent.MediaId : mediaContent.MediaId;
        var image = mediaId != null ? await DownloadImageAsync(Uri.IsWellFormedUriString(mediaId, UriKind.Absolute) ? mediaId : Utils.GenerateMediaUri(mediaId)) : null;

        var drawWidth = Math.Min(contentWidth, maxMediaWidth);
        var drawHeight = drawWidth;
        if (image != null)
        {
            var aspect = (float)image.Width / image.Height;
            drawHeight = drawWidth / aspect;
        }

        var hasDescription = !string.IsNullOrEmpty(mediaContent.Description);
        var height = drawHeight;

        return new RenderBlock(height, (canvas, x, y) =>
        {
            try
            {
                var mediaRect = new SKRect(x, y, x + drawWidth, y + drawHeight);
                canvas.Save();
                canvas.ClipRoundRect(new SKRoundRect(mediaRect, MediaCornerRadius));

                if (image != null)
                {
                    canvas.DrawImage(image, mediaRect, new SKSamplingOptions(SKFilterMode.Linear), style.WhitePaint);
                }
                else
                {
                    style.FillPaint.Color = ProgressTrackColor;
                    canvas.DrawRect(mediaRect, style.FillPaint);
                }

                if (isVideo)
                {
                    style.FillPaint.Color = OverlayColor;
                    var center = new SKPoint(mediaRect.MidX, mediaRect.MidY);
                    canvas.DrawCircle(center, PlayCircleRadius, style.FillPaint);

                    var tri = PlayCircleRadius * 0.5f;
                    var triangle = new SKPathBuilder();
                    triangle.MoveTo(center.X - tri * 0.6f, center.Y - tri);
                    triangle.LineTo(center.X - tri * 0.6f, center.Y + tri);
                    triangle.LineTo(center.X + tri, center.Y);
                    triangle.Close();
                    style.FillPaint.Color = SKColors.White;
                    canvas.DrawPath(triangle.Detach(), style.FillPaint);
                }
                // Description overlay (top, mirrors MediaContentTemplate: 40dp bar at VerticalOptions=Start)
                if (hasDescription)
                {
                    var barRect = new SKRect(x, y, x + drawWidth, y + DescriptionBarHeight);
                    style.FillPaint.Color = OverlayColor;
                    canvas.DrawRect(barRect, style.FillPaint);

                    var lines = WrapRuns([new TextRun(mediaContent.Description, style.BoldFont, style.WhitePaint)], drawWidth);
                    var fontHeight = style.BoldFont.Metrics.Descent - style.BoldFont.Metrics.Ascent;
                    var baseline = barRect.MidY - (lines.Count - 1) * style.LineHeight / 2 - fontHeight / 2 - style.BoldFont.Metrics.Ascent;
                    foreach (var line in lines.Take(2))
                    {
                        var lineWidth = line.Runs.Sum(run => run.Font.MeasureText(run.Text));
                        var cursor = barRect.MidX - lineWidth / 2;
                        foreach (var run in line.Runs)
                        {
                            canvas.DrawText(run.Text, cursor, baseline, SKTextAlign.Left, run.Font, run.Paint);
                            cursor += run.Font.MeasureText(run.Text);
                        }
                        baseline += style.LineHeight;
                    }
                }
                canvas.Restore();
            }
            finally { image?.Dispose(); }
        });
    }

    private static RenderBlock BuildPollBlock(PollContent pollContent, float contentWidth, RenderStyle style)
    {
        var innerWidth = contentWidth - CardPadding * 2;
        var questionLines = WrapRuns([new TextRun(pollContent.Question ?? string.Empty, style.TitleFont, style.TextPaint)], innerWidth);
        var questionHeight = questionLines.Count * style.LineHeight;

        var showResults = pollContent.TotalVotes > 0;
        var options = new List<(List<TextLine> Lines, float Percentage, string PercentageText)>();
        var optionsHeight = 0f;
        foreach (var option in pollContent.Options ?? [])
        {
            var percentage = showResults ? (double)option.VoteCount / pollContent.TotalVotes : 0;
            var percentageText = showResults ? $"{percentage:P0}" : null;

            var measuredWidth = innerWidth - OptionPaddingX * 2 - (percentageText != null ? style.BoldFont.MeasureText(percentageText) : 0);
            var optionLines = WrapRuns([new TextRun(option.Text ?? string.Empty, style.BodyFont, style.TextPaint)], measuredWidth);
            var optionHeight = optionLines.Count * style.LineHeight + OptionPaddingY * 2 + (showResults ? ProgressBarHeight : 0);

            options.Add((optionLines, (float)percentage, percentageText));
            optionsHeight += optionHeight;
        }
        optionsHeight += Math.Max(0, options.Count - 1) * OptionSpacing;

        var footerText = $"{pollContent.TotalVoters}명 참여";
        var expiresText = GetExpiresAtText(pollContent);
        var height = CardPadding * 2 + questionHeight + optionsHeight + 12 + style.SmallFont.Size;

        return new RenderBlock(height, (canvas, x, y) =>
        {
            var cardRect = new SKRect(x, y, x + contentWidth, y + height);
            style.FillPaint.Color = CardColor;
            canvas.DrawRoundRect(cardRect, CardCornerRadius, CardCornerRadius, style.FillPaint);

            var cursorY = y + CardPadding;

            // Question
            var questionBaseline = cursorY - style.TitleFont.Metrics.Ascent;
            foreach (var line in questionLines)
            {
                var lineX = x + CardPadding;
                foreach (var run in line.Runs)
                {
                    canvas.DrawText(run.Text, lineX, questionBaseline, SKTextAlign.Left, run.Font, run.Paint);
                    lineX += run.Font.MeasureText(run.Text);
                }
                questionBaseline += style.LineHeight;
            }
            cursorY += questionHeight + 12;

            // Options
            foreach (var (optionLines, percentage, percentageText) in options)
            {
                var optionTextHeight = optionLines.Count * style.LineHeight;
                var optionRect = new SKRect(x + CardPadding, cursorY, x + contentWidth - CardPadding, cursorY + optionTextHeight + OptionPaddingY * 2 + (showResults ? ProgressBarHeight : 0));
                style.FillPaint.Color = SKColors.White;
                canvas.DrawRoundRect(optionRect, OptionCornerRadius, OptionCornerRadius, style.FillPaint);

                var innerY = optionRect.Top + OptionPaddingY;
                if (showResults)
                {
                    var trackRect = new SKRect(optionRect.Left + OptionPaddingX, innerY, optionRect.Right - OptionPaddingX, innerY + ProgressBarHeight);
                    style.FillPaint.Color = ProgressTrackColor;
                    canvas.DrawRoundRect(trackRect, ProgressBarHeight / 2, ProgressBarHeight / 2, style.FillPaint);

                    if (percentage > 0)
                    {
                        var fillRect = new SKRect(trackRect.Left, trackRect.Top, trackRect.Left + trackRect.Width * percentage, trackRect.Bottom);
                        style.FillPaint.Color = PrimaryColor;
                        canvas.DrawRoundRect(fillRect, ProgressBarHeight / 2, ProgressBarHeight / 2, style.FillPaint);
                    }

                    innerY += ProgressBarHeight;
                }

                var textBaseline = innerY - style.BodyFont.Metrics.Ascent;
                foreach (var line in optionLines)
                {
                    var lineX = optionRect.Left + OptionPaddingX;
                    foreach (var run in line.Runs)
                    {
                        canvas.DrawText(run.Text, lineX, textBaseline, SKTextAlign.Left, run.Font, run.Paint);
                        lineX += run.Font.MeasureText(run.Text);
                    }
                    textBaseline += style.LineHeight;
                }

                if (percentageText != null)
                {
                    var percentageWidth = style.BoldFont.MeasureText(percentageText);
                    canvas.DrawText(percentageText, optionRect.Right - OptionPaddingX - percentageWidth, optionRect.Top + OptionPaddingY + (showResults ? ProgressBarHeight : 0) - style.BoldFont.Metrics.Ascent, SKTextAlign.Left, style.BoldFont, style.PrimaryPaint);
                }

                cursorY += optionRect.Height + OptionSpacing;
            }

            // Footer
            var footerBaseline = cursorY - OptionSpacing + style.SmallFont.Metrics.Ascent;
            canvas.DrawText(footerText, x + CardPadding, footerBaseline, SKTextAlign.Left, style.SmallFont, style.SecondaryPaint);
            var expiresWidth = style.SmallFont.MeasureText(expiresText);
            canvas.DrawText(expiresText, x + contentWidth - CardPadding - expiresWidth, footerBaseline, SKTextAlign.Left, style.SmallFont, style.SecondaryPaint);
        });
    }

    private static async Task<RenderBlock> BuildExternalBlockAsync(ExternalUrlContent externalUrlContent, float contentWidth, RenderStyle style)
    {
        var image = externalUrlContent.ThumbnailImageUrl != null ? await DownloadImageAsync(externalUrlContent.ThumbnailImageUrl) : null;
        var height = ExternalUrlHeight;

        return new RenderBlock(height, (canvas, x, y) =>
        {
            try
            {
                var rect = new SKRect(x, y, x + contentWidth, y + height);
                canvas.Save();
                canvas.ClipRoundRect(new SKRoundRect(rect, MediaCornerRadius));

                if (image != null)
                {
                    var scale = Math.Max(contentWidth / (float)image.Width, height / (float)image.Height);
                    var drawWidth = image.Width * scale;
                    var drawHeight = image.Height * scale;
                    var dest = new SKRect(rect.MidX - drawWidth / 2, rect.MidY - drawHeight / 2, rect.MidX + drawWidth / 2, rect.MidY + drawHeight / 2);
                    canvas.DrawImage(image, dest, new SKSamplingOptions(SKFilterMode.Linear), style.WhitePaint);
                }
                else
                {
                    style.FillPaint.Color = ProgressTrackColor;
                    canvas.DrawRect(rect, style.FillPaint);
                }

                style.FillPaint.Color = ExternalOverlayColor;
                canvas.DrawRect(rect, style.FillPaint);
                canvas.Restore();

                // Bottom text stack (description → title → domain), matching the template order
                var textLeft = rect.Left + 24;
                var textRight = rect.Right - 24;
                var baseline = rect.Bottom - 36;

                var description = TruncateText(externalUrlContent.Description, style.BodyFont, textRight - textLeft);
                if (description != null)
                {
                    canvas.DrawText(description, textLeft, baseline, SKTextAlign.Left, style.BodyFont, style.LightTextPaint);
                    baseline -= (style.BodyFont.Metrics.Descent - style.BodyFont.Metrics.Ascent) + 6;
                }

                var title = TruncateText(externalUrlContent.Title, style.TitleFont, textRight - textLeft);
                if (title != null)
                {
                    canvas.DrawText(title, textLeft, baseline, SKTextAlign.Left, style.TitleFont, style.WhitePaint);
                    baseline -= (style.TitleFont.Metrics.Descent - style.TitleFont.Metrics.Ascent) + 6;
                }

                var domain = TruncateText(externalUrlContent.Domain, style.SmallFont, textRight - textLeft);
                if (domain != null) canvas.DrawText(domain, textLeft, baseline, SKTextAlign.Left, style.SmallFont, style.LightTextPaint);
            }
            finally { image?.Dispose(); }
        });
    }

    private static void AddRuns(List<TextRun> runs, string text, SKFont font, SKPaint paint)
    {
        if (string.IsNullOrEmpty(text)) return;

        var segments = text.Split('\n');
        for (var i = 0; i < segments.Length; i++)
        {
            if (i > 0) runs.Add(TextRun.LineBreak);
            if (segments[i].Length == 0) continue;
            runs.Add(new TextRun(segments[i], font, paint));
        }
    }

    private static List<TextLine> WrapRuns(List<TextRun> inputRuns, float maxWidth)
    {
        var lines = new List<TextLine>();
        var currentX = 0f;
        var currentLine = new TextLine();

        foreach (var run in inputRuns)
        {
            if (run.IsLineBreak)
            {
                lines.Add(currentLine);
                currentLine = new TextLine();
                currentX = 0f;
                continue;
            }

            var remaining = run.Text;
            while (remaining.Length > 0)
            {
                var available = maxWidth - currentX;
                var count = available > 0 ? run.Font.BreakText(remaining, available) : 0;
                if (count <= 0)
                {
                    if (currentLine.Runs.Count == 0) count = 1;
                    else
                    {
                        lines.Add(currentLine);
                        currentLine = new TextLine();
                        currentX = 0f;
                        continue;
                    }
                }

                var part = remaining[..count];
                currentLine.Runs.Add(new TextRun(part, run.Font, run.Paint));
                currentX += run.Font.MeasureText(part);
                remaining = remaining[count..];
                if (remaining.Length > 0)
                {
                    lines.Add(currentLine);
                    currentLine = new TextLine();
                    currentX = 0f;
                }
            }
        }

        if (currentLine.Runs.Count > 0) lines.Add(currentLine);
        return lines;
    }

    private static string TruncateText(string text, SKFont font, float maxWidth)
    {
        if (string.IsNullOrEmpty(text)) return null;
        if (font.MeasureText(text) <= maxWidth) return text;

        var ellipsisWidth = font.MeasureText("…");
        var count = font.BreakText(text, maxWidth - ellipsisWidth);
        if (count <= 0) return "…";
        return text[..count] + "…";
    }

    private static string GetExpiresAtText(PollContent poll)
    {
        if (poll.ExpiresAt == null) return "마감 없음";
        if (poll.IsExpired) return "마감됨";

        var remaining = poll.ExpiresAt.Value - DateTime.UtcNow;
        if (remaining.TotalDays >= 1) return $"{remaining.Days}일 남음";
        if (remaining.TotalHours >= 1) return $"{remaining.Hours}시간 남음";
        if (remaining.TotalMinutes >= 1) return $"{remaining.Minutes}분 남음";
        return "곧 마감";
    }

    private static async Task<(SKTypeface Regular, SKTypeface Bold)> GetTypefacesAsync()
    {
        if (s_regularTypeface != null) return (s_regularTypeface, s_boldTypeface);

        await s_typefaceSemaphore.WaitAsync();
        try
        {
            if (s_regularTypeface != null) return (s_regularTypeface, s_boldTypeface);

            SKTypeface typeface = null;
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("PretendardVariable.ttf");
                using var data = SKData.Create(stream);
                typeface = SKTypeface.FromData(data);
            }
            catch { }

            typeface ??= SKTypeface.Default;
            s_regularTypeface = typeface;
            s_boldTypeface = CreateBoldTypeface(typeface) ?? typeface;
            return (s_regularTypeface, s_boldTypeface);
        }
        finally { s_typefaceSemaphore.Release(); }
    }

    private static SKTypeface CreateBoldTypeface(SKTypeface typeface)
    {
        try
        {
            var axes = typeface.VariationDesignParameters;
            var wghtAxis = axes.FirstOrDefault(axis => axis.Tag.ToString() == "wght");
            if (wghtAxis.Tag == default) return null;

            var position = new SKFontVariationPositionCoordinate
            {
                Axis = wghtAxis.Tag,
                Value = 700
            };
            return typeface.Clone([position]);
        }
        catch { return null; }
    }

    private static async Task<SKImage> DownloadImageAsync(string url, string referer = null)
    {
        if (string.IsNullOrEmpty(url)) return null;
        try
        {
            if (referer != null)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Referrer = new Uri(referer);
                using var response = await s_httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;
                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                return SKImage.FromEncodedData(imageBytes);
            }

            var bytes = await s_httpClient.GetByteArrayAsync(url);
            return SKImage.FromEncodedData(bytes);
        }
        catch { return null; }
    }

    // Aggregates the fonts, paints, and line height shared across the block builders
    // so signatures don't have to thread every SkiaSharp object individually.
    private sealed record RenderStyle(SKFont BodyFont, SKFont BoldFont, SKFont TitleFont, SKFont SmallFont, SKPaint TextPaint, SKPaint PrimaryPaint, SKPaint SecondaryPaint, SKPaint LightTextPaint, SKPaint WhitePaint, SKPaint FillPaint, float LineHeight);

    private sealed class RenderBlock(float height, Action<SKCanvas, float, float> draw)
    {
        public float Height { get; } = height;

        public void Draw(SKCanvas canvas, float x, float y) => draw(canvas, x, y);
    }

    private sealed class TextRun(string text, SKFont font, SKPaint paint)
    {
        public static TextRun LineBreak { get; } = new(string.Empty, null, null);

        public string Text { get; } = text;
        public SKFont Font { get; } = font;
        public SKPaint Paint { get; } = paint;
        public bool IsLineBreak => Font == null;
    }

    private sealed class TextLine
    {
        public List<TextRun> Runs { get; } = [];
    }
}
