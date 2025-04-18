using System.Diagnostics;

namespace Owlcat.Blueprints.Server.Communication
{
    public class Command
    {
        public CommandTypes Type { get; set; }
        public string Payload { get; set; }
        public Stopwatch Stopwatch { get; set; }
    }
}