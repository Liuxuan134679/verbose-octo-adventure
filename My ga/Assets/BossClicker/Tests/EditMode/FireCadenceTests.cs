using NUnit.Framework;

namespace BossClicker.Tests
{
    public sealed class FireCadenceTests
    {
        [Test]
        public void PressFiresImmediatelyAndHoldUsesTheConfiguredInterval()
        {
            var cadence = new FireCadence();

            Assert.AreEqual(1, cadence.Press(10, .5));
            Assert.AreEqual(0, cadence.Tick(10.49, .5));
            Assert.AreEqual(1, cadence.Tick(10.5, .5));
            Assert.AreEqual(2, cadence.Tick(11.6, .5), "Low frame rate should catch up due shots.");
        }

        [Test]
        public void RepeatedPressCannotBypassTheCurrentCooldown()
        {
            var cadence = new FireCadence();

            Assert.AreEqual(1, cadence.Press(2, 1));
            cadence.Release();
            Assert.AreEqual(0, cadence.Press(2.2, 1));
            Assert.AreEqual(0, cadence.Tick(2.99, 1));
            Assert.AreEqual(1, cadence.Tick(3, 1));
        }

        [Test]
        public void ReleaseAndResetStopAutomaticShots()
        {
            var cadence = new FireCadence();
            cadence.Press(0, .5);
            cadence.Release();

            Assert.AreEqual(0, cadence.Tick(2, .5));
            cadence.Reset();
            Assert.AreEqual(1, cadence.Press(2, .5));
        }
    }
}
