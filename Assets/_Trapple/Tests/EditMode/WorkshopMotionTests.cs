using NUnit.Framework;
using Trapple.Workshop;

namespace Trapple.Tests
{
    public sealed class WorkshopMotionTests
    {
        [Test]
        public void EveryUpgradeLevelHasDistinctSpeedCadenceDodgeAndJump()
        {
            for(int level=1;level<=3;level++)
            {
                Assert.That(SpeedPreviewAnimator.PixelsPerSecond(level),Is.GreaterThan(SpeedPreviewAnimator.PixelsPerSecond(level-1)));
                Assert.That(SpeedPreviewAnimator.FramesPerSecond(level),Is.GreaterThan(SpeedPreviewAnimator.FramesPerSecond(level-1)));
                Assert.That(JumpPreviewAnimator.Altitude(.5f,level),Is.GreaterThan(JumpPreviewAnimator.Altitude(.5f,level-1)));
                Assert.That(AgilityPreviewAnimator.Offset(.5f,level).y,Is.GreaterThan(AgilityPreviewAnimator.Offset(.5f,level-1).y));
                Assert.That(AgilityPreviewAnimator.Duration(level),Is.LessThan(AgilityPreviewAnimator.Duration(level-1)));
            }
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void JumpReturnsToGroundAtBothEnds(int level)
        {
            Assert.That(JumpPreviewAnimator.Altitude(0,level),Is.Zero);
            Assert.That(JumpPreviewAnimator.Altitude(1,level),Is.Zero);
            Assert.That(JumpPreviewAnimator.Altitude(.5f,level),Is.EqualTo(JumpPreviewAnimator.Height(level)));
        }
    }
}
