namespace Filtering.Net.Generator;

internal static class PerPropertyClassEmitter
{
    public static string Emit(FilterClassModel model, PropertyMappingModel property) =>
        ScribanRuntime.Render("PerPropertyClass", BuildView(model, property));

    public static string EmitOverride(FilterClassModel model, PropertyOverrideModel overrideModel) =>
        ScribanRuntime.Render("PerPropertyClass", BuildOverrideView(model, overrideModel));

    internal static PerPropertyView BuildView(FilterClassModel model, PropertyMappingModel property)
    {
        var entityFullName = "global::" + model.FullEntityTypeName;
        var propertyIdentifier = EmissionNames.PropertyIdentifier(property.PropertyName);
        var valueShapeSourceType = property.PropertyClrType;
        var valueShape = PropertyValueShapeResolver.Resolve(valueShapeSourceType, property.ExtractorProfileFullName);
        var interceptor = model.Interceptors.FirstOrDefault(i => i.PropertyName == property.PropertyName);
        var grouping = OperatorShapeGrouping.Build(property);
        var hasInterceptor = interceptor is not null && !interceptor.Raw && interceptor.ValueClrType is not null;

        var build = new PerPropertyBuildView(
            IsOverride: false,
            ScalarArm: BuildScalarArm(valueShape, interceptor, model, grouping.ScalarOperators),
            ArrayArm: BuildArrayArm(valueShape, grouping.ArrayOperators),
            NoneOperators: grouping.NoneOperators,
            CustomBuildArms: [.. grouping.CustomOperators.Select(BuildCustomArm)],
            OverrideBuildArms: []);

        var simpleForwarder = (!hasInterceptor && grouping.CustomOperators.Count == 0)
            ? BuildSimpleValidateForwarder(valueShape, grouping)
            : null;

        var validate = new PerPropertyValidateView(
            IsOverride: false,
            ScalarArm: BuildScalarValidateArm(property, valueShape, interceptor, model, grouping.ScalarOperators),
            ArrayArm: BuildArrayValidateArm(valueShape, grouping.ArrayOperators),
            NoneOperators: grouping.NoneOperators,
            CustomValidateArms: [.. grouping.CustomOperators.Select(BuildCustomValidateArm)],
            OverrideValidateArms: [],
            SimpleForwarder: simpleForwarder);

        var typedLeafMethods = property.AllowedOperators
            .Select(op => RenderMappedTypedLeafMethod(entityFullName, property, valueShape, op))
            .ToList();

        var sort = property.Sortable
            ? new PerPropertySortView(EmissionNames.PropertyAccessor("entity", property.PropertyName))
            : null;

        return new PerPropertyView(
            PropertyIdentifier: propertyIdentifier,
            PropertyName: property.PropertyName,
            PropertyKindLabel: $"Per-property helpers for '{property.PropertyName}'.",
            IsOverrideKind: false,
            OverrideHasUsableBody: true,
            EntityFullName: entityFullName,
            BuildThreadsOptions: property.HasTypedValueOperator,
            ValidateThreadsOptions: property.HasTypedValueOperator,
            Build: build,
            Validate: validate,
            TypedLeafMethods: typedLeafMethods,
            Sort: sort);
    }

