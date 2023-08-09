using Akka.Actor;

namespace ApartmentsAndManagement.Api.Actors;

public static class ParentActor
{
    public static IActorRef ElasticsearchPersistenceActor = ActorRefs.Nobody;
    public static ActorSystem ActorSystem;

    public static SupervisorStrategy GetDefaultStrategy()
    {
        return new OneForOneStrategy(
            3,
            TimeSpan.FromSeconds(3),
            exception =>
            {
                if (exception is not ActorInitializationException) return Directive.Resume;

                ActorSystem.Terminate().Wait(1000);
                return Directive.Stop;
            });
    }
}