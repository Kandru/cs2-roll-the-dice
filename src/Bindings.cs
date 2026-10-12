using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.UserMessages;
using RollTheDice.Dices;
using System.Reflection;

namespace RollTheDice
{
    public static class Bindings
    {
        private const BindingFlags InstancePublic = BindingFlags.Public | BindingFlags.Instance;

        private static readonly MethodInfo? RegisterListenerApi = FindApi("RegisterListener", 1, generic: true);
        private static readonly MethodInfo? RemoveListenerApi = FindApi("RemoveListener", 1, generic: true);
        private static readonly MethodInfo? RegisterEventApi = FindApi("RegisterEventHandler", 2, generic: true);
        private static readonly MethodInfo? DeregisterEventApi = FindApi("DeregisterEventHandler", 2, generic: true);
        private static readonly MethodInfo? HookUserMessageApi = FindApi("HookUserMessage", 3);
        private static readonly MethodInfo? UnhookUserMessageApi = FindApi("UnhookUserMessage", 3);

        public static void BindModuleListener(BasePlugin basePlugin, string listenerName, DiceBlueprint module, bool register)
        {
            Type? listenerType = typeof(Listeners).GetNestedType(listenerName);
            MethodInfo? method = module.GetType().GetMethod(listenerName);
            MethodInfo? api = register ? RegisterListenerApi : RemoveListenerApi;
            if (listenerType == null || method == null || api == null)
            {
                return;
            }

            Delegate handler = Delegate.CreateDelegate(listenerType, module, method);
            _ = api.MakeGenericMethod(listenerType).Invoke(basePlugin, [handler]);
        }

        public static void BindModuleEventHandler(BasePlugin basePlugin, string eventName, DiceBlueprint module, bool register)
        {
            Type? eventType = typeof(BasePlugin).Assembly.GetType($"CounterStrikeSharp.API.Core.{eventName}")
                ?? typeof(GameEvent).Assembly.GetType($"CounterStrikeSharp.API.Modules.Events.{eventName}");
            MethodInfo? method = module.GetType().GetMethod(eventName);
            MethodInfo? api = register ? RegisterEventApi : DeregisterEventApi;
            if (eventType == null || method == null || api == null)
            {
                return;
            }

            Type handlerType = typeof(BasePlugin).GetNestedType("GameEventHandler`1")!.MakeGenericType(eventType);
            Delegate handler = Delegate.CreateDelegate(handlerType, module, method);
            _ = api.MakeGenericMethod(eventType).Invoke(basePlugin, [handler, HookMode.Pre]);
        }

        public static void BindUserMessage(BasePlugin basePlugin, int messageId, DiceBlueprint module, HookMode hookMode, bool register)
        {
            MethodInfo? method = module.GetType().GetMethod($"HookUserMessage{messageId}");
            MethodInfo? api = register ? HookUserMessageApi : UnhookUserMessageApi;
            if (method == null || api == null)
            {
                if (register)
                {
                    Console.WriteLine($"[Bindings] Method not found for UserMessage ID: {messageId}");
                }
                return;
            }

            Delegate handler = Delegate.CreateDelegate(typeof(UserMessage.UserMessageHandler), module, method);
            _ = api.Invoke(basePlugin, [messageId, handler, hookMode]);
        }

        private static MethodInfo? FindApi(string name, int parameterCount, bool generic = false) =>
            typeof(BasePlugin).GetMethods(InstancePublic)
                .FirstOrDefault(m => m.Name == name
                    && m.IsGenericMethodDefinition == generic
                    && m.GetParameters().Length == parameterCount);
    }
}
