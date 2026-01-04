using NUnit.Framework;

namespace Audio.Tests.Editor
{
    /// <summary>
    /// Unit tests for VolumeCalculator.
    /// </summary>
    [TestFixture]
    public class VolumeCalculatorTests
    {
        [Test]
        public void Calculate_AllMaxValues_ReturnsOne()
        {
            float result = VolumeCalculator.Calculate(
                clipVolume: 1f,
                layerVolume: 1f,
                masterVolume: 1f,
                isMuted: false,
                duckingMultiplier: 1f
            );

            Assert.AreEqual(1f, result, 0.0001f);
        }

        [Test]
        public void Calculate_AllZeroValues_ReturnsZero()
        {
            float result = VolumeCalculator.Calculate(
                clipVolume: 0f,
                layerVolume: 0f,
                masterVolume: 0f,
                isMuted: false,
                duckingMultiplier: 0f
            );

            Assert.AreEqual(0f, result);
        }

        [Test]
        public void Calculate_HalfValues_ReturnsCorrectProduct()
        {
            // 0.5 * 0.5 * 0.5 * 1.0 = 0.125
            float result = VolumeCalculator.Calculate(
                clipVolume: 0.5f,
                layerVolume: 0.5f,
                masterVolume: 0.5f,
                isMuted: false,
                duckingMultiplier: 1f
            );

            Assert.AreEqual(0.125f, result, 0.0001f);
        }

        [Test]
        public void Calculate_WithDucking_ReducesVolume()
        {
            float withoutDucking = VolumeCalculator.Calculate(1f, 1f, 1f, false, 1f);
            float withDucking = VolumeCalculator.Calculate(1f, 1f, 1f, false, 0.3f);

            Assert.Less(withDucking, withoutDucking);
            Assert.AreEqual(0.3f, withDucking, 0.0001f);
        }

        [Test]
        public void Calculate_WhenMuted_ReturnsZero()
        {
            float result = VolumeCalculator.Calculate(
                clipVolume: 1f,
                layerVolume: 1f,
                masterVolume: 1f,
                isMuted: true,
                duckingMultiplier: 1f
            );

            Assert.AreEqual(0f, result);
        }

        [Test]
        public void Calculate_NegativeClipVolume_ClampsToZero()
        {
            float result = VolumeCalculator.Calculate(
                clipVolume: -0.5f,
                layerVolume: 1f,
                masterVolume: 1f,
                isMuted: false,
                duckingMultiplier: 1f
            );

            Assert.GreaterOrEqual(result, 0f);
        }

        [Test]
        public void Calculate_OverOneClipVolume_ClampsToOne()
        {
            float result = VolumeCalculator.Calculate(
                clipVolume: 2f,
                layerVolume: 1f,
                masterVolume: 1f,
                isMuted: false,
                duckingMultiplier: 1f
            );

            Assert.LessOrEqual(result, 1f);
        }

        [Test]
        public void Calculate_ZeroMaster_ReturnsZero()
        {
            float result = VolumeCalculator.Calculate(
                clipVolume: 1f,
                layerVolume: 1f,
                masterVolume: 0f,
                isMuted: false,
                duckingMultiplier: 1f
            );

            Assert.AreEqual(0f, result);
        }

        [Test]
        public void Calculate_ZeroLayer_ReturnsZero()
        {
            float result = VolumeCalculator.Calculate(
                clipVolume: 1f,
                layerVolume: 0f,
                masterVolume: 1f,
                isMuted: false,
                duckingMultiplier: 1f
            );

            Assert.AreEqual(0f, result);
        }

        [Test]
        public void Calculate_RealisticScenario_ReturnsExpectedValue()
        {
            // Typical game scenario:
            // Clip at 80%, layer at 70%, master at 100%, no ducking
            // 0.8 * 0.7 * 1.0 * 1.0 = 0.56
            float result = VolumeCalculator.Calculate(
                clipVolume: 0.8f,
                layerVolume: 0.7f,
                masterVolume: 1f,
                isMuted: false,
                duckingMultiplier: 1f
            );

            Assert.AreEqual(0.56f, result, 0.0001f);
        }

        [Test]
        public void Calculate_DialogueDuckingScenario_ReturnsExpectedValue()
        {
            // Music during dialogue:
            // Clip at 100%, layer at 80%, master at 100%, ducking at 30%
            // 1.0 * 0.8 * 1.0 * 0.3 = 0.24
            float result = VolumeCalculator.Calculate(
                clipVolume: 1f,
                layerVolume: 0.8f,
                masterVolume: 1f,
                isMuted: false,
                duckingMultiplier: 0.3f
            );

            Assert.AreEqual(0.24f, result, 0.0001f);
        }

        [Test]
        public void GetDuckingMultiplier_NoDialogue_ReturnsOne()
        {
            float result = VolumeCalculator.GetDuckingMultiplier(0, 0.3f);
            Assert.AreEqual(1f, result);
        }

        [Test]
        public void GetDuckingMultiplier_WithDialogue_ReturnsDuckingAmount()
        {
            float result = VolumeCalculator.GetDuckingMultiplier(1, 0.3f);
            Assert.AreEqual(0.3f, result);
        }
    }
}
