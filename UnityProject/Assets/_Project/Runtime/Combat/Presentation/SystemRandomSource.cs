using System;
using EFTM.Combat.Foundation;

namespace EFTM.Combat.Presentation
{
    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random random;

        public SystemRandomSource(int seed)
        {
            random = new Random(seed);
        }

        public float Next01()
        {
            return (float)random.NextDouble();
        }
    }
}
