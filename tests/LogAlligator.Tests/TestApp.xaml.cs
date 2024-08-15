﻿using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml;

[assembly: AvaloniaTestApplication(typeof(LogAlligator.Tests.TestAppBuilder))]

namespace LogAlligator.Tests
{
    public class TestApp : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }

    public class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
    }
}
