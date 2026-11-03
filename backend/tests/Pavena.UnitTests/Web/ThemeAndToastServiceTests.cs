using FluentAssertions;
using Microsoft.JSInterop;
using Moq;
using Pavena.Web.Services.Theme;
using Pavena.Web.Services.Toast;
using Xunit;

namespace Pavena.UnitTests.Web;

public class ThemeAndToastServiceTests
{
    [Fact]
    public async Task ThemeService_SetTheme_UpdatesThemeAndNotifiesListener()
    {
        var jsMock = new Mock<IJSRuntime>();
        jsMock.Setup(j => j.InvokeAsync<string>(
            "PavenaTheme.setTheme",
            It.IsAny<object[]>()
        )).ReturnsAsync("dark");

        var service = new ThemeService(jsMock.Object);
        var eventFired = false;
        service.OnThemeChanged += () => eventFired = true;

        await service.SetThemeAsync(ThemeMode.Dark);

        service.CurrentTheme.Should().Be(ThemeMode.Dark);
        service.EffectiveTheme.Should().Be("dark");
        eventFired.Should().BeTrue();
    }

    [Fact]
    public void ThemeService_OnSystemThemeChanged_WhenModeIsSystem_UpdatesEffectiveTheme()
    {
        var jsMock = new Mock<IJSRuntime>();
        var service = new ThemeService(jsMock.Object);
        var eventFired = false;
        service.OnThemeChanged += () => eventFired = true;

        service.OnSystemThemeChanged("dark");

        service.CurrentTheme.Should().Be(ThemeMode.System);
        service.EffectiveTheme.Should().Be("dark");
        eventFired.Should().BeTrue();
    }

    [Fact]
    public void ToastService_ShowAndDismiss_ManagesToastsCorrectly()
    {
        var service = new ToastService();
        var changeCount = 0;
        service.OnToastsChanged += () => changeCount++;

        service.Success("Akun berhasil diperbarui", "Sukses");
        service.Toasts.Should().HaveCount(1);
        service.Toasts[0].Type.Should().Be(ToastType.Success);
        service.Toasts[0].Title.Should().Be("Sukses");
        service.Toasts[0].Message.Should().Be("Akun berhasil diperbarui");

        service.Error("Terjadi kesalahan server");
        service.Toasts.Should().HaveCount(2);
        service.Toasts[1].Type.Should().Be(ToastType.Danger);

        var firstId = service.Toasts[0].Id;
        service.Dismiss(firstId);

        service.Toasts.Should().HaveCount(1);
        service.Toasts[0].Type.Should().Be(ToastType.Danger);
        changeCount.Should().Be(3);
    }
}
