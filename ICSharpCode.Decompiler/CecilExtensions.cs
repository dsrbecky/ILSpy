﻿// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ICSharpCode.Decompiler
{
	/// <summary>
	/// Cecil helper methods.
	/// </summary>
	public static class CecilExtensions
	{
		#region GetPushDelta / GetPopDelta
		public static int GetPushDelta(this Instruction instruction)
		{
			OpCode code = instruction.OpCode;
			switch (code.StackBehaviourPush) {
				case StackBehaviour.Push0:
					return 0;

				case StackBehaviour.Push1:
				case StackBehaviour.Pushi:
				case StackBehaviour.Pushi8:
				case StackBehaviour.Pushr4:
				case StackBehaviour.Pushr8:
				case StackBehaviour.Pushref:
					return 1;

				case StackBehaviour.Push1_push1:
					return 2;

				case StackBehaviour.Varpush:
					if (code.FlowControl != FlowControl.Call)
						break;

					IMethodSignature method = (IMethodSignature) instruction.Operand;
					return IsVoid (method.ReturnType) ? 0 : 1;
			}

			throw new NotSupportedException ();
		}
		
		public static int GetPopDelta(this Instruction instruction, MethodDefinition current, int currentStackSize)
		{
			OpCode code = instruction.OpCode;
			switch (code.StackBehaviourPop) {
				case StackBehaviour.Pop0:
					return 0;
				case StackBehaviour.Popi:
				case StackBehaviour.Popref:
				case StackBehaviour.Pop1:
					return 1;

				case StackBehaviour.Pop1_pop1:
				case StackBehaviour.Popi_pop1:
				case StackBehaviour.Popi_popi:
				case StackBehaviour.Popi_popi8:
				case StackBehaviour.Popi_popr4:
				case StackBehaviour.Popi_popr8:
				case StackBehaviour.Popref_pop1:
				case StackBehaviour.Popref_popi:
					return 2;

				case StackBehaviour.Popi_popi_popi:
				case StackBehaviour.Popref_popi_popi:
				case StackBehaviour.Popref_popi_popi8:
				case StackBehaviour.Popref_popi_popr4:
				case StackBehaviour.Popref_popi_popr8:
				case StackBehaviour.Popref_popi_popref:
					return 3;

				case StackBehaviour.PopAll:
					return currentStackSize;

				case StackBehaviour.Varpop:
					if (code == OpCodes.Ret)
						return IsVoid (current.ReturnType) ? 0 : 1;

					if (code.FlowControl != FlowControl.Call)
						break;

					IMethodSignature method = (IMethodSignature) instruction.Operand;
					int count = method.HasParameters ? method.Parameters.Count : 0;
					if (method.HasThis && code != OpCodes.Newobj)
						++count;

					return count;
			}

			throw new NotSupportedException ();
		}
		
		public static bool IsVoid(this TypeReference type)
		{
			return type.FullName == "System.Void" && !(type is TypeSpecification);
		}
		
		public static bool IsValueTypeOrVoid(this TypeReference type)
		{
			while (type is OptionalModifierType || type is RequiredModifierType)
				type = ((TypeSpecification)type).ElementType;
			if (type is ArrayType)
				return false;
			return type.IsValueType || type.IsVoid();
		}
		#endregion
		
		/// <summary>
		/// Gets the (exclusive) end offset of this instruction.
		/// </summary>
		public static int GetEndOffset(this Instruction inst)
		{
			return inst.Offset + inst.GetSize();
		}
		
		public static string OffsetToString(int offset)
		{
			return string.Format("IL_{0:x4}", offset);
		}
		
		public static HashSet<MethodDefinition> GetAccessorMethods(this TypeDefinition type)
		{
			HashSet<MethodDefinition> accessorMethods = new HashSet<MethodDefinition>();
			foreach (var property in type.Properties) {
				accessorMethods.Add(property.GetMethod);
				accessorMethods.Add(property.SetMethod);
				if (property.HasOtherMethods) {
					foreach (var m in property.OtherMethods)
						accessorMethods.Add(m);
				}
			}
			foreach (EventDefinition ev in type.Events) {
				accessorMethods.Add(ev.AddMethod);
				accessorMethods.Add(ev.RemoveMethod);
				accessorMethods.Add(ev.InvokeMethod);
				if (ev.HasOtherMethods) {
					foreach (var m in ev.OtherMethods)
						accessorMethods.Add(m);
				}
			}
			return accessorMethods;
		}
		
		public static TypeDefinition ResolveWithinSameModule(this TypeReference type)
		{
			if (type != null && type.GetElementType().Module == type.Module)
				return type.Resolve();
			else
				return null;
		}
		
		public static FieldDefinition ResolveWithinSameModule(this FieldReference field)
		{
			if (field != null && field.DeclaringType.GetElementType().Module == field.Module)
				return field.Resolve();
			else
				return null;
		}
		
		public static MethodDefinition ResolveWithinSameModule(this MethodReference method)
		{
			if (method != null && method.DeclaringType.GetElementType().Module == method.Module)
				return method.Resolve();
			else
				return null;
		}
		
		public static bool IsCompilerGenerated(this ICustomAttributeProvider provider)
		{
			if (provider != null && provider.HasCustomAttributes) {
				foreach (CustomAttribute a in provider.CustomAttributes) {
					if (a.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute")
						return true;
				}
			}
			return false;
		}
		
		public static bool IsCompilerGeneratedOrIsInCompilerGeneratedClass(this IMemberDefinition member)
		{
			if (member == null)
				return false;
			if (member.IsCompilerGenerated())
				return true;
			return IsCompilerGeneratedOrIsInCompilerGeneratedClass(member.DeclaringType);
		}
	}
}
