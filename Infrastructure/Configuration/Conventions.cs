using NServiceBus;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Configuration
{
    public class NSBConventions
    {

        public static void ConfigureConventions(ConventionsBuilder conventions)
        {
            conventions.DefiningCommandsAs(
                type =>
                {
                    return type.Namespace != null &&
                            type.Namespace.EndsWith("Commands");
                });
            conventions.DefiningEventsAs(
                type =>
                {
                    return type.Namespace != null &&
                        type.Namespace.EndsWith("Events");
                });
            conventions.DefiningMessagesAs(
                type =>
                {
                    return type.Namespace != null &&
                        type.Namespace.EndsWith("Messages");
                });
            conventions.DefiningDataBusPropertiesAs(
                property =>
                {
                    return property.Name.EndsWith("DataBus");
                });
            conventions.DefiningExpressMessagesAs(
                type =>
                {
                    return type.Name.EndsWith("Express");
                });
            conventions.DefiningTimeToBeReceivedAs(
                type =>
                {
                    if (type.Name.EndsWith("Expires"))
                    {
                        return TimeSpan.FromSeconds(30);
                    }
                    return TimeSpan.MaxValue;
                });
        }
    }
}
