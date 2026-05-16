using AutoAccessLinkA.Models;
using AutoAccessLinkA.Services;

namespace AutoAccessLinkA
{
    public partial class MainPage : ContentPage
    {
        private readonly FirebaseSyncService _firebaseSyncService;

        public MainPage()
        {
            InitializeComponent();
            _firebaseSyncService = new FirebaseSyncService();
            SetupUI();
        }

        private void SetupUI()
        {
            // 1. Nạp danh sách trình duyệt vào Picker
            pickerBrowser.ItemsSource = Enum.GetNames(typeof(BrowserType));
            pickerBrowser.SelectedIndex = 0; // Chọn mặc định cái đầu tiên (Brave)

            // 2. Set giờ mặc định cho TimePicker là giờ hiện tại + 5 phút
            timePicker.Time = DateTime.Now.TimeOfDay.Add(TimeSpan.FromMinutes(5));

            LogMessage("Hệ thống khởi động thành công. Sẵn sàng nhận lệnh!");

#if WINDOWS
            // Nếu là Windows (đóng vai trò Executor), bật lắng nghe Firebase
            LogMessage("Bật chế độ lắng nghe lệnh từ Firebase...");
            _firebaseSyncService.ListenForCommands(msg => 
            {
                Dispatcher.Dispatch(() => LogMessage(msg));
            });
#endif
        }

        private void LogMessage(string message)
        {
            // Thêm log mới lên đầu, hiển thị thời gian
            string timeStamp = DateTime.Now.ToString("HH:mm:ss");
            lblLog.Text = $"[{timeStamp}] {message}\n" + lblLog.Text;
        }

        private async void OnStartClicked(object sender, EventArgs e)
        {
            string link = txtLink.Text?.Trim();

            // 1. Kiểm tra đầu vào
            if (string.IsNullOrEmpty(link) || !link.StartsWith("http"))
            {
                await DisplayAlert("Lỗi", "Vui lòng nhập Link Google Meet hợp lệ!", "OK");
                return;
            }

            // Lấy giờ và trình duyệt đã chọn
            TimeSpan selectedTime = (TimeSpan)timePicker.Time;
            string selectedBrowser = pickerBrowser.SelectedItem?.ToString() ?? "Brave";

            // 2. Khóa nút để tránh bấm nhiều lần
            btnStart.IsEnabled = false;
            btnStart.Text = "⏳ ĐANG XỬ LÝ...";

            // Ghi Log
            LogMessage($"Nhận lệnh: Vào {link}");
            LogMessage($"Chỉ định lúc: {selectedTime:hh\\:mm} bằng {selectedBrowser}");

            try
            {
                // Đồng bộ lệnh lên Firebase
                LogMessage("Đang đồng bộ lệnh lên hệ thống Cloud...");
                
                DateTime scheduledDateTime = DateTime.Today.Add(selectedTime);
                if (scheduledDateTime < DateTime.Now)
                {
                    // Nếu thời gian chọn nhỏ hơn thời gian hiện tại, có thể người dùng muốn hẹn cho ngày mai
                    scheduledDateTime = scheduledDateTime.AddDays(1);
                }

                var command = new MeetCommand
                {
                    MeetLink = link,
                    ScheduledTime = scheduledDateTime,
                    Browser = (BrowserType)Enum.Parse(typeof(BrowserType), selectedBrowser)
                };

                await _firebaseSyncService.PushCommandAsync(command);

                LogMessage("✅ Thiết lập và đồng bộ thành công! Lệnh đang chờ xử lý.");
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Lỗi đồng bộ: {ex.Message}");
            }
            finally
            {
                // 3. Mở khóa nút
                btnStart.IsEnabled = true;
                btnStart.Text = "🚀 THIẾT LẬP HẸN GIỜ";
            }
        }
    }
}
