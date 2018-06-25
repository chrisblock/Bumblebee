﻿using System;
using System.Collections.Generic;

using Bumblebee.Interfaces;
using Bumblebee.Setup;
using Bumblebee.Specifications;

using OpenQA.Selenium;

namespace Bumblebee.Implementation
{
	public abstract class Block : IBlock
	{
		protected static readonly ISpecification By = new Specification();

		internal Block(Session session, By @by)
		{
			if (session == null)
			{
				throw new ArgumentNullException(nameof (session));
			}

			if (@by == null)
			{
				throw new ArgumentNullException(nameof (@by));
			}

			Session = session;
			Specification = @by;

			InitializeCurrentBlock();

			Session.Monkey?.Invoke(this);
		}

		protected Block(IBlock parent, By @by)
		{
			if (parent == null)
			{
				throw new ArgumentNullException(nameof (parent));
			}

			if (@by == null)
			{
				throw new ArgumentNullException(nameof (@by));
			}

			Parent = parent;
			Session = parent.Session;
			Specification = @by;

			InitializeCurrentBlock();

			Session.Monkey?.Invoke(this);
		}

		/// <summary>
		/// Allows for the creation of a derived Block based on a Session using reflection.
		/// </summary>
		/// <remarks>
		/// This method is used by the Session.CurrentBlock() method, the FindRelated&lt;T&gt;() method and the Page.Create&lt;T&gt;(Session) method.
		/// </remarks>
		/// <param name="session"></param>
		/// <typeparam name="TBlock">The type of block to create.</typeparam>
		/// <returns>The newly constructed Block.</returns>
		/// <exception cref="ArgumentNullException">Will be thrown if session is null.</exception>
		/// <exception cref="ArgumentException">Will be thrown if type TBlock has no constructor accepting only a Session.</exception>
		internal static TBlock Create<TBlock>(Session session) where TBlock : IBlock
		{
			if (session == null)
			{
				throw new ArgumentNullException(nameof (session));
			}

			var type = typeof (TBlock);
			var ctor = type.GetConstructor(new[] { typeof (Session) });

			if (ctor == null)
			{
				throw new ArgumentException($"The specified type ({type}) is not a valid block or page. It must have a constructor that takes only a session.");
			}

			var result = (TBlock) ctor.Invoke(new object[] { session });

			return result;
		}

		/// <summary>
		/// Allows for the creation of a derived Block based on a parent Block and By specification using reflection.
		/// </summary>
		/// <param name="parent">The parent Block of the type to create.</param>
		/// <param name="by">The specification used to find the backing element of the block.</param>
		/// <typeparam name="TBlock">The type of Block to be constructed.</typeparam>
		/// <returns>The newly constructed Block.</returns>
		/// <exception cref="ArgumentNullException">Will be thrown if either parent or by is null.</exception>
		/// <exception cref="ArgumentException">Will be thrown if type TBlock has no constructor accepting an IBlock parent and a By specification.</exception>
		public static TBlock Create<TBlock>(IBlock parent, By @by) where TBlock : IBlock
		{
			if (parent == null)
			{
				throw new ArgumentNullException(nameof (parent));
			}

			if (@by == null)
			{
				throw new ArgumentNullException(nameof (@by));
			}

			var type = typeof (TBlock);
			var ctor = type.GetConstructor(new[] { typeof (IBlock), typeof (By) });

			if (ctor == null)
			{
				throw new ArgumentException($"The specified type ({type}) is not a valid block. It must have a constructor that takes an IBlock parent and a By specification.");
			}

			var result = (TBlock) ctor.Invoke(new object[] { parent, @by });

			return result;
		}

		public IBlock Parent { get; }

		public Session Session { get; }

		public By Specification { get; }

		private void InitializeCurrentBlock()
		{
			Session.SetCurrentBlock(this);
		}

		/// <summary>
		/// Gets the Selenium IWebElement that underpins this component.
		/// </summary>
		public virtual IWebElement Tag => Parent.FindElement(Specification);

		/// <summary>
		/// Gets the value of the specified attribute for this component.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <returns>The value of the attribute.</returns>
		public virtual string GetAttribute(string name)
		{
			return Tag.GetAttribute(name);
		}

		public virtual IWebElement FindElement(By @by)
		{
			return Tag.FindElement(@by);
		}

		public virtual IEnumerable<IWebElement> FindElements(By @by)
		{
			return Tag.FindElements(@by);
		}

		[Obsolete("This method is obsolete. Please use FindElement(By @by) instead.", error: true)]
		public virtual IWebElement GetElement(By @by)
		{
			return null;
		}

		[Obsolete("This method is obsolete. Please use FindElements(By @by) instead.", error: true)]
		public virtual IEnumerable<IWebElement> GetElements(By @by)
		{
			return null;
		}

		protected virtual T FindBlock<T>(By @by) where T : IBlock
		{
			return Create<T>(this, @by);
		}

		protected virtual IEnumerable<T> FindBlocks<T>(By @by) where T : IBlock
		{
			return new Blocks<T>(this, @by);
		}

		protected virtual T FindElement<T>(By @by) where T : IElement
		{
			return Element.Create<T>(this, @by);
		}

		/// <summary>
		/// Gets an <see cref="Implementation.Elements" /> collection of element type <typeparamref name="T" />.
		/// </summary>
		/// <typeparam name="T">The type of element object to create. Must extend <see cref="IElement" />.</typeparam>
		/// <param name="by">The specification for finding the set of elements.</param>
		/// <returns></returns>
		protected virtual IEnumerable<T> FindElements<T>(By @by) where T : IElement
		{
			return new Elements<T>(this, @by);
		}

		public virtual IPerformsDragAndDrop GetDragAndDropPerformer()
		{
			return new WebDragAndDropPerformer(Session.Driver);
		}

		public virtual void VerifyMonkeyState()
		{
		}
	}
}