    internal static PerPropertyView BuildOverrideView(FilterClassModel model, PropertyOverrideModel overrideModel)
    {
        var entityFullName = "global::" + model.FullEntityTypeName;
        var propertyIdentifier = EmissionNames.PropertyIdentifier(overrideModel.PropertyName);
        var hasUsableBody = !string.IsNullOrEmpty(overrideModel.PropertyAccessorBodyCSharp)
            && overrideModel.Operators.Count > 0;

        if (!hasUsableBody)
        {
            var stubValidate = new PerPropertyValidateView(
                IsOverride: true,
                ScalarArm: null,
                ArrayArm: null,
                NoneOperators: [],
                CustomValidateArms: [],
                OverrideValidateArms: [.. overrideModel.Operators.Select(BuildOverrideValidateArm)],
                SimpleForwarder: null);

            return new PerPropertyView(
                PropertyIdentifier: propertyIdentifier,
                PropertyName: overrideModel.PropertyName,
                PropertyKindLabel: $"Per-property helpers for [PropertyMap]-overridden property '{overrideModel.PropertyName}'.",
                IsOverrideKind: true,
                OverrideHasUsableBody: false,
                EntityFullName: entityFullName,
                BuildThreadsOptions: overrideModel.HasTypedValueOperator,
                ValidateThreadsOptions: overrideModel.HasTypedValueOperator,
                Build: new PerPropertyBuildView(true, null, null, [], [], []),
                Validate: stubValidate,
                TypedLeafMethods: [],
                Sort: null);
        }


        var build = new PerPropertyBuildView(
            IsOverride: true,
            ScalarArm: null,
            ArrayArm: null,
            NoneOperators: [],
            CustomBuildArms: [],
            OverrideBuildArms: [.. overrideModel.Operators.Select(BuildOverrideBuildArm)]);

        var validate = new PerPropertyValidateView(
            IsOverride: true,
            ScalarArm: null,
            ArrayArm: null,
            NoneOperators: [],
            CustomValidateArms: [],
            OverrideValidateArms: [.. overrideModel.Operators.Select(BuildOverrideValidateArm)],
            SimpleForwarder: null);

        var entityVariable = "entity";
        var rewrittenAccessor = CustomOperatorEmitter.RewriteLambdaBody(
            overrideModel.PropertyAccessorBodyCSharp,
            overrideModel.EntityParameterName,
            entityVariable,
            valueParameterName: null,
            valueReplacement: null);

        var typedLeafMethods = overrideModel.Operators
            .Select(op => RenderOverrideTypedLeafMethod(entityFullName, entityVariable, rewrittenAccessor, op))
            .ToList();

        return new PerPropertyView(
            PropertyIdentifier: propertyIdentifier,
            PropertyName: overrideModel.PropertyName,
            PropertyKindLabel: $"Per-property helpers for [PropertyMap]-overridden property '{overrideModel.PropertyName}'.",
            IsOverrideKind: true,
            OverrideHasUsableBody: true,
            EntityFullName: entityFullName,
            BuildThreadsOptions: overrideModel.HasTypedValueOperator,
            ValidateThreadsOptions: overrideModel.HasTypedValueOperator,
            Build: build,
            Validate: validate,
            TypedLeafMethods: typedLeafMethods,
            Sort: null);
    }

    private static PerPropertyScalarArmView? BuildScalarArm(
        PropertyValueShape valueShape,
        InterceptorModel? interceptor,
        FilterClassModel model,
        IReadOnlyList<string> scalarOperators)
    {
        if (scalarOperators.Count == 0) return null;
        var extractCall = ProfileExtractorEmitter.EmitScalarCall(
            valueShape, "leaf.Value", outValueIdentifier: "rawLeafValue", outErrorIdentifier: "typeError");
        var hasInterceptor = interceptor is not null && !interceptor.Raw && interceptor.ValueClrType is not null;
        var qualifiedInterceptor = hasInterceptor ? QualifyEnclosingMember(model, interceptor!.MethodName) : null;
        return new PerPropertyScalarArmView(
            OperatorKeys: [.. scalarOperators.Select(o => o.ToUpperInvariant())],
            ExtractCall: extractCall,
            HasInterceptor: hasInterceptor,
            InterceptorMethodQualified: qualifiedInterceptor,
            Dispatch: [.. scalarOperators.Select(o =>
                new PerPropertyOperatorDispatchView(o.ToUpperInvariant(), EmissionNames.OperatorIdentifier(o)))]);
    }

