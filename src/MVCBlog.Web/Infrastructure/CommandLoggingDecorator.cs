﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MVCBlog.Business;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MVCBlog.Web.Infrastructure
{
    public class CommandLoggingDecorator<TCommand> : ICommandHandler<TCommand>
    {
        private readonly ICommandHandler<TCommand> handler;

        private readonly ILogger<CommandLoggingDecorator<TCommand>> logger;

        private readonly IHttpContextAccessor httpContextAccessor;

        public CommandLoggingDecorator(
            ICommandHandler<TCommand> handler,
            ILogger<CommandLoggingDecorator<TCommand>> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            this.handler = handler;
            this.logger = logger;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task HandleAsync(TCommand command)
        {
            this.logger.LogInformation(
                "Executing command '{0}' (User: '{1}', Data: '{2}')",
                command.GetType().Name,
                this.httpContextAccessor.HttpContext.User?.Identity?.Name,
                JsonConvert.SerializeObject(command, new ObscureJsonConverter()));

            await this.handler.HandleAsync(command);
        }

        private class ObscureJsonConverter : JsonConverter
        {
            private static readonly string[] PropertiesToObscure = new string[] { "Password" };

            public override bool CanRead
            {
                get { return false; }
            }

            public override bool CanConvert(Type objectType)
            {
                return true;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var t = JToken.FromObject(value);

                if (t.Type == JTokenType.Object)
                {
                    var o = (JObject)t;

                    foreach (var property in o.Properties())
                    {
                        if (PropertiesToObscure.Any(p => p == property.Name))
                        {
                            property.Value = "---";
                        }
                    }

                    o.WriteTo(writer);
                }
                else
                {
                    t.WriteTo(writer);
                }
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
    }
}
