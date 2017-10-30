# Coding Guidelines

## Definitions

* [CamelCase](http://en.wikipedia.org/wiki/CamelCase) is a casing convention where the first letter is lower-case, words are not separated by any character but have their first letter capitalized. Example: <code>thisIsCamelCased</code>. 
* [PascalCase](http://c2.com/cgi/wiki?PascalCase) is a casing convention where the first letter of each word is capitalized, and no separating character is included between words. Example: <code>ThisIsPascalCased</code>. 

## C# coding conventions

We should use the [Allman bracing style](http://en.wikipedia.org/wiki/Indent_style#Allman_style) for consistency.

We are using the C# coding conventions described in this document as a guide, not everything in this doc is gospel and is open to debate: [C# Coding Guidelines](http://blogs.msdn.com/brada/articles/361363.aspx) with the following exceptions:

* Each file should not start with a copyright notice. The ones at the root of the source tree will suffice. 
* Regions (#region) are not used. 
* using statements are on top of a file (outside of namespace {...}) 
* Use var only if you have an anonymous type or you can clearly tell what the type is from the right hand side of the expression 
* Member variables should always be private, public access should be provided by an encapsulated property.

#### Naming
Follow all .NET Framework Design Guidelines for both internal and external members. Highlights of these include:
* Do use camelCasing for member variables, parameters and local variables
* Do use PascalCasing for function, property, event, and class names
* Do prefix interfaces names with “I”
* Do __not__ use Hungarian notation
* Do __not__ use a prefix for member variables (_, m_, s_, etc.). If you want to distinguish between local and member variables you should use “this.”
* Do __not__ prefix enums, classes, or delegates with any letter

Here is some sample code that follows these conventions.

	using System;
	namespace NuGet
	{
		public class ClassName
		{
			private List<SomeType> privateMember;

			public List<SomeType> SomeProperty
			{
				get
				{
					return privateMember;
				}
			}

			public string SomeAutoProperty { get; set; }

			public string SomeMethod(bool someCondition)
			{
				if (someCondition)
				{
					DoSomething(someArgument);
				}
				else
				{
					return someArray[10];
				}

				switch (status)
				{
					case Status.Foo:
						return "Foo";

					case Status.Bar:
						return "Bar";

					default:
						return "Bar";
				}
				return String.Empty;
			}
			
			private string AnotherMethod(){
				return privateMember.Count;
			}
		}
    }		
