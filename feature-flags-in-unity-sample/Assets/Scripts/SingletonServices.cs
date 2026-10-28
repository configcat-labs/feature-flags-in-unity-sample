using System;
using System.Globalization;
using System.Threading;
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
        if (Interlocked.CompareExchange(ref instance, this, null) is not null)
        {
            // If another instance has been created already, get rid of this one.
            Destroy(this);
            return;
        }

        ConfigCatClient = ConfigCat.Client.ConfigCatClient.Get("YOUR_CONFIGCAT-SDK_KEY", options =>
        {
            options.Logger = new ConfigCatToUnityDebugLogAdapter(LogLevel.Warning);
        });

        DontDestroyOnLoad(this.gameObject);
    }

    // Start is called before the first frame update
    void Start() { }

    // Update is called once per frame
    void Update() { }

    private void OnDestroy()
    {
        this.ConfigCatClient?.Dispose();
        this.ConfigCatClient = null;
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
            get => this.logLevel;
            set { throw new NotSupportedException(); }
        }

        public void Log(LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception exception = null)
        {
            var eventIdString = eventId.Id.ToString(CultureInfo.InvariantCulture);
            var exceptionString = exception is null ? string.Empty : Environment.NewLine + exception;
            var logMessage = $"ConfigCat[{eventIdString}] {message.InvariantFormattedMessage}{exceptionString}";
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
