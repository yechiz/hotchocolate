using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

namespace HotChocolate.Data.Filters;

public class FilterInputTypeDescriptor
    : DescriptorBase<FilterInputTypeDefinition>
    , IFilterInputTypeDescriptor
{
    protected FilterInputTypeDescriptor(
        IDescriptorContext context,
        Type entityType,
        string? scope)
        : base(context)
    {
        Convention = context.GetFilterConvention(scope);
        Definition.EntityType = entityType ??
            throw new ArgumentNullException(nameof(entityType));
        Definition.RuntimeType = typeof(object);
        Definition.Name = Convention.GetTypeName(entityType);
        Definition.Description = Convention.GetTypeDescription(entityType);
        Definition.Fields.BindingBehavior = context.Options.DefaultBindingBehavior;
        Definition.Scope = scope;
        Definition.UseAnd = Convention.IsAndAllowed();
        Definition.UseOr = Convention.IsOrAllowed();
    }

    protected FilterInputTypeDescriptor(
        IDescriptorContext context,
        string? scope)
        : base(context)
    {
        Convention = context.GetFilterConvention(scope);
        Definition.RuntimeType = typeof(object);
        Definition.EntityType = typeof(object);
        Definition.Scope = scope;
    }

    protected FilterInputTypeDescriptor(
        IDescriptorContext context,
        FilterInputTypeDefinition definition,
        string? scope)
        : base(context)
    {
        Convention = context.GetFilterConvention(scope);
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    protected IFilterConvention Convention { get; }

    protected internal override FilterInputTypeDefinition Definition { get; protected set; } = new();

    protected BindableList<FilterFieldDescriptor> Fields { get; } = new();

    protected BindableList<FilterOperationFieldDescriptor> Operations { get; } = new();

    Type IHasRuntimeType.RuntimeType => Definition.RuntimeType;

    protected override void OnCreateDefinition(FilterInputTypeDefinition definition)
    {
        if (!Definition.AttributesAreApplied && Definition.EntityType is not null)
        {
            Context.TypeInspector.ApplyAttributes(Context, this, Definition.EntityType);
            Definition.AttributesAreApplied = true;
        }

        var fields = new Dictionary<NameString, FilterFieldDefinition>();
        var handledProperties = new HashSet<MemberInfo>();

        FieldDescriptorUtilities.AddExplicitFields(
            Fields.Select(t => t.CreateDefinition())
                .Concat(Operations.Select(t => t.CreateDefinition())),
            f => f.Member,
            fields,
            handledProperties);

        OnCompleteFields(fields, handledProperties);

        Definition.Fields.AddRange(fields.Values);
    }

    protected virtual void OnCompleteFields(
        IDictionary<NameString, FilterFieldDefinition> fields,
        ISet<MemberInfo> handledProperties)
    {
    }

    /// <inheritdoc />
    public IFilterInputTypeDescriptor Name(NameString value)
    {
        Definition.Name = value.EnsureNotEmpty(nameof(value));
        return this;
    }

    /// <inheritdoc />
    public IFilterInputTypeDescriptor Description(string? value)
    {
        Definition.Description = value;
        return this;
    }

    protected IFilterInputTypeDescriptor BindFields(BindingBehavior bindingBehavior)
    {
        Definition.Fields.BindingBehavior = bindingBehavior;
        return this;
    }

    protected IFilterInputTypeDescriptor BindFieldsExplicitly() =>
        BindFields(BindingBehavior.Explicit);

    protected IFilterInputTypeDescriptor BindFieldsImplicitly() =>
        BindFields(BindingBehavior.Implicit);

    /// <inheritdoc />
    public IFilterOperationFieldDescriptor Operation(int operationId)
    {
        FilterOperationFieldDescriptor? fieldDescriptor =
            Operations.FirstOrDefault(t => t.Definition.Id == operationId);

        if (fieldDescriptor is null)
        {
            fieldDescriptor = FilterOperationFieldDescriptor.New(
                Context,
                operationId,
                Definition.Scope);
            Operations.Add(fieldDescriptor);
        }

        return fieldDescriptor;
    }

    /// <inheritdoc />
    public IFilterFieldDescriptor Field(NameString name)
    {
        FilterFieldDescriptor? fieldDescriptor =
            Fields.FirstOrDefault(t => t.Definition.Name == name);

        if (fieldDescriptor is null)
        {
            fieldDescriptor = FilterFieldDescriptor.New(Context, name, Definition.Scope);
            Fields.Add(fieldDescriptor);
        }

        return fieldDescriptor;
    }

    /// <inheritdoc />
    public IFilterFieldDescriptor Field(
        NameString name,
        Action<IFilterInputTypeDescriptor> configure)
    {
        IFilterFieldDescriptor descriptor = Field(name);
        descriptor.Extend().Definition.CreateFieldTypeDefinition = CreateFieldTypeDefinition;
        return descriptor;

        FilterInputTypeDefinition CreateFieldTypeDefinition(
            IDescriptorContext context,
            string? scope)
        {
            FilterInputTypeDescriptor descriptor = Inline(context, typeof(object), scope);
            configure(descriptor);
            return descriptor.CreateDefinition();
        }
    }

    /// <inheritdoc />
    public IFilterInputTypeDescriptor Ignore(int operationId)
    {
        FilterOperationFieldDescriptor? fieldDescriptor =
            Operations.FirstOrDefault(t => t.Definition.Id == operationId);

        if (fieldDescriptor is null)
        {
            fieldDescriptor = FilterOperationFieldDescriptor.New(
                Context,
                operationId,
                Definition.Scope);
            Operations.Add(fieldDescriptor);
        }

        fieldDescriptor.Ignore();
        return this;
    }

    /// <inheritdoc />
    public IFilterInputTypeDescriptor Ignore(NameString name)
    {
        FilterFieldDescriptor? fieldDescriptor =
            Fields.FirstOrDefault(t => t.Definition.Name == name);

        if (fieldDescriptor is null)
        {
            fieldDescriptor = FilterFieldDescriptor.New(
                Context,
                name,
                Definition.Scope);
            Fields.Add(fieldDescriptor);
        }

        fieldDescriptor.Ignore();
        return this;
    }

    /// <inheritdoc />
    public IFilterInputTypeDescriptor AllowOr(bool allow = true)
    {
        Definition.UseOr = allow;
        return this;
    }

    /// <inheritdoc />
    public IFilterInputTypeDescriptor AllowAnd(bool allow = true)
    {
        Definition.UseAnd = allow;
        return this;
    }

    /// <inheritdoc />
    public IFilterInputTypeDescriptor Directive<TDirective>(TDirective directive)
        where TDirective : class
    {
        Definition.AddDirective(directive, Context.TypeInspector);
        return this;
    }

    /// <inheritdoc />
    public IFilterInputTypeDescriptor Directive<TDirective>()
        where TDirective : class, new()
    {
        Definition.AddDirective(new TDirective(), Context.TypeInspector);
        return this;
    }

    /// <inheritdoc />
    public IFilterInputTypeDescriptor Directive(
        NameString name,
        params ArgumentNode[] arguments)
    {
        Definition.AddDirective(name, arguments);
        return this;
    }

    public static FilterInputTypeDescriptor New(
        IDescriptorContext context,
        Type entityType,
        string? scope = null) =>
        new FilterInputTypeDescriptor(context, entityType, scope);

    public static FilterInputTypeDescriptor<T> New<T>(
        IDescriptorContext context,
        Type entityType,
        string? scope = null) =>
        new FilterInputTypeDescriptor<T>(context, entityType, scope);

    public static FilterInputTypeDescriptor FromSchemaType(
        IDescriptorContext context,
        Type schemaType,
        string? scope = null)
    {
        FilterInputTypeDescriptor? descriptor = New(context, schemaType, scope);
        descriptor.Definition.RuntimeType = typeof(object);
        return descriptor;
    }

    internal static FilterInputTypeDescriptor Inline(
        IDescriptorContext context,
        Type entityType,
        string? scope = null)
    {
        FilterInputTypeDescriptor descriptor = New(context, entityType, scope);

        descriptor.BindFieldsExplicitly();

        // This resets the name on the definition. This way we can check if the user has
        // set a custom name. The context the user specifying descriptor.Name("Foo") is
        // preserved this way.
        descriptor.Definition.Name = default!;

        // we deactivate And and Or by default.
        descriptor.Definition.UseAnd = false;
        descriptor.Definition.UseOr = false;

        return descriptor;
    }

    internal static FilterInputTypeDescriptor<T> Inline<T>(
        IDescriptorContext context,
        Type entityType,
        string? scope = null)
    {
        FilterInputTypeDescriptor<T> descriptor = New<T>(context, entityType, scope);

        descriptor.BindFieldsExplicitly();

        // This resets the name on the definition. This way we can check if the user has
        // set a custom name. The context the user specifying descriptor.Name("Foo") is
        // preserved this way.
        descriptor.Definition.Name = default!;

        // we deactivate And and Or by default.
        descriptor.Definition.UseAnd = false;
        descriptor.Definition.UseOr = false;

        return descriptor;
    }

    public static FilterInputTypeDescriptor From(
        IDescriptorContext context,
        FilterInputTypeDefinition definition,
        string? scope = null) =>
        new FilterInputTypeDescriptor(context, definition, scope);

    public static FilterInputTypeDescriptor<T> From<T>(
        IDescriptorContext context,
        FilterInputTypeDefinition definition,
        string? scope = null) =>
        new FilterInputTypeDescriptor<T>(context, definition, scope);

    public static FilterInputTypeDescriptor<T> From<T>(
        FilterInputTypeDescriptor descriptor,
        string? scope = null) =>
        From<T>(descriptor.Context, descriptor.Definition, scope);
}
