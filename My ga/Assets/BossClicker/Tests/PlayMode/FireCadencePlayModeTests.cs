using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace BossClicker.Tests
{
    public sealed class FireCadencePlayModeTests
    {
        [UnityTest]
        public IEnumerator HoldCatchesUpAndReleaseStopsShotsInThePlayerLoop()
        {
            var cadence = new FireCadence();

            Assert.AreEqual(1, cadence.Press(5, .5));
            yield return null;
            Assert.AreEqual(2, cadence.Tick(6.1, .5));

            cadence.Release();
            yield return null;
            Assert.AreEqual(0, cadence.Tick(20, .5));
        }
    }
}
