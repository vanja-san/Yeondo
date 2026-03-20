using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using Yeondo.Services;

namespace Yeondo
{
    public partial class App : Application
    {
        private static Mutex? _mutex;
        private const string MutexName = "Yeondo-SymLink-Creator-SingleInstance";
        private static bool _isPrimaryInstance;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Проверка на запуск второго экземпляра
            bool createdNew;
            _mutex = new Mutex(true, MutexName, out createdNew);

            if (!createdNew)
            {
                // Активируем существующее окно
                var process = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
                foreach (var p in process)
                {
                    if (p.MainWindowHandle != IntPtr.Zero)
                    {
                        ShowWindow(p.MainWindowHandle, SW_RESTORE);
                        SetForegroundWindow(p.MainWindowHandle);
                        break;
                    }
                }

                // Завершаем второй экземпляр (не освобождаем мьютекс, т.к. не захватывали его)
                _isPrimaryInstance = false;
                _mutex?.Dispose();
                _mutex = null;
                Shutdown();
                return;
            }

            _isPrimaryInstance = true;
            // Инициализация локализации
            LocalizationService.Instance.Initialize();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_isPrimaryInstance && _mutex != null)
            {
                try
                {
                    _mutex.ReleaseMutex();
                    _mutex.Dispose();
                }
                catch (ApplicationException)
                {
                    // Игнорируем, если мьютекс уже освобождён
                }
            }
            _mutex = null;
            base.OnExit(e);
        }
    }
}
