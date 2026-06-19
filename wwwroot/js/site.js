window.playAudioById = function(id) {
  try {
    const audio = document.getElementById(id);
    if (!audio) { console.warn('audio element not found', id); return; }
    // Unmute if needed and play
    audio.currentTime = 0;
    const p = audio.play();
    if (p && p.catch) p.catch(err => console.warn('audio play failed', err));
  } catch (err) {
    console.warn('playAudioById error', err);
  }
};

window.pauseAudioById = function(id) {
  try {
    const audio = document.getElementById(id);
    if (!audio) { console.warn('audio element not found', id); return; }
    audio.pause();
  } catch (err) {
    console.warn('pauseAudioById error', err);
  }
};

window.stopAudioById = function(id) {
  try {
    const audio = document.getElementById(id);
    if (!audio) { console.warn('audio element not found', id); return; }
    audio.pause();
    audio.currentTime = 0;
  } catch (err) {
    console.warn('stopAudioById error', err);
  }
};
