using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Misaki.HighPerformance.HPC.Generator.IR;
using System;
using System.Collections.Generic;

namespace Misaki.HighPerformance.HPC.Generator.Analysis
{
    /// <summary>
    /// Walks a Roslyn <see cref="MethodDeclarationSyntax"/> and produces an
    /// <see cref="HPCMethodIR"/> that is completely independent of Roslyn after
    /// this phase completes.
    ///
    /// <para>Uses <see cref="CSharpSyntaxWalker"/> (read-only walk) rather than
    /// <see cref="CSharpSyntaxRewriter"/> so that the semantic model is never
    /// consulted during rewriting — all queries happen here, once, upfront.</para>
    /// </summary>
    internal sealed class HPCAnalyzer : CSharpSyntaxWalker
    {
        // ── State ─────────────────────────────────────────────────────────────

        private readonly HPCTypeResolver _typeResolver;

        /// <summary>Statement stack — top is the current block being built.</summary>
        private readonly Stack<List<HPCStmt>> _blocks = new();

        // ── Construction ─────────────────────────────────────────────────────

        public HPCAnalyzer(SemanticModel semanticModel)
        {
            _typeResolver = new HPCTypeResolver(semanticModel);
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Analyses <paramref name="method"/> and returns the completed IR.
        /// </summary>
        public HPCMethodIR Analyze(MethodDeclarationSyntax method, HPComputeMethodInfo info)
        {
            _typeResolver.RegisterConstraints(method);
            _blocks.Clear();

            // Push the root block
            _blocks.Push(new List<HPCStmt>());

            if (method.Body is not null)
                Visit(method.Body);
            else if (method.ExpressionBody is not null)
            {
                // Expression-bodied method: treat as `return <expr>;`
                var expr = AnalyzeExpression(method.ExpressionBody.Expression);
                var returnType = _typeResolver.Resolve(method.ExpressionBody.Expression)
                    ?? HPCType.Float;
                Emit(new HPCReturn(expr));
            }

            var body = _blocks.Pop().ToArray();

            return new HPCMethodIR
            {
                Name = method.Identifier.Text,
                OriginalName = method.Identifier.Text,
                ReturnType = ResolveReturnType(method),
                Parameters = BuildParameters(method),
                Body = body,
                TargetISA = info.InstructionSet,
                Precision = info.Precision,
                Mode = info.Mode,
                ContainingNamespace = info.MethodSymbol.ContainingNamespace.ToDisplayString(),
                ContainingTypeName = info.MethodSymbol.ContainingType.Name,
            };
        }

        // ── Statement visitors ───────────────────────────────────────────────

        public override void VisitBlock(BlockSyntax node)
        {
            // Visit each statement inside the block in order
            foreach (var stmt in node.Statements)
                Visit(stmt);
        }

        public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            foreach (var declarator in node.Declaration.Variables)
            {
                if (declarator.Initializer is null)
                    continue;

                var init = AnalyzeExpression(declarator.Initializer.Value);
                var hpcType = _typeResolver.Resolve(declarator.Initializer.Value)
                    ?? InferFromExpr(init);

                Emit(new HPCVarDecl(declarator.Identifier.Text, hpcType, init));
            }
        }

        public override void VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            if (node.Expression is AssignmentExpressionSyntax assign)
            {
                var target = assign.Left.ToString(); // simple name or member access text
                var value = AnalyzeExpression(assign.Right);
                Emit(new HPCAssignment(target, value));
            }
            else
            {
                var expr = AnalyzeExpression(node.Expression);
                Emit(new HPCExprStmt(expr));
            }
        }

        public override void VisitReturnStatement(ReturnStatementSyntax node)
        {
            var value = node.Expression is null ? null : AnalyzeExpression(node.Expression);
            Emit(new HPCReturn(value));
        }

