using AutoAccessLinkA.Models;
using AutoAccessLinkA.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace AutoAccessLinkA
{
    public partial class MainPage : ContentPage
    {
        private readonly FirebaseSyncService _firebaseSyncService;

        public ObservableCollection<SavedLink> SavedLinks { get; set; } = new ObservableCollection<SavedLink>();
        public ICommand DeleteLinkCommand { get; private set; }

        public MainPage()
        {
            InitializeComponent();
            _firebaseSyncService = new FirebaseSyncService();
            
            DeleteLinkCommand = new Command<SavedLink>(async (link) =>
            {
                if (link != null)
                {
                    bool confirm = await DisplayAlert("Xác nhận", $"Bạn có chắc muốn xoá link '{link.Title}'?", "Xoá", "Hủy");
                    if (confirm)
                    {
                        await _firebaseSyncService.DeleteLinkAsync(link.Id);
                    }
                }
            });

            BindingContext = this;
            cvSavedLinks.ItemsSource = SavedLinks;

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

            // Lắng nghe danh sách Link đã lưu
            _firebaseSyncService.GetSavedLinksAsObservable().Subscribe(d =>
            {
                Dispatcher.Dispatch(() =>
                {
                    if (d.EventType == Firebase.Database.Streaming.FirebaseEventType.Delete)
                    {
                        var item = SavedLinks.FirstOrDefault(x => x.Id == d.Key);
                        if (item != null) SavedLinks.Remove(item);
                    }
                    else if (d.EventType == Firebase.Database.Streaming.FirebaseEventType.InsertOrUpdate)
                    {
                        var item = SavedLinks.FirstOrDefault(x => x.Id == d.Object.Id);
                        if (item != null)
                        {
                            item.Title = d.Object.Title;
                            item.Url = d.Object.Url;
                            item.Browser = d.Object.Browser;
                        }
                        else
                        {
                            SavedLinks.Add(d.Object);
                        }
                    }
                });
            });

            // Lắng nghe trạng thái PC (Heartbeat)
            _firebaseSyncService.GetPcStateAsObservable().Subscribe(d =>
            {
                Dispatcher.Dispatch(() =>
                {
                    if (d.Object == "Online")
                    {
                        pcStateDot.Fill = Colors.LimeGreen;
                        lblPcState.Text = "💻 PC đang hoạt động";
                    }
                });
            });

            // Nếu PC offline quá 35s, tự động báo đỏ
            Dispatcher.StartTimer(TimeSpan.FromSeconds(35), () =>
            {
                // Chỉ đổi màu đỏ nếu cần (Thực tế nên lưu timestamp báo cáo cuối cùng để check)
                // Trong phiên bản đơn giản này, ta reset trạng thái nếu không nhận được heartbeat
                pcStateDot.Fill = Colors.Red;
                lblPcState.Text = "PC đang tắt";
                return true;
            });

#if WINDOWS
            // Bật báo cáo Heartbeat
            _ = _firebaseSyncService.StartPcHeartbeat();

            // Nếu là Windows (đóng vai trò Executor), bật lắng nghe Firebase
            LogMessage("Bật chế độ lắng nghe lệnh từ Firebase...");
            _firebaseSyncService.ListenForCommands(msg => 
            {
                Dispatcher.Dispatch(() => LogMessage(msg));
            });
#endif
        }

        private void OnSavedLinkTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is SavedLink link)
            {
                txtLink.Text = link.Url;
                pickerBrowser.SelectedItem = link.Browser.ToString();
                LogMessage($"Đã nạp nhanh link: {link.Title}");
            }
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

                // Tự động lưu link nếu chưa có
                if (!SavedLinks.Any(x => x.Url == link))
                {
                    string title = link.Length > 25 ? "Meet " + link.Substring(link.Length - 10) : link;
                    await _firebaseSyncService.SaveLinkAsync(new SavedLink
                    {
                        Title = title,
                        Url = link,
                        Browser = command.Browser
                    });
                }

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
