namespace LibApp.ApplicationTests.ArchitectureTests;

[TestFixture]
public class AuditCoverageTests
{
    [Test]
    public void EveryWriteCommand_MustAuditOrDeclareNoAudit()
    {
        var writeCommandInterfaces = new[]
        {
            typeof(ICreateOrUpdateCommand<,>),
            typeof(IDeleteCommand<,>)
        };

        var commandTypes = typeof(ICommand).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => writeCommandInterfaces.Any(wci =>
                t.GetInterfaces().Any(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == wci)))
            .ToList();

        var offenders = new List<string>();

        foreach (var type in commandTypes)
        {
            var hasNoAudit = type.GetCustomAttribute<NoAuditAttribute>() is not null;
            var inheritsAuditedBase = typeof(AuditedCommandBase).IsAssignableFrom(type);
            // Fallback для команд, которые по каким-то причинам не на базе (например,
            // LoginCommand — формально IGetQuery, но инжектит IAuditService явно).
            var injectsAudit = type.GetConstructors()
                .Any(c => c.GetParameters()
                    .Any(p => typeof(IAuditService).IsAssignableFrom(p.ParameterType)));

            if (!hasNoAudit && !inheritsAuditedBase && !injectsAudit)
                offenders.Add(type.Name);
        }

        Assert.That(offenders, Is.Empty,
            $"Write-команды без аудита: {string.Join(", ", offenders)}. " +
            "Наследуйте AuditedCommandBase, инжектируйте IAuditService " +
            "или пометьте команду [NoAudit(\"причина\")].");
    }
}