        public override void VisitIfStatement(IfStatementSyntax node)
        {
            var condition = AnalyzeExpression(node.Condition);

            _blocks.Push(new List<HPCStmt>());
            Visit(node.Statement);
            var thenBody = _blocks.Pop().ToArray();

            HPCStmt[]? elseBody = null;
            if (node.Else is not null)
            {
                _blocks.Push(new List<HPCStmt>());
                Visit(node.Else.Statement);
                elseBody = _blocks.Pop().ToArray();
            }

            Emit(new HPCIf(condition, thenBody, elseBody));
        }

        public override void VisitForStatement(ForStatementSyntax node)
        {
            // Only handles the simple canonical for (var i = init; i <cond>; i++) form.
            if (node.Declaration?.Variables.Count != 1 ||
                node.Condition is null ||
                node.Incrementors.Count != 1)
            {
                // Fall back: treat body as pass-through
                _blocks.Push(new List<HPCStmt>());
                Visit(node.Statement);
                var rawBody = _blocks.Pop().ToArray();
                foreach (var s in rawBody)
                    Emit(s);
                return;
            }

            var decl = node.Declaration.Variables[0];
            var initExpr = decl.Initializer is not null
                ? AnalyzeExpression(decl.Initializer.Value)
                : new HPCLiteral("0", HPCType.Int);
            var iterDecl = new HPCVarDecl(decl.Identifier.Text, HPCType.Int, initExpr);
            var cond = AnalyzeExpression(node.Condition);
            var incr = AnalyzeExpression(node.Incrementors[0]);

            _blocks.Push(new List<HPCStmt>());
            Visit(node.Statement);
            var body = _blocks.Pop().ToArray();

            Emit(new HPCForLoop(iterDecl, cond, incr, body));
        }

        // ── Expression analysis ───────────────────────────────────────────────

        private HPCExpr AnalyzeExpression(ExpressionSyntax expr) => expr switch
        {
            BinaryExpressionSyntax n => AnalyzeBinary(n),
            PrefixUnaryExpressionSyntax n => AnalyzePrefixUnary(n),
            PostfixUnaryExpressionSyntax n => AnalyzePostfixUnary(n),
            InvocationExpressionSyntax n => AnalyzeInvocation(n),
            MemberAccessExpressionSyntax n => AnalyzeMemberAccess(n),
            LiteralExpressionSyntax n => AnalyzeLiteral(n),
            IdentifierNameSyntax n => AnalyzeIdentifier(n),
            ParenthesizedExpressionSyntax n => AnalyzeExpression(n.Expression),
            CastExpressionSyntax n => AnalyzeCast(n),
            _ => FallbackExpr(expr)
        };

        private HPCExpr AnalyzeBinary(BinaryExpressionSyntax node)
        {
            var left = AnalyzeExpression(node.Left);
            var right = AnalyzeExpression(node.Right);
            var hpcType = _typeResolver.Resolve(node) ?? InferFromExpr(left);

            var kind = node.Kind() switch
            {
                SyntaxKind.AddExpression => HPCBinaryKind.Add,
                SyntaxKind.SubtractExpression => HPCBinaryKind.Subtract,
                SyntaxKind.MultiplyExpression => HPCBinaryKind.Multiply,
                SyntaxKind.DivideExpression => HPCBinaryKind.Divide,
                SyntaxKind.ModuloExpression => HPCBinaryKind.Modulo,
                SyntaxKind.BitwiseAndExpression => HPCBinaryKind.BitwiseAnd,
                SyntaxKind.BitwiseOrExpression => HPCBinaryKind.BitwiseOr,
                SyntaxKind.ExclusiveOrExpression => HPCBinaryKind.BitwiseXor,
                SyntaxKind.LeftShiftExpression => HPCBinaryKind.ShiftLeft,
                SyntaxKind.RightShiftExpression => HPCBinaryKind.ShiftRight,
                SyntaxKind.EqualsExpression => HPCBinaryKind.Equal,
                SyntaxKind.NotEqualsExpression => HPCBinaryKind.NotEqual,
                SyntaxKind.LessThanExpression => HPCBinaryKind.LessThan,
                SyntaxKind.LessThanOrEqualExpression => HPCBinaryKind.LessThanOrEqual,
                SyntaxKind.GreaterThanExpression => HPCBinaryKind.GreaterThan,
                SyntaxKind.GreaterThanOrEqualExpression => HPCBinaryKind.GreaterThanOrEqual,
                _ => throw new NotSupportedException($"Binary operator not supported: {node.Kind()}")
            };

            return new HPCBinaryOp(kind, left, right, hpcType);
        }

