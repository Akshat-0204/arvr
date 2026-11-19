using NUnit.Framework;

namespace VoxelAR.Tests
{
    public class SmokeTests
    {
        [Test]
        public void RuntimeAssemblyLoads()
        {
            Assert.IsNotNull(typeof(VoxelAR.Core.SceneBootstrap));
        }
    }
}
