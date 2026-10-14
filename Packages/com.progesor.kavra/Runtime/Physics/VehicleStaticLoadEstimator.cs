using System.Collections.Generic;
using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// Nominal flat-ground wheel load fractions from static vertical equilibrium.
    /// Minimize deviation from equal shares while approximating two moment balances.
    /// No fixed four-wheel layout is assumed. This is NOT a dynamic load solver.
    /// </summary>
    public static class VehicleStaticLoadEstimator
    {
        /// <summary>
        /// Writes non-negative, sum-to-one wheel load weights. Returns false if
        /// the requested CoM projection cannot be supported by the footprint
        /// (for example the CoM is outside the wheel contact polygon).
        /// </summary>
        public static bool Estimate(IReadOnlyList<WheelDefinition> wheels,
            Vector3 centerOfMassLocal, float[] outputWeights)
        {
            if (wheels == null || wheels.Count == 0 || outputWeights == null ||
                outputWeights.Length < wheels.Count)
                return false;

            int count = wheels.Count;
            bool[] active = new bool[count]; // Awake/vehicle-init only.
            for (int i = 0; i < count; i++)
            {
                if (wheels[i] == null) return false;
                active[i] = true;
                outputWeights[i] = 0f;
            }

            for (int iteration = 0; iteration < count; iteration++)
            {
                int remaining = 0;
                double meanX = 0d, meanZ = 0d;
                for (int i = 0; i < count; i++)
                {
                    if (!active[i]) continue;
                    remaining++;
                    meanX += wheels[i].MountLocalPosition.x;
                    meanZ += wheels[i].MountLocalPosition.z;
                }
                if (remaining <= 0)
                    break;

                meanX /= remaining;
                meanZ /= remaining;
                double xx = 0d, zz = 0d, xz = 0d;
                for (int i = 0; i < count; i++)
                {
                    if (!active[i]) continue;
                    double x = wheels[i].MountLocalPosition.x - meanX;
                    double z = wheels[i].MountLocalPosition.z - meanZ;
                    xx += x * x;
                    zz += z * z;
                    xz += x * z;
                }

                double residualX = centerOfMassLocal.x - meanX;
                double residualZ = centerOfMassLocal.z - meanZ;
                double lambdaX = 0d, lambdaZ = 0d;
                double determinant = xx * zz - xz * xz;
                if (determinant > 1e-9d * System.Math.Max(1d, xx * zz))
                {
                    lambdaX = (residualX * zz - residualZ * xz) / determinant;
                    lambdaZ = (residualZ * xx - residualX * xz) / determinant;
                }
                else if (xx > zz && xx > 1e-9d)
                {
                    lambdaX = residualX / xx;
                }
                else if (zz > 1e-9d)
                {
                    lambdaZ = residualZ / zz;
                }

                float smallest = -1e-6f;
                int remove = -1;
                for (int i = 0; i < count; i++)
                {
                    if (!active[i])
                    {
                        outputWeights[i] = 0f;
                        continue;
                    }

                    double localX = wheels[i].MountLocalPosition.x - meanX;
                    double localZ = wheels[i].MountLocalPosition.z - meanZ;
                    float weight = (float)(1d / remaining +
                        lambdaX * localX + lambdaZ * localZ);
                    outputWeights[i] = weight;
                    if (weight < smallest)
                    {
                        smallest = weight;
                        remove = i;
                    }
                }

                if (remove >= 0)
                {
                    active[remove] = false;
                    outputWeights[remove] = 0f;
                    continue;
                }

                float total = 0f;
                for (int i = 0; i < count; i++)
                {
                    outputWeights[i] = Mathf.Max(0f, outputWeights[i]);
                    total += outputWeights[i];
                }

                if (total <= 0f) break;
                float achievedX = 0f, achievedZ = 0f;
                for (int i = 0; i < count; i++)
                {
                    outputWeights[i] /= total;
                    achievedX += outputWeights[i] * wheels[i].MountLocalPosition.x;
                    achievedZ += outputWeights[i] * wheels[i].MountLocalPosition.z;
                }
                return Mathf.Abs(achievedX - centerOfMassLocal.x) <= 0.025f &&
                       Mathf.Abs(achievedZ - centerOfMassLocal.z) <= 0.025f;
            }

            // Degenerate layout: no division by zero, but flag that balance
            // is not a physically valid solution for the requested CoM.
            float equal = 1f / count;
            for (int i = 0; i < count; i++) outputWeights[i] = equal;
            return false;
        }
    }
}