        private HPCExpr AnalyzePrefixUnary(PrefixUnaryExpressionSyntax node)
        {
            var operand = AnalyzeExpression(node.Operand);
            var hpcType = _typeResolver.Resolve(node) ?? InferFromExpr(operand);

            var kind = node.Kind() switch
            {
                SyntaxKind.UnaryMinusExpression => HPCUnaryKind.Negate,
                SyntaxKind.BitwiseNotExpression => HPCUnaryKind.BitwiseNot,
                _ => throw new NotSupportedException($"Prefix unary not supported: {node.Kind()}")
            };

            return new HPCUnaryOp(kind, operand, hpcType);
        }

        private HPCExpr AnalyzePostfixUnary(PostfixUnaryExpressionSyntax node)
        {
            // i++ / i-- — treat as i + 1 / i - 1 in an IR context (no mutation semantics)
            var operand = AnalyzeExpression(node.Operand);
            var hpcType = InferFromExpr(operand);
            var one = new HPCLiteral("1", hpcType);

            return node.Kind() == SyntaxKind.PostIncrementExpression
                ? new HPCBinaryOp(HPCBinaryKind.Add, operand, one, hpcType)
                : new HPCBinaryOp(HPCBinaryKind.Subtract, operand, one, hpcType);
        }

        private HPCExpr AnalyzeInvocation(InvocationExpressionSyntax node)
        {
            // ── Instance method on SPMD lane: lane.Add(b), lane.MultiplyAdd(a,b,c) ──
            if (node.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var receiverType = _typeResolver.Resolve(memberAccess.Expression);
                var methodName = memberAccess.Name.Identifier.Text;

                if (receiverType != null)
                {
                    // Unary math on the receiver: lane.Abs(), lane.Sqrt(), etc.
                    if (TryMapUnaryMethod(methodName, out var unaryKind) &&
                        node.ArgumentList.Arguments.Count == 0)
                    {
                        var receiver = AnalyzeExpression(memberAccess.Expression);
                        return new HPCUnaryOp(unaryKind, receiver, receiverType);
                    }

                    // Multi-arg static-style methods called as instance methods via ISPMDLane
                    if (TryMapIntrinsicMethod(methodName, out var intrinsic))
                    {
                        var args = new List<HPCExpr> { AnalyzeExpression(memberAccess.Expression) };
                        foreach (var arg in node.ArgumentList.Arguments)
                            args.Add(AnalyzeExpression(arg.Expression));
                        return new HPCIntrinsicCall(intrinsic, args.ToArray(), receiverType);
                    }
                }

                // Static call on the lane type: TSelf.Sin(v), TSelf.Load(ref p), etc.
                if (memberAccess.Expression is IdentifierNameSyntax typeIdent &&
                    _typeResolver.IsSPMDTypeParam(typeIdent.Identifier.Text))
                {
                    var elemType = _typeResolver.ResolveByName(typeIdent.Identifier.Text) ?? HPCType.Float;

                    if (TryMapUnaryMethod(methodName, out var unaryKind) &&
                        node.ArgumentList.Arguments.Count == 1)
                    {
                        var arg = AnalyzeExpression(node.ArgumentList.Arguments[0].Expression);
                        return new HPCUnaryOp(unaryKind, arg, elemType);
                    }

                    if (TryMapIntrinsicMethod(methodName, out var intrinsic))
                    {
                        var args = new List<HPCExpr>();
                        foreach (var arg in node.ArgumentList.Arguments)
                            args.Add(AnalyzeExpression(arg.Expression));
                        return new HPCIntrinsicCall(intrinsic, args.ToArray(), elemType);
                    }
                }
            }

            // ── Fall through: unknown call, emit as pass-through ──────────────
            return FallbackExpr(node);
        }

