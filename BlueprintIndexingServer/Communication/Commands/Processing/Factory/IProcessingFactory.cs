using System;

namespace Owlcat.Blueprints.Server.Communication.Commands.Processing.Factory;

public interface IProcessingFactory
{
    IProcessingFactory RegistrationProcessCommand(CommandTypes type, Func<Command, FileDatabase.FileDatabase, Command?> process);
    bool TryProcessCommand(Command @in, FileDatabase.FileDatabase database, out Command? @out);
}