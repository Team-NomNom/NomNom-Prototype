namespace SingularityGroup.HotReload {
    internal interface IServerHealthCheck {
        bool IsServerHealthy { get; }
        void CheckHealth();
    }
}