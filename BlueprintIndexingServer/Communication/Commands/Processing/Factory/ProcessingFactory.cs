using System;
using System.Collections.Generic;

namespace Owlcat.Blueprints.Server.Communication.Commands.Processing.Factory;

public class ProcessingFactory : IProcessingFactory
{
    public static IProcessingFactory Create() => new ProcessingFactory();

    private readonly Dictionary<CommandTypes, Func<Command, FileDatabase.FileDatabase, Command?>> m_processings;

    private ProcessingFactory()
    {
        m_processings = new Dictionary<CommandTypes, Func<Command, FileDatabase.FileDatabase, Command?>>();
    }
    
    IProcessingFactory IProcessingFactory.RegistrationProcessCommand(CommandTypes type, Func<Command, FileDatabase.FileDatabase, Command?> process)
    {
        if (m_processings.ContainsKey(type))
        {
            throw new Exception($"Duplicate command processor for type {type} registration!");
        }
        
        m_processings.Add(type, process);
        return this;
    }

    bool IProcessingFactory.TryProcessCommand(Command @in, FileDatabase.FileDatabase database, out Command? @out)
    {
        @out = null;
        if (m_processings.TryGetValue(@in.Type, out var processing))
        {
            @out = processing.Invoke(@in, database);
        }
        
        return @out != null;
    }
}