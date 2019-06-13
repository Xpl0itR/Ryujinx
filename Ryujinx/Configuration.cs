using JsonPrettyPrinterPlus;
using LibHac.Fs;
using OpenTK.Input;
using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.HLE;
using Ryujinx.HLE.HOS.SystemState;
using Ryujinx.HLE.HOS.Services;
using Ryujinx.HLE.Input;
using Ryujinx.UI.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Utf8Json;
using Utf8Json.Resolvers;

namespace Ryujinx
{
    public class Configuration
    {
        /// <summary>
        /// The default configuration instance
        /// </summary>
        public static Configuration Instance { get; private set; }

        /// <summary>
        /// Dumps shaders in this local directory
        /// </summary>
        public string GraphicsShadersDumpPath { get; private set; }

        /// <summary>
        /// Enables printing debug log messages
        /// </summary>
        public bool LoggingEnableDebug { get; set; }

        /// <summary>
        /// Enables printing stub log messages
        /// </summary>
        public bool LoggingEnableStub { get; set; }

        /// <summary>
        /// Enables printing info log messages
        /// </summary>
        public bool LoggingEnableInfo { get; set; }

        /// <summary>
        /// Enables printing warning log messages
        /// </summary>
        public bool LoggingEnableWarn { get; set; }

        /// <summary>
        /// Enables printing error log messages
        /// </summary>
        public bool LoggingEnableError { get; set; }

        /// <summary>
        /// Controls which log messages are written to the log targets
        /// </summary>
        public LogClass[] LoggingFilteredClasses { get; set; }

        /// <summary>
        /// Enables or disables logging to a file on disk
        /// </summary>
        public bool EnableFileLog { get; set; }

        /// <summary>
        /// Change System Language
        /// </summary>
        public SystemLanguage SystemLanguage { get; set; }

        /// <summary>
        /// Enables or disables Docked Mode
        /// </summary>
        public bool DockedMode { get; set; }

        /// <summary>
        /// Enables or disables Discord Rich Presense
        /// </summary>
        public bool EnableDiscordIntergration { get; set; }

        /// <summary>
        /// Enables or disables Vertical Sync
        /// </summary>
        public bool EnableVsync { get; set; }

        /// <summary>
        /// Enables or disables multi-core scheduling of threads
        /// </summary>
        public bool EnableMulticoreScheduling { get; set; }

        /// <summary>
        /// Enables integrity checks on Game content files
        /// </summary>
        public bool EnableFsIntegrityChecks { get; set; }

        /// <summary>
        /// Enable or Disable aggressive CPU optimizations
        /// </summary>
        public bool EnableAggressiveCpuOpts { get; set; }

        /// <summary>
        /// Enable or disable ignoring missing services
        /// </summary>
        public bool IgnoreMissingServices { get; set; }

        /// <summary>
        ///  The primary controller's type
        /// </summary>
        public HidControllerType ControllerType { get; set; }

        /// <summary>
        /// A list of directories containing games to be used to load games into the games list
        /// </summary>
        public List<string> GameDirs { get; set; }

        /// <summary>
        /// Enable or disable custom themes in the GUI
        /// </summary>
        public bool EnableCustomTheme { get; set; }

        /// <summary>
        /// Path to custom GUI theme
        /// </summary>
        public string CustomThemePath { get; set; }

        /// <summary>
        /// Enable or disable keyboard support (Independent from controllers binding)
        /// </summary>
        public bool EnableKeyboard { get; set; }

        /// <summary>
        /// Keyboard control bindings
        /// </summary>
        public NpadKeyboard KeyboardControls { get; set; }

        /// <summary>
        /// Controller control bindings
        /// </summary>
        public NpadController GamepadControls { get; private set; }

        /// <summary>
        /// Loads a configuration file from disk
        /// </summary>
        /// <param name="path">The path to the JSON configuration file</param>
        public static void Load(string path)
        {
            var resolver = CompositeResolver.Create(
                new[] { new ConfigurationEnumFormatter<Key>() },
                new[] { StandardResolver.AllowPrivateSnakeCase }
            );

            using (Stream stream = File.OpenRead(path))
            {
                Instance = JsonSerializer.Deserialize<Configuration>(stream, resolver);
            }
        }

        /// <summary>
        /// Loads a configuration file asynchronously from disk
        /// </summary>
        /// <param name="path">The path to the JSON configuration file</param>
        public static async Task LoadAsync(string path)
        {
            var resolver = CompositeResolver.Create(
                new[] { new ConfigurationEnumFormatter<Key>() },
                new[] { StandardResolver.AllowPrivateSnakeCase }
            );

            using (Stream stream = File.OpenRead(path))
            {
                Instance = await JsonSerializer.DeserializeAsync<Configuration>(stream, resolver);
            }
        }

