using System;
using System.Globalization;
using System.Threading.Tasks;
using ConfigCat.Client;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.SceneManagement;

public class SingletonServices : MonoBehaviour
{
    private static SingletonServices instance;

    public static SingletonServices Instance => instance;

    [field: NonSerialized]
    public IConfigCatClient ConfigCatClient { get; private set; }

    [NonSerialized]
    private volatile IConfigCatClientSnapshot latestConfigCatSnapshot;

    public IConfigCatClientSnapshot LatestConfigCatClientSnapshot => latestConfigCatSnapshot;

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

#if DEBUG
        var sdkKey = "YOUR-CONFIGCAT-SDK-KEY-FOR-DEVELOPMENT";
        var pollInterval = TimeSpan.FromSeconds(2);
#else
        var sdkKey = "YOUR-CONFIGCAT-SDK-KEY-FOR-PRODUCTION";
        var pollInterval = TimeSpan.FromSeconds(60);
#endif

        // Initialize the ConfigCat client with Auto Polling.
        try
        {
            ConfigCatClient = ConfigCat.Client.ConfigCatClient.Get(
                sdkKey,
                options =>
                {
                    options.PollingMode = PollingModes.AutoPoll(pollInterval);

                    options.Logger =
                        new ConfigCatToUnityDebugLogAdapter(LogLevel.Warning);

                    // Update the snapshot when the feature flag data changes using the ConfigChanged hook
                    // (https://configcat.com/docs/sdk-reference/dotnet/#hooks).
                    options.ConfigChanged += (s, _) =>
                        latestConfigCatSnapshot = ((IConfigCatClient)s).Snapshot();
                }
            );
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{nameof(SingletonServices)}] Failed to create ConfigCat client. {ex}");
#if !DEBUG
            Utils.ForceCrash(ForcedCrashCategory.FatalError);
#endif
        }
    }

    async void Start()
    {
        try
        {
            float start = Time.time;

            // Wait until the client is ready before creating the first snapshot.
            Debug.Log($"[{nameof(SingletonServices)}] Loading feature flags...");

            await ConfigCatClient.WaitForReadyAsync();

            latestConfigCatSnapshot ??= ConfigCatClient.Snapshot();

            Debug.Log($"[{nameof(SingletonServices)}] Feature flags loaded.");

            AsyncOperation op = SceneManager.LoadSceneAsync("GetStarted_Scene");
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
            {
                await Task.Yield();
            }

            // Ensure the loading screen is visible for at least minimumDisplayTime
            const float minimumDisplayTime = 2f;
            float elapsed = Time.time - start;
            if (elapsed < minimumDisplayTime)
                await Task.Delay((int)((minimumDisplayTime - elapsed) * 1000));

            op.allowSceneActivation = true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{nameof(SingletonServices)}] Start up failed. {ex}");
#if !DEBUG
            Utils.ForceCrash(ForcedCrashCategory.FatalError);
#endif

        }
    }

    private void OnDestroy()
    {
        if (instance != this)
        {
            return;
        }

        ConfigCatClient?.Dispose();
        ConfigCatClient = null;
        latestConfigCatSnapshot = null;
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
