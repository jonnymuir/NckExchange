//------------------------------------------------------------------------------
// <auto-generated>
//   This code was generated by a tool.
//
//    Umbraco.ModelsBuilder.Embedded v13.9.0+97cc3ca
//
//   Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Linq.Expressions;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Infrastructure.ModelsBuilder;
using Umbraco.Cms.Core;
using Umbraco.Extensions;

namespace Umbraco.Cms.Web.Common.PublishedModels
{
	// Mixin Content Type with alias "visibilityProperties"
	/// <summary>Visibility Properties</summary>
	public partial interface IVisibilityProperties : IPublishedElement
	{
		/// <summary>Hide</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "13.9.0+97cc3ca")]
		bool UmbracoNaviHide { get; }
	}

	/// <summary>Visibility Properties</summary>
	[PublishedModel("visibilityProperties")]
	public partial class VisibilityProperties : PublishedElementModel, IVisibilityProperties
	{
		// helpers
#pragma warning disable 0109 // new is redundant
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "13.9.0+97cc3ca")]
		public new const string ModelTypeAlias = "visibilityProperties";
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "13.9.0+97cc3ca")]
		public new const PublishedItemType ModelItemType = PublishedItemType.Content;
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "13.9.0+97cc3ca")]
		[return: global::System.Diagnostics.CodeAnalysis.MaybeNull]
		public new static IPublishedContentType GetModelContentType(IPublishedSnapshotAccessor publishedSnapshotAccessor)
			=> PublishedModelUtility.GetModelContentType(publishedSnapshotAccessor, ModelItemType, ModelTypeAlias);
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "13.9.0+97cc3ca")]
		[return: global::System.Diagnostics.CodeAnalysis.MaybeNull]
		public static IPublishedPropertyType GetModelPropertyType<TValue>(IPublishedSnapshotAccessor publishedSnapshotAccessor, Expression<Func<VisibilityProperties, TValue>> selector)
			=> PublishedModelUtility.GetModelPropertyType(GetModelContentType(publishedSnapshotAccessor), selector);
#pragma warning restore 0109

		private IPublishedValueFallback _publishedValueFallback;

		// ctor
		public VisibilityProperties(IPublishedElement content, IPublishedValueFallback publishedValueFallback)
			: base(content, publishedValueFallback)
		{
			_publishedValueFallback = publishedValueFallback;
		}

		// properties

		///<summary>
		/// Hide: Set this to be true if you want to hide this page from search results and list pages
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "13.9.0+97cc3ca")]
		[ImplementPropertyType("umbracoNaviHide")]
		public virtual bool UmbracoNaviHide => GetUmbracoNaviHide(this, _publishedValueFallback);

		/// <summary>Static getter for Hide</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "13.9.0+97cc3ca")]
		public static bool GetUmbracoNaviHide(IVisibilityProperties that, IPublishedValueFallback publishedValueFallback) => that.Value<bool>(publishedValueFallback, "umbracoNaviHide");
	}
}
