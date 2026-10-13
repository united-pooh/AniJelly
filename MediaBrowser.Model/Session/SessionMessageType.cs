#pragma warning disable CS1591

namespace MediaBrowser.Model.Session
{
    /// <summary>
    /// The different kinds of messages that are used in the WebSocket api.
    /// </summary>
    public enum SessionMessageType
    {
        // Server -> Client
        ForceKeepAlive,
        GeneralCommand,
        UserDataChanged,
        Sessions,
        Play,
        SyncPlayCommand,
        SyncPlayGroupUpdate,
        Playstate,
        RestartRequired,
        ServerShuttingDown,
        ServerRestarting,
        LibraryChanged,
        UserDeleted,
        UserUpdated,
        // Values 14-17 belonged to retired DVR notifications.
        RefreshProgress = 18,
        ScheduledTaskEnded,
        PackageInstallationCancelled,
        PackageInstallationFailed,
        PackageInstallationCompleted,
        PackageInstalling,
        PackageUninstalled,
        ActivityLogEntry,
        ScheduledTasksInfo,

        // Client -> Server
        ActivityLogEntryStart,
        ActivityLogEntryStop,
        SessionsStart,
        SessionsStop,
        ScheduledTasksInfoStart,
        ScheduledTasksInfoStop,

        // Shared
        KeepAlive,
    }
}