        private HPCExpr AnalyzeMemberAccess(MemberAccessExpressionSyntax node)
        {
            var receiverType = _typeResolver.Resolve(node.Expression);
            var memberName = node.Name.Identifier.Text;

            if (receiverType != null)
            {
                // LaneWidth → Count (property remap)
                var mapped = memberName switch
                {
                    "LaneWidth" => "Count",
                    _ => memberName
                };
                var receiver = AnalyzeExpression(node.Expression);
                return new HPCPropertyAccess(receiver, mapped, receiverType);
            }

            return FallbackExpr(node);
        }

        private static HPCExpr AnalyzeLiteral(LiteralExpressionSyntax node)
        {
            var text = node.Token.ValueText;
            var hpcType = node.Kind() switch
            {
                SyntaxKind.NumericLiteralExpression when node.Token.Value is float => HPCType.Float,
                SyntaxKind.NumericLiteralExpression when node.Token.Value is double => HPCType.Double,
                SyntaxKind.NumericLiteralExpression when node.Token.Value is int => HPCType.Int,
                SyntaxKind.NumericLiteralExpression when node.Token.Value is long => HPCType.Long,
                _ => HPCType.Float
            };
            return new HPCLiteral(text, hpcType);
        }

        private HPCExpr AnalyzeIdentifier(IdentifierNameSyntax node)
        {
            var name = node.Identifier.Text;
            var hpcType = _typeResolver.Resolve(node)
                ?? _typeResolver.ResolveByName(name)
                ?? HPCType.Float;
            return new HPCVarRef(name, hpcType);
        }

        private HPCExpr AnalyzeCast(CastExpressionSyntax node)
        {
            var operand = AnalyzeExpression(node.Expression);
            var hpcType = _typeResolver.Resolve(node) ?? HPCType.Float;
            return new HPCIntrinsicCall(HPCIntrinsic.Cast, [operand], hpcType);
        }

        private HPCExpr FallbackExpr(ExpressionSyntax node)
        {
            // Preserve anything we don't understand as a verbatim pass-through
            var hpcType = _typeResolver.Resolve(node) ?? HPCType.Float;
            return new HPCPassThroughCall(
                node.ToString(),
                Array.Empty<HPCExpr>(),
                hpcType);
        }

        // ── Method mapping tables ─────────────────────────────────────────────

        private static readonly Dictionary<string, HPCUnaryKind> s_unaryMethods = new()
        {
            ["Abs"] = HPCUnaryKind.Abs,
            ["Sqrt"] = HPCUnaryKind.Sqrt,
            ["Floor"] = HPCUnaryKind.Floor,
            ["Ceil"] = HPCUnaryKind.Ceil,
            ["Round"] = HPCUnaryKind.Round,
            ["Trunc"] = HPCUnaryKind.Trunc,
            ["Frac"] = HPCUnaryKind.Frac,
            ["Sign"] = HPCUnaryKind.Sign,
            ["Saturate"] = HPCUnaryKind.Saturate,
            ["Rcp"] = HPCUnaryKind.Rcp,
            ["Rsqrt"] = HPCUnaryKind.Rsqrt,
            ["Sin"] = HPCUnaryKind.Sin,
            ["Cos"] = HPCUnaryKind.Cos,
            ["Tan"] = HPCUnaryKind.Tan,
            ["Asin"] = HPCUnaryKind.Asin,
            ["Acos"] = HPCUnaryKind.Acos,
            ["Atan"] = HPCUnaryKind.Atan,
            ["Exp"] = HPCUnaryKind.Exp,
            ["Exp2"] = HPCUnaryKind.Exp2,
            ["Log"] = HPCUnaryKind.Log,
            ["Log2"] = HPCUnaryKind.Log2,
        };

