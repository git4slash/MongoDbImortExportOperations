namespace Sandbox.ProgressReporters {
    internal interface ISimmpleProgressReporter : System.IProgress<string> {
        new void Report(string message);
        void SetMax(int val);
        void Start(string message);
        void End(string message);
    }
}