
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;
using RollTheDice.Dices;
using System.Reflection;

namespace RollTheDice.Utils
{
    public static class DynamicHandlers
    {
        public static void RegisterModuleListener(BasePlugin basePlugin, string listenerName, DiceBlueprint plugin)
        {
            // get the listener type from CounterStrikeSharp.API.Core.Listeners
            Type? listenerType = typeof(Listeners).GetNestedType(listenerName);
            if (listenerType == null)
            {
                return;
            }
            // get the method from the module
            MethodInfo? method = plugin.GetType().GetMethod(listenerName);
            if (method == null)
            {
                return;
            }
            // create delegate
            Delegate handler = Delegate.CreateDelegate(listenerType, plugin, method);
            // use reflection to call RegisterListener<T>
            MethodInfo? registerMethod = typeof(BasePlugin).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(static m => m.Name == "RegisterListener" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);
            if (registerMethod == null)
            {
                return;
            }
            MethodInfo genericRegisterMethod = registerMethod.MakeGenericMethod(listenerType);
            _ = genericRegisterMethod.Invoke(basePlugin, [handler]);
        }

        public static void DeregisterModuleListener(BasePlugin basePlugin, string listenerName, DiceBlueprint plugin)
        {
            // get the listener type from CounterStrikeSharp.API.Core.Listeners
            Type? listenerType = typeof(Listeners).GetNestedType(listenerName);
            if (listenerType == null)
            {
                return;
            }
            // get the method from the module
            MethodInfo? method = plugin.GetType().GetMethod(listenerName);
            if (method == null)
            {
                return;
            }
            // create delegate
            Delegate handler = Delegate.CreateDelegate(listenerType, plugin, method);
            // use reflection to call RemoveListener<T>
            MethodInfo? removeMethod = typeof(BasePlugin).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(static m => m.Name == "RemoveListener" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);
            if (removeMethod == null)
            {
                return;
            }
            MethodInfo genericRemoveMethod = removeMethod.MakeGenericMethod(listenerType);
            _ = genericRemoveMethod.Invoke(basePlugin, [handler]);
        }

        public static void RegisterModuleEventHandler(BasePlugin basePlugin, string eventName, DiceBlueprint plugin)
        {
            // get the event type from CounterStrikeSharp.API.Core
            Type? eventType = typeof(BasePlugin).Assembly.GetType($"CounterStrikeSharp.API.Core.{eventName}");
            if (eventType == null)
            {
                return;
            }

            // get the method from the module
            MethodInfo? method = plugin.GetType().GetMethod(eventName);
            if (method == null)
            {
                return;
            }

            // create delegate using Func<T, GameEventInfo, HookResult> for event handlers
            Type gameEventHandlerType = typeof(BasePlugin).GetNestedType("GameEventHandler`1")!.MakeGenericType(eventType);
            Delegate handler = Delegate.CreateDelegate(gameEventHandlerType, plugin, method);

            // use reflection to call RegisterEventHandler<T>
            MethodInfo? registerMethod = typeof(BasePlugin).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(static m => m.Name == "RegisterEventHandler" && m.IsGenericMethodDefinition && m.GetParameters().Length == 2);
            if (registerMethod == null)
            {
                return;
            }
            MethodInfo genericRegisterMethod = registerMethod.MakeGenericMethod(eventType);
            _ = genericRegisterMethod.Invoke(basePlugin, [handler, HookMode.Pre]);
        }

        public static void DeregisterModuleEventHandler(BasePlugin basePlugin, string eventName, DiceBlueprint module)
        {
            // get the event type from CounterStrikeSharp.API.Core
            Type? eventType = typeof(BasePlugin).Assembly.GetType($"CounterStrikeSharp.API.Core.{eventName}");
            if (eventType == null)
            {
                return;
            }

            // get the method from the module
            MethodInfo? method = module.GetType().GetMethod(eventName);
            if (method == null)
            {
                return;
            }

            // create delegate using BasePlugin.GameEventHandler<T>
            Type gameEventHandlerType = typeof(BasePlugin).GetNestedType("GameEventHandler`1")!.MakeGenericType(eventType);
            Delegate handler = Delegate.CreateDelegate(gameEventHandlerType, module, method);

            // use reflection to call DeregisterEventHandler<T>
            MethodInfo? deregisterMethod = typeof(BasePlugin).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(static m => m.Name == "DeregisterEventHandler" && m.IsGenericMethodDefinition && m.GetParameters().Length == 2);
            if (deregisterMethod == null)
            {
                return;
            }
            MethodInfo genericDeregisterMethod = deregisterMethod.MakeGenericMethod(eventType);
            _ = genericDeregisterMethod.Invoke(basePlugin, [handler, HookMode.Pre]);
        }

