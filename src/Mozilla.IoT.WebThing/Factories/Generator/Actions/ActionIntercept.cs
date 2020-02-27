using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Mozilla.IoT.WebThing.Actions;
using Mozilla.IoT.WebThing.Attributes;
using Mozilla.IoT.WebThing.Extensions;
using Mozilla.IoT.WebThing.Factories.Generator.Intercepts;

namespace Mozilla.IoT.WebThing.Factories.Generator.Actions
{
    public class ActionIntercept : IActionIntercept
    {
        private static readonly MethodInfo s_getLength = typeof(string).GetProperty(nameof(string.Length)).GetMethod;
        private const MethodAttributes s_getSetAttributes =
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

        private readonly ModuleBuilder _moduleBuilder;
        private readonly ThingOption _option;
        public  Dictionary<string, ActionContext> Actions { get; }

        public ActionIntercept(ModuleBuilder moduleBuilder, ThingOption option)
        {
            _option = option;
            _moduleBuilder = moduleBuilder;
            Actions = option.IgnoreCase ? new Dictionary<string, ActionContext>(StringComparer.InvariantCultureIgnoreCase) 
                : new Dictionary<string, ActionContext>();
        }

        public void Before(Thing thing)
        {
        }

        public void After(Thing thing)
        {
        }

        public void Intercept(Thing thing, MethodInfo action, ThingActionAttribute? actionInfo)
        {
            var thingType = thing.GetType();
            var inputBuilder = _moduleBuilder.DefineType($"{action.Name}Input",
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.AutoClass);
            
            var parameters = action.GetParameters();
            foreach (var parameter in parameters)
            {
                if (parameter.GetCustomAttribute<FromServicesAttribute>() == null
                    && parameter.ParameterType != typeof(CancellationToken))
                {
                    CreateProperty(inputBuilder, parameter.Name!, parameter.ParameterType);   
                }
            }
            
            var inputType = inputBuilder.CreateType()!;
            
            var actionBuilder = _moduleBuilder.DefineType($"{thingType.Name}{action.Name}ActionInfo",
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.AutoClass,
                typeof(ActionInfo));
            var input = CreateProperty(actionBuilder, "input", inputType);
            var name = actionInfo?.Name ?? action.Name;
            CreateActionName(actionBuilder, name);
            
            var isValid = actionBuilder.DefineMethod("IsValid",
                MethodAttributes.Private | MethodAttributes.Static, typeof(bool),
                parameters
                    .Where(x => x.GetCustomAttribute<FromServicesAttribute>() == null 
                                && x.ParameterType != typeof(CancellationToken))
                    .Select(x => x.ParameterType)
                    .ToArray());
            var isValidIl = isValid.GetILGenerator();
            
            CreateParameterValidation(isValidIl, parameters);
            CreateInputValidation(actionBuilder, inputBuilder, isValid, input);
            CreateExecuteAsync(actionBuilder, inputBuilder,input, action, thingType);
            
            Actions.Add(_option.PropertyNamingPolicy.ConvertName(name), new ActionContext(actionBuilder.CreateType()!));
        }
        
        private static PropertyBuilder CreateProperty(TypeBuilder builder, string fieldName, Type type)
        {
            var fieldBuilder = builder.DefineField($"_{fieldName}", type, FieldAttributes.Private);
            var parameterName = fieldName.FirstCharToUpper();
            var propertyBuilder = builder.DefineProperty(parameterName, 
                PropertyAttributes.HasDefault,
                type, null);

            var getProperty = builder.DefineMethod($"get_{parameterName}", s_getSetAttributes,
                type, Type.EmptyTypes);

            var getPropertyIL = getProperty.GetILGenerator();
            getPropertyIL.Emit(OpCodes.Ldarg_0);
            getPropertyIL.Emit(OpCodes.Ldfld, fieldBuilder);
            getPropertyIL.Emit(OpCodes.Ret);

            // Define the "set" accessor method for CustomerName.
            var setProperty = builder.DefineMethod($"set_{parameterName}", s_getSetAttributes,
                null, new[] {type});

            var setPropertyIL = setProperty.GetILGenerator();

            setPropertyIL.Emit(OpCodes.Ldarg_0);
            setPropertyIL.Emit(OpCodes.Ldarg_1);
            setPropertyIL.Emit(OpCodes.Stfld, fieldBuilder);
            setPropertyIL.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getProperty);
            propertyBuilder.SetSetMethod(setProperty);
            