    private static PerPropertyArrayArmView? BuildArrayArm(
        PropertyValueShape valueShape,
        IReadOnlyList<string> arrayOperators)
    {
        if (arrayOperators.Count == 0) return null;
        var extractCall = ProfileExtractorEmitter.EmitArrayCall(
            valueShape, "leaf.Value", outValuesIdentifier: "typedLeafValues", outErrorIdentifier: "arrayError");
        var argExpression = valueShape.IsNullableValueType
            ? $"(global::System.Linq.Enumerable.Select(typedLeafValues, leafValue => ({valueShape.LeafValueClrType}?)leafValue).ToArray())"
            : "(typedLeafValues)";
        return new PerPropertyArrayArmView(
            OperatorKeys: [.. arrayOperators.Select(o => o.ToUpperInvariant())],
            ExtractCall: extractCall,
            ArgExpression: argExpression,
            Dispatch: [.. arrayOperators.Select(o =>
                new PerPropertyOperatorDispatchView(o.ToUpperInvariant(), EmissionNames.OperatorIdentifier(o)))]);
    }

    private static PerPropertyCustomBuildArmView BuildCustomArm(CustomOperatorModel customOperator) =>
        new(
            OperatorKeyUpper: customOperator.OperatorName.ToUpperInvariant(),
            OperatorIdentifier: EmissionNames.OperatorIdentifier(customOperator.OperatorName),
            IsUnary: customOperator.ValueParameterName is null || customOperator.ValueClrType is null,
            ValueClrType: customOperator.ValueClrType,
            OperatorName: customOperator.OperatorName);

    private static PerPropertyOverrideBuildArmView BuildOverrideBuildArm(OverrideOperatorModel op) =>
        new(
            OperatorKeyUpper: op.Name.ToUpperInvariant(),
            OperatorIdentifier: EmissionNames.OperatorIdentifier(op.Name),
            IsUnary: op.ValueParameterName is null || op.ValueClrType is null,
            ValueClrType: op.ValueClrType,
            OperatorName: op.Name);

    private static PerPropertyOverrideValidateArmView BuildOverrideValidateArm(OverrideOperatorModel op) =>
        new(
            OperatorKeyUpper: op.Name.ToUpperInvariant(),
            IsUnary: op.ValueParameterName is null || op.ValueClrType is null,
            ValueClrType: op.ValueClrType);

    private static SimpleValidateForwarderView BuildSimpleValidateForwarder(
        PropertyValueShape valueShape,
        OperatorShapeGrouping grouping)
    {
        var profileFullName = $"global::{valueShape.ProfileFullName}";
        var hasScalar = grouping.ScalarOperators.Count > 0;
        var hasArray = grouping.ArrayOperators.Count > 0;
        return new SimpleValidateForwarderView(
            ScalarValueClrType: valueShape.LeafValueClrType,
            ScalarOpsArrayLiteral: RenderUppercaseOpsArrayLiteral(grouping.ScalarOperators),
            ArrayOpsArrayLiteral: RenderUppercaseOpsArrayLiteral(grouping.ArrayOperators),
            NoneOpsArrayLiteral: RenderUppercaseOpsArrayLiteral(grouping.NoneOperators),
            ScalarExtractorMethodGroup: hasScalar ? $"{profileFullName}.TryGetValue" : null,
            ArrayExtractorMethodGroup: hasArray ? $"{profileFullName}.TryGetArray" : null);
    }

    private static string RenderUppercaseOpsArrayLiteral(IReadOnlyList<string> operators)
    {
        if (operators.Count == 0) return "global::System.Array.Empty<string>()";
        var items = string.Join(", ", operators.Select(op => $"\"{op.ToUpperInvariant()}\""));
        return $"new[] {{ {items} }}";
    }

    private static PerPropertyScalarValidateArmView? BuildScalarValidateArm(
        PropertyMappingModel property,
        PropertyValueShape valueShape,
        InterceptorModel? interceptor,
        FilterClassModel model,
        IReadOnlyList<string> scalarOperators)
    {
        if (scalarOperators.Count == 0) return null;
        var extractCall = ProfileExtractorEmitter.EmitScalarCall(
            valueShape, "leaf.Value", outValueIdentifier: "typedValue", outErrorIdentifier: "typeError");
        var hasInterceptor = interceptor is not null && !interceptor.Raw && interceptor.ValueClrType is not null;
        var qualifiedInterceptor = hasInterceptor ? QualifyEnclosingMember(model, interceptor!.MethodName) : null;
        return new PerPropertyScalarValidateArmView(
            OperatorKeys: [.. scalarOperators.Select(o => o.ToUpperInvariant())],
            ExtractCall: extractCall,
            HasInterceptor: hasInterceptor,
            InterceptorMethodQualified: qualifiedInterceptor,
            PropertyName: property.PropertyName);
    }

