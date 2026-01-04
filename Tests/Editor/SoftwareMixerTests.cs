using NUnit.Framework;

namespace Audio.Tests.Editor
{
    /// <summary>
    /// Unit tests for SoftwareMixer.
    /// </summary>
    [TestFixture]
    public class SoftwareMixerTests
    {
        private SoftwareMixer _mixer;

        [SetUp]
        public void SetUp()
        {
            _mixer = new SoftwareMixer();
        }

        #region Initial State

        [Test]
        public void InitialState_MasterVolume_IsOne()
        {
            Assert.AreEqual(1f, _mixer.GetVolume(AudioLayer.Master), 0.0001f);
        }

        [Test]
        public void InitialState_AllLayerVolumes_AreOne()
        {
            Assert.AreEqual(1f, _mixer.GetVolume(AudioLayer.SFX), 0.0001f);
            Assert.AreEqual(1f, _mixer.GetVolume(AudioLayer.Music), 0.0001f);
            Assert.AreEqual(1f, _mixer.GetVolume(AudioLayer.Dialogue), 0.0001f);
        }

        [Test]
        public void InitialState_NoLayersMuted()
        {
            Assert.IsFalse(_mixer.IsMuted(AudioLayer.SFX));
            Assert.IsFalse(_mixer.IsMuted(AudioLayer.Music));
            Assert.IsFalse(_mixer.IsMuted(AudioLayer.Dialogue));
        }

        [Test]
        public void InitialState_NotMasterMuted()
        {
            Assert.IsFalse(_mixer.IsMuted(AudioLayer.Master));
        }

        #endregion

        #region Master Volume

        [Test]
        public void SetMasterVolume_UpdatesValue()
        {
            _mixer.SetVolume(AudioLayer.Master, 0.5f);
            Assert.AreEqual(0.5f, _mixer.GetVolume(AudioLayer.Master), 0.0001f);
        }

        [Test]
        public void SetMasterVolume_ClampsToZero()
        {
            _mixer.SetVolume(AudioLayer.Master, -1f);
            Assert.AreEqual(0f, _mixer.GetVolume(AudioLayer.Master), 0.0001f);
        }

        [Test]
        public void SetMasterVolume_ClampsToOne()
        {
            _mixer.SetVolume(AudioLayer.Master, 2f);
            Assert.AreEqual(1f, _mixer.GetVolume(AudioLayer.Master), 0.0001f);
        }

        [Test]
        public void SetMasterMute_MutesAll()
        {
            _mixer.SetMuted(AudioLayer.Master, true);

            float result = _mixer.GetEffectiveVolume(AudioLayer.SFX);
            Assert.AreEqual(0f, result);
        }

        #endregion

        #region Layer Volume

        [Test]
        public void SetLayerVolume_UpdatesValue()
        {
            _mixer.SetVolume(AudioLayer.Music, 0.7f);
            Assert.AreEqual(0.7f, _mixer.GetVolume(AudioLayer.Music), 0.0001f);
        }

        [Test]
        public void SetLayerVolume_DoesNotAffectOtherLayers()
        {
            _mixer.SetVolume(AudioLayer.Music, 0.5f);

            Assert.AreEqual(1f, _mixer.GetVolume(AudioLayer.SFX), 0.0001f);
            Assert.AreEqual(1f, _mixer.GetVolume(AudioLayer.Dialogue), 0.0001f);
        }

        [Test]
        public void SetLayerMute_MutesOnlyThatLayer()
        {
            _mixer.SetMuted(AudioLayer.Music, true);

            Assert.IsTrue(_mixer.IsMuted(AudioLayer.Music));
            Assert.IsFalse(_mixer.IsMuted(AudioLayer.SFX));
            Assert.IsFalse(_mixer.IsMuted(AudioLayer.Dialogue));
        }

        [Test]
        public void MutedLayer_ReturnsZeroEffectiveVolume()
        {
            _mixer.SetMuted(AudioLayer.SFX, true);

            float result = _mixer.GetEffectiveVolume(AudioLayer.SFX);
            Assert.AreEqual(0f, result);
        }

