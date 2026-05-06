using Misaki.HighPerformance.HPC.Generator.IR;
using System.Linq;

namespace Misaki.HighPerformance.HPC.Generator.IR
{
    /// <summary>
    /// Base class for passes that transform the IR tree.
    /// Uses the <em>immutable rewrite</em> pattern: each Visit method returns
    /// either the original node (if nothing changed) or a new <c>with</c>-expression
    /// copy, leaving the input tree untouched.
    /// </summary>
    internal abstract class HPCNodeRewriter
    {
        // ── Entry points ─────────────────────────────────────────────────────

        public virtual HPCExpr RewriteExpr(HPCExpr expr) => expr switch
        {
            HPCBinaryOp n       => RewriteBinaryOp(n),
            HPCUnaryOp n        => RewriteUnaryOp(n),
            HPCIntrinsicCall n  => RewriteIntrinsicCall(n),
            HPCPassThroughCall n => RewritePassThroughCall(n),
            HPCPropertyAccess n => RewritePropertyAccess(n),
            HPCVarRef n         => RewriteVarRef(n),
            HPCLiteral n        => RewriteLiteral(n),
            _ => expr
        };

        public virtual HPCStmt RewriteStmt(HPCStmt stmt) => stmt switch
        {
            HPCVarDecl n    => RewriteVarDecl(n),
            HPCAssignment n => RewriteAssignment(n),
            HPCExprStmt n   => RewriteExprStmt(n),
            HPCReturn n     => RewriteReturn(n),
            HPCIf n         => RewriteIf(n),
            HPCForLoop n    => RewriteForLoop(n),
            _ => stmt
        };

        // ── Expression rewrites (override to modify specific patterns) ───────

        protected virtual HPCExpr RewriteBinaryOp(HPCBinaryOp node)
        {
            var left  = RewriteExpr(node.Left);
            var right = RewriteExpr(node.Right);
            return ReferenceEquals(left, node.Left) && ReferenceEquals(right, node.Right)
                ? node
                : node with { Left = left, Right = right };
        }

        protected virtual HPCExpr RewriteUnaryOp(HPCUnaryOp node)
        {
            var operand = RewriteExpr(node.Operand);
            return ReferenceEquals(operand, node.Operand)
                ? node
                : node with { Operand = operand };
        }

        protected virtual HPCExpr RewriteIntrinsicCall(HPCIntrinsicCall node)
        {
            var args = RewriteArgs(node.Args);
            return ReferenceEquals(args, node.Args)
                ? node
                : node with { Args = args };
        }

        protected virtual HPCExpr RewritePassThroughCall(HPCPassThroughCall node)
        {
            var args = RewriteArgs(node.Args);
            return ReferenceEquals(args, node.Args)
                ? node
                : node with { Args = args };
        }

        protected virtual HPCExpr RewritePropertyAccess(HPCPropertyAccess node)
        {
            var target = RewriteExpr(node.Target);
            return ReferenceEquals(target, node.Target)
                ? node
                : node with { Target = target };
        }

        protected virtual HPCExpr RewriteVarRef(HPCVarRef node) => node;

        protected virtual HPCExpr RewriteLiteral(HPCLiteral node) => node;

        // ── Statement rewrites ───────────────────────────────────────────────

        protected virtual HPCStmt RewriteVarDecl(HPCVarDecl node)
        {
            var init = RewriteExpr(node.Initializer);
            return ReferenceEquals(init, node.Initializer)
                ? node
                : node with { Initializer = init };
        }

        protected virtual HPCStmt RewriteAssignment(HPCAssignment node)
        {
            var value = RewriteExpr(node.Value);
            return ReferenceEquals(value, node.Value)
                ? node
                : node with { Value = value };
        }

        protected virtual HPCStmt RewriteExprStmt(HPCExprStmt node)
        {
            var expr = RewriteExpr(node.Expression);
            return ReferenceEquals(expr, node.Expression)
                ? node
                : node with { Expression = expr };
        }

        protected virtual HPCStmt RewriteReturn(HPCReturn node)
        {
            if (node.Value is null) return node;
            var value = RewriteExpr(node.Value);
            return ReferenceEquals(value, node.Value)
                ? node
                : node with { Value = value };
        }

        protected virtual HPCStmt RewriteIf(HPCIf node)
        {
            var cond     = RewriteExpr(node.Condition);
            var thenBody = RewriteBody(node.ThenBody);
            var elseBody = node.ElseBody is null ? null : RewriteBody(node.ElseBody);
            return ReferenceEquals(cond, node.Condition) &&
                   ReferenceEquals(thenBody, node.ThenBody) &&
                   ReferenceEquals(elseBody, node.ElseBody)
                ? node
                : node with { Condition = cond, ThenBody = thenBody, ElseBody = elseBody };
        }

        protected virtual HPCStmt RewriteForLoop(HPCForLoop node)
        {
            var iter = (HPCVarDecl)RewriteVarDecl(node.Iterator);
            var cond = RewriteExpr(node.Condition);
            var incr = RewriteExpr(node.Increment);
            var body = RewriteBody(node.Body);
            return ReferenceEquals(iter, node.Iterator) &&
                   ReferenceEquals(cond, node.Condition) &&
                   ReferenceEquals(incr, node.Increment) &&
                   ReferenceEquals(body, node.Body)
                ? node
                : node with { Iterator = iter, Condition = cond, Increment = incr, Body = body };
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        protected HPCStmt[] RewriteBody(HPCStmt[] body)
        {
            HPCStmt[]? result = null;
            for (int i = 0; i < body.Length; i++)
            {
                var original = body[i];
                var rewritten = RewriteStmt(original);
                if (!ReferenceEquals(original, rewritten))
                {
                    result ??= body.ToArray();
                    result[i] = rewritten;
                }
            }
            return result ?? body;
        }

        private HPCExpr[] RewriteArgs(HPCExpr[] args)
        {
            HPCExpr[]? result = null;
            for (int i = 0; i < args.Length; i++)
            {
                var original  = args[i];
                var rewritten = RewriteExpr(original);
                if (!ReferenceEquals(original, rewritten))
                {
                    result ??= args.ToArray();
                    result[i] = rewritten;
                }
            }
            return result ?? args;
        }
    }
}