            return propertyBuilder;
        }
        private static void CreateActionName(TypeBuilder builder, string value)
        {
            var propertyBuilder = builder.DefineProperty("ActionName", 
                PropertyAttributes.HasDefault | PropertyAttributes.SpecialName,
                typeof(string), null);

            var getProperty = builder.DefineMethod("get_ActionName", 
                MethodAttributes.Family | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                typeof(string), Type.EmptyTypes);

            var getPropertyIL = getProperty.GetILGenerator();
            getPropertyIL.Emit(OpCodes.Ldstr, value);
            getPropertyIL.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getProperty);
        }

        private static void CreateInputValidation(TypeBuilder builder, TypeBuilder input, MethodInfo isValid, PropertyBuilder inputProperty)
        {
            var isInputValidBuilder = builder.DefineMethod(nameof(ActionInfo.IsValid),
                MethodAttributes.Public | MethodAttributes.ReuseSlot | MethodAttributes.Virtual | MethodAttributes.HideBySig,
                typeof(bool), Type.EmptyTypes);
            
            var isInputValid = isInputValidBuilder.GetILGenerator();

            foreach (var property in input.GetProperties())
            {
                isInputValid.Emit(OpCodes.Ldarg_0);
                isInputValid.EmitCall(OpCodes.Call, inputProperty.GetMethod!, null);
                isInputValid.EmitCall(OpCodes.Callvirt, property.GetMethod!, null );
            }
            
            isInputValid.EmitCall(OpCodes.Call, isValid, null);
            isInputValid.Emit(OpCodes.Ret);
        }
        
        private static void CreateParameterValidation(ILGenerator il, ParameterInfo[] parameters)
        {
            Label? next = null;
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var validationParameter = parameter.GetCustomAttribute<ThingParameterAttribute>();

                if (parameter.GetCustomAttribute<FromServicesAttribute>() != null 
                    || validationParameter == null
                    || parameter.ParameterType == typeof(CancellationToken))
                {
                    continue;
                }

                var parameterType = parameter.ParameterType.GetUnderlyingType();
                
                if (IsNumber(parameter.ParameterType))
                {
                    if (validationParameter.MinimumValue.HasValue)
                    {
                        var code = IsComplexNumber(parameterType) ? OpCodes.Bge_Un_S : OpCodes.Bge_S;
                        GenerateNumberValidation(il, i, parameterType, validationParameter.MinimumValue.Value, code, ref next);
                    }
                
                    if (validationParameter.MaximumValue.HasValue)
                    {
                        var code = IsComplexNumber(parameterType) ? OpCodes.Ble_Un_S : OpCodes.Ble_S;
                        GenerateNumberValidation(il, i, parameterType, validationParameter.MaximumValue.Value, code, ref next);
                    }
                    
                    if (validationParameter.ExclusiveMinimumValue.HasValue)
                    {
                        var code = IsComplexNumber(parameterType) ? OpCodes.Bgt_Un_S : OpCodes.Bgt_S;
                        GenerateNumberValidation(il, i, parameterType, validationParameter.ExclusiveMinimumValue.Value, code, ref next);
                    }
                    
                    if (validationParameter.ExclusiveMaximumValue.HasValue)
                    {
                        var code = IsComplexNumber(parameterType) ? OpCodes.Blt_Un_S : OpCodes.Blt_S;
                        GenerateNumberValidation(il, i, parameterType, validationParameter.ExclusiveMaximumValue.Value, code, ref next);
                    }
                
                    if (validationParameter.MultipleOfValue.HasValue)
                    {
                        if (next != null)
                        {
                            il.MarkLabel(next.Value);
                        }
                        
                        next = il.DefineLabel();
                        
                        il.Emit(OpCodes.Ldarg_S, i);
                        SetValue(il, validationParameter.MultipleOfValue.Value, parameter.ParameterType);

                        if (parameter.ParameterType == typeof(float) 
                            || parameter.ParameterType == typeof(double) 
                            || parameter.ParameterType == typeof(decimal))
                        {
                            il.Emit(OpCodes.Rem);
                            if (parameter.ParameterType == typeof(float))
                            {
                                il.Emit(OpCodes.Ldc_R4 , (float)0);
                            }
                            else
                            {
                                il.Emit(OpCodes.Ldc_R8, (double)0);
                            }
                            
                            il.Emit(OpCodes.Beq_S, next.Value);
                        }
                        else
                        {
                            il.Emit(parameter.ParameterType == typeof(ulong) ? OpCodes.Rem_Un : OpCodes.Rem);
                            il.Emit(OpCodes.Brfalse_S, next.Value);
                        }
                        
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Ret);
                    }
                }
                else if (IsString(parameter.ParameterType))
                {
                    if (validationParameter.MinimumLengthValue.HasValue)
                    {
                        GenerateStringLengthValidation(il, i, validationParameter.MinimumLengthValue.Value, OpCodes.Bge_S, ref next);
                    }
                    
                    if (validationParameter.MaximumLengthValue.HasValue)
                    {
                        GenerateStringLengthValidation(il, i, validationParameter.MaximumLengthValue.Value, OpCodes.Ble_S, ref next);
                    }
                }
            }
            
            if (next.HasValue)
            {
                il.MarkLabel(next.Value);
            }
            
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Ret);

            static void GenerateNumberValidation(ILGenerator generator, int fieldIndex, Type fieldType, double value, OpCode code, ref Label? next)
            {
                if (next != null)
                {
                    generator.MarkLabel(next.Value);
                }

                next = generator.DefineLabel();
                
                generator.Emit(OpCodes.Ldarg_S, fieldIndex);
                SetValue(generator, value, fieldType);
                
                generator.Emit(code, next.Value);
                
                generator.Emit(OpCodes.Ldc_I4_0);
                generator.Emit(OpCodes.Ret);
            }

            static void GenerateStringLengthValidation(ILGenerator generator, int fieldIndex, uint value, OpCode code, ref Label? next)
            {
                if (next != null)
                {
                    generator.MarkLabel(next.Value);
                }

                next = generator.DefineLabel();

                var nextCheckNull = generator.DefineLabel();
                
                generator.Emit(OpCodes.Ldarg_S, fieldIndex);
                generator.Emit(OpCodes.Brfalse_S, nextCheckNull);
                
                generator.Emit(OpCodes.Ldarg_S, fieldIndex);
                generator.EmitCall(OpCodes.Callvirt, s_getLength, null);
                generator.Emit(OpCodes.Ldc_I4, value);
                generator.Emit(code, next.Value);
                
                generator.MarkLabel(nextCheckNull);
                generator.Emit(OpCodes.Ldc_I4_0);
                generator.Emit(OpCodes.Ret);
            }
            
            static void SetValue(ILGenerator generator, double value, Type fieldType)
            {
                if (fieldType == typeof(byte))
                {
                    var convert = Convert.ToByte(value);
                    generator.Emit(OpCodes.Ldc_I4_S, convert);
                }
                else if (fieldType == typeof(sbyte))
                {
                    var convert = Convert.ToSByte(value);
                    generator.Emit(OpCodes.Ldc_I4_S, convert);
                }
                else if (fieldType == typeof(short))
                {
                    var convert = Convert.ToInt16(value);
                    generator.Emit(OpCodes.Ldc_I4_S, convert);
                }
                else if (fieldType == typeof(ushort))
                {
                    var convert = Convert.ToUInt16(value);
                    generator.Emit(OpCodes.Ldc_I4_S, convert);
                }
                else if (fieldType == typeof(int))
                {
                    var convert = Convert.ToInt32(value);
                    generator.Emit(OpCodes.Ldc_I4_S, convert);
                }
                else if (fieldType == typeof(uint))
                {
                    var convert = Convert.ToUInt32(value);
                    generator.Emit(OpCodes.Ldc_I4_S, convert);
                }
                else if (fieldType == typeof(long))
                {
                    var convert = Convert.ToInt64(value);
                    generator.Emit(OpCodes.Ldc_I8, convert);
                }
                else if (fieldType == typeof(ulong))
                {
                    var convert = Convert.ToUInt64(value);
                    if (convert <= uint.MaxValue)
                    {
                        generator.Emit(OpCodes.Ldc_I4_S, (int)convert);
                        generator.Emit(OpCodes.Conv_I8);
                    }
                    else
                    {
                        generator.Emit(OpCodes.Ldc_I8, convert);
                    }
                }
                else if (fieldType == typeof(float))
                {
                    var convert = Convert.ToSingle(value);
                    generator.Emit(OpCodes.Ldc_R4, convert);
                }
                else
                {
                    var convert = Convert.ToDouble(value);
                    generator.Emit(OpCodes.Ldc_R8, convert);
                }
            }

            static bool IsComplexNumber(Type parameterType)
                => parameterType == typeof(ulong)
                   || parameterType == typeof(float)
                   || parameterType == typeof(double)
                   || parameterType == typeof(decimal);

            static bool IsString(Type type)
                => type == typeof(string);
            
            static bool IsNumber(Type type)
                => type == typeof(int)
                   || type == typeof(uint)
                   || type == typeof(long)
                   || type == typeof(ulong)
                   || type == typeof(short)
                   || type == typeof(ushort)
                   || type == typeof(double)
                   || type == typeof(float)
                   || type == typeof(decimal)
                   || type == typeof(byte)
                   || type == typeof(sbyte);
        }

        private static void CreateExecuteAsync(TypeBuilder builder, TypeBuilder inputBuilder, PropertyBuilder input, MethodInfo action, Type thingType)
        {
            var executeBuilder = builder.DefineMethod("InternalExecuteAsync",
                MethodAttributes.Family | MethodAttributes.ReuseSlot | MethodAttributes.Virtual | MethodAttributes.HideBySig,
                typeof(ValueTask), new [] { typeof(Thing), typeof(IServiceProvider) });
            
            var getService = typeof(IServiceProvider).GetMethod(nameof(IServiceProvider.GetService));
            var getTypeFromHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle));

            var getSource = typeof(ActionInfo).GetProperty("Source", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var getToken = typeof(CancellationTokenSource).GetProperty(nameof(CancellationTokenSource.Token), 
                BindingFlags.Public | BindingFlags.Instance)!;
            
            var il = executeBuilder.GetILGenerator();
            il.DeclareLocal(typeof(ValueTask));
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, thingType);


            var inputProperties = inputBuilder.GetProperties();
            var counter = 0;
            foreach (var parameter in action.GetParameters())
            {
                if (parameter.GetCustomAttribute<FromServicesAttribute>() != null)
                {
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Ldtoken, parameter.ParameterType);
                    il.EmitCall(OpCodes.Call, getTypeFromHandle, null);
                    il.EmitCall(OpCodes.Callvirt, getService, null);
                    il.Emit(OpCodes.Castclass, parameter.ParameterType);
                }
                else if (parameter.ParameterType == typeof(CancellationToken))
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.EmitCall(OpCodes.Call, getSource.GetMethod!, null);
                    il.EmitCall(OpCodes.Callvirt, getToken.GetMethod!, null);
                }
                else
                {
                    var property = inputProperties[counter++];
                    il.Emit(OpCodes.Ldarg_0);
                    il.EmitCall(OpCodes.Call, input.GetMethod!, null);
                    il.EmitCall(OpCodes.Callvirt, property.GetMethod!, null);
                }
            }

            il.EmitCall(OpCodes.Callvirt, action, null);
            if (action.ReturnType == typeof(void))
            {
                il.Emit(OpCodes.Ldloca_S, 0);
                il.Emit(OpCodes.Initobj, typeof(ValueTask));
                il.Emit(OpCodes.Ldloc_0);
            }
            else if(action.ReturnType == typeof(Task))
            {
                var constructor = typeof(ValueTask).GetConstructor(new[] {typeof(Task)});
                il.Emit(OpCodes.Newobj, constructor);
            }
            
            il.Emit(OpCodes.Ret);
        }

       
    }
}