        /// <summary>
        /// Save a configuration file to disk
        /// </summary>
        /// <param name="path">The path to the JSON configuration file</param>
        public static void SaveConfig(Configuration config, string path)
        {
            var resolver = CompositeResolver.Create(
                new[] { new ConfigurationEnumFormatter<Key>() },
                new[] { StandardResolver.AllowPrivateSnakeCase }
            );

            var data = JsonSerializer.Serialize(config, resolver);
            File.WriteAllText(path, Encoding.UTF8.GetString(data, 0, data.Length).PrettyPrintJson());
        }

        /// <summary>
        /// Configures a <see cref="Switch"/> instance
        /// </summary>
        /// <param name="device">The instance to configure</param>
        public static void InitialConfigure(Switch device)
        {
            if (Instance == null)
            {
                throw new InvalidOperationException("Configuration has not been loaded yet.");
            }

            SwitchSettings.ConfigureSettings(Instance);

            Logger.AddTarget(new AsyncLogTargetWrapper(
                new ConsoleLogTarget(),
                1000,
                AsyncLogTargetOverflowAction.Block
            ));

            if (Instance.EnableFileLog)
            {
                Logger.AddTarget(new AsyncLogTargetWrapper(
                    new FileLogTarget(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Ryujinx.log")),
                    1000,
                    AsyncLogTargetOverflowAction.Block
                ));
            }

            Configure(device, Instance);
        }

        public static void Configure(Switch device, Configuration SwitchConfig)
        {
            GraphicsConfig.ShadersDumpPath = SwitchConfig.GraphicsShadersDumpPath;

            Logger.SetEnable(LogLevel.Debug, SwitchConfig.LoggingEnableDebug);
            Logger.SetEnable(LogLevel.Stub, SwitchConfig.LoggingEnableStub);
            Logger.SetEnable(LogLevel.Info, SwitchConfig.LoggingEnableInfo);
            Logger.SetEnable(LogLevel.Warning, SwitchConfig.LoggingEnableWarn);
            Logger.SetEnable(LogLevel.Error, SwitchConfig.LoggingEnableError);

            if (SwitchConfig.LoggingFilteredClasses.Length > 0)
            {
                foreach (var logClass in EnumExtensions.GetValues<LogClass>())
                {
                    Logger.SetEnable(logClass, false);
                }

                foreach (var logClass in SwitchConfig.LoggingFilteredClasses)
                {
                    Logger.SetEnable(logClass, true);
                }
            }

            MainMenu.DiscordIntergrationEnabled = SwitchConfig.EnableDiscordIntergration;

            device.EnableDeviceVsync = SwitchConfig.EnableVsync;

            device.System.State.DockedMode = SwitchConfig.DockedMode;

            device.System.State.SetLanguage(SwitchConfig.SystemLanguage);

            if (SwitchConfig.EnableMulticoreScheduling)
            {
                device.System.EnableMultiCoreScheduling();
            }

            device.System.FsIntegrityCheckLevel = SwitchConfig.EnableFsIntegrityChecks
                ? IntegrityCheckLevel.ErrorOnInvalid
                : IntegrityCheckLevel.None;

            if (SwitchConfig.EnableAggressiveCpuOpts)
            {
                Optimizations.AssumeStrictAbiCompliance = true;
            }

            ServiceConfiguration.IgnoreMissingServices = SwitchConfig.IgnoreMissingServices;

            if (SwitchConfig.GamepadControls.Enabled)
            {
                if (GamePad.GetName(SwitchConfig.GamepadControls.Index) == "Unmapped Controller")
                {
                    SwitchConfig.GamepadControls.SetEnabled(false);
                }
            }

            device.Hid.InitilizePrimaryController(SwitchConfig.ControllerType);
            device.Hid.InitilizeKeyboard();
        }

        private class ConfigurationEnumFormatter<T> : IJsonFormatter<T>
            where T : struct
        {
            public void Serialize(ref JsonWriter writer, T value, IJsonFormatterResolver formatterResolver)
            {
                formatterResolver.GetFormatterWithVerify<string>()
                                 .Serialize(ref writer, value.ToString(), formatterResolver);
            }

            public T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
            {
                if (reader.ReadIsNull())
                {
                    return default(T);
                }

                var enumName = formatterResolver.GetFormatterWithVerify<string>()
                                                .Deserialize(ref reader, formatterResolver);

                if(Enum.TryParse<T>(enumName, out T result))
                {
                    return result;
                }

                return default(T);
            }
        }
    }
}