        public static void RegisterUserMessageHook(BasePlugin basePlugin, int messageId, DiceBlueprint plugin, HookMode hookMode)
        {
            // get the method from the module
            MethodInfo? method = plugin.GetType().GetMethod($"HookUserMessage{messageId}");
            if (method == null)
            {
                Console.WriteLine("[DynamicHandlers] Method not found for UserMessage ID: " + messageId);
                return;
            }

            // create delegate using UserMessage.UserMessageHandler - not Func<UserMessage, HookResult>
            Type delegateType = typeof(UserMessage.UserMessageHandler);
            Delegate handler = Delegate.CreateDelegate(delegateType, plugin, method);

            // use reflection to call HookUserMessage with correct signature
            MethodInfo? hookMethod = typeof(BasePlugin).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(static m => m.Name == "HookUserMessage" && m.GetParameters().Length == 3);
            if (hookMethod == null)
            {
                Console.WriteLine("[DynamicHandlers] HookUserMessage method not found.");
                return;
            }
            _ = hookMethod.Invoke(basePlugin, [messageId, handler, hookMode]);
        }

        public static void DeregisterUserMessageHook(BasePlugin basePlugin, int messageId, DiceBlueprint plugin, HookMode hookMode)
        {
            // get the method from the module
            MethodInfo? method = plugin.GetType().GetMethod($"HookUserMessage{messageId}");
            if (method == null)
            {
                return;
            }

            // create delegate using UserMessage.UserMessageHandler - not Func<UserMessage, HookResult>
            Type delegateType = typeof(UserMessage.UserMessageHandler);
            Delegate handler = Delegate.CreateDelegate(delegateType, plugin, method);

            // use reflection to call UnhookUserMessage with correct signature
            MethodInfo? unhookMethod = typeof(BasePlugin).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(static m => m.Name == "UnhookUserMessage" && m.GetParameters().Length == 3);
            if (unhookMethod == null)
            {
                return;
            }
            _ = unhookMethod.Invoke(basePlugin, [messageId, handler, hookMode]);
        }

        public static void RegisterCommand(BasePlugin basePlugin, string command, string description, DiceBlueprint plugin)
        {
            // get the method from the module
            MethodInfo? method = plugin.GetType().GetMethod($"Command{command.First().ToString().ToUpper(System.Globalization.CultureInfo.CurrentCulture) + command[1..]}");
            if (method == null)
            {
                Console.WriteLine("[DynamicHandlers] Method not found for command name: " + command);
                return;
            }

            // use reflection to call AddCommand with correct signature
            MethodInfo? addCommandMethod = typeof(BasePlugin).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(static m => m.Name == "AddCommand" && m.GetParameters().Length == 3);
            if (addCommandMethod == null)
            {
                Console.WriteLine("[DynamicHandlers] AddCommand method not found.");
                return;
            }

            // get the delegate type from the AddCommand parameters (usually CommandDelegate)
            Type delegateType = addCommandMethod.GetParameters()[2].ParameterType;
            Delegate handler = Delegate.CreateDelegate(delegateType, plugin, method);

            _ = addCommandMethod.Invoke(basePlugin, [command, description, handler]);
        }

        public static void DeregisterCommand(BasePlugin basePlugin, string command, DiceBlueprint plugin)
        {
            // get the method from the module
            MethodInfo? method = plugin.GetType().GetMethod($"Command{command.First().ToString().ToUpper(System.Globalization.CultureInfo.CurrentCulture) + command[1..]}");
            if (method == null)
            {
                return;
            }

            // find RemoveCommand method to get the correct delegate type
            MethodInfo? removeCommandMethod = typeof(BasePlugin).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(static m => m.Name == "RemoveCommand" && m.GetParameters().Length == 3);
            if (removeCommandMethod == null)
            {
                return;
            }

            Type delegateType = removeCommandMethod.GetParameters()[2].ParameterType;
            Delegate handler = Delegate.CreateDelegate(delegateType, plugin, method);

            _ = removeCommandMethod.Invoke(basePlugin, [command, handler]);
        }
    }
}