using System;
using System.Globalization;
using ConfigCat.Client;
using UnityEngine;

public class SingletonServices : MonoBehaviour
{
    private static SingletonServices instance;

    public static SingletonServices Instance => instance;

    [field: NonSerialized]
    public IConfigCatClient ConfigCatClient { get; private set; }

    private void Awake()
    {
        // Ensure only one instance exists.
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        // Keep the service available across scene changes.
        DontDestroyOnLoad(gameObject);

        // Initialize the ConfigCat client once.
        ConfigCatClient = ConfigCat.Client.ConfigCatClient.Get(
            "YOUR_CONFIGCAT_SDK_KEY",
            options =>
            {
                options.Logger =
                    new ConfigCatToUnityDebugLogAdapter(LogLevel.Warning);
            }
        );
    }

    private void OnDestroy()
    {
        if (instance != this)
        {
            return;
        }

        ConfigCatClient?.Dispose();
        ConfigCatClient = null;
        instance = null;
    }

    private sealed class ConfigCatToUnityDebugLogAdapter : IConfigCatLogger
    {
        private readonly LogLevel logLevel;

        public ConfigCatToUnityDebugLogAdapter(LogLevel logLevel)
        {
            this.logLevel = logLevel;
        }

        public LogLevel LogLevel
        {
            get => logLevel;
            set => throw new NotSupportedException();
        }

        public void Log(
            LogLevel level,
            LogEventId eventId,
            ref FormattableLogMessage message,
            Exception exception = null)
        {
            var eventIdString =
                eventId.Id.ToString(CultureInfo.InvariantCulture);

            var exceptionString = exception == null
                ? string.Empty
                : Environment.NewLine + exception;

            var logMessage =
                $"ConfigCat[{eventIdString}] " +
                $"{message.InvariantFormattedMessage}{exceptionString}";

            switch (level)
            {
                case LogLevel.Error:
                    Debug.LogError(logMessage);
                    break;

                case LogLevel.Warning:
                    Debug.LogWarning(logMessage);
                    break;

                case LogLevel.Info:
                    Debug.Log(logMessage);
                    break;

                case LogLevel.Debug:
                    Debug.Log(logMessage);
                    break;
            }
        }
    }
}
