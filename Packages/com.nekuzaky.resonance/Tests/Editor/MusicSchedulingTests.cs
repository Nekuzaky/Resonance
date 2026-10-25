using NUnit.Framework;

namespace Nekuzaky.Resonance.Tests
{
    public sealed class MusicSchedulingTests : Fixture
    {
        [Test]
        public void LateUpdate_SchedulesTheNextSegmentInTheFuture()
        {
            var track = Asset<MusicTrack>("Score");
            track.segments.Add(new MusicSegment { name = "Loop", clip = Clip("loop", 2f), bars = 1 });
            Engine.Music.Play(track, 100.0);

            var late = 100.0 + MusicPlayer.Lookahead + track.BarLength + 1.0;
            Engine.Music.Tick(late);

            Assert.IsNotNull(Engine.Music.Pending);
            Assert.GreaterOrEqual(Engine.Music.PendingTime, late, "A segment scheduled in the past starts late and drifts from the beat grid");
        }
    }
}
