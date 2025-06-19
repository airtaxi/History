using History.ApiService.Services.Interfaces;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.Enums;

namespace History.ApiService.Services
{
    public class BirthdayService(ILogger<BirthdayService> logger, IUserService userService, IPostService postService) : IHostedService, IDisposable
    {
        private Timer _timer;
        private DateTime _lastTaskRun = DateTime.MinValue;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _lastTaskRun = Configuration.GetValue<DateTime>("LastBirthdayServiceRun");
            _timer = new Timer(ExecuteTask, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));

            var currentTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Asia/Seoul");
            logger.LogInformation("Birthday service started at {Time} / Last run: {LastRun} / Current time: {CurrentTime} / Started : {Started}",
                DateTime.UtcNow, _lastTaskRun, currentTime, IsStartTime(currentTime));

            return Task.CompletedTask;
        }

        private void ExecuteTask(object state)
        {
            var currentTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Asia/Seoul");
            if (IsStartTime(currentTime) && _lastTaskRun.Date != currentTime.Date)
            {
                _lastTaskRun = currentTime;
                Configuration.SetValue("LastBirthdayServiceRun", currentTime.Date);
                RunTask();
            }
        }

        private void RunTask()
        {
            logger.LogInformation("Starting birthday service task at {Time}", DateTime.UtcNow);
            _ = Task.Run(async () =>
            {
                var currentTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Asia/Seoul");
                var today = currentTime.Date;
                var birthdayUsersResult = await userService.GetUsersByBirthdayAsync(today);
                var birthdayUsers = birthdayUsersResult.Value;

                foreach (var user in birthdayUsers)
                {
                    var contents = new List<BaseContent>
                    {
                        new TextContent() { Text = "안녕하세요, " },
                        new ProfileContent() { UserId = user.Id, Nickname = user.Nickname },
                        new TextContent() { Text = "님\n생일을 진심으로 축하드립니다!\n오늘 하루는 누구보다도 특별하고, 기쁨이 가득한 날이 되시길 바랍니다.\n늘 노력하시고 주변에 좋은 에너지를 전해주시는 " },
                        new ProfileContent() { UserId = user.Id, Nickname = user.Nickname },
                        new TextContent() { Text = "님께 감사드리며, \n앞으로의 날들에도 건강과 행운이 함께하길 기원합니다.\n다시 한 번 생일 축하드립니다 🎁🎈\n행복한 하루 되세요!\n따뜻한 마음을 담아,\n히스토리 개발자 " },
                        new ProfileContent() { UserId = "101978644582797207383", Nickname = "이호원" },
                        new TextContent() { Text = "드림" },
                        new MediaContent()
                        {
                            Description = "생일 축하합니다! 🎉",
                            MediaId = "birthday",
                            MimeType = "image/webp",
                            ThumbnailMediaId = "birthday"
                        }
                    };

                    await postService.WritePostAsync(user.Id, new WritePostRequestDto()
                    {
                        Contents = contents,
                        DiscoveryOption = DiscoveryOption.Friends
                    }, []);
                }
            });
        }

        private static bool IsStartTime(DateTime currentTime) => currentTime.Hour >= 12;

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
