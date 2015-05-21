﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SharpLearning.Optimization
{
    /// <summary>
    /// Simple grid search that tries all combinations of the provided parameters
    /// </summary>
    public sealed class GridSearchOptimizer
    {
        readonly int m_maxDegreeOfParallelism;
        readonly double[][] m_parameters;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameterRanges">Each row is a series of values for a specific parameter</param>
        public GridSearchOptimizer(double[][] parameterRanges)
            : this(parameterRanges, int.MaxValue)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameterRanges">Each row is a series of values for a specific parameter</param>
        /// <param name="maxDegreeOfParallelism">How many cores must be used for the optimization. 
        /// The function to minimize must be thread safe to use multi threading</param>
        public GridSearchOptimizer(double[][] parameterRanges, int maxDegreeOfParallelism)
        {
            if (parameterRanges == null) { throw new ArgumentNullException("parameterRanges"); }
            if (maxDegreeOfParallelism < 1) { throw new ArgumentException("maxDegreeOfParallelism must be at least 1"); }
            m_parameters = parameterRanges;
            m_maxDegreeOfParallelism = maxDegreeOfParallelism;
        }

        public OptimizerResult Optimize(Func<double[], OptimizerResult> functionToMinimize)
        {
            // Generate the cartesian product between all parameters
            double[][] grid = CartesianProduct(m_parameters);

            // Initialize the search
            var results = new ConcurrentBag<OptimizerResult>();
            var options = new ParallelOptions();
            options.MaxDegreeOfParallelism = m_maxDegreeOfParallelism;

            Parallel.ForEach(grid, options, param =>
            {
                // Get the current parameters for the current point
                var result = functionToMinimize(param);
                //Trace.WriteLine("Error: " + result.Error);//
                results.Add(result);
            });

            // Return the best model found.
            return results.OrderBy(r => r.Error).First();
        }

        static T[][] CartesianProduct<T>(T[][] sequences)
        {
            var cartesian = CartesianProductEnumerable(sequences);
            return cartesian.Select(row => row.ToArray()).ToArray();
        }

        static IEnumerable<IEnumerable<T>> CartesianProductEnumerable<T>(IEnumerable<IEnumerable<T>> sequences)
        {
            IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };
            return sequences.Aggregate(
                emptyProduct,
                (accumulator, sequence) =>
                    from accseq in accumulator
                    from item in sequence
                    select accseq.Concat(new[] { item }));
        }
    }

}