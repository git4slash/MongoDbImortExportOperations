using System;

namespace Sandbox.ProgressReporters {
    /// <summary>Implementation of <see cref="ISimmpleProgressReporter"/> which uses standard output stream to report progress</summary>
    public class StandardOutputStreamProgressReporter : ISimmpleProgressReporter {

        public static StandardOutputStreamProgressReporter Create() => new StandardOutputStreamProgressReporter();

        public void SetMax(int val) => Console.WriteLine($"max was set to: {val}");

        public void Report(string message) => Console.WriteLine($"Report message: {message}");

        public void Start(string message) => Console.WriteLine($"Start message: {message}");

        public void End(string message) => Console.WriteLine($"End message: {message}");
    }

}
