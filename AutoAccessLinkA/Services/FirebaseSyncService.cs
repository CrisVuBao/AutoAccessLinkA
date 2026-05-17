using AutoAccessLinkA.Models;
using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoAccessLinkA.Services
{
    public class FirebaseSyncService
    {
        private readonly FirebaseClient _firebaseClient;
        private readonly MeetAutomationService _automationService;

        public FirebaseSyncService()
        {
            // URL từ Firebase Realtime Database
            _firebaseClient = new FirebaseClient("https://autoacceinka-default-rtdb.firebaseio.com");
            _automationService = new MeetAutomationService();
        }

        // Dùng cho Mobile (hoặc UI) để đẩy lệnh
        public async Task PushCommandAsync(MeetCommand command)
        {
            await _firebaseClient
                .Child("Commands")
                .Child(command.Id)
                .PutAsync(command);
        }

        // Cập nhật trạng thái lệnh
        public async Task UpdateCommandStatusAsync(string commandId, string status)
        {
            await _firebaseClient
                .Child("Commands")
                .Child(commandId)
                .PatchAsync(new { Status = status });
        }

        // --- TÍNH NĂNG SAVED LINKS ---
        public async Task SaveLinkAsync(SavedLink link)
        {
            await _firebaseClient
                .Child("SavedLinks")
                .Child(link.Id)
                .PutAsync(link);
        }

        public async Task DeleteLinkAsync(string linkId)
        {
            await _firebaseClient
                .Child("SavedLinks")
                .Child(linkId)
                .DeleteAsync();
        }

        public IObservable<Firebase.Database.Streaming.FirebaseEvent<SavedLink>> GetSavedLinksAsObservable()
        {
            return _firebaseClient
                .Child("SavedLinks")
                .AsObservable<SavedLink>();
        }

        // --- TÍNH NĂNG ĐỒNG BỘ TRẠNG THÁI (HEARTBEAT) ---
        public IObservable<Firebase.Database.Streaming.FirebaseEvent<string>> GetPcStateAsObservable()
        {
            return _firebaseClient
                .Child("Status")
                .Child("ExecutorState")
                .AsObservable<string>();
        }

#if WINDOWS
        // Báo cáo trạng thái PC liên tục (Heartbeat)
        public async Task StartPcHeartbeat()
        {
            await Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await _firebaseClient
                            .Child("Status")
                            .Child("ExecutorState")
                            .PutAsync("Online");
                        await Task.Delay(30000); // 30s báo 1 lần
                    }
                    catch
                    {
                        await Task.Delay(5000); // Chờ 5s rồi thử lại nếu lỗi
                    }
                }
            });
        }

        // Dùng cho Desktop để lắng nghe lệnh mới
        public void ListenForCommands(Action<string> onLogMessage)
        {
            _firebaseClient
                .Child("Commands")
                .AsObservable<MeetCommand>()
                .Subscribe(async d =>
                {
                    if (d.EventType == Firebase.Database.Streaming.FirebaseEventType.InsertOrUpdate)
                    {
                        var command = d.Object;
                        if (command != null && command.Status == "Pending")
                        {
                            onLogMessage?.Invoke($"[Cloud] Phát hiện lệnh mới: {command.MeetLink}");

                            // 1. Đổi trạng thái sang Executing để không bị xử lý lại
                            await UpdateCommandStatusAsync(command.Id, "Executing");

                            // 2. Tính toán độ trễ thời gian (Từ lúc này đến ScheduledTime)
                            TimeSpan delay = command.ScheduledTime - DateTime.Now;

                            if (delay.TotalMilliseconds > 0)
                            {
                                onLogMessage?.Invoke($"[Cloud] Đang đếm ngược {delay.ToString(@"hh\:mm\:ss")} đến thời điểm chạy...");
                                await Task.Delay(delay);
                            }

                            // 3. Thực thi Automation
                            onLogMessage?.Invoke($"[Cloud] Bắt đầu tự động hoá tham gia {command.MeetLink} bằng {command.Browser}!");
                            await _automationService.StartMeetAsync(command.MeetLink, command.Browser);

                            // 4. Xong việc
                            await UpdateCommandStatusAsync(command.Id, "Done");
                            onLogMessage?.Invoke($"[Cloud] Xong việc! Lệnh {command.Id} đã hoàn tất.");
                        }
                    }
                });
        }
#endif
    }
}
