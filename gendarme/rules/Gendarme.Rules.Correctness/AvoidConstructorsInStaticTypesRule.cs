//
// Gendarme.Rules.Correctness.AvoidConstructorsInStaticTypesRule
//
// Authors:
//	Lukasz Knop <lukasz.knop@gmail.com>
//	Sebastien Pouliot <sebastien@ximian.com>
//
// Copyright (C) 2007 Lukasz Knop
// Copyright (C) 2007-2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using Mono.Cecil;

using Gendarme.Framework;
using Gendarme.Framework.Rocks;

namespace Gendarme.Rules.Correctness {

	// note: this rule is similar to Gendarme.Rules.Design.ConsiderUsingStaticTypeRule
	// but is also applicable to the 1.x runtime (hidding the default ctor was still 
	// the correct way of handling this back then). The goal is to make this ctor 
	// non-visible so people can't instantiate it (e.g. by mistake)

	[Problem ("This type (and ancestors) contains only static fields and methods but has a visible, non static, constructor.")]
	[Solution ("You should remove the visible constructor to make sure this type cannot be instancied, or if using 2.0+, change it to a static type.")]
	public class AvoidConstructorsInStaticTypesRule : Rule, ITypeRule {

		static bool IsAllStatic (TypeDefinition type)
		{
			foreach (MethodDefinition ctor in type.Constructors) {
				// let's the default ctor pass (since it's always here for 1.x code)
				if (!ctor.IsStatic && (ctor.Parameters.Count > 1))
					return false;
			}

			foreach (MethodDefinition method in type.Methods) {
				if (!method.IsStatic)
					return false;
			}

			foreach (FieldDefinition field in type.Fields) {
				if (!field.IsStatic)
					return false;
			}

			if (type.BaseType.FullName == "System.Object")
				return true;

			return IsAllStatic (type.BaseType.Resolve ());
		}

		public RuleResult CheckType (TypeDefinition type)
		{
			// rule applies only if the type isn't: an enum, an interface, a struct, a delegate or compiler generated
			if (type.IsEnum || type.IsInterface || type.IsValueType || type.IsDelegate () || type.IsGeneratedCode ())
				return RuleResult.DoesNotApply;

			// it also does not apply if the type is static (2.0+ only)
			if (type.IsStatic ())
				return RuleResult.DoesNotApply;

			if (!IsAllStatic (type))
				return RuleResult.Success;

			// rule applies!

			foreach (MethodDefinition ctor in type.Constructors) {
				if (!ctor.IsStatic && ctor.IsVisible ()) {
					Runner.Report (ctor, Severity.Low, Confidence.High);
				}
			}

			return Runner.CurrentRuleResult;
		}
	}
}
