# Coding Guidelines

## Definitions

* [CamelCase](http://en.wikipedia.org/wiki/CamelCase) is a casing convention where the first letter is lower-case, words are not separated by any character but have their first letter capitalized. Example: <code>thisIsCamelCased</code>. 
* [PascalCase](http://c2.com/cgi/wiki?PascalCase) is a casing convention where the first letter of each word is capitalized, and no separating character is included between words. Example: <code>ThisIsPascalCased</code>. 

## C# coding conventions

We should use the [Allman bracing style](http://en.wikipedia.org/wiki/Indent_style#Allman_style) for consistency.

We are using the C# coding conventions described in this document: [C# Coding Guidelines](http://blogs.msdn.com/brada/articles/361363.aspx) with the following exceptions:

* Each file should not start with a copyright notice. The ones at the root of the source tree will suffice. 
* Regions (#region) are not used. 
* using statements are on top of a file (outside of namespace {...}) 
* Use var only if you have an anonymous type or you can clearly tell what the type is from the right hand side of the expression (see examples below). 

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
		}
    }		