        #endregion

        #region Effective Volume

        [Test]
        public void GetEffectiveVolume_CombinesMasterAndLayer()
        {
            _mixer.SetVolume(AudioLayer.Master, 0.8f);
            _mixer.SetVolume(AudioLayer.SFX, 0.5f);

            // layerVolume * masterVolume = 0.5 * 0.8 = 0.4
            float result = _mixer.GetEffectiveVolume(AudioLayer.SFX);
            Assert.AreEqual(0.4f, result, 0.0001f);
        }

        #endregion

        #region Ducking

        [Test]
        public void Ducking_WhenDialoguePlays_ReducesMusicVolume()
        {
            _mixer.SetDuckingEnabled(true);
            _mixer.SetDuckingAmount(0.3f);
            _mixer.OnDialogueStarted();

            float musicVolume = _mixer.GetEffectiveVolume(AudioLayer.Music);
            float sfxVolume = _mixer.GetEffectiveVolume(AudioLayer.SFX);

            Assert.Less(musicVolume, sfxVolume);
            Assert.AreEqual(0.3f, musicVolume, 0.0001f);
        }

        [Test]
        public void Ducking_WhenDialogueStops_RestoresMusicVolume()
        {
            _mixer.SetDuckingEnabled(true);
            _mixer.SetDuckingAmount(0.3f);
            _mixer.OnDialogueStarted();
            _mixer.OnDialogueStopped();

            float result = _mixer.GetEffectiveVolume(AudioLayer.Music);
            Assert.AreEqual(1f, result, 0.0001f);
        }

        [Test]
        public void Ducking_DoesNotAffectOtherLayers()
        {
            _mixer.SetDuckingEnabled(true);
            _mixer.SetDuckingAmount(0.3f);
            _mixer.OnDialogueStarted();

            float sfxVolume = _mixer.GetEffectiveVolume(AudioLayer.SFX);
            Assert.AreEqual(1f, sfxVolume, 0.0001f);
        }

        [Test]
        public void Ducking_CombinesWithOtherFactors()
        {
            _mixer.SetVolume(AudioLayer.Master, 0.8f);
            _mixer.SetVolume(AudioLayer.Music, 0.5f);
            _mixer.SetDuckingEnabled(true);
            _mixer.SetDuckingAmount(0.5f);
            _mixer.OnDialogueStarted();

            // 0.5 (layer) * 0.8 (master) * 0.5 (ducking) = 0.2
            float result = _mixer.GetEffectiveVolume(AudioLayer.Music);
            Assert.AreEqual(0.2f, result, 0.0001f);
        }

        [Test]
        public void Ducking_WhenDisabled_DoesNotAffectVolume()
        {
            _mixer.SetDuckingEnabled(false);
            _mixer.SetDuckingAmount(0.3f);
            _mixer.OnDialogueStarted();

            float musicVolume = _mixer.GetEffectiveVolume(AudioLayer.Music);
            Assert.AreEqual(1f, musicVolume, 0.0001f);
        }

        [Test]
        public void GetDuckingMultiplier_WhenDialogueActive_ReturnsDuckingAmount()
        {
            _mixer.SetDuckingEnabled(true);
            _mixer.SetDuckingAmount(0.3f);
            _mixer.OnDialogueStarted();

            float multiplier = _mixer.GetDuckingMultiplier(AudioLayer.Music);
            Assert.AreEqual(0.3f, multiplier, 0.0001f);
        }

        [Test]
        public void GetDuckingMultiplier_WhenNoDialogue_ReturnsOne()
        {
            _mixer.SetDuckingEnabled(true);
            _mixer.SetDuckingAmount(0.3f);

            float multiplier = _mixer.GetDuckingMultiplier(AudioLayer.Music);
            Assert.AreEqual(1f, multiplier, 0.0001f);
        }

        #endregion

        #region Events