        private static readonly Dictionary<string, HPCIntrinsic> s_intrinsicMethods = new()
        {
            ["MultiplyAdd"] = HPCIntrinsic.MultiplyAdd,
            ["MultiplySubtract"] = HPCIntrinsic.MultiplySubtract,
            ["Atan2"] = HPCIntrinsic.Atan2,
            ["Pow"] = HPCIntrinsic.Pow,
            ["SinCos"] = HPCIntrinsic.SinCos,
            ["Lerp"] = HPCIntrinsic.Lerp,
            ["Min"] = HPCIntrinsic.Min,
            ["Max"] = HPCIntrinsic.Max,
            ["Clamp"] = HPCIntrinsic.Clamp,
            ["Select"] = HPCIntrinsic.Select,
            ["CopySign"] = HPCIntrinsic.CopySign,
            ["ReduceAdd"] = HPCIntrinsic.ReduceAdd,
            ["ReduceMax"] = HPCIntrinsic.ReduceMax,
            ["ReduceMin"] = HPCIntrinsic.ReduceMin,
            ["Load"] = HPCIntrinsic.Load,
            ["MaskLoad"] = HPCIntrinsic.MaskLoad,
            ["Gather"] = HPCIntrinsic.Gather,
            ["MaskGather"] = HPCIntrinsic.MaskGather,
            ["Store"] = HPCIntrinsic.Store,
            ["MaskStore"] = HPCIntrinsic.MaskStore,
            ["Scatter"] = HPCIntrinsic.Scatter,
            ["MaskScatter"] = HPCIntrinsic.MaskScatter,
            ["CompressStore"] = HPCIntrinsic.CompressStore,
        };

        private static bool TryMapUnaryMethod(string name, out HPCUnaryKind kind)
            => s_unaryMethods.TryGetValue(name, out kind);

        private static bool TryMapIntrinsicMethod(string name, out HPCIntrinsic intrinsic)
            => s_intrinsicMethods.TryGetValue(name, out intrinsic);

        // ── Helpers ───────────────────────────────────────────────────────────

        private void Emit(HPCStmt stmt) => _blocks.Peek().Add(stmt);

        private static HPCType InferFromExpr(HPCExpr expr) => expr.Type;

        private HPCType ResolveReturnType(MethodDeclarationSyntax method)
        {
            var typeText = method.ReturnType.ToString();
            if (typeText == "void")
                return new HPCType("void", IsFloatingPoint: false);
            return _typeResolver.ResolveByName(typeText)
                ?? HPCType.FromElementName(typeText);
        }

        private HPCParameter[] BuildParameters(MethodDeclarationSyntax method)
        {
            var result = new List<HPCParameter>();
            foreach (var param in method.ParameterList.Parameters)
            {
                // Skip generic SPMD type parameters (they become the vector type in the backend)
                var paramTypeName = param.Type?.ToString() ?? "object";
                if (_typeResolver.IsSPMDTypeParam(paramTypeName))
                    continue;

                var hpcType = _typeResolver.ResolveByName(paramTypeName)
                    ?? HPCType.FromElementName(paramTypeName);

                bool isOut = false, isRef = false;
                foreach (var mod in param.Modifiers)
                {
                    if (mod.IsKind(SyntaxKind.OutKeyword))
                        isOut = true;
                    if (mod.IsKind(SyntaxKind.RefKeyword))
                        isRef = true;
                }

                result.Add(new HPCParameter(param.Identifier.Text, hpcType, isOut, isRef));
            }
            return result.ToArray();
        }
    }
}