    private static PerPropertyArrayValidateArmView? BuildArrayValidateArm(
        PropertyValueShape valueShape,
        IReadOnlyList<string> arrayOperators)
    {
        if (arrayOperators.Count == 0) return null;
        var extractCall = ProfileExtractorEmitter.EmitArrayCallDiscardingValue(
            valueShape, "leaf.Value", outErrorIdentifier: "arrayError");
        return new PerPropertyArrayValidateArmView(
            OperatorKeys: [.. arrayOperators.Select(o => o.ToUpperInvariant())],
            ExtractCall: extractCall);
    }

    private static PerPropertyCustomValidateArmView BuildCustomValidateArm(CustomOperatorModel customOperator) =>
        new(
            OperatorKeyUpper: customOperator.OperatorName.ToUpperInvariant(),
            IsUnary: customOperator.ValueParameterName is null || customOperator.ValueClrType is null,
            ValueClrType: customOperator.ValueClrType);

    private static string RenderMappedTypedLeafMethod(
        string entityFullName,
        PropertyMappingModel property,
        PropertyValueShape valueShape,
        string operatorName)
    {
        var operatorIdentifier = EmissionNames.OperatorIdentifier(operatorName);
        var entityVariable = "entity";
        var accessor = EmissionNames.PropertyAccessor(entityVariable, property.PropertyName);
        var shape = BuiltInProfileCatalog.ShapeOf(property.ProfileFullName, operatorName);
        var lambdaSignature = $"private static global::System.Linq.Expressions.Expression<global::System.Func<{entityFullName}, bool>> {operatorIdentifier}";
        var paramName = "leafValue";
        var arrayParamName = "leafValues";

        var isBuiltInProfile = BuiltInProfileCatalog.IsBuiltIn(property.ProfileFullName);
        if (!isBuiltInProfile)
        {
            CustomOperatorModel? customOperator = null;
            foreach (var candidate in property.CustomOperators)
            {
                if (candidate.OperatorName == operatorName) { customOperator = candidate; break; }
            }
            if (customOperator is not null)
            {
                return RenderCustomOperatorLeafMethod(entityVariable, accessor, lambdaSignature, customOperator);
            }
        }

        return operatorName switch
        {
            "eq" => $"{lambdaSignature}({valueShape.LeafValueClrType} {paramName}) => {entityVariable} => {accessor} == {paramName};",
            "ne" => $"{lambdaSignature}({valueShape.LeafValueClrType} {paramName}) => {entityVariable} => {accessor} != {paramName};",
            "gt" => $"{lambdaSignature}({valueShape.LeafValueClrType} {paramName}) => {entityVariable} => {accessor} > {paramName};",
            "gte" => $"{lambdaSignature}({valueShape.LeafValueClrType} {paramName}) => {entityVariable} => {accessor} >= {paramName};",
            "lt" => $"{lambdaSignature}({valueShape.LeafValueClrType} {paramName}) => {entityVariable} => {accessor} < {paramName};",
            "lte" => $"{lambdaSignature}({valueShape.LeafValueClrType} {paramName}) => {entityVariable} => {accessor} <= {paramName};",
            "contains" => $"{lambdaSignature}({valueShape.LeafValueClrType} {paramName}) => {entityVariable} => {accessor}.Contains({paramName});",
            "startsWith" => $"{lambdaSignature}({valueShape.LeafValueClrType} {paramName}) => {entityVariable} => {accessor}.StartsWith({paramName});",
            "endsWith" => $"{lambdaSignature}({valueShape.LeafValueClrType} {paramName}) => {entityVariable} => {accessor}.EndsWith({paramName});",
            "in" when valueShape.IsNullableValueType =>
                $"{lambdaSignature}({valueShape.LeafValueClrType}?[] {arrayParamName}) => {entityVariable} => {arrayParamName}.Contains({accessor});",
            "in" =>
                $"{lambdaSignature}({valueShape.LeafValueClrType}[] {arrayParamName}) => {entityVariable} => {arrayParamName}.Contains({accessor});",
            "isNull" => $"{lambdaSignature}() => {entityVariable} => {accessor} == null;",
            _ => RenderUnknownOperatorStub(lambdaSignature, valueShape, paramName, arrayParamName, operatorName, shape, property)
        };
    }

