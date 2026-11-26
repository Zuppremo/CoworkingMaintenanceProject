using System.Collections.Concurrent;
using System.Windows.Input;

// Minimal MAUI platform shims so CoworkingMobile ViewModels and Services can be unit-tested on net10.0.
namespace CoworkingMobile.Tests.Shims
{
    public static class MauiTestHost
    {
        public static ConcurrentDictionary<string, string> Storage { get; } = new();
        public static ConcurrentQueue<string> NavigatedRoutes { get; } = new();
        public static ConcurrentQueue<(string Title, string Message)> Alerts { get; } = new();
        public static string PlatformName { get; set; } = "Linux";

        public static void Reset()
        {
            Storage.Clear();
            NavigatedRoutes.Clear();
            Alerts.Clear();
            PlatformName = "Linux";
        }
    }
}

namespace Microsoft.Maui.ApplicationModel
{
    public static class MainThread
    {
        public static void BeginInvokeOnMainThread(Action action) => action();
    }
}

namespace Microsoft.Maui.Devices
{
    public enum DevicePlatform
    {
        Android,
        iOS,
        WinUI,
        Unknown
    }

    public static class DeviceInfo
    {
        public static DevicePlatform Platform =>
            CoworkingMobile.Tests.Shims.MauiTestHost.PlatformName == "Android"
                ? DevicePlatform.Android
                : DevicePlatform.Unknown;
    }
}

namespace Microsoft.Maui.Storage
{
    public interface ISecureStorage
    {
        Task SetAsync(string key, string value);
        Task<string?> GetAsync(string key);
        bool Remove(string key);
    }

    public static class SecureStorage
    {
        public static ISecureStorage Default { get; } = new InMemorySecureStorage();

        private sealed class InMemorySecureStorage : ISecureStorage
        {
            public Task SetAsync(string key, string value)
            {
                CoworkingMobile.Tests.Shims.MauiTestHost.Storage[key] = value;
                return Task.CompletedTask;
            }

            public Task<string?> GetAsync(string key)
            {
                CoworkingMobile.Tests.Shims.MauiTestHost.Storage.TryGetValue(key, out var val);
                return Task.FromResult<string?>(val);
            }

            public bool Remove(string key) =>
                CoworkingMobile.Tests.Shims.MauiTestHost.Storage.TryRemove(key, out _);
        }
    }
}

namespace Microsoft.Maui.Controls
{
    public class Command : ICommand
    {
        private readonly Action execute;
        public Command(Action execute) => this.execute = execute;
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute();
    }

    public class Shell
    {
        public static Shell Current { get; } = new();

        public Task GoToAsync(string route)
        {
            CoworkingMobile.Tests.Shims.MauiTestHost.NavigatedRoutes.Enqueue(route);
            return Task.CompletedTask;
        }

        public Task DisplayAlert(string title, string message, string cancel)
        {
            CoworkingMobile.Tests.Shims.MauiTestHost.Alerts.Enqueue((title, message));
            return Task.CompletedTask;
        }
    }
}