        [Test]
        public void VolumeChanged_FiresOnMasterChange()
        {
            bool eventFired = false;
            AudioLayer changedLayer = AudioLayer.SFX;
            float newVolume = 0f;
            _mixer.OnVolumeChanged += (layer, vol) =>
            {
                eventFired = true;
                changedLayer = layer;
                newVolume = vol;
            };

            _mixer.SetVolume(AudioLayer.Master, 0.5f);

            Assert.IsTrue(eventFired);
            Assert.AreEqual(AudioLayer.Master, changedLayer);
            Assert.AreEqual(0.5f, newVolume, 0.0001f);
        }

        [Test]
        public void VolumeChanged_FiresOnLayerChange()
        {
            bool eventFired = false;
            AudioLayer changedLayer = AudioLayer.Master;
            _mixer.OnVolumeChanged += (layer, _) =>
            {
                eventFired = true;
                changedLayer = layer;
            };

            _mixer.SetVolume(AudioLayer.SFX, 0.5f);

            Assert.IsTrue(eventFired);
            Assert.AreEqual(AudioLayer.SFX, changedLayer);
        }

        [Test]
        public void MuteChanged_FiresOnChange()
        {
            bool eventFired = false;
            AudioLayer changedLayer = AudioLayer.Master;
            bool newMuteState = false;
            _mixer.OnMuteChanged += (layer, muted) =>
            {
                eventFired = true;
                changedLayer = layer;
                newMuteState = muted;
            };

            _mixer.SetMuted(AudioLayer.Music, true);

            Assert.IsTrue(eventFired);
            Assert.AreEqual(AudioLayer.Music, changedLayer);
            Assert.IsTrue(newMuteState);
        }

        [Test]
        public void VolumeChanged_DoesNotFireWhenSameValue()
        {
            int fireCount = 0;
            _mixer.OnVolumeChanged += (_, _) => fireCount++;

            _mixer.SetVolume(AudioLayer.SFX, 1f); // Same as initial
            _mixer.SetVolume(AudioLayer.SFX, 1f); // Same again

            Assert.AreEqual(0, fireCount);
        }

        [Test]
        public void MuteChanged_DoesNotFireWhenSameState()
        {
            int fireCount = 0;
            _mixer.OnMuteChanged += (_, _) => fireCount++;

            _mixer.SetMuted(AudioLayer.SFX, false); // Same as initial
            _mixer.SetMuted(AudioLayer.SFX, false); // Same again

            Assert.AreEqual(0, fireCount);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void MultipleLayersMuted_AllReturnZero()
        {
            _mixer.SetMuted(AudioLayer.SFX, true);
            _mixer.SetMuted(AudioLayer.Music, true);

            Assert.AreEqual(0f, _mixer.GetEffectiveVolume(AudioLayer.SFX));
            Assert.AreEqual(0f, _mixer.GetEffectiveVolume(AudioLayer.Music));
            Assert.AreEqual(1f, _mixer.GetEffectiveVolume(AudioLayer.Dialogue));
        }

        [Test]
        public void ToggleMute_WorksCorrectly()
        {
            _mixer.SetMuted(AudioLayer.SFX, true);
            Assert.IsTrue(_mixer.IsMuted(AudioLayer.SFX));

            _mixer.SetMuted(AudioLayer.SFX, false);
            Assert.IsFalse(_mixer.IsMuted(AudioLayer.SFX));
        }

        [Test]
        public void MultipleDialogues_DuckingRemainsActive()
        {
            _mixer.SetDuckingEnabled(true);
            _mixer.SetDuckingAmount(0.3f);

            _mixer.OnDialogueStarted();
            _mixer.OnDialogueStarted();
            _mixer.OnDialogueStopped();

            // Still one dialogue active
            float multiplier = _mixer.GetDuckingMultiplier(AudioLayer.Music);
            Assert.AreEqual(0.3f, multiplier, 0.0001f);

            _mixer.OnDialogueStopped();

            // No dialogues active
            multiplier = _mixer.GetDuckingMultiplier(AudioLayer.Music);
            Assert.AreEqual(1f, multiplier, 0.0001f);
        }

        #endregion
    }
}