    private static string RenderUnknownOperatorStub(
        string lambdaSignature,
        PropertyValueShape valueShape,
        string paramName,
        string arrayParamName,
        string operatorName,
        OperatorShape shape,
        PropertyMappingModel property)
    {
        var comment = $"// Custom operator '{operatorName}' from profile {property.ProfileFullName} could not be extracted; emitting throwing stub.";
        var stub = shape switch
        {
            OperatorShape.Scalar =>
                $"{lambdaSignature}({valueShape.LeafValueClrType} {paramName}) => throw new global::Filtering.Net.FilterDispatchException(\"Custom operator '{operatorName}' has no extractable lambda body.\");",
            OperatorShape.Array =>
                $"{lambdaSignature}({valueShape.LeafValueClrType}[] {arrayParamName}) => throw new global::Filtering.Net.FilterDispatchException(\"Custom operator '{operatorName}' has no extractable lambda body.\");",
            _ =>
                $"{lambdaSignature}() => throw new global::Filtering.Net.FilterDispatchException(\"Custom operator '{operatorName}' has no extractable lambda body.\");"
        };
        return comment + "\n    " + stub;
    }

    private static string RenderCustomOperatorLeafMethod(
        string entityVariable,
        string accessor,
        string lambdaSignature,
        CustomOperatorModel customOperator)
    {
        var valueParameterName = "leafValue";
        var rewrittenBody = CustomOperatorEmitter.RewriteLambdaBody(
            customOperator.LambdaBodyCSharp,
            customOperator.ColumnParameterName,
            accessor,
            customOperator.ValueParameterName,
            customOperator.ValueParameterName is null ? null : valueParameterName);

        if (customOperator.ValueParameterName is null)
        {
            return $"{lambdaSignature}() => {entityVariable} => {rewrittenBody};";
        }
        var valueClrType = customOperator.ValueClrType ?? "object";
        return $"{lambdaSignature}({valueClrType} {valueParameterName}) => {entityVariable} => {rewrittenBody};";
    }

    private static string RenderOverrideTypedLeafMethod(
        string entityFullName,
        string entityVariable,
        string accessor,
        OverrideOperatorModel operatorModel)
    {
        var operatorIdentifier = EmissionNames.OperatorIdentifier(operatorModel.Name);
        var lambdaSignature = $"private static global::System.Linq.Expressions.Expression<global::System.Func<{entityFullName}, bool>> {operatorIdentifier}";
        var valueParameterName = "leafValue";
        var rewrittenBody = CustomOperatorEmitter.RewriteLambdaBody(
            operatorModel.PredicateBodyCSharp,
            operatorModel.ColumnParameterName,
            accessor,
            operatorModel.ValueParameterName,
            operatorModel.ValueParameterName is null ? null : valueParameterName);

        if (operatorModel.ValueParameterName is null || operatorModel.ValueClrType is null)
        {
            return $"{lambdaSignature}() => {entityVariable} => {rewrittenBody};";
        }
        return $"{lambdaSignature}({operatorModel.ValueClrType} {valueParameterName}) => {entityVariable} => {rewrittenBody};";
    }

    private static string QualifyEnclosingMember(FilterClassModel model, string memberName) =>
        string.IsNullOrEmpty(model.Namespace)
            ? $"global::{model.ClassName}.{memberName}"
            : $"global::{model.Namespace}.{model.ClassName}.{memberName}";
}
