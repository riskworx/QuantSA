﻿using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Optimization;
using MathNet.Numerics.RootFinding;
using QuantSA.Core.Optimization;

namespace QuantSA.Core.RootFinding
{
    /// <summary>
    /// Wrap the mathnet Broyden method behind the <see cref="IVectorRootFinder"/> interface.
    /// </summary>
    public class BroydenWrapper : IVectorRootFinder
    {
        /// <summary>
        /// Find the root given an objective function and given initial guess.
        /// </summary>
        /// <param name="objective">The objective function to minimize</param>
        /// <param name="initialGuess">The initial guess</param>
        /// <returns></returns>
        public VectorMinimizationResult FindRoot(IObjectiveVectorFunction objective, Vector<double> initialGuess)
        {
            var function = new FunctionEvaluator(objective);
            var root = Broyden.FindRoot(function.Eval, initialGuess.ToArray(), 1e-8, 100, 1e-8);
            var result = new VectorMinimizationResult
            {
                FunctionInfoAtMinimum = objective,
                Iterations = -1,
                MinimizingPoint = new DenseVector(root),
                ReasonForExit = ExitCondition.Converged
            };
            return result;
        }
    }